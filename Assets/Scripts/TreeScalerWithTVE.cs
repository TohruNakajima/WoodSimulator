using UnityEngine;

public class TreeScalerWithTVE : MonoBehaviour
{
    public float targetHeight = 10.0f;
    public float targetDiameter = 0.3f;
    public float newHeight = 10.0f;
    public float newDiameter = 0.3f;

    private Renderer treeRenderer;
    private Vector3 originalScale;
    private float lastHeight = -1f;
    private float lastDiameter = -1f;

    void Start()
    {
        treeRenderer = GetComponent<Renderer>();
        originalScale = transform.localScale;
    }

    public void ApplyScale(float height, float diameter)
    {
        // 元のスケールを基準に計算
        float heightRatio = height / targetHeight;
        float diameterRatio = diameter / targetDiameter;
        /*
        transform.localScale = new Vector3(
            originalScale.x * diameterRatio,
            originalScale.y * heightRatio,
            originalScale.z * diameterRatio
        );
        */

        // TVEシェーダープロパティ変更
        if (treeRenderer != null && treeRenderer.sharedMaterial != null)
        {
            Debug.Log("TVEシェーダープロパティ変更");
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            treeRenderer.GetPropertyBlock(props);

            // 高さマスク乗算
            props.SetFloat("_VertexAMultiplier", heightRatio);

            // テクスチャTiling調整
            props.SetVector("_MainTex_ST", new Vector4(1, heightRatio, 0, 0));

            treeRenderer.SetPropertyBlock(props);
        }
    }

    void Update()
    {
        // 値が変更された時のみ更新（毎フレーム計算を避ける）
        if (!Mathf.Approximately(newHeight, lastHeight) || !Mathf.Approximately(newDiameter, lastDiameter))
        {
            ApplyScale(newHeight, newDiameter);
            lastHeight = newHeight;
            lastDiameter = newDiameter;
        }
    }
}
