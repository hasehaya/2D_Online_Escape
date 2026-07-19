# RunconaLib.Localization

キーに対応する日本語・英語を保持する汎用ローカライズテーブルです。

## LocalizationTable

`Create > Escape > Localization > Table`から作成できます。

```csharp
using RunconaLib.Localization;

string japanese = table.Get("ui.create", 0);
string english = table.Get("ui.create", 1);
```

`TryGetKey`では、キー・日本語・英語のいずれからでもキーを逆引きできます。これにより、プロジェクト側の管理クラスからシーン内テキストを自動ローカライズできます。

## CSVインポート

`Tools > RunconaLib > ローカライズCSVをインポート`から、Google SheetまたはローカルCSVを`LocalizationTable`へ取り込めます。

```csv
key,ja,en
ui.create,部屋を作る,Create
ui.close,閉じる,Close
```

CSVの取得処理は`RunconaLib.Spreadsheet`の共通基底を利用しますが、列の意味とテーブル更新処理はLocalization側にあります。
