using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Map.Elias.Dungeon
{
    [DisallowMultipleComponent]
    public class LightsOutCrystalProjection : MonoBehaviour
    {
        private const int CellCount = 9;

        [Header("Crystal Projection")] [SerializeField]
        private Image[] _lightImages = new Image[CellCount];

        [SerializeField] private Sprite _offSprite;
        [SerializeField] private Sprite _onSprite;

        [Header("Network")] [SerializeField]
        private string _boardBitsKey = PhotonRoomPropertyKeys.DungeonLightsOutPuzzleBoardBits;

        [SerializeField] private FlagType _completedFlag = FlagType.Dungeon_LightsOutPuzzleCompleted;

        [Header("Events")] [SerializeField] private UnityEvent _onSolved;

        private bool _hasInvokedSolved;

        private void OnEnable()
        {
            if (GameStateService.Instance != null)
            {
                GameStateService.Instance.OnPropertyChanged += OnPropertyChanged;
                GameStateService.Instance.OnFlagChanged += OnFlagChanged;
            }

            RefreshProjection();
        }

        private void Start()
        {
            RefreshProjection();
        }

        private void OnDisable()
        {
            if (GameStateService.Instance != null)
            {
                GameStateService.Instance.OnPropertyChanged -= OnPropertyChanged;
                GameStateService.Instance.OnFlagChanged -= OnFlagChanged;
            }
        }

        [ContextMenu("Refresh Projection")]
        public void RefreshProjection()
        {
            if (IsCompleted())
            {
                ApplyAll(true);
                InvokeSolvedOnce();
                return;
            }

            ApplyBoardBits(ReadBoardBits());
        }

        private void OnPropertyChanged(string key, object value)
        {
            if (key != _boardBitsKey) return;
            if (IsCompleted()) return;

            if (TryConvertToInt(value, out int boardBits))
            {
                ApplyBoardBits(boardBits);
            }
        }

        private void OnFlagChanged(FlagType flag, bool value)
        {
            if (flag != _completedFlag || !value) return;

            ApplyAll(true);
            InvokeSolvedOnce();
        }

        private void ApplyBoardBits(int boardBits)
        {
            for (int i = 0; i < CellCount; i++)
            {
                ApplyLightState(i, (boardBits & (1 << i)) != 0);
            }
        }

        private void ApplyAll(bool isOn)
        {
            for (int i = 0; i < CellCount; i++)
            {
                ApplyLightState(i, isOn);
            }
        }

        private void ApplyLightState(int index, bool isOn)
        {
            if (_lightImages == null || index < 0 || index >= _lightImages.Length) return;

            Image image = _lightImages[index];
            if (image == null) return;

            Sprite sprite = isOn ? _onSprite : _offSprite;
            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }

        private int ReadBoardBits()
        {
            if (string.IsNullOrEmpty(_boardBitsKey)) return 0;
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return 0;
            if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(_boardBitsKey, out object value)) return 0;

            return TryConvertToInt(value, out int boardBits) ? boardBits : 0;
        }

        private bool IsCompleted()
        {
            return _completedFlag != FlagType.None
                   && GameStateService.Instance != null
                   && GameStateService.Instance.GetFlag(_completedFlag);
        }

        private void InvokeSolvedOnce()
        {
            if (_hasInvokedSolved) return;

            _hasInvokedSolved = true;
            _onSolved?.Invoke();
        }

        private static bool TryConvertToInt(object value, out int result)
        {
            if (value is int intValue)
            {
                result = intValue;
                return true;
            }

            try
            {
                result = Convert.ToInt32(value);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }

        private void OnValidate()
        {
            if (_lightImages == null || _lightImages.Length != CellCount)
            {
                Array.Resize(ref _lightImages, CellCount);
            }
        }
    }
}