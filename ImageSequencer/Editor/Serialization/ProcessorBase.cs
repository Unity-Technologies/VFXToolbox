using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    internal abstract class ProcessorBase : ScriptableObject
    {
        public abstract string shaderPath { get; }
        public abstract string processorName { get; }
        public virtual string label => processorName;

        protected ProcessingNode processor;

        public void AttachTo(ProcessingNode processor)
        {
            this.processor = processor;
        }

        public virtual int numU => processor.InputSequence.numU;
        public virtual int numV => processor.InputSequence.numV;
        public virtual int sequenceLength => processor.InputSequence.length;
        public abstract bool Process(int frame);
        public virtual void UpdateOutputSize()
        {
            processor.SetOutputSize(processor.InputSequence.width, processor.InputSequence.height);
        }
        public abstract bool OnInspectorGUI(bool changed, SerializedObject serializedObject);
        public virtual bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            return false;
        }
        public abstract void Default();
    }
}


