using System.Collections;
using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// 1本の杉成長シミュレーション
    /// 6つのPrefabを段階的に差し替え、クロスフェード演出で自然な成長を表現
    /// </summary>
    public class SingleTreeGrowth : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("成長データベース")]
        public GrowthDatabase growthDatabase;

        [Header("Tree Prefabs (Size Order)")]
        [Tooltip("Age10_Tree (BC_PM_P02_japanese_cedar_01, 3.72m)")]
        public GameObject age10Prefab;

        [Tooltip("Age25_Tree (BC_PM_P02_japanese_cedar_03, 3.80m)")]
        public GameObject age25Prefab;

        [Tooltip("Age40_Tree (BC_PM_P02_japanese_cedar_05, 3.90m)")]
        public GameObject age40Prefab;

        [Tooltip("Age55_Tree (BC_PM_P02_japanese_cedar_04, 3.97m)")]
        public GameObject age55Prefab;

        [Tooltip("Age75_Tree (BC_PM_P02_japanese_cedar_06, 4.36m)")]
        public GameObject age75Prefab;

        [Tooltip("Age100_Tree (BC_PM_P02_japanese_cedar_02, 4.75m)")]
        public GameObject age100Prefab;

        [Header("Growth Settings")]
        [Tooltip("クロスフェード時間（秒）")]
        public float crossFadeDuration = 1.0f;

        private GameObject currentTree;
        private int currentAge = 10;
        private Coroutine fadeCoroutine;

        private void Start()
        {
            // 初期状態（林齢10年）で木を生成
            SetAge(10);
        }

        /// <summary>
        /// 林齢設定（外部から呼び出し）
        /// </summary>
        public void SetAge(int age)
        {
            if (age == currentAge)
                return;

            currentAge = age;
            UpdateTree(age);
        }

        /// <summary>
        /// 木の更新（メッシュ差し替え + スケーリング）
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

            // 適切なPrefabを選択
            GameObject targetPrefab = SelectPrefabByAge(age);
            if (targetPrefab == null)
            {
                Debug.LogError($"SingleTreeGrowth: No prefab found for age {age}");
                return;
            }

            // メッシュ切り替えが必要か確認
            bool needsMeshChange = ShouldChangeMesh(age);

            if (needsMeshChange)
            {
                // クロスフェードでメッシュ差し替え
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }
                fadeCoroutine = StartCoroutine(CrossFadeToNewMesh(targetPrefab, data));
            }
            else
            {
                // 同じメッシュでスケーリングのみ更新
                ApplyScaling(currentTree, data);
            }
        }

        /// <summary>
        /// 林齢に応じたPrefab選択
        /// </summary>
        private GameObject SelectPrefabByAge(int age)
        {
            if (age <= 17) return age10Prefab;
            if (age <= 32) return age25Prefab;
            if (age <= 47) return age40Prefab;
            if (age <= 67) return age55Prefab;
            if (age <= 87) return age75Prefab;
            return age100Prefab;
        }

        /// <summary>
        /// メッシュ変更が必要か判定
        /// </summary>
        private bool ShouldChangeMesh(int age)
        {
            if (currentTree == null)
                return true;

            GameObject targetPrefab = SelectPrefabByAge(age);

            // 現在のメッシュ名と目標Prefab名を比較
            string currentMeshName = currentTree.name.Replace("(Clone)", "").Trim();
            string targetPrefabName = targetPrefab.name;

            return currentMeshName != targetPrefabName;
        }

        /// <summary>
        /// クロスフェードで新しいメッシュに差し替え
        /// </summary>
        private IEnumerator CrossFadeToNewMesh(GameObject newPrefab, GrowthData data)
        {
            // 新しい木をインスタンス化（同じ座標）
            GameObject newTree = Instantiate(newPrefab, transform.position, Quaternion.identity, transform);
            ApplyScaling(newTree, data);

            // 新しい木を完全透明で開始
            SetTreeAlpha(newTree, 0f);

            GameObject oldTree = currentTree;
            float elapsed = 0f;

            // クロスフェード
            while (elapsed < crossFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / crossFadeDuration;

                if (oldTree != null)
                {
                    SetTreeAlpha(oldTree, 1f - t); // 旧: 不透明→透明
                }
                SetTreeAlpha(newTree, t); // 新: 透明→不透明

                yield return null;
            }

            // フェード完了: 旧メッシュ削除
            if (oldTree != null)
            {
                Destroy(oldTree);
            }

            // 新メッシュを完全不透明に
            SetTreeAlpha(newTree, 1f);
            currentTree = newTree;
            fadeCoroutine = null;
        }

        /// <summary>
        /// 木のスケーリング適用
        /// </summary>
        private void ApplyScaling(GameObject tree, GrowthData data)
        {
            if (tree == null)
                return;

            // ベースメッシュの高さを取得（Bounds使用）
            Renderer renderer = tree.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning("SingleTreeGrowth: No renderer found in tree");
                return;
            }

            float baseHeight = renderer.bounds.size.y;
            if (baseHeight <= 0)
            {
                Debug.LogWarning($"SingleTreeGrowth: Invalid base height: {baseHeight}");
                return;
            }

            // 目標高さに合わせてY軸スケーリング
            float scaleRatio = data.height / baseHeight;
            tree.transform.localScale = new Vector3(1f, scaleRatio, 1f);
        }

        /// <summary>
        /// 木の透明度設定
        /// </summary>
        private void SetTreeAlpha(GameObject tree, float alpha)
        {
            if (tree == null)
                return;

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
                    color.a = alpha;
                    mat.color = color;
                }
            }
        }

        private void OnDestroy()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            if (currentTree != null)
            {
                Destroy(currentTree);
            }
        }
    }
}
