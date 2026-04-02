using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WoodSimulator
{
    /// <summary>
    /// 1本の杉成長シミュレーションUI制御（ボタン操作版）
    /// </summary>
    public class SingleTreeUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("成長データベース")]
        public GrowthDatabase growthDatabase;

        [Tooltip("ForestManager（森林全体の制御）")]
        public ForestManager forestManager;

        [Header("UI Components - Buttons")]
        [Tooltip("自動進行/停止ボタン")]
        public Button autoPlayButton;

        [Tooltip("1段階進めるボタン")]
        public Button nextButton;

        [Tooltip("1段階戻すボタン")]
        public Button prevButton;

        [Tooltip("リセットボタン")]
        public Button resetButton;

        [Header("UI Components - Text")]
        [Tooltip("林齢表示テキスト")]
        public Text ageText;

        [Tooltip("樹高表示テキスト")]
        public Text heightText;

        [Tooltip("直径表示テキスト")]
        public Text diameterText;

        [Tooltip("自動進行ボタンのテキスト")]
        public Text autoPlayButtonText;

        [Header("Auto Play Settings")]
        [Tooltip("自動進行の間隔（秒）")]
        public float autoPlayInterval = 2.0f;

        private int currentAge = 10;
        private bool isAutoPlaying = false;
        private Coroutine autoPlayCoroutine;

        private void Start()
        {
            if (growthDatabase == null)
            {
                Debug.LogError("SingleTreeUI: growthDatabase is not assigned.");
                return;
            }

            if (forestManager == null)
            {
                Debug.LogError("SingleTreeUI: forestManager is not assigned.");
                return;
            }

            // ボタンイベント登録
            if (autoPlayButton != null)
                autoPlayButton.onClick.AddListener(OnAutoPlayButtonClick);

            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextButtonClick);

            if (prevButton != null)
                prevButton.onClick.AddListener(OnPrevButtonClick);

            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetButtonClick);

            // 初期表示
            currentAge = 10;
            UpdateUI(currentAge);
            forestManager.SetAge(currentAge);
            UpdateAutoPlayButtonText();
        }

        /// <summary>
        /// 自動進行/停止ボタンクリック
        /// </summary>
        private void OnAutoPlayButtonClick()
        {
            if (isAutoPlaying)
            {
                StopAutoPlay();
            }
            else
            {
                StartAutoPlay();
            }
        }

        /// <summary>
        /// 1段階進めるボタンクリック
        /// </summary>
        private void OnNextButtonClick()
        {
            StopAutoPlay();
            ChangeAge(currentAge + 5);
        }

        /// <summary>
        /// 1段階戻すボタンクリック
        /// </summary>
        private void OnPrevButtonClick()
        {
            StopAutoPlay();
            ChangeAge(currentAge - 5);
        }

        /// <summary>
        /// リセットボタンクリック
        /// </summary>
        private void OnResetButtonClick()
        {
            StopAutoPlay();
            ChangeAge(10);
        }

        /// <summary>
        /// 自動進行開始
        /// </summary>
        private void StartAutoPlay()
        {
            if (isAutoPlaying)
                return;

            isAutoPlaying = true;
            autoPlayCoroutine = StartCoroutine(AutoPlayCoroutine());
            UpdateAutoPlayButtonText();
        }

        /// <summary>
        /// 自動進行停止
        /// </summary>
        private void StopAutoPlay()
        {
            if (!isAutoPlaying)
                return;

            isAutoPlaying = false;
            if (autoPlayCoroutine != null)
            {
                StopCoroutine(autoPlayCoroutine);
                autoPlayCoroutine = null;
            }
            UpdateAutoPlayButtonText();
        }

        /// <summary>
        /// 自動進行コルーチン
        /// </summary>
        private IEnumerator AutoPlayCoroutine()
        {
            while (isAutoPlaying)
            {
                yield return new WaitForSeconds(autoPlayInterval);

                int nextAge = currentAge + 5;
                if (nextAge > 100)
                {
                    nextAge = 10; // ループ
                }

                ChangeAge(nextAge);
            }
        }

        /// <summary>
        /// 年齢変更
        /// </summary>
        private void ChangeAge(int newAge)
        {
            newAge = Mathf.Clamp(newAge, 10, 100);

            if (newAge == currentAge)
                return;

            currentAge = newAge;
            UpdateUI(newAge);
            forestManager.SetAge(newAge);
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

        /// <summary>
        /// 自動進行ボタンのテキスト更新
        /// </summary>
        private void UpdateAutoPlayButtonText()
        {
            if (autoPlayButtonText != null)
            {
                autoPlayButtonText.text = isAutoPlaying ? "停止" : "自動進行";
            }
        }

        private void OnDestroy()
        {
            StopAutoPlay();
        }
    }
}
