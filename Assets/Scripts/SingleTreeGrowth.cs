using System.Collections;
using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// 1本の杉成長シミュレーション
    /// TreeContainer配下の6つのPrefabをSetActiveで切り替え、クロスフェード演出で自然な成長を表現
    /// </summary>
    public class SingleTreeGrowth : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("成長データベース")]
        public GrowthDatabase growthDatabase;

        [Header("Tree Container")]
        [Tooltip("6段階すべてのPrefabを含むコンテナ")]
        public Transform treeContainer;

        [Header("Growth Settings")]
        [Tooltip("クロスフェード時間（秒）")]
        public float crossFadeDuration = 1.0f;

        // モデル高さ標準化のための定数
        private const float NORMALIZED_BASE_HEIGHT = 4.464f; // 標準化後の基準高さ（元3.72m × Scale1.2）
        private const float BASE_DIAMETER = 6.3f; // 基準直径（cm、林齢10年の直径）

        // 各モデルの標準化スケール係数（元の高さの違いを吸収）
        private readonly float[] modelNormalizationScales = new float[] {
            1.0000f,  // Age10_Tree (cedar_01: 3.72m)
            0.9789f,  // Age25_Tree (cedar_03: 3.80m)
            0.9538f,  // Age40_Tree (cedar_05: 3.90m)
            0.9370f,  // Age55_Tree (cedar_04: 3.97m)
            0.8532f,  // Age75_Tree (cedar_06: 4.36m)
            0.7832f,  // Age100_Tree (cedar_02: 4.75m)
        };

        private GameObject[] treeObjects = new GameObject[6];
        private int currentAge = 10;
        private int currentTreeIndex = 0;
        private Coroutine fadeCoroutine;
        private Coroutine scaleCoroutine;

        private void Start()
        {
            // TreeContainer配下の6つのGameObjectを取得
            if (treeContainer == null)
            {
                Debug.LogError("SingleTreeGrowth: treeContainer is null");
                return;
            }

            if (treeContainer.childCount != 6)
            {
                Debug.LogError($"SingleTreeGrowth: treeContainer should have 6 children, but has {treeContainer.childCount}");
                return;
            }

            // Age10_Tree, Age25_Tree, Age40_Tree, Age55_Tree, Age75_Tree, Age100_Treeの順に取得
            for (int i = 0; i < 6; i++)
            {
                treeObjects[i] = treeContainer.GetChild(i).gameObject;
            }

            // 全ての木を初期化：SetActive(true) + Alpha=0（透明状態で待機）
            InitializeTreeAlphas();

            // 初期状態（林齢10年）で木を表示
            SetAge(10);
        }

        /// <summary>
        /// 全ての木の初期化：SetActive(true) + Alpha=0（透明状態）
        /// </summary>
        private void InitializeTreeAlphas()
        {
            Debug.Log("[InitializeTreeAlphas] 全モデルをSetActive(true) + Alpha=0に初期化");
            foreach (var tree in treeObjects)
            {
                if (tree == null) continue;
                // 全モデルをActive化して透明状態に
                tree.SetActive(true);
                SetTreeAlpha(tree, 0f);
            }
            Debug.Log("[InitializeTreeAlphas] 初期化完了");
        }

        /// <summary>
        /// 林齢設定（外部から呼び出し）
        /// </summary>
        public void SetAge(int age)
        {
            if (age == currentAge)
                return;

            Debug.Log($"[SetAge] 林齢変更: {currentAge}年 → {age}年");
            currentAge = age;
            UpdateTree(age);
        }

        /// <summary>
        /// 木の更新（SetActive切り替え + スケーリング）
        /// </summary>
        private void UpdateTree(int age)
        {
            GrowthData data = growthDatabase.GetDataByAge(age);
            if (data == null)
            {
                data = growthDatabase.GetNearestDataByAge(age);
            }

            if (data == null)
            {
                Debug.LogError($"SingleTreeGrowth: No data found for age {age}");
                return;
            }

            // 適切なTreeIndexを選択
            int targetTreeIndex = SelectTreeIndexByAge(age);
            Debug.Log($"[UpdateTree] Age={age}年 → TargetTreeIndex={targetTreeIndex}");

            // メッシュ切り替えが必要か確認
            bool needsMeshChange = (targetTreeIndex != currentTreeIndex);

            if (needsMeshChange)
            {
                Debug.Log($"[UpdateTree] メッシュ切り替え必要: CurrentIndex={currentTreeIndex} → TargetIndex={targetTreeIndex}");
                // クロスフェードでメッシュ差し替え
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                    Debug.Log($"[UpdateTree] 実行中のフェードを停止");
                }
                fadeCoroutine = StartCoroutine(CrossFadeToNewMesh(targetTreeIndex, data));
            }
            else
            {
                Debug.Log($"[UpdateTree] 同じメッシュ: スケーリングアニメーション開始");
                // スケーリングアニメーション実行
                if (scaleCoroutine != null)
                {
                    StopCoroutine(scaleCoroutine);
                    Debug.Log($"[UpdateTree] 実行中のスケーリングアニメーションを停止");
                }
                scaleCoroutine = StartCoroutine(AnimateScaling(treeObjects[currentTreeIndex], data, crossFadeDuration));
            }
        }

        /// <summary>
        /// 林齢に応じたTreeIndex選択（0=Age10, 1=Age25, 2=Age40, 3=Age55, 4=Age75, 5=Age100）
        /// </summary>
        private int SelectTreeIndexByAge(int age)
        {
            if (age <= 17) return 0; // Age10_Tree
            if (age <= 32) return 1; // Age25_Tree
            if (age <= 47) return 2; // Age40_Tree
            if (age <= 67) return 3; // Age55_Tree
            if (age <= 87) return 4; // Age75_Tree
            return 5; // Age100_Tree
        }

        /// <summary>
        /// スケーリングアニメーション（同一メッシュ内での成長）
        /// </summary>
        private IEnumerator AnimateScaling(GameObject tree, GrowthData data, float duration)
        {
            if (tree == null)
            {
                Debug.LogWarning("[AnimateScaling] tree is null");
                scaleCoroutine = null;
                yield break;
            }

            // 現在のスケール取得
            Vector3 startScale = tree.transform.localScale;

            // 目標スケール計算（ApplyScaling()と同じロジック）
            int treeIndex = System.Array.IndexOf(treeObjects, tree);
            if (treeIndex < 0 || treeIndex >= modelNormalizationScales.Length)
            {
                Debug.LogWarning($"[AnimateScaling] Invalid tree index");
                scaleCoroutine = null;
                yield break;
            }

            float normalizationScale = modelNormalizationScales[treeIndex];
            float heightScale = data.height / NORMALIZED_BASE_HEIGHT;
            float diameterScale = data.diameter / BASE_DIAMETER;

            Vector3 targetScale = new Vector3(
                normalizationScale * diameterScale,
                normalizationScale * heightScale,
                normalizationScale * diameterScale
            );

            Debug.Log($"[AnimateScaling開始] Tree={tree.name}, Duration={duration}秒, StartScale={startScale}, TargetScale={targetScale}");

            // 補間アニメーション
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                tree.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

                // 10フレームごとにログ出力
                if (Time.frameCount % 10 == 0)
                {
                    Debug.Log($"[AnimateScaling進行中] t={t:F3}, CurrentScale={tree.transform.localScale}, elapsed={elapsed:F3}秒");
                }

                yield return null;
            }

            // 最終値を確実に設定
            tree.transform.localScale = targetScale;
            Debug.Log($"[AnimateScaling完了] FinalScale={targetScale}");

            scaleCoroutine = null;
        }

        /// <summary>
        /// クロスフェードで新しいメッシュに差し替え（スケーリングアニメーション統合）
        /// 全モデルは常にSetActive(true)なのでAlphaのみで制御
        /// </summary>
        private IEnumerator CrossFadeToNewMesh(int newTreeIndex, GrowthData data)
        {
            GameObject oldTree = treeObjects[currentTreeIndex];
            GameObject newTree = treeObjects[newTreeIndex];

            Debug.Log($"[CrossFade開始] 旧TreeIndex={currentTreeIndex} → 新TreeIndex={newTreeIndex}");

            // 旧Treeの現在スケールを取得
            Vector3 oldStartScale = oldTree.transform.localScale;

            // 旧Treeの目標スケール計算（現在のデータに基づく）
            GrowthData oldData = growthDatabase.GetDataByAge(currentAge);
            Vector3 oldTargetScale = CalculateTargetScale(currentTreeIndex, oldData);

            // 新Treeの目標スケール計算（既にSetActive(true)なのでスケールのみ設定）
            Vector3 newTargetScale = CalculateTargetScale(newTreeIndex, data);
            newTree.transform.localScale = newTargetScale;

            // 新TreeはAlpha=0から開始（既に透明状態で待機中）
            SetTreeAlpha(newTree, 0f);

            Debug.Log($"[CrossFade] 新Tree Alpha=0.0, Scale={newTargetScale}に設定完了（既にSetActive=true）");

            // 1フレーム待機
            yield return null;

            float elapsed = 0f;
            Debug.Log($"[CrossFade] フェード+スケーリング開始: Duration={crossFadeDuration}秒");

            // クロスフェード + スケーリングアニメーション
            while (elapsed < crossFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / crossFadeDuration;

                if (oldTree != null)
                {
                    SetTreeAlpha(oldTree, 1f - t); // 旧: 不透明→透明
                    // 旧Treeも目標スケールに向けてアニメーション
                    oldTree.transform.localScale = Vector3.Lerp(oldStartScale, oldTargetScale, t);
                }

                SetTreeAlpha(newTree, t); // 新: 透明→不透明（滑らかにフェードイン）

                // 10フレームごとにログ出力
                if (Time.frameCount % 10 == 0)
                {
                    Debug.Log($"[CrossFade進行中] t={t:F3}, 旧Alpha={1f - t:F3}, 新Alpha={t:F3}, elapsed={elapsed:F3}秒");
                }

                yield return null;
            }

            Debug.Log($"[CrossFade完了] フェード終了");

            // フェード完了: 旧TreeをAlpha=0に戻す（SetActiveはそのまま）
            if (oldTree != null)
            {
                SetTreeAlpha(oldTree, 0f); // 透明状態で待機
                oldTree.transform.localScale = oldTargetScale; // 最終スケール確定
                Debug.Log($"[CrossFade] 旧Tree Alpha=0.0, Scale={oldTargetScale}（透明状態で待機）");
            }

            // 新メッシュを完全不透明に、スケール確定
            SetTreeAlpha(newTree, 1f);
            newTree.transform.localScale = newTargetScale;
            Debug.Log($"[CrossFade] 新Tree Alpha=1.0, Scale={newTargetScale}に設定完了");

            currentTreeIndex = newTreeIndex;
            fadeCoroutine = null;
        }

        /// <summary>
        /// 目標スケール計算（共通ロジック）
        /// </summary>
        private Vector3 CalculateTargetScale(int treeIndex, GrowthData data)
        {
            if (treeIndex < 0 || treeIndex >= modelNormalizationScales.Length || data == null)
            {
                return Vector3.one;
            }

            float normalizationScale = modelNormalizationScales[treeIndex];
            float heightScale = data.height / NORMALIZED_BASE_HEIGHT;
            float diameterScale = data.diameter / BASE_DIAMETER;

            return new Vector3(
                normalizationScale * diameterScale,
                normalizationScale * heightScale,
                normalizationScale * diameterScale
            );
        }

        /// <summary>
        /// 木のスケーリング適用
        /// </summary>
        private void ApplyScaling(GameObject tree, GrowthData data)
        {
            if (tree == null)
                return;

            // 現在のTreeIndexを取得
            int treeIndex = System.Array.IndexOf(treeObjects, tree);
            if (treeIndex < 0 || treeIndex >= modelNormalizationScales.Length)
            {
                Debug.LogWarning($"SingleTreeGrowth: Invalid tree index for scaling");
                return;
            }

            // ステップ1: モデルの標準化スケールを適用（全モデルを同じ基準高さに統一）
            float normalizationScale = modelNormalizationScales[treeIndex];

            // ステップ2: データの高さに合わせてスケーリング
            // NORMALIZED_BASE_HEIGHT (4.464m) を data.height に変換
            float heightScale = data.height / NORMALIZED_BASE_HEIGHT;

            // ステップ3: データの太さに合わせてスケーリング
            // BASE_DIAMETER (6.3cm) を data.diameter に変換
            float diameterScale = data.diameter / BASE_DIAMETER;

            // 最終スケール
            float finalHeightScale = normalizationScale * heightScale;  // Y軸: 高さ
            float finalDiameterScale = normalizationScale * diameterScale;  // XZ軸: 太さ

            // Y軸は高さ、XZ軸は太さを適用
            tree.transform.localScale = new Vector3(finalDiameterScale, finalHeightScale, finalDiameterScale);
        }

        /// <summary>
        /// 木の透明度設定（各モデルに専用マテリアルが割り当て済み）
        /// </summary>
        private void SetTreeAlpha(GameObject tree, float alpha)
        {
            if (tree == null)
                return;

            Renderer[] renderers = tree.GetComponentsInChildren<Renderer>();
            int materialCount = 0;
            int changedCount = 0;

            foreach (var renderer in renderers)
            {
                // renderer.materialsでインスタンスマテリアルを取得（アセット変更を防ぐ）
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materialCount++;
                    if (materials[i] != null)
                    {
                        string shaderName = materials[i].shader.name;

                        // TVEシェーダーの場合
                        if (materials[i].HasProperty("_MainColor"))
                        {
                            // _MainColorのAlpha値を変更
                            Color mainColor = materials[i].GetColor("_MainColor");
                            mainColor.a = alpha;
                            materials[i].SetColor("_MainColor", mainColor);

                            // TVEシェーダーの_GlobalAlphaを変更（0.0～1.0で透明度制御）
                            if (materials[i].HasProperty("_GlobalAlpha"))
                            {
                                materials[i].SetFloat("_GlobalAlpha", alpha);
                            }

                            // TVEシェーダーの_LocalAlphaを変更（0.0～1.0で透明度制御）
                            if (materials[i].HasProperty("_LocalAlpha"))
                            {
                                materials[i].SetFloat("_LocalAlpha", alpha);
                            }

                            Debug.Log($"[SetTreeAlpha] Mat={materials[i].name}, Shader={shaderName}, _MainColor.a={alpha:F3}");
                            changedCount++;
                        }
                        // 標準シェーダーの_Colorを変更（TVEシェーダーにはない）
                        else if (materials[i].HasProperty("_Color"))
                        {
                            Color color = materials[i].GetColor("_Color");
                            color.a = alpha;
                            materials[i].SetColor("_Color", color);
                            Debug.Log($"[SetTreeAlpha] Mat={materials[i].name}, Shader={shaderName}, _Color.a={alpha:F3}");
                            changedCount++;
                        }
                        else
                        {
                            Debug.LogWarning($"[SetTreeAlpha] Mat={materials[i].name}, Shader={shaderName}, プロパティ_MainColorも_Colorもなし");
                        }
                    }
                }
                // 変更したマテリアル配列をRendererに再設定
                renderer.materials = materials;
            }

            Debug.Log($"[SetTreeAlpha] Tree={tree.name}, Alpha={alpha:F3}, マテリアル数={materialCount}, 変更数={changedCount}");
        }

        private void OnDestroy()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
        }
    }
}
