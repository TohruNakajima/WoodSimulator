using UnityEngine;
using UnityEngine.UI;

namespace WoodSimulator
{
    /// <summary>
    /// FPS表示用コンポーネント。
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        public Text fpsText;

        private float deltaTime;

        private void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

            if (fpsText != null)
            {
                float fps = 1.0f / deltaTime;
                fpsText.text = $"FPS: {fps:F0}";
            }
        }
    }
}
