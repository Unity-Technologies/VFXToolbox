namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Texture Sheet","Break Flipbook", typeof(BreakFlipbookProcessor))]
    public class BreakFilpbookProcessorSettings : ProcessorSettingsBase
    {
        public int FlipbookNumU;
        public int FlipbookNumV;

        public override void Default()
        {
            FlipbookNumU = 5;
            FlipbookNumV = 5;
        }
    }
}

