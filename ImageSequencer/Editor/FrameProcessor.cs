using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    internal abstract class FrameProcessor
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
                    return GetNumU();
                else
                    return InputSequence.numU;
            }
        }
        public int NumV
        {
            get {
                if (Enabled)
                    return GetNumV();
                else
                    return InputSequence.numV;
            }
        }

        public bool GenerateMipMaps;
        public bool Linear;

        public bool Enabled { get{ return m_bEnabled; } set {SetEnabled(value); } }

        public ProcessingFrameSequence InputSequence
        {
            get { return m_ProcessorStack.GetInputSequence(this); }
        }
        public ProcessingFrameSequence OutputSequence
        {
            get { if (m_bEnabled) return m_OutputSequence; else return InputSequence; }
        }

        public ProcessorInfo ProcessorInfo
        {
            get { return m_ProcessorInfo; }
        }

        protected FrameProcessorStack m_ProcessorStack;
        protected ProcessingFrameSequence m_OutputSequence;

        protected bool m_bEnabled;

        protected int m_OutputWidth;
        protected int m_OutputHeight;

        protected ProcessorInfo m_ProcessorInfo;

        public FrameProcessor(FrameProcessorStack processorStack, ProcessorInfo info)
        {
            m_ProcessorInfo = info;
            m_ProcessorInfo.ProcessorName = GetName();
            m_bEnabled = m_ProcessorInfo.Enabled;
            m_ProcessorStack = processorStack;
            m_OutputSequence = new ProcessingFrameSequence(this);
            Linear = true;
            GenerateMipMaps = true;
        }

        public void SetEnabled(bool value)
        {
            m_bEnabled = value;
            var info = new SerializedObject(m_ProcessorInfo);
            info.Update();
            info.FindProperty("Enabled").boolValue = value;
            info.ApplyModifiedProperties();
        }

        public virtual void Dispose()
        {
            m_OutputSequence.Dispose();
        }

        public void Refresh()
        {
            if(Enabled != m_ProcessorInfo.Enabled)
                Enabled = m_ProcessorInfo.Enabled;
            UpdateSequenceLength();
            UpdateOutputSize();
        }

        protected virtual void UpdateOutputSize()
        {
            SetOutputSize(InputSequence.width, InputSequence.height);
        }

        protected virtual int GetOutputWidth()
        {
            UpdateOutputSize();
            return m_OutputWidth;
        }
        protected virtual int GetOutputHeight()
        {
            UpdateOutputSize();
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

        protected abstract int GetNumU();
        protected abstract int GetNumV();

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
                m_ProcessorStack.Invalidate(this);
                bHasChanged = true;
            }
            return bHasChanged;
        }

        protected abstract bool DrawSidePanelContent(bool hasChanged);

        public abstract bool OnSidePanelGUI(ImageSequence asset, int ProcessorIndex);

        public abstract bool OnCanvasGUI(ImageSequencerCanvas canvas);

        public virtual void RequestProcessOneFrame(int currentFrame)
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

        public abstract bool Process(int frame);

        public virtual int GetProcessorSequenceLength()
        {
            return InputSequence.length;
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

        public virtual void Invalidate()
        {
            UpdateSequenceLength();
            SetOutputSize(GetOutputWidth(), GetOutputHeight());
            m_OutputSequence.InvalidateAll();

            FrameProcessor next = m_ProcessorStack.GetNextProcessor(this);
            if(next != null)
                next.Invalidate();
        }

        public abstract string GetName();

        public virtual string GetLabel()
        {
            return GetName();
        }

        public override string ToString()
        {
            return GetLabel() + (Enabled ? "" : " (Disabled)");
        }

        public abstract ProcessorSettingsBase GetSettingsAbstract();

    }

    internal abstract class FrameProcessor<T> : FrameProcessor where T : ProcessorSettingsBase
    {
        public T settings { get { return m_Settings; } private set { m_Settings = value;  m_SerializedObject = new SerializedObject(m_Settings); } }

        private T m_Settings;
        protected SerializedObject m_SerializedObject;

        public FrameProcessor(FrameProcessorStack stack, ProcessorInfo info) : base(stack, info)
        {
            m_ProcessorInfo = info;
            settings = (T)m_ProcessorInfo.Settings;
        }

        public override bool OnSidePanelGUI(ImageSequence asset, int ProcessorIndex)
        {
            bool bHasChanged = DrawSidePanelHeader();

            using (new EditorGUI.DisabledScope(!Enabled))
            {
                m_SerializedObject.Update();
                bHasChanged = DrawSidePanelContent(bHasChanged);
                m_SerializedObject.ApplyModifiedProperties();
            }

            return bHasChanged;
        }

        public sealed override ProcessorSettingsBase GetSettingsAbstract()
        {
            return settings;
        }
    }
}
