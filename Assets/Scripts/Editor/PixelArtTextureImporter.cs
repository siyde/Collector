using UnityEditor;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// Assets/Art altındaki her texture'ı pixel art kurallarıyla import eder:
    /// Filter Mode = Point, Compression = None, Mipmap kapalı.
    /// PPU ve pivot yalnızca ilk import'ta atanır; sonradan elle değiştirebilirsin.
    /// </summary>
    public class PixelArtTextureImporter : AssetPostprocessor
    {
        public const int PixelsPerUnit = 32;
        const string ArtRoot = "Assets/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;

            var importer = (TextureImporter)assetImporter;

            // Her import'ta zorunlu tutulan ayarlar
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            if (!importer.importSettingsMissing) return;

            // Yalnızca ilk import: PPU + pivot
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = UsesBottomPivot(assetPath)
                ? (int)SpriteAlignment.BottomCenter
                : (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
        }

        // Zemine basan objeler (mobilya, karakter, oyuncak) Y-sorting için alt-orta pivot kullanır.
        static bool UsesBottomPivot(string path)
        {
            return path.StartsWith(ArtRoot + "Furniture/")
                || path.StartsWith(ArtRoot + "Characters/")
                || path.StartsWith(ArtRoot + "Collectibles/")
                || path.StartsWith(ArtRoot + "Props/");
        }
    }
}
