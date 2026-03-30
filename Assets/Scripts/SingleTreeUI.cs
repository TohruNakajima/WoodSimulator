using UnityEngine;
using UnityEngine.UI;

namespace WoodSimulator
{
    /// <summary>
    /// 1本の杉成長シミュレーションUI制御
    /// </summary>
    public class SingleTreeUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("成長データベース")]
        public GrowthDatabase growthDatabase;

        [Tooltip("SingleTreeGrowth")]
        public SingleTreeGrowth singleTreeGrowth;

        [Header("UI Components")]
        [Tooltip("林齢スライダー")]
        public Slider ageSlider;

        [Tooltip("林齢表示テキスト")]
        public Text ageText;

        [Tooltip("樹高表示テキスト")]
        public Text heightText;

        [Tooltip("直径表示テキスト")]
        public Text diameterText;

        private int currentAge = 10;

        private void Start()
        {
            if (growthDatabase == null)
            {
                Debug.LogError("SingleTreeUI: growthDatabase is not assigned.");
                return;
            }

            if (singleTreeGrowth == null)
            {
                Debug.LogError("SingleTreeUI: singleTreeGrowth is not assigned.");
                return;
            }

            // Slider設定
            if (ageSlider != null)
            {
                var (min, max) = growthDatabase.GetAgeRange();
                ageSlider.minValue = min;
                ageSlider.maxValue = max;
                ageSlider.wholeNumbers = true;
                ageSlider.value = min;
                ageSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            // 初期表示
            currentAge = 10;
            UpdateUI(currentAge);
            singleTreeGrowth.SetAge(currentAge);
        }

        /// <summary>
        /// スライダー値変更時
        /// </summary>
        private void OnSliderValueChanged(float value)
        {
            int age = Mathf.RoundToInt(value);

            // 5年刻みにステップ制御
            age = Mathf.RoundToInt(age / 5f) * 5;
            age = Mathf.Clamp(age, 10, 100);

            if (age == currentAge)
                return;

            currentAge = age;
            UpdateUI(age);
            singleTreeGrowth.SetAge(age);
        }

        /// <summary>
        /// UI表示更新
        /// </summary>
        private void UpdateUI(int age)
        {
            GrowthData data = growthDatabase.GetDataByAge(age);
            if (data == null)
            {
                data = growthDatabase.GetNearestDataByAge(age);
            }

            if (data == null)
            {
                Debug.LogWarning($"SingleTreeUI: No data found for age {age}");
                return;
            }

            if (ageText != null)
                ageText.text = $"林齢: {data.age}年";

            if (heightText != null)
                heightText.text = $"樹高: {data.height:F1}m";

            if (diameterText != null)
                diameterText.text = $"直径: {data.diameter:F1}cm";
        }
    }
}
