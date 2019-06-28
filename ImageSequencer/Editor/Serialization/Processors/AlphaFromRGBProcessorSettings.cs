using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Color","Alpha From RGB", typeof(AlphaFromRGBProcessor))]
    public class AlphaFromRGBProcessorSettings : ProcessorSettingsBase
    {
        public Color BWFilterTint;
        //public Vector3 Weights;

        public override void Default()
        {
            BWFilterTint = Color.white;
            //Weights = new Vector3(0.2126f, 0.7152f, 0.0722f);
        }
    }
}
