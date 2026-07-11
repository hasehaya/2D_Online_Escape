# RunconaLib.Localization

キーに対応する日本語・英語を保持する汎用ローカライズテーブルです。

## フォルダ

```text
Localization/
├─ Runtime/
│  └─ LocalizationTable.cs
└─ README.md
```

名前空間は `RunconaLib.Localization` です。

`RunconaLib.Localization.asmdef`により独立したRuntimeアセンブリとしてコンパイルされます。

## LocalizationTableの作成

UnityのProjectウィンドウから次を選択します。

`Create > Escape > Localization > Table`

```csharp
using RunconaLib.Localization;

string japanese = table.Get("ui.create", 0);
string english = table.Get("ui.create", 1);
```

言語番号は `0 = 日本語`、`1 = English` です。キーが存在しない場合はキー自身を返します。

このプロジェクトでは、シーン間の言語選択と `PlayerPrefs` 保存をゲーム側の `LocalizationManager` が担当します。

## Spreadsheetとの連携

`RunconaLib.Spreadsheet` のLocalization CSV Importerから、このテーブルを更新できます。

```csv
key,ja,en
ui.create,部屋を作る,Create
ui.close,閉じる,Close
```

## 単独移植

`Assets/RunconaLib/Localization` をフォルダ単位でコピーしてください。Spreadsheet取込機能を使わない場合、このライブラリ単独で利用できます。
