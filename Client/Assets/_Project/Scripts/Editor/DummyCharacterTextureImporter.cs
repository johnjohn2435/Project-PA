using UnityEditor;
using UnityEngine;

namespace PocketArcade.Editor
{
    public sealed class DummyCharacterTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            const string directory = "Assets/_Project/Art/Characters/Dummy/";
            if (!assetPath.StartsWith(directory, System.StringComparison.Ordinal) ||
                !assetPath.EndsWith("_dummy.png", System.StringComparison.Ordinal))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);
        }
    }
}
