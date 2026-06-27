using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Escape.SceneObject.Noel.Wake
{
    public class NoelLookUpEvent : MonoBehaviour
    {
        [Header("Eyelid Objects")] [SerializeField]
        private RectTransform upperEyelid;

        [SerializeField] private RectTransform lowerEyelid;

        [Header("Camera")] [SerializeField] private Camera targetCamera;

        [Header("Still")] [SerializeField] private StillNode stillNode;

        [Header("Animation Settings")] [SerializeField]
        private float eyelidOpenDuration = 0.5f;

        [SerializeField] private float eyelidOpenDistance = 1000f;
        [SerializeField] private float cameraMoveDelay = 0.5f;
        [SerializeField] private float cameraMoveDuration = 1.5f;
        [SerializeField] private float cameraMoveDistance = 300f;

        private Coroutine lookUpRoutine;

        public void PlayLookUpSequence()
        {
            if (lookUpRoutine != null)
            {
                StopCoroutine(lookUpRoutine);
            }

            lookUpRoutine = StartCoroutine(LookUpSequence());
        }

        private IEnumerator LookUpSequence()
        {
            // まぶたを開けるアニメーション
            Sequence eyelidSequence = DOTween.Sequence();

            // 上まぶたを上に移動
            eyelidSequence.Join(upperEyelid.DOAnchorPosY(
                upperEyelid.anchoredPosition.y + eyelidOpenDistance,
                eyelidOpenDuration
            ).SetEase(Ease.OutCubic));

            // 下まぶたを下に移動
            eyelidSequence.Join(lowerEyelid.DOAnchorPosY(
                lowerEyelid.anchoredPosition.y - eyelidOpenDistance,
                eyelidOpenDuration
            ).SetEase(Ease.OutCubic));

            // まぶたが開くのを待つ
            yield return eyelidSequence.WaitForCompletion();

            // カメラ移動前の待機
            yield return new WaitForSeconds(cameraMoveDelay);

            // カメラを上に移動
            Vector3 targetPosition = targetCamera.transform.position;
            targetPosition.y += cameraMoveDistance;

            yield return targetCamera.transform.DOMove(targetPosition, cameraMoveDuration)
                .SetEase(Ease.InOutQuad)
                .WaitForCompletion();

            // Stillを表示
            ViewController.Instance.ShowStill(stillNode);

            lookUpRoutine = null;
        }
    }
}