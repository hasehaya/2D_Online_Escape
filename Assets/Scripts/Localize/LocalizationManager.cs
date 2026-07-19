using System;
using System.Collections.Generic;
using RunconaLib.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>言語選択とローカライズテーブル参照を管理するプロジェクト側の窓口。</summary>
public sealed class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    public const string LanguageKey = "Language";

    [SerializeField] private LocalizationTable _table;
    public event Action<int> OnLanguageChanged;
    public int CurrentLanguageIndex { get; private set; }

    private static readonly Dictionary<string, string[]> Defaults = new Dictionary<string, string[]>
    {
        ["title.connecting"] = new[] { "Photonに接続中...", "Connecting to Photon..." },
        ["title.connecting_lobby"] = new[] { "ロビーに接続中...", "Connecting to lobby..." },
        ["title.ready"] = new[] { "部屋を作成するか、ルームIDを入力して参加してください。", "Create a room or enter a Room ID to join." },
        ["title.create_room"] = new[] { "部屋「{0}」を作成中...", "Creating room \"{0}\"..." },
        ["title.search_room"] = new[] { "参加できる部屋を検索中...", "Searching for available rooms..." },
        ["title.input_room_id"] = new[] { "ルームIDを入力してください。", "Please enter a Room ID." },
        ["title.room_id_invalid"] = new[] { "ルームIDは6桁の数字です。", "Room ID must be 6 digits." },
        ["title.joining_room"] = new[] { "部屋「{0}」に参加中...", "Joining room \"{0}\"..." },
        ["title.joined_room"] = new[] { "部屋に参加しました。待機画面へ移動します...", "Joined room. Moving to matching room..." },
        ["title.create_room_failed"] =
            new[] { "部屋の作成に失敗しました。もう一度お試しください。", "Failed to create room. Please try again." },
        ["title.no_room_found"] = new[] { "参加できる部屋が見つかりませんでした。", "No joinable room was found." },
        ["title.join_room_failed"] = new[] { "部屋への参加に失敗しました: {0}", "Failed to join room: {0}" },
        ["title.connect_error"] = new[] { "接続エラー: {0}", "Connection error: {0}" },
        ["ui.create"] = new[] { "部屋を作る", "Create" }, ["ui.join"] = new[] { "参加", "Join" },
        ["ui.settings"] = new[] { "設定", "Settings" }, ["ui.close"] = new[] { "閉じる", "Close" },
        ["matching.all_joined"] = new[] { "全員揃いました。準備ができたらOKを押してください。", "Both players are here. Press OK when ready." },
        ["matching.player_left"] = new[] { "プレイヤーが退出しました。", "A player left the room." },
        ["matching.waiting_players"] = new[] { "プレイヤーを待っています... ({0}/2)", "Waiting for players... ({0}/2)" },
        ["matching.starting"] = new[] { "ゲームを開始します...", "Starting game..." },
        ["matching.leave_room"] = new[] { "部屋から退出中...", "Leaving room..." },
        ["matching.waiting"] = new[] { "待機中...", "Waiting..." },
        ["matching.ready_ok"] = new[] { "OK", "OK" }, ["matching.ready_cancel"] = new[] { "キャンセル", "Cancel" }
    };

    private readonly Dictionary<TMP_Text, string> _autoLocalizedTexts = new Dictionary<TMP_Text, string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CurrentLanguageIndex = Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 1);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start() => RefreshAllTexts();

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void SetLanguage(int index)
    {
        index = Mathf.Clamp(index, 0, 1);
        if (CurrentLanguageIndex == index) return;
        CurrentLanguageIndex = index;
        PlayerPrefs.SetInt(LanguageKey, index);
        PlayerPrefs.Save();
        RefreshAllTexts();
        OnLanguageChanged?.Invoke(index);
    }

    public string Get(string key) => Get(key, key, key);

    public string Get(string key, string japaneseFallback, string englishFallback)
    {
        if (_table != null)
        {
            string value = _table.Get(key, CurrentLanguageIndex);
            if (value != key) return value;
        }

        if (Defaults.TryGetValue(key, out string[] values)) return values[CurrentLanguageIndex];
        string fallback = CurrentLanguageIndex == 1 ? englishFallback : japaneseFallback;
        return string.IsNullOrEmpty(fallback) ? key : fallback;
    }

    private void HandleSceneLoaded(Scene _, LoadSceneMode __) => RefreshAllTexts();

    private void RefreshAllTexts()
    {
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text text in texts)
        {
            if (text == null || !text.gameObject.scene.IsValid()) continue;
            if (!_autoLocalizedTexts.TryGetValue(text, out string key))
            {
                if (!TryResolveKey(text.text, out key)) continue;
                _autoLocalizedTexts[text] = key;
            }

            text.text = Get(key);
        }

        var destroyed = new List<TMP_Text>();
        foreach (TMP_Text text in _autoLocalizedTexts.Keys)
            if (text == null)
                destroyed.Add(text);
        foreach (TMP_Text text in destroyed) _autoLocalizedTexts.Remove(text);
    }

    private bool TryResolveKey(string text, out string key)
    {
        if (_table != null && _table.TryGetKey(text, out key)) return true;
        foreach (KeyValuePair<string, string[]> pair in Defaults)
        {
            if (pair.Key == text || pair.Value[0] == text || pair.Value[1] == text)
            {
                key = pair.Key;
                return true;
            }
        }

        key = null;
        return false;
    }
}