# 2D_Online_Escape

## GitHub ActionsによるWebGLビルド

`.github/workflows/build-webgl.yml` は、タグをPushしたときだけUnityのWebGLビルドを実行し、生成物をActionsのArtifactとして保存します。

初回のみ、GitHubリポジトリの `Settings > Secrets and variables > Actions` にUnityライセンス情報を登録してください。

- Unity Personal: `UNITY_LICENSE`
- Unity Pro / Plus: `UNITY_EMAIL`、`UNITY_PASSWORD`、`UNITY_SERIAL`

ビルドを開始するには、例えば次のようにバージョンタグをPushします。

```bash
git tag v1.0.0
git push origin v1.0.0
```

完了後はリポジトリの `Actions` タブから、タグ名のArtifact `WebGL-<タグ名>` をダウンロードできます。

## Important

- `.meta` / `.prefab` などの Unity YAML アセットは、参照関係や設定値を確認するために読むのは可。
- ただし、これらの YAML ファイルを直接編集して仕様や実装を変更しないこと。
- 変更が必要な場合は、Unity の Inspector 操作または C# スクリプト修正を優先する。
