using System;
using System.Collections;
using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// 間伐対象の木にアタッチし、Y方向スケール縮小でフェードアウト演出を行う。
    /// 演出完了後にコールバックを呼び出す。
    /// </summary>
    public class ThinningAnimator : MonoBehaviour
    {
        private float duration = 1.0f;
        private Action onComplete;

        public void Play(float duration, Action onComplete)
        {
            this.duration = duration;
            this.onComplete = onComplete;
            StartCoroutine(AnimateCoroutine());
        }

        private IEnumerator AnimateCoroutine()
        {
            Vector3 startScale = transform.localScale;
            Vector3 endScale = new Vector3(startScale.x, 0f, startScale.z);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseInで加速感を出す
                float easedT = t * t;
                transform.localScale = Vector3.Lerp(startScale, endScale, easedT);
                yield return null;
            }

            transform.localScale = endScale;
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }
    }
}
