# RunconaLib.Audio

BGM・SEの再生、音量管理、`PlayerPrefs`への永続化を提供するゲーム非依存ライブラリです。

## 構成

```text
Audio/
├─ Runtime/
│  ├─ AudioDatabase.cs
│  └─ AudioManager.cs
└─ README.md
```

名前空間は `RunconaLib.Audio` です。

`RunconaLib.Audio.asmdef`により独立したRuntimeアセンブリとしてコンパイルされます。

## 依存関係

Unity標準の `AudioSource`、`AudioClip`、`PlayerPrefs` のみに依存します。ゲーム固有Enum、シーンオブジェクト、他のRunconaLibには依存しません。

## 利用方法

`Create > RunconaLib > Audio > Audio Database` からデータベースを作成し、BGM・SEそれぞれへ文字列IDとAudioClipを登録します。

```csharp
using RunconaLib.Audio;

AudioManager.Instance.PlayBGM("Title");
AudioManager.Instance.PlaySE("Correct");
```

このゲームでは `SESoundType` / `BGMSoundType` をプロジェクト側に残し、`EscapeAudioExtensions` がEnum名を文字列IDへ変換します。このアダプターはRunconaLibには含まれません。

## 音量保存

- `RunconaLib.Audio.BGMVolume`
- `RunconaLib.Audio.SEVolume`

旧バージョンの `BGM_Volume` / `SE_Volume` が存在する場合は、初回起動時に新しいキーへ引き継ぎます。

## シーンを跨ぐ利用

最初のシーンへ `AudioManager` を1つ配置します。子オブジェクトの場合は起動時にルートへ移動し、`DontDestroyOnLoad`で維持します。重複インスタンスは自動的に破棄されます。

## 単独移植

`Assets/RunconaLib/Audio` を `.meta` と一緒にコピーしてください。
