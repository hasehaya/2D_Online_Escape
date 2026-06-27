# サウンド仕様

## 概要

- SE / BGM の種類は `Assets/Scripts/Audio/SoundType.cs` の enum で管理する
- `Assets/Scripts/Audio/AudioDatabase.cs` の AudioDatabase に各 enum 値に対応するクリップをアサインする
- 追加ルールは `05_Technical/TECHNICAL_SPECS.md` を参照

## SESoundType 一覧

| 値 | 定数名 | 用途 |
|---:|---|---|
| 0 | `Correct` | 正解時の汎用SE |
| 1 | `CorrectBoxOpen` | 宝箱を開けたときのSE |
| 2 | `CauldronInsert` | 釜に正解のアイテムを投入したときのSE |
| 3 | `CauldronFail` | 釜の手順を間違えたときのSE |
| 4 | `CauldronComplete` | 釜での調合が完了し、瓶詰めしたときのSE |
| 5 | `PlanterGrow` | 種や苗木が成長期になったときのSE |
| 6 | `PlanterMature` | 植物が成熟期になった（完成した）ときのSE |
| 7 | `PlanterFail` | 手順を間違えて植物が枯れたときのSE |
| 8 | `PlanterHarvest` | 成熟した植物からアイテムを収穫したときのSE |

## BGMSoundType 一覧

| 値 | 定数名 | 用途 |
|---:|---|---|
| *(未登録)* | | |
