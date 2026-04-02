// Assets/SmartCreatorProceduralTrees/Core/InstancedLeafRenderer.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SmartCreator.ProceduralTrees
{
    [RequireComponent(typeof(MeshRenderer))]
    public class InstancedLeafRenderer : MonoBehaviour
    {
        private Mesh _mesh;
        private Material _mat;
        private Matrix4x4[] _mats;

        /// <summary>
        /// Called by TreeGeneratorOptimized.BuildLeaves() when GPU instancing is on.
        /// </summary>
        public void Setup(Mesh mesh, Material mat, List<Matrix4x4> matrices)
        {
            _mesh = mesh;
            _mat  = mat;
            _mats = matrices.ToArray();
            GetComponent<MeshRenderer>().enabled = false;
        }

        private void OnRenderObject()
        {
            if (_mesh == null || _mat == null || _mats == null || _mats.Length == 0)
                return;

            const int batchSize = 1023;
            for (int i = 0; i < _mats.Length; i += batchSize)
            {
                int count = Math.Min(batchSize, _mats.Length - i);

                // Copy the slice of matrices for this batch
                var slice = new Matrix4x4[count];
                Array.Copy(_mats, i, slice, 0, count);

                Graphics.DrawMeshInstanced(
                    _mesh,
                    0,
                    _mat,
                    slice,
                    count,
                    null,
                    ShadowCastingMode.On,
                    true,
                    gameObject.layer
                );
            }
        }
    }
}
