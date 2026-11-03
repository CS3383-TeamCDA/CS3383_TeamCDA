using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeThe90s.WorldGen
{
    /// <summary>
    /// Forces the import settings palette-index art needs, so nobody has to remember them:
    ///   *_index.png  -> Sprite (single), 16 PPU, bottom-centre pivot, Point, uncompressed, no mipmaps, sRGB OFF
    ///   palette.png  -> Default texture, Point, clamp, uncompressed, no mipmaps, sRGB ON
    /// Compression, filtering or sRGB on an index texture silently corrupts the palette lookup.
    /// </summary>
    public class IndexTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string file = Path.GetFileName(assetPath).ToLowerInvariant();
            bool isIndex = file.EndsWith("_index.png");
            bool isPalette = file == "palette.png";
            if (!isIndex && !isPalette) return;

            var imp = (TextureImporter)assetImporter;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.isReadable = false;

            if (isPalette)
            {
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = true;
                return;
            }

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = WorldConstants.CellPixels;
            imp.sRGBTexture = false;
            imp.alphaIsTransparency = false;

            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spritePixelsPerUnit = WorldConstants.CellPixels;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
        }
    }
}
