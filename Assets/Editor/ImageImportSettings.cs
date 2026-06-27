using UnityEditor;
using UnityEngine;

public class ImageImportSettings : AssetPostprocessor
{
    private const string ImagesPath = "Assets/Images/";

    void OnPreprocessTexture()
    {
        // Only process textures in the Images folder
        if (!assetPath.StartsWith(ImagesPath))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;

        // Set texture type to Sprite (2D and UI)
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;

        // Optional: Set common sprite settings
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        Debug.Log($"[ImageImportSettings] Set '{assetPath}' to Sprite (2D and UI)");
    }
}