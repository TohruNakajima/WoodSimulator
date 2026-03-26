using System.Collections.Generic;
using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// 森林成長データベース（ScriptableObject）
    /// 19段階の成長データを管理
    /// </summary>
    [CreateAssetMenu(fileName = "GrowthDatabase", menuName = "WoodSimulator/Growth Database")]
    public class GrowthDatabase : ScriptableObject
    {
        [Header("Growth Stages")]
        [Tooltip("林齢10～100年の成長データ（19段階）")]
        public List<GrowthData> growthStages = new List<GrowthData>();

        /// <summary>
        /// 林齢からGrowthDataを取得
        /// </summary>
        public GrowthData GetDataByAge(int age)
        {
            return growthStages.Find(data => data.age == age);
        }

        /// <summary>
        /// 最も近い林齢のGrowthDataを取得（補間用）
        /// </summary>
        public GrowthData GetNearestDataByAge(int age)
        {
            if (growthStages.Count == 0)
                return null;

            GrowthData nearest = growthStages[0];
            int minDiff = Mathf.Abs(growthStages[0].age - age);

            foreach (var data in growthStages)
            {
                int diff = Mathf.Abs(data.age - age);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    nearest = data;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 2つのデータ間で補間（スムーズな成長表現用）
        /// </summary>
        public GrowthData Lerp(GrowthData from, GrowthData to, float t)
        {
            return new GrowthData(
                Mathf.RoundToInt(Mathf.Lerp(from.age, to.age, t)),
                Mathf.Lerp(from.height, to.height, t),
                Mathf.Lerp(from.diameter, to.diameter, t),
                Mathf.RoundToInt(Mathf.Lerp(from.treeCount, to.treeCount, t))
            );
        }

        /// <summary>
        /// 林齢範囲を取得
        /// </summary>
        public (int min, int max) GetAgeRange()
        {
            if (growthStages.Count == 0)
                return (0, 0);

            int min = growthStages[0].age;
            int max = growthStages[0].age;

            foreach (var data in growthStages)
            {
                if (data.age < min) min = data.age;
                if (data.age > max) max = data.age;
            }

            return (min, max);
        }

        /// <summary>
        /// データ数を取得
        /// </summary>
        public int Count => growthStages.Count;

        /// <summary>
        /// データをソート（林齢順）
        /// </summary>
        public void SortByAge()
        {
            growthStages.Sort((a, b) => a.age.CompareTo(b.age));
        }

#if UNITY_EDITOR
        /// <summary>
        /// JSONデータからインポート
        /// </summary>
        public void ImportFromJSON(string jsonText)
        {
            try
            {
                // JSON配列をパース
                var wrapper = JsonUtility.FromJson<GrowthDataArrayWrapper>("{\"items\":" + jsonText + "}");

                growthStages.Clear();

                foreach (var item in wrapper.items)
                {
                    growthStages.Add(new GrowthData(
                        item.age,
                        item.height,
                        item.diameter,
                        item.count
                    ));
                }

                SortByAge();

                UnityEditor.EditorUtility.SetDirty(this);

                Debug.Log($"Imported {growthStages.Count} growth stages from JSON");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import JSON: {e.Message}");
            }
        }

        [System.Serializable]
        private class GrowthDataArrayWrapper
        {
            public List<JSONGrowthData> items;
        }

        [System.Serializable]
        private class JSONGrowthData
        {
            public int age;
            public float height;
            public float diameter;
            public int count;
        }
#endif
    }
}
