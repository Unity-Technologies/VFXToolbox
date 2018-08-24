using System.IO;

namespace UnityEditor.VFXToolbox
{
    /// <summary>
    /// Example of an Asset PostProcessor that configures all textures within a given folder (default Asset/Resources) 
    /// to be imported with settings according to VFX Toolbox Image Sequencer recommandations.
    /// </summary>
    public class ImageSequencerSourcePostprocessor : AssetPostprocessor
    {
        // Internal flags for usage
        public enum Usage
        {
            Color,
            LinearData
        }
        
        public const string m_RootFolder = "Assets/VFXResources";
        public const string m_NormalNomenclaturePostFix = "_nrm";
        public const string m_LinearNomenclatureSuffix = "_lin";
        public readonly string[] m_Labels = new string[] { "Weapon", "Audio" };
        
        void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(m_RootFolder)) // for all assets in VFX resources folder
            {
                string filename = Path.GetFileName(assetPath);
                string extension = Path.GetExtension(assetPath);

                // Default usage is color
                Usage usage = Usage.Color;

                // if containing normal suffix, switch to linear
                if (filename.ToLower().Contains(m_NormalNomenclaturePostFix.ToLower()))
                    usage = Usage.LinearData;

                // if containing linear suffix, switch to linear
                if (filename.ToLower().Contains(m_LinearNomenclatureSuffix.ToLower()))
                    usage = Usage.LinearData;

                // if HDR, switch to linear
                if(extension.ToLower() == "EXR".ToLower())
                    usage = Usage.LinearData;

                TextureImporter importer = (TextureImporter)assetImporter;

                // Even if we have normalmaps, we don't want to encode them in swizzled NM yet.
                importer.textureType = TextureImporterType.Default;

                switch(usage)
                {
                    default: // Color, but should not happen
                    case Usage.Color:
                        importer.sRGBTexture = true;
                        break;
                    case Usage.LinearData:
                        importer.sRGBTexture = false;
                        break;
                }

                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.maxTextureSize = 8192;
                importer.mipmapEnabled = true;
                importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

            }
        }
    }
}
