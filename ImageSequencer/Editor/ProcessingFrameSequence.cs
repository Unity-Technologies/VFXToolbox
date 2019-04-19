using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    internal class ProcessingFrameSequence
    {
        public int numU
        {
            get
            {
                if (m_Processor != null)
                    return m_Processor.NumU;
                else
                    return 1;
            }
        }

        public int numV
        {
            get
            {
                if (m_Processor != null)
                    return m_Processor.NumV;
                else
                    return 1;
            }
        }

        public List<ProcessingFrame> frames
        {
            get
            {
                return m_Frames;
            }
        }

        public FrameProcessor processor
        {
            get
            {
                return m_Processor;
            }
        }

        public int width
        {
            get
            {
                if (m_Processor == null)
                {
                    if(m_Frames.Count > 0)
                        return m_Frames[0].texture.width;
                    else
                        return -1;
                }
                else
                {
                    return m_Processor.OutputWidth;
                }
                    
            }
        }

        public int height
        {
            get
            {
                if (m_Processor == null)
                {
                    if(m_Frames.Count > 0)
                        return m_Frames[0].texture.height;
                    else
                        return -1;
                }
                else
                {
                    return m_Processor.OutputHeight;
                }
                    
            }
        }

        public int length { get { return m_Frames.Count; } }

        private List<ProcessingFrame> m_Frames;
        private FrameProcessor m_Processor;

        public ProcessingFrameSequence(FrameProcessor processor)
        {
            m_Frames = new List<ProcessingFrame>();
            m_Processor = processor;
        }

        public void InvalidateAll()
        {
            for(int i = 0; i < m_Frames.Count; i++)
            {
                m_Frames[i].dirty = true;
            }
        }

        public void Dispose()
        {
            foreach(ProcessingFrame frame in m_Frames)
            {
                frame.Dispose();
            }
            frames.Clear();
        }

        public ProcessingFrame RequestFrame(int index)
        {
            if (m_Processor == null)
            {
                return m_Frames[index];
            }

            if(m_Processor.Enabled)
            {
                m_Processor.UpdateSequenceLength();

                if (m_Frames[index].dirty)
                {
                    Process(index);
                }
                return m_Frames[index];
            }
            else
            {
                return m_Processor.InputSequence.RequestFrame(index);
            }
        }

        public bool Process(int index)
        {
            bool processed = m_Frames[index].Process();
            return processed;
        }

    }
}
