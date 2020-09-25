using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    internal class ProcessingNode
    {
        public int OutputWidth
        {
            get {
                if (Enabled)
                    return GetOutputWidth();
                else
                    return
                        InputSequence.width;
            }
        }
        public int OutputHeight
        {
            get
            {
                if (Enabled)
                    return GetOutputHeight();
                else
                    return
                        InputSequence.width;
            }
        }

        public int NumU
        {
            get {
                if (Enabled)
                    return m_Processor.numU;
                else
                    return InputSequence.numU;
            }
        }
        public int NumV
        {
            get {
                if (Enabled)
                    return m_Processor.numV;
                else
                    return InputSequence.numV;
            }
        }

        public bool GenerateMipMaps;
        public bool Linear;

        public bool Enabled { get{ return m_bEnabled; } set {SetEnabled(value); } }

        public ProcessingFrameSequence InputSequence
        {
            get { return m_ProcessingNodeStack.GetInputSequence(this); }
        }
        public ProcessingFrameSequence OutputSequence
        {
            get { if (m_bEnabled) return m_OutputSequence; else return InputSequence; }
        }

        public ProcessorInfo ProcessorInfo
        {
            get { return m_ProcessorInfo; }
        }

        private ProcessingNodeStack m_ProcessingNodeStack;
        private ProcessingFrameSequence m_OutputSequence;

        private bool m_bEnabled;

        private int m_OutputWidth;
        private int m_OutputHeight;

        public ProcessorBase processor { get { return m_Processor; } private set { m_Processor = value; m_SerializedObject = new SerializedObject(m_Processor); } }

        private SerializedObject m_SerializedObject;
        private ProcessorBase m_Processor;
        private ProcessorInfo m_ProcessorInfo;
        public Shader shader { get; private set; }
        public Material material { get; private set; }

        public bool isCurrentlyPreviewed => m_ProcessingNodeStack.imageSequencer.previewCanvas.sequence.processingNode == this;
        public int previewCurrentFrame => m_ProcessingNodeStack.imageSequencer.previewCanvas.currentFrameIndex;
        public int previewSequenceLength => m_ProcessingNodeStack.imageSequencer.previewCanvas.numFrames;

        public ProcessingNode(ProcessingNodeStack processorStack, ProcessorInfo info)
        {
            m_ProcessorInfo = info;
            m_bEnabled = m_ProcessorInfo.Enabled;
            m_ProcessingNodeStack = processorStack;
            processor = m_ProcessorInfo.Settings;
            m_OutputSequence = new ProcessingFrameSequence(this);

            shader = AssetDatabase.LoadAssetAtPath<Shader>(processor.shaderPath);
            material = new Material(shader) { hideFlags = HideFlags.DontSave };
            material.hideFlags = HideFlags.DontSave;

            Linear = true;
            GenerateMipMaps = true;

            processor.AttachTo(this);
        }

        public void SetEnabled(bool value)
        {
            m_bEnabled = value;
            var info = new SerializedObject(m_ProcessorInfo);
            info.Update();
            info.FindProperty("Enabled").boolValue = value;
            info.ApplyModifiedProperties();
        }

        public void Dispose()
        {
            Material.DestroyImmediate(material);
            m_OutputSequence.Dispose();
        }

        public void Refresh()
        {
            if(Enabled != m_ProcessorInfo.Enabled)
                Enabled = m_ProcessorInfo.Enabled;
            UpdateSequenceLength();
            m_Processor.UpdateOutputSize();
        }

        protected virtual int GetOutputWidth()
        {
            m_Processor.UpdateOutputSize();
            return m_OutputWidth;
        }
        protected virtual int GetOutputHeight()
        {
            m_Processor.UpdateOutputSize();
            return m_OutputHeight;
        }
        public void SetOutputSize(int width, int height)
        {
            if(m_OutputWidth != width || m_OutputHeight != height)
            {
                m_OutputWidth = Mathf.Clamp(width,1,8192);
                m_OutputHeight = Mathf.Clamp(height,1,8192);
            }
        }
        protected int GetNumU()
        {
            if (InputSequence.processingNode == null)
                return 1;
            return InputSequence.numU;
        }
        protected int GetNumV()
        {
            if (InputSequence.processingNode == null)
                return 1;
            return InputSequence.numV;
        }
        protected bool DrawSidePanelHeader()
        {
            bool bHasChanged = false;
            bool previousEnabled = Enabled;
            Enabled = VFXToolboxGUIUtility.ToggleableHeader(Enabled, false, GetName());

            if(previousEnabled != Enabled)
            {
                SerializedObject o = new SerializedObject(m_ProcessorInfo);
                o.FindProperty("Enabled").boolValue = Enabled;
                o.ApplyModifiedProperties();
                m_ProcessingNodeStack.Invalidate(this);
                bHasChanged = true;
            }
            return bHasChanged;
        }
        public bool OnSidePanelGUI(ImageSequence asset, int ProcessorIndex)
        {
            bool bHasChanged = DrawSidePanelHeader();

            using (new EditorGUI.DisabledScope(!Enabled))
            {
                m_SerializedObject.Update();
                bHasChanged = m_Processor.OnInspectorGUI(bHasChanged, m_SerializedObject);
                m_SerializedObject.ApplyModifiedProperties();
            }

            return bHasChanged;
        }
        public bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
           return m_Processor.OnCanvasGUI(canvas);
        }
        public void RequestProcessOneFrame(int currentFrame)
        {
            int length = OutputSequence.length;

            int i = (currentFrame + 1) % length;

            while (i != currentFrame)
            {
                bool advance = false;
                if(OutputSequence.frames[i].dirty)
                {
                    advance = OutputSequence.Process(i);
                    if(advance) return;
                }

                i = (i + 1);
                i %= length;
            }
        }
        public bool Process(int frame)
        {
            return m_Processor.Process(frame);
        }
        public void ExecuteShaderAndDump(int outputframe, Texture mainTex)
        {
            ExecuteShaderAndDump(outputframe, mainTex, material);
        }
        public void ExecuteShaderAndDump(int outputframe, Texture mainTex, Material material)
        {
            RenderTexture backup = RenderTexture.active;
            Graphics.Blit(mainTex, (RenderTexture)m_OutputSequence.frames[outputframe].texture, material);
            RenderTexture.active = backup;
        }
        public int GetProcessorSequenceLength()
        {
            return m_Processor.sequenceLength;
        }
        public bool Process(ProcessingFrame frame)
        {
            return Process(OutputSequence.frames.IndexOf(frame));
        }
        public void UpdateSequenceLength()
        {
            int currentCount = m_OutputSequence.frames.Count;
            int requiredCount = GetProcessorSequenceLength();

            if (currentCount == requiredCount)
                return;

            if(currentCount > requiredCount)
            {
                for(int i = requiredCount - 1; i < currentCount - 1; i++)
                {
                    m_OutputSequence.frames[i].Dispose();
                }

                m_OutputSequence.frames.RemoveRange(requiredCount - 1, currentCount - requiredCount);
            }
            else
            {
                for(int i = 0; i < requiredCount - currentCount; i++)
                {
                    m_OutputSequence.frames.Add(new ProcessingFrame(this));
                }
            }
        }
        public void Invalidate()
        {
            UpdateSequenceLength();
            SetOutputSize(GetOutputWidth(), GetOutputHeight());
            m_OutputSequence.InvalidateAll();

            ProcessingNode next = m_ProcessingNodeStack.GetNextProcessor(this);
            if(next != null)
                next.Invalidate();
        }
        public string GetName()
        {
            return m_Processor.processorName;
        }
        public string GetLabel()
        {
            return m_Processor.label;
        }
        public override string ToString()
        {
            return GetLabel() + (Enabled ? "" : " (Disabled)");
        }
        public ProcessorBase GetSettingsAbstract()
        {
            return processor;
        }

    }
}
