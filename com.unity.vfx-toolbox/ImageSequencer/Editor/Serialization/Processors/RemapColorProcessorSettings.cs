using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Color","Remap Color", typeof(RemapColorProcessor))]
    class RemapColorProcessorSettings : ProcessorSettingsBase
    {
        public enum RemapColorSource
        {
            sRGBLuminance = 0,
            LinearRGBLuminance = 1,
            Alpha = 2,
            LinearR = 3,
            LinearG = 4,
            LinearB = 5
        }

        public Gradient Gradient;
        public RemapColorSource ColorSource;

        public override void Default()
        {
            ColorSource = RemapColorSource.sRGBLuminance;
            DefaultGradient();
        }

        public void DefaultGradient()
        {
            Gradient = new Gradient();
            GradientColorKey[] colors = new GradientColorKey[2] { new GradientColorKey(Color.black, 0),new GradientColorKey(Color.white, 1) };
            GradientAlphaKey[] alpha = new GradientAlphaKey[2] { new GradientAlphaKey(0,0), new GradientAlphaKey(1,1) };
            Gradient.SetKeys(colors, alpha);
        }
    }
}
