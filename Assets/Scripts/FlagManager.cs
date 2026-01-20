using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// ゲーム内のフラグ（状態）を管理し、ネットワーク越しに同期するクラス。
/// 特定のパズルが解かれたか、ドアが開いたかなどの状態を管理する。
/// </summary>
public class FlagManager : MonoBehaviourPunCallbacks
{
    public static FlagManager Instance { get; private set; }

    // 自分自身のフラグ状態
    private Dictionary<FlagType, bool> _localFlags = new Dictionary<FlagType, bool>();

    // 相手プレイヤーのフラグ状態
    private Dictionary<FlagType, bool> _remoteFlags = new Dictionary<FlagType, bool>();

    // フラグ変更イベント (flag, newValue, isLocal)
    public event Action<FlagType, bool, bool> OnFlagChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 自分のフラグを設定し、相手に同期する。
    /// </summary>
    public void SetFlag(FlagType flag, bool value)
    {
        if (flag == FlagType.None) return;

        // ローカル辞書を更新
        if (!_localFlags.ContainsKey(flag) || _localFlags[flag] != value)
        {
            _localFlags[flag] = value;
            OnFlagChanged?.Invoke(flag, value, true);
            Debug.Log($"[Local] Flag Set: {flag} = {value}");

            // 相手に通知 (Enumをintにキャストして送る)
            photonView.RPC(nameof(RPC_UpdateRemoteFlag), RpcTarget.Others, (int)flag, value);
        }
    }

    /// <summary>
    /// 自分のフラグ状態を取得する。
    /// </summary>
    public bool GetLocalFlag(FlagType flag)
    {
        if (_localFlags.ContainsKey(flag))
        {
            return _localFlags[flag];
        }
        return false;
    }

    /// <summary>
    /// 相手のフラグ状態を取得する。
    /// </summary>
    public bool GetRemoteFlag(FlagType flag)
    {
        if (_remoteFlags.ContainsKey(flag))
        {
            return _remoteFlags[flag];
        }
        return false;
    }

    [PunRPC]
    private void RPC_UpdateRemoteFlag(int flagId, bool value)
    {
        FlagType flag = (FlagType)flagId;

        // 相手から送られてきたフラグは「相手のフラグ」として保存する
        if (!_remoteFlags.ContainsKey(flag) || _remoteFlags[flag] != value)
        {
            _remoteFlags[flag] = value;
            OnFlagChanged?.Invoke(flag, value, false);
            Debug.Log($"[Remote] Flag Updated: {flag} = {value}");
        }
    }
}
