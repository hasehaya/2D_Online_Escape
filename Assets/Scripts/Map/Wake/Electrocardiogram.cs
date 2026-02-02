using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Noel
{
    /// <summary>
    /// 心電図を表示するクラス。
    /// 一定間隔（心拍数依存）で波形を描画し、それ以外はフラットにする。
    /// </summary>
    public class Electrocardiogram : MonoBehaviour
    {
        [Header("心拍数表示")] [SerializeField] private TextMeshProUGUI _heartRateText;
        [SerializeField] private float _maxHeartRate = 120f;
        [SerializeField] private float _minHeartRate = 60f;

        [Header("心電図波形設定")] [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private int _waveformPoints = 100; // 画面上に表示する点の総数

        [Tooltip("波形の流れる速さ（1秒間に何点進むか）")] [SerializeField]
        private float _scrollSpeed = 50f;

        [Tooltip("1回の波形（ドクン）が表示される長さ（秒）。BPMが早くなってもこの長さは固定され、間隔だけが詰まります。")] [SerializeField]
        private float _waveDuration = 0.4f;

        [SerializeField] private float _baseAmplitudeRatio = 0.2f; // 基本振幅
        [SerializeField] private float _maxAmplitudeRatio = 0.4f; // 最大振幅
        [SerializeField] private float _lineWidthRatio = 0.01f; // 線の太さ

        [Header("Photon設定")] [SerializeField] private string _distanceRatioKey = "LaserDistanceRatio";

        [Header("Events")] [SerializeField] private UnityEvent _onAllCorrect;

        [Header("View Node")] [SerializeField] private ViewNode _targetViewNode;

        // 内部変数
        private float _currentHeartRate;
        private float _currentRatio;
        private float[] _waveformData;
        private float _timeBuffer = 0f; // 更新タイミング調整用

        // 波形生成制御用
        private float _timeSinceLastBeat = 0f; // 前回の拍動からの経過時間

        private bool _hasTriggeredLookUpEvent = false; // LookUpEventが既に実行されたかどうか

        private void Start()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();

            InitializeLineRenderer();

            // 配列初期化（すべて0）
            _waveformData = new float[_waveformPoints];
            for (int i = 0; i < _waveformPoints; i++) _waveformData[i] = 0f;

            // 初期値取得
            if (GameStateService.Instance != null && GameStateService.Instance.HasFloat(_distanceRatioKey))
            {
                _currentRatio = GameStateService.Instance.GetFloat(_distanceRatioKey);
            }

            // 初期レート設定
            UpdateHeartRateFromRatio(_currentRatio);

            // 最初の波がすぐ来るように時間をセット
            _timeSinceLastBeat = 100f;
        }

        private void Update()
        {
            // 1. GameState更新チェック
            CheckGameStateUpdate();

            // 2. 経過時間分だけデータを進める
            _timeBuffer += Time.deltaTime;
            float timePerPoint = 1f / _scrollSpeed;

            // 必要な点数分だけループしてシフト＆生成
            while (_timeBuffer >= timePerPoint)
            {
                _timeBuffer -= timePerPoint;
                AddNewPoint(timePerPoint);
            }

            // 3. 描画
            RenderWaveform();
        }

        /// <summary>
        /// 配列をシフトし、右端に新しい値を入れる
        /// </summary>
        /// <param name="dt">この1点が進む時間（秒）</param>
        private void AddNewPoint(float dt)
        {
            // A. 配列を左にシフト
            for (int i = 0; i < _waveformPoints - 1; i++)
            {
                _waveformData[i] = _waveformData[i + 1];
            }

            // B. 時間を経過させる
            _timeSinceLastBeat += dt;

            // C. 次の拍動が来るべきか判定
            // 60 BPM = 1秒間隔, 120 BPM = 0.5秒間隔
            float beatInterval = 60f / _currentHeartRate;

            if (_timeSinceLastBeat >= beatInterval)
            {
                _timeSinceLastBeat = 0f; // カウントリセット（新しい波の開始）
            }

            // D. 値の生成
            float newY = 0f;

            // 「波の長さ(_waveDuration)」以内の時間であれば波形を描く
            // それ以外の時間は 0 (フラット) のまま
            if (_timeSinceLastBeat <= _waveDuration)
            {
                // 0.0 ～ 1.0 の進行度（波形単体の中での進行度）
                float t = _timeSinceLastBeat / _waveDuration;

                // 振幅計算
                Vector2 worldSize = GetWorldSize();
                float waveformHeight = worldSize.y;
                float amplitudeMultiplier = Mathf.Lerp(
                    waveformHeight * _baseAmplitudeRatio,
                    waveformHeight * _maxAmplitudeRatio,
                    (_currentHeartRate - _minHeartRate) / (_maxHeartRate - _minHeartRate)
                );

                newY = GenerateECGValue(t) * amplitudeMultiplier;
            }
            else
            {
                newY = 0f; // 波が終わったら平らにする
            }

            // E. 配列の末尾に格納
            _waveformData[_waveformPoints - 1] = newY;
        }

        /// <summary>
        /// 心電図の形状関数 (t: 0.0 -> 1.0)
        /// 波形持続時間(_waveDuration)内での形状
        /// </summary>
        private float GenerateECGValue(float t)
        {
            // P波 (小さな山)
            if (t >= 0.0f && t < 0.2f)
            {
                float localT = t / 0.2f; // 0-1
                return Mathf.Sin(localT * Mathf.PI) * 0.15f;
            }
            // QRS波 (鋭い動き)
            else if (t >= 0.25f && t < 0.45f)
            {
                float localT = (t - 0.25f) / 0.2f; // 0-1

                // Q (下がる)
                if (localT < 0.2f) return -0.15f * Mathf.Sin((localT / 0.2f) * Mathf.PI);
                // R (大きく上がる)
                if (localT < 0.6f) return 1.0f * Mathf.Sin(((localT - 0.2f) / 0.4f) * Mathf.PI);
                // S (下がる)
                return -0.2f * Mathf.Sin(((localT - 0.6f) / 0.4f) * Mathf.PI);
            }
            // T波 (中くらいの山)
            else if (t >= 0.6f && t < 0.9f)
            {
                float localT = (t - 0.6f) / 0.3f; // 0-1
                return Mathf.Sin(localT * Mathf.PI) * 0.25f;
            }

            return 0f;
        }

        private void RenderWaveform()
        {
            if (_lineRenderer == null) return;

            Vector2 worldSize = GetWorldSize();
            float width = worldSize.x;
            float halfWidth = width / 2f;

            for (int i = 0; i < _waveformPoints; i++)
            {
                float t = (float)i / (_waveformPoints - 1);
                float x = t * width - halfWidth;
                float y = _waveformData[i];

                _lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            }
        }

        // --- 補助メソッド ---

        private void CheckGameStateUpdate()
        {
            if (GameStateService.Instance != null && GameStateService.Instance.HasFloat(_distanceRatioKey))
            {
                float newRatio = GameStateService.Instance.GetFloat(_distanceRatioKey);
                // 値が変わった時だけ更新
                if (Mathf.Abs(newRatio - _currentRatio) > 0.001f)
                {
                    _currentRatio = newRatio;
                    UpdateHeartRateFromRatio(_currentRatio);
                }
            }
        }

        private void UpdateHeartRateFromRatio(float ratio)
        {
            float heartRate = Mathf.Lerp(_minHeartRate, _maxHeartRate, ratio);
            _currentHeartRate = Mathf.Clamp(heartRate, _minHeartRate, _maxHeartRate);

            if (_heartRateText != null)
            {
                // ご要望の形式: "100/120"
                _heartRateText.text = $"{_currentHeartRate:F0}/{_maxHeartRate:F0}";
            }
        }

        private void InitializeLineRenderer()
        {
            if (_lineRenderer == null) return;
            _lineRenderer.positionCount = _waveformPoints;
            _lineRenderer.useWorldSpace = false;
            UpdateLineWidth();
        }

        private Vector2 GetWorldSize()
        {
            if (_rectTransform == null) return Vector2.one * 10f;
            Vector3[] corners = new Vector3[4];
            _rectTransform.GetWorldCorners(corners);
            float width = Vector3.Distance(corners[0], corners[3]);
            float height = Vector3.Distance(corners[0], corners[1]);
            return new Vector2(width, height);
        }

        private void UpdateLineWidth()
        {
            if (_lineRenderer == null) return;
            Vector2 worldSize = GetWorldSize();
            float lineWidth = worldSize.y * _lineWidthRatio;
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
        }

        private void OnEnable()
        {
            if (GameStateService.Instance != null)
            {
                GameStateService.Instance.OnPropertyChanged += OnPropertyChanged;
                GameStateService.Instance.OnFlagChanged += OnFlagChanged;
            }
        }

        private void OnDisable()
        {
            if (GameStateService.Instance != null)
            {
                GameStateService.Instance.OnPropertyChanged -= OnPropertyChanged;
                GameStateService.Instance.OnFlagChanged -= OnFlagChanged;
            }
        }

        private void OnPropertyChanged(string key, object value)
        {
            if (key == _distanceRatioKey && value is float floatValue)
            {
                _currentRatio = floatValue;
                UpdateHeartRateFromRatio(_currentRatio);
            }
        }

        private void OnFlagChanged(FlagType flag, bool value)
        {
            if (flag == FlagType.Wake_LaserCompleted && value && !_hasTriggeredLookUpEvent)
            {
                _hasTriggeredLookUpEvent = true;
                ViewController.Instance.ShowView(_targetViewNode);
                _onAllCorrect?.Invoke();
            }
        }
    }
}