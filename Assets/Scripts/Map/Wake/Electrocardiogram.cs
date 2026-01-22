using TMPro;
using UnityEngine;

namespace B
{
    /// <summary>
    /// 心電図を表示するクラス。
    /// 心拍数をテキストで表示し、心電図の波形を線で描画する。
    /// GameStateServiceから距離割合を受け取って表示する。
    /// </summary>
    public class Electrocardiogram : MonoBehaviour
    {
        [Header("心拍数表示")] [SerializeField] private TextMeshProUGUI _heartRateText;
        [SerializeField] private float _maxHeartRate = 120f;

        [Header("心電図波形")] [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private int _waveformPoints = 100; // 波形の点の数
        [SerializeField] private float _waveformWidth = 10f; // 波形の幅
        [SerializeField] private float _baseAmplitude = 0.5f; // 基本振幅
        [SerializeField] private float _maxAmplitude = 2f; // 最大振幅
        [SerializeField] private float _waveSpeed = 2f; // 波形のスクロール速度

        [Header("Photon設定")] [SerializeField]
        private string _distanceRatioKey = "LaserDistanceRatio"; // Room Custom Propertyのキー

        [SerializeField] private float _minHeartRate = 60f; // 最小心拍数

        private float _currentHeartRate;
        private float _timeOffset;
        private float _currentRatio; // 現在の距離割合

        private void Start()
        {
            InitializeLineRenderer();

            // GameStateServiceから初期値を読み取る
            if (GameStateService.Instance != null && GameStateService.Instance.HasFloat(_distanceRatioKey))
            {
                _currentRatio = GameStateService.Instance.GetFloat(_distanceRatioKey);
            }

            UpdateHeartRateDisplay();
        }

        private void Update()
        {
            // GameStateServiceから割合を取得して心拍数を更新
            if (GameStateService.Instance != null && GameStateService.Instance.HasFloat(_distanceRatioKey))
            {
                _currentRatio = GameStateService.Instance.GetFloat(_distanceRatioKey);
                float heartRate = ConvertRatioToHeartRate(_currentRatio);
                _currentHeartRate = Mathf.Clamp(heartRate, 0f, _maxHeartRate);
                UpdateHeartRateDisplay();
            }

            // 時間経過で波形をスクロール
            _timeOffset += Time.deltaTime * _waveSpeed;
            UpdateWaveform();
        }

        /// <summary>
        /// 割合を心拍数に変換
        /// </summary>
        /// <param name="ratio">0-1の割合（0=遠い、1=近い）</param>
        /// <returns>心拍数</returns>
        private float ConvertRatioToHeartRate(float ratio)
        {
            // 割合を心拍数に変換（最小60から最大120）
            return Mathf.Lerp(_minHeartRate, _maxHeartRate, ratio);
        }

        /// <summary>
        /// LineRendererを初期化
        /// </summary>
        private void InitializeLineRenderer()
        {
            if (_lineRenderer == null)
            {
                Debug.LogWarning("LineRendererが設定されていません。");
                return;
            }

            _lineRenderer.positionCount = _waveformPoints;
            _lineRenderer.useWorldSpace = false;

            // 線の見た目を設定（必要に応じて調整）
            _lineRenderer.startWidth = 0.05f;
            _lineRenderer.endWidth = 0.05f;
        }

        /// <summary>
        /// 心拍数を設定し、表示を更新
        /// </summary>
        /// <param name="heartRate">現在の心拍数</param>
        public void SetHeartRate(float heartRate)
        {
            _currentHeartRate = Mathf.Clamp(heartRate, 0f, _maxHeartRate);
            UpdateHeartRateDisplay();
        }

        /// <summary>
        /// 心拍数のテキスト表示を更新
        /// </summary>
        private void UpdateHeartRateDisplay()
        {
            if (_heartRateText != null)
            {
                _heartRateText.text = $"{_currentHeartRate:F0}/{_maxHeartRate:F0}";
            }
        }

        /// <summary>
        /// 心電図の波形を更新
        /// </summary>
        private void UpdateWaveform()
        {
            if (_lineRenderer == null) return;

            // 心拍数に基づいて振幅を計算（心拍数が高いほど振幅が大きくなる）
            float amplitudeMultiplier = Mathf.Lerp(_baseAmplitude, _maxAmplitude, _currentHeartRate / _maxHeartRate);

            // 心拍数に基づいて周波数を計算（心拍数が高いほど波形が密になる）
            float frequency = Mathf.Lerp(1f, 4f, _currentHeartRate / _maxHeartRate);

            for (int i = 0; i < _waveformPoints; i++)
            {
                float t = (float)i / (_waveformPoints - 1);
                float x = t * _waveformWidth - _waveformWidth / 2f;

                // 心電図の波形を生成（QRS波を模擬）
                float phase = (t * frequency * Mathf.PI * 4f) + _timeOffset;
                float y = GenerateECGWave(phase) * amplitudeMultiplier;

                _lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            }
        }

        /// <summary>
        /// 心電図の波形パターンを生成
        /// </summary>
        /// <param name="phase">位相</param>
        /// <returns>波形の高さ</returns>
        private float GenerateECGWave(float phase)
        {
            // 位相を0-2πの範囲に正規化
            float normalizedPhase = phase % (Mathf.PI * 2f);

            // P波、QRS波、T波を模擬した波形
            float wave = 0f;

            // QRS複合体（鋭いピーク）
            if (normalizedPhase < 0.3f)
            {
                wave = Mathf.Sin(normalizedPhase * 10f) * 1.5f;
            }
            // T波（なだらかな波）
            else if (normalizedPhase > 1.5f && normalizedPhase < 3f)
            {
                wave = Mathf.Sin((normalizedPhase - 1.5f) * 2f) * 0.3f;
            }
            // P波（小さな波）
            else if (normalizedPhase > 5f && normalizedPhase < 5.8f)
            {
                wave = Mathf.Sin((normalizedPhase - 5f) * 8f) * 0.2f;
            }

            return wave;
        }

        private void OnEnable()
        {
            // GameStateServiceのイベントを購読
            if (GameStateService.Instance != null)
            {
                GameStateService.Instance.OnPropertyChanged += OnPropertyChanged;
            }
        }

        private void OnDisable()
        {
            // イベント購読を解除
            if (GameStateService.Instance != null)
            {
                GameStateService.Instance.OnPropertyChanged -= OnPropertyChanged;
            }
        }

        /// <summary>
        /// GameStateServiceのプロパティが変更されたときに呼ばれる
        /// </summary>
        private void OnPropertyChanged(string key, object value)
        {
            // 距離割合が更新された場合、即座に反映
            if (key == _distanceRatioKey && value is float floatValue)
            {
                _currentRatio = floatValue;
                float heartRate = ConvertRatioToHeartRate(_currentRatio);
                _currentHeartRate = Mathf.Clamp(heartRate, 0f, _maxHeartRate);
                UpdateHeartRateDisplay();
            }
        }
    }
}