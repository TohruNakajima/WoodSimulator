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

                // GameObjectを生成
                var go = new GameObject($"Tree_{i:D3}");
                go.transform.SetParent(treeContainer, false);
                go.transform.position = instance.position;
                go.transform.rotation = Quaternion.Euler(0f, instance.rotationY, 0f);

                var gen = go.AddComponent<PineTreeGenerator>();
                gen.autoRegenerate = false; // 手動管理
                gen.barkMaterial = barkMaterial;
                gen.leafMaterial = leafMaterial;

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
            var activeList = GetActiveTrees();
            updateTotal = activeList.Count;
            updateProgress = 0;

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            for (int i = 0; i < activeList.Count; i++)
            {
                var tree = activeList[i];
                if (tree.gameObject == null || !tree.gameObject.activeSelf) continue;

                TreeParameterMapper.ApplyToGenerator(tree.generator, data, tree.growthFactor, tree.seed);
                updateProgress = i + 1;
                OnUpdateProgress?.Invoke(updateProgress, updateTotal);

                if (sw.Elapsed.TotalMilliseconds >= frameBudgetMs)
                {
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

                var animator = tree.gameObject.AddComponent<ThinningAnimator>();
                animator.Play(thinningFadeDuration, null);
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
                    // ThinningAnimatorが残っていたら除去
                    var animator = tree.gameObject.GetComponent<ThinningAnimator>();
                    if (animator != null)
                        Destroy(animator);

                    tree.gameObject.SetActive(true);
                    tree.gameObject.transform.localScale = Vector3.one;
                }

                restored++;
            }

        }

        /// <summary>
        /// 現在アクティブな（間伐されていない）木のリストを返す。
        /// </summary>
        private List<ProceduralTreeInstance> GetActiveTrees()
        {
            var active = new List<ProceduralTreeInstance>();
            foreach (var tree in allTrees)
            {
                if (!tree.isThinned && tree.gameObject != null && tree.gameObject.activeSelf)
                    active.Add(tree);
            }
            return active;
        }

        /// <summary>
        /// 外部からアクティブ本数を取得。
        /// </summary>
        public int GetActiveTreeCount()
        {
            int count = 0;
            foreach (var tree in allTrees)
            {
                if (!tree.isThinned) count++;
            }
            return count;
        }
    }
}
