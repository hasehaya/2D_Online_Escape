# RunconaLib.Spreadsheet

CSV解析とGoogle SpreadsheetのCSV取得URL生成、および用途非依存のEditor Importer基底クラスを提供します。
ローカライズやゲーム固有データの形式は扱いません。

## 構成

```text
Spreadsheet/
├─ Runtime/
│  ├─ CsvReader.cs
│  └─ GoogleSheetsCsv.cs
└─ Editor/
   └─ CsvImportWindow.cs
```

`CsvImportWindow`を継承し、出力先のGUIと`ImportCsv`だけを実装すると、Google Sheetからの取得とローカルCSV選択を共通利用できます。
ローカライズ用Importerは`RunconaLib.Localization/Editor`、ゲーム固有Importerはプロジェクト側に置きます。
