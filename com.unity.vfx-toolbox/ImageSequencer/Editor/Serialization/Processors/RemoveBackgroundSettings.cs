using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Color","Remove Background", typeof(RemoveBackgroundBlendingProcessor))]
    class RemoveBackgroundSettings : ProcessorSettingsBase
    {
        public Color BackgroundColor;

        public override void Default()
        {
            BackgroundColor = new Color(0.25f,0.25f,0.25f,0.0f);
        }
    }
}
