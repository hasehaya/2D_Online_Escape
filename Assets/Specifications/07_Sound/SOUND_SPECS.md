# サウンド仕様

## 概要

- SE / BGM の種類は `SoundType.cs` の enum で管理する
- AudioDatabase に各 enum 値に対応するクリップをアサインする
- 追加ルールは `05_Technical/TECHNICAL_SPECS.md` を参照

## SESoundType 一覧

| 値 | 定数名 | 用途 |
|---:|---|---|
| 0 | `Correct` | 正解時の汎用SE |
| 1 | `CorrectBoxOpen` | 宝箱を開けたときのSE |

## BGMSoundType 一覧

| 値 | 定数名 | 用途 |
|---:|---|---|
| *(未登録)* | | |
