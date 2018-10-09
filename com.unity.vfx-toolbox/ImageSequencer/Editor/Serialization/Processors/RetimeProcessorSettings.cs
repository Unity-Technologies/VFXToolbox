using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Sequence","Retime", typeof(RetimeProcessor))]
    class RetimeProcessorSettings : ProcessorSettingsBase
    {
        public AnimationCurve curve;
        public int outputSequenceLength;
        public bool useCurve;

        public override void Default()
        {
            curve = new AnimationCurve();
            outputSequenceLength = 25;
            useCurve = true;
        }
    }
}
