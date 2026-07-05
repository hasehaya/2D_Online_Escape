using UnityEditor;
using UnityEngine;

public class ImageImportSettings : AssetPostprocessor
{
    private const string ImagesPath = "Assets/Images/";

    void OnPreprocessTexture()
    {
        // Imagesフォルダ内のテクスチャだけを処理する
        if (!assetPath.StartsWith(ImagesPath))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;

        // テクスチャタイプをSprite（2DとUI）に設定する
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;

        // 必要に応じて共通のSprite設定を行う
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        Debug.Log($"[ImageImportSettings] Set '{assetPath}' to Sprite (2D and UI)");
    }
}