using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    internal partial class ProcessingNodeStack
    {
        public void AddSettingsObjectToAsset(ImageSequence asset, ScriptableObject settings)
        {
                AssetDatabase.AddObjectToAsset(settings,asset);
                settings.hideFlags = HideFlags.HideInHierarchy;
        }

        public void AddProcessorInfoObjectToAsset(ImageSequence asset, ProcessorInfo info)
        {
                AssetDatabase.AddObjectToAsset(info,asset);
                info.hideFlags = HideFlags.HideInHierarchy;
        }

        public void RemoveAllInputFrames(ImageSequence asset)
        {
            asset.inputFrameGUIDs.Clear();
            m_InputSequence.frames.Clear();

            EditorUtility.SetDirty(asset);
        }

        public void SortAllInputFrames(ImageSequence asset)
        {
            asset.inputFrameGUIDs.Sort((guidA,guidB) => {
                return string.Compare(AssetDatabase.GUIDToAssetPath(guidA), AssetDatabase.GUIDToAssetPath(guidB));
            });

            EditorUtility.SetDirty(asset);
        }

        public void ReverseAllInputFrames(ImageSequence asset)
        {
            asset.inputFrameGUIDs.Reverse();
            EditorUtility.SetDirty(asset);
        }

        public void LoadFramesFromAsset(ImageSequence asset)
        {
            inputSequence.frames.Clear();
            if (asset.inputFrameGUIDs != null && asset.inputFrameGUIDs.Count > 0)
            {
                int count = asset.inputFrameGUIDs.Count;
                int i = 1;
                foreach (string guid in asset.inputFrameGUIDs)
                {
                    VFXToolboxGUIUtility.DisplayProgressBar("Image Sequencer", "Loading Textures (" + i + "/" + count + ")", (float)i/count, 0.1f);
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (t != null)
                    {
                        inputSequence.frames.Add(new ProcessingFrame(t));
                    }
                    else
                    {
                        inputSequence.frames.Add(ProcessingFrame.Missing);
                    }
                    i++;
                }
                VFXToolboxGUIUtility.ClearProgressBar();
            }
        }

        public void SyncFramesToAsset(ImageSequence asset)
        {
            asset.inputFrameGUIDs.Clear();
            foreach(ProcessingFrame f in inputSequence.frames)
            {
                asset.inputFrameGUIDs.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(f.texture)));
            }
            EditorUtility.SetDirty(asset);
        }

        public void AddProcessor(ProcessingNode node, ImageSequence asset)
        {
            AddProcessorInfoObjectToAsset(asset, node.ProcessorInfo);
            asset.processorInfos.Add(node.ProcessorInfo);

            ProcessorBase settings = node.GetSettingsAbstract();
            if (settings != null)
            {
                AddSettingsObjectToAsset(asset, settings);
                node.ProcessorInfo.Settings = settings;
            }
            m_ProcessingNodes.Add(node);

            EditorUtility.SetDirty(asset);
        }

        public void RemoveAllProcessors(ImageSequence asset)
        {
            asset.processorInfos.Clear();
            m_ProcessingNodes.Clear();

            EditorUtility.SetDirty(asset);
        }

        public void RemoveProcessor(int index, ImageSequence asset)
        {
            asset.processorInfos.RemoveAt(index);
            m_ProcessingNodes.RemoveAt(index);

            EditorUtility.SetDirty(asset);
        }

        public void ReorderProcessors(ImageSequence asset)
        {
            if(m_ProcessingNodes.Count > 0)
            {
                List<ProcessingNode> old = new List<ProcessingNode>();
                foreach(ProcessingNode n in m_ProcessingNodes)
                {
                    old.Add(n);
                }

                m_ProcessingNodes.Clear();
                foreach(ProcessorInfo info in asset.processorInfos)
                {
                    foreach(ProcessingNode p in old)
                    {
                        if(p.ProcessorInfo.Equals(info))
                        {
                            m_ProcessingNodes.Add(p);
                            break;
                        }
                    }
                }
                EditorUtility.SetDirty(asset);
            }
        }

        public void LoadProcessorsFromAsset(ImageSequence asset)
        {
            m_ProcessingNodes.Clear();

            var infos = asset.processorInfos;

            UpdateProcessorsFromAssembly();

            // Creating Runtime
            foreach(ProcessorInfo procInfo in infos)
            {
                var processor = (ProcessingNode)Activator.CreateInstance(typeof(ProcessingNode), this, procInfo);
                m_ProcessingNodes.Add(processor);
            }
        }


    }
}
