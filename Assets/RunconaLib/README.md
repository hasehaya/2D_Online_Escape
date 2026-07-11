# RunconaLib

Unityプロジェクト間で再利用するライブラリ群です。各ライブラリは直下のフォルダ単位で分離・移植できる構成にしています。

## 構成

```text
Assets/RunconaLib/
├─ Localization/
│  ├─ Runtime/
│  └─ README.md
├─ Audio/
│  ├─ Runtime/
│  └─ README.md
├─ Spreadsheet/
│  ├─ Runtime/
│  ├─ Editor/
│  └─ README.md
└─ README.md
```

| ライブラリ | 名前空間 | 詳細 |
| --- | --- | --- |
| Localization | `RunconaLib.Localization` | [Localization README](Localization/README.md) |
| Audio | `RunconaLib.Audio` | [Audio README](Audio/README.md) |
| Spreadsheet | `RunconaLib.Spreadsheet` / `RunconaLib.Spreadsheet.Editor` | [Spreadsheet README](Spreadsheet/README.md) |

ゲーム固有の `LocalizationManager`、`StillDialogueCatalog`、`StillNode` はこのフォルダ外に置き、RunconaLibを利用するプロジェクト側の実装として分離しています。

## 移植

すべて移植する場合は `Assets/RunconaLib` をコピーします。個別に移植する場合は `Localization` または `Spreadsheet` をフォルダ単位でコピーしてください。Unity内で移動・コピーするときは `.meta` も一緒に扱ってください。
