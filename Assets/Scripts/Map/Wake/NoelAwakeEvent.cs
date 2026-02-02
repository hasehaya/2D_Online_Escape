using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Elias
{
    public class NoelAwakeEvent : MonoBehaviour
    {
        [Header("Image Objects")] [SerializeField]
        private Image beforeImage;

        [SerializeField] private Image awakeImage;
        [SerializeField] private StillNode stillNode;

        [Header("Fade Settings")] [SerializeField]
        private float fadeDuration = 1f;

        private Coroutine switchRoutine;

        public void PlayAwakeSequence()
        {
            if (switchRoutine != null)
            {
                StopCoroutine(switchRoutine);
            }

            switchRoutine = StartCoroutine(SwitchToStillAfterSeconds());
        }

        private IEnumerator SwitchToStillAfterSeconds()
        {
            yield return new WaitForSeconds(1f);

            // フェード遷移を開始
            yield return StartCoroutine(FadeTransition());

            yield return new WaitForSeconds(4f);
            ViewController.Instance.ShowStill(stillNode);
            switchRoutine = null;
        }

        private IEnumerator FadeTransition()
        {
            awakeImage.gameObject.SetActive(true);

            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeDuration;

                // beforeImageをフェードアウト
                Color beforeColor = beforeImage.color;
                beforeColor.a = Mathf.Lerp(1f, 0f, t);
                beforeImage.color = beforeColor;

                // awakeImageをフェードイン
                Color awakeColor = awakeImage.color;
                awakeColor.a = Mathf.Lerp(0f, 1f, t);
                awakeImage.color = awakeColor;

                yield return null;
            }

            // 最終状態を確実に設定
            Color finalBeforeColor = beforeImage.color;
            finalBeforeColor.a = 0f;
            beforeImage.color = finalBeforeColor;

            Color finalAwakeColor = awakeImage.color;
            finalAwakeColor.a = 1f;
            awakeImage.color = finalAwakeColor;

            beforeImage.gameObject.SetActive(false);
        }
    }
}