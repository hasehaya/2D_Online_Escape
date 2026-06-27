# InGame 共通仕様

## 目的

- InGame の共通操作、UI、インベントリ、セーブ、共有同期の仕様をまとめる。
- 役割別ギミックは `01_Elias` / `02_Noel` 配下に分け、各キャラ内で `01_Wake` / `02_Prepare` / `03_Dungeon` に分けて管理する。

## 仕様書構成

- `00_Common/COMMON_SYSTEMS.md`: 共通システム
- `01_Elias/01_Wake/WAKE.md`: Elias Wake 仕様
- `01_Elias/02_Prepare/PREPARE.md`: Elias Prepare 仕様
- `01_Elias/03_Dungeon/DUNGEON.md`: Elias Dungeon 仕様
- `02_Noel/01_Wake/WAKE.md`: Noel Wake 仕様
- `02_Noel/02_Prepare/PREPARE.md`: Noel Prepare 仕様
- `02_Noel/03_Dungeon/LIGHTS_OUT_PUZZLE.md`: Noel Dungeon 仕様

## 共通操作

- 操作: マウス中心。クリックで `InteractableObject` を実行する。
- 視点: `ViewController` で左右移動 / ズーム / 戻るを管理する。
- 会話: `StillNode` + `DialogueController` で連続表示する。
- Zoom: `ZoomObject` と `IViewable` 系の View/Still を使って表示を切り替える。

## 共通 UI / Scene 構成

- `Game_Elias`: `WorldCanvas` + `Assets/Prefabs/UICanvas.prefab` + `Assets/Prefabs/Manager.prefab` 構成。
- `Game_Noel`: `GlobalMap` + `Assets/Prefabs/UICanvas.prefab` + `Assets/Prefabs/Manager.prefab` 構成。
- 両シーンとも `Map` 配下を `Wake` / `WakeStill` / `Prepare` / `PrepareStill` / `Dungeon` の5ブロックで管理する。
- 共通 UI は `RightArrow` / `LeftArrow` / `BackArrow` / `DialoguePanel` / `ItemBox` を持つ。

## インベントリ

- 実装フォルダ: `Assets/Scripts/Escape`
- 主なクラス: `ItemDatabase` / `InventoryManager` / `InventorySlot` / `ItemData` / `ItemType` / `ItemZoomPanel`
- `ItemDatabase` は ScriptableObject で、`ItemData` の List を Inspector からインライン編集して定義する。
- `ItemData` は `ItemType` enum と `Sprite icon` を持つ。
- `InventoryManager` は所持状態を `ItemType` で管理し、表示時に `ItemDatabase` から icon を引く。
- `InventorySlot` は `Assets/Prefabs/ItemSlot.prefab` 上の View コンポーネントとして、アイコン表示、タップ通知、選択表示のみを担当する。
- 所持数が減っても既存スロット欄は消さず、空のスロットも枠だけ残して UI レイアウトを維持する。
- `ItemType.MagicSack` を取得すると、通信相手と共有する追加スロットを1枠だけアンロックする。
- 共有スロットは通常の `ItemSlot` と同じ UI / 選択 / 使用 / 削除処理で扱う。
- 共有スロットの中身は Room Custom Properties でリアルタイム同期する。

## インタラクト

- 基底: `InteractableObject`
- 取得: `PickupObject` は取得後に対象を非表示にする。
- 要求: `ItemRequireObject` は指定アイテムを選択状態で使用するとイベントを発火する。
- 消費 + 有効化: `ItemConsumeActivateObject` は指定アイテムを消費して対象オブジェクトを有効化し、状態を保存する。
- 捨てる: `TrashObject` は選択中アイテムが破棄可能な場合だけ削除する。

## セーブ

- 実装フォルダ: `Assets/Scripts/Save`
- 基底: `SaveableBehaviour` + `ISaveable`
- 集約: `PairSaveCoordinator`
- データ単位は `PairSaveData` とし、ペア識別子、Elias/Noel の役割固定ID、クリア済み状態、役割別保存データ、共有進行を持つ。
- 保存対象:
  - `ItemType` の所持一覧
  - Room Custom Properties の共有進捗
  - 現在ステージ（シーン名）
  - `SaveableBehaviour` の状態 JSON
- 通常インベントリは各プレイヤー個別の所持一覧として保存する。
- MagicSack の共有スロットは Room Custom Properties 側の共有進捗として保存し、ペア再開時も同じ共有枠として復元する。
- Room Custom Properties のうち、保存する共有進行は `PhotonRoomPropertyKeys.IsPersistentSharedProgressKey` で判定する。
- `Transient.` で始まる一時同期値、`Save.*` のセッション情報、表示用リアルタイム値は保存対象から除外する。
- ペアの `isCleared` はクリア後の再プレイ判定に使う。通常保存では `true` を `false` に戻さない。

## 実装ファイル

- `Assets/Scripts/Escape/ViewController.cs`
- `Assets/Scripts/Escape/ViewNode.cs`
- `Assets/Scripts/Escape/StillNode.cs`
- `Assets/Scripts/Escape/DialogueController.cs`
- `Assets/Scripts/Escape/InventoryManager.cs`
- `Assets/Scripts/Escape/InventorySlot.cs`
- `Assets/Scripts/Escape/ItemDatabase.cs`
- `Assets/Scripts/Escape/ItemType.cs`
- `Assets/Scripts/Escape/InteractableObject.cs`
- `Assets/Scripts/Escape/ItemRequireObject.cs`
- `Assets/Scripts/Escape/ItemConsumeActivateObject.cs`
- `Assets/Scripts/Escape/PickupObject.cs`
- `Assets/Scripts/Save/PairSaveCoordinator.cs`
