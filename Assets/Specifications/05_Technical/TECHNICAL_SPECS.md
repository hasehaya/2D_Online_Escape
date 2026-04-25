# 技術仕様（現行実装）

## 開発環境
- Engine: Unity
- Language: C#
- 主要パッケージ: `com.unity.inputsystem`, `com.unity.ugui`, `TextMeshPro`

## ネットワーク
- 通信ライブラリ: **Photon PUN2**
- `PhotonNetwork` を用いて接続・ルーム作成・参加・シーン遷移を実装
- ルーム状態同期:
  - `GameStateService` で Room Custom Properties を管理
  - `Float` / `Bool` / `Enum` / `FlagType` の同期に対応

## ビルドシーン（EditorBuildSettings）
1. `Assets/Scenes/TitleScene.unity`
2. `Assets/Scenes/MatchingRoom.unity`
3. `Assets/Scenes/Game_Elias.unity`
4. `Assets/Scenes/Game_Noel.unity`

## 音響・設定
- `AudioManager` による BGM/SE 音量管理
- 設定値は `PlayerPrefs` に保存

## 既知の未実装領域
- Steamworks 連携
- 統合的なリザルトシステム
- 言語切替ロジック本体（設定UIは存在）
