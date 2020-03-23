using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    internal class ProcessingFrameSequence
    {
        public int numU
        {
            get
            {
                if (m_ProcessingNode != null)
                    return m_ProcessingNode.NumU;
                else
                    return 1;
            }
        }

        public int numV
        {
            get
            {
                if (m_ProcessingNode != null)
                    return m_ProcessingNode.NumV;
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

        public ProcessingNode processingNode
        {
            get
            {
                return m_ProcessingNode;
            }
        }

        public int width
        {
            get
            {
                if (m_ProcessingNode == null)
                {
                    if(m_Frames.Count > 0)
                        return m_Frames[0].texture.width;
                    else
                        return -1;
                }
                else
                {
                    return m_ProcessingNode.OutputWidth;
                }
                    
            }
        }

        public int height
        {
            get
            {
                if (m_ProcessingNode == null)
                {
                    if(m_Frames.Count > 0)
                        return m_Frames[0].texture.height;
                    else
                        return -1;
                }
                else
                {
                    return m_ProcessingNode.OutputHeight;
                }
                    
            }
        }

        public int length { get { return m_Frames.Count; } }

        private List<ProcessingFrame> m_Frames;
        private ProcessingNode m_ProcessingNode;

        public ProcessingFrameSequence(ProcessingNode node)
        {
            m_Frames = new List<ProcessingFrame>();
            m_ProcessingNode = node;
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
            if (m_ProcessingNode == null)
            {
                return m_Frames[index];
            }

            if(m_ProcessingNode.Enabled)
            {
                m_ProcessingNode.UpdateSequenceLength();

                if (m_Frames[index].dirty)
                {
                    Process(index);
                }
                return m_Frames[index];
            }
            else
            {
                return m_ProcessingNode.InputSequence.RequestFrame(index);
            }
        }

        public bool Process(int index)
        {
            bool processed = m_Frames[index].Process();
            return processed;
        }

    }
}
