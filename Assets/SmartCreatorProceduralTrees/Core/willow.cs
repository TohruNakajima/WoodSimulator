// Assets/SmartCreatorProceduralTrees/Core/WillowTreeGenerator.cs
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SmartCreator.ProceduralTrees
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WillowTreeGenerator : MonoBehaviour
    {
        [Header("Trunk")]
        public float trunkHeight = 6f;
        public float trunkRadius = 0.2f;
        [Range(3, 32)] public int trunkRadialSegments = 12;
        [Range(1, 20)] public int trunkHeightSegments = 8;

        [Header("Trunk Bend")]
        public Vector3 trunkCurveDirection = Vector3.forward;
        [Range(0f, 1f)] public float trunkCurveStrength = 0.2f;

        [Header("Main Branches")]
        [Range(6, 32)] public int branchCount = 16;
        [Range(2f, 8f)] public float branchLength = 5f;
        [Range(4, 16)] public int branchSegments = 10;
        [Range(0f, 1f)] public float branchInitialDroop = 0.4f;
        [Range(30f, 90f)] public float branchDroopAngle = 80f;
        public float branchRadius = 0.05f;
        public Material branchMaterial;

        [Header("Secondary Branches")]
        [Range(2, 6)] public int subBranchPerMain = 4;
        [Range(0.2f, 1f)] public float subBranchLenMin = 0.4f;
        [Range(0.2f, 1f)] public float subBranchLenMax = 0.7f;
        [Range(30f, 90f)] public float subBranchDroopAngle = 70f;
        public float subBranchRadius = 0.025f;

        [Header("Tertiary Tendrils")]
        [Range(1, 4)] public int tendrilsPerSub = 3;
        [Range(0.3f, 1f)] public float tendrilLenMin = 0.3f;
        [Range(0.3f, 1f)] public float tendrilLenMax = 0.6f;
        [Range(60f, 100f)] public float tendrilDroop = 90f;
        public float tendrilRadius = 0.012f;

        [Header("Leaves")]
        public GameObject leafPrefab;
        [Range(8, 40)] public int leavesPerTendril = 20;
        [Range(0.2f, 1.2f)] public float leafScaleMin = 0.4f;
        [Range(0.2f, 1.2f)] public float leafScaleMax = 1f;
        [Tooltip("Side offset of leaves from tendril center")]
        public float leafSideOffset = 0.02f;

        [Header("Materials")]
        public Material trunkMaterial;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
            EditorApplication.delayCall += () => { if (this) BuildWillow(); };
        }
#else
        private void OnValidate() => BuildWillow();
#endif

        [ContextMenu("Build Willow")]
        public void BuildWillow()
        {
            // Remove old children
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            BuildTrunk();
            BuildBranches();
        }

        private void BuildTrunk()
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();
            float H = trunkHeight;

            var mesh = new Mesh { name = "WillowTrunk" };
            mf.sharedMesh = mesh;

            int R = trunkRadialSegments, Y = trunkHeightSegments;
            Vector3[] verts = new Vector3[(R + 1) * (Y + 1)];
            Vector3[] norms = new Vector3[verts.Length];
            Vector2[] uv    = new Vector2[verts.Length];
            int[] tris      = new int[R * Y * 6];

            Vector3 curveDir = new Vector3(trunkCurveDirection.x, 0, trunkCurveDirection.z).normalized;
            int vi = 0, ti = 0;

            for (int y = 0; y <= Y; y++)
            {
                float ty = (float)y / Y;
                float yPos = ty * H;
                Vector3 offset = curveDir * Mathf.Sin(ty * Mathf.PI) * trunkCurveStrength;

                for (int x = 0; x <= R; x++)
                {
                    float tx = (float)x / R;
                    float ang = tx * Mathf.PI * 2f;
                    Vector3 dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                    verts[vi] = dir * trunkRadius + Vector3.up * yPos + offset;
                    norms[vi] = dir;
                    uv[vi++]  = new Vector2(tx, ty);
                }
            }

            for (int y = 0; y < Y; y++)
            {
                for (int x = 0; x < R; x++)
                {
                    int i0 = y * (R + 1) + x;
                    int i1 = i0 + (R + 1);

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
            mesh.uv        = uv;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            mr.sharedMaterial = trunkMaterial != null
                ? trunkMaterial
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }

        private void BuildBranches()
        {
            if (branchMaterial == null || leafPrefab == null) return;

            Vector3 top = transform.position + Vector3.up * trunkHeight;

            for (int b = 0; b < branchCount; b++)
            {
                float yaw = 360f * b / branchCount + Random.Range(-10f, 10f);
                Vector3 flatDir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                Vector3 dir0 = Vector3.Lerp(flatDir, Vector3.down, branchInitialDroop).normalized;

                var mainPts = new List<Vector3>();
                for (int i = 0; i <= branchSegments; i++)
                {
                    float t = i / (float)branchSegments;
                    Vector3 p = top + dir0 * (branchLength * t);
                    p -= Vector3.up * branchLength * (t * t) * (branchDroopAngle / 90f);
                    mainPts.Add(p);
                }

                var mainGO = new GameObject($"MainBranch_{b}");
                mainGO.transform.SetParent(transform, false);

                // main branch segments
                for (int i = 0; i < mainPts.Count - 1; i++)
                    CreateCylinderSegment(mainGO.transform, mainPts[i], mainPts[i + 1],
                        Mathf.Lerp(branchRadius, branchRadius * 0.6f, (float)i / mainPts.Count),
                        branchMaterial, $"MB_{b}_{i}");

                // secondary branches and tendrils + leaves…
                for (int s = 0; s < subBranchPerMain; s++)
                {
                    float t0 = Random.Range(0.2f, 0.8f);
                    int idx0 = Mathf.FloorToInt(t0 * (mainPts.Count - 1));
                    Vector3 baseP = mainPts[idx0];
                    Vector3 dir1 = Vector3.Lerp(flatDir, Vector3.down, subBranchDroopAngle / 90f).normalized;
                    float slen = branchLength * Random.Range(subBranchLenMin, subBranchLenMax);

                    var subPts = new List<Vector3>();
                    int segs = Mathf.Max(3, branchSegments / 2);
                    for (int i = 0; i <= segs; i++)
                    {
                        float u = i / (float)segs;
                        Vector3 p = baseP + dir1 * (slen * u);
                        p -= Vector3.up * slen * (u * u) * (subBranchDroopAngle / 90f);
                        subPts.Add(p);
                    }

                    var subGO = new GameObject($"SubBranch_{b}_{s}");
                    subGO.transform.SetParent(mainGO.transform, false);

                    for (int i = 0; i < subPts.Count - 1; i++)
                        CreateCylinderSegment(subGO.transform, subPts[i], subPts[i + 1],
                            Mathf.Lerp(subBranchRadius, subBranchRadius * 0.7f, (float)i / subPts.Count),
                            branchMaterial, $"SB_{b}_{s}_{i}");

                    // tendrils + leaves
                    for (int t = 0; t < tendrilsPerSub; t++)
                    {
                        float tt0 = Random.Range(0.2f, 0.9f);
                        int id0 = Mathf.FloorToInt(tt0 * (subPts.Count - 1));
                        Vector3 bP = subPts[id0];
                        Vector3 dir2 = Vector3.Lerp(flatDir, Vector3.down, tendrilDroop / 90f).normalized;
                        float len2 = slen * Random.Range(tendrilLenMin, tendrilLenMax);

                        var tendPts = new List<Vector3>();
                        int seg2 = Mathf.Max(3, segs / 2);
                        for (int i = 0; i <= seg2; i++)
                        {
                            float u = i / (float)seg2;
                            Vector3 p = bP + dir2 * (len2 * u);
                            p -= Vector3.up * len2 * (u * u) * (tendrilDroop / 90f);
                            tendPts.Add(p);
                        }

                        var tenGO = new GameObject($"Tendril_{b}_{s}_{t}");
                        tenGO.transform.SetParent(subGO.transform, false);

                        for (int i = 0; i < tendPts.Count - 1; i++)
                            CreateCylinderSegment(tenGO.transform, tendPts[i], tendPts[i + 1],
                                tendrilRadius, branchMaterial, $"T_{b}_{s}_{t}_{i}");

                        for (int L = 0; L < leavesPerTendril; L++)
                        {
                            float uL = (L + 1f) / (leavesPerTendril + 1f);
                            int idxL = Mathf.Clamp(Mathf.RoundToInt(uL * (tendPts.Count - 1)), 0, tendPts.Count - 1);
                            Vector3 lp = tendPts[idxL];
                            Vector3 ahead = tendPts[Mathf.Min(idxL + 1, tendPts.Count - 1)];
                            Vector3 behind = tendPts[Mathf.Max(idxL - 1, 0)];
                            Vector3 tangent = (ahead - behind).normalized;
                            Vector3 side = Vector3.Cross(tangent, Vector3.up).normalized;
                            Vector3 offset = side * ((L % 2 == 0) ? leafSideOffset : -leafSideOffset);

                            var leaf = Instantiate(leafPrefab, lp + offset,
                                Quaternion.LookRotation(-tangent, Vector3.up), tenGO.transform);
                            leaf.name = $"Leaf_{b}_{s}_{t}_{L}";
                            leaf.transform.Rotate(
                                Random.Range(-15f, 15f),
                                Random.Range(-10f, 10f),
                                Random.Range(-15f, 15f),
                                Space.Self);
                            leaf.transform.localScale = Vector3.one * Random.Range(leafScaleMin, leafScaleMax);
                        }
                    }
                }
            }
        }

        private void CreateCylinderSegment(Transform parent, Vector3 p0, Vector3 p1, float radius, Material mat, string name)
        {
            Vector3 mid = (p0 + p1) * 0.5f;
            Vector3 dir = (p1 - p0).normalized;
            float len = Vector3.Distance(p0, p1);

            var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seg.name = name;
            seg.transform.SetParent(parent, true);
            seg.transform.position = mid;
            seg.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            seg.transform.localScale = new Vector3(radius * 2f, len * 0.5f, radius * 2f);

            DestroyImmediate(seg.GetComponent<Collider>());
            seg.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

#if UNITY_EDITOR
        [ContextMenu("Bake As Prefab")]
        public void BakeAsPrefab()
        {
            var temp = Instantiate(gameObject);
            temp.name = gameObject.name + "_Baked";
            temp.GetComponent<WillowTreeGenerator>().BuildWillow();

            // Ensure root is at (0,0,0) and bottom is at base
            temp.transform.position = Vector3.zero;
            temp.transform.rotation = Quaternion.identity;
            temp.transform.localScale = Vector3.one;

            const string root = "Assets/SmartCreatorProceduralTrees";
            const string folder = root + "/MyTrees";
            if (!AssetDatabase.IsValidFolder(root))
                AssetDatabase.CreateFolder("Assets", "SmartCreatorProceduralTrees");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(root, "MyTrees");

            string safe = temp.name.Replace(" ", "_");
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safe}.prefab");
            PrefabUtility.SaveAsPrefabAssetAndConnect(temp, path, InteractionMode.UserAction);
            DestroyImmediate(temp);
        }
#endif
    }
}
