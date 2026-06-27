# Elias Wake 仕様

## 概要

- Elias 側ではレーザーギミックを担当する。
- ハンドル操作で交点を調整し、ターゲットを順番に正解して進行する。
- ターゲット進捗と距離割合を Room Custom Properties 経由で Noel 側へ共有する。

## 実装配置

- 実装先: `Assets/Scripts/Map/Elias/Wake`
- 管理クラス: `GimmickLaser`
- 操作クラス: `LaserHandle`
- 演出クラス: `NoelAwakeEvent`

## レーザーギミック

- `GimmickLaser` は水平 / 垂直ハンドルから交点を算出する。
- 正解対象は `_targetPoints` の順番どおりに判定する。
- `_correctRatioThreshold` 以上まで交点が近づくと、そのターゲットを正解扱いにする。
- 正解時は `_targetCorrectSprite` に切り替え、対応するフラグを `true` にする。
- すべてのターゲットが正解済みになると `Wake_LaserCompleted` を `true` にし、`_onAllCorrect` を実行する。

## 同期

- 距離割合キー: `PhotonRoomPropertyKeys.WakeLaserDistanceRatio`
  - 実値: `LaserDistanceRatio`
- ターゲットフラグ:
  - `FlagType.Wake_LaserTarget1`
  - `FlagType.Wake_LaserTarget2`
  - `FlagType.Wake_LaserTarget3`
  - `FlagType.Wake_LaserCompleted`
- 距離割合は MasterClient のみが一定間隔で更新する。
- Noel 側は `Electrocardiogram` と `GimmickLaserLightView` でこの同期値を受け取る。
- 距離割合は表示用リアルタイム値であり、セーブ対象にしない。正解済み状態はターゲットフラグで保存する。

## 実装ファイル

- `Assets/Scripts/Map/Elias/Wake/GimmickLaser.cs`
- `Assets/Scripts/Map/Elias/Wake/LaserHandle.cs`
- `Assets/Scripts/Map/Elias/Wake/NoelAwakeEvent.cs`
- `Assets/Scripts/Escape/FlagType.cs`
- `Assets/Scripts/Photon/GameStateService.cs`
