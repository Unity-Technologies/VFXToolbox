using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [HelpURL("https://drive.google.com/open?id=1YUwzA1mGvzWRpajDV-XF0iUd4RhW--bhMpqo-gmj9B8")]
    public class ImageSequence : ScriptableObject
    {
        public List<string> inputFrameGUIDs = new List<string>();
        public List<ProcessorInfo> processorInfos = new List<ProcessorInfo>();
        public ExportSettings exportSettings = defaultExportSettings;
        public EditSettings editSettings = defaultEditSettings;

        public ImageSequence inheritSettingsReference;

        [System.Serializable]
        public struct ExportSettings
        {
            public string fileName;
            public ushort frameCount;
            public ExportMode exportMode;
            public bool exportAlpha;
            public bool exportSeparateAlpha;
            public bool sRGB;
            public bool highDynamicRange;
            public bool compress;
            public bool generateMipMaps;
            public TextureWrapMode wrapMode;
            public FilterMode filterMode;
            public DataContents dataContents;
        }

        public static ExportSettings defaultExportSettings
        {
            get
            {
                return new ExportSettings
                {
                    fileName = "",
                    frameCount = 0,
                    exportMode = ExportMode.Targa,
                    exportAlpha = true,
                    exportSeparateAlpha = false,
                    sRGB = true,
                    highDynamicRange = false,
                    compress = true,
                    generateMipMaps = true,
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear,
                    dataContents = DataContents.Color
                };
            }
        }

        [System.Serializable]
        public struct EditSettings
        {
            public int selectedProcessor;
            public int lockedProcessor;
        }

        public static EditSettings defaultEditSettings
        {
            get
            {
                return new EditSettings()
                {
                    selectedProcessor = -1,
                    lockedProcessor = -1
                };
            }
        }

        [System.Serializable]
        public enum ExportMode
        {
            Targa = 0,
            EXR = 1,
            PNG = 2
        }

        [System.Serializable]
        public enum DataContents
        {
            Color = 0,
            NormalMap = 1,
            NormalMapFromGrayscale = 2,
            Sprite = 3
        }

    }
}

