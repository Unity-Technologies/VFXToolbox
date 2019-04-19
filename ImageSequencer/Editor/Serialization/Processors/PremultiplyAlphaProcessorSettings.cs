namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Color","Premultiply Alpha", typeof(PremultiplyAlphaProcessor))]
    class PremultiplyAlphaProcessorSettings : ProcessorSettingsBase
    {
        public bool RemoveAlpha;
        public float AlphaValue;

        public override void Default()
        {
            RemoveAlpha = false;
            AlphaValue = 1.0f;
        }
    }
}
