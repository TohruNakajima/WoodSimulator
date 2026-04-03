using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmartCreator.ProceduralTrees;

namespace WoodSimulator
{
    /// <summary>
    /// プロシージャル林の全体管理。
    /// Poisson配置で木を生成し、年齢変更時に段階的にGenerate()を再実行する。
    /// 間伐時は成長度合い（growthFactor）が小さい個体を優先して伐採する。
    /// </summary>
    public class ProceduralForestManager : MonoBehaviour
    {
        [Header("Growth Data")]
        [Tooltip("成長データベース")]
        public GrowthDatabase growthDatabase;

        [Header("Forest Layout")]
        [Tooltip("配置エリアの中心")]
        public Vector3 forestCenter = Vector3.zero;

        [Tooltip("Terrain上に配置する場合にセット（Y座標を地形に合わせる）")]
        public Terrain terrain;

        [Tooltip("Terrain上でこの高さ(m)以下には木を配置しない（川底除外）")]
        public float minTerrainHeight = 5f;

        [Tooltip("配置エリアのサイズ (X, Z)")]
        public Vector2 forestSize = new Vector2(32f, 32f);

        [Tooltip("初期最大本数（3000本/haの1/10スケール）")]
        public int maxTreeCount = 300;

        [Tooltip("配置ランダムシード")]
        public int baseSeed = 12345;

        [Header("Materials")]
        [Tooltip("樹皮マテリアル")]
        public Material barkMaterial;

        [Tooltip("葉マテリアル")]
        public Material leafMaterial;

        [Header("Performance")]
        [Tooltip("1フレームあたりのGenerate()に使える時間バジェット（ミリ秒）")]
        public float frameBudgetMs = 16f;


        [Header("Thinning")]
        [Tooltip("間伐フェードアウト時間（秒）")]
        public float thinningFadeDuration = 1.0f;

        [Tooltip("表示スケール（3000本/ha → displayScale倍）")]
        public float displayScale = 0.1f;

        // --- 内部状態 ---
        private List<ProceduralTreeInstance> allTrees = new List<ProceduralTreeInstance>();
        private List<ProceduralTreeInstance> activeTreesCache = new List<ProceduralTreeInstance>();
        private Transform treeContainer;
        private int currentStageIndex = -1;
        private Coroutine updateCoroutine;
        private int updateProgress;
        private int updateTotal;

        // --- イベント ---
        /// <summary>更新進捗通知 (current, total)</summary>
        public event Action<int, int> OnUpdateProgress;
        /// <summary>年齢変更通知 (stageIndex, GrowthData)</summary>
        public event Action<int, GrowthData> OnAgeChanged;

        public int CurrentStageIndex => currentStageIndex;
        public bool IsUpdating => updateCoroutine != null;

        private void Awake()
        {
            treeContainer = new GameObject("TreeContainer").transform;
            treeContainer.SetParent(transform, false);
        }

        private void Start()
        {
            if (growthDatabase == null || growthDatabase.Count == 0)
            {
                Debug.LogError("[ProceduralForestManager] GrowthDatabase is missing or empty.");
                return;
            }

            InitializeForest();
            SetStage(0);
        }

        /// <summary>
        /// Poisson配置で木のインスタンスを生成する。
        /// </summary>
        private void InitializeForest()
        {
            UnityEngine.Random.InitState(baseSeed);
            var points = PoissonDiskSampling.GeneratePointsWithCount(forestCenter, forestSize, maxTreeCount);

            // 生成本数がmaxTreeCountを超える場合は切り詰め
            int count = Mathf.Min(points.Count, maxTreeCount);

            for (int i = 0; i < count; i++)
            {
                int treeSeed = baseSeed + i;
                var instance = new ProceduralTreeInstance(treeSeed, points[i]);

                // GameObjectを生成（Terrainがあれば高さを合わせる、川底除外）
                Vector3 pos = instance.position;
                if (terrain != null)
                {
                    float terrainY = terrain.SampleHeight(pos) + terrain.transform.position.y;
                    if (terrainY < minTerrainHeight)
                        continue; // 川底・低地をスキップ
                    pos.y = terrainY;
                }

                var go = new GameObject("CedarTree");
                go.transform.SetParent(treeContainer, false);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(0f, instance.rotationY, 0f);

                var gen = go.AddComponent<PineTreeGenerator>();
                gen.autoRegenerate = false; // 手動管理
                gen.barkMaterial = barkMaterial;
                gen.leafMaterial = leafMaterial;

                // LOD1用の簡略Generator（子オブジェクトに配置）
                var lod1GO = new GameObject("LOD1");
                lod1GO.transform.SetParent(go.transform, false);
                var lod1Gen = lod1GO.AddComponent<PineTreeGenerator>();
                lod1Gen.autoRegenerate = false;
                lod1Gen.barkMaterial = barkMaterial;
                lod1Gen.leafMaterial = leafMaterial;
                lod1Gen.runtimeQuality = 0.3f;

                // ThinningAnimatorを事前にアタッチ（無効状態）
                var animator = go.AddComponent<ThinningAnimator>();
                animator.enabled = false;

                // LODGroup
                var lodGroup = go.AddComponent<LODGroup>();
                instance.lodGroup = lodGroup;
                instance.lod1Generator = lod1Gen;

                instance.gameObject = go;
                instance.generator = gen;
                allTrees.Add(instance);
            }

            // growthFactor昇順ソート（間伐時の選別用）
            allTrees.Sort((a, b) => a.growthFactor.CompareTo(b.growthFactor));

        }

        /// <summary>
        /// 指定ステージ（GrowthDatabaseのインデックス）に遷移する。
        /// </summary>
        public void SetStage(int stageIndex)
        {
            if (growthDatabase == null) return;
            stageIndex = Mathf.Clamp(stageIndex, 0, growthDatabase.Count - 1);

            int prevIndex = currentStageIndex;
            currentStageIndex = stageIndex;

            var data = growthDatabase.growthStages[currentStageIndex];
            OnAgeChanged?.Invoke(currentStageIndex, data);

            // 間伐 or 復活の判定
            if (prevIndex >= 0)
            {
                var prevData = growthDatabase.growthStages[prevIndex];
                int prevDisplayCount = GetDisplayCount(prevData.treeCount);
                int newDisplayCount = GetDisplayCount(data.treeCount);

                if (newDisplayCount < prevDisplayCount)
                {
                    // 前進（間伐）: 小さい木から伐採
                    ApplyThinning(data.age, prevDisplayCount - newDisplayCount);
                }
                else if (newDisplayCount > prevDisplayCount)
                {
                    // 後退（復活）: 間伐された木を復活
                    RestoreThinnedTrees(data.age, newDisplayCount - prevDisplayCount);
                }
            }

            // 全アクティブ木を段階的にGenerate()更新
            if (updateCoroutine != null)
                StopCoroutine(updateCoroutine);
            updateCoroutine = StartCoroutine(UpdateTreesGradually(data));
        }

        /// <summary>
        /// treeCount(本/ha)を表示本数に変換。
        /// </summary>
        private int GetDisplayCount(int treeCountPerHa)
        {
            return Mathf.RoundToInt(treeCountPerHa * displayScale);
        }

        /// <summary>
        /// 全アクティブ木をフレーム時間バジェット内でGenerate()更新するコルーチン。
        /// 1本更新するたびに経過時間をチェックし、バジェット超過でyieldする。
        /// </summary>
        private IEnumerator UpdateTreesGradually(GrowthData data)
        {
            RefreshActiveTreesCache();
            updateTotal = activeTreesCache.Count;
            updateProgress = 0;

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            int lastReportedProgress = 0;

            for (int i = 0; i < activeTreesCache.Count; i++)
            {
                var tree = activeTreesCache[i];
                if (tree.gameObject == null || !tree.gameObject.activeSelf) continue;

                TreeParameterMapper.ApplyToGenerator(tree.generator, data, tree.growthFactor, tree.seed);
                ApplyLOD1Generator(tree.lod1Generator, tree.generator, data, tree.growthFactor, tree.seed);
                SetupLODGroup(tree);
                updateProgress = i + 1;

                if (sw.Elapsed.TotalMilliseconds >= frameBudgetMs)
                {
                    OnUpdateProgress?.Invoke(updateProgress, updateTotal);
                    lastReportedProgress = updateProgress;
                    yield return null;
                    sw.Restart();
                }
            }

            updateProgress = updateTotal;
            OnUpdateProgress?.Invoke(updateProgress, updateTotal);
            updateCoroutine = null;
        }

        /// <summary>
        /// 間伐: growthFactorが小さい順に指定本数を伐採する。
        /// </summary>
        private void ApplyThinning(int currentAge, int removeCount)
        {
            int removed = 0;
            for (int i = 0; i < allTrees.Count && removed < removeCount; i++)
            {
                var tree = allTrees[i];
                if (tree.isThinned || tree.gameObject == null || !tree.gameObject.activeSelf)
                    continue;

                tree.isThinned = true;
                tree.thinnedAtAge = currentAge;

                var animator = tree.gameObject.GetComponent<ThinningAnimator>();
                animator.Play(thinningFadeDuration);
                removed++;
            }
        }

        /// <summary>
        /// 年齢を戻した際に間伐された木を復活させる。
        /// </summary>
        private void RestoreThinnedTrees(int currentAge, int restoreCount)
        {
            int restored = 0;
            // growthFactor大きい順（リスト末尾）から復活
            for (int i = allTrees.Count - 1; i >= 0 && restored < restoreCount; i--)
            {
                var tree = allTrees[i];
                if (!tree.isThinned || tree.thinnedAtAge <= currentAge)
                    continue;

                tree.isThinned = false;
                tree.thinnedAtAge = -1;

                if (tree.gameObject != null)
                {
                    var animator = tree.gameObject.GetComponent<ThinningAnimator>();
                    if (animator != null)
                        animator.Stop();

                    tree.gameObject.SetActive(true);
                    tree.gameObject.transform.localScale = Vector3.one;
                }

                restored++;
            }
        }

        /// <summary>
        /// LOD1用Generatorにパラメータを半減コピーしてGenerate()する。
        /// </summary>
        private void ApplyLOD1Generator(PineTreeGenerator lod1Gen, PineTreeGenerator lod0Gen, GrowthData data, float growthFactor, int seed)
        {
            if (lod1Gen == null) return;

            // LOD0のパラメータをコピーしつつ構造を半減
            lod1Gen.trunkHeight = lod0Gen.trunkHeight;
            lod1Gen.trunkRadius = lod0Gen.trunkRadius;
            lod1Gen.trunkTipRadius = lod0Gen.trunkTipRadius;
            lod1Gen.trunkTaper = lod0Gen.trunkTaper;
            lod1Gen.trunkNoiseStrength = lod0Gen.trunkNoiseStrength;
            lod1Gen.trunkNoiseFrequency = lod0Gen.trunkNoiseFrequency;
            lod1Gen.baseBranchLength = lod0Gen.baseBranchLength;
            lod1Gen.tipBranchLength = lod0Gen.tipBranchLength;
            lod1Gen.branchDownwardAngle = lod0Gen.branchDownwardAngle;
            lod1Gen.branchRandomTilt = lod0Gen.branchRandomTilt;
            lod1Gen.branchThickness = lod0Gen.branchThickness;
            lod1Gen.branchUpCurve = lod0Gen.branchUpCurve;
            lod1Gen.branchDownwardCurve = lod0Gen.branchDownwardCurve;
            lod1Gen.branchStartHeight = lod0Gen.branchStartHeight;
            lod1Gen.branchEndHeight = lod0Gen.branchEndHeight;
            lod1Gen.leafCardLength = lod0Gen.leafCardLength;
            lod1Gen.leafCardWidth = lod0Gen.leafCardWidth;
            lod1Gen.leafBend = lod0Gen.leafBend;
            lod1Gen.seed = seed;

            // 構造を半減
            lod1Gen.whorlCount = Mathf.Max(4, lod0Gen.whorlCount / 2);
            lod1Gen.branchesPerWhorl = Mathf.Max(2, lod0Gen.branchesPerWhorl / 2);
            lod1Gen.baseLeavesPerBranch = Mathf.Max(8, lod0Gen.baseLeavesPerBranch / 3);

            lod1Gen.Generate();
        }

        /// <summary>
        /// LODGroupにLOD0/LOD1のRendererを登録する。Generate()後に呼ぶ。
        /// </summary>
        private void SetupLODGroup(ProceduralTreeInstance tree)
        {
            if (tree.lodGroup == null || tree.gameObject == null) return;

            // LOD0: メインGeneratorの子（Trunk/Branches/Leaves）
            var lod0Renderers = new List<Renderer>();
            foreach (Transform child in tree.gameObject.transform)
            {
                if (child.name == "LOD1") continue;
                var r = child.GetComponent<Renderer>();
                if (r != null) lod0Renderers.Add(r);
            }

            // LOD1: LOD1子オブジェクト内のRenderer
            var lod1Renderers = new List<Renderer>();
            if (tree.lod1Generator != null)
            {
                var lod1Transform = tree.lod1Generator.transform;
                foreach (Transform child in lod1Transform)
                {
                    var r = child.GetComponent<Renderer>();
                    if (r != null) lod1Renderers.Add(r);
                }
            }

            float lod0Ratio = 0.03f;  // 画面の3%以上 → LOD0
            float cullRatio = 0.005f;  // 画面の0.5%以下 → カリング

            var lods = new LOD[2];
            lods[0] = new LOD(lod0Ratio, lod0Renderers.ToArray());
            lods[1] = new LOD(cullRatio, lod1Renderers.ToArray());
            tree.lodGroup.SetLODs(lods);

            // 樹高ベースでLODGroupのサイズを手動設定（RecalculateBoundsは初期メッシュに依存して不正確）
            float treeHeight = tree.generator.trunkHeight;
            tree.lodGroup.size = treeHeight * 1.2f;

        }

        /// <summary>
        /// activeTreesCacheを最新の状態に更新する。
        /// </summary>
        private void RefreshActiveTreesCache()
        {
            activeTreesCache.Clear();
            foreach (var tree in allTrees)
            {
                if (!tree.isThinned && tree.gameObject != null && tree.gameObject.activeSelf)
                    activeTreesCache.Add(tree);
            }
        }

    }
}
