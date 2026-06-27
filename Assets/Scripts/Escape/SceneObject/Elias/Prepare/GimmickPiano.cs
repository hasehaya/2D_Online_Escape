using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Escape.SceneObject.Elias.Prepare
{
    /// <summary>
    /// ピアノギミックを管理するクラス。
    /// 鍵盤が正しい順序で押されたかを判定し、成功時にUnityEventを発火する。
    /// </summary>
    public class GimmickPiano : MonoBehaviour
    {
        [Header("Piano Settings")] [SerializeField]
        private int[] _correctSequence;

        [SerializeField] private bool _resetOnWrongKey = true;

        [Header("Events")] [SerializeField] private UnityEvent _onUnlocked;

        private List<int> _pressedSequence = new List<int>();

        /// <summary>
        /// 鍵盤が押された時に呼ばれる
        /// </summary>
        public void OnKeyPressed(int keyIndex)
        {
            _pressedSequence.Add(keyIndex);

            // 現在の入力が正解の途中経過かチェック
            if (!IsSequenceValid())
            {
                if (_resetOnWrongKey)
                {
                    Debug.Log($"[GimmickPiano] 間違った鍵盤が押されました。リセットします。");
                    ResetSequence();
                }

                return;
            }

            // 正しい順序で全て押されたかチェック
            if (_pressedSequence.Count == _correctSequence.Length)
            {
                Debug.Log($"[GimmickPiano] 正しい順序で全て押されました！ロック解除。");
                _onUnlocked?.Invoke();
                ResetSequence();
            }
        }

        /// <summary>
        /// 現在の入力シーケンスが正解の途中経過として有効かチェック
        /// </summary>
        private bool IsSequenceValid()
        {
            for (int i = 0; i < _pressedSequence.Count; i++)
            {
                if (i >= _correctSequence.Length || _pressedSequence[i] != _correctSequence[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 入力シーケンスをリセット
        /// </summary>
        public void ResetSequence()
        {
            _pressedSequence.Clear();
        }
    }
}