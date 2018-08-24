namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Common","Crop", typeof(CropProcessor))]
    public class CropProcessorSettings : ProcessorSettingsBase
    {
        public uint Crop_Top;
        public uint Crop_Bottom;
        public uint Crop_Left;
        public uint Crop_Right;

        public float AutoCropThreshold = 0.003f;

        public override void Default()
        {
                Crop_Top = 0;
                Crop_Bottom = 0;
                Crop_Left = 0;
                Crop_Right = 0;
                AutoCropThreshold = 0.003f;
        }
    }
}

