# Noel Prepare 仕様

## 概要

- Noel Prepare では、大釜、スライドパズル、本棚、炎、破棄オブジェクトを担当する。
- 共通のインベントリやセーブ仕様は `../../../00_Common/COMMON_SYSTEMS.md` を参照。

## 実装配置

- 実装先: `Assets/Scripts/Map/Noel/Prepare`
- 大釜: `GimmickCauldron`
- スライドパズル: `GimmickSlidePazzle` / `SlidePazzlePiece`
- 本棚: `BookShelfObject`
- 炎: `SpriteLoopAnimator`
- 破棄: `TrashObject`

## 大釜

- `GimmickCauldron` は2つの投入手順と最後の瓶詰めで完成品を生成する。
- 1回目の投入でルートを確定し、Image の色を `_firstStateColor` に切り替える。
- 2回目の投入で `_secondStateColor` に切り替え、瓶詰めフェーズへ進む。
- 完成後に対応する空瓶を使用すると完成品の `ItemType` をインベントリに追加する。
- 誤ったアイテムを使うと初期状態に戻る。

## スライドパズル

- `GimmickSlidePazzle` は `WoodBox_Close` 上で 2x3 のスライド操作を行う。
- 初期並び: `1,2,3 / 4,5,6`
- 空白: `6`
- 正解並び: `3,4,5 / 1,2,6`
- 同一行または同一列の複数ピースをまとめて押し出す移動に対応する。
- 正解時は `FadeSwitchService` で閉じた箱から開いた箱へ表示を切り替える。

## 本棚 / 炎 / 破棄

- `BookShelfObject` は `SaveableBehaviour` を継承し、本棚が左へスライドした状態を保存する。
- `SpriteLoopAnimator` は複数 Sprite をフェード付きで順番に切り替え、炎などのループ表現に使う。
- `TrashObject` は選択中アイテムが破棄可能な場合だけインベントリから削除し、結果ごとの UnityEvent を実行する。

## サウンド

- 大釜投入: `SESoundType.CauldronInsert`
- 大釜失敗: `SESoundType.CauldronFail`
- 大釜完成: `SESoundType.CauldronComplete`
- スライドパズル正解: `SESoundType.Correct`

## 実装ファイル

- `Assets/Scripts/Map/Noel/Prepare/GimmickCauldron.cs`
- `Assets/Scripts/Map/Noel/Prepare/GimmickSlidePazzle.cs`
- `Assets/Scripts/Map/Noel/Prepare/SlidePazzlePiece.cs`
- `Assets/Scripts/Map/Noel/Prepare/BookShelfObject.cs`
- `Assets/Scripts/Map/Noel/Prepare/SpriteLoopAnimator.cs`
- `Assets/Scripts/Map/Noel/Prepare/TrashObject.cs`
- `Assets/Scripts/FadeSwitchService.cs`
- `Assets/Scripts/Audio/SoundType.cs`
