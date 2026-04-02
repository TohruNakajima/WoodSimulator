using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering; // For IndexFormat
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SmartCreator.ProceduralTrees.Editor
{
    [CustomEditor(typeof(WillowTreeGenerator))]
    public class WillowTreeGeneratorEditor : UnityEditor.Editor
    {
        WillowTreeGenerator gen;

        // Serialized properties
        SerializedProperty
            pTrunkHeight, pTrunkRadius, pTrunkRadialSegs, pTrunkHeightSegs,
            pTrunkCurveDir, pTrunkCurveStr,
            pBranchCount, pBranchLength, pBranchSegs, pBranchInitDroop, pBranchDroopAngle, pBranchRadius, pBranchMat,
            pSubPerMain, pSubLenMin, pSubLenMax, pSubDroopAngle, pSubRadius,
            pTendPerSub, pTendLenMin, pTendLenMax, pTendDroop, pTendRadius,
            pLeafPrefab, pLeavesPerTendril, pLeafScaleMin, pLeafScaleMax, pLeafSideOffset,
            pTrunkMat;

        void OnEnable()
        {
            gen = (WillowTreeGenerator)target;

            // Trunk
            pTrunkHeight     = serializedObject.FindProperty(nameof(gen.trunkHeight));
            pTrunkRadius     = serializedObject.FindProperty(nameof(gen.trunkRadius));
            pTrunkRadialSegs = serializedObject.FindProperty(nameof(gen.trunkRadialSegments));
            pTrunkHeightSegs = serializedObject.FindProperty(nameof(gen.trunkHeightSegments));
            pTrunkCurveDir   = serializedObject.FindProperty(nameof(gen.trunkCurveDirection));
            pTrunkCurveStr   = serializedObject.FindProperty(nameof(gen.trunkCurveStrength));

            // Main Branches
            pBranchCount     = serializedObject.FindProperty(nameof(gen.branchCount));
            pBranchLength    = serializedObject.FindProperty(nameof(gen.branchLength));
            pBranchSegs      = serializedObject.FindProperty(nameof(gen.branchSegments));
            pBranchInitDroop = serializedObject.FindProperty(nameof(gen.branchInitialDroop));
            pBranchDroopAngle= serializedObject.FindProperty(nameof(gen.branchDroopAngle));
            pBranchRadius    = serializedObject.FindProperty(nameof(gen.branchRadius));
            pBranchMat       = serializedObject.FindProperty(nameof(gen.branchMaterial));

            // Secondary Branches
            pSubPerMain      = serializedObject.FindProperty(nameof(gen.subBranchPerMain));
            pSubLenMin       = serializedObject.FindProperty(nameof(gen.subBranchLenMin));
            pSubLenMax       = serializedObject.FindProperty(nameof(gen.subBranchLenMax));
            pSubDroopAngle   = serializedObject.FindProperty(nameof(gen.subBranchDroopAngle));
            pSubRadius       = serializedObject.FindProperty(nameof(gen.subBranchRadius));

            // Tendrils
            pTendPerSub      = serializedObject.FindProperty(nameof(gen.tendrilsPerSub));
            pTendLenMin      = serializedObject.FindProperty(nameof(gen.tendrilLenMin));
            pTendLenMax      = serializedObject.FindProperty(nameof(gen.tendrilLenMax));
            pTendDroop       = serializedObject.FindProperty(nameof(gen.tendrilDroop));
            pTendRadius      = serializedObject.FindProperty(nameof(gen.tendrilRadius));

            // Leaves
            pLeafPrefab      = serializedObject.FindProperty(nameof(gen.leafPrefab));
            pLeavesPerTendril= serializedObject.FindProperty(nameof(gen.leavesPerTendril));
            pLeafScaleMin    = serializedObject.FindProperty(nameof(gen.leafScaleMin));
            pLeafScaleMax    = serializedObject.FindProperty(nameof(gen.leafScaleMax));
            pLeafSideOffset  = serializedObject.FindProperty(nameof(gen.leafSideOffset));

            // Materials
            pTrunkMat        = serializedObject.FindProperty(nameof(gen.trunkMaterial));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Trunk", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(pTrunkHeight);
            EditorGUILayout.PropertyField(pTrunkRadius);
            EditorGUILayout.PropertyField(pTrunkRadialSegs, new GUIContent("Radial Segments"));
            EditorGUILayout.PropertyField(pTrunkHeightSegs, new GUIContent("Height Segments"));
            EditorGUILayout.PropertyField(pTrunkCurveDir);
            EditorGUILayout.PropertyField(pTrunkCurveStr);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Main Branches", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(pBranchCount);
            EditorGUILayout.PropertyField(pBranchLength);
            EditorGUILayout.PropertyField(pBranchSegs, new GUIContent("Segments"));
            EditorGUILayout.PropertyField(pBranchInitDroop, new GUIContent("Initial Droop"));
            EditorGUILayout.PropertyField(pBranchDroopAngle);
            EditorGUILayout.PropertyField(pBranchRadius);
            EditorGUILayout.PropertyField(pBranchMat);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Secondary Branches", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(pSubPerMain, new GUIContent("Per Main"));
            EditorGUILayout.PropertyField(pSubLenMin);
            EditorGUILayout.PropertyField(pSubLenMax);
            EditorGUILayout.PropertyField(pSubDroopAngle);
            EditorGUILayout.PropertyField(pSubRadius);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tendrils", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(pTendPerSub, new GUIContent("Per Sub-branch"));
            EditorGUILayout.PropertyField(pTendLenMin);
            EditorGUILayout.PropertyField(pTendLenMax);
            EditorGUILayout.PropertyField(pTendDroop);
            EditorGUILayout.PropertyField(pTendRadius);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Leaves", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(pLeafPrefab);
            EditorGUILayout.PropertyField(pLeavesPerTendril);
            EditorGUILayout.PropertyField(pLeafScaleMin);
            EditorGUILayout.PropertyField(pLeafScaleMax);
            EditorGUILayout.PropertyField(pLeafSideOffset);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(pTrunkMat, new GUIContent("Trunk Material"));

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Willow"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Build Willow");
                    gen.BuildWillow();
                    // Only mark scene dirty in edit mode, not in play mode!
                    if (!Application.isPlaying)
                        EditorSceneManager.MarkSceneDirty(gen.gameObject.scene);
                }
                if (GUILayout.Button("Save Prefab"))
                {
                    var path = EditorUtility.SaveFilePanelInProject(
                        "Save Willow Prefab", gen.name + ".prefab", "prefab", "Choose a location"
                    );
                    if (!string.IsNullOrEmpty(path))
                    {
                        var temp = Object.Instantiate(gen.gameObject);
                        temp.name = gen.name + "_Prefab";
                        temp.GetComponent<WillowTreeGenerator>().BuildWillow();
                        PrefabUtility.SaveAsPrefabAssetAndConnect(temp, path, InteractionMode.UserAction);
                        Object.DestroyImmediate(temp);
                    }
                }
                if (GUILayout.Button("Bake For Terrain"))
                {
                    var tmp = Object.Instantiate(gen.gameObject, gen.transform.position, gen.transform.rotation);
                    tmp.name = gen.name + "_Terrain";
                    tmp.GetComponent<WillowTreeGenerator>().BuildWillow();

                    var comb = new List<CombineInstance>();
                    foreach (var f in tmp.GetComponentsInChildren<MeshFilter>())
                        if (f.sharedMesh != null)
                            comb.Add(new CombineInstance {
                                mesh      = f.sharedMesh,
                                transform = f.transform.localToWorldMatrix
                            });

                    var merged = new Mesh {
                        name = tmp.name + "_Merged",
                        indexFormat = IndexFormat.UInt32
                    };
                    merged.CombineMeshes(comb.ToArray(), true, true);
                    merged.RecalculateBounds();
                    tmp.GetComponent<MeshFilter>().sharedMesh = merged;

                    var mr = tmp.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = gen.trunkMaterial;
                    for (int i = tmp.transform.childCount - 1; i >= 0; i--)
                        Object.DestroyImmediate(tmp.transform.GetChild(i).gameObject);

                    const string ROOT = "Assets/SmartCreatorProceduralTrees";
                    const string FOLD = ROOT + "/MyTrees";
                    if (!AssetDatabase.IsValidFolder(ROOT))
                        AssetDatabase.CreateFolder("Assets", "SmartCreatorProceduralTrees");
                    if (!AssetDatabase.IsValidFolder(FOLD))
                        AssetDatabase.CreateFolder(ROOT, "MyTrees");

                    var meshPath = AssetDatabase.GenerateUniqueAssetPath($"{FOLD}/{tmp.name}_Mesh.asset");
                    AssetDatabase.CreateAsset(merged, meshPath);
                    var prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{FOLD}/{tmp.name}.prefab");
                    PrefabUtility.SaveAsPrefabAssetAndConnect(tmp, prefabPath, InteractionMode.UserAction);

                    Object.DestroyImmediate(tmp);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
