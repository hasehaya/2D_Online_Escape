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

        [Header("View Node")] [SerializeField] private ViewNode _targetViewNode;

        [Header("Settings")] [SerializeField] private float _correctRatioThreshold = 0.91f; // 91%まで近づいたら正解
        [SerializeField] private float _maxDistance = 1000f; // 最大距離（距離割合の計算に使用）

        [Header("Events")] [SerializeField] private UnityEvent _onAllCorrect;

        [Header("Distance Sharing")] [SerializeField]
        private float _distanceUpdateInterval = 1f; // 距離情報の更新間隔（秒）

        [SerializeField] private string _distanceRatioKey = "LaserDistanceRatio"; // Room Custom Propertyのキー

        private FlagType[] _targetFlags;
        private FlagType _completedFlag;
        private bool[] _isCorrect;
        private bool _isCompleted;
        private float _distanceUpdateTimer;
        private int _nextTargetIndex; // 次に正解にするターゲットのインデックス（順番制御用）

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
            _nextTargetIndex = 0; // 最初のターゲット（インデックス0）から開始
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
            // 既に全て完了している場合は何もしない
            if (_nextTargetIndex >= _targetPoints.Length)
            {
                return;
            }

            // 次に正解にするべきターゲットをチェック
            int targetIndex = _nextTargetIndex;
            float distance = GetDistanceToTarget(targetIndex);

            if (distance < 0)
            {
                return; // 無効な距離の場合は何もしない
            }

            // 距離を0-1の割合に変換（近い=1、遠い=0）
            float ratio = Mathf.Clamp01(1f - (distance / _maxDistance));

            // 93%以上近づいたら正解
            if (ratio >= _correctRatioThreshold && !_isCorrect[targetIndex])
            {
                // ランプを正解にする
                _isCorrect[targetIndex] = true;

                // スプライトを正解用に変更
                if (_targetImages[targetIndex] != null && _targetCorrectSprite != null)
                {
                    _targetImages[targetIndex].sprite = _targetCorrectSprite;
                }

                // 対応する番号のフラグを設定
                if (_targetFlags[targetIndex] != FlagType.None && GameStateService.Instance != null)
                {
                    GameStateService.Instance.SetFlag(_targetFlags[targetIndex], true);
                    Debug.Log(
                        $"[GimmickLaser] ターゲット{targetIndex + 1}を正解に設定: Flag={_targetFlags[targetIndex]}, Ratio={ratio:F3}");
                }

                // 次のターゲットに進む
                _nextTargetIndex++;

                // 全て正解したかチェック
                if (_nextTargetIndex >= _targetPoints.Length && !_isCompleted)
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
        /// 現在有効なTarget（次に正解にするべきTarget）の距離を返す
        /// </summary>
        /// <returns>有効なTargetまでの距離。有効なTargetがない場合は-1を返す</returns>
        public float GetClosestActiveTargetDistance()
        {
            // 全て正解済みの場合
            if (_nextTargetIndex >= _targetPoints.Length)
            {
                return -1f;
            }

            // 次に正解にするべきターゲットの距離を返す
            return GetDistanceToTarget(_nextTargetIndex);
        }

        /// <summary>
        /// 距離割合を計算してRoom Custom Propertiesに設定（MasterClientのみ）
        /// </summary>
        private void UpdateDistanceRatioProperty()
        {
            if (GameStateService.Instance == null) return;

            float distance = GetClosestActiveTargetDistance();

            // 距離を0-1の割合に変換（近い=1、遠い=0）
            float ratio;

            if (distance < 0)
            {
                ratio = 0f; // 無効な距離の場合は0
            }
            else
            {
                ratio = Mathf.Clamp01(1f - (distance / _maxDistance));
            }

            // GameStateServiceを使って設定
            GameStateService.Instance.SetFloat(_distanceRatioKey, ratio);

            // 送信ログ
            Debug.Log($"[GimmickLaser] 距離割合を送信: Key={_distanceRatioKey}, Ratio={ratio:F3}, Distance={distance:F1}");
        }

        private IEnumerator TriggerCompletionEvent()
        {
            yield return new WaitForSeconds(1f);

            // ViewNodeに移動
            if (_targetViewNode != null && ViewManager.Instance != null)
            {
                ViewManager.Instance.ShowView(_targetViewNode);
                _onAllCorrect?.Invoke();
            }
        }
    }
}