using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 言語インデックスを保持し、簡易ローカライズ文字列を提供する。
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    private const string LanguageKey = "Language";

    public event Action<int> OnLanguageChanged;

    public int CurrentLanguageIndex { get; private set; }

    private readonly Dictionary<string, string> _ja = new Dictionary<string, string>
    {
        ["title.connecting"] = "Photonに接続中...",
        ["title.connecting_lobby"] = "ロビーに接続中...",
        ["title.ready"] = "部屋を作成するか、IDを入力して参加してください",
        ["title.create_room"] = "部屋「{0}」を作成中...",
        ["title.search_room"] = "既存の部屋を検索中...",
        ["title.input_room_id"] = "部屋IDを入力してください",
        ["title.room_id_invalid"] = "部屋IDは6桁の数字です",
        ["title.joining_room"] = "部屋「{0}」に参加中...",
        ["title.joined_room"] = "部屋に参加しました！マッチングルームに移動中...",
        ["title.create_room_failed"] = "部屋作成に失敗しました。もう一度お試しください。",
        ["title.no_room_found"] = "参加できる部屋が見つかりませんでした。部屋を作成してください。",
        ["title.join_room_failed"] = "部屋参加失敗: {0}\n（IDが間違っているか、満員です）",
        ["title.connect_error"] = "接続エラー: {0}",
        ["ui.create"] = "部屋作成",
        ["ui.join"] = "参加",
        ["ui.settings"] = "設定",
        ["ui.close"] = "閉じる",
        ["matching.all_joined"] = "全員揃いました！準備ができたらOKボタンを押してください",
        ["matching.player_left"] = "プレイヤーが退室しました。新しいプレイヤーを待っています...",
        ["matching.waiting_players"] = "プレイヤーを待っています... ({0}/2)",
        ["matching.starting"] = "ゲームを開始します...",
        ["matching.leave_room"] = "部屋から退出中...",
        ["matching.waiting"] = "待機中...",
        ["matching.ready_ok"] = "OK",
        ["matching.ready_cancel"] = "キャンセル"
    };

    private readonly Dictionary<string, string> _en = new Dictionary<string, string>
    {
        ["title.connecting"] = "Connecting to Photon...",
        ["title.connecting_lobby"] = "Connecting to lobby...",
        ["title.ready"] = "Create a room or enter Room ID to join.",
        ["title.create_room"] = "Creating room \"{0}\"...",
        ["title.search_room"] = "Searching for available rooms...",
        ["title.input_room_id"] = "Please enter Room ID.",
        ["title.room_id_invalid"] = "Room ID must be 6 digits.",
        ["title.joining_room"] = "Joining room \"{0}\"...",
        ["title.joined_room"] = "Joined room! Moving to matching room...",
        ["title.create_room_failed"] = "Failed to create room. Please try again.",
        ["title.no_room_found"] = "No joinable room found. Please create a room.",
        ["title.join_room_failed"] = "Failed to join room: {0}\n(Check ID or room capacity.)",
        ["title.connect_error"] = "Connection error: {0}",
        ["ui.create"] = "Create",
        ["ui.join"] = "Join",
        ["ui.settings"] = "Settings",
        ["ui.close"] = "Close",
        ["matching.all_joined"] = "Both players are here! Press OK when ready.",
        ["matching.player_left"] = "A player left the room. Waiting for another player...",
        ["matching.waiting_players"] = "Waiting for players... ({0}/2)",
        ["matching.starting"] = "Starting game...",
        ["matching.leave_room"] = "Leaving room...",
        ["matching.waiting"] = "Waiting...",
        ["matching.ready_ok"] = "OK",
        ["matching.ready_cancel"] = "Cancel"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CurrentLanguageIndex = PlayerPrefs.GetInt(LanguageKey, 0);
    }

    public void SetLanguage(int index)
    {
        int clampedIndex = Mathf.Clamp(index, 0, 1);
        if (CurrentLanguageIndex == clampedIndex)
        {
            return;
        }

        CurrentLanguageIndex = clampedIndex;
        PlayerPrefs.SetInt(LanguageKey, CurrentLanguageIndex);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke(CurrentLanguageIndex);
    }

    public string Get(string key)
    {
        Dictionary<string, string> table = CurrentLanguageIndex == 1 ? _en : _ja;
        if (table.TryGetValue(key, out string value))
        {
            return value;
        }

        return key;
    }
}