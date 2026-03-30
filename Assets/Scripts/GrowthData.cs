using System;
using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// 森林成長データ（1年齢分）
    /// </summary>
    [Serializable]
    public class GrowthData
    {
        [Header("Basic Info")]
        [Tooltip("林齢（年）")]
        public int age;

        [Header("Tree Parameters")]
        [Tooltip("樹高（m）")]
        public float height;

        [Tooltip("直径（cm）")]
        public float diameter;

        [Tooltip("本数（本/ha）")]
        public int treeCount;

        [Header("Display Settings")]
        [Tooltip("使用するモデル名（Age10_SmallTree等）")]
        public string modelName=> DetermineModelName(age);

        public GrowthData(int age, float height, float diameter, int treeCount)
        {
            this.age = age;
            this.height = height;
            this.diameter = diameter;
            this.treeCount = treeCount;
            //this.modelName = DetermineModelName(age);
        }

        /// <summary>
        /// 林齢に応じて適切なモデル名を決定
        /// </summary>
        private string DetermineModelName(int age)
        {
            if (age <= 17) return "Age10_SmallTree";
            if (age <= 32) return "Age25_YoungTree";
            if (age <= 47) return "Age40_MediumTree";
            if (age <= 67) return "Age55_MatureTree";
            if (age <= 87) return "Age75_OldTree";
            return "Age100_AncientTree";
        }

        public override string ToString()
        {
            return $"Age:{age} H:{height}m D:{diameter}cm Count:{treeCount} Model:{modelName}";
        }
    }
}
