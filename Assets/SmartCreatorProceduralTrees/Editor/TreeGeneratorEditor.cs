// Assets/SmartCreatorProceduralTrees/Editor/TreeGeneratorEditor.cs
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SmartCreator.ProceduralTrees;
using UObject = UnityEngine.Object;

namespace SmartCreator.ProceduralTrees.Editor
{
    [CustomEditor(typeof(TreeGeneratorOptimized), true)]
    [CanEditMultipleObjects]
    public class SmartTreeGeneratorOptimizedEditor : UnityEditor.Editor
    {
        bool showProfileFold, showLeafShaderFold;
        SerializedProperty profileProp;
        SerializedProperty barkMaterialProp, branchMaterialProp, leafMaterialProp, fruitMaterialProp;
        SerializedProperty fruitCountProp, fruitScaleProp;
        SerializedProperty trunkMeshProp, useSimpleTaperProp, tipRadiusFactorProp, taperExponentProp;
        SerializedProperty childRadiusFactorProp, childLenFactorMinProp, childLenFactorMaxProp, childCountRangeProp, branchLengthMultiplierProp;
        SerializedProperty branchStartMinProp, branchStartMaxProp, branchHeightOffsetProp, droopBranchesProp, balancedPrimaryBranchesProp, branchWeepFactorProp;
        SerializedProperty clusterLeavesAlongParentProp, clusterMinOnBranchProp, clusterMaxOnBranchProp;
        SerializedProperty coconutPrefabProp, coconutCountProp, coconutHeightRangeProp, coconutScaleProp;
        SerializedProperty useFineBranchesProp, droopFineBranchesProp, fineBranchDroopAngleProp;
        SerializedProperty fineBranchRadiusProp, fineBranchLengthProp, fineBranchesPerLeafClusterProp, fineBranchPlacementProp;
        SerializedProperty fineBranchSpacingProp, fineBranchSpacingJitterProp, leafPosMinOnTwigProp, leafPosMaxOnTwigProp;
        SerializedProperty minLeafDepthProp, leafDensityProp, seedProp, liveUpdateProp, surfaceNoiseStrengthProp, surfaceNoiseFrequencyProp;
        SerializedProperty trunkBulgeAmplitudeProp, trunkBulgeFrequencyProp, trunkGrooveDepthProp, trunkGrooveCountProp, trunkGrooveTwistProp;
        SerializedProperty gnarlStrengthProp, gnarlFrequencyProp, trunkLeanCurveProp;

        SerializedObject profileSO;

        void OnEnable()
        {
            profileProp  = serializedObject.FindProperty("profile");
            barkMaterialProp   = serializedObject.FindProperty("barkMaterial");
            branchMaterialProp = serializedObject.FindProperty("branchMaterial");
            leafMaterialProp   = serializedObject.FindProperty("leafMaterial");
            fruitMaterialProp  = serializedObject.FindProperty("fruitMaterial");
            fruitCountProp     = serializedObject.FindProperty("fruitCount");
            fruitScaleProp     = serializedObject.FindProperty("fruitScale");
            trunkMeshProp       = serializedObject.FindProperty("trunkMesh");
            useSimpleTaperProp  = serializedObject.FindProperty("useSimpleTaper");
            tipRadiusFactorProp = serializedObject.FindProperty("tipRadiusFactor");
            taperExponentProp   = serializedObject.FindProperty("taperExponent");
            childRadiusFactorProp      = serializedObject.FindProperty("childRadiusFactor");
            childLenFactorMinProp      = serializedObject.FindProperty("childLenFactorMin");
            childLenFactorMaxProp      = serializedObject.FindProperty("childLenFactorMax");
            childCountRangeProp        = serializedObject.FindProperty("childCountRange");
            branchLengthMultiplierProp = serializedObject.FindProperty("branchLengthMultiplier");
            branchStartMinProp           = serializedObject.FindProperty("branchStartMin");
            branchStartMaxProp           = serializedObject.FindProperty("branchStartMax");
            branchHeightOffsetProp       = serializedObject.FindProperty("branchHeightOffset");
            droopBranchesProp            = serializedObject.FindProperty("droopBranches");
            balancedPrimaryBranchesProp  = serializedObject.FindProperty("balancedPrimaryBranches");
            branchWeepFactorProp         = serializedObject.FindProperty("branchWeepFactor");
            clusterLeavesAlongParentProp = serializedObject.FindProperty("clusterLeavesAlongParent");
            clusterMinOnBranchProp       = serializedObject.FindProperty("clusterMinOnBranch");
            clusterMaxOnBranchProp       = serializedObject.FindProperty("clusterMaxOnBranch");
            coconutPrefabProp      = serializedObject.FindProperty("coconutPrefab");
            coconutCountProp       = serializedObject.FindProperty("coconutCount");
            coconutHeightRangeProp = serializedObject.FindProperty("coconutHeightRange");
            coconutScaleProp       = serializedObject.FindProperty("coconutScale");
            useFineBranchesProp            = serializedObject.FindProperty("useFineBranches");
            droopFineBranchesProp          = serializedObject.FindProperty("droopFineBranches");
            fineBranchDroopAngleProp       = serializedObject.FindProperty("fineBranchDroopAngle");
            fineBranchRadiusProp           = serializedObject.FindProperty("fineBranchRadius");
            fineBranchLengthProp           = serializedObject.FindProperty("fineBranchLength");
            fineBranchesPerLeafClusterProp = serializedObject.FindProperty("fineBranchesPerLeafCluster");
            fineBranchPlacementProp        = serializedObject.FindProperty("fineBranchPlacement");
            fineBranchSpacingProp          = serializedObject.FindProperty("fineBranchSpacing");
            fineBranchSpacingJitterProp    = serializedObject.FindProperty("fineBranchSpacingJitter");
            leafPosMinOnTwigProp           = serializedObject.FindProperty("leafPosMinOnTwig");
            leafPosMaxOnTwigProp           = serializedObject.FindProperty("leafPosMaxOnTwig");
            minLeafDepthProp          = serializedObject.FindProperty("minLeafDepth");
            leafDensityProp           = serializedObject.FindProperty("leafDensity");
            seedProp                  = serializedObject.FindProperty("seed");
            liveUpdateProp            = serializedObject.FindProperty("liveUpdate");
            surfaceNoiseStrengthProp  = serializedObject.FindProperty("surfaceNoiseStrength");
            surfaceNoiseFrequencyProp = serializedObject.FindProperty("surfaceNoiseFrequency");
            trunkBulgeAmplitudeProp = serializedObject.FindProperty("trunkBulgeAmplitude");
            trunkBulgeFrequencyProp = serializedObject.FindProperty("trunkBulgeFrequency");
            trunkGrooveDepthProp    = serializedObject.FindProperty("trunkGrooveDepth");
            trunkGrooveCountProp    = serializedObject.FindProperty("trunkGrooveCount");
            trunkGrooveTwistProp    = serializedObject.FindProperty("trunkGrooveTwist");
            gnarlStrengthProp       = serializedObject.FindProperty("gnarlStrength");
            gnarlFrequencyProp      = serializedObject.FindProperty("gnarlFrequency");
            trunkLeanCurveProp      = serializedObject.FindProperty("trunkLeanCurve");
            // Reset trunkLeanCurve on enable
            if (trunkLeanCurveProp != null && trunkLeanCurveProp.propertyType == SerializedPropertyType.Float)
            {
                trunkLeanCurveProp.floatValue = 0f;
                serializedObject.ApplyModifiedProperties();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(profileProp, new GUIContent("Tree Profile"));
            if (profileProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a TreeProfile asset to enable tree generation.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            DrawProfileFoldout();
            EditorGUILayout.Space();
            DrawMaterialsAndFruit();
            DrawLeafShaderSettings();
            DrawGeneratorSettings();
            DrawBakeExport();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawProfileFoldout()
        {
            showProfileFold = EditorGUILayout.Foldout(showProfileFold, "Tree Profile Settings", true);
            if (!showProfileFold) return;
            if (profileSO == null || profileSO.targetObject != profileProp.objectReferenceValue)
            {
                profileSO?.Dispose();
                profileSO = new SerializedObject(profileProp.objectReferenceValue);
            }
            profileSO.Update();
            var it = profileSO.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                if (it.name != "m_Script")
                    EditorGUILayout.PropertyField(it, true);
                enter = false;
            }
            profileSO.ApplyModifiedProperties();
        }

        void DrawMaterialsAndFruit()
        {
            EditorGUILayout.LabelField("Materials & Fruit Settings", EditorStyles.boldLabel);
            DrawProperty(barkMaterialProp);
            DrawProperty(branchMaterialProp);
            DrawProperty(leafMaterialProp);
            DrawProperty(fruitMaterialProp);
            DrawSlider(fruitCountProp, 0, 100);
            DrawSlider(fruitScaleProp, 0.1f, 5f);
        }

        void DrawLeafShaderSettings()
        {
            var leafMat = leafMaterialProp.objectReferenceValue as Material;
            if (leafMat == null) return;
            showLeafShaderFold = EditorGUILayout.Foldout(showLeafShaderFold, "Leaf Shader Settings", true);
            if (!showLeafShaderFold) return;
            var me    = (MaterialEditor)CreateEditor((UObject)leafMat);
            var props = MaterialEditor.GetMaterialProperties(new[] { (UObject)leafMat });
            string[,] pairs =
            {
                { "_OcclusionStrength", "Occlusion Strength" },
                { "_WindAmplitude",     "Wind Amplitude"     },
                { "_WindFrequency",     "Wind Frequency"     },
                { "_WindSpeed",         "Wind Speed"         },
                { "_WindRandomize",     "Wind Randomize"     },
                { "_SubsurfacePower",   "Subsurface Power"   },
                { "_LightStrength",     "Light Strength"     },
                { "_LeafVisMult",       "Leaf Visibility"    },
                { "_LeafDensity",       "Leaf Density [0–1]" },
                { "_ExtraLeafCopies",   "Extra Leaf Copies"  },
                { "_LeafSpread",        "Copy Spread (m)"    },
                { "_CopyYawDeg",        "Random Yaw (deg)"   }
            };
            for (int i = 0; i < pairs.GetLength(0); i++)
                DrawShaderProp(me, props, pairs[i, 0], pairs[i, 1]);
            EditorGUILayout.Space();
            DestroyImmediate(me);
        }

        void DrawGeneratorSettings()
        {
            EditorGUILayout.LabelField("Tree Generator Settings", EditorStyles.boldLabel);
            DrawProperty(trunkMeshProp);
            DrawProperty(useSimpleTaperProp);
            DrawSlider(tipRadiusFactorProp, 0f, 1f);
            DrawSlider(taperExponentProp, 0.1f, 5f);
            DrawProperty(childRadiusFactorProp);
            DrawProperty(childLenFactorMinProp);
            DrawProperty(childLenFactorMaxProp);
            DrawProperty(childCountRangeProp);
            DrawProperty(branchLengthMultiplierProp);
            DrawProperty(branchStartMinProp);
            DrawProperty(branchStartMaxProp);
            DrawProperty(branchHeightOffsetProp);
            DrawProperty(droopBranchesProp);
            DrawProperty(balancedPrimaryBranchesProp);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Weeping / Droop", EditorStyles.boldLabel);
            DrawSlider(branchWeepFactorProp, 0f, 1f);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Leaf Clustering Along Parent", EditorStyles.boldLabel);
            DrawProperty(clusterLeavesAlongParentProp);
            if (clusterLeavesAlongParentProp.boolValue)
            {
                Vector2 range = new Vector2(clusterMinOnBranchProp.floatValue, clusterMaxOnBranchProp.floatValue);
                EditorGUILayout.MinMaxSlider("Cluster Range", ref range.x, ref range.y, 0f, 1f);
                clusterMinOnBranchProp.floatValue = range.x;
                clusterMaxOnBranchProp.floatValue = range.y;
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Coconut Settings", EditorStyles.boldLabel);
            DrawProperty(coconutPrefabProp);
            DrawSlider(coconutCountProp, 0, 20);
            Vector2 cr = coconutHeightRangeProp.vector2Value;
            EditorGUILayout.MinMaxSlider("Coconut Height Range", ref cr.x, ref cr.y, 0f, 1f);
            coconutHeightRangeProp.vector2Value = cr;
            DrawSlider(coconutScaleProp, 0.1f, 5f);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fine Branches & Leaf Attachment", EditorStyles.boldLabel);
            DrawProperty(useFineBranchesProp);
            if (useFineBranchesProp.boolValue)
            {
                DrawProperty(droopFineBranchesProp);
                DrawSlider(fineBranchDroopAngleProp, 0f, 90f);
                DrawSlider(fineBranchRadiusProp, 0.001f, 0.05f);
                DrawSlider(fineBranchLengthProp, 0.1f, 1f);
                DrawProperty(fineBranchesPerLeafClusterProp);
                DrawProperty(fineBranchPlacementProp);
                DrawSlider(fineBranchSpacingProp, 0f, 1f);
                DrawSlider(fineBranchSpacingJitterProp, 0f, 1f);
                DrawSlider(leafPosMinOnTwigProp, 0f, 1f);
                DrawSlider(leafPosMaxOnTwigProp, 0f, 1f);
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Leaves & Generation", EditorStyles.boldLabel);
            DrawProperty(minLeafDepthProp);
            DrawProperty(leafDensityProp);
            DrawProperty(seedProp);
            DrawProperty(liveUpdateProp);
            DrawSlider(surfaceNoiseStrengthProp, 0f, 1f);
            DrawSlider(surfaceNoiseFrequencyProp, 0.1f, 10f);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Trunk Detail", EditorStyles.boldLabel);
            DrawSlider(trunkBulgeAmplitudeProp, 0f, 0.8f);
            DrawSlider(trunkBulgeFrequencyProp, 0.1f, 10f);
            DrawSlider(trunkGrooveDepthProp, 0f, 0.5f);
            DrawProperty(trunkGrooveCountProp);
            DrawSlider(trunkGrooveTwistProp, -3f, 3f);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gnarl / Age Marks", EditorStyles.boldLabel);
            DrawSlider(gnarlStrengthProp, 0f, 1f);
            DrawSlider(gnarlFrequencyProp, 0.1f, 10f);
            if (trunkLeanCurveProp != null && trunkLeanCurveProp.propertyType == SerializedPropertyType.Float)
                DrawSlider(trunkLeanCurveProp, 0f, 2f);
        }

        void DrawBakeExport()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bake & Export", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Regenerate", GUILayout.Height(32)))
                CallOnTargets("Regenerate");
            if (GUILayout.Button("Save Prefab", GUILayout.Height(32)))
                CallOnTargets("SaveAsPrefabToMyTrees");
            if (GUILayout.Button("Bake Terrain", GUILayout.Height(32)))
                CallOnTargets("BakeForTerrain");
            EditorGUILayout.EndHorizontal();
        }

        void DrawProperty(SerializedProperty prop)
        {
            if (prop != null)
                EditorGUILayout.PropertyField(prop, true);
        }
        void DrawSlider(SerializedProperty prop, float min, float max)
        {
            if (prop == null) return;
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:
                    prop.floatValue = EditorGUILayout.Slider(ObjectNames.NicifyVariableName(prop.name), prop.floatValue, min, max);
                    break;
                case SerializedPropertyType.Integer:
                    prop.intValue = EditorGUILayout.IntSlider(ObjectNames.NicifyVariableName(prop.name), prop.intValue, (int)min, (int)max);
                    break;
                default:
                    EditorGUILayout.PropertyField(prop, true);
                    break;
            }
        }
        static void DrawShaderProp(MaterialEditor me, MaterialProperty[] props, string propName, string label)
        {
            var mp = Array.Find(props, m => m.name == propName);
            if (mp != null) me.ShaderProperty(mp, label);
        }
        void CallOnTargets(string methodName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            bool destroyed = false;
            foreach (var obj in targets)
            {
                if (obj == null) continue;
                var mi = obj.GetType().GetMethod(methodName, flags);
                if (mi != null)
                {
                    try { mi.Invoke(obj, null); }
                    catch (Exception ex) { Debug.LogError($"Error invoking {methodName}: {ex.Message}"); }
                }
                else Debug.LogWarning($"Method '{methodName}' not found on {obj.GetType().Name}");
                if (obj == null) destroyed = true;
            }
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            if (destroyed) GUIUtility.ExitGUI();
        }
    }
}
