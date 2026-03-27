using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class MeshInflateController : MonoBehaviour
{
    [SerializeField]
    [Range(0f, 0.1f)]
    [Tooltip("法線方向への押し出し量")]
    private float inflateAmount = 0f;

    private Material sharedMaterial;
    private Renderer targetRenderer;
    private static readonly int InflateAmountProperty = Shader.PropertyToID("_InflateAmount");

    private void OnEnable()
    {
        InitializeMaterial();
    }

    private void Awake()
    {
        InitializeMaterial();
    }

    private void InitializeMaterial()
    {
        targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            sharedMaterial = targetRenderer.sharedMaterial;

            if (sharedMaterial != null && sharedMaterial.HasProperty(InflateAmountProperty))
            {
                UpdateInflateAmount();
            }
            else if (sharedMaterial != null)
            {
                Debug.LogWarning($"Material on {gameObject.name} does not have _InflateAmount property. Please use StandardInflate shader.");
            }
        }
    }

    private void OnValidate()
    {
        InitializeMaterial();
        UpdateInflateAmount();
    }

    private void UpdateInflateAmount()
    {
        if (sharedMaterial != null && sharedMaterial.HasProperty(InflateAmountProperty))
        {
            sharedMaterial.SetFloat(InflateAmountProperty, inflateAmount);
        }
    }

    /// <summary>
    /// スクリプトから押し出し量を設定
    /// </summary>
    /// <param name="amount">押し出し量（0～0.1）</param>
    public void SetInflateAmount(float amount)
    {
        inflateAmount = Mathf.Clamp(amount, 0f, 0.1f);
        UpdateInflateAmount();
    }

    /// <summary>
    /// 現在の押し出し量を取得
    /// </summary>
    public float GetInflateAmount()
    {
        return inflateAmount;
    }
}
