using System.Collections.Generic;
using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// 樹木オブジェクトプール（GC最適化）
    /// </summary>
    public class TreePool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [Tooltip("プールする樹木Prefab")]
        public GameObject treePrefab;

        [Tooltip("初期プールサイズ")]
        public int initialPoolSize = 3000;

        [Tooltip("自動拡張を有効化")]
        public bool autoExpand = true;

        private Queue<GameObject> pool = new Queue<GameObject>();
        private List<GameObject> activeObjects = new List<GameObject>();

        /// <summary>
        /// プール初期化
        /// </summary>
        public void Initialize()
        {
            if (treePrefab == null)
            {
                Debug.LogError("TreePool: treePrefab is not assigned.");
                return;
            }

            // 初期オブジェクト生成
            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject obj = Instantiate(treePrefab, transform);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }

            Debug.Log($"TreePool: Initialized with {initialPoolSize} objects.");
        }

        /// <summary>
        /// プールからオブジェクト取得
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else if (autoExpand)
            {
                Debug.LogWarning("TreePool: Pool exhausted, creating new object.");
                obj = Instantiate(treePrefab, transform);
            }
            else
            {
                Debug.LogError("TreePool: Pool exhausted and autoExpand is disabled.");
                return null;
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            activeObjects.Add(obj);

            return obj;
        }

        /// <summary>
        /// オブジェクトをプールに返却
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null)
                return;

            obj.SetActive(false);
            obj.transform.SetParent(transform);
            activeObjects.Remove(obj);
            pool.Enqueue(obj);
        }

        /// <summary>
        /// 全アクティブオブジェクトを返却
        /// </summary>
        public void ReturnAll()
        {
            // activeObjectsをコピーしてから返却（Returnで要素削除されるため）
            var objects = new List<GameObject>(activeObjects);
            foreach (var obj in objects)
            {
                Return(obj);
            }

            Debug.Log($"TreePool: Returned all {objects.Count} active objects.");
        }

        /// <summary>
        /// プールクリア
        /// </summary>
        public void Clear()
        {
            ReturnAll();

            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            pool.Clear();
            activeObjects.Clear();

            Debug.Log("TreePool: Cleared.");
        }

        /// <summary>
        /// プール状態取得
        /// </summary>
        public (int pooled, int active, int total) GetStatus()
        {
            return (pool.Count, activeObjects.Count, pool.Count + activeObjects.Count);
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
