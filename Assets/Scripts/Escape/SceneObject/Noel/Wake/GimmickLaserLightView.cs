using System;
using Escape.SceneObject.Wake;
using UnityEngine;
using UnityEngine.UI;

namespace Escape.SceneObject.Noel.Wake
{
    /// <summary>
    /// Mirrors Elias GimmickLaser target lights to Noel UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class GimmickLaserLightView : MonoBehaviour
    {
        private static readonly FlagType[] TargetFlags = WakeLaserProgress.TargetFlags;

        [Header("Light Images")] [SerializeField]
        private Image[] _lightImages = new Image[3];

        [SerializeField] private Sprite _offSprite;
        [SerializeField] private Sprite _onSprite;

        [Header("Light Colors")] [SerializeField]
        private bool _applyColor = true;

        [SerializeField] private Color _offColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color _onColor = Color.white;

        [Header("Optional Objects")] [SerializeField]
        private GameObject[] _offObjects = new GameObject[3];

        [SerializeField] private GameObject[] _onObjects = new GameObject[3];

        private void OnEnable()
        {
            if (GameStateService.Instance != null)
            {
                GameStateService.Instance.OnFlagChanged += OnFlagChanged;
            }

            RefreshLights();
        }

        private void Start()
        {
            RefreshLights();
        }

        private void OnDisable()
        {
            if (GameStateService.Instance != null)
            {
                GameStateService.Instance.OnFlagChanged -= OnFlagChanged;
            }
        }

        [ContextMenu("Refresh Lights")]
        public void RefreshLights()
        {
            for (int i = 0; i < TargetFlags.Length; i++)
            {
                bool isOn = GameStateService.Instance != null && GameStateService.Instance.GetFlag(TargetFlags[i]);
                ApplyLightState(i, isOn);
            }
        }

        private void OnFlagChanged(FlagType flag, bool value)
        {
            int index = Array.IndexOf(TargetFlags, flag);
            if (index < 0)
            {
                return;
            }

            ApplyLightState(index, value);
        }

        private void ApplyLightState(int index, bool isOn)
        {
            Image image = GetArrayItem(_lightImages, index);
            if (image != null)
            {
                Sprite sprite = isOn ? _onSprite : _offSprite;
                if (sprite != null)
                {
                    image.sprite = sprite;
                }

                if (_applyColor)
                {
                    image.color = isOn ? _onColor : _offColor;
                }
            }

            GameObject offObject = GetArrayItem(_offObjects, index);
            if (offObject != null)
            {
                offObject.SetActive(!isOn);
            }

            GameObject onObject = GetArrayItem(_onObjects, index);
            if (onObject != null)
            {
                onObject.SetActive(isOn);
            }
        }

        private static T GetArrayItem<T>(T[] values, int index) where T : class
        {
            if (values == null || index < 0 || index >= values.Length)
            {
                return null;
            }

            return values[index];
        }

        private void OnValidate()
        {
            ResizeToTargetCount(ref _lightImages);
            ResizeToTargetCount(ref _offObjects);
            ResizeToTargetCount(ref _onObjects);
        }

        private static void ResizeToTargetCount<T>(ref T[] values)
        {
            if (values == null || values.Length != TargetFlags.Length)
            {
                Array.Resize(ref values, TargetFlags.Length);
            }
        }
    }
}