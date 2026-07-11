using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Photonのルームカスタムプロパティを使用してゲーム状態を管理する汎用Serviceクラス。
/// Float、Bool、Enum値をネットワーク越しに同期する。
/// 旧FlagManagerの機能も統合する。
/// </summary>
public class GameStateService : IInRoomCallbacks
{
    private static GameStateService _instance;

    public static GameStateService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameStateService();
            }

            return _instance;
        }
    }

    // 値変更イベント
    public event Action<string, object> OnPropertyChanged;
    public event Action<FlagType, bool> OnFlagChanged;

    private GameStateService()
    {
        // コールバックを登録
        PhotonNetwork.AddCallbackTarget(this);
    }

    /// <summary>
    /// サービスを破棄する際に呼び出す
    /// </summary>
    public void Dispose()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    #region Float値の管理

    /// <summary>
    /// Float値を設定してネットワーク同期
    /// </summary>
    /// <param name="key">プロパティキー</param>
    /// <param name="value">設定する値</param>
    public void SetFloat(string key, float value)
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning($"[GameStateService] Room内ではありません。Float値 '{key}' を設定できません。");
            return;
        }

        var properties = new Hashtable { { key, value } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    /// <summary>
    /// Float値を取得
    /// </summary>
    /// <param name="key">プロパティキー</param>
    /// <param name="defaultValue">キーが存在しない場合のデフォルト値</param>
    /// <returns>取得した値</returns>
    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key))
        {
            return defaultValue;
        }

        object value = PhotonNetwork.CurrentRoom.CustomProperties[key];
        if (value is float floatValue)
        {
            return floatValue;
        }

        // intやdoubleからの変換も試みる
        try
        {
            return Convert.ToSingle(value);
        }
        catch
        {
            Debug.LogWarning($"[GameStateService] キー '{key}' の値をFloatに変換できません。デフォルト値を返します。");
            return defaultValue;
        }
    }

    /// <summary>
    /// Float値が存在するかチェック
    /// </summary>
    public bool HasFloat(string key)
    {
        return PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key);
    }

    #endregion

    #region Bool値の管理

    /// <summary>
    /// Bool値を設定してネットワーク同期
    /// </summary>
    /// <param name="key">プロパティキー</param>
    /// <param name="value">設定する値</param>
    public void SetBool(string key, bool value)
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning($"[GameStateService] Room内ではありません。Bool値 '{key}' を設定できません。");
            return;
        }

        var properties = new Hashtable { { key, value } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    /// <summary>
    /// Bool値を取得
    /// </summary>
    /// <param name="key">プロパティキー</param>
    /// <param name="defaultValue">キーが存在しない場合のデフォルト値</param>
    /// <returns>取得した値</returns>
    public bool GetBool(string key, bool defaultValue = false)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key))
        {
            return defaultValue;
        }

        object value = PhotonNetwork.CurrentRoom.CustomProperties[key];
        if (value is bool boolValue)
        {
            return boolValue;
        }

        Debug.LogWarning($"[GameStateService] キー '{key}' の値はBoolではありません。デフォルト値を返します。");
        return defaultValue;
    }

    /// <summary>
    /// Bool値が存在するかチェック
    /// </summary>
    public bool HasBool(string key)
    {
        return PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key);
    }

    #endregion

    #region Int値の管理

    /// <summary>
    /// Int値を設定してネットワーク同期
    /// </summary>
    /// <param name="key">プロパティキー</param>
    /// <param name="value">設定する値</param>
    public void SetInt(string key, int value)
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning($"[GameStateService] Room内ではありません。Int値 '{key}' を設定できません。");
            return;
        }

        var properties = new Hashtable { { key, value } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    /// <summary>
    /// Int値を取得
    /// </summary>
    /// <param name="key">プロパティキー</param>
    /// <param name="defaultValue">キーが存在しない場合のデフォルト値</param>
    /// <returns>取得した値</returns>
    public int GetInt(string key, int defaultValue = 0)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key))
        {
            return defaultValue;
        }

        object value = PhotonNetwork.CurrentRoom.CustomProperties[key];
        if (value is int intValue)
        {
            return intValue;
        }

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            Debug.LogWarning($"[GameStateService] キー '{key}' の値をIntに変換できません。デフォルト値を返します。");
            return defaultValue;
        }
    }

    /// <summary>
    /// Int値が存在するかチェック
    /// </summary>
    public bool HasInt(string key)
    {
        return PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key);
    }

    #endregion

    #region Enum値の管理

    /// <summary>
    /// Enum値を設定してネットワーク同期（intにキャストして保存）
    /// </summary>
    /// <typeparam name="T">Enum型</typeparam>
    /// <param name="key">プロパティキー</param>
    /// <param name="value">設定する値</param>
    public void SetEnum<T>(string key, T value) where T : Enum
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning($"[GameStateService] Room内ではありません。Enum値 '{key}' を設定できません。");
            return;
        }

        int intValue = Convert.ToInt32(value);
        var properties = new Hashtable { { key, intValue } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    /// <summary>
    /// Enum値を取得
    /// </summary>
    /// <typeparam name="T">Enum型</typeparam>
    /// <param name="key">プロパティキー</param>
    /// <param name="defaultValue">キーが存在しない場合のデフォルト値</param>
    /// <returns>取得した値</returns>
    public T GetEnum<T>(string key, T defaultValue = default) where T : Enum
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key))
        {
            return defaultValue;
        }

        object value = PhotonNetwork.CurrentRoom.CustomProperties[key];
        try
        {
            int intValue = Convert.ToInt32(value);
            return (T)Enum.ToObject(typeof(T), intValue);
        }
        catch
        {
            Debug.LogWarning($"[GameStateService] キー '{key}' の値をEnum '{typeof(T).Name}' に変換できません。デフォルト値を返します。");
            return defaultValue;
        }
    }

    /// <summary>
    /// Enum値が存在するかチェック
    /// </summary>
    public bool HasEnum(string key)
    {
        return PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key);
    }

    #endregion

    #region Flag管理（旧FlagManager機能）

    /// <summary>
    /// フラグを設定してネットワーク同期（旧FlagManager.SetFlag互換）
    /// </summary>
    /// <param name="flag">フラグタイプ</param>
    /// <param name="value">設定する値</param>
    public void SetFlag(FlagType flag, bool value)
    {
        if (flag == FlagType.None) return;

        string key = PhotonRoomPropertyKeys.BuildFlagKey(flag);
        SetBool(key, value);
    }

    /// <summary>
    /// フラグの状態を取得（旧FlagManager.GetLocalFlag互換）
    /// </summary>
    /// <param name="flag">フラグタイプ</param>
    /// <returns>フラグの状態</returns>
    public bool GetFlag(FlagType flag)
    {
        if (flag == FlagType.None) return false;

        string key = PhotonRoomPropertyKeys.BuildFlagKey(flag);
        return GetBool(key);
    }

    #endregion

    #region IInRoomCallbacksの実装

    /// <summary>
    /// ルームのカスタムプロパティが変更されたときに呼ばれる
    /// </summary>
    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        foreach (var key in propertiesThatChanged.Keys)
        {
            string keyString = key.ToString();
            object value = propertiesThatChanged[key];

            // 汎用イベント
            OnPropertyChanged?.Invoke(keyString, value);

            // フラグイベント
            if (PhotonRoomPropertyKeys.TryParseFlagKey(keyString, out FlagType flagType) && value is bool boolValue)
            {
                OnFlagChanged?.Invoke(flagType, boolValue);
                Debug.Log($"[GameStateService] Flag Changed: {flagType} = {boolValue}");
            }
        }
    }

    // IInRoomCallbacksの他のメソッド（未使用）
    public void OnPlayerEnteredRoom(Player newPlayer)
    {
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
    }

    #endregion
}