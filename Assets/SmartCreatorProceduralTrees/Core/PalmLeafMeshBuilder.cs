using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SmartCreator.ProceduralTrees
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PalmLeafDoubleSpitzMeshBuilder : MonoBehaviour
    {
        [Header("Leaf Shape")]
        public float length = 2f;
        public float maxWidth = 0.3f;
        [Range(0, 1)] public float endWidthFactor = 0.05f;
        [Range(0, 1)] public float curveAmount = 0.5f;
        [Range(2, 64)] public int segments = 12;

    #if UNITY_EDITOR
        private static bool _busy;
        private void OnValidate()
        {
            if (!Application.isPlaying) GenerateLeaf();
        }
    #endif

        private void OnEnable()
        {
            if (!Application.isPlaying) GenerateLeaf();
        }

        public void GenerateLeaf()
        {
    #if UNITY_EDITOR
            if (_busy) return;
            if (AssetDatabase.IsAssetImportWorkerProcess()) return;
            _busy = true;
    #endif
            try
            {
                Mesh mesh = BuildMesh();
    #if UNITY_EDITOR
                if (!Application.isPlaying)
                    mesh = SaveOrUpdateAsset(mesh);
    #endif
                GetComponent<MeshFilter>().sharedMesh = mesh;
            }
            finally
            {
    #if UNITY_EDITOR
                _busy = false;
    #endif
            }
        }

        private Mesh BuildMesh()
        {
            Mesh m = new Mesh { name = "PalmLeafDoubleSpitz" };

            int vCount = (segments + 1) * 2;
            var v = new Vector3[vCount];
            var uv = new Vector2[vCount];
            var tr = new int[segments * 6];

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;

                float width = Mathf.Lerp(maxWidth * endWidthFactor, maxWidth, Mathf.Sin(t * Mathf.PI));
                float a = Mathf.Lerp(0f, Mathf.PI * curveAmount, t);
                float y = Mathf.Sin(a) * length;
                float z = Mathf.Cos(a) * length;

                Vector3 center = new Vector3(0, y * 0.5f, z * 0.5f);
                int vi = i * 2;

                v[vi] = center + new Vector3(-width * 0.5f, 0, 0);
                v[vi + 1] = center + new Vector3(width * 0.5f, 0, 0);

                uv[vi] = new Vector2(0, t);
                uv[vi + 1] = new Vector2(1, t);
            }

            for (int i = 0; i < segments; i++)
            {
                int vi = i * 2;
                int ti = i * 6;
                tr[ti] = vi;
                tr[ti + 1] = vi + 2;
                tr[ti + 2] = vi + 1;
                tr[ti + 3] = vi + 1;
                tr[ti + 4] = vi + 2;
                tr[ti + 5] = vi + 3;
            }

            m.vertices = v;
            m.uv = uv;
            m.triangles = tr;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

    #if UNITY_EDITOR
        private Mesh SaveOrUpdateAsset(Mesh src)
        {
            // Try to find a persistent asset path to save mesh under this GameObject's folder
            string ownerPath = AssetDatabase.GetAssetPath(gameObject);
            if (string.IsNullOrEmpty(ownerPath))
                ownerPath = "Assets/SmartCreatorProceduralTrees/Quads/Quads.asset"; // fallback

            string baseFolder = Path.GetDirectoryName(ownerPath)?.Replace("\\", "/") ?? "Assets/SmartCreatorProceduralTrees/Quads";
            string folder = baseFolder + "/GeneratedMeshes";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parentFolder = baseFolder;
                string folderName = "GeneratedMeshes";
                if (!AssetDatabase.IsValidFolder(baseFolder))
                {
                    string[] split = baseFolder.Split('/');
                    parentFolder = split[0];
                    for (int i = 1; i < split.Length; i++)
                    {
                        string check = string.Join("/", split, 0, i + 1);
                        if (!AssetDatabase.IsValidFolder(check))
                            AssetDatabase.CreateFolder(parentFolder, split[i]);
                        parentFolder = check;
                    }
                }
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }

            string assetPath = folder + $"/{gameObject.name}_Leaf.asset";

            Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (asset != null)
            {
                if (asset.vertexCount == src.vertexCount)
                    return asset; // Already up to date

                EditorUtility.CopySerialized(src, asset);
                EditorUtility.SetDirty(asset);
            }
            else
            {
                asset = Object.Instantiate(src);
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            AssetDatabase.SaveAssets();
            return asset;
        }
    #endif
    }
}
