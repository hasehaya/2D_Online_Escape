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
        [SerializeField] private RectTransform _rectTransform; // RectTransformの参照
        [SerializeField] private int _waveformPoints = 100; // 波形の点の数
        [SerializeField] private float _baseAmplitudeRatio = 0.2f; // 基本振幅の比率（高さに対する）
        [SerializeField] private float _maxAmplitudeRatio = 0.4f; // 最大振幅の比率（高さに対する）
        [SerializeField] private float _waveSpeed = 2f; // 波形のスクロール速度
        [SerializeField] private float _lineWidthRatio = 0.01f; // 線の太さの比率（高さに対する）

        [Header("Photon設定")] [SerializeField]
        private string _distanceRatioKey = "LaserDistanceRatio"; // Room Custom Propertyのキー

        [SerializeField] private float _minHeartRate = 60f; // 最小心拍数

        private float _currentHeartRate;
        private float _timeOffset;
        private float _currentRatio; // 現在の距離割合

        private void Start()
        {
            // RectTransformが設定されていない場合、自動で取得
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            InitializeLineRenderer();

            // GameStateServiceから初期値を読み取る
            if (GameStateService.Instance != null && GameStateService.Instance.HasFloat(_distanceRatioKey))
            {
                _currentRatio = GameStateService.Instance.GetFloat(_distanceRatioKey);
                Debug.Log($"[Electrocardiogram] 初期値を受信: Key={_distanceRatioKey}, Ratio={_currentRatio:F3}");
            }
            else
            {
                Debug.Log($"[Electrocardiogram] 初期値が見つかりません: Key={_distanceRatioKey}");
            }

            UpdateHeartRateDisplay();
        }

        private void Update()
        {
            // GameStateServiceから割合を取得して心拍数を更新
            if (GameStateService.Instance != null && GameStateService.Instance.HasFloat(_distanceRatioKey))
            {
                float newRatio = GameStateService.Instance.GetFloat(_distanceRatioKey);

                // 値が変わった場合のみログを出力
                if (Mathf.Abs(newRatio - _currentRatio) > 0.001f)
                {
                    Debug.Log($"[Electrocardiogram] Update内で距離割合を受信: Key={_distanceRatioKey}, Ratio={newRatio:F3}");
                    _currentRatio = newRatio;
                }

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

            // World空間でのRectTransformのサイズに基づいて線の太さを設定
            UpdateLineWidth();
        }

        /// <summary>
        /// World空間でのRectTransformのサイズを取得
        /// </summary>
        private Vector2 GetWorldSize()
        {
            if (_rectTransform == null) return Vector2.one * 10f;

            // RectTransformの四隅を取得
            Vector3[] corners = new Vector3[4];
            _rectTransform.GetWorldCorners(corners);

            // 幅と高さを計算
            float width = Vector3.Distance(corners[0], corners[3]);
            float height = Vector3.Distance(corners[0], corners[1]);

            return new Vector2(width, height);
        }

        /// <summary>
        /// 線の太さを更新
        /// </summary>
        private void UpdateLineWidth()
        {
            if (_lineRenderer == null) return;

            Vector2 worldSize = GetWorldSize();
            float lineWidth = worldSize.y * _lineWidthRatio;

            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
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

            // World空間でのRectTransformのサイズを取得
            Vector2 worldSize = GetWorldSize();
            float waveformWidth = worldSize.x;
            float waveformHeight = worldSize.y;

            // 心拍数に基づいて振幅を計算（心拍数が高いほど振幅が大きくなる）
            float baseAmplitude = waveformHeight * _baseAmplitudeRatio;
            float maxAmplitude = waveformHeight * _maxAmplitudeRatio;
            float amplitudeMultiplier = Mathf.Lerp(baseAmplitude, maxAmplitude, _currentHeartRate / _maxHeartRate);

            // 心拍数に基づいて周波数を計算（心拍数が高いほど波形が密になる）
            float frequency = Mathf.Lerp(1f, 4f, _currentHeartRate / _maxHeartRate);

            for (int i = 0; i < _waveformPoints; i++)
            {
                float t = (float)i / (_waveformPoints - 1);
                float x = t * waveformWidth - waveformWidth / 2f;

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
            float t = (phase % (Mathf.PI * 2f)) / (Mathf.PI * 2f); // 0-1に正規化

            // QRS波（鋭いピーク）- より細かく滑らかに
            if (t < 0.15f)
            {
                float qrsPhase = t / 0.15f; // 0-1に正規化
                return Mathf.Sin(qrsPhase * Mathf.PI) * 1.5f;
            }
            // T波（なだらかな波）
            else if (t > 0.3f && t < 0.6f)
            {
                float tPhase = (t - 0.3f) / 0.3f;
                return Mathf.Sin(tPhase * Mathf.PI) * 0.3f;
            }
            // P波（小さな波）
            else if (t > 0.8f)
            {
                float pPhase = (t - 0.8f) / 0.2f;
                return Mathf.Sin(pPhase * Mathf.PI) * 0.2f;
            }

            return 0f;
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
                Debug.Log($"[Electrocardiogram] OnPropertyChangedで距離割合を受信: Key={key}, Ratio={floatValue:F3}");
                _currentRatio = floatValue;
                float heartRate = ConvertRatioToHeartRate(_currentRatio);
                _currentHeartRate = Mathf.Clamp(heartRate, 0f, _maxHeartRate);
                UpdateHeartRateDisplay();
            }
        }
    }
}