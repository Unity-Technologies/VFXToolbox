using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    internal abstract class ProcessorBase : ScriptableObject
    {
        public abstract string shaderPath { get; }
        public abstract string processorName { get; }
        public virtual string label => processorName;
        public virtual int GetNumU(FrameProcessor processor) => 1;
        public virtual int GetNumV(FrameProcessor processor) => 1;

        public abstract bool Process(FrameProcessor processor, int frame);
        public virtual void UpdateOutputSize(FrameProcessor processor)
        {
            processor.SetOutputSize(processor.InputSequence.width, processor.InputSequence.height);
        }
        public abstract int GetProcessorSequenceLength(FrameProcessor processor);

        public abstract bool OnInspectorGUI(bool changed, SerializedObject serializedObject, FrameProcessor processor);
        public abstract bool OnCanvasGUI(ImageSequencerCanvas canvas);
        public abstract void Default();


    }
}


