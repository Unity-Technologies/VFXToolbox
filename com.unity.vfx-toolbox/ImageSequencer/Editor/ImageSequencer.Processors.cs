using UnityEngine;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    internal partial class ImageSequencer : EditorWindow
    {
        [System.NonSerialized]
        ProcessorDataProvider m_ProcessorDataProvider;

        private void ShowAddProcessorMenu(ReorderableList list)
        {
            if(m_ProcessorDataProvider == null)
            {
                m_ProcessorDataProvider = new ProcessorDataProvider(m_processorStack, m_CurrentAsset);
            }

            FilterPopupWindow.Show(Event.current.mousePosition, m_ProcessorDataProvider);
           
        }

        private void MenuSelectProcessor(ReorderableList list)
        {
            if (m_CurrentAsset.editSettings.selectedProcessor == list.index)
                return;

            if (list.count > 0  && list.index != -1)
            {
                SetCurrentFrameProcessor(m_processorStack.processors[list.index], false);
            }
            else
                SetCurrentFrameProcessor(null, false);
        }

        private void ReorderProcessor(ReorderableList list)
        {
            Undo.RecordObject(m_CurrentAsset, "Reorder Processors");
            m_processorStack.ReorderProcessors(m_CurrentAsset);
            m_processorStack.InvalidateAll();
            UpdateViewport();

            // If locked processor is present, update its index
            if(m_LockedPreviewProcessor != null)
            {
                m_CurrentAsset.editSettings.lockedProcessor = m_processorStack.processors.IndexOf(m_LockedPreviewProcessor);
                EditorUtility.SetDirty(m_CurrentAsset);
            }

        }

        private void MenuRemoveProcessor(ReorderableList list)
        {
            int idx = list.index;

            Undo.RecordObject(m_CurrentAsset, "Remove Processor : " + m_processorStack.processors[idx].GetName());
            m_processorStack.RemoveProcessor(idx,m_CurrentAsset);

            // If was locked, unlock beforehand
            if (idx == m_CurrentAsset.editSettings.lockedProcessor)
                SetCurrentFrameProcessor(null, true);
            else if (idx < m_CurrentAsset.editSettings.lockedProcessor)
                m_CurrentAsset.editSettings.lockedProcessor--;

            if(m_processorStack.processors.Count > 0)
            {
                int newIdx = Mathf.Clamp(idx - 1, 0, m_processorStack.processors.Count - 1);

                SetCurrentFrameProcessor(m_processorStack.processors[newIdx], false);
                list.index = newIdx;
            }
            else
            {
                SetCurrentFrameProcessor(null, false);
                list.index = -1;
            }

            previewCanvas.currentFrameIndex = 0;
            m_processorStack.InvalidateAll();
            UpdateViewport();
        }

        public void RefreshCanvas()
        {
            if(m_CurrentProcessor != null)
                previewCanvas.sequence = m_CurrentProcessor.OutputSequence;
            else
                previewCanvas.sequence = m_processorStack.inputSequence;

            previewCanvas.currentFrameIndex = Mathf.Clamp(previewCanvas.currentFrameIndex, 0, previewCanvas.sequence.length - 1);

            UpdateViewport();
            Invalidate();
        }

        public void SetCurrentFrameProcessor(FrameProcessor processor, bool wantLock)
        {
            if(wantLock)
            {
                m_LockedPreviewProcessor = processor;
                if(processor != null)
                {
                    Undo.RecordObject(m_CurrentAsset, "Lock Processor");
                    m_CurrentProcessor = processor;
                    m_CurrentAsset.editSettings.lockedProcessor = m_processorStack.processors.IndexOf(processor);
                }
                else
                {
                    Undo.RecordObject(m_CurrentAsset, "Unlock Processor");
                    if(m_ProcessorsReorderableList.index != -1)
                        m_CurrentProcessor = m_processorStack.processors[Mathf.Min(m_ProcessorsReorderableList.index, m_processorStack.processors.Count-1)];
                    m_CurrentAsset.editSettings.lockedProcessor = -1;
                }
            }
            else
            {
                bool needChange = (m_CurrentProcessor != processor);

                if(needChange)
                    Undo.RecordObject(m_CurrentAsset, "Select Processor");

                if(m_LockedPreviewProcessor == null)
                    m_CurrentProcessor = processor;
                else
                    m_CurrentProcessor = m_LockedPreviewProcessor;
            }

            m_CurrentAsset.editSettings.selectedProcessor = m_processorStack.processors.IndexOf(processor);
            RefreshCanvas();
            EditorUtility.SetDirty(m_CurrentAsset);
        }

        public void DrawRListPreviewProcessorElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            Rect toggle_rect = new Rect(rect.x + 4, rect.y, 16, rect.height);
            Rect label_rect = new Rect(rect.x + 24, rect.y, rect.width - 24, rect.height);

            using (new EditorGUI.DisabledScope(true))
            {
                GUI.Toggle(toggle_rect, m_processorStack.processors[index].Enabled, "");
            }

            GUI.Label( label_rect, string.Format("#{0} - {1} ",index+1, m_processorStack.processors[index].ToString()), VFXToolboxStyles.RListLabel);
        }


        public void DrawRListProcessorElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            Rect toggle_rect = new Rect(rect.x + 4, rect.y, 16, rect.height);
            Rect label_rect = new Rect(rect.x + 24, rect.y, rect.width - 24, rect.height);
            Rect view_rect = new Rect(rect.x + rect.width - 37, rect.y, 19, 18);
            Rect lock_rect = new Rect(rect.x + rect.width - 16, rect.y+1, 16, 14);

            bool enabled = GUI.Toggle(toggle_rect, m_processorStack.processors[index].Enabled,"");
            if(enabled != m_processorStack.processors[index].Enabled)
            {
                m_processorStack.processors[index].Enabled = enabled;
                m_processorStack.processors[index].Invalidate();
                RefreshCanvas();
            }

            GUI.Label( label_rect, string.Format("#{0} - {1} ",index+1, m_processorStack.processors[index].ToString()), VFXToolboxStyles.RListLabel);

            if((m_LockedPreviewProcessor == null && isActive) || m_processorStack.processors.IndexOf(m_LockedPreviewProcessor) == index)
                GUI.DrawTexture(view_rect, (Texture2D)EditorGUIUtility.LoadRequired("ViewToolOrbit On.png"));

            bool locked = (m_LockedPreviewProcessor != null) && index == m_processorStack.processors.IndexOf(m_LockedPreviewProcessor);

            if(isActive || locked)
            {
                bool b = GUI.Toggle(lock_rect, locked,"", styles.LockToggle);
                if(b != locked)
                {
                    if(b)
                        SetCurrentFrameProcessor(m_processorStack.processors[index],true);
                    else
                        SetCurrentFrameProcessor(null, true);
                }
            }
        }
    }
}
