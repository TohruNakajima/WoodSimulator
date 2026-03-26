using UnityEngine;
using UnityEditor;
using System.IO;

namespace WoodSimulator.Editor
{
    /// <summary>
    /// GrowthDatabaseのカスタムエディタ
    /// JSONインポート機能を提供
    /// </summary>
    [CustomEditor(typeof(GrowthDatabase))]
    public class GrowthDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GrowthDatabase database = (GrowthDatabase)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("JSON Import", EditorStyles.boldLabel);

            if (GUILayout.Button("Import from forest_growth_data.json"))
            {
                string jsonPath = Path.Combine(Application.dataPath, "..", "forest_growth_data.json");

                if (File.Exists(jsonPath))
                {
                    string jsonText = File.ReadAllText(jsonPath);
                    database.ImportFromJSON(jsonText);
                    EditorUtility.DisplayDialog("Success", $"Imported {database.Count} growth stages", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", $"File not found: {jsonPath}", "OK");
                }
            }

            if (GUILayout.Button("Import from Custom JSON..."))
            {
                string path = EditorUtility.OpenFilePanel("Select JSON file", Application.dataPath, "json");

                if (!string.IsNullOrEmpty(path))
                {
                    string jsonText = File.ReadAllText(path);
                    database.ImportFromJSON(jsonText);
                    EditorUtility.DisplayDialog("Success", $"Imported {database.Count} growth stages", "OK");
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Database Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Stages: {database.Count}");

            if (database.Count > 0)
            {
                var (min, max) = database.GetAgeRange();
                EditorGUILayout.LabelField($"Age Range: {min} - {max} years");
            }

            if (GUILayout.Button("Sort by Age"))
            {
                database.SortByAge();
                EditorUtility.SetDirty(database);
                EditorUtility.DisplayDialog("Success", "Sorted by age", "OK");
            }
        }
    }
}
