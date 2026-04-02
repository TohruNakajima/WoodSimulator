using UnityEngine;

namespace SmartCreator.ProceduralTrees
{
    [CreateAssetMenu(fileName = "TreeProfile", menuName = "ProceduralTrees/Tree Profile")]
    public class TreeProfile : ScriptableObject
    {
        public enum TreeType
        {
            Generic,
            Palm,
            Pine,
            Birch,
            Willow,
            Olive,
            Fruit,
            Stylized
        }

        [Header("Tree Type")]
        public TreeType treeType = TreeType.Generic;

        [Header("Trunk Settings")]
        public float trunkHeight = 5f;
        public float trunkRadius = 0.6f;
        public float curvature = 1f;

        [Header("Branching Settings")]
        [Range(1, 10)] public int recursionDepth = 4;
        [Range(1, 50)] public int primaryBranches = 6;
        [Range(5f, 90f)] public float maxBranchAngle = 45f;

        [Header("Radius Scaling")]
        public bool useRadiusSliders = false;
        [Range(0f, 1f)] public float radiusStart = 1f;
        [Range(0f, 1f)] public float radiusEnd = 0.1f;
        public AnimationCurve radiusByDepth = AnimationCurve.Linear(0f, 1f, 1f, 0.1f);

        [Header("Leaf Settings")]
        [Range(0.01f, 1f)] public float leafScale = 0.2f;
        [Range(0.01f, 1f)] public float leafThicknessMin = 0.05f;
        [Range(0.01f, 1f)] public float leafThicknessMax = 0.15f;
        [Range(1, 100)] public int leavesPerBranch = 5;
        [Range(0, 5000)] public int globalLeafCount = 500;
        public GameObject leafPrefab;
        public Material leafMaterial;

        [Header("Fruit Settings")]
        [Range(0, 100)] public int fruitCount = 20;
        [Range(0.1f, 5f)] public float fruitScale = 0.3f;
        public int minFruitDepth = 2;
        public Material fruitMaterial;

        [Header("Palm Settings")]
        public bool enablePalmSettings = false;
        [Range(-5f, 5f)] public float palmLeafYOffset = -0.5f;
        [Range(0f, 2f)] public float palmLeafSpread = 1f;
        public GameObject coconutPrefab;

        [Header("Coconut Settings")]
        [Range(0, 20)] public int coconutCount = 5;
        public Vector2 coconutHeightRange = new Vector2(0.8f, 0.95f);
        [Range(0.1f, 5f)] public float coconutScale = 1f;

        [Header("Materials")]
        public Material barkMaterial;
    }
}




