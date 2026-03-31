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

            // 初期状態（林齢10年）で木を表示
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

            // メッシュ切り替えが必要か確認
            bool needsMeshChange = (targetTreeIndex != currentTreeIndex);

            if (needsMeshChange)
            {
                // クロスフェードでメッシュ差し替え
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }
                fadeCoroutine = StartCoroutine(CrossFadeToNewMesh(targetTreeIndex, data));
            }
            else
            {
                // 同じメッシュでスケーリングのみ更新
                ApplyScaling(treeObjects[currentTreeIndex], data);
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
        /// クロスフェードで新しいメッシュに差し替え
        /// </summary>
        private IEnumerator CrossFadeToNewMesh(int newTreeIndex, GrowthData data)
        {
            GameObject oldTree = treeObjects[currentTreeIndex];
            GameObject newTree = treeObjects[newTreeIndex];

            // 新しい木をActive化
            newTree.SetActive(true);
            ApplyScaling(newTree, data);

            // 新しい木を完全透明で開始
            SetTreeAlpha(newTree, 0f);

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

            // フェード完了: 旧メッシュ非表示、α値を1に戻す
            if (oldTree != null)
            {
                oldTree.SetActive(false);
                SetTreeAlpha(oldTree, 1f); // 次回表示のためにα値をリセット
            }

            // 新メッシュを完全不透明に
            SetTreeAlpha(newTree, 1f);
            currentTreeIndex = newTreeIndex;
            fadeCoroutine = null;
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
        }
    }
}
