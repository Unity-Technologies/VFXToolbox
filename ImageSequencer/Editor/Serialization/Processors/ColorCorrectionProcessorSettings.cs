using UnityEngine;
using UnityEngine.VFXToolbox;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Color","Color Correction", typeof(ColorCorrectionProcessor))]
    public class ColorCorrectionProcessorSettings : ProcessorSettingsBase
    {
        [FloatSlider(0.0f,2.0f)]
        public float Brightness;
        [FloatSlider(0.0f,2.0f)]
        public float Contrast;
        [FloatSlider(0.0f,2.0f)]
        public float Saturation;

        public AnimationCurve AlphaCurve;

        public override void Default()
        {
            Brightness = 1.0f;
            Contrast = 1.0f;
            Saturation = 1.0f;
            DefaultCurve();
        }

        public void DefaultCurve()
        {
            AlphaCurve = AnimationCurve.Linear(0, 0, 1, 1);
        }
    }
}

