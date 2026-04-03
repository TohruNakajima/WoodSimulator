using UnityEngine;
using SmartCreator.ProceduralTrees;

namespace WoodSimulator
{
    /// <summary>
    /// プロシージャル木の個体データ。
    /// MonoBehaviourではなく、ProceduralForestManagerが一括管理する純粋データクラス。
    /// </summary>
    [System.Serializable]
    public class ProceduralTreeInstance
    {
        public int seed;
        public float growthFactor;
        public Vector3 position;
        public float rotationY;
        public GameObject gameObject;
        public PineTreeGenerator generator;
        public bool isThinned;
        public int thinnedAtAge;

        public ProceduralTreeInstance(int seed, Vector3 position)
        {
            this.seed = seed;
            this.position = position;

            // seedベースで一貫したgrowthFactorを生成（0.7～1.3）
            var rng = new System.Random(seed);
            growthFactor = 0.7f + (float)rng.NextDouble() * 0.6f;
            rotationY = (float)rng.NextDouble() * 360f;

            isThinned = false;
            thinnedAtAge = -1;
        }
    }
}
