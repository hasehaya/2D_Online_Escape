# Elias Dungeon 仕様

## 概要

- Elias Dungeon では、Noel 側のライツアウトパズル状態を水晶表示へ投影する。
- 入力と盤面更新は Noel 側が担当し、Elias 側は Room Custom Properties とクリアフラグを購読して表示を更新する。

## 実装配置

- 実装先: `Assets/Scripts/Map/Elias/Dungeon`
- 表示クラス: `LightsOutCrystalProjection`
- Noel 側入力仕様: `../../../02_Noel/03_Dungeon/LIGHTS_OUT_PUZZLE.md`

## 表示仕様

- `LightsOutCrystalProjection` は 9 個の `Image` を持つ。
- 受信した盤面状態に応じて、各 `Image` の Sprite を `_offSprite` / `_onSprite` で切り替える。
- クリア済みフラグを受け取った場合は、盤面途中状態に関係なく全マスを点灯表示にする。
- クリア時に外部演出をつなげるため、`_onSolved` の `UnityEvent` を持つ。
- `_onSolved` は一度だけ実行する。

## 同期

- 盤面キー: `PhotonRoomPropertyKeys.DungeonLightsOutPuzzleBoardBits`
  - 実値: `Transient.DungeonLightsOutPuzzle.BoardBits`
- クリアフラグ: `FlagType.Dungeon_LightsOutPuzzleCompleted`
- `GameStateService.OnPropertyChanged` で盤面キーの変更を受け取る。
- `GameStateService.OnFlagChanged` でクリアフラグの変更を受け取る。

## Inspector 設定

- `_lightImages` に水晶上の 9 個のライト Image を左上から右下の行優先順で設定する。
- `_offSprite` に消灯 Sprite を設定する。
- `_onSprite` に点灯 Sprite を設定する。
- `_onSolved` にクリア時演出や後続進行を接続する。

## 実装ファイル

- `Assets/Scripts/Map/Elias/Dungeon/LightsOutCrystalProjection.cs`
- `Assets/Scripts/Photon/PhotonRoomPropertyKeys.cs`
- `Assets/Scripts/Escape/FlagType.cs`
- `Assets/Scripts/Photon/GameStateService.cs`
