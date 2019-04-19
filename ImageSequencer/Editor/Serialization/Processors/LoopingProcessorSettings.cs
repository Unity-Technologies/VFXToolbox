using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Sequence","Loop Sequence", typeof(LoopingProcessor))]
    class LoopingProcessorSettings : ProcessorSettingsBase
    {
        public AnimationCurve curve;
        public int syncFrame;
        public int outputSequenceLength;

        public override void Default()
        {
            curve = new AnimationCurve();
            syncFrame = 25;
            outputSequenceLength = 25;
        }
    }
}
