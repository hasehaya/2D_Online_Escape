# Noel Wake 仕様

## 概要

- Noel Wake では、Elias 側レーザーギミックの進捗を受信して表示・演出に反映する。
- 心電図は距離割合を心拍表示へ変換し、レーザー完了フラグで後続演出へ進む。

## 実装配置

- 実装先: `Assets/Scripts/Map/Noel/Wake`
- 心電図: `Electrocardiogram`
- ライト表示: `GimmickLaserLightView`
- 視線上げ演出: `NoelLookUpEvent`

## 心電図

- `Electrocardiogram` は `PhotonRoomPropertyKeys.WakeLaserDistanceRatio` を読み取り、心拍数を `_minHeartRate` から `_maxHeartRate` の範囲で補間する。
- 心拍数は `_heartRateText` に `{current}/{max}` 形式で表示する。
- `LineRenderer` で ECG 風の波形を描画する。
- `FlagType.Wake_LaserCompleted` が `true` になったら `_targetViewNode` へ遷移し、`_onAllCorrect` を実行する。
- 距離割合は表示用リアルタイム値であり、セーブ対象にしない。完了状態は `FlagType.Wake_LaserCompleted` を保存対象とする。

## ライト表示

- `GimmickLaserLightView` は Elias 側のターゲットフラグを3つのライト表示へ反映する。
- 対象フラグ:
  - `FlagType.Wake_LaserTarget1`
  - `FlagType.Wake_LaserTarget2`
  - `FlagType.Wake_LaserTarget3`
- Sprite、色、任意の on/off GameObject を切り替える。

## 視線上げ演出

- `NoelLookUpEvent` は上まぶた / 下まぶたを DOTween で開く。
- まぶた演出後にカメラを上へ移動し、最後に `StillNode` を表示する。

## 実装ファイル

- `Assets/Scripts/Map/Noel/Wake/Electrocardiogram.cs`
- `Assets/Scripts/Map/Noel/Wake/GimmickLaserLightView.cs`
- `Assets/Scripts/Map/Noel/Wake/NoelLookUpEvent.cs`
- `Assets/Scripts/Escape/FlagType.cs`
- `Assets/Scripts/Photon/GameStateService.cs`
