# Dungeon ライツアウトパズル仕様

## 概要

- Noel 側で 3x3 のボタンを押し、Elias 側の水晶に 9 個のライト状態を投影する協力ギミック。
- Noel 側のボタン自体は点灯状態を表示しない。押した瞬間だけボタン差分 Sprite に切り替えて、入力したことだけを示す。
- Elias 側の水晶表示が現在のライト状態を表示する。
- 9 個すべてが点灯状態になったらクリアとする。

## 配置

### Noel 側

- 配置先: `Assets/Scripts/Map/Noel/Dungeon`
- 管理クラス: `GimmickLightsOutPuzzle`
- セルクラス: `LightsOutPuzzleCell`

### Elias 側

- 配置先: `Assets/Scripts/Map/Elias/Dungeon`
- 表示クラス: `LightsOutCrystalProjection`
- 想定用途: 水晶オブジェクト上に Noel 側のライツアウト状態を映す。

## パズルルール

- 盤面は 3x3、合計 9 マス。
- 配列 index は左上から右下への行優先順とする。
  - `0,1,2`
  - `3,4,5`
  - `6,7,8`
- Noel 側で任意のセルを押すと、押したセルと上下左右の隣接セルを反転する。
- 盤面状態は `bool[9]` として管理する。
- 正解状態は Inspector の `_answerState` で指定する。現行初期値は全マス `true`。
- 初期状態は Inspector の `_initialState` で指定する。

## Noel 側の表示仕様

- Noel 側はライトの on/off を見せない。
- `LightsOutPuzzleCell` は以下のみを担当する。
  - クリック/タップを受ける。
  - `UnityEvent<int>` で押された index を通知する。
  - 押下時に `normalSprite` から `pressedSprite` へ一時的に切り替える。
- `LightsOutPuzzleCell` は `GimmickLightsOutPuzzle` を参照しない。
- `GimmickLightsOutPuzzle` が 9 個の `LightsOutPuzzleCell` を持ち、各セルの `OnPressed` を購読する。

## Elias 側の表示仕様

- `LightsOutCrystalProjection` は 9 個の `Image` を持つ。
- 受信した盤面状態に応じて、各 `Image` の Sprite を `offSprite` / `onSprite` で切り替える。
- クリア済みフラグを受け取った場合は、盤面途中状態に関係なく全マスを点灯表示にする。
- クリア時に外部演出をつなげるため、`_onSolved` の `UnityEvent` を持つ。

## 通信仕様

### 途中状態

- 途中状態は Photon Room Custom Properties に int の bit mask として送信する。
- キーは `DungeonLightsOutPuzzleKeys.BoardBits` を使う。
- 現行キー: `Transient.DungeonLightsOutPuzzle.BoardBits`
- bit 対応は盤面 index と同じ。
  - bit 0: 左上
  - bit 1: 上中央
  - bit 2: 右上
  - bit 3: 中左
  - bit 4: 中央
  - bit 5: 中右
  - bit 6: 左下
  - bit 7: 下中央
  - bit 8: 右下
- Noel 側の入力ごとに `GimmickLightsOutPuzzle` が盤面 bit mask を送信する。
- Elias 側の `LightsOutCrystalProjection` は `GameStateService.OnPropertyChanged` で同キーの変更を受け取り、表示を更新する。

### クリア状態

- クリア状態は `FlagType.Dungeon_LightsOutPuzzleCompleted` で管理する。
- Room Custom Properties 上のキーは `Flag_Dungeon_LightsOutPuzzleCompleted`。
- クリア時は Noel 側がクリアフラグを `true` に設定する。
- Elias 側は `GameStateService.OnFlagChanged` で同フラグを受け取り、全点灯表示と `_onSolved` を実行する。

## セーブ仕様

- 途中状態は保存しない。
- `PairSaveCoordinator` は `Transient.` で始まる Room Custom Properties を保存対象から除外する。
- クリア済みかどうかのみ保存する。
- `Flag_Dungeon_LightsOutPuzzleCompleted` は通常の shared progress として保存される。
- ロード時にクリアフラグが `true` の場合:
  - Noel 側は入力済み扱いにして、正解状態を再送信する。
  - Elias 側は全点灯表示にする。

## Inspector 設定

### Noel: `GimmickLightsOutPuzzle`

- `_cells` に 9 個の `LightsOutPuzzleCell` を左上から右下の行優先順で設定する。
- `_initialState` に開始時の盤面を設定する。
- `_answerState` にクリア条件を設定する。基本は全マス `true`。
- `_completedFlag` は `Dungeon_LightsOutPuzzleCompleted` のままにする。
- `_boardBitsKey` は原則 `DungeonLightsOutPuzzleKeys.BoardBits` の既定値を使う。

### Noel: `LightsOutPuzzleCell`

- `_targetImage` にボタン表示用 Image を設定する。
- `_normalSprite` に通常時 Sprite を設定する。
- `_pressedSprite` に押下時の暗い差分 Sprite を設定する。
- `_pressedDuration` に押下差分の表示時間を設定する。

### Elias: `LightsOutCrystalProjection`

- `_lightImages` に水晶上の 9 個のライト Image を左上から右下の行優先順で設定する。
- `_offSprite` に消灯 Sprite を設定する。
- `_onSprite` に点灯 Sprite を設定する。
- `_onSolved` にクリア時演出や後続進行を接続する。

## 実装ファイル

- `Assets/Scripts/Map/Noel/Dungeon/GimmickLightsOutPuzzle.cs`
- `Assets/Scripts/Map/Noel/Dungeon/LightsOutPuzzleCell.cs`
- `Assets/Scripts/Map/Elias/Dungeon/LightsOutCrystalProjection.cs`
- `Assets/Scripts/Map/DungeonLightsOutPuzzleKeys.cs`
- `Assets/Scripts/FlagType.cs`
- `Assets/Scripts/Save/PairSaveCoordinator.cs`
