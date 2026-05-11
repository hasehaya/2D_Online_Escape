# インゲーム仕様（現行実装）

## 共通システム
- 操作: マウス中心（クリックでインタラクト）
- 視点: `ViewController` で左右移動 / ズーム / 戻るを管理
- 会話: `StillNode` + `DialogueController` で連続表示
- アイテム: `InventoryManager` + `InventorySlot`
  - 取得、選択、選択中アイテムの拡大表示
  - `InventoryManager` がデータ管理、スロットUI生成、選択処理、拡大表示UI制御を一括で担当
  - `InventorySlot` は `ItemInventry` プレハブ上のViewコンポーネントとして、タップ通知と選択表示のみを担当
- インタラクト: `InteractableObject`
  - Zoom 遷移
  - Pickup（取得後に対象を非表示）
- セーブ:
  - `SaveableBehaviour` 基底 + `ISaveable` 契約でオブジェクト状態を管理
  - `PairSaveCoordinator` がシーン内 `SaveableBehaviour` と `InventoryManager` を集約保存
  - 保存対象: `Item取得状況`, `Room Custom Properties(謎進捗)`, `現在ステージ(シーン名)`, `InteractableObjectのactive状態`

## シーン構成
- `Game_Elias`: Elias 側のギミックを担当（`WorldCanvas` + `UICanvas` + `Manager` 構成）
- `Game_Noel`: Noel 側のギミックを担当（`GlobalMap` + `UICanvas` + `Manager` 構成）
- 両シーンとも `Map` 配下を `Wake` / `WakeStill` / `Prepare` の3ブロックで管理
- UIは共通して `RightArrow` / `LeftArrow` / `BackArrow` / `DialoguePanel` / `ItemBox` を持つ

## 実装済みギミック

### 共通 / 汎用
- 大釜ギミック（`GimmickCauldron`）
  - 指定されたアイテムを順に投入すると画像が切り替わり、最後に瓶を入れると完成品（アイテム）を取得。
  - 間違ったアイテムを入れると初期状態にリセット。
- アイテム要求ギミック（`ItemRequireObject`）
  - 指定されたアイテムを選択状態で使用するとイベントを発火。

### Elias 側
- レーザーギミック（`GimmickLaser`）
  - ハンドル操作で交点を調整
  - ターゲットを順番に正解して進行
  - 進捗を Room Custom Properties に共有
- ピアノギミック（`GimmickPiano` / `PianoKey`）
  - 鍵盤の入力順を判定し、正解時にイベント発火
- 覚醒演出（`NoelAwakeEvent`）
  - フェード演出後に Still へ遷移

### Noel 側
- 心電図ギミック（`Electrocardiogram`）
  - 共有された距離割合から心拍表示を更新
  - フラグ変化に応じて進行イベントを発火
- 視線上げ演出（`NoelLookUpEvent`）
  - まぶた演出・カメラ移動後に Still へ遷移
- スライドパズル（`GimmickSlidePazzle` / `SlidePazzlePiece`）
  - `WoodBox_Close` 上で 2x3 のスライド操作を行う（初期並び: 1,2,3 / 4,5,6）
  - 空白は `6`、正解並びは `3,4,5 / 1,2,6`
  - 同一行・同一列の複数ピースをまとめて押し出す移動に対応
  - 正解時に `WoodBox_Open` を表示して `WoodBox_Close` を閉じる

## 現時点の前提
- 非対称協力（役割分離）を前提に進行する。
- クリア後の専用リザルト画面は未実装。
