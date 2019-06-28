using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Sequence","Fade", typeof(FadeProcessor))]
    public class FadeProcessorSettings : ProcessorSettingsBase
    {
        public AnimationCurve FadeCurve;
        public Color FadeToColor;

        public override void Default()
        {
            FadeCurve = new AnimationCurve();
            FadeToColor = new Color(0.25f,0.25f,0.25f,0.0f);
        }
    }
}
