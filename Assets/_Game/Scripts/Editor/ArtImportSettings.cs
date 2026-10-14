using UnityEditor;
using UnityEngine;

namespace Biskas.EditorTools
{
    /// <summary>
    /// Настройки импорта для Assets/_Game/Art: Sprite, без mipmaps и Read/Write, Bilinear,
    /// ограничение размера для Android. Применяется только при первом импорте файла,
    /// чтобы не затирать ручные правки в инспекторе.
    /// </summary>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/_Game/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot) || !assetImporter.importSettingsMissing) return;
            Apply((TextureImporter)assetImporter);
        }

        public static void Apply(TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.isReadable = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.spritePixelsPerUnit = 100;
            ti.maxTextureSize = 1024;
            ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 512,
                format = TextureImporterFormat.ASTC_6x6
            });
        }

        /// <summary>Гарантирует, что файл импортирован как спрайт, и возвращает его.</summary>
        public static Sprite LoadSprite(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return null;
            if (ti.textureType != TextureImporterType.Sprite)
            {
                Apply(ti);
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
