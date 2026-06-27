# アウトゲーム仕様（現行実装）

## 画面遷移
1. `TitleScene`
2. `MatchingRoom`
3. `Game_Elias` / `Game_Noel`（役割別シーン）

## TitleScene
- Photon に接続し、部屋の作成 / 参加を行う。
- 作成時はランダム 6 桁 ID を採番。
- 参加時は 6 桁 ID を入力して参加。
- 現行シーン上の主要UIは `CreateBtn` / `JoinBtn` / `RoomId` 入力欄 / 接続状態テキスト（`ConnectText`）。
- `SettingsBtn` から設定パネル（`SettingsPanel`）を開閉可能。
- 起動時に暫定ローカルIDを生成し、ペア継続判定に使用する。

### Editor とビルドの挙動差
- Editor: ロビー経由で既存部屋への参加を補助（テスト向け）。
- ビルド: ルームID入力による参加を前提。

## MatchingRoom
- 部屋ID表示、プレイヤー2名の表示、準備状態表示。
- 各プレイヤーが OK / キャンセルを切替。
- 2人とも準備完了で開始し、部屋をクローズしてゲームへ遷移。
- 退出ボタンで `TitleScene` へ戻る。
- 開始時に両プレイヤーの暫定ローカルIDからペアキーを生成する。
- 同一ペアの既存セーブがある場合は前回の役割（Elias/Noel）を維持し、同じシーンへ遷移する。
- 既存セーブがクリア済みの場合は保存スロットとペア索引を削除し、新規ペアと同じくID順で初回役割を決定する。
- 既存セーブがないペアはID順で初回役割を決定し、その役割を以後固定する。

## 設定（Settings）
- `SettingsController` が設定パネルを制御し、必要な UI 要素（Slider / Dropdown / CloseButton）を不足時に動的生成する。
- BGM / SE 音量を `AudioManager` 経由で変更し、`PlayerPrefs` に永続化。
- 言語設定は `LocalizationManager` で管理し、日本語 / 英語の切り替えを反映。
