using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    internal partial class ProcessingNodeStack
    {
        public ProcessingFrameSequence inputSequence
        {
            get
            {
                return m_InputSequence;
            }
        }

        public ProcessingFrameSequence outputSequence
        {
            get
            {
                if (m_ProcessingNodes.Count > 0)
                    return m_ProcessingNodes[m_ProcessingNodes.Count - 1].OutputSequence;
                else
                    return m_InputSequence;
            }
        }

        public ImageSequencer imageSequencer
        {
            get { return m_ImageSequencer; }
        }

        public List<ProcessingNode> nodes
        {
            get
            {
                return m_ProcessingNodes;
            }
        }

        private List<ProcessingNode> m_ProcessingNodes;
        private ProcessingFrameSequence m_InputSequence;
        private ImageSequencer m_ImageSequencer;

        public ProcessingNodeStack(ProcessingFrameSequence inputSequence, ImageSequencer imageSequencer)
        {
            m_InputSequence = inputSequence;
            m_ProcessingNodes = new List<ProcessingNode>();
            m_ImageSequencer = imageSequencer;
            
        }

        public void Dispose()
        {
            foreach(ProcessingNode p in m_ProcessingNodes)
            {
                p.Dispose();
            }
            m_ProcessingNodes.Clear();
        }

        public ProcessingFrameSequence GetOutputSequence()
        {
            if(m_ProcessingNodes.Count > 0)
            {
                return m_ProcessingNodes[m_ProcessingNodes.Count - 1].OutputSequence;
            }
            else
            {
                return inputSequence;
            }
        }

        public ProcessingFrameSequence GetInputSequence(ProcessingNode processor)
        {
            int index = m_ProcessingNodes.IndexOf(processor);

            if (index > 0)
            {
                return m_ProcessingNodes[index - 1].OutputSequence;
            }
            else
                return m_InputSequence;
        }

        public ProcessingNode GetNextProcessor(ProcessingNode node)
        {
            int index = m_ProcessingNodes.IndexOf(node);
            if(index < m_ProcessingNodes.Count-1)
            {
                return m_ProcessingNodes[index + 1];
            }
            return null;
        }

        public void Invalidate(ProcessingNode node)
        {
            int index = m_ProcessingNodes.IndexOf(node);
            if(index != -1)
                m_ProcessingNodes[index].Invalidate();
        }

        public void InvalidateAll()
        {
            if (m_ProcessingNodes.Count > 0)
                m_ProcessingNodes[0].Invalidate();
        }


        public Dictionary<Type, ProcessorAttribute> settingsDefinitions { get; private set; }

        public void UpdateProcessorsFromAssembly()
        {
            settingsDefinitions = new Dictionary<Type, ProcessorAttribute>();

            var processorType = typeof(ProcessorBase);
            var attrType = typeof(ProcessorAttribute);

            Type[] allProcessorTypes = new Type[0];

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies() )
            {
                try
                {
                    allProcessorTypes = allProcessorTypes.Concat(assembly.GetTypes().Where(t =>
                        t.IsClass
                        && !t.IsAbstract
                        && t.IsSubclassOf(processorType)
                        && t.IsDefined(attrType, false)
                        )).ToArray();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }

            foreach (var type in allProcessorTypes)
            {
                var attr = (ProcessorAttribute)type.GetCustomAttributes(attrType, false)[0];
                settingsDefinitions.Add(type, attr);
            }
        }


    }

}
