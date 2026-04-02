// Assets/SmartCreatorProceduralTrees/Core/TreeGeneratorOptimized.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Random = UnityEngine.Random;

namespace SmartCreator.ProceduralTrees
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TreeGeneratorOptimized : MonoBehaviour
    {
        [Header("Tree Data")] public TreeProfile profile;
        [Header("Materials")]
        public Material barkMaterial;
        public Material branchMaterial;
        public Material leafMaterial;
        public Material fruitMaterial;

        [Header("Fruit")]
        [Range(0, 100)] public int fruitCount = 10;
        [Range(0.1f, 5f)] public float fruitScale = 1f;

        public enum TrunkMeshMode { PrimitiveCylinder, ProceduralSpline }
        public TrunkMeshMode trunkMesh = TrunkMeshMode.ProceduralSpline;

        [Header("Taper")]
        public bool useSimpleTaper = true;
        [Range(0f, 1f)] public float tipRadiusFactor = 0.07f;
        [Range(0.1f, 5f)] public float taperExponent = 1.5f;

        [Header("Branch Scaling")]
        [Range(0.2f, 0.9f)] public float childRadiusFactor = 0.5f;
        [Range(0.05f, 1f)] public float childLenFactorMin = 0.25f;
        [Range(0.05f, 1f)] public float childLenFactorMax = 0.7f;
        public Vector2Int childCountRange = new Vector2Int(2, 4);
        [Range(0.1f, 3f)] public float branchLengthMultiplier = 1f;

        [Header("Branch Placement")]
        [Range(0f, 1f)] public float branchStartMin = 0.4f;
        [Range(0f, 1f)] public float branchStartMax = 0.9f;
        public float branchHeightOffset = 0f;
        public bool droopBranches = false;

        [Header("Branch Distribution")]
        public bool balancedPrimaryBranches = false;

        [Header("Fine Branches & Leaf Attachment")]
        public bool useFineBranches = true;
        public bool droopFineBranches = false;
        [Range(0f, 90f)] public float fineBranchDroopAngle = 30f;
        [Range(0.001f, 0.05f)] public float fineBranchRadius = 0.02f;
        [Range(0.1f, 1f)] public float fineBranchLength = 0.2f;
        [Range(1, 5)] public int fineBranchesPerLeafCluster = 3;
        public enum FineBranchPlacement { Clustered, Even, RandomAlongParent }
        public FineBranchPlacement fineBranchPlacement = FineBranchPlacement.RandomAlongParent;
        [Min(0.01f)] public float fineBranchSpacing = 0.25f;
        [Range(0f, 1f)] public float fineBranchSpacingJitter = 0.5f;
        [Range(0f, 1f)] public float leafPosMinOnTwig = 0.8f;
        [Range(0f, 1f)] public float leafPosMaxOnTwig = 1f;

        [Header("Leaf Clustering Along Parent")]
        public bool clusterLeavesAlongParent = false;
        [Range(0f, 1f)] public float clusterMinOnBranch = 0.7f;
        [Range(0f, 1f)] public float clusterMaxOnBranch = 1.0f;

        [Header("Palm Options")]
        [Range(0f, 5f)] public float extraTrunkCarve = 0f;
        public Vector2 palmCurveDirectionAndHeight = new Vector2(1, 1);
        [Range(0.1f, 5f)] public float leafMultiplier = 1f;
        [Range(0f, 5f)] public float palmLeafSpread = 1f;
        [Range(0f, 2f)] public float palmLeafYOffset = 0f;

        [Header("Coconut Settings")]
        public GameObject coconutPrefab;
        [Range(0, 20)] public int coconutCount = 5;
        public Vector2 coconutHeightRange = new Vector2(0.8f, 0.95f);
        [Range(0.1f, 5f)] public float coconutScale = 1f;

        [Header("Canopy Scatter")]
        public int minLeafDepth = 2;
        public float leafDensity = 6f;

        [Header("Weeping / Droop")]
        [Range(0f, 1f)] public float branchWeepFactor = 0f;

        [Header("General")]
        public int seed = 0;
        public bool liveUpdate = false;

        [Header("Surface Noise")]
        [Range(0f, 0.5f)] public float surfaceNoiseStrength = 0.04f;
        [Range(0f, 10f)] public float surfaceNoiseFrequency = 2f;

        [Header("Trunk Detail")]
        [Range(0f, 0.8f)] public float trunkBulgeAmplitude = 0.2f;
        [Range(0.1f, 10f)] public float trunkBulgeFrequency = 0.7f;
        [Range(0f, 0.5f)] public float trunkGrooveDepth = 0.1f;
        [Min(1)] public int trunkGrooveCount = 5;
        [Range(-3f, 3f)] public float trunkGrooveTwist = 0.4f;

        [Header("Gnarl / Age")]
        [Range(0f, 1f)] public float gnarlStrength = 0.25f;
        [Range(0.1f, 10f)] public float gnarlFrequency = 0.6f;
        [Range(0f, 2f)] public float trunkLeanCurve = 0f;  // now default zero

        // **FIX**: full definition now matches all usages below
        private struct SegmentData
        {
            public Vector3    pos;
            public Quaternion rot;
            public float      bottomR;
            public float      topR;
            public float      len;
            public int        radialSeg;
            public int        heightSeg;
            public int        seed;
            public Vector2    curve;
            public int        depth;
        }

        private struct BranchState { public Vector3 pos, dir; public float radius; public int depth; }

        private readonly List<SegmentData> segments = new List<SegmentData>();
        private readonly List<Matrix4x4>   leafMats  = new List<Matrix4x4>();
        private readonly List<Vector3>     fruitPos  = new List<Vector3>();
        private readonly List<Vector3>     fruitNrm  = new List<Vector3>();

        private System.Random rng;
        private Mesh           leafMesh;
        private Vector3        leafScale;

#if UNITY_EDITOR
        private bool scheduled;
        static bool InPrefabAsset(UnityEngine.Object o) => PrefabUtility.IsPartOfPrefabAsset(o);
#else
        static bool InPrefabAsset(UnityEngine.Object o) => false;
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (InPrefabAsset(this)) return;
            trunkLeanCurve = Mathf.Max(0f, trunkLeanCurve);
            if (!Application.isPlaying && liveUpdate && profile && !scheduled)
            {
                scheduled = true;
                EditorApplication.delayCall += () =>
                {
                    if (this) Regenerate();
                    scheduled = false;
                };
            }
        }
#endif

        private void OnEnable() => Regenerate();

        [ContextMenu("Regenerate")]
        public void Regenerate()
        {
            if (!profile || !profile.leafPrefab) return;
            ClearGenerated();
            InitData();
            BuildTree();
            ScatterFruits();
            BuildMeshes();
        }

        private void ClearGenerated()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c.name.StartsWith("Combined_") || c.name.StartsWith("Baked_"))
                    DestroyImmediate(c.gameObject);
            }
            MeshCache.Clear();
        }

        private void InitData()
        {
            rng      = seed == 0 ? new System.Random() : new System.Random(seed);
            segments.Clear(); leafMats.Clear(); fruitPos.Clear(); fruitNrm.Clear();
            var mf   = profile.leafPrefab.GetComponent<MeshFilter>();
            leafMesh = mf ? mf.sharedMesh : profile.leafPrefab.GetComponent<SkinnedMeshRenderer>()?.sharedMesh;
            leafScale= profile.leafPrefab.transform.localScale;
        }

        private Vector3 ApplyWeep(Vector3 dir) =>
            branchWeepFactor > 0f
              ? Vector3.Slerp(dir, -transform.up, branchWeepFactor).normalized
              : dir;

        private void BuildTree()
        {
            float H    = profile.trunkHeight + (profile.enablePalmSettings ? extraTrunkCarve : 0f);
            bool  palm = profile.enablePalmSettings;

            // root segment
            PushSegment(Vector3.zero, Vector3.up, H, profile.trunkRadius, 0, palm);

            Vector3 trunkTop = Vector3.up * H;
            Vector3 crown    = trunkTop
                              + Vector3.forward * palmCurveDirectionAndHeight.x
                              + Vector3.up      * palmCurveDirectionAndHeight.y * H
                              + Vector3.up      * palmLeafYOffset * H;

            if (palm && coconutPrefab && coconutCount > 0) SpawnCoconuts(crown, H);
            if (palm) { SpawnLeavesAtCrown(crown, palmLeafSpread); return; }

            var stack = new Stack<BranchState>();
            for (int i = 0; i < profile.primaryBranches; i++)
            {
                float yawBase = 360f * i / profile.primaryBranches;
                int   pairCt  = balancedPrimaryBranches ? 2 : 1;
                for (int p = 0; p < pairCt; p++)
                {
                    float yawDeg = yawBase + p * 180f;
                    float t      = (i + 0.5f) / profile.primaryBranches;
                    float h      = Mathf.Lerp(H * branchStartMin, H * branchStartMax, t)
                                 + branchHeightOffset;
                    Vector3 pStart = Vector3.up * h;
                    Quaternion yaw = Quaternion.AngleAxis(yawDeg, Vector3.up);
                    float pitch   = droopBranches
                                  ? RandRange(-profile.maxBranchAngle, -10f)
                                  : RandRange(10f, profile.maxBranchAngle);
                    Quaternion pit = Quaternion.AngleAxis(pitch, Vector3.right);
                    Vector3 dir   = ApplyWeep((yaw * pit) * Vector3.up);

                    stack.Push(new BranchState
                    {
                        pos    = pStart,
                        dir    = dir,
                        radius = profile.trunkRadius * childRadiusFactor,
                        depth  = 1
                    });
                }
            }

            while (stack.Count > 0)
            {
                var st = stack.Pop();
                if (st.depth > profile.recursionDepth)
                {
                    SpawnLeaves(st.pos, st.dir, fineBranchLength * 1.5f, st.depth);
                    continue;
                }

                float len = Mathf.Lerp(
                              profile.trunkHeight * childLenFactorMin,
                              profile.trunkHeight * childLenFactorMax,
                              Rand01()
                            )
                            * branchLengthMultiplier
                            / st.depth;

                if (st.depth >= minLeafDepth)
                    ScatterLeaves(st.pos, st.dir, len, st.depth);

                PushSegment(st.pos, st.dir, len, st.radius, st.depth, palm);

                Vector3 end = st.pos + st.dir * len;
                int     cn  = rng.Next(
                                childCountRange.x,
                                childCountRange.y + 1
                              );
                for (int c = 0; c < cn; c++)
                {
                    Quaternion yaw2 = Quaternion.AngleAxis(
                                        RandRange(0f, 360f),
                                        st.dir
                                      );
                    Quaternion pit2= Quaternion.AngleAxis(
                                        RandRange(15f, profile.maxBranchAngle),
                                        Vector3.right
                                      );
                    Vector3 dir2   = ApplyWeep((yaw2 * pit2) * st.dir);

                    stack.Push(new BranchState
                    {
                        pos    = end,
                        dir    = dir2,
                        radius = st.radius * childRadiusFactor,
                        depth  = st.depth + 1
                    });
                }
            }
        }

        private void PushSegment(Vector3 pos, Vector3 dir, float len, float rad, int depth, bool curved)
        {
            int   radial  = Mathf.Max(4, 12 - depth * 2);
            int   height  = Mathf.Clamp(
                                Mathf.RoundToInt(len / (rad * 1.0f)),
                                1, 4
                            );
            float tipR    = useSimpleTaper
                            ? rad * tipRadiusFactor
                            : rad * Mathf.Pow(tipRadiusFactor, taperExponent);
            float wander  = trunkLeanCurve * Mathf.Pow(depth + 1, 0.7f);
            pos += new Vector3(
                  Mathf.Sin(seed + depth * 1.3f) * wander,
                  0f,
                  Mathf.Cos(seed + depth * 0.9f) * wander
            );

            segments.Add(new SegmentData
            {
                pos        = pos,
                rot        = Quaternion.FromToRotation(Vector3.up, dir),
                bottomR    = rad,
                topR       = tipR,
                len        = len,
                radialSeg  = radial,
                heightSeg  = height,
                seed       = seed + depth * 999,
                curve      = curved ? palmCurveDirectionAndHeight : Vector2.zero,
                depth      = depth
            });
        }
        // --- Sprout “fine” branchlets alongside main branches ---
        private void SproutFineBranchesAlong(Vector3 startPos, Vector3 dir, float parentLen, int parentDepth)
        {
            if (!useFineBranches) return;
            float travelled = 0f;
            while (travelled < parentLen)
            {
                float tBase = travelled / parentLen;
                float t     = clusterLeavesAlongParent
                            ? Random.Range(clusterMinOnBranch, clusterMaxOnBranch)
                            : tBase;

                Vector3 origin = startPos + dir * (t * parentLen);
                float   jitter = fineBranchSpacing * Random.Range(
                                    1f - fineBranchSpacingJitter,
                                    1f + fineBranchSpacingJitter
                                );

                Quaternion yaw   = Quaternion.AngleAxis(Random.Range(0f, 360f), dir);
                Vector3   right = Vector3.Cross(dir, Vector3.up).normalized;
                float     pitchAng = droopFineBranches
                                    ? -Random.Range(0f, fineBranchDroopAngle)
                                    : Random.Range(-15f, 15f);
                Quaternion pitch    = Quaternion.AngleAxis(pitchAng, right);

                Vector3 twigDir     = ApplyWeep((yaw * pitch) * dir);
                float   twigLen     = Random.Range(fineBranchLength * 0.8f, fineBranchLength * 1.2f);

                PushSegment(origin, twigDir, twigLen, fineBranchRadius, parentDepth + 1, false);
                AddLeavesAlongTwig(origin, twigDir, twigLen);

                travelled += fineBranchPlacement switch
                {
                    FineBranchPlacement.Even             => fineBranchSpacing,
                    FineBranchPlacement.RandomAlongParent => jitter,
                    _                                     => parentLen
                };
            }
        }

        private void AddLeavesAlongTwig(Vector3 twigBase, Vector3 twigDir, float twigLen)
        {
            int leafN = Mathf.Max(
                            1,
                            Mathf.RoundToInt(
                                profile.leavesPerBranch * leafMultiplier
                               / fineBranchesPerLeafCluster
                            )
                        );
            for (int i = 0; i < leafN; i++)
            {
                float t = Random.Range(leafPosMinOnTwig, leafPosMaxOnTwig);
                Vector3 p = twigBase + twigDir * (t * twigLen);
                AddLeaf(p, twigDir);
            }
        }

        private void ScatterLeaves(Vector3 start, Vector3 dir, float len, int depth)
        {
            int clumps = Mathf.CeilToInt(leafDensity * len);
            for (int i = 0; i < clumps; i++)
            {
                float t = clusterLeavesAlongParent
                        ? Random.Range(clusterMinOnBranch, clusterMaxOnBranch)
                        : Random.Range(0f, 1f);
                SpawnLeaves(start + dir * (t * len), dir, len, depth);
            }
        }

        private void SpawnLeaves(Vector3 pos, Vector3 dir, float parentLen, int depth)
        {
            if (useFineBranches && fineBranchPlacement != FineBranchPlacement.Clustered)
            {
                SproutFineBranchesAlong(pos, dir, parentLen, depth);
                return;
            }

            int cnt = Mathf.RoundToInt(profile.leavesPerBranch * leafMultiplier);
            for (int i = 0; i < cnt; i++)
            {
                if (useFineBranches)
                {
                    Vector3 s = pos;
                    for (int j = 0; j < fineBranchesPerLeafCluster; j++)
                    {
                        Quaternion yaw = Quaternion.AngleAxis(Random.Range(0f, 360f), dir);
                        Vector3   right= Vector3.Cross(dir, Vector3.up).normalized;
                        float     pitch= droopFineBranches
                                      ? -Random.Range(0f, fineBranchDroopAngle)
                                      : Random.Range(-30f, 30f);
                        Quaternion pit  = Quaternion.AngleAxis(pitch, right);

                        Vector3 twigDir = ApplyWeep((yaw * pit) * dir);
                        float   twigLen = Random.Range(
                                            fineBranchLength * 0.8f,
                                            fineBranchLength * 1.2f
                                          );

                        PushSegment(s, twigDir, twigLen, fineBranchRadius, depth + 1, false);
                        s += twigDir * twigLen;
                    }
                    AddLeaf(s, dir);
                }
                else
                {
                    AddLeaf(pos, dir);
                }
            }
        }

        private void AddLeaf(Vector3 pos, Vector3 dir)
        {
            float cone = 60f * Mathf.Deg2Rad;
            Vector3 rnd  = Random.onUnitSphere;
            if (Vector3.Dot(rnd, dir) < Mathf.Cos(cone))
                rnd = Vector3.Slerp(dir, rnd, Random.Range(0f, 1f)).normalized;

            Vector3 offset = rnd * Random.Range(0f, 0.1f);
            Quaternion baseRot = Quaternion.LookRotation(rnd, dir);
            Quaternion yawQ    = Quaternion.AngleAxis(Random.Range(0f, 360f), rnd);
            Vector3   perp     = Vector3.Cross(rnd, dir).normalized;
            Quaternion pitQ    = Quaternion.AngleAxis(Random.Range(-0.15f, 0.15f), perp);
            Quaternion rot     = baseRot * yawQ * pitQ;

            float sc = profile.leafScale * (1f + Random.Range(-0.2f, 0.2f));
            leafMats.Add(Matrix4x4.TRS(pos + offset, rot, leafScale * sc));
        }

        private void SpawnLeavesAtCrown(Vector3 pos, float spread)
        {
            int cnt = Mathf.RoundToInt(profile.leavesPerBranch * leafMultiplier);
            for (int i = 0; i < cnt; i++)
            {
                float a   = 360f * i / cnt;
                Vector3 outd= Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Quaternion dp = Quaternion.AngleAxis(
                                    Random.Range(20f, 40f),
                                    Vector3.Cross(outd, Vector3.up)
                                );
                Vector3 dir = dp * outd;
                AddLeaf(pos + outd * spread, dir);
            }
        }

        private void ScatterFruits()
        {
            fruitPos.Clear(); fruitNrm.Clear();
            if (fruitCount <= 0 || leafMats.Count == 0) return;
            if (fruitMaterial == null && profile.fruitMaterial == null) return;

            var pick = Enumerable.Range(0, leafMats.Count)
                         .OrderBy(_ => rng.Next())
                         .Take(fruitCount);

            foreach (int idx in pick)
            {
                var m = leafMats[idx];
                fruitPos.Add(m.GetColumn(3));
                fruitNrm.Add(m.MultiplyVector(Vector3.up).normalized);
            }
        }

        private void SpawnCoconuts(Vector3 crown, float H)
        {
            for (int i = 0; i < coconutCount; i++)
            {
                float t  = RandRange(coconutHeightRange.x, coconutHeightRange.y);
                Vector3 bp = crown + Vector3.up * (H * t);
                float   a  = RandRange(0f, 360f) * Mathf.Deg2Rad;
                float   r  = profile.trunkRadius * (1f - t) * 0.5f;
                Vector3 p  = bp + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);

                var go = Instantiate(coconutPrefab, p, Quaternion.identity, transform);
                go.transform.localScale = Vector3.one * coconutScale;
            }
        }

        private void BuildMeshes()
        {
            float twigLimit = fineBranchRadius * 1.5f;
            var twigSegs  = segments.Where(s => s.bottomR <= twigLimit).ToList();
            var trunkSegs = segments.Except(twigSegs).ToList();

            Mesh trunkMesh = CombineSegments(trunkSegs);
            if (trunkMesh == null || trunkMesh.vertexCount == 0) return;

            WeldSharedRings(trunkMesh);
            var mfRoot = GetComponent<MeshFilter>();
            var mrRoot = GetComponent<MeshRenderer>();

            mfRoot.sharedMesh     = trunkMesh;
            mrRoot.sharedMaterial = barkMaterial ? barkMaterial : profile.barkMaterial;

            if (twigSegs.Count > 0)
            {
                Mesh twigMesh = CombineSegments(twigSegs);
                if (twigMesh != null && twigMesh.vertexCount > 0)
                {
                    var goTwig = new GameObject("Combined_Twigs");
                    goTwig.transform.SetParent(transform, false);
                    goTwig.AddComponent<MeshFilter>().sharedMesh     = twigMesh;
                    goTwig.AddComponent<MeshRenderer>().sharedMaterial =
                        branchMaterial ? branchMaterial : mrRoot.sharedMaterial;
                }
            }

            if (leafMats.Count > 0)   BuildLeaves();
            if (fruitPos.Count > 0)   BuildFruits();
        }

        private Mesh CombineSegments(List<SegmentData> list)
        {
            if (list.Count == 0) return null;
            var combineList = new List<CombineInstance>();
            int  vtx         = 0;
            float trunkR     = profile.trunkRadius;

            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                float raw      = Mathf.InverseLerp(trunkR * 0.2f, trunkR, s.bottomR);
                float dispMask = Mathf.Max(0.25f, Mathf.Round(raw * 20f) / 20f);

                var mesh = MeshCache.GetOrCreate(
                    new MeshCache.Key
                    {
                        bottomR     = s.bottomR,
                        topR        = s.topR,
                        length      = s.len,
                        radialSeg   = s.radialSeg,
                        heightSeg   = s.heightSeg,
                        noiseStr    = surfaceNoiseStrength * dispMask,
                        noiseFreq   = surfaceNoiseFrequency,
                        seed        = s.seed,
                        curve       = s.curve,
                        bulgeAmp    = trunkBulgeAmplitude * dispMask,
                        bulgeFreq   = trunkBulgeFrequency,
                        grooveDepth = trunkGrooveDepth * dispMask,
                        grooveCount = trunkGrooveCount,
                        grooveTwist = trunkGrooveTwist,
                        gnarlStrength   = gnarlStrength * dispMask,
                        gnarlFrequency  = gnarlFrequency,
                        dispMask        = dispMask
                    },
                    () => BuildCurvedTaperedCylinderMesh(
                              s.bottomR, s.topR, s.len,
                              s.radialSeg, s.heightSeg,
                              surfaceNoiseStrength * dispMask,
                              surfaceNoiseFrequency,
                              s.seed, s.curve,
                              trunkBulgeAmplitude * dispMask,
                              trunkBulgeFrequency,
                              trunkGrooveDepth * dispMask,
                              trunkGrooveCount,
                              trunkGrooveTwist,
                              gnarlStrength * dispMask,
                              gnarlFrequency
                          )
                );

                if (mesh == null || mesh.vertexCount == 0) continue;

                combineList.Add(
                    new CombineInstance
                    {
                        mesh      = mesh,
                        transform = Matrix4x4.TRS(s.pos, s.rot, Vector3.one)
                    }
                );
                vtx += mesh.vertexCount;
            }

            if (combineList.Count == 0) return null;
            var res = new Mesh
            {
                indexFormat = vtx > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            res.CombineMeshes(combineList.ToArray(), true, true);
            res.RecalculateBounds();
            return res;
        }
        private void BuildLeaves()
        {
            Material matLeaves = leafMaterial
                              ? leafMaterial
                              : (profile.leafMaterial ?? profile.leafPrefab
                                                    .GetComponent<Renderer>()
                                                    ?.sharedMaterial);
            if (!matLeaves) return;

            bool inst   = Application.isPlaying && matLeaves.enableInstancing;
            bool prefab = InPrefabAsset(gameObject);

            if (inst && !prefab)
            {
                var go = new GameObject("Combined_Leaves");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh     = leafMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.enabled = false;
                var lmi = go.AddComponent<LeafMaterialInstancer>();
                lmi.Setup(leafMesh, matLeaves, leafMats);
                return;
            }

            var ci = new CombineInstance[leafMats.Count];
            for (int i = 0; i < leafMats.Count; i++)
                ci[i] = new CombineInstance { mesh = leafMesh, transform = leafMats[i] };

            var meshLeaves = new Mesh
            {
                indexFormat = leafMesh.vertexCount * leafMats.Count > 65535
                              ? IndexFormat.UInt32
                              : IndexFormat.UInt16
            };
            meshLeaves.CombineMeshes(ci, true, true);
            meshLeaves.RecalculateBounds();
            meshLeaves.RecalculateNormals();

            var goLeaves = new GameObject("Combined_Leaves");
            goLeaves.transform.SetParent(transform, false);
            goLeaves.AddComponent<MeshFilter>().sharedMesh       = meshLeaves;
            goLeaves.AddComponent<MeshRenderer>().sharedMaterial = matLeaves;
        }

        private void BuildFruits()
        {
            Material matF = fruitMaterial ?? profile.fruitMaterial;
            Mesh     sph  = BuildFruitSphereMesh(fruitScale);

            var ci = new CombineInstance[fruitPos.Count];
            for (int i = 0; i < fruitPos.Count; i++)
            {
                ci[i] = new CombineInstance
                {
                    mesh      = sph,
                    transform = Matrix4x4.TRS(fruitPos[i], Quaternion.LookRotation(fruitNrm[i]), Vector3.one)
                };
            }

            var meshF = new Mesh { indexFormat = IndexFormat.UInt32 };
            meshF.CombineMeshes(ci, true, true);

            var goF = new GameObject("Combined_Fruits");
            goF.transform.SetParent(transform, false);
            goF.AddComponent<MeshFilter>().sharedMesh       = meshF;
            goF.AddComponent<MeshRenderer>().sharedMaterial = matF;
        }

        private static Mesh BuildCurvedTaperedCylinderMesh(
            float    bottomR, float topR, float height,
            int      radialSeg, int heightSeg,
            float    noiseStr, float noiseFreq,
            int      seed, Vector2 curve,
            float    bulgeAmp, float bulgeFreq,
            float    grooveDepth, int grooveCount, float grooveTwist,
            float    gnarlStr, float gnarlFreq)
        {
            int rings = radialSeg + 1;
            var verts = new Vector3[rings * (heightSeg + 1)];
            var norms = new Vector3[verts.Length];
            var uvs   = new Vector2[verts.Length];
            var tris  = new int[radialSeg * heightSeg * 6];
            int ti     = 0;
            var prng   = new System.Random(seed);
            float phase= (float)prng.NextDouble() * Mathf.PI * 2f;

            for (int y = 0; y <= heightSeg; y++)
            {
                float v  = (float)y / heightSeg;
                float yP = v * height;
                float r0 = Mathf.Lerp(bottomR, topR, v);
                float bulge = bulgeAmp * Mathf.Sin((v * height * bulgeFreq + phase) * Mathf.PI * 2f);
                float rW = r0 * (1f + bulge);
                float xO = Mathf.Sin(v * Mathf.PI * 0.5f) * curve.x * curve.y;

                for (int x = 0; x <= radialSeg; x++)
                {
                    int idx = y * rings + x;
                    float u   = (float)x / radialSeg;
                    float ang = u * Mathf.PI * 2f;
                    float cx  = Mathf.Cos(ang);
                    float sx  = Mathf.Sin(ang);
                    float nVal= (Mathf.PerlinNoise(u * noiseFreq + seed,
                                                    v * noiseFreq + seed) - .5f)
                              * 2f * noiseStr;
                    float groove= grooveDepth > 0f && grooveCount > 0
                                ? 1f - grooveDepth
                                    * (0.5f
                                    + 0.5f
                                    * Mathf.Sin(grooveCount * ang
                                        + grooveTwist
                                        * height * v
                                        * 2f * Mathf.PI))
                                : 1f;
                    float gnarl = gnarlStr
                                * Mathf.Sin((v * height + u)
                                    * gnarlFreq
                                    * Mathf.PI * 2f + seed);
                    float rF = rW * (1f + nVal) * groove * (1f + gnarl);

                    verts[idx] = new Vector3(xO + rF * cx, yP, rF * sx);

                    Vector3 radialDir = new Vector3(cx, (bottomR - topR) / height, sx);
                    Vector3 normal    = (radialDir
                                     + new Vector3(cx, 0, sx)
                                     * (bulgeAmp * bulgeFreq
                                     * Mathf.Cos((v * height * bulgeFreq + phase)
                                     * Mathf.PI * 2f)))
                                     .normalized;
                    norms[idx] = normal;
                    uvs[idx]   = new Vector2(u, v);

                    if (y < heightSeg && x < radialSeg)
                    {
                        tris[ti++] = idx;
                        tris[ti++] = idx + rings;
                        tris[ti++] = idx + rings + 1;
                        tris[ti++] = idx;
                        tris[ti++] = idx + rings + 1;
                        tris[ti++] = idx + 1;
                    }
                }
            }

            var mesh = new Mesh { vertices = verts, normals = norms, uv = uvs, triangles = tris };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildFruitSphereMesh(float scale)
        {
            var src   = GameObject
                      .CreatePrimitive(PrimitiveType.Sphere)
                      .GetComponent<MeshFilter>()
                      .sharedMesh;
            var verts = src.vertices.ToArray();
            for (int i = 0; i < verts.Length; i++) verts[i] *= scale;
            var mesh  = new Mesh
            {
                vertices  = verts,
                normals   = src.normals,
                uv        = src.uv,
                triangles = src.triangles
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void WeldSharedRings(Mesh mesh, float eps = 1e-4f)
        {
            var verts  = mesh.vertices;
            var norms  = mesh.normals;
            var map    = new int[verts.Length];
            var dict   = new Dictionary<Vector3, int>();
            var newV   = new List<Vector3>();
            var newN   = new List<Vector3>();

            for (int i = 0; i < verts.Length; i++)
            {
                var v   = verts[i];
                var key = new Vector3(
                              Mathf.Round(v.x / eps) * eps,
                              Mathf.Round(v.y / eps) * eps,
                              Mathf.Round(v.z / eps) * eps
                          );
                if (dict.TryGetValue(key, out var idx))
                {
                    newN[idx] += norms[i];
                    map[i]    = idx;
                }
                else
                {
                    idx       = newV.Count;
                    dict[key] = idx;
                    newV.Add(v);
                    newN.Add(norms[i]);
                    map[i]    = idx;
                }
            }

            var tris = mesh.triangles;
            for (int i = 0; i < tris.Length; i++)
                tris[i] = map[tris[i]];

            for (int i = 0; i < newN.Count; i++)
                newN[i].Normalize();

            mesh.Clear();
            mesh.SetVertices(newV);
            mesh.SetNormals(newN);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        internal static class MeshCache
        {
            public struct Key : IEquatable<Key>
            {
                public float bottomR, topR, length, noiseStr, noiseFreq;
                public int   radialSeg, heightSeg, seed;
                public Vector2 curve;
                public float bulgeAmp, bulgeFreq, grooveDepth, grooveTwist;
                public int   grooveCount;
                public float gnarlStrength, gnarlFrequency, dispMask;

                public bool Equals(Key o) =>
                       Mathf.Approximately(bottomR, o.bottomR)
                    && Mathf.Approximately(topR,    o.topR)
                    && Mathf.Approximately(length,  o.length)
                    && Mathf.Approximately(noiseStr,o.noiseStr)
                    && Mathf.Approximately(noiseFreq,o.noiseFreq)
                    && radialSeg == o.radialSeg
                    && heightSeg == o.heightSeg
                    && seed      == o.seed
                    && curve     == o.curve
                    && Mathf.Approximately(bulgeAmp, o.bulgeAmp)
                    && Mathf.Approximately(bulgeFreq,o.bulgeFreq)
                    && Mathf.Approximately(grooveDepth, o.grooveDepth)
                    && grooveCount   == o.grooveCount
                    && Mathf.Approximately(grooveTwist, o.grooveTwist)
                    && Mathf.Approximately(gnarlStrength, o.gnarlStrength)
                    && Mathf.Approximately(gnarlFrequency,o.gnarlFrequency)
                    && Mathf.Approximately(dispMask,      o.dispMask);

                public override int GetHashCode()
                {
                    var h = new HashCode();
                    h.Add(bottomR); h.Add(topR); h.Add(length);
                    h.Add(noiseStr); h.Add(noiseFreq);
                    h.Add(radialSeg); h.Add(heightSeg); h.Add(seed); h.Add(curve);
                    h.Add(bulgeAmp); h.Add(bulgeFreq);
                    h.Add(grooveDepth); h.Add(grooveCount); h.Add(grooveTwist);
                    h.Add(gnarlStrength); h.Add(gnarlFrequency); h.Add(dispMask);
                    return h.ToHashCode();
                }
            }

            private static readonly Dictionary<Key, Mesh> cache = new();
            public static Mesh GetOrCreate(Key k, Func<Mesh> build)
            {
                if (cache.TryGetValue(k, out var m))
                {
                    if (m == null) cache.Remove(k);
                    else if (m.vertexCount == 0) return null;
                    else return m;
                }
                m = build();
                cache[k] = m;
                return m;
            }

            public static void Clear() { cache.Clear(); }
        }

        private float Rand01(float max = 1f)    => (float)rng.NextDouble() * max;
        private float RandRange(float a, float b) => a + (float)rng.NextDouble() * (b - a);

#if UNITY_EDITOR
        [ContextMenu("Save As Prefab To MyTrees")]
        public void SaveAsPrefabToMyTrees()
        {
            var temp = Instantiate(gameObject);
            temp.name = gameObject.name + "_BakeTemp";
            temp.GetComponent<TreeGeneratorOptimized>().Regenerate();

            // strip any LeafMaterialInstancer
            foreach (var inst in temp.GetComponentsInChildren<MonoBehaviour>(true))
                if (inst && inst.GetType().Name == "LeafMaterialInstancer")
                    DestroyImmediate(inst);

            const string root   = "Assets/SmartCreator";
            const string folder = root + "/MyTrees";
            if (!AssetDatabase.IsValidFolder(root))   AssetDatabase.CreateFolder("Assets","SmartCreator");
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(root,"MyTrees");

            string safe = name.Replace(" ", "_");
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safe}.prefab");
            PrefabUtility.SaveAsPrefabAssetAndConnect(temp, path, InteractionMode.UserAction);
            DestroyImmediate(temp);
        }

        [ContextMenu("Bake For Terrain")]
        public void BakeForTerrain()
        {
            var tmp = Instantiate(gameObject, transform.position, transform.rotation);
            tmp.name = name + "_TerrainBake";
            var gen = tmp.GetComponent<TreeGeneratorOptimized>();

            var leafMat = gen.leafMaterial ? gen.leafMaterial : gen.profile.leafMaterial;
            bool hadInst = leafMat && leafMat.enableInstancing;
            if (hadInst) leafMat.enableInstancing = false;

            gen.Regenerate();
            if (hadInst) leafMat.enableInstancing = true;

            foreach (var inst in tmp.GetComponentsInChildren<MonoBehaviour>(true))
                if (inst && inst.GetType().Name == "LeafMaterialInstancer")
                    DestroyImmediate(inst);

            var rootMF  = tmp.GetComponent<MeshFilter>();
            var rootMR  = tmp.GetComponent<MeshRenderer>();
            var childMF = tmp.GetComponentsInChildren<MeshFilter>()
                           .Where(mf => mf.transform != tmp.transform).ToArray();

            var list = new List<CombineInstance> {
                new CombineInstance { mesh = rootMF.sharedMesh, transform = Matrix4x4.identity }
            };
            foreach (var mf in childMF)
                list.Add(new CombineInstance {
                    mesh      = mf.sharedMesh,
                    transform = tmp.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix
                });

            var merged = new Mesh { name = tmp.name + "_Merged" };
            merged.CombineMeshes(list.ToArray(), false, true);
            merged.RecalculateBounds();

            rootMF.sharedMesh = merged;
            Material bark  = gen.barkMaterial ? gen.barkMaterial : gen.profile.barkMaterial;
            Material leaves= leafMat;
            rootMR.sharedMaterials = leaves ? new[] { bark, leaves } : new[] { bark };

            foreach (var mf in childMF) DestroyImmediate(mf.gameObject);

            const string folder = "Assets/SmartCreator/MyTrees";
            if (!AssetDatabase.IsValidFolder("Assets/SmartCreator"))
                AssetDatabase.CreateFolder("Assets","SmartCreator");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/SmartCreator","MyTrees");

            string meshPath   = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{tmp.name}_mesh.asset");
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{tmp.name}.prefab");
            AssetDatabase.CreateAsset(merged, meshPath);
            PrefabUtility.SaveAsPrefabAssetAndConnect(tmp, prefabPath, InteractionMode.UserAction);
            DestroyImmediate(tmp);
        }
#endif
    }
}
