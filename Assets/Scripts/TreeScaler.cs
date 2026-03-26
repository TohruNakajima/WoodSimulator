using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// 樹木モデルのY軸スケーリングとテクスチャTiling調整
    /// </summary>
    public class TreeScaler : MonoBehaviour
    {
        [Header("Base Settings")]
        [Tooltip("元のモデル高さ（メートル）")]
        public float baseHeight = 3.72f;

        [Header("Target Settings")]
        [Tooltip("目標高さ（メートル）")]
        public float targetHeight = 5.4f;

        [Header("Texture Settings")]
        [Tooltip("テクスチャTiling調整を有効化（引き伸ばし防止）")]
        public bool adjustTextureTiling = true;

        private Vector3 originalScale;
        private Material[] originalMaterials;

        private void Awake()
        {
            // 元のスケールを保存
            originalScale = transform.localScale;

            // マテリアル保存
            if (adjustTextureTiling)
            {
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (originalMaterials == null)
                    {
                        originalMaterials = renderer.materials;
                    }
                }
            }
        }

        /// <summary>
        /// 指定高さにスケーリング
        /// </summary>
        public void ScaleToHeight(float height)
        {
            targetHeight = height;
            ApplyScale();
        }

        /// <summary>
        /// GrowthDataからスケーリング
        /// </summary>
        public void ScaleFromGrowthData(GrowthData data)
        {
            targetHeight = data.height;
            ApplyScale();
        }

        /// <summary>
        /// スケール適用
        /// </summary>
        private void ApplyScale()
        {
            if (baseHeight <= 0)
            {
                Debug.LogWarning($"TreeScaler: Invalid baseHeight ({baseHeight}). Skipping scale.");
                return;
            }

            float ratio = targetHeight / baseHeight;

            // Y軸のみスケール（元のlocalScale考慮）
            transform.localScale = new Vector3(
                originalScale.x,
                originalScale.y * ratio,
                originalScale.z
            );

            // テクスチャTiling調整
            if (adjustTextureTiling)
            {
                AdjustTextureTiling(ratio);
            }
        }

        /// <summary>
        /// テクスチャTiling調整（Y軸引き伸ばし補正）
        /// </summary>
        private void AdjustTextureTiling(float scaleRatio)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_MainTex"))
                    {
                        Vector2 tiling = mat.mainTextureScale;
                        mat.mainTextureScale = new Vector2(tiling.x, tiling.y * scaleRatio);
                    }
                }
            }
        }

        /// <summary>
        /// 元のスケールに戻す
        /// </summary>
        public void ResetScale()
        {
            transform.localScale = originalScale;

            if (adjustTextureTiling && originalMaterials != null)
            {
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.materials = originalMaterials;
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Apply Scale (Inspector)")]
        private void ApplyScaleInspector()
        {
            originalScale = transform.localScale;
            ApplyScale();
        }
#endif
    }
}
