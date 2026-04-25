# アウトゲーム仕様（現行実装）

## 画面遷移
1. `TitleScene`
2. `MatchingRoom`
3. `Game_Elias` / `Game_Noel`（役割別シーン）

## TitleScene
- Photon に接続し、部屋の作成 / 参加を行う。
- 作成時はランダム 6 桁 ID を採番。
- 参加時は 6 桁 ID を入力して参加。
- 設定パネルを開くボタンを持つ。

### Editor とビルドの挙動差
- Editor: ロビー経由で既存部屋への参加を補助（テスト向け）。
- ビルド: ルームID入力による参加を前提。

## MatchingRoom
- 部屋ID表示、プレイヤー2名の表示、準備状態表示。
- 各プレイヤーが OK / キャンセルを切替。
- 2人とも準備完了で開始し、部屋をクローズしてゲームへ遷移。
- 退出ボタンで `TitleScene` へ戻る。

## 設定（Settings）
- BGM 音量（PlayerPrefs 永続化）
- SE 音量（PlayerPrefs 永続化）
- 言語インデックス保存（UI文言切替の本実装は未対応）
