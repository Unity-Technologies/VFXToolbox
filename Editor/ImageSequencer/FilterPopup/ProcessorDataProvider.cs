﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    internal class ProcessorDataProvider : IProvider
    {
        private Dictionary<Type, ProcessorAttribute> m_DataSource;
        private ProcessingNodeStack m_ProcessingNodeStack;
        private ImageSequence m_CurrentAsset;

        public class ProcessorElement : FilterPopupWindow.Element
        {
            public Action<ProcessorElement> m_SpawnCallback;
            public ProcessorAttribute m_Desc;
            public Type m_ProcessorSettingType;

            public ProcessorElement(int level, KeyValuePair<Type, ProcessorAttribute> desc, Action<ProcessorElement> spawncallback)
            {
                this.level = level;

                content = new GUIContent(EditorGUIUtility.IconContent("SceneViewFx"));
                content.text = desc.Value.name;

                m_Desc = desc.Value;
                m_ProcessorSettingType = desc.Key;
                m_SpawnCallback = spawncallback;
            }
        }

        internal ProcessorDataProvider(ProcessingNodeStack stack, ImageSequence asset)
        {
            m_DataSource = stack.settingsDefinitions;
            m_ProcessingNodeStack = stack;
            m_CurrentAsset = asset;
        }

        public void CreateComponentTree(List<FilterPopupWindow.Element> tree)
        {
            tree.Add(new FilterPopupWindow.GroupElement(0, "Add new Processor..."));

            var processors = m_DataSource.ToList();
            processors.Sort((processorA, processorB) => {
                int res = processorA.Value.category.CompareTo(processorB.Value.category);
                return res != 0 ? res : processorA.Value.name.CompareTo(processorB.Value.name);
            });

            HashSet<string> categories = new HashSet<string>();

            foreach( KeyValuePair<Type, ProcessorAttribute> desc in processors)
            {
                int i = 0; 

                if(!categories.Contains(desc.Value.category) && desc.Value.category != "")
                {
                    string[] split = desc.Value.category.Split('/');
                    string current = "";

                    while(i < split.Length)
                    {
                        current += split[i];
                        if(!categories.Contains(current))
                            tree.Add(new FilterPopupWindow.GroupElement(i+1,split[i]));
                        i++;
                        current += "/";
                    }
                    categories.Add(desc.Value.category);
                }
                else
                {
                    i = desc.Value.category.Split('/').Length;
                }

                if (desc.Value.category != "")
                    i++;

                tree.Add(new ProcessorElement(i, desc, AddProcessor));

            }
        }

        public void AddProcessor(ProcessorElement element)
        {
            var settingType = element.m_ProcessorSettingType;

            // Add Element
            Undo.RecordObject(m_CurrentAsset, "Add Processor");

            ProcessingNode processor = null;

            // Reflection Stuff here
            ProcessorAttribute attribute = m_ProcessingNodeStack.settingsDefinitions[settingType];

            var info = ProcessorInfo.CreateDefault(attribute.name, true, settingType);

            processor = (ProcessingNode)Activator.CreateInstance(typeof(ProcessingNode), m_ProcessingNodeStack, info);

            if(processor != null)
            {
                m_ProcessingNodeStack.AddProcessor(processor, m_CurrentAsset);
                m_ProcessingNodeStack.InvalidateAll();
            } 
        }

        public bool GoToChild(FilterPopupWindow.Element element, bool addIfComponent)
        {
            if (element is ProcessorElement)
            {
                ((ProcessorElement)element).m_SpawnCallback.Invoke((ProcessorElement)element);
                return true;
            }

            return false;
        }
    }
}
