using UnityEditor;
using UnityEngine;

namespace WizardGarden.EditorTools
{
    /// <summary>
    /// Assets/Art 아래 텍스처에 픽셀 아트 임포트 규약을 자동 적용 (A01 확정).
    /// 아트 교체·추가 시 손으로 세팅하지 않아도 되게 하는 것이 목적 — 규약 변경은 여기 한 곳만 고친다.
    /// </summary>
    public sealed class PixelArtImportSettings : AssetPostprocessor
    {
        /// <summary>아트 루트 — 이 경로 아래만 규약을 적용한다.</summary>
        public const string ArtRoot = "Assets/Art/";

        /// <summary>1 유닛 = 32px (타일 크기 기준, A01 확정).</summary>
        public const float PixelsPerUnit = 32f;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot, System.StringComparison.Ordinal))
                return;

            var importer = (TextureImporter)assetImporter;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;          // 픽셀 뭉개짐 방지
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;

            var settings = new TextureImporterPlatformSettings
            {
                name = "DefaultTexturePlatform",
                maxTextureSize = 2048,
                format = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed
            };
            importer.SetPlatformTextureSettings(settings);
        }
    }
}
