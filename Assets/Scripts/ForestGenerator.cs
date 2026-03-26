using System.Collections.Generic;
using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// 森林生成管理
    /// Poisson Disk Sampling配置、Object Pooling、6メッシュ選択的表示
    /// </summary>
    public class ForestGenerator : MonoBehaviour
    {
        [Header("Database")]
        [Tooltip("成長データベース")]
        public GrowthDatabase growthDatabase;

        [Header("Tree Settings")]
        [Tooltip("樹木Prefab（6メッシュ内包）")]
        public GameObject treePrefab;

        [Header("Forest Area")]
        [Tooltip("森林領域中心")]
        public Vector3 forestCenter = Vector3.zero;

        [Tooltip("森林領域サイズ（m×m）")]
        public Vector2 forestSize = new Vector2(50, 50);

        [Header("Performance")]
        [Tooltip("表示本数倍率（実データ×この値）")]
        [Range(0.01f, 1.0f)]
        public float treeCountScale = 0.1f;

        [Header("Components")]
        private TreePool treePool;
        private List<GameObject> currentTrees = new List<GameObject>();

        // 6メッシュ名マッピング
        private readonly Dictionary<string, string> meshNameMap = new Dictionary<string, string>
        {
            { "Age10_SmallTree", "Age10_SmallTree" },
            { "Age25_YoungTree", "Age25_YoungTree" },
            { "Age40_MediumTree", "Age40_MediumTree" },
            { "Age55_MatureTree", "Age55_MatureTree" },
            { "Age75_OldTree", "Age75_OldTree" },
            { "Age100_AncientTree", "Age100_AncientTree" }
        };

        private void Awake()
        {
            // TreePool初期化
            treePool = GetComponent<TreePool>();
            if (treePool == null)
            {
                treePool = gameObject.AddComponent<TreePool>();
            }

            treePool.treePrefab = treePrefab;
            treePool.initialPoolSize = 500;
            treePool.Initialize();
        }

        /// <summary>
        /// 指定年齢で森林生成
        /// </summary>
        public void GenerateForest(int age)
        {
            if (growthDatabase == null)
            {
                Debug.LogError("ForestGenerator: growthDatabase is not assigned.");
                return;
            }

            GrowthData data = growthDatabase.GetDataByAge(age);
            if (data == null)
            {
                data = growthDatabase.GetNearestDataByAge(age);
                if (data == null)
                {
                    Debug.LogError($"ForestGenerator: No data found for age {age}.");
                    return;
                }
            }

            GenerateForest(data);
        }

        /// <summary>
        /// GrowthDataで森林生成
        /// </summary>
        public void GenerateForest(GrowthData data)
        {
            // 初回のみ最大本数で配置（位置・向き固定）
            if (currentTrees.Count == 0)
            {
                // 最大本数（3000本）で配置
                int maxTreeCount = Mathf.RoundToInt(3000 * treeCountScale);
                List<Vector3> positions = PoissonDiskSampling.GeneratePointsWithCount(
                    forestCenter,
                    forestSize,
                    maxTreeCount
                );

                foreach (Vector3 pos in positions)
                {
                    GameObject tree = treePool.Get(pos, Quaternion.identity);
                    if (tree == null)
                        continue;

                    // モデル選択のみ（位置・向き・スケールは変更しない）
                    SetActiveModel(tree, data.modelName);

                    currentTrees.Add(tree);
                }
            }

            // alpha調整で本数を表現（位置・向き・スケールは変更しない）
            // データは3000→744と減少するので逆転（最大3000-最小744=2256を基準に逆転）
            int invertedTreeCount = 3000 - data.treeCount + 744;
            int displayTreeCount = Mathf.Max(10, Mathf.RoundToInt(invertedTreeCount * treeCountScale));
            UpdateTreeVisibility(displayTreeCount);

            Debug.Log($"ForestGenerator: Showing {displayTreeCount}/{currentTrees.Count} trees (age: {data.age}, inverted from {data.treeCount})");
        }

        /// <summary>
        /// 6メッシュから適切なモデルをアクティブ化
        /// </summary>
        private void SetActiveModel(GameObject tree, string modelName)
        {
            // 全メッシュを無効化
            foreach (Transform child in tree.transform)
            {
                child.gameObject.SetActive(false);
            }

            // 指定モデルのみアクティブ化
            Transform targetMesh = tree.transform.Find(modelName);
            if (targetMesh != null)
            {
                targetMesh.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"ForestGenerator: Model '{modelName}' not found in tree prefab. Available: {GetChildNames(tree)}");
                // フォールバック: 最初の子をアクティブ化
                if (tree.transform.childCount > 0)
                {
                    tree.transform.GetChild(0).gameObject.SetActive(true);
                }
            }
        }


        /// <summary>
        /// 全樹木をプールに返却
        /// </summary>
        public void ReturnAllTrees()
        {
            foreach (GameObject tree in currentTrees)
            {
                if (tree != null)
                {
                    treePool.Return(tree);
                }
            }
            currentTrees.Clear();
        }

        /// <summary>
        /// 間伐実行（指定本数を削除）
        /// </summary>
        public void ApplyThinning(int targetCount)
        {
            // 表示本数を倍率で調整
            int displayTargetCount = Mathf.Max(10, Mathf.RoundToInt(targetCount * treeCountScale));

            if (currentTrees.Count <= displayTargetCount)
            {
                Debug.LogWarning($"ForestGenerator: Current tree count ({currentTrees.Count}) <= target ({displayTargetCount}). No thinning needed.");
                return;
            }

            int thinCount = currentTrees.Count - displayTargetCount;

            // グリッド分割均等選択
            List<GameObject> toRemove = SelectTreesForThinning(thinCount);

            foreach (GameObject tree in toRemove)
            {
                if (tree != null)
                {
                    StartCoroutine(FadeOutAndRemove(tree));
                }
            }

            Debug.Log($"ForestGenerator: Thinning {thinCount} trees ({currentTrees.Count} -> {targetCount})");
        }

        /// <summary>
        /// 間伐対象選択（均等分散）
        /// </summary>
        private List<GameObject> SelectTreesForThinning(int count)
        {
            List<GameObject> selected = new List<GameObject>();

            // 単純な等間隔選択
            float interval = (float)currentTrees.Count / count;
            for (int i = 0; i < count; i++)
            {
                int index = Mathf.FloorToInt(i * interval);
                if (index < currentTrees.Count)
                {
                    selected.Add(currentTrees[index]);
                }
            }

            return selected;
        }

        /// <summary>
        /// フェードアウト演出後に削除
        /// </summary>
        private System.Collections.IEnumerator FadeOutAndRemove(GameObject tree)
        {
            // 半透明化演出（0.5秒）
            float duration = 0.5f;
            float elapsed = 0f;

            Renderer[] renderers = tree.GetComponentsInChildren<Renderer>();
            Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>();

            // 元の色保存
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (!originalColors.ContainsKey(mat))
                    {
                        originalColors[mat] = mat.color;
                    }
                }
            }

            // フェードアウト
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / duration);

                foreach (var renderer in renderers)
                {
                    foreach (var mat in renderer.materials)
                    {
                        if (originalColors.ContainsKey(mat))
                        {
                            Color color = originalColors[mat];
                            color.a = alpha;
                            mat.color = color;
                        }
                    }
                }

                yield return null;
            }

            // プールに返却
            currentTrees.Remove(tree);
            treePool.Return(tree);

            // 色をリセット
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (originalColors.ContainsKey(mat))
                    {
                        mat.color = originalColors[mat];
                    }
                }
            }
        }

        /// <summary>
        /// 子オブジェクト名取得（デバッグ用）
        /// </summary>
        private string GetChildNames(GameObject obj)
        {
            List<string> names = new List<string>();
            foreach (Transform child in obj.transform)
            {
                names.Add(child.name);
            }
            return string.Join(", ", names);
        }

        /// <summary>
        /// 木の透明度を調整して本数を制御
        /// </summary>
        private void UpdateTreeVisibility(int visibleCount)
        {
            // グラデーション範囲（この範囲で徐々に透明化）
            int fadeRange = Mathf.Min(50, currentTrees.Count / 10);

            // 表示する木と透明にする木を決定
            for (int i = 0; i < currentTrees.Count; i++)
            {
                GameObject tree = currentTrees[i];
                if (tree == null)
                    continue;

                float targetAlpha;
                if (i < visibleCount)
                {
                    // 完全表示
                    targetAlpha = 1f;
                }
                else if (i < visibleCount + fadeRange)
                {
                    // グラデーション（徐々に透明化）
                    float fadeProgress = (float)(i - visibleCount) / fadeRange;
                    targetAlpha = 1f - fadeProgress;
                }
                else
                {
                    // 完全透明
                    targetAlpha = 0f;
                }

                // Rendererのalpha設定
                Renderer[] renderers = tree.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    foreach (var mat in renderer.materials)
                    {
                        // Transparentモードに変更
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = 3000;

                        // alpha値設定
                        Color color = mat.color;
                        color.a = targetAlpha;
                        mat.color = color;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            ReturnAllTrees();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 森林領域可視化
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(forestCenter, new Vector3(forestSize.x, 0, forestSize.y));
        }
#endif
    }
}
