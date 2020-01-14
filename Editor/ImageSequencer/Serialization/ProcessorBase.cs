using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    /// <summary>
    /// Base Class for Custom Processors. Derive from this class to add a new Processor.
    /// In order to populate processors in the menu, you need to implement the [ProcessorAttribute] to the class.
    /// </summary>
    public abstract class ProcessorBase : ScriptableObject
    {
        /// <summary>
        /// Asset Path of the Shader used for this processor (eg: "Assets/Shaders/MyShader.shader")
        /// </summary>
        public abstract string shaderPath { get; }

        /// <summary>
        /// Name of the Processor
        /// </summary>
        public abstract string processorName { get; }

        /// <summary>
        /// Display Label text of the processor (will be displayed in the processor list and asset inspector)
        /// </summary>
        public virtual string label => processorName;

        /// <summary>
        /// Number of U (Columns) defined by this processor. Implement to override default (passthrough input sequence's numU)
        /// </summary>
        public virtual int numU => inputSequenceNumU;

        /// <summary>
        /// Number of V (Rows) defined by this processor. Implement to override default (passthrough input sequence's numV)
        /// </summary>
        public virtual int numV => inputSequenceNumV;

        /// <summary>
        /// Number of frames defined by this processor. Implement to override default (passthrough input's sequence length)
        /// </summary>
        public virtual int sequenceLength => processingNode.InputSequence.length;

        /// <summary>
        /// Determines the actual processing of the frame. Will be called when this frame is requested by the Image Sequencer.
        /// </summary>
        /// <param name="frame">the requested frame index</param>
        /// <returns></returns>
        public abstract bool Process(int frame);

        /// <summary>
        /// Updates the output size of the processing node (resets internal render targets), implement to override the default (passthrough the input sequence frame width and height)
        /// </summary>
        public virtual void UpdateOutputSize()
        {
            processingNode.SetOutputSize(processingNode.InputSequence.width, processingNode.InputSequence.height);
        }

        /// <summary>
        /// Displays the Processor inspector in the left pane of the Image Sequencer
        /// </summary>
        /// <param name="changed">Whether the inspector has already caught changes.</param>
        /// <param name="serializedObject">The processor's serializedObject</param>
        /// <returns>whether there has changes to apply</returns>
        public abstract bool OnInspectorGUI(bool changed, SerializedObject serializedObject);

        /// <summary>
        /// Displays the Processor's Canvas Helpers as overlay.
        /// </summary>
        /// <param name="canvas">The Image Sequencer Canvas currently drawn</param>
        /// <returns>whether the canvas needs to redraw</returns>
        public virtual bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            return false;
        }

        /// <summary>
        /// Sets the default values of the processor. Will be called to configure the default state when a new processor is added to the Image Sequence.
        /// </summary>
        public abstract void Default();

        #region PROCESSINGNODE ACCESS
        /// <summary>
        /// The Input Sequence Frame Count
        /// </summary>
        public int inputSequenceLength => processingNode.InputSequence.length;

        /// <summary>
        /// The Input Sequence Frame Width (in pixels)
        /// </summary>
        public int inputSequenceWidth => processingNode.InputSequence.width;

        /// <summary>
        /// The Input Sequence Frame Height (in pixels)
        /// </summary>
        public int inputSequenceHeight => processingNode.InputSequence.height;

        /// <summary>
        /// The Input Sequence Flipbook U Count (Columns)
        /// </summary>
        public int inputSequenceNumU => processingNode.InputSequence.numU;

        /// <summary>
        /// The Input Sequence Flipbook V Count (Rows)
        /// </summary>
        public int inputSequenceNumV => processingNode.InputSequence.numV;

        /// <summary>
        /// Whether the Input frame Sequence is the Asset's Input Frame List (use to determine whether it needs gamma correction or not)
        /// </summary>
        public bool isInputFrameSequence => processingNode.InputSequence.processingNode == null;

        /// <summary>
        /// Whether the current processor is being previewed in the Image Sequencer Viewport, or not (for example, when the view is locked to another processor's result)
        /// </summary>
        public bool isCurrentlyPreviewed => processingNode.isCurrentlyPreviewed;

        /// <summary>
        /// The current Image Sequencer Viewport's preview sequence length. Please not that this is not necessarily this Processor's preview, use isCurrentlyPreviewed to check.
        /// </summary>
        public int previewSequenceLength => processingNode.previewSequenceLength;

        /// <summary>
        /// The current Image Sequencer Viewport's preview image index. Please not that this is not necessarily this Processor's preview, use isCurrentlyPreviewed to check.
        /// </summary>
        public int previewCurrentFrame => processingNode.previewCurrentFrame;

        /// <summary>
        /// The material internally used for this processor. It is created from the shader defined using this.shaderPath.
        /// </summary>
        public Material material => processingNode.material;

        /// <summary>
        /// Requests the Texture (and its Processing) of the Input Sequence Image at given index.
        /// </summary>
        /// <param name="index">The index of the Frame to be returned.</param>
        /// <returns>The texture object corresponding to the frame.</returns>
        public Texture RequestInputTexture(int index)
        {
            return processingNode.InputSequence.RequestFrame(index).texture;
        }

        /// <summary>
        /// Requests the Texture of the Output Sequence Image at given index.
        /// </summary>
        /// <param name="index">The index of the Frame to be returned.</param>
        /// <returns>The texture object corresponding to the frame.</returns>
        public Texture RequestOutputTexture(int index)
        {
            return processingNode.OutputSequence.frames[index].texture;
        }

        /// <summary>
        /// Sets the Output Size to the Processing Node
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetOutputSize(int width, int height)
        {
            processingNode.SetOutputSize(width, height);
        }

        /// <summary>
        /// Process the Frame at Given Index, using given material and texture as MainTexture.
        /// </summary>
        /// <param name="outputIndex">Frame Index to process</param>
        /// <param name="mainTexture">Texture object to pass as MainTexture</param>
        /// <param name="material">Material to use for the procesing</param>
        public void ProcessFrame(int outputIndex, Texture mainTexture, Material material)
        {
            processingNode.ExecuteShaderAndDump(outputIndex, mainTexture, material);
        }

        /// <summary>
        /// Process the Frame at Given Index, using default processor material and texture as MainTexture.
        /// </summary>
        /// <param name="outputIndex">Frame Index to process</param>
        /// <param name="mainTexture">Texture object to pass as MainTexture</param>
        public void ProcessFrame(int outputIndex, Texture mainTexture = null)
        {
            processingNode.ExecuteShaderAndDump(outputIndex, mainTexture);
        }

        /// <summary>
        /// Invalidates the Processor (will require to rebake frames)
        /// </summary>
        public void Invalidate()
        {
            processingNode.Invalidate();
        }

        #endregion

        #region INTERNAL
        private ProcessingNode processingNode;

        internal void AttachTo(ProcessingNode processor)
        {
            this.processingNode = processor;
        }
        #endregion

    }
}


