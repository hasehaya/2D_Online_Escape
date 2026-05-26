# 技術仕様（現行実装）

## 開発環境
- Engine: Unity
- Language: C#
- 主要パッケージ: `com.unity.inputsystem`, `com.unity.ugui`, `com.unity.render-pipelines.universal`, `TextMeshPro`
- 外部ライブラリ: Photon PUN2, DOTween（`DG.Tweening`）

## ネットワーク
- 通信ライブラリ: **Photon PUN2**
- `PhotonNetwork` を用いて接続・ルーム作成・参加・シーン遷移を実装
- ルーム状態同期:
  - `GameStateService` で Room Custom Properties を管理
  - `Float` / `Bool` / `Enum` / `FlagType` の同期に対応

## セーブシステム
- 方式: **B案（`SaveableBehaviour` 基底でID管理一元化）**
- 基底: `SaveableBehaviour` + `ISaveable`
  - 各保存対象オブジェクトは `saveId` を持ち、`CaptureState` / `RestoreState` を実装
- 集約: `PairSaveCoordinator`
  - インベントリ、`SaveableBehaviour` 状態、Room Custom Properties、現在シーン名をペア単位で保存/復元
- 永続化: `PlayerPrefs` に保存スロットキー単位でJSON保存
- ペア識別: 両プレイヤーIDをソートして `pairKey` を生成
- プレイヤーID: Steam連携前は `LocalIdentityProvider` が暫定ローカルIDを発行
- 役割固定: 同一ペアは前回保存された Elias/Noel 割り当てを再利用。初回のみID順で決定
- 保存スロット: `pairKey` と **役割割り当て** を結合したキーで管理する
- 引き継ぎは **同一キャラのみ** 可能とする
- キャラを変更する場合、またはクリア後の再プレイは **最初から（新規スロット）** とする

## ビルドシーン（EditorBuildSettings）
1. `Assets/Scenes/TitleScene.unity`
2. `Assets/Scenes/MatchingRoom.unity`
3. `Assets/Scenes/Game_Elias.unity`
4. `Assets/Scenes/Game_Noel.unity`

## 音響・設定
- `AudioManager` は `TitleScene` に配置し、`DontDestroyOnLoad` で全シーン共有
- `SettingsController` は `SettingsPanel` を制御し、必要UIを不足時に動的生成
- 設定値は `PlayerPrefs` 保存（BGM/SE/Language）
- `LocalizationManager` で言語インデックスを管理し、アウトゲーム文言の一部を多言語化

### enum 追加ルール

- 値は**明示的な整数を必ず付与**し、途中削除による値ずれを防ぐ
- 値は削除せず欠番にする（セーブデータ互換性のため）
- 欠番にした場合はコードにコメントで理由を残す
- 既存値の再採番は禁止する
- **値を追加したらこのテーブルも必ず更新すること**

## プログラミング方針
- 前提条件（Inspector割り当て/仕様上必須の参照・設定）は満たされているものとし、前提不足による Null チェック等の防御的実装は行わない
- 不整合があれば早期にエラーで落ちることを許容する（問題の早期発見を優先）
- 例外: 通信周りのバッファ処理（シリアライズ/デシリアライズ、受信データの解析）は例外処理や検証を多めに行う

## Unityアセット編集ルール
- `.meta` / `.prefab` などの Unity YAML アセットは、参照関係や設定値を確認するために読むのは可
- ただし、これらの YAML を直接編集して仕様・実装を変更しない
- Unity 上での Inspector 操作や C# スクリプトの修正を優先し、YAML は参照追跡用に限定する

## 既知の未実装領域
- Steamworks 連携（暫定ローカルIDからSteamIDへの移行）
- 統合的なリザルトシステム
- インゲーム側UIテキストの多言語化
