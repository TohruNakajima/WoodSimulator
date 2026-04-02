using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SmartCreator.ProceduralTrees
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PalmTreeGenerator : MonoBehaviour
    {
        [Header("Trunk")]
        public float trunkHeight = 5f;
        public float trunkRadius = 0.25f;
        [Range(3, 32)] public int trunkSegments = 16;
        [Range(1, 20)] public int trunkHeightSegments = 8;

        [Header("Curve")]
        [Tooltip("Direction (in local XZ) along which the trunk will bend")]
        public Vector3 trunkCurveDirection = Vector3.right;
        [Tooltip("Strength (max offset) of the trunk bend")]
        public float trunkCurveStrength = 0f;

        [Header("Carve Settings")]
        [Tooltip("How much of the top of the trunk mesh to remove")]
        public float extraTrunkCarve = 0f;
        [Tooltip("Vertical offset for the fronds above the carved top")]
        public float palmLeafYOffset = 0f;

        [Header("Materials")]
        public Material trunkMaterial;

        [Header("Fronds")]
        public GameObject frondPrefab;
        [Range(4, 32)] public int frondCount = 12;
        public float frondLength = 3f;
        public float frondDroopAngle = 45f;

        [Header("Coconuts")]
        public GameObject coconutPrefab;
        [Range(0, 10)] public int coconutCount = 5;
        public float coconutRadiusOffset = 0.5f;
        public float coconutScale = 0.3f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Only generate if not a prefab asset and in Editor
            if (PrefabUtility.IsPartOfPrefabAsset(this)) return;
            EditorApplication.delayCall += () => { if (this && !PrefabUtility.IsPartOfPrefabAsset(this)) BuildPalm(); };
        }
#else
        private void OnValidate() { if (Application.isPlaying) BuildPalm(); }
#endif

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!PrefabUtility.IsPartOfPrefabAsset(this))
#endif
                BuildPalm();
        }

        [ContextMenu("Build Palm")]
        public void BuildPalm()
        {
            // Clean all children (fronds/coconuts)
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            BuildTrunk();
            BuildFronds();
            BuildCoconuts();
        }

        private void BuildTrunk()
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();

            float meshHeight = Mathf.Max(0f, trunkHeight - extraTrunkCarve);
            var mesh = new Mesh();
            mesh.name = "PalmTrunk";

            int radialSegs = trunkSegments;
            int heightSegs = trunkHeightSegments;
            int vertCount = (radialSegs + 1) * (heightSegs + 1);

            var verts = new Vector3[vertCount];
            var norms = new Vector3[vertCount];
            var uvs   = new Vector2[vertCount];
            var tris  = new int[radialSegs * heightSegs * 6];

            Vector3 curveDir = new Vector3(trunkCurveDirection.x, 0, trunkCurveDirection.z).normalized;

            int vi = 0;
            for (int y = 0; y <= heightSegs; y++)
            {
                float ty   = (float)y / heightSegs;
                float yPos = Mathf.Lerp(0, meshHeight, ty);
                Vector3 offset = curveDir * Mathf.Sin(ty * Mathf.PI) * trunkCurveStrength;

                for (int x = 0; x <= radialSegs; x++)
                {
                    float tx  = (float)x / radialSegs;
                    float ang = tx * Mathf.PI * 2f;
                    Vector3 dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                    verts[vi] = dir * trunkRadius + Vector3.up * yPos + offset;
                    norms[vi] = dir;
                    uvs[vi]   = new Vector2(tx, ty);
                    vi++;
                }
            }

            int ti = 0;
            for (int y = 0; y < heightSegs; y++)
            {
                for (int x = 0; x < radialSegs; x++)
                {
                    int i0 = y * (radialSegs + 1) + x;
                    int i1 = (y + 1) * (radialSegs + 1) + x;
                    tris[ti++] = i0;
                    tris[ti++] = i1;
                    tris[ti++] = i1 + 1;
                    tris[ti++] = i0;
                    tris[ti++] = i1 + 1;
                    tris[ti++] = i0 + 1;
                }
            }

            mesh.vertices  = verts;
            mesh.normals   = norms;
            mesh.uv        = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            mr.sharedMaterial = trunkMaterial != null
                ? trunkMaterial
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }

        private void BuildFronds()
        {
            if (frondPrefab == null) return;
            float y0 = Mathf.Max(0f, trunkHeight - extraTrunkCarve) + palmLeafYOffset;

            for (int i = 0; i < frondCount; i++)
            {
                float angle = 360f * i / frondCount;
                Quaternion yaw  = Quaternion.AngleAxis(angle, Vector3.up);
                Quaternion drop = Quaternion.AngleAxis(frondDroopAngle, Vector3.right);
                Vector3 dir     = yaw * drop * Vector3.forward;

                var go = Instantiate(frondPrefab, transform);
                go.name                    = $"Frond_{i}";
                go.transform.localPosition = Vector3.up * y0;
                go.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);
                go.transform.localScale    = new Vector3(1, 1, frondLength);
            }
        }

        private void BuildCoconuts()
        {
            if (coconutPrefab == null || coconutCount == 0) return;
            float carvedTop = Mathf.Max(0f, trunkHeight - extraTrunkCarve);
            float leafBase  = carvedTop + palmLeafYOffset;

            for (int i = 0; i < coconutCount; i++)
            {
                float t    = Random.value;
                float y    = Mathf.Lerp(carvedTop, leafBase, t);
                float aRad = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(aRad), 0, Mathf.Sin(aRad)) * coconutRadiusOffset;

                var go = Instantiate(coconutPrefab, transform);
                go.name                    = $"Coconut_{i}";
                go.transform.localPosition = offset + Vector3.up * y;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale    = Vector3.one * coconutScale;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Save Palm Prefab")]
        public void SavePalmPrefab()
        {
            var temp = Instantiate(gameObject);
            temp.name = name + "_Prefab";
            temp.GetComponent<PalmTreeGenerator>().BuildPalm();

            const string R = "Assets/SmartCreatorProceduralTrees";
            const string F = R + "/MyTrees";
            if (!AssetDatabase.IsValidFolder(R)) AssetDatabase.CreateFolder("Assets", "SmartCreatorProceduralTrees");
            if (!AssetDatabase.IsValidFolder(F)) AssetDatabase.CreateFolder(R, "MyTrees");

            string path = AssetDatabase.GenerateUniqueAssetPath($"{F}/{name}.prefab");
            PrefabUtility.SaveAsPrefabAssetAndConnect(temp, path, InteractionMode.UserAction);
            DestroyImmediate(temp);
        }

        [ContextMenu("Bake For Terrain")]
        public void BakeForTerrain()
        {
            var tmp = Instantiate(gameObject, transform.position, transform.rotation);
            tmp.name = name + "_Terrain";
            tmp.GetComponent<PalmTreeGenerator>().BuildPalm();

            var combines = new List<CombineInstance>();
            foreach (var f in tmp.GetComponentsInChildren<MeshFilter>())
                if (f.sharedMesh != null)
                    combines.Add(new CombineInstance { mesh = f.sharedMesh, transform = f.transform.localToWorldMatrix });

            var merged = new Mesh { name = tmp.name + "_Merged" };
            merged.CombineMeshes(combines.ToArray(), true, true);
            merged.RecalculateBounds();
            tmp.GetComponent<MeshFilter>().sharedMesh = merged;

            var rootMR = tmp.GetComponent<MeshRenderer>();
            var mats   = new List<Material> { trunkMaterial ?? rootMR.sharedMaterial };
            foreach (Transform c in tmp.transform)
                if (c.TryGetComponent<MeshRenderer>(out var mr) && mr.sharedMaterial != null)
                    mats.Add(mr.sharedMaterial);
            rootMR.sharedMaterials = mats.ToArray();

            for (int i = tmp.transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(tmp.transform.GetChild(i).gameObject);

            const string R2 = "Assets/SmartCreatorProceduralTrees/MyTrees";
            if (!AssetDatabase.IsValidFolder("Assets/SmartCreatorProceduralTrees"))
                AssetDatabase.CreateFolder("Assets", "SmartCreatorProceduralTrees");
            if (!AssetDatabase.IsValidFolder(R2))
                AssetDatabase.CreateFolder("Assets/SmartCreatorProceduralTrees", "MyTrees");

            string meshPath  = AssetDatabase.GenerateUniqueAssetPath($"{R2}/{tmp.name}_Mesh.asset");
            AssetDatabase.CreateAsset(merged, meshPath);
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{R2}/{tmp.name}.prefab");
            PrefabUtility.SaveAsPrefabAssetAndConnect(tmp, prefabPath, InteractionMode.UserAction);
            DestroyImmediate(tmp);
        }
#endif
    }
}
