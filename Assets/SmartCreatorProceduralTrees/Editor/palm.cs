// Assets/SmartCreatorProceduralTrees/Editor/PalmTreeGeneratorEditor.cs
using UnityEditor;
using UnityEngine;
using SmartCreator.ProceduralTrees;

namespace SmartCreator.ProceduralTrees.Editor
{
    [CustomEditor(typeof(PalmTreeGenerator))]
    public class PalmTreeGeneratorEditor : UnityEditor.Editor
    {
        SerializedProperty
            p_trunkHeight, p_trunkRadius, p_trunkSegments, p_trunkHeightSegments,
            p_curveDir, p_curveStrength,
            p_extraCarve, p_leafYOffset,
            p_trunkMat,
            p_frondPrefab, p_frondCount, p_frondLength, p_frondDroop,
            p_cocoPrefab, p_cocoCount, p_cocoOffset, p_cocoScale;

        void OnEnable()
        {
            var s = serializedObject;
            p_trunkHeight        = s.FindProperty("trunkHeight");
            p_trunkRadius        = s.FindProperty("trunkRadius");
            p_trunkSegments      = s.FindProperty("trunkSegments");
            p_trunkHeightSegments= s.FindProperty("trunkHeightSegments");
            p_curveDir           = s.FindProperty("trunkCurveDirection");
            p_curveStrength      = s.FindProperty("trunkCurveStrength");
            p_extraCarve         = s.FindProperty("extraTrunkCarve");
            p_leafYOffset        = s.FindProperty("palmLeafYOffset");
            p_trunkMat           = s.FindProperty("trunkMaterial");
            p_frondPrefab        = s.FindProperty("frondPrefab");
            p_frondCount         = s.FindProperty("frondCount");
            p_frondLength        = s.FindProperty("frondLength");
            p_frondDroop         = s.FindProperty("frondDroopAngle");
            p_cocoPrefab         = s.FindProperty("coconutPrefab");
            p_cocoCount          = s.FindProperty("coconutCount");
            p_cocoOffset         = s.FindProperty("coconutRadiusOffset");
            p_cocoScale          = s.FindProperty("coconutScale");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Trunk Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p_trunkHeight);
            EditorGUILayout.PropertyField(p_trunkRadius);
            EditorGUILayout.PropertyField(p_trunkSegments);
            EditorGUILayout.PropertyField(p_trunkHeightSegments, new GUIContent("Height Segments"));
            EditorGUILayout.PropertyField(p_curveDir, new GUIContent("Curve Direction"));
            EditorGUILayout.PropertyField(p_curveStrength, new GUIContent("Curve Strength"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Carve & Foliage Offset", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p_extraCarve, new GUIContent("Extra Trunk Carve"));
            EditorGUILayout.PropertyField(p_leafYOffset, new GUIContent("Leaf Y Offset"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p_trunkMat, new GUIContent("Trunk Material"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Frond Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p_frondPrefab);
            EditorGUILayout.PropertyField(p_frondCount);
            EditorGUILayout.PropertyField(p_frondLength);
            EditorGUILayout.PropertyField(p_frondDroop, new GUIContent("Droop Angle"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Coconut Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p_cocoPrefab);
            EditorGUILayout.PropertyField(p_cocoCount);
            EditorGUILayout.PropertyField(p_cocoOffset, new GUIContent("Radius Offset"));
            EditorGUILayout.PropertyField(p_cocoScale);

            serializedObject.ApplyModifiedProperties();

            // live rebuild on any property change
            if (GUI.changed)
            {
                foreach (PalmTreeGenerator gen in targets)
                    gen.BuildPalm();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
            var palm = (PalmTreeGenerator)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Palm"))       palm.BuildPalm();
                if (GUILayout.Button("Save Palm Prefab")) palm.SavePalmPrefab();
                if (GUILayout.Button("Bake For Terrain")) palm.BakeForTerrain();
            }
        }
    }
}
