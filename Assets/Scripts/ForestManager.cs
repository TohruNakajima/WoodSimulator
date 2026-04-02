using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// 森林全体の年齢管理
    /// 静的イベントで全SingleTreeGrowthに年齢・本数変更を通知
    /// </summary>
    public class ForestManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("成長データベース")]
        public GrowthDatabase growthDatabase;

        /// <summary>
        /// 年齢・本数変更イベント
        /// </summary>
        public static event System.Action<int, int> OnAgeChanged;

        private int currentAge = 10;

        /// <summary>
        /// 年齢設定（UIから呼び出される）
        /// </summary>
        public void SetAge(int age)
        {
            if (age == currentAge)
                return;

            currentAge = age;

            // 成長データ取得
            GrowthData data = growthDatabase.GetDataByAge(age);
            if (data == null)
            {
                data = growthDatabase.GetNearestDataByAge(age);
            }

            if (data == null)
            {
                Debug.LogError($"ForestManager: No data found for age {age}");
                return;
            }

            // イベント発火: 全SingleTreeGrowthに通知
            OnAgeChanged?.Invoke(age, data.treeCount);
            Debug.Log($"[ForestManager] OnAgeChanged発火: Age={age}, TreeCount={data.treeCount}");
        }

        /// <summary>
        /// 現在の年齢取得
        /// </summary>
        public int GetCurrentAge()
        {
            return currentAge;
        }
    }
}
