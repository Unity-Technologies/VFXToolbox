using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    internal partial class ImageSequencer : EditorWindow
    {

        private int m_InputFramesHashCode;

        private void AddInputFrame(ReorderableList list, List<string> names)
        {
            if(names.Count> 0)
            {
                names.Sort();

                foreach (string s in names)
                {
                    Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(s);
                    if(t != null)  m_processorStack.inputSequence.frames.Add(new ProcessingFrame(t));
                }

                previewCanvas.currentFrameIndex = 0;
                m_processorStack.InvalidateAll();
                UpdateViewport();
                m_processorStack.SyncFramesToAsset(m_CurrentAsset);
                UpdateInputTexturesHash();
            }
        }

        private void AddInputFrame(ReorderableList list)
        {
            if (Selection.activeObject == null)
            {
                Debug.LogWarning("Could not add frames with no selection : please select input frames to add in the project view and click the add button. Or drag & drop directly into the Image Sequencer Editor Window");
                return;
            }

            string[] guids;
            List<string> names = new List<string>();

            if(VFXToolboxUtility.IsDirectorySelected())
            {
                names.AddRange(VFXToolboxUtility.GetAllTexturesInPath(AssetDatabase.GetAssetPath(Selection.activeObject)));
            }
            else
            {
                guids = Selection.assetGUIDs;
                foreach (string s in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(s);
                    Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if(t != null)
                        names.Add(path);
                }
            }

            if(names.Count > 0)
            {
                Undo.RecordObject(m_CurrentAsset, "Add Input Frames");
                AddInputFrame(list, names);
            }
            else
            {
                Debug.LogWarning("No suitable textures found in selection, make sure you selected either a directory containing textures or texture themselves in project view.");
            }

        }

        private void ReorderInputFrame(ReorderableList list)
        {
            Undo.RecordObject(m_CurrentAsset, "Reorder Input Frames");
            UpdateViewport();
            m_processorStack.SyncFramesToAsset(m_CurrentAsset);
            UpdateInputTexturesHash();
        }

        private void RemoveInputFrame(ReorderableList list)
        {
            int index = list.index;
            previewCanvas.sequence.frames.RemoveAt(index);
            
            if (list.count == 0)
                previewCanvas.currentFrame = null;
            else
            {
                if(previewCanvas.currentFrameIndex == index)
                {
                    previewCanvas.currentFrameIndex = Mathf.Max(0, index - 1);
                    previewCanvas.currentFrame = previewCanvas.sequence.frames[previewCanvas.currentFrameIndex];
                }
            }
            Undo.RecordObject(m_CurrentAsset, "Remove Input Frames");
            m_processorStack.SyncFramesToAsset(m_CurrentAsset);
            UpdateViewport();
            UpdateInputTexturesHash();

            if(m_processorStack.inputSequence.length > 0)
                m_processorStack.InvalidateAll();
        }

        public void DrawInputFrameRListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            int numbering = (int)Mathf.Floor(Mathf.Log10(m_InputFramesReorderableList.list.Count))+1;
            GUI.Label(rect, new GUIContent("#" + (index+1).ToString("D"+numbering.ToString())+ " - " + m_InputFramesReorderableList.list[index].ToString()));
        }

        public void SelectInputFrameRListElement(ReorderableList list)
        {
            if (list.count > 0  && list.index != -1)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(m_CurrentAsset.inputFrameGUIDs[list.index]));
                if (texture != null)
                    EditorGUIUtility.PingObject(texture);
                m_PreviewCanvas.currentFrameIndex = list.index;
            }
        }

        private int GetInputTexturesHashCode()
        {
            if(m_CurrentAsset != null)
            {
                var builder = new System.Text.StringBuilder();
                foreach (string s in m_CurrentAsset.inputFrameGUIDs)
                    builder.Append(s);
                return builder.ToString().GetHashCode();
            }
            else
                return 0;
        }

        public void UpdateInputTexturesHash()
        {
            m_InputFramesHashCode = GetInputTexturesHashCode();
        }

        #region menu actions

        private void MenuClearInputFrames()
        {
            Undo.RecordObject(m_CurrentAsset, "Clear All Input Frames");
            // Remove frames and update hash
            m_processorStack.RemoveAllInputFrames(m_CurrentAsset);
            m_processorStack.SyncFramesToAsset(m_CurrentAsset);
            m_InputFramesHashCode = GetInputTexturesHashCode();
            // Update view
            sidePanelViewMode = SidePanelMode.InputFrames;
            m_CurrentProcessor = null;
            m_LockedPreviewProcessor = null;
            m_CurrentAsset.editSettings.lockedProcessor = -1;
            m_CurrentAsset.editSettings.selectedProcessor = -1;
            m_PreviewCanvas.sequence = m_processorStack.inputSequence;
            // Request an update
            Invalidate();
            RefreshCanvas();
        }


        private void MenuSortInputFrames()
        {
            Undo.RecordObject(m_CurrentAsset, "Sort All Input Frames");
            // Sort frames and update hash
            m_processorStack.SortAllInputFrames(m_CurrentAsset);
            m_InputFramesHashCode = GetInputTexturesHashCode();

            LoadAsset(m_CurrentAsset);

            // Request an update
            Invalidate();
            RefreshCanvas();
        }


        private void MenuReverseInputFrames()
        {
            Undo.RecordObject(m_CurrentAsset, "Reverse Input Frames Order");
            // Inverse frame order and update hash
            m_processorStack.ReverseAllInputFrames(m_CurrentAsset);
            m_InputFramesHashCode = GetInputTexturesHashCode();

            LoadAsset(m_CurrentAsset);

            // Request an update
            Invalidate();
            RefreshCanvas();
        } 

        #endregion

    }
}
