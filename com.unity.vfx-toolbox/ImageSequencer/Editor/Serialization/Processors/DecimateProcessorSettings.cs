namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Sequence","Decimate", typeof(DecimateProcessor))]
    class DecimateProcessorSettings : ProcessorSettingsBase
    {
        public ushort DecimateBy;

        public override void Default()
        {
            DecimateBy = 3;
        }
    }
}


