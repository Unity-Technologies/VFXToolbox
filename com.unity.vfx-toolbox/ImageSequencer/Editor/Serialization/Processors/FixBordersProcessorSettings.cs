using UnityEngine;
using UnityEngine.VFXToolbox;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Common","Fix Borders", typeof(FixBordersProcessor))]
    class FixBordersProcessorSettings : ProcessorSettingsBase
    {
        public Vector4 FixFactors;
        public Color FadeToColor;
        public float FadeToAlpha;
        [FloatSlider(0.5f,4.0f)]
        public float Exponent;

        public override void Default()
        {
            FixFactors = Vector4.zero;
            FadeToColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            FadeToAlpha = 0.0f;
            Exponent = 1.5f;
        }
    }
}
