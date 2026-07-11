# RunconaLib.Spreadsheet

CSV解析、Google SpreadsheetのCSV URL生成、ローカライズCSVのUnity取込を提供します。

## Assembly Definition

- `RunconaLib.Spreadsheet`: Runtime。ほかのRunconaLibへ依存しません
- `RunconaLib.Spreadsheet.Editor`: Editor専用。`RunconaLib.Spreadsheet`と`RunconaLib.Localization`を参照します

```text
Spreadsheet/
├─ Runtime/
│  ├─ RunconaLib.Spreadsheet.asmdef
│  ├─ CsvReader.cs
│  └─ GoogleSheetsCsv.cs
├─ Editor/
│  ├─ RunconaLib.Spreadsheet.Editor.asmdef
│  └─ LocalizationCsvImporter.cs
└─ README.md
```

## CSV解析

```csharp
using RunconaLib.Spreadsheet;

List<string[]> rows = CsvReader.Parse(csvText);
string url = GoogleSheetsCsv.BuildExportUrl(spreadsheetId, sheetId);
```

## Localization CSV Importer

`Tools > RunconaLib > Import Localization CSV`から開きます。

```csv
key,ja,en
ui.create,部屋を作る,Create
```

## ゲーム固有アダプター

`StillCsvImporter`は`DialogueEntry`と`StillDialogueCatalog`へ依存するため、RunconaLibのasmdefには含めていません。次のプロジェクト側Editorフォルダにあります。

`Assets/Editor/RunconaLibAdapters/Spreadsheet/StillCsvImporter.cs`

この分離により、`RunconaLib.Spreadsheet`および`RunconaLib.Spreadsheet.Editor`から`Assembly-CSharp`への逆依存はありません。
