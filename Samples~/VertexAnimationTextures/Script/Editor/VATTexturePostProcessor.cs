using UnityEngine;
using UnityEditor;

public class VATTexturePostProcessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {

        if(    assetPath.EndsWith("-VATPOS.tga")
            || assetPath.EndsWith("-VATPOS.exr")
            || assetPath.EndsWith("-VATNRM.tga")
            || assetPath.EndsWith("-VATNRM.exr")
            )
        {
            var importer = (TextureImporter)assetImporter;

            importer.sRGBTexture = false;
            importer.maxTextureSize = 16384;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
        }
    }
}
