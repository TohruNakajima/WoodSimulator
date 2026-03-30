using UnityEngine;
using WoodSimulator;

public class Tree : MonoBehaviour
{
    [SerializeField]
    GrowthData data;

    public void initialize(GrowthData data)
    {
        this.data = data;
    }
    /// <summary>
    /// 6メッシュから適切なモデルをアクティブ化
    /// </summary>
     [ContextMenu("手動で木の成長表示更新")]
    private void SetActiveModel()
    {
        string modelName=this.data.modelName;
        Debug.Log(modelName);
        // 全メッシュを無効化
        foreach (Transform child in this.transform)
        {
            child.gameObject.SetActive(false);
        }

        // 指定モデルのみアクティブ化
        Transform targetMesh = this.transform.Find(modelName);
        if (targetMesh != null)
        {
            targetMesh.gameObject.SetActive(true);
        }
        else
        {
            //Debug.LogWarning($"ForestGenerator: Model '{modelName}' not found in tree prefab. Available: {GetChildNames(tree)}");
            // フォールバック: 最初の子をアクティブ化
            if (this.transform.childCount > 0)
            {
                this.transform.GetChild(0).gameObject.SetActive(true);
            }
        }
    }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
