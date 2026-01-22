using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace A
{
    public class GimmickLaser : MonoBehaviour
    {
        [Header("Laser Handles")] [SerializeField]
        private LaserHandle _horizontalHandle;

        [SerializeField] private LaserHandle _verticalHandle;

        [Header("Target Points (Vector2)")] [SerializeField]
        private Vector2[] _targetPoints = new Vector2[3];

        [Header("Target Images")] [SerializeField]
        private Image[] _targetImages = new Image[3];

        [Header("Target Correct Sprite")] [SerializeField]
        private Sprite _targetCorrectSprite;

        [Header("Settings")] [SerializeField] private float _correctThreshold = 50f;
        [SerializeField] private float _eventDelaySeconds = 1f;

        [Header("Events")] [SerializeField] private UnityEvent _onAllCorrect;

        [Header("Distance Sharing")] [SerializeField]
        private float _distanceUpdateInterval = 1f; // 距離情報の更新間隔（秒）

        [SerializeField] private string _distanceRatioKey = "LaserDistanceRatio"; // Room Custom Propertyのキー

        private FlagType[] _targetFlags;
        private FlagType _completedFlag;
        private bool[] _isCorrect;
        private bool _isCompleted;
        private float _distanceUpdateTimer;

        private void Awake()
        {
            _targetFlags = new FlagType[]
            {
                FlagType.Wake_LaserTarget1,
                FlagType.Wake_LaserTarget2,
                FlagType.Wake_LaserTarget3
            };
            _completedFlag = FlagType.Wake_LaserCompleted;
            _isCorrect = new bool[3];
        }

        private void Start()
        {
            // 初期状態のリセット
            for (int i = 0; i < _targetImages.Length; i++)
            {
                _isCorrect[i] = false;
            }
        }

        private void Update()
        {
            if (_isCompleted) return;

            CheckAllTargets();

            // MasterClientのみが距離割合を計算してRoom Custom Propertiesに設定
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                _distanceUpdateTimer += Time.deltaTime;

                if (_distanceUpdateTimer >= _distanceUpdateInterval)
                {
                    _distanceUpdateTimer = 0f;
                    UpdateDistanceRatioProperty();
                }
            }
        }

        private void CheckAllTargets()
        {
            int correctCount = 0;

            for (int i = 0; i < _targetPoints.Length; i++)
            {
                float distance = GetDistanceToTarget(i);
                bool isNowCorrect = distance >= 0 && distance <= _correctThreshold;

                if (isNowCorrect && !_isCorrect[i])
                {
                    // 新たに正解になった
                    _isCorrect[i] = true;

                    // スプライトを正解用に変更
                    if (_targetImages[i] != null && _targetCorrectSprite != null)
                    {
                        _targetImages[i].sprite = _targetCorrectSprite;
                    }

                    // フラグを設定
                    if (_targetFlags[i] != FlagType.None && GameStateService.Instance != null)
                    {
                        GameStateService.Instance.SetFlag(_targetFlags[i], true);
                    }
                }

                if (_isCorrect[i])
                {
                    correctCount++;
                }
            }

            // 3つ全て正解したらイベント発火
            if (correctCount >= 3 && !_isCompleted)
            {
                _isCompleted = true;

                // 完了フラグを設定
                if (_completedFlag != FlagType.None && GameStateService.Instance != null)
                {
                    GameStateService.Instance.SetFlag(_completedFlag, true);
                }

                StartCoroutine(TriggerCompletionEvent());
            }
        }

        /// <summary>
        /// 2つのハンドルの交点を計算（UI座標）
        /// </summary>
        public Vector2 GetIntersectionPoint()
        {
            float x = 0f;
            float y = 0f;

            if (_horizontalHandle != null)
            {
                y = _horizontalHandle.GetIntersectionPoint().y;
            }

            if (_verticalHandle != null)
            {
                x = _verticalHandle.GetIntersectionPoint().x;
            }

            return new Vector2(x, y);
        }

        /// <summary>
        /// 交点と指定したターゲット地点との距離を返す
        /// </summary>
        /// <param name="targetIndex">ターゲットのインデックス (0-2)</param>
        /// <returns>距離（Float）。無効なインデックスの場合は-1を返す</returns>
        public float GetDistanceToTarget(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _targetPoints.Length)
            {
                return -1f;
            }

            Vector2 intersection = GetIntersectionPoint();
            return Vector2.Distance(intersection, _targetPoints[targetIndex]);
        }

        /// <summary>
        /// 現在有効なTargetのうち一番近いものの距離を返す
        /// </summary>
        /// <returns>有効なTargetまでの最小距離。有効なTargetがない場合は-1を返す</returns>
        public float GetClosestActiveTargetDistance()
        {
            float closestDistance = float.MaxValue;
            bool hasValidTarget = false;

            for (int i = 0; i < _targetPoints.Length; i++)
            {
                if (!_isCorrect[i])
                {
                    float distance = GetDistanceToTarget(i);
                    if (distance >= 0 && distance < closestDistance)
                    {
                        closestDistance = distance;
                        hasValidTarget = true;
                    }
                }
            }

            return hasValidTarget ? closestDistance : -1f;
        }

        /// <summary>
        /// 距離割合を計算してRoom Custom Propertiesに設定（MasterClientのみ）
        /// </summary>
        private void UpdateDistanceRatioProperty()
        {
            if (GameStateService.Instance == null) return;

            float distance = GetClosestActiveTargetDistance();

            // 距離を0-1の割合に変換（近い=1、遠い=0）
            // ここでは500を最大距離として使用
            float maxDistance = 500f;
            float ratio;

            if (distance < 0)
            {
                ratio = 0f; // 無効な距離の場合は0
            }
            else
            {
                ratio = Mathf.Clamp01(1f - (distance / maxDistance));
            }

            // GameStateServiceを使って設定
            GameStateService.Instance.SetFloat(_distanceRatioKey, ratio);
        }

        private IEnumerator TriggerCompletionEvent()
        {
            yield return new WaitForSeconds(_eventDelaySeconds);
            _onAllCorrect?.Invoke();
        }
    }
}