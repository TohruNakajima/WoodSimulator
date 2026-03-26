using UnityEngine;
using UnityEngine.UI;

namespace WoodSimulator
{
    /// <summary>
    /// 成長タイムライン制御（UI Controller）
    /// Sliderステップ制御、更新頻度制限、自動再生モード
    /// </summary>
    public class GrowthTimeline : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("成長データベース")]
        public GrowthDatabase growthDatabase;

        [Tooltip("森林生成管理")]
        public ForestGenerator forestGenerator;

        [Header("UI Components")]
        [Tooltip("林齢スライダー")]
        public Slider ageSlider;

        [Tooltip("林齢表示テキスト")]
        public Text ageText;

        [Tooltip("樹高表示テキスト")]
        public Text heightText;

        [Tooltip("直径表示テキスト")]
        public Text diameterText;

        [Tooltip("本数表示テキスト")]
        public Text treeCountText;

        [Tooltip("自動再生ボタン")]
        public Button playButton;

        [Header("Playback Settings")]
        [Tooltip("自動再生速度（秒/ステップ）")]
        public float playbackSpeed = 1.0f;

        [Tooltip("更新頻度制限（秒）")]
        public float updateInterval = 1.0f;

        private int currentAge = 10;
        private float lastUpdateTime = 0f;
        private bool isPlaying = false;
        private float playbackTimer = 0f;

        private void Start()
        {
            if (growthDatabase == null)
            {
                Debug.LogError("GrowthTimeline: growthDatabase is not assigned.");
                return;
            }

            if (forestGenerator == null)
            {
                Debug.LogError("GrowthTimeline: forestGenerator is not assigned.");
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
            UpdateForest(currentAge);
        }

        private void Update()
        {
            // 自動再生モード
            if (isPlaying)
            {
                playbackTimer += Time.deltaTime;
                if (playbackTimer >= playbackSpeed)
                {
                    playbackTimer = 0f;
                    int nextAge = currentAge + 5;
                    var (min, max) = growthDatabase.GetAgeRange();
                    if (nextAge > max)
                    {
                        nextAge = min; // ループ
                    }
                    SetAge(nextAge);
                }
            }
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

            // 更新頻度制限
            if (Time.time - lastUpdateTime < updateInterval && age != currentAge)
            {
                return;
            }

            SetAge(age);
        }

        /// <summary>
        /// 林齢設定
        /// </summary>
        public void SetAge(int age)
        {
            currentAge = age;

            if (ageSlider != null && Mathf.RoundToInt(ageSlider.value) != age)
            {
                ageSlider.value = age;
            }

            UpdateForest(age);
            lastUpdateTime = Time.time;
        }

        /// <summary>
        /// 森林更新
        /// </summary>
        private void UpdateForest(int age)
        {
            GrowthData data = growthDatabase.GetDataByAge(age);
            if (data == null)
            {
                data = growthDatabase.GetNearestDataByAge(age);
            }

            if (data == null)
            {
                Debug.LogWarning($"GrowthTimeline: No data found for age {age}.");
                return;
            }

            // UI更新
            UpdateUI(data);

            // 森林生成（alpha調整で本数制御）
            forestGenerator.GenerateForest(data);
        }

        /// <summary>
        /// UI表示更新
        /// </summary>
        private void UpdateUI(GrowthData data)
        {
            if (ageText != null)
                ageText.text = $"林齢: {data.age}年";

            if (heightText != null)
                heightText.text = $"樹高: {data.height:F1}m";

            if (diameterText != null)
                diameterText.text = $"直径: {data.diameter:F1}cm";

            // 本数は逆転表示（データは3000→744と減少するが、表示は744→3000と増加）
            int invertedTreeCount = 3000 - data.treeCount + 744;
            if (treeCountText != null)
                treeCountText.text = $"本数: {invertedTreeCount}本/ha";
        }

        /// <summary>
        /// 自動再生トグル
        /// </summary>
        public void TogglePlayback()
        {
            isPlaying = !isPlaying;
            playbackTimer = 0f;

            // ボタンの色を変更
            if (playButton != null)
            {
                UnityEngine.UI.Image buttonImage = playButton.GetComponent<UnityEngine.UI.Image>();
                if (buttonImage != null)
                {
                    if (isPlaying)
                    {
                        // 再生中は赤色
                        buttonImage.color = Color.red;
                    }
                    else
                    {
                        // 停止中は白色
                        buttonImage.color = Color.white;
                    }
                }
            }

            Debug.Log($"[GrowthTimeline] TogglePlayback: {(isPlaying ? "再生開始" : "再生停止")} (現在の林齢: {currentAge}年)");
        }

        /// <summary>
        /// 次のステップ
        /// </summary>
        public void NextStep()
        {
            int nextAge = currentAge + 5;
            var (min, max) = growthDatabase.GetAgeRange();
            if (nextAge > max)
                nextAge = max;
            Debug.Log($"[GrowthTimeline] NextStep: {currentAge}年 → {nextAge}年");
            SetAge(nextAge);
        }

        /// <summary>
        /// 前のステップ
        /// </summary>
        public void PreviousStep()
        {
            int prevAge = currentAge - 5;
            var (min, max) = growthDatabase.GetAgeRange();
            if (prevAge < min)
                prevAge = min;
            Debug.Log($"[GrowthTimeline] PreviousStep: {currentAge}年 → {prevAge}年");
            SetAge(prevAge);
        }

        /// <summary>
        /// 最初にリセット
        /// </summary>
        public void ResetToStart()
        {
            var (min, max) = growthDatabase.GetAgeRange();
            Debug.Log($"[GrowthTimeline] ResetToStart: {currentAge}年 → {min}年（最初にリセット）");
            SetAge(min);
        }
    }
}
