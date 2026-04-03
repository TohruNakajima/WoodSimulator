using UnityEngine;
using SmartCreator.ProceduralTrees;

namespace WoodSimulator
{
    /// <summary>
    /// GrowthDataとgrowthFactorからPineTreeGeneratorのパラメータを算出する静的ユーティリティ。
    /// </summary>
    public static class TreeParameterMapper
    {
        // GrowthDatabaseの値域（10年～100年）
        private const float MinHeight = 5.4f;
        private const float MaxHeight = 27.2f;
        private const float MinDiameter = 6.3f;
        private const float MaxDiameter = 28.2f;

        /// <summary>
        /// GrowthDataとgrowthFactorに基づきPineTreeGeneratorのパラメータを設定し、Generate()を呼ぶ。
        /// </summary>
        public static void ApplyToGenerator(PineTreeGenerator gen, GrowthData data, float growthFactor, int seed)
        {
            float hNorm = Mathf.InverseLerp(MinHeight, MaxHeight, data.height);
            float dNorm = Mathf.InverseLerp(MinDiameter, MaxDiameter, data.diameter);

            // サイズ系パラメータにgrowthFactorを乗算
            gen.trunkHeight = Mathf.Lerp(5f, 25f, hNorm) * growthFactor;
            gen.trunkRadius = Mathf.Lerp(0.05f, 0.22f, dNorm) * growthFactor;
            gen.trunkTipRadius = Mathf.Lerp(0.02f, 0.06f, dNorm) * growthFactor;
            gen.baseBranchLength = Mathf.Lerp(1.5f, 4.5f, hNorm) * growthFactor;
            gen.tipBranchLength = Mathf.Lerp(0.3f, 1.5f, hNorm) * growthFactor;
            gen.branchThickness = Mathf.Lerp(0.03f, 0.10f, dNorm) * growthFactor;
            gen.leafCardLength = Mathf.Lerp(0.3f, 1.0f, hNorm) * growthFactor;
            gen.leafCardWidth = Mathf.Lerp(0.10f, 0.30f, hNorm) * growthFactor;

            // 構造系パラメータ（growthFactor不要、整数丸め）
            gen.whorlCount = Mathf.RoundToInt(Mathf.Lerp(10f, 28f, hNorm));
            gen.branchesPerWhorl = Mathf.RoundToInt(Mathf.Lerp(4f, 10f, hNorm));
            gen.baseLeavesPerBranch = Mathf.RoundToInt(Mathf.Lerp(20f, 48f, hNorm));

            // 角度系（成長に応じて変化、growthFactor不要）
            gen.branchDownwardAngle = Mathf.Lerp(30f, 55f, hNorm);
            gen.branchDownwardCurve = Mathf.Lerp(0.8f, 2.5f, hNorm);
            gen.branchUpCurve = Mathf.Lerp(0.15f, 0.35f, hNorm);

            // 固定パラメータ
            gen.trunkTaper = Mathf.Lerp(1.1f, 1.5f, hNorm);
            gen.trunkNoiseStrength = 0.06f;
            gen.trunkNoiseFrequency = 4f;
            gen.branchRandomTilt = Mathf.Lerp(6f, 10f, hNorm);
            gen.branchStartHeight = Mathf.Lerp(0.05f, 0.15f, hNorm);
            gen.branchEndHeight = Mathf.Lerp(0.90f, 0.98f, hNorm);
            gen.leafBend = 14f;

            // 個体固有seed
            gen.seed = seed;

            gen.Generate();
        }
    }
}
