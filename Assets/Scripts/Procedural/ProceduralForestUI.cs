using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WoodSimulator
{
    /// <summary>
    /// プロシージャル林成長シミュレーションのUI制御。
    /// ボタン操作（AutoPlay/Next/Prev/Reset）とテキスト表示。
    /// </summary>
    public class ProceduralForestUI : MonoBehaviour
    {
        [Header("References")]
        public GrowthDatabase growthDatabase;
        public ProceduralForestManager forestManager;

        [Header("UI - Buttons")]
        public Button autoPlayButton;
        public Button nextButton;
        public Button prevButton;
        public Button resetButton;

        [Header("UI - Text")]
        public Text ageText;
        public Text heightText;
        public Text diameterText;
        public Text treeCountText;
        public Text progressText;
        public Text autoPlayButtonText;

        [Header("Auto Play")]
        public float autoPlayInterval = 3.0f;

        private bool isAutoPlaying;
        private Coroutine autoPlayCoroutine;

        private void Start()
        {
            if (growthDatabase == null || forestManager == null)
            {
                Debug.LogError("[ProceduralForestUI] Missing references.");
                return;
            }

            if (autoPlayButton != null) autoPlayButton.onClick.AddListener(OnAutoPlayClick);
            if (nextButton != null) nextButton.onClick.AddListener(OnNextClick);
            if (prevButton != null) prevButton.onClick.AddListener(OnPrevClick);
            if (resetButton != null) resetButton.onClick.AddListener(OnResetClick);

            forestManager.OnAgeChanged += HandleAgeChanged;
            forestManager.OnUpdateProgress += HandleUpdateProgress;

            UpdateAutoPlayButtonText();
        }

        private void OnDestroy()
        {
            if (forestManager != null)
            {
                forestManager.OnAgeChanged -= HandleAgeChanged;
                forestManager.OnUpdateProgress -= HandleUpdateProgress;
            }
            StopAutoPlay();
        }

        private void OnAutoPlayClick()
        {
            if (isAutoPlaying)
                StopAutoPlay();
            else
                StartAutoPlay();
        }

        private void OnNextClick()
        {
            StopAutoPlay();
            ChangeStage(forestManager.CurrentStageIndex + 1);
        }

        private void OnPrevClick()
        {
            StopAutoPlay();
            ChangeStage(forestManager.CurrentStageIndex - 1);
        }

        private void OnResetClick()
        {
            StopAutoPlay();
            ChangeStage(0);
        }

        private void ChangeStage(int stageIndex)
        {
            stageIndex = Mathf.Clamp(stageIndex, 0, growthDatabase.Count - 1);
            if (stageIndex == forestManager.CurrentStageIndex) return;
            forestManager.SetStage(stageIndex);
        }

        private void StartAutoPlay()
        {
            if (isAutoPlaying) return;
            isAutoPlaying = true;
            autoPlayCoroutine = StartCoroutine(AutoPlayCoroutine());
            UpdateAutoPlayButtonText();
        }

        private void StopAutoPlay()
        {
            if (!isAutoPlaying) return;
            isAutoPlaying = false;
            if (autoPlayCoroutine != null)
            {
                StopCoroutine(autoPlayCoroutine);
                autoPlayCoroutine = null;
            }
            UpdateAutoPlayButtonText();
        }

        private IEnumerator AutoPlayCoroutine()
        {
            while (isAutoPlaying)
            {
                // モデル更新完了を待つ
                while (forestManager.IsUpdating)
                    yield return null;

                yield return new WaitForSeconds(autoPlayInterval);

                int next = forestManager.CurrentStageIndex + 1;
                if (next >= growthDatabase.Count)
                    next = 0; // ループ

                ChangeStage(next);
            }
        }

        private void HandleAgeChanged(int stageIndex, GrowthData data)
        {
            if (ageText != null) ageText.text = $"林齢: {data.age}年";
            if (heightText != null) heightText.text = $"樹高: {data.height:F1}m";
            if (diameterText != null) diameterText.text = $"直径: {data.diameter:F1}cm";
            if (treeCountText != null) treeCountText.text = $"本数: {data.treeCount}本/ha";
        }

        private void HandleUpdateProgress(int current, int total)
        {
            if (progressText != null)
            {
                if (current < total)
                    progressText.text = $"更新中... {current}/{total}";
                else
                    progressText.text = "";
            }
        }

        private void UpdateAutoPlayButtonText()
        {
            if (autoPlayButtonText != null)
                autoPlayButtonText.text = isAutoPlaying ? "停止" : "自動進行";
        }
    }
}
