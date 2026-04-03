// Assets/SmartCreatorProceduralTrees/Core/PineTreeGenerator.cs

using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SmartCreator.ProceduralTrees
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PineTreeGenerator : MonoBehaviour
    {
        [Header("Pine Shape")]
        [Range(8, 32)] public int whorlCount = 16;
        [Range(1, 16)] public int branchesPerWhorl = 8;
        [Range(0.4f, 1.2f)] public float whorlSpacing = 0.75f;
        [Range(5, 25)] public float trunkHeight = 13f;
        [Range(0.05f, 0.5f)] public float trunkRadius = 0.22f;
        [Range(0.02f, 0.15f)] public float trunkTipRadius = 0.04f;
        [Range(0.8f, 2f)] public float trunkTaper = 1.33f;
        [Range(0f, 0.5f)] public float trunkNoiseStrength = 0.09f;
        [Range(0f, 12f)] public float trunkNoiseFrequency = 3.7f;

        [Header("Branch Settings")]
        [Range(1f, 7f)] public float baseBranchLength = 3.8f;
        [Range(0.1f, 4f)] public float tipBranchLength = 1.1f;
        [Range(15f, 75f)] public float branchDownwardAngle = 42f;
        [Range(0f, 20f)] public float branchRandomTilt = 8f;
        [Range(0.02f, 0.15f)] public float branchThickness = 0.07f;
        [Range(0f, 1f)] public float branchUpCurve = 0.22f;
        [Range(0f, 4f)] public float branchDownwardCurve = 1.2f;
        [Range(0f, 0.5f)] public float branchStartHeight = 0.09f;
        [Range(0.5f, 1f)] public float branchEndHeight = 0.95f;

        [Header("Leaves (Card)")]
        [Range(16, 64)] public int baseLeavesPerBranch = 36;
        [Range(0.2f, 2f)] public float leafCardLength = 0.72f;
        [Range(0.08f, 1f)] public float leafCardWidth = 0.23f;
        [Range(0f, 45f)] public float leafBend = 13f;
        public Material leafMaterial;
        public Material barkMaterial;

        [Header("LOD & General")]
        public int seed = 42;
        public bool autoRegenerate = true;
        public bool addLODGroup = false;

        [Header("Runtime Quality")]
        [Tooltip("ランタイム時のメッシュ品質（0.0〜1.0）。低いほど軽い")]
        [Range(0.1f, 1.0f)]
        public float runtimeQuality = 0.5f;

        // Delayed regeneration flag (for safe auto-regeneration)
        [System.NonSerialized] private bool pendingAutoGen = false;

        // キャッシュ: ランタイム時のMesh再利用
        private Mesh cachedTrunkMesh;
        private Mesh cachedBranchesMesh;
        private Mesh cachedLeavesMesh;
        private BranchPlacement[] cachedPlacements;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Never destroy objects directly in OnValidate!
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
            if (autoRegenerate && Application.isEditor && !Application.isPlaying)
            {
                if (!pendingAutoGen)
                {
                    pendingAutoGen = true;
                    EditorApplication.delayCall += () =>
                    {
                        if (this && !Application.isPlaying)
                        {
                            pendingAutoGen = false;
                            Generate();
                        }
                    };
                }
            }
        }
#else
        private void OnValidate()
        {
            // In build: only allow runtime regen if needed
            if (autoRegenerate && !Application.isPlaying)
                Generate();
        }
#endif

        private void Reset() => Generate();

        [ContextMenu("Regenerate")]
        public void Generate()
        {
#if UNITY_EDITOR
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
#endif
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isUpdating || UnityEditor.EditorApplication.isCompiling)
                    return;
#endif
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
#if UNITY_EDITOR
                    if (PrefabUtility.IsPartOfPrefabAsset(transform.GetChild(i).gameObject)) continue;
#endif
                    DestroyImmediate(transform.GetChild(i).gameObject);
                }
            }

            if (addLODGroup && GetComponent<LODGroup>() == null)
                gameObject.AddComponent<LODGroup>();

            if (Application.isPlaying)
            {
                // ランタイム: キャッシュ済みplacementsを使い、Meshオブジェクトを再利用
                cachedPlacements = GenerateBranchPlacements();

                cachedTrunkMesh = BuildTrunkMeshInto(cachedTrunkMesh);
                cachedBranchesMesh = BuildAllBranchesMeshInto(cachedBranchesMesh, cachedPlacements);
                cachedLeavesMesh = BuildAllLeavesMeshInto(cachedLeavesMesh, cachedPlacements);

                UpdateOrCreatePart("Trunk", cachedTrunkMesh, barkMaterial);
                UpdateOrCreatePart("Branches", cachedBranchesMesh, barkMaterial);
                UpdateOrCreatePart("Leaves", cachedLeavesMesh, leafMaterial);
            }
            else
            {
                var trunkMesh    = BuildTrunkMesh();
                var branchesMesh = BuildAllBranchesMesh(false);
                var leavesMesh   = BuildAllLeavesMesh(false);

                if (!IsMeshFinite(trunkMesh)) trunkMesh = DummySafeMesh("TrunkFallback");
                if (!IsMeshFinite(branchesMesh)) branchesMesh = DummySafeMesh("BranchesFallback");
                if (!IsMeshFinite(leavesMesh)) leavesMesh = DummySafeMesh("LeavesFallback");

                CreatePart("Trunk", trunkMesh, barkMaterial);
                CreatePart("Branches", branchesMesh, barkMaterial);
                CreatePart("Leaves", leavesMesh, leafMaterial);
            }
        }

        void UpdateOrCreatePart(string name, Mesh mesh, Material mat)
        {
            if (mesh == null || mesh.vertexCount < 3) return;
            var child = transform.Find(name);
            if (child != null)
            {
                child.GetComponent<MeshFilter>().sharedMesh = mesh;
                child.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
            else
            {
                CreatePart(name, mesh, mat);
            }
        }

        void CreatePart(string name, Mesh mesh, Material mat)
        {
            if (mesh == null || mesh.vertexCount < 3) return;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = mat;
        }

        // Utility Methods
        Vector3 SanitizeVec(Vector3 v)
        {
            if (!float.IsFinite(v.x)) v.x = 0;
            if (!float.IsFinite(v.y)) v.y = 0;
            if (!float.IsFinite(v.z)) v.z = 0;
            return v;
        }
        Quaternion SanitizeQuat(Quaternion q)
        {
            if (!float.IsFinite(q.x)) q.x = 0;
            if (!float.IsFinite(q.y)) q.y = 0;
            if (!float.IsFinite(q.z)) q.z = 0;
            if (!float.IsFinite(q.w) || Mathf.Abs(q.w) < 0.01f) q.w = 1;
            return q.normalized;
        }
        bool IsMeshFinite(Mesh mesh)
        {
            if (mesh == null || mesh.vertexCount == 0) return false;
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                if (!float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z))
                    return false;
            }
            return true;
        }
        Mesh DummySafeMesh(string name)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = new Vector3[3] { Vector3.zero, Vector3.right * 0.1f, Vector3.up * 0.1f };
            mesh.normals = new Vector3[3] { Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new int[3] { 0, 1, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        // --- ランタイム最適化ビルダー（Meshオブジェクト再利用 + 品質削減） ---

        Mesh BuildTrunkMeshInto(Mesh existing)
        {
            float q = runtimeQuality;
            int segs = Mathf.Max(6, Mathf.RoundToInt(whorlCount * 2 * q));
            int heightSegs = Mathf.Max(5, Mathf.RoundToInt(whorlCount * q));

            int vertCount = (heightSegs + 1) * (segs + 1) + 2;
            var verts = new Vector3[vertCount];
            var norms = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            int triCount = heightSegs * segs * 6 + segs * 3 * 2;
            var tris = new int[triCount];

            int vi = 0;
            for (int y = 0; y <= heightSegs; y++)
            {
                float t = y / (float)heightSegs;
                float rad = Mathf.Max(0.01f, Mathf.Lerp(trunkRadius, trunkTipRadius, Mathf.Pow(t, trunkTaper)));
                float h = t * trunkHeight;
                for (int i = 0; i <= segs; i++)
                {
                    float ang = 2 * Mathf.PI * i / segs;
                    float noise = 0f;
                    if (trunkNoiseStrength > 0f && trunkNoiseFrequency > 0f)
                    {
                        float nx = Mathf.Cos(ang) * trunkNoiseFrequency + t * trunkNoiseFrequency * 0.5f + seed * 0.17f;
                        float nz = Mathf.Sin(ang) * trunkNoiseFrequency + t * trunkNoiseFrequency * 0.37f - seed * 0.11f;
                        noise = Mathf.PerlinNoise(nx, nz) * trunkNoiseStrength;
                    }
                    float r = Mathf.Max(0.01f, rad + noise);
                    verts[vi] = new Vector3(Mathf.Cos(ang) * r, h, Mathf.Sin(ang) * r);
                    norms[vi] = (new Vector3(Mathf.Cos(ang), 0.6f, Mathf.Sin(ang))).normalized;
                    uvs[vi] = new Vector2(i / (float)segs, t);
                    vi++;
                }
            }

            int ring = segs + 1;
            int ti = 0;
            for (int y = 0; y < heightSegs; y++)
                for (int i = 0; i < segs; i++)
                {
                    int idx = y * ring + i;
                    tris[ti++] = idx; tris[ti++] = idx + ring; tris[ti++] = idx + ring + 1;
                    tris[ti++] = idx; tris[ti++] = idx + ring + 1; tris[ti++] = idx + 1;
                }

            // Tip
            verts[vi] = new Vector3(0, trunkHeight + trunkTipRadius * 1.1f, 0);
            norms[vi] = Vector3.up;
            uvs[vi] = new Vector2(0.5f, 1f);
            int tipVert = vi; vi++;
            int yTip = heightSegs * ring;
            for (int i = 0; i < segs; i++)
            {
                tris[ti++] = yTip + i; tris[ti++] = yTip + i + 1; tris[ti++] = tipVert;
            }

            // Base cap
            verts[vi] = Vector3.zero;
            norms[vi] = Vector3.down;
            uvs[vi] = new Vector2(0.5f, 0);
            int baseVert = vi;
            for (int i = 0; i < segs; i++)
            {
                tris[ti++] = i + 1; tris[ti++] = i; tris[ti++] = baseVert;
            }

            if (existing == null) existing = new Mesh();
            existing.Clear();
            existing.name = "Trunk";
            existing.vertices = verts;
            existing.normals = norms;
            existing.uv = uvs;
            existing.triangles = tris;
            existing.RecalculateBounds();
            return existing;
        }

        /// <summary>
        /// 全枝メッシュを直接バッファに書き込む（CombineMeshes廃止）。
        /// </summary>
        Mesh BuildAllBranchesMeshInto(Mesh existing, BranchPlacement[] placements)
        {
            if (existing == null) existing = new Mesh();
            existing.Clear();
            existing.name = "AllBranches";

            if (branchesPerWhorl < 1 || whorlCount < 2 || branchEndHeight <= branchStartHeight)
                return existing;

            float q = runtimeQuality;
            int sides = Mathf.Max(4, Mathf.RoundToInt(7 * q));
            int steps = Mathf.Max(3, Mathf.RoundToInt(6 * q));
            int branchStep = q < 0.6f ? 2 : 1;

            // 枝本数を事前計算してバッファサイズを確定
            int branchCount = 0;
            for (int i = 0; i < placements.Length; i += branchStep) branchCount++;
            int tipBranchCount = Mathf.Max(3, branchesPerWhorl);
            int totalBranches = branchCount + tipBranchCount;

            int vertsPerBranch = (steps + 1) * (sides + 1);
            int trisPerBranch = steps * sides * 6;
            var allVerts = new Vector3[totalBranches * vertsPerBranch];
            var allNorms = new Vector3[totalBranches * vertsPerBranch];
            var allUvs = new Vector2[totalBranches * vertsPerBranch];
            var allTris = new int[totalBranches * trisPerBranch];

            int globalVi = 0, globalTi = 0, ring = sides + 1;

            // 通常枝
            for (int i = 0; i < placements.Length; i += branchStep)
            {
                var bp = placements[i];
                float safeNorm = Mathf.Min(bp.heightNorm, 0.99f);
                float bLen = Mathf.Max(0.2f, Mathf.Lerp(baseBranchLength, tipBranchLength, safeNorm) * bp.lengthScale);
                float tRad = Mathf.Max(0.02f, Mathf.Lerp(trunkRadius * 1.4f, trunkTipRadius * 2.3f, safeNorm));
                float thisCurve = Mathf.Lerp(branchDownwardCurve, branchDownwardCurve * 0.28f, safeNorm);
                float thisThick = Mathf.Lerp(branchThickness, branchThickness * 0.5f, safeNorm);

                Quaternion rot = Quaternion.Euler(bp.pitchAngle, bp.yawAngle, bp.rollAngle);
                Vector3 pos = new Vector3(0, safeNorm * trunkHeight, 0) + rot * (Vector3.right * tRad);
                Matrix4x4 mat = Matrix4x4.TRS(pos, rot, Vector3.one);

                WriteBranchVerts(allVerts, allNorms, allUvs, allTris, ref globalVi, ref globalTi,
                    mat, bLen, thisCurve, thisThick, sides, steps, ring);
            }

            // 頂部枝
            Random.InitState(seed + 7777);
            float tipY2 = trunkHeight;
            float tipRad2 = trunkTipRadius * 2.5f;
            float tipLen2 = Mathf.Max(0.2f, tipBranchLength * 0.7f);
            for (int b = 0; b < tipBranchCount; b++)
            {
                float yaw = Random.Range(0f, 360f);
                float pitch = Random.Range(-15f, 25f);
                float roll = Random.Range(-5f, 5f);
                float lenScale = Random.Range(0.6f, 1.1f);

                Quaternion rot = Quaternion.Euler(pitch, yaw, roll);
                Vector3 pos = new Vector3(0, tipY2, 0) + rot * (Vector3.right * tipRad2);
                Matrix4x4 mat = Matrix4x4.TRS(pos, rot, Vector3.one);

                WriteBranchVerts(allVerts, allNorms, allUvs, allTris, ref globalVi, ref globalTi,
                    mat, tipLen2 * lenScale, branchDownwardCurve * 0.2f, branchThickness * 0.6f, sides, steps, ring);
            }

            existing.vertices = allVerts;
            existing.normals = allNorms;
            existing.uv = allUvs;
            existing.triangles = allTris;
            existing.RecalculateBounds();
            return existing;
        }

        /// <summary>
        /// 1本の枝の頂点・三角形を直接バッファに書き込む。
        /// </summary>
        void WriteBranchVerts(Vector3[] verts, Vector3[] norms, Vector2[] uvs, int[] tris,
            ref int vi, ref int ti, Matrix4x4 mat,
            float branchLen, float downCurve, float thickness, int sides, int steps, int ring)
        {
            float baseRad = Mathf.Max(0.01f, thickness * 0.98f);
            float tipRad = Mathf.Max(0.004f, thickness * 0.19f);
            float curveMult = downCurve * branchLen * 0.28f;
            float noiseMult = thickness * 0.31f;
            int baseVert = vi;

            for (int y = 0; y <= steps; y++)
            {
                float t = y / (float)steps;
                float len = t * branchLen;
                float rad = Mathf.Max(0.004f, Mathf.Lerp(baseRad, tipRad, t));
                float sinPiT = Mathf.Max(0f, Mathf.Sin(Mathf.PI * t));
                float curveY = -Mathf.Pow(sinPiT, 1.13f) * curveMult;
                float upCurve = branchUpCurve * sinPiT * branchLen * 0.11f;
                float noise = Mathf.PerlinNoise(t * 3.1f + seed * 0.11f, len * 0.5f + seed * 0.41f) * noiseMult * Mathf.Pow(Mathf.Max(0f, 1 - t), 2.2f);

                for (int i = 0; i <= sides; i++)
                {
                    float ang = 2 * Mathf.PI * i / sides;
                    float nx = Mathf.Sin(ang + len * 0.16f + seed) * noise;
                    float nz = Mathf.Cos(ang + len * 0.13f + seed) * noise;
                    Vector3 local = new Vector3(len, Mathf.Sin(ang) * rad + curveY + upCurve + nx, Mathf.Cos(ang) * rad + nz);
                    verts[vi] = mat.MultiplyPoint3x4(local);
                    norms[vi] = mat.MultiplyVector(Vector3.right);
                    uvs[vi] = new Vector2(i / (float)sides, t);
                    vi++;
                }
            }

            for (int y = 0; y < steps; y++)
                for (int i = 0; i < sides; i++)
                {
                    int idx = baseVert + y * ring + i;
                    tris[ti++] = idx; tris[ti++] = idx + ring; tris[ti++] = idx + ring + 1;
                    tris[ti++] = idx; tris[ti++] = idx + ring + 1; tris[ti++] = idx + 1;
                }
        }

        /// <summary>
        /// 全葉メッシュを直接バッファに書き込む（CombineMeshes廃止）。
        /// </summary>
        Mesh BuildAllLeavesMeshInto(Mesh existing, BranchPlacement[] placements)
        {
            if (existing == null) existing = new Mesh();
            existing.Clear();
            existing.name = "AllLeaves";

            float q = runtimeQuality;
            int leafStep = q < 0.6f ? 2 : 1;
            float leafSkipRate = q;

            float L = Mathf.Max(0.01f, leafCardLength);
            float W = Mathf.Max(0.01f, leafCardWidth);
            float halfW = W * 0.5f;

            // 葉カードの4頂点テンプレート
            Vector3 lv0 = new Vector3(0, -halfW, 0);
            Vector3 lv1 = new Vector3(L, -halfW, 0);
            Vector3 lv2 = new Vector3(L, halfW, 0);
            Vector3 lv3 = new Vector3(0, halfW, 0);

            // 総葉数を事前計算
            int totalLeaves = 0;
            Random.InitState(seed + 9999);
            for (int i = 0; i < placements.Length; i += leafStep)
            {
                float safeNorm = Mathf.Min(placements[i].heightNorm, 0.99f);
                int lpb = Mathf.RoundToInt(Mathf.Lerp(baseLeavesPerBranch * 1.2f, baseLeavesPerBranch * 0.45f, safeNorm) * leafSkipRate);
                if (lpb > 0) totalLeaves += lpb;
            }
            int tipBranchCount = Mathf.Max(3, branchesPerWhorl);
            int tipLeavesPerBranch = Mathf.RoundToInt(baseLeavesPerBranch * 0.5f * leafSkipRate);
            totalLeaves += tipBranchCount * tipLeavesPerBranch;

            if (totalLeaves == 0) return existing;

            var allVerts = new Vector3[totalLeaves * 4];
            var allNorms = new Vector3[totalLeaves * 4];
            var allUvs = new Vector2[totalLeaves * 4];
            var allTris = new int[totalLeaves * 6];

            int vi = 0, ti = 0;

            // 通常枝の葉
            Random.InitState(seed + 9999);
            for (int i = 0; i < placements.Length; i += leafStep)
            {
                var bp = placements[i];
                float safeNorm = Mathf.Min(bp.heightNorm, 0.99f);
                float bLen = Mathf.Max(0.2f, Mathf.Lerp(baseBranchLength, tipBranchLength, safeNorm) * bp.lengthScale);
                float tRad = Mathf.Max(0.02f, Mathf.Lerp(trunkRadius * 1.4f, trunkTipRadius * 2.3f, safeNorm));

                Quaternion rot = Quaternion.Euler(bp.pitchAngle, bp.yawAngle, bp.rollAngle);
                Vector3 pos = new Vector3(0, safeNorm * trunkHeight * 0.98f, 0) + rot * (Vector3.right * tRad);

                int lpb = Mathf.RoundToInt(Mathf.Lerp(baseLeavesPerBranch * 1.2f, baseLeavesPerBranch * 0.45f, safeNorm) * leafSkipRate);
                if (lpb <= 0) continue;

                Matrix4x4 branchMat = Matrix4x4.TRS(pos, rot, Vector3.one);
                for (int lf = 0; lf < lpb; lf++)
                {
                    float frac = lf / (float)lpb;
                    float lpos = bLen * (0.14f + 0.75f * frac);
                    float yOff = Random.Range(-0.04f, 0.04f) * bLen;
                    float rnd = Random.Range(-0.13f, 0.13f) * bLen;
                    float tilt = Random.Range(-leafBend, leafBend);
                    float leafRotY = Random.Range(-80f, 80f);

                    Matrix4x4 leafMat = branchMat * Matrix4x4.TRS(
                        new Vector3(lpos + rnd, yOff, 0), Quaternion.Euler(tilt, leafRotY, 0), Vector3.one);

                    WriteLeafQuad(allVerts, allNorms, allUvs, allTris, ref vi, ref ti,
                        leafMat, lv0, lv1, lv2, lv3);
                }
            }

            // 頂部の葉
            Random.InitState(seed + 7777);
            float tipY = trunkHeight;
            float tipRadius = trunkTipRadius * 2.5f;
            float tipLen = Mathf.Max(0.2f, tipBranchLength * 0.7f);
            for (int b = 0; b < tipBranchCount; b++)
            {
                float yaw = Random.Range(0f, 360f);
                float pitch = Random.Range(-15f, 25f);
                float roll = Random.Range(-5f, 5f);
                float lenScale = Random.Range(0.6f, 1.1f);

                Quaternion rot = Quaternion.Euler(pitch, yaw, roll);
                Vector3 pos = new Vector3(0, tipY, 0) + rot * (Vector3.right * tipRadius);
                float bLen = tipLen * lenScale;
                Matrix4x4 branchMat = Matrix4x4.TRS(pos, rot, Vector3.one);

                for (int lf = 0; lf < tipLeavesPerBranch; lf++)
                {
                    float frac = lf / (float)tipLeavesPerBranch;
                    float lpos = bLen * (0.14f + 0.75f * frac);
                    float yOff = Random.Range(-0.03f, 0.03f) * bLen;
                    float rnd = Random.Range(-0.1f, 0.1f) * bLen;
                    float tilt = Random.Range(-leafBend, leafBend);
                    float leafRotY = Random.Range(-80f, 80f);

                    Matrix4x4 leafMat = branchMat * Matrix4x4.TRS(
                        new Vector3(lpos + rnd, yOff, 0), Quaternion.Euler(tilt, leafRotY, 0), Vector3.one);

                    WriteLeafQuad(allVerts, allNorms, allUvs, allTris, ref vi, ref ti,
                        leafMat, lv0, lv1, lv2, lv3);
                }
            }

            existing.vertices = allVerts;
            existing.normals = allNorms;
            existing.uv = allUvs;
            existing.triangles = allTris;
            existing.RecalculateBounds();
            return existing;
        }

        /// <summary>
        /// 1枚の葉クワッド（4頂点6インデックス）を直接バッファに書き込む。
        /// </summary>
        void WriteLeafQuad(Vector3[] verts, Vector3[] norms, Vector2[] uvs, int[] tris,
            ref int vi, ref int ti, Matrix4x4 mat, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int baseIdx = vi;
            Vector3 n = mat.MultiplyVector(Vector3.up);
            verts[vi] = mat.MultiplyPoint3x4(v0); norms[vi] = n; uvs[vi] = new Vector2(0, 0); vi++;
            verts[vi] = mat.MultiplyPoint3x4(v1); norms[vi] = n; uvs[vi] = new Vector2(1, 0); vi++;
            verts[vi] = mat.MultiplyPoint3x4(v2); norms[vi] = n; uvs[vi] = new Vector2(1, 1); vi++;
            verts[vi] = mat.MultiplyPoint3x4(v3); norms[vi] = n; uvs[vi] = new Vector2(0, 1); vi++;

            tris[ti++] = baseIdx; tris[ti++] = baseIdx + 1; tris[ti++] = baseIdx + 2;
            tris[ti++] = baseIdx; tris[ti++] = baseIdx + 2; tris[ti++] = baseIdx + 3;
        }

        // --- エディタ用ビルダー（既存のまま） ---

        // Full Mesh Builders!
        Mesh BuildTrunkMesh()
        {
            int segs = Mathf.Max(10, whorlCount * 2);
            int heightSegs = Mathf.Max(7, whorlCount);

            List<Vector3> verts = new();
            List<Vector3> norms = new();
            List<int> tris = new();
            List<Vector2> uvs = new();

            for (int y = 0; y <= heightSegs; y++)
            {
                float t = y / (float)heightSegs;
                float rad = Mathf.Max(0.01f, Mathf.Lerp(trunkRadius, trunkTipRadius, Mathf.Pow(t, trunkTaper)));
                float h = t * trunkHeight;
                for (int i = 0; i <= segs; i++)
                {
                    float ang = 2 * Mathf.PI * i / segs;
                    float noise = 0f;
                    if (trunkNoiseStrength > 0f && trunkNoiseFrequency > 0f)
                    {
                        float nx = Mathf.Cos(ang) * trunkNoiseFrequency + t * trunkNoiseFrequency * 0.5f + seed * 0.17f;
                        float nz = Mathf.Sin(ang) * trunkNoiseFrequency + t * trunkNoiseFrequency * 0.37f - seed * 0.11f;
                        noise = Mathf.PerlinNoise(nx, nz) * trunkNoiseStrength;
                    }
                    float r = Mathf.Max(0.01f, rad + noise);
                    verts.Add(SanitizeVec(new Vector3(Mathf.Cos(ang) * r, h, Mathf.Sin(ang) * r)));
                    norms.Add((new Vector3(Mathf.Cos(ang), 0.6f, Mathf.Sin(ang))).normalized);
                    uvs.Add(new Vector2(i / (float)segs, t));
                }
            }
            int ring = segs + 1;
            for (int y = 0; y < heightSegs; y++)
                for (int i = 0; i < segs; i++)
                {
                    int idx = y * ring + i;
                    tris.Add(idx);
                    tris.Add(idx + ring);
                    tris.Add(idx + ring + 1);
                    tris.Add(idx);
                    tris.Add(idx + ring + 1);
                    tris.Add(idx + 1);
                }

            // Cone tip
            Vector3 tipPos = new Vector3(0, trunkHeight + trunkTipRadius * 1.1f, 0);
            verts.Add(tipPos);
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 1f));
            int tipVert = verts.Count - 1;
            int yTip = heightSegs * ring;
            for (int i = 0; i < segs; i++)
            {
                tris.Add(yTip + i);
                tris.Add(yTip + i + 1);
                tris.Add(tipVert);
            }
            // Cap base
            verts.Add(Vector3.zero);
            norms.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0));
            int baseVert = verts.Count - 1;
            for (int i = 0; i < segs; i++)
            {
                tris.Add(i + 1);
                tris.Add(i);
                tris.Add(baseVert);
            }

            Mesh mesh = new Mesh();
            mesh.name = "Trunk";
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Generate random branch placement data shared between branches and leaves
        struct BranchPlacement
        {
            public float heightNorm;  // 0..1 normalized height on trunk
            public float yawAngle;    // 0..360 random angle around trunk
            public float pitchAngle;  // downward angle
            public float rollAngle;   // slight roll
            public float lengthScale; // multiplier for branch length
        }

        BranchPlacement[] GenerateBranchPlacements()
        {
            int totalBranches = whorlCount * branchesPerWhorl;
            var placements = new BranchPlacement[totalBranches];
            Random.InitState(seed);

            for (int i = 0; i < totalBranches; i++)
            {
                float heightNorm = Mathf.Lerp(branchStartHeight, branchEndHeight, Random.Range(0f, 1f));
                float yaw = Random.Range(0f, 360f);
                float basePitch = Mathf.Lerp(branchDownwardAngle + 15f, branchDownwardAngle - 25f, heightNorm);
                float pitch = basePitch + Random.Range(-branchRandomTilt, branchRandomTilt);
                float roll = Random.Range(-branchUpCurve * 10f, branchUpCurve * 10f);
                float lenScale = Random.Range(0.7f, 1.3f);

                placements[i] = new BranchPlacement
                {
                    heightNorm = heightNorm,
                    yawAngle = yaw,
                    pitchAngle = pitch,
                    rollAngle = roll,
                    lengthScale = lenScale
                };
            }
            return placements;
        }

        Mesh BuildAllBranchesMesh(bool forBake)
        {
            if (branchesPerWhorl < 1 || whorlCount < 2 || branchEndHeight <= branchStartHeight)
                return DummySafeMesh("AllBranches");

            var placements = GenerateBranchPlacements();
            List<CombineInstance> branchMeshes = new List<CombineInstance>();
            int skipped = 0;

            for (int i = 0; i < placements.Length; i++)
            {
                var bp = placements[i];
                float safeNorm = Mathf.Min(bp.heightNorm, 0.99f);
                float y = safeNorm * trunkHeight;
                float branchLen = Mathf.Max(0.2f, Mathf.Lerp(baseBranchLength, tipBranchLength, safeNorm) * bp.lengthScale);
                float trunkRad = Mathf.Max(0.02f, Mathf.Lerp(trunkRadius * 1.4f, trunkTipRadius * 2.3f, safeNorm));

                Quaternion rot = SanitizeQuat(Quaternion.Euler(bp.pitchAngle, bp.yawAngle, bp.rollAngle));
                Vector3 pos = SanitizeVec(new Vector3(0, y, 0) + rot * (Vector3.right * trunkRad));
                float thisBranchDownwardCurve = Mathf.Lerp(branchDownwardCurve, branchDownwardCurve * 0.28f, safeNorm);
                float thisThickness = Mathf.Lerp(branchThickness, branchThickness * 0.5f, safeNorm);
                Mesh branchMesh = BuildProceduralBranchMesh(branchLen, thisBranchDownwardCurve, thisThickness);

                if (!IsMeshFinite(branchMesh))
                {
                    skipped++;
                    continue;
                }
                CombineInstance ciBranch = new CombineInstance();
                ciBranch.mesh = branchMesh;
                ciBranch.transform = Matrix4x4.TRS(pos, rot, Vector3.one);
                branchMeshes.Add(ciBranch);
            }

            // --- 頂部（crown）の枝を追加：上向きに生やす ---
            {
                float tipY = trunkHeight;
                float tipRadius = trunkTipRadius * 2.5f;
                int tipBranchCount = Mathf.Max(3, branchesPerWhorl);
                float tipLen = Mathf.Max(0.2f, tipBranchLength * 0.7f);

                Random.InitState(seed + 7777);
                for (int b = 0; b < tipBranchCount; b++)
                {
                    float yaw = Random.Range(0f, 360f);
                    // 小さいpitch角で上向き（0=真横, 負=上向き）
                    float pitch = Random.Range(-15f, 25f);
                    float roll = Random.Range(-5f, 5f);
                    float lenScale = Random.Range(0.6f, 1.1f);

                    Quaternion rot = SanitizeQuat(Quaternion.Euler(pitch, yaw, roll));
                    Vector3 pos = SanitizeVec(new Vector3(0, tipY, 0) + rot * (Vector3.right * tipRadius));
                    Mesh branchMesh = BuildProceduralBranchMesh(tipLen * lenScale, branchDownwardCurve * 0.2f, branchThickness * 0.6f);

                    if (!IsMeshFinite(branchMesh))
                    {
                        skipped++;
                        continue;
                    }
                    CombineInstance ciBranch = new CombineInstance();
                    ciBranch.mesh = branchMesh;
                    ciBranch.transform = Matrix4x4.TRS(pos, rot, Vector3.one);
                    branchMeshes.Add(ciBranch);
                }
            }

            if (branchMeshes.Count == 0)
                return DummySafeMesh("AllBranches");
            if (skipped > 0)
                Debug.LogWarning($"{skipped} branch meshes skipped for NaN.");

            Mesh allBranches = new Mesh();
            allBranches.name = "AllBranches";
            allBranches.CombineMeshes(branchMeshes.ToArray(), true, true, false);
            allBranches.RecalculateBounds();
            if (!IsMeshFinite(allBranches))
                return DummySafeMesh("AllBranchesFinal");
            return allBranches;
        }

        Mesh BuildAllLeavesMesh(bool forBake)
        {
            var placements = GenerateBranchPlacements();
            List<CombineInstance> leafInstances = new List<CombineInstance>();

            // Use a separate seed offset for leaf randomness
            Random.InitState(seed + 9999);

            for (int i = 0; i < placements.Length; i++)
            {
                var bp = placements[i];
                float safeNorm = Mathf.Min(bp.heightNorm, 0.99f);
                float y = safeNorm * trunkHeight * 0.98f;
                float branchLen = Mathf.Max(0.2f, Mathf.Lerp(baseBranchLength, tipBranchLength, safeNorm) * bp.lengthScale);
                float trunkRad = Mathf.Max(0.02f, Mathf.Lerp(trunkRadius * 1.4f, trunkTipRadius * 2.3f, safeNorm));

                Quaternion rot = SanitizeQuat(Quaternion.Euler(bp.pitchAngle, bp.yawAngle, bp.rollAngle));
                Vector3 pos = SanitizeVec(new Vector3(0, y, 0) + rot * (Vector3.right * trunkRad));

                int leavesPerBranch = Mathf.RoundToInt(Mathf.Lerp(baseLeavesPerBranch * 1.2f, baseLeavesPerBranch * 0.45f, safeNorm));
                if (leavesPerBranch <= 0) continue;

                for (int lf = 0; lf < leavesPerBranch; lf++)
                {
                    float frac = lf / (float)leavesPerBranch;
                    float lpos = branchLen * (0.14f + 0.75f * frac);
                    float yOffset = Random.Range(-0.04f, 0.04f) * branchLen;
                    float rand = Random.Range(-0.13f, 0.13f) * branchLen;
                    float tilt = Random.Range(-leafBend, leafBend);
                    float leafRotY = Random.Range(-80f, 80f);

                    Vector3 leafLocal = SanitizeVec(new Vector3(lpos + rand, yOffset, 0));
                    Quaternion leafRot = SanitizeQuat(Quaternion.Euler(tilt, leafRotY, 0));
                    Mesh leafMesh = BuildLeafCardMesh();

                    if (!IsMeshFinite(leafMesh)) continue;
                    CombineInstance ciLeaf = new CombineInstance();
                    ciLeaf.mesh = leafMesh;
                    ciLeaf.transform = Matrix4x4.TRS(pos, rot, Vector3.one) * Matrix4x4.TRS(leafLocal, leafRot, Vector3.one);
                    leafInstances.Add(ciLeaf);
                }
            }

            // --- 頂部（crown）の葉を追加 ---
            {
                float tipY = trunkHeight;
                float tipRadius = trunkTipRadius * 2.5f;
                int tipBranchCount = Mathf.Max(3, branchesPerWhorl);
                float tipLen = Mathf.Max(0.2f, tipBranchLength * 0.7f);
                int tipLeavesPerBranch = Mathf.RoundToInt(baseLeavesPerBranch * 0.5f);

                Random.InitState(seed + 7777);
                for (int b = 0; b < tipBranchCount; b++)
                {
                    float yaw = Random.Range(0f, 360f);
                    float pitch = Random.Range(-15f, 25f);
                    float roll = Random.Range(-5f, 5f);
                    float lenScale = Random.Range(0.6f, 1.1f);

                    Quaternion rot = SanitizeQuat(Quaternion.Euler(pitch, yaw, roll));
                    Vector3 pos = SanitizeVec(new Vector3(0, tipY, 0) + rot * (Vector3.right * tipRadius));
                    float branchLen = tipLen * lenScale;

                    for (int lf = 0; lf < tipLeavesPerBranch; lf++)
                    {
                        float frac = lf / (float)tipLeavesPerBranch;
                        float lpos = branchLen * (0.14f + 0.75f * frac);
                        float yOffset = Random.Range(-0.03f, 0.03f) * branchLen;
                        float rand = Random.Range(-0.1f, 0.1f) * branchLen;
                        float tilt = Random.Range(-leafBend, leafBend);
                        float leafRotY = Random.Range(-80f, 80f);

                        Vector3 leafLocal = SanitizeVec(new Vector3(lpos + rand, yOffset, 0));
                        Quaternion leafRot = SanitizeQuat(Quaternion.Euler(tilt, leafRotY, 0));
                        Mesh leafMesh = BuildLeafCardMesh();

                        if (!IsMeshFinite(leafMesh)) continue;
                        CombineInstance ciLeaf = new CombineInstance();
                        ciLeaf.mesh = leafMesh;
                        ciLeaf.transform = Matrix4x4.TRS(pos, rot, Vector3.one) * Matrix4x4.TRS(leafLocal, leafRot, Vector3.one);
                        leafInstances.Add(ciLeaf);
                    }
                }
            }

            if (leafInstances.Count == 0)
                return DummySafeMesh("AllLeaves");
            Mesh allLeaves = new Mesh();
            allLeaves.name = "AllLeaves";
            allLeaves.CombineMeshes(leafInstances.ToArray(), true, true, false);
            allLeaves.RecalculateBounds();
            if (!IsMeshFinite(allLeaves))
                return DummySafeMesh("AllLeavesFinal");
            return allLeaves;
        }

        Mesh BuildProceduralBranchMesh(float branchLen, float downwardCurveAmount, float thickness)
        {
            int sides = 7, steps = 6;
            float baseRad = Mathf.Max(0.01f, thickness * 0.98f);
            float tipRad = Mathf.Max(0.004f, thickness * 0.19f);
            branchLen = Mathf.Max(0.2f, branchLen);

            List<Vector3> verts = new();
            List<Vector3> norms = new();
            List<int> tris = new();
            List<Vector2> uvs = new();

            float curveMult = downwardCurveAmount * branchLen * 0.28f;
            float noiseMult = thickness * 0.31f;

            for (int y = 0; y <= steps; y++)
            {
                float t = y / (float)steps;
                float len = t * branchLen;
                float rad = Mathf.Max(0.004f, Mathf.Lerp(baseRad, tipRad, t));
                float sinPiT = Mathf.Max(0f, Mathf.Sin(Mathf.PI * t));
                float curveY = -Mathf.Pow(sinPiT, 1.13f) * curveMult;
                float upCurve = branchUpCurve * sinPiT * branchLen * 0.11f;
                float noise = Mathf.PerlinNoise(t * 3.1f + seed * 0.11f, len * 0.5f + seed * 0.41f) * noiseMult * Mathf.Pow(Mathf.Max(0f, 1 - t), 2.2f);

                for (int i = 0; i <= sides; i++)
                {
                    float ang = 2 * Mathf.PI * i / sides;
                    float nx = Mathf.Sin(ang + len * 0.16f + seed) * noise;
                    float nz = Mathf.Cos(ang + len * 0.13f + seed) * noise;

                    Vector3 p = SanitizeVec(new Vector3(
                        len,
                        Mathf.Sin(ang) * rad + curveY + upCurve + nx,
                        Mathf.Cos(ang) * rad + nz
                    ));
                    verts.Add(p);
                    norms.Add(Vector3.right);
                    uvs.Add(new Vector2(i / (float)sides, t));
                }
            }
            int ring = sides + 1;
            for (int y = 0; y < steps; y++)
                for (int i = 0; i < sides; i++)
                {
                    int idx = y * ring + i;
                    tris.Add(idx);
                    tris.Add(idx + ring);
                    tris.Add(idx + ring + 1);
                    tris.Add(idx);
                    tris.Add(idx + ring + 1);
                    tris.Add(idx + 1);
                }
            if (verts.Count < 3)
                return null;
            Mesh mesh = new Mesh();
            mesh.name = "Branch";
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateBounds();
            return mesh;
        }

        Mesh BuildLeafCardMesh()
        {
            float L = Mathf.Max(0.01f, leafCardLength * Random.Range(0.97f, 1.03f));
            float W = Mathf.Max(0.01f, leafCardWidth * Random.Range(0.97f, 1.03f));
            Vector3[] v = new Vector3[4] {
                new Vector3(0, -W*0.5f, 0),
                new Vector3(L, -W*0.5f, 0),
                new Vector3(L,  W*0.5f, 0),
                new Vector3(0,  W*0.5f, 0)
            };
            int[] t = new int[6] {0,1,2, 0,2,3};
            Vector2[] uv = new Vector2[4] {
                new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1)
            };
            Vector3[] n = new Vector3[4] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            Mesh mesh = new Mesh();
            mesh.name = "LeafCard";
            mesh.vertices = v;
            mesh.triangles = t;
            mesh.uv = uv;
            mesh.normals = n;
            mesh.RecalculateBounds();
            return mesh;
        }

#if UNITY_EDITOR
        [ContextMenu("Bake As Prefab")]
        public void BakeAsPrefab()
        {
            string baseFolder = "Assets/SmartCreatorProceduralTrees/MyTrees";
            if (!AssetDatabase.IsValidFolder(baseFolder))
            {
                string[] split = baseFolder.Split('/');
                string parent = split[0];
                for (int i = 1; i < split.Length; i++)
                {
                    string check = string.Join("/", split, 0, i + 1);
                    if (!AssetDatabase.IsValidFolder(check))
                        AssetDatabase.CreateFolder(parent, split[i]);
                    parent = check;
                }
            }

            Mesh trunkMesh    = BuildTrunkMesh();
            Mesh branchesMesh = BuildAllBranchesMesh(true);
            Mesh leavesMesh   = BuildAllLeavesMesh(true);

            var combine = new List<CombineInstance>();
            var mats    = new List<Material>();

            combine.Add(new CombineInstance { mesh = trunkMesh, transform = Matrix4x4.identity });
            combine.Add(new CombineInstance { mesh = branchesMesh, transform = Matrix4x4.identity });
            combine.Add(new CombineInstance { mesh = leavesMesh, transform = Matrix4x4.identity });
            mats.Add(barkMaterial);
            mats.Add(barkMaterial);
            mats.Add(leafMaterial);

            var combined = new Mesh { name = gameObject.name + "_Combined" };
            combined.CombineMeshes(combine.ToArray(), false, false, false);
            combined.RecalculateBounds();
            if (!IsMeshFinite(combined))
                combined = DummySafeMesh("BakedFallback");

            var prefabGO = new GameObject(gameObject.name + "_Baked");
            var mf = prefabGO.AddComponent<MeshFilter>();
            var mr = prefabGO.AddComponent<MeshRenderer>();
            mf.sharedMesh = combined;
            mr.sharedMaterials = mats.ToArray();

            string meshPath   = baseFolder + "/" + prefabGO.name + ".asset";
            string prefabPath = baseFolder + "/" + prefabGO.name + ".prefab";
            AssetDatabase.CreateAsset(combined, meshPath);
            PrefabUtility.SaveAsPrefabAssetAndConnect(prefabGO, prefabPath, InteractionMode.UserAction);

            DestroyImmediate(prefabGO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tree baked as prefab: " + prefabPath);
        }
#endif
    }
}

