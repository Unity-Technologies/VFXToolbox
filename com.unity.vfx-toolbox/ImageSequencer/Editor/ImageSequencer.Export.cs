using UnityEngine;
using System.IO;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    internal partial class ImageSequencer : EditorWindow
    {
        private string GetNumberedFileName(string pattern, int number, int maxFrames)
        {
            int numbering = (int)Mathf.Floor(Mathf.Log10(maxFrames))+1;
            return pattern.Replace("#", number.ToString("D" + numbering.ToString()));
        }

        public string ExportToFile(bool useCurrentFileName)
        {
            bool bIsInsideProject = true;
            string path;
            if(useCurrentFileName)
            {
                path = m_CurrentAsset.exportSettings.fileName;
            }
            else
            {
                string title = "Save Texture, use # for frame numbering.";
                string defaultFileName, extension;

                int count = m_processorStack.outputSequence.frames.Count;
                int numU = m_processorStack.outputSequence.numU;
                int numV = m_processorStack.outputSequence.numV;

                string defaultDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(m_CurrentAsset));

                defaultFileName =  m_CurrentAsset.name;

                if (count > 1)
                    defaultFileName += "_#";

                if(numU * numV != 1)
                    defaultFileName += "_"+numU+"x"+numV;

                switch (m_CurrentAsset.exportSettings.exportMode)
                {
                    case ImageSequence.ExportMode.EXR:
                        defaultFileName += ".exr";
                        extension = "exr";

                        break;
                    case ImageSequence.ExportMode.Targa:
                        defaultFileName += ".tga";
                        extension = "tga";

                        break;
                    case ImageSequence.ExportMode.PNG:
                        defaultFileName += ".png";
                        extension = "png";

                        break;
                    default: return null;
                }

                 path = EditorUtility.SaveFilePanel(title, defaultDir, defaultFileName, extension);
                if (path == null || path == "")
                    return "";

                if (path.Contains(Application.dataPath))
                    path = path.Replace(Application.dataPath, "Assets");

            }

            if(!path.StartsWith("Assets/"))
            {
                bIsInsideProject = false;
                Debug.LogWarning("VFX Toolbox Warning : Saving a texture outside the project's scope. Import Settings will not be applied");
            }

            int frameCount = m_processorStack.outputSequence.length;

            if(frameCount > 1 && !Path.GetFileNameWithoutExtension(path).Contains("#"))
            {
                if (!EditorUtility.DisplayDialog("VFX Toolbox", "You are currently exporting a sequence of images with no # in filename for numbering, do you want to add _# as a suffix of the filename?", "Add Postfix", "Cancel Export"))
                    return "";

                string newpath = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + "_#" + Path.GetExtension(path);
                path = newpath;
            }

            ImageSequence.ExportSettings settings = m_CurrentAsset.exportSettings;
            bool bCanceled = false;

            try
            {
                int i = 1;
                foreach (ProcessingFrame frame in m_processorStack.outputSequence.frames)
                {
                    if(VFXToolboxGUIUtility.DisplayProgressBar("Image Sequencer", "Exporting Frame #" + i + "/" + frameCount, (float)i / frameCount, 0, true))
                    {
                        bCanceled = true;
                        break;
                    }

                    // Export frame : first, dump data into color array

                    Color[] inputs;
                    if (frame.texture is Texture2D) // if using input frame
                    {
                        RenderTexture temp = RenderTexture.GetTemporary(frame.texture.width, frame.texture.height, 0, RenderTextureFormat.ARGBHalf);
                        Graphics.Blit((Texture2D)frame.texture, temp); 
                        inputs = ReadBack(temp);
                    }
                    else // frame.texture is RenderTexture
                    {
                        frame.Process();
                        inputs = ReadBack((RenderTexture)frame.texture);
                    }

                    string fileName = GetNumberedFileName(path, i, frameCount);

                    // Dump data
                    byte[] bytes;

                    switch(m_CurrentAsset.exportSettings.exportMode)
                    {
                        case ImageSequence.ExportMode.EXR:
#if UNITY_5_6_OR_NEWER
                            // New Exporter
                            {
                                Texture2D texture = new Texture2D(frame.texture.width, frame.texture.height, TextureFormat.RGBAHalf, settings.generateMipMaps, !settings.sRGB);
                                texture.SetPixels(inputs);
                                texture.Apply(true);
                                bytes = texture.EncodeToEXR();
                            }
#else
                            // Old Exporter
                            {
                                bytes = MiniEXR.MiniEXR.MiniEXRWrite((ushort)frame.texture.width, (ushort)frame.texture.height, settings.exportAlpha, inputs, true);
                            }
#endif
                            break;
                        case ImageSequence.ExportMode.Targa:
                            {
                                bytes = MiniTGA.MiniTGA.MiniTGAWrite((ushort)frame.texture.width, (ushort)frame.texture.height, settings.exportAlpha, inputs);
                            }
                            break;
                        case ImageSequence.ExportMode.PNG:
                            {
                                Texture2D texture = new Texture2D(frame.texture.width, frame.texture.height, TextureFormat.RGBA32, settings.generateMipMaps, !settings.sRGB);
                                texture.SetPixels(inputs);
                                texture.Apply(true);
                                bytes = texture.EncodeToPNG();
                            }
                            break;
                        default:
                            {
                                bytes = new byte[0] { }; // Empty file that should not happen
                            }
                            break;
                    }
                    File.WriteAllBytes(fileName, bytes);

                    AssetDatabase.Refresh();

                    // Process Import if saved inside project
                    if(bIsInsideProject)
                    {
                        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(fileName);
                        importer.wrapMode = m_CurrentAsset.exportSettings.wrapMode;
                        importer.filterMode = m_CurrentAsset.exportSettings.filterMode;
                        switch(m_CurrentAsset.exportSettings.dataContents)
                        {
                            case ImageSequence.DataContents.Color:
                                importer.textureType = TextureImporterType.Default;
                                break;
                            case ImageSequence.DataContents.NormalMap:
                                importer.textureType = TextureImporterType.NormalMap;
                            importer.convertToNormalmap = false;
                            break;
                        case ImageSequence.DataContents.NormalMapFromGrayscale:
                            importer.textureType = TextureImporterType.NormalMap;
                            importer.convertToNormalmap = true;
                                break;
                            case ImageSequence.DataContents.Sprite:
                                importer.textureType = TextureImporterType.Sprite;
                                importer.spriteImportMode = SpriteImportMode.Multiple;
                                importer.spritesheet = GetSpriteMetaData(frame, m_processorStack.outputSequence.numU, m_processorStack.outputSequence.numV );
                                break;
                        }
                        importer.mipmapEnabled = m_CurrentAsset.exportSettings.generateMipMaps;

                        switch(m_CurrentAsset.exportSettings.exportMode)
                        {
                            case ImageSequence.ExportMode.Targa:
                                importer.sRGBTexture = m_CurrentAsset.exportSettings.sRGB;
                                importer.alphaSource = m_CurrentAsset.exportSettings.exportAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                                importer.textureCompression = m_CurrentAsset.exportSettings.compress ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
                                break;
                            case ImageSequence.ExportMode.EXR:
                                importer.sRGBTexture = false;
                                importer.alphaSource = (m_CurrentAsset.exportSettings.exportAlpha && !m_CurrentAsset.exportSettings.compress) ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                                importer.textureCompression = m_CurrentAsset.exportSettings.compress ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
                                break;
                            case ImageSequence.ExportMode.PNG:
                                importer.sRGBTexture = m_CurrentAsset.exportSettings.sRGB;
                                importer.alphaSource = m_CurrentAsset.exportSettings.exportAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                                importer.textureCompression = m_CurrentAsset.exportSettings.compress ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
                                break;
                        }

                        AssetDatabase.ImportAsset(fileName, ImportAssetOptions.ForceUpdate);
                    }

                    // Separate Alpha
                    if (m_CurrentAsset.exportSettings.exportSeparateAlpha)
                    {
                        string alphaFilename = fileName.Substring(0, fileName.Length - 4) + "_alpha.tga";
                        // build alpha
                        for(int k = 0; k < inputs.Length; k++)
                        {
                            float a = inputs[k].a;
                            inputs[k] = new Color(a, a, a, a);
                        }
                        MiniTGA.MiniTGA.MiniTGAWrite(alphaFilename,(ushort)frame.texture.width, (ushort)frame.texture.height, false, inputs);

                        AssetDatabase.Refresh();

                        // Process Importer for alpha if inside project
                        if(bIsInsideProject)
                        {
                            TextureImporter alphaImporter = (TextureImporter)TextureImporter.GetAtPath(alphaFilename);

                            if (m_CurrentAsset.exportSettings.dataContents == ImageSequence.DataContents.Sprite)
                            {
                                alphaImporter.textureType = TextureImporterType.Sprite;
                                alphaImporter.spriteImportMode = SpriteImportMode.Multiple;
                                alphaImporter.spritesheet = GetSpriteMetaData(frame, m_processorStack.outputSequence.numU, m_processorStack.outputSequence.numV);
                                alphaImporter.alphaSource = TextureImporterAlphaSource.None;
                            }
                            else
                            {
                                alphaImporter.textureType = TextureImporterType.SingleChannel;
                                alphaImporter.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                            }

                            alphaImporter.wrapMode = m_CurrentAsset.exportSettings.wrapMode;
                            alphaImporter.filterMode = m_CurrentAsset.exportSettings.filterMode;
                            alphaImporter.sRGBTexture = false;
                            alphaImporter.mipmapEnabled = m_CurrentAsset.exportSettings.generateMipMaps;
                            alphaImporter.textureCompression = m_CurrentAsset.exportSettings.compress ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;

                            AssetDatabase.ImportAsset(alphaFilename, ImportAssetOptions.ForceUpdate);
                        }
                    }

                    i++;
                }
            }
            catch(System.Exception e)
            {
                VFXToolboxGUIUtility.ClearProgressBar();
                Debug.LogError(e.Message);
            }

            VFXToolboxGUIUtility.ClearProgressBar();

            if(bCanceled)
                return "";
            else
                return path;
        }

        public  static void PingOutputTexture(string fileName)
        {

            if (fileName == "")
                return; 

            string dir = System.IO.Path.GetDirectoryName(fileName);
            string file = System.IO.Path.GetFileNameWithoutExtension(fileName);

            if(!fileName.StartsWith("Assets/"))
                return;

            if(fileName.Contains("#"))
            {
                if(System.IO.Directory.Exists(dir))
                {
                    string[] guids = AssetDatabase.FindAssets(file.Replace('#', '*'), new string[] { dir });
                    fileName = AssetDatabase.GUIDToAssetPath(guids[0]);
                }
            }

            bool fileFound = (fileName != "")&&(System.IO.File.Exists(fileName));

            if(fileFound)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(fileName);
                if (texture != null) EditorGUIUtility.PingObject(texture);
            }
            else
            {
                Debug.LogWarning("Could not ping output texture, either the file was moved or removed, you probably need to export your sequence again");
            }
        }

        private void PingCurrentAsset()
        {
            EditorGUIUtility.PingObject(m_CurrentAsset);
        }

        private void UpdateExportedAssets()
        {
            if (ExportToFile(true) != "")
                m_CurrentAsset.exportSettings.frameCount = (ushort)m_processorStack.outputSequence.frames.Count;
            else
                m_CurrentAsset.exportSettings.frameCount = 0;
        }

        private Color[] ReadBack(RenderTexture renderTexture)
        {
            Color[] inputs = VFXToolboxUtility.ReadBack(renderTexture);

            if(QualitySettings.activeColorSpace == ColorSpace.Linear && m_CurrentAsset.exportSettings.sRGB)
            {
                Color[] outputs = new Color[inputs.Length];
                for (int j = 0; j < inputs.Length; j++)
                {
                    outputs[j] = inputs[j].gamma;
                }
                return outputs;
            }
            return inputs;
        }

        private SpriteMetaData[] GetSpriteMetaData(ProcessingFrame frame, int numU, int numV)
        {
            SpriteMetaData[] result = new SpriteMetaData[numU * numV];

            float width = (float)frame.texture.width / numU;
            float height = (float)frame.texture.height / numV;

            for(int i = 0; i < numU; i++)
                for(int j = 0; j < numV; j++)
                {
                    SpriteMetaData data = new SpriteMetaData();
                    data.name = "Frame_" + (i + (j * numU));
                    data.rect = new Rect(i * width, (numV - j - 1) * height, width, height);
                    result[i + (j * numU)] = data;
                }

            return result;
        }
 
        private static GUIContent[] GetExportModeFriendlyNames()
        {
            return new GUIContent[] { VFXToolboxGUIUtility.Get("Targa"), VFXToolboxGUIUtility.Get("OpenEXR (HDR)"), VFXToolboxGUIUtility.Get("PNG") };
        }
    }
}
