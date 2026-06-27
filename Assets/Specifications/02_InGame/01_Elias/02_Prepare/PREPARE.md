# Elias Prepare 仕様

## 概要

- Elias Prepare では、ピアノ入力とプランター成長ギミックを担当する。
- プランターは成長ブースターの選択順でルートを確定し、成熟後にアイテムを収穫できる。

## 実装配置

- 実装先: `Assets/Scripts/Map/Elias/Prepare`
- ピアノ: `GimmickPiano` / `PianoKey`
- プランター: `GimmickPlanter` / `GimmickGrowthBooster` / `PlantRouteData`

## ピアノ

- `PianoKey` はクリック時に押下表示を一時的に有効化し、親の `GimmickPiano` に key index を通知する。
- `GimmickPiano` は `_correctSequence` と現在入力列を比較する。
- 入力途中で不一致になった場合、`_resetOnWrongKey` が true なら入力列をリセットする。
- 正しい順序ですべて押されると `_onUnlocked` を実行する。

## プランター

- `GimmickGrowthBooster` は画面上のブースターや水やりを選択状態にする。
- `GimmickPlanter` は選択中の `GimmickGrowthBooster` を受け取り、`BoosterActionType` の履歴で成長ルートを判定する。
- 1回目の入力で `PlantRouteData` から候補ルートを確定する。
- 2回目で成長期に遷移し、`Leaf` 子オブジェクトの Image に `GrowingSprite` を表示する。
- 4回目で成熟期に遷移し、`MatureSprite` を表示する。
- 成熟期のプランターをクリックすると `ResultItem` をインベントリへ追加し、プランターを初期状態に戻す。
- 手順が不一致の場合は枯れ扱いとして初期状態へ戻す。

## サウンド

- 成長期: `SESoundType.PlanterGrow`
- 成熟期: `SESoundType.PlanterMature`
- 手順失敗: `SESoundType.PlanterFail`
- 収穫: `SESoundType.PlanterHarvest`

## 実装ファイル

- `Assets/Scripts/Map/Elias/Prepare/GimmickPiano.cs`
- `Assets/Scripts/Map/Elias/Prepare/PianoKey.cs`
- `Assets/Scripts/Map/Elias/Prepare/GimmickGrowthBooster.cs`
- `Assets/Scripts/Map/Elias/Prepare/GimmickPlanter.cs`
- `Assets/Scripts/Map/Elias/Prepare/PlantRouteData.cs`
- `Assets/Scripts/Audio/SoundType.cs`
