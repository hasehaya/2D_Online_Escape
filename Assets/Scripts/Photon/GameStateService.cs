using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Photon Room Custom Propertiesを使用してゲーム状態を管理する汎用Serviceクラス。
/// Float、Bool、Enum値をネットワーク越しに同期する。
/// 旧FlagManagerの機能も統合。
/// </summary>
public class GameStateService : MonoBehaviourPunCallbacks
{
    public static GameStateService Instance { get; private set; }

    // 値変更イベント
    public event Action<string, object> OnPropertyChanged;
    public event Action<FlagType, bool> OnFlagChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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

        // MasterClientのみが設定可能
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning($"[GameStateService] MasterClientではありません。Float値 '{key}' を設定できません。");
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

        // MasterClientのみが設定可能
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning($"[GameStateService] MasterClientではありません。Bool値 '{key}' を設定できません。");
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

        // MasterClientのみが設定可能
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning($"[GameStateService] MasterClientではありません。Enum値 '{key}' を設定できません。");
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

        string key = GetFlagKey(flag);
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

        string key = GetFlagKey(flag);
        return GetBool(key);
    }

    /// <summary>
    /// フラグキーの生成（内部用）
    /// </summary>
    private string GetFlagKey(FlagType flag)
    {
        return $"Flag_{flag}";
    }

    #endregion

    #region Photonコールバック

    /// <summary>
    /// Room Custom Propertiesが変更されたときに呼ばれる
    /// </summary>
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        foreach (var key in propertiesThatChanged.Keys)
        {
            string keyString = key.ToString();
            object value = propertiesThatChanged[key];

            // 汎用イベント
            OnPropertyChanged?.Invoke(keyString, value);

            // フラグイベント（Flag_で始まるキー）
            if (keyString.StartsWith("Flag_") && value is bool boolValue)
            {
                string flagName = keyString.Substring(5); // "Flag_"を除去
                if (Enum.TryParse(flagName, out FlagType flagType))
                {
                    OnFlagChanged?.Invoke(flagType, boolValue);
                    Debug.Log($"[GameStateService] Flag Changed: {flagType} = {boolValue}");
                }
            }
        }
    }

    #endregion
}