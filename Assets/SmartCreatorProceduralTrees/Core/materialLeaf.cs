// Assets/SmartCreatorProceduralTrees/Core/LeafMaterialInstancer.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SmartCreator.ProceduralTrees
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-100)]
    public class LeafMaterialInstancer : MonoBehaviour
    {
        const int BATCH = 1023;

        private Mesh mesh;
        private Material mat;
        private Matrix4x4[] mats;

        // Optionally, for automatic detection
        private bool isSetup = false;

        // REQUIRED for TreeGeneratorOptimized!
        public void Setup(Mesh mesh, Material material, List<Matrix4x4> matrices)
        {
            this.mesh = mesh;
            this.mat = material;
            this.mats = matrices != null ? matrices.ToArray() : null;
            isSetup = true;
        }

        void OnEnable()
        {
            isSetup = mesh && mat && mats != null && mats.Length > 0;
#if UNITY_EDITOR
            SceneView.duringSceneGui += EditorRender;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            SceneView.duringSceneGui -= EditorRender;
#endif
        }

        void OnRenderObject()
        {
            if (!Application.isPlaying || !isSetup || !mesh || !mat || !mat.enableInstancing || mats == null || mats.Length == 0)
                return;

            // Draw for all cameras
            for (int i = 0; i < mats.Length; i += BATCH)
            {
                int count = Mathf.Min(BATCH, mats.Length - i);
                Graphics.DrawMeshInstanced(
                    mesh, 0, mat, mats, count, null,
                    ShadowCastingMode.On, true, gameObject.layer, Camera.current
                );
            }
        }

#if UNITY_EDITOR
        void EditorRender(SceneView sv)
        {
            if (Application.isPlaying) return;
            if (!isSetup || !mesh || !mat || !mat.enableInstancing || mats == null || mats.Length == 0)
                return;

            Camera cam = sv.camera;
            for (int i = 0; i < mats.Length; i += BATCH)
            {
                int count = Mathf.Min(BATCH, mats.Length - i);
                Graphics.DrawMeshInstanced(
                    mesh, 0, mat, mats, count, null,
                    ShadowCastingMode.On, true, gameObject.layer, cam
                );
            }
        }
#endif

        // Optional: Clean up after prefab baking
        public void Clear()
        {
            mesh = null;
            mat = null;
            mats = null;
            isSetup = false;
        }
    }
}
