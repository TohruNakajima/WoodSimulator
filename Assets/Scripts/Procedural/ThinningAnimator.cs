using System.Collections;
using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// 間伐対象の木にアタッチし、Y方向スケール縮小でフェードアウト演出を行う。
    /// 初期化時にアタッチされ、Play/Stopで再利用される。
    /// </summary>
    public class ThinningAnimator : MonoBehaviour
    {
        private float duration = 1.0f;
        private Coroutine animCoroutine;

        public void Play(float duration)
        {
            this.duration = duration;
            enabled = true;
            if (animCoroutine != null)
                StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimateCoroutine());
        }

        public void Stop()
        {
            if (animCoroutine != null)
            {
                StopCoroutine(animCoroutine);
                animCoroutine = null;
            }
            enabled = false;
        }

        private IEnumerator AnimateCoroutine()
        {
            Vector3 startScale = transform.localScale;
            Vector3 endScale = new Vector3(startScale.x, 0f, startScale.z);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easedT = t * t;
                transform.localScale = Vector3.Lerp(startScale, endScale, easedT);
                yield return null;
            }

            transform.localScale = endScale;
            gameObject.SetActive(false);
            animCoroutine = null;
            enabled = false;
        }
    }
}
