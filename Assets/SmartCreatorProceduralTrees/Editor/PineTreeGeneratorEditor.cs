using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SmartCreator.ProceduralTrees.Editor
{
    [CustomEditor(typeof(PineTreeGenerator))]
    public class PineTreeGeneratorEditor : UnityEditor.Editor
    {
        PineTreeGenerator gen;

        void OnEnable()
        {
            gen = (PineTreeGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Regenerate", GUILayout.Height(28)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Regenerate Pine");
                    gen.Generate();
                    if (!Application.isPlaying)
                        EditorSceneManager.MarkSceneDirty(gen.gameObject.scene);
                }
                if (GUILayout.Button("Save Prefab", GUILayout.Height(28)))
                {
                    string path = EditorUtility.SaveFilePanelInProject(
                        "Save Pine Prefab", gen.name + ".prefab", "prefab", "Choose location to save prefab"
                    );
                    if (!string.IsNullOrEmpty(path))
                    {
                        var temp = Object.Instantiate(gen.gameObject);
                        temp.name = gen.name + "_Prefab";
                        temp.GetComponent<PineTreeGenerator>().Generate();
                        PrefabUtility.SaveAsPrefabAssetAndConnect(temp, path, InteractionMode.UserAction);
                        Object.DestroyImmediate(temp);
                    }
                }
                if (GUILayout.Button("Bake For Terrain", GUILayout.Height(28)))
                {
                    var tmp = Object.Instantiate(gen.gameObject, gen.transform.position, gen.transform.rotation);
                    tmp.name = gen.name + "_Terrain";
                    tmp.GetComponent<PineTreeGenerator>().Generate();

                    // Gather and combine all meshes to single mesh (for Unity terrain trees)
                    var comb = new System.Collections.Generic.List<CombineInstance>();
                    foreach (var mf in tmp.GetComponentsInChildren<MeshFilter>())
                        if (mf.sharedMesh != null)
                            comb.Add(new CombineInstance {
                                mesh = mf.sharedMesh,
                                transform = mf.transform.localToWorldMatrix
                            });

                    var merged = new Mesh {
                        name = tmp.name + "_Merged",
                        indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
                    };
                    merged.CombineMeshes(comb.ToArray(), true, true);
                    merged.RecalculateBounds();
                    tmp.GetComponent<MeshFilter>().sharedMesh = merged;

                    var mr = tmp.GetComponent<MeshRenderer>();
                    // If you want, you can improve material assignment here
                    // (for now, just assign main bark/leaf mats)
                    mr.sharedMaterials = new[] { gen.barkMaterial, gen.leafMaterial };

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

            EditorGUILayout.HelpBox("Use the buttons above to quickly regenerate, save prefab, or bake a terrain tree mesh. Baking creates a single combined mesh optimized for Unity Terrain placement.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
