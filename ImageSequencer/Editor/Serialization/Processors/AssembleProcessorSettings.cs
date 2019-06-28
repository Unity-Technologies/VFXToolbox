namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Texture Sheet","Assemble Flipbook", typeof(AssembleProcessor))]
    public class AssembleProcessorSettings : ProcessorSettingsBase
    {
        public enum AssembleMode
        {
            FullSpriteSheet = 0,
            VerticalSequence = 1
        }

        public int FlipbookNumU;
        public int FlipbookNumV;
        public AssembleMode Mode;

        public override void Default()
        {
            FlipbookNumU = 5;
            FlipbookNumV = 5;
            Mode = AssembleMode.FullSpriteSheet;
        }
    }
}


