# 2人協力型脱出ゲーム - マッチングシステムセットアップガイド

## 作成したスクリプト

1. **TitleController.cs** - タイトル画面の制御
2. **MatchingRoomController.cs** - マッチングルームの制御
3. **GameManager.cs** - ゲーム本編の制御（更新版）

## 必要なシーン構成

### 1. TitleScene（タイトル画面）

#### 必要なUI要素:
- **Canvas**
  - **RoomNameInputField** (TMP_InputField) - 部屋名入力欄
  - **CreateRoomButton** (Button) - 部屋作成ボタン
  - **JoinRoomButton** (Button) - 部屋参加ボタン
  - **StatusText** (TextMeshProUGUI) - ステータス表示
  - **ConnectingPanel** (GameObject, 任意) - 接続中表示パネル

#### セットアップ手順:
1. 新規シーン「TitleScene」を作成
2. Canvas を作成（UI > Canvas）
3. 空のGameObjectを作成し、「TitleManager」と命名
4. TitleController スクリプトをアタッチ
5. 以下のUI要素を作成:
   ```
   Canvas
   ├── Panel (背景)
   ├── Title Text (TextMeshProUGUI) - "2人協力脱出ゲーム"
   ├── RoomNameInputField (TMP_InputField) - Placeholder: "部屋名を入力"
   ├── CreateRoomButton (Button) - Text: "部屋を作成"
   ├── JoinRoomButton (Button) - Text: "部屋に参加"
   ├── StatusText (TextMeshProUGUI)
   └── ConnectingPanel (Optional)
   ```
6. TitleController の Inspector で各UI要素を割り当て

---

### 2. MatchingRoom（マッチングルーム）

#### 必要なUI要素:
- **Canvas**
  - **RoomNameText** (TextMeshProUGUI) - 部屋名表示
  - **Player1NameText** (TextMeshProUGUI) - プレイヤー1の名前
  - **Player2NameText** (TextMeshProUGUI) - プレイヤー2の名前
  - **Player1ReadyIcon** (GameObject/Image) - プレイヤー1の準備完了アイコン
  - **Player2ReadyIcon** (GameObject/Image) - プレイヤー2の準備完了アイコン
  - **ReadyButton** (Button) - OK/キャンセルボタン
  - **ReadyButtonText** (TextMeshProUGUI) - ボタンのテキスト
  - **LeaveRoomButton** (Button) - 退室ボタン
  - **StatusText** (TextMeshProUGUI) - ステータス表示

#### セットアップ手順:
1. 新規シーン「MatchingRoom」を作成
2. Canvas を作成
3. 空のGameObjectを作成し、「MatchingRoomManager」と命名
4. MatchingRoomController スクリプトをアタッチ
5. **重要**: MatchingRoomManager に **PhotonView** コンポーネントを追加
   - View ID: 1 (任意の固定値)
   - Observed Components: MatchingRoomController を追加
6. 以下のUI要素を作成:
   ```
   Canvas
   ├── RoomNameText (TextMeshProUGUI) - Top
   ├── PlayersPanel
   │   ├── Player1Panel
   │   │   ├── Player1NameText (TextMeshProUGUI)
   │   │   └── Player1ReadyIcon (Image) - 初期状態: 非表示
   │   └── Player2Panel
   │       ├── Player2NameText (TextMeshProUGUI)
   │       └── Player2ReadyIcon (Image) - 初期状態: 非表示
   ├── ReadyButton (Button)
   │   └── ReadyButtonText (TextMeshProUGUI) - "OK"
   ├── LeaveRoomButton (Button) - Text: "退室"
   └── StatusText (TextMeshProUGUI) - Bottom
   ```
7. MatchingRoomController の Inspector で各UI要素を割り当て

---

### 3. GameScene（ゲーム本編）

#### セットアップ手順:
1. 既存の「SampleScene」を「GameScene」にリネーム（または新規作成）
2. 空のGameObjectを作成し、「GameManager」と命名
3. GameManager スクリプトをアタッチ
4. スポーンポイントを設定:
   ```
   Hierarchy
   ├── GameManager (GameManager.cs)
   ├── Player1SpawnPoint (Empty GameObject)
   └── Player2SpawnPoint (Empty GameObject)
   ```
5. GameManager の Inspector で:
   - Player1SpawnPoint を割り当て
   - Player2SpawnPoint を割り当て
   - Player Prefab Name: "NetworkObject"

---

## Build Settings の設定

1. **File > Build Settings** を開く
2. 以下のシーンを順番に追加:
   - TitleScene (Index 0)
   - MatchingRoom (Index 1)
   - GameScene (Index 2)

---

## Photon設定の確認

1. **PhotonServerSettings** の確認:
   - AppIdRealtime が設定されているか確認
   - RpcList に "UpdatePlayerReady" が含まれているか確認

2. **自動シーン同期の有効化**:
   - PhotonNetwork.AutomaticallySyncScene = true がデフォルトで有効

---

## 動作の流れ

1. **TitleScene**: プレイヤーが部屋名を入力して「部屋を作成」または「部屋に参加」
2. **MatchingRoom**: 2人が揃うまで待機
3. 両方のプレイヤーが「OK」ボタンを押す
4. **GameScene**: ゲーム本編が開始

---

## テスト方法

### エディタでテスト:
1. ビルドを作成（File > Build And Run）
2. エディタでPlayモードに入る
3. ビルド版で同じ部屋名を入力して参加
4. 両方で「OK」ボタンを押す

### 2つのエディタインスタンスでテスト:
1. ParrelSync（アセットストア）をインストール
2. クローンプロジェクトを作成
3. 両方のエディタで同時にテスト

---

## トラブルシューティング

### 問題: 部屋に参加できない
- Photonに接続されているか確認（Consoleログを確認）
- 部屋名が正確に一致しているか確認
- 部屋が満員（2人）になっていないか確認

### 問題: OKボタンが押せない
- 2人揃っているか確認
- MatchingRoomController にPhotonViewがアタッチされているか確認

### 問題: ゲームシーンに移動しない
- Build Settingsに"GameScene"が追加されているか確認
- マスタークライアントがシーンをロードしているか確認（Consoleログ）

### 問題: プレイヤーがスポーンしない
- Resources フォルダに "NetworkObject" プレハブがあるか確認
- NetworkObject に PhotonView と PunPlayer がアタッチされているか確認

---

## 次のステップ

1. ゲーム本編のギミック実装
2. 協力要素の追加（パズル、鍵、ドアなど）
3. UI/UXの改善
4. サウンド/BGMの追加
5. プレイヤーのアニメーション
6. チャット機能の追加（任意）

---

## 注意事項

- PhotonのCCU（同時接続数）制限に注意
- 本番環境では適切なエラーハンドリングを追加
- セキュリティ対策（部屋のパスワード機能など）を検討

