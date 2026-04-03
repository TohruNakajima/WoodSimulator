#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using SmartCreator.ProceduralTrees;
using System.Collections.Generic;

namespace WoodSimulator
{
    /// <summary>
    /// 代表年齢パターンごとにLODGroup付きプレハブを一括Bakeするエディタツール。
    /// LOD0: フルメッシュ（幹・枝・葉を別パーツ）
    /// LOD1: 簡略化メッシュ（3パーツを1メッシュに結合、頂点数削減）
    /// LOD2: ビルボードクワッド
    /// </summary>
    public static class LODTreePrefabBaker
    {
        private static readonly int[] RepresentativeAges = { 10, 25, 40, 55, 75, 100 };
        private const string OutputFolder = "Assets/BakedTrees";

        [MenuItem("WoodSimulator/Bake LOD Tree Prefabs")]
        public static void BakeAllPatterns()
        {
            // GrowthDatabase取得
            var growthDB = AssetDatabase.LoadAssetAtPath<GrowthDatabase>("Assets/Model/GrowthDatabase.asset");
            if (growthDB == null)
            {
                Debug.LogError("[LODTreePrefabBaker] GrowthDatabase.asset not found.");
                return;
            }

            // マテリアル取得
            var barkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SmartCreatorProceduralTrees/Materials/PineTrunk.mat");
            var leafMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SmartCreatorProceduralTrees/Materials/PineLeaf.mat");
            if (leafMat == null || barkMat == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CedarTreeTest/Prefabs/CedarTreeTreeGeneratorOptimized.prefab");
                if (prefab != null)
                {
                    var gen = prefab.GetComponent<PineTreeGenerator>();
                    if (gen != null)
                    {
                        if (barkMat == null) barkMat = gen.barkMaterial;
                        if (leafMat == null) leafMat = gen.leafMaterial;
                    }
                }
            }

            if (barkMat == null || leafMat == null)
            {
                Debug.LogError("[LODTreePrefabBaker] Materials not found.");
                return;
            }

            // GPU Instancing有効化
            EnableGPUInstancing(barkMat);
            EnableGPUInstancing(leafMat);

            // 出力フォルダ作成
            EnsureFolder(OutputFolder);
            EnsureFolder(OutputFolder + "/Meshes");

            // 一時的なGeneratorを作成
            var tempGO = new GameObject("_TempTreeGenerator");
            var generator = tempGO.AddComponent<PineTreeGenerator>();
            generator.autoRegenerate = false;
            generator.barkMaterial = barkMat;
            generator.leafMaterial = leafMat;

            int bakedCount = 0;

            foreach (int age in RepresentativeAges)
            {
                var data = growthDB.GetDataByAge(age);
                if (data == null)
                {
                    data = growthDB.GetNearestDataByAge(age);
                    if (data == null) continue;
                }

                // TreeParameterMapperで設定（growthFactor=1.0の標準木）
                TreeParameterMapper.ApplyToGenerator(generator, data, 1.0f, age * 100);

                // LOD付きプレハブを生成
                BakeLODPrefab(generator, barkMat, leafMat, age, data);
                bakedCount++;
            }

            Object.DestroyImmediate(tempGO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LODTreePrefabBaker] Baked {bakedCount} LOD prefabs to {OutputFolder}");
        }

        private static void BakeLODPrefab(PineTreeGenerator gen, Material barkMat, Material leafMat, int age, GrowthData data)
        {
            string prefabName = $"CedarTree_Age{age:D3}";

            // --- LOD0: フルメッシュ（3サブメッシュ） ---
            Mesh lod0Mesh = BuildLOD0Mesh(gen, prefabName);

            // --- LOD1: 簡略化メッシュ ---
            Mesh lod1Mesh = BuildLOD1Mesh(gen, prefabName);

            // --- LOD2: ビルボードクワッド ---
            Mesh lod2Mesh = BuildBillboardMesh(gen.trunkHeight, gen.baseBranchLength, prefabName);

            // メッシュをアセットとして保存
            AssetDatabase.CreateAsset(lod0Mesh, $"{OutputFolder}/Meshes/{prefabName}_LOD0.asset");
            AssetDatabase.CreateAsset(lod1Mesh, $"{OutputFolder}/Meshes/{prefabName}_LOD1.asset");
            AssetDatabase.CreateAsset(lod2Mesh, $"{OutputFolder}/Meshes/{prefabName}_LOD2.asset");

            // プレハブ用GameObjectを構築
            var rootGO = new GameObject(prefabName);
            var lodGroup = rootGO.AddComponent<LODGroup>();

            // LOD0 子オブジェクト
            var lod0GO = CreateMeshObject("LOD0", rootGO.transform, lod0Mesh,
                new Material[] { barkMat, barkMat, leafMat });

            // LOD1 子オブジェクト
            var lod1GO = CreateMeshObject("LOD1", rootGO.transform, lod1Mesh,
                new Material[] { barkMat, barkMat, leafMat });

            // LOD2 子オブジェクト（ビルボード）
            var lod2GO = CreateMeshObject("LOD2", rootGO.transform, lod2Mesh,
                new Material[] { leafMat });

            // LODGroup設定
            var lods = new LOD[3];
            lods[0] = new LOD(0.3f, lod0GO.GetComponents<Renderer>());  // 30%以上 → LOD0
            lods[1] = new LOD(0.1f, lod1GO.GetComponents<Renderer>());  // 10%以上 → LOD1
            lods[2] = new LOD(0.01f, lod2GO.GetComponents<Renderer>()); // 1%以上 → LOD2
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            // プレハブとして保存
            string prefabPath = $"{OutputFolder}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath);
            Object.DestroyImmediate(rootGO);
        }

        /// <summary>
        /// LOD0: エディタ用ビルダーでフルクオリティメッシュを生成（3サブメッシュ）。
        /// </summary>
        private static Mesh BuildLOD0Mesh(PineTreeGenerator gen, string name)
        {
            // Generateで子オブジェクトにメッシュが作られるので、それを取得して結合
            // 直接ビルダーを呼ぶためにリフレクション不要 - publicなGenerate()後にtransformから取得
            gen.Generate();

            var combine = new List<CombineInstance>();
            var trunk = gen.transform.Find("Trunk");
            var branches = gen.transform.Find("Branches");
            var leaves = gen.transform.Find("Leaves");

            if (trunk != null) combine.Add(new CombineInstance { mesh = trunk.GetComponent<MeshFilter>().sharedMesh, transform = Matrix4x4.identity });
            if (branches != null) combine.Add(new CombineInstance { mesh = branches.GetComponent<MeshFilter>().sharedMesh, transform = Matrix4x4.identity });
            if (leaves != null) combine.Add(new CombineInstance { mesh = leaves.GetComponent<MeshFilter>().sharedMesh, transform = Matrix4x4.identity });

            var mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.name = name + "_LOD0";
            mesh.CombineMeshes(combine.ToArray(), false, false, false);
            mesh.RecalculateBounds();

            // 一時子オブジェクトを削除
            CleanupGeneratedChildren(gen.transform);
            return mesh;
        }

        /// <summary>
        /// LOD1: LOD0と同じメッシュ構造だが、枝・葉を間引いた簡略版。
        /// whorlCountとbaseLeavesPerBranchを半減して生成。
        /// </summary>
        private static Mesh BuildLOD1Mesh(PineTreeGenerator gen, string name)
        {
            // パラメータを一時退避して簡略化
            int origWhorl = gen.whorlCount;
            int origBranches = gen.branchesPerWhorl;
            int origLeaves = gen.baseLeavesPerBranch;

            gen.whorlCount = Mathf.Max(4, origWhorl / 2);
            gen.branchesPerWhorl = Mathf.Max(2, origBranches / 2);
            gen.baseLeavesPerBranch = Mathf.Max(8, origLeaves / 2);

            gen.Generate();

            var combine = new List<CombineInstance>();
            var trunk = gen.transform.Find("Trunk");
            var branches = gen.transform.Find("Branches");
            var leaves = gen.transform.Find("Leaves");

            if (trunk != null) combine.Add(new CombineInstance { mesh = trunk.GetComponent<MeshFilter>().sharedMesh, transform = Matrix4x4.identity });
            if (branches != null) combine.Add(new CombineInstance { mesh = branches.GetComponent<MeshFilter>().sharedMesh, transform = Matrix4x4.identity });
            if (leaves != null) combine.Add(new CombineInstance { mesh = leaves.GetComponent<MeshFilter>().sharedMesh, transform = Matrix4x4.identity });

            var mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.name = name + "_LOD1";
            mesh.CombineMeshes(combine.ToArray(), false, false, false);
            mesh.RecalculateBounds();

            // パラメータを復元・一時子オブジェクトを削除
            gen.whorlCount = origWhorl;
            gen.branchesPerWhorl = origBranches;
            gen.baseLeavesPerBranch = origLeaves;
            CleanupGeneratedChildren(gen.transform);
            return mesh;
        }

        /// <summary>
        /// LOD2: 樹高と樹冠幅に基づくビルボードクワッド。
        /// </summary>
        private static Mesh BuildBillboardMesh(float treeHeight, float crownWidth, string name)
        {
            float halfW = Mathf.Max(crownWidth, treeHeight * 0.3f);
            float h = treeHeight * 1.1f;

            var mesh = new Mesh();
            mesh.name = name + "_LOD2";
            mesh.vertices = new Vector3[]
            {
                new Vector3(-halfW, 0, 0),
                new Vector3(halfW, 0, 0),
                new Vector3(halfW, h, 0),
                new Vector3(-halfW, h, 0)
            };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(1, 1), new Vector2(0, 1)
            };
            mesh.normals = new Vector3[]
            {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreateMeshObject(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterials = materials;
            return go;
        }

        private static void EnableGPUInstancing(Material mat)
        {
            if (mat != null && !mat.enableInstancing)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
            }
        }

        private static void CleanupGeneratedChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string parent = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string check = string.Join("/", parts, 0, i + 1);
                    if (!AssetDatabase.IsValidFolder(check))
                        AssetDatabase.CreateFolder(parent, parts[i]);
                    parent = check;
                }
            }
        }
    }
}
#endif
