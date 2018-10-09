using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [CustomEditor(typeof(ImageSequence))]
    internal class ImageSequenceAssetEditor : Editor
    {
        private bool m_PreviewInput = false;
        private bool m_PreviewOutput = false;
        private bool m_RequireConstantRepaint = false;

        public override bool RequiresConstantRepaint()
        {
            return m_RequireConstantRepaint;
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_RequireConstantRepaint = false;

            using (new EditorGUILayout.VerticalScope())
            {
                if (GUILayout.Button(VFXToolboxGUIUtility.Get("Edit Sequence"), GUILayout.Height(40)))
                {
                    ImageSequencer toolbox = EditorWindow.GetWindow<ImageSequencer>();
                    toolbox.LoadAsset((ImageSequence)Selection.activeObject);
                }

                VFXToolboxGUIUtility.ToggleableHeader(true, false, "Input Frames");
                {
                    var inputFrames = serializedObject.FindProperty("inputFrameGUIDs");
                    int inputFrameCount = inputFrames.arraySize;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Input sequence contains " + inputFrameCount + " frame(s).");
                        GUILayout.FlexibleSpace();
                        m_PreviewInput = GUILayout.Toggle(m_PreviewInput, VFXToolboxGUIUtility.Get("Preview"), EditorStyles.miniButton);
                    }

                    if(inputFrameCount > 0  && m_PreviewInput)
                    {
                        int index;

                        if(inputFrameCount > 1)
                        {
                            m_RequireConstantRepaint = true;
                            float time = (float)EditorApplication.timeSinceStartup;
                            index = (int)Mathf.Floor((time * 30) % inputFrameCount); 
                        }
                        else
                        {
                            index = 0;
                        }

                        var frame = inputFrames.GetArrayElementAtIndex(index);
                        string guid = frame.stringValue;
                        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                        DrawAnimatedPreviewLayout(texture, ((float)index / inputFrameCount));
                    }
                    else
                    {
                        m_PreviewInput = false;
                    }
                }


                GUILayout.Space(24);
                VFXToolboxGUIUtility.ToggleableHeader(true, false, "Processors");
                {
                    var processors = serializedObject.FindProperty("processorInfos");
                    int processorsCount = processors.arraySize;
                    EditorGUILayout.LabelField("Asset contains " + processorsCount + " Processor (s).");
                    EditorGUI.indentLevel++;
                    for(int i = 0; i < processorsCount; i++)
                    {
                        var item = processors.GetArrayElementAtIndex(i).objectReferenceValue as ProcessorInfo;
                        EditorGUILayout.LabelField("#"+i+" - " + item.ProcessorName + (item.Enabled?"":" (Disabled)"));
                    }
                    EditorGUI.indentLevel--;
                }
                

                GUILayout.Space(24);
                VFXToolboxGUIUtility.ToggleableHeader(true, false, "Export Settings");

                var exportSettings = serializedObject.FindProperty("exportSettings");

                string fileName = exportSettings.FindPropertyRelative("fileName").stringValue;
                var mode = (ImageSequence.ExportMode)exportSettings.FindPropertyRelative("exportMode").enumValueIndex;
                var frameCount = exportSettings.FindPropertyRelative("frameCount");

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.EnumPopup(VFXToolboxGUIUtility.Get("Export Format"), mode);
                EditorGUI.EndDisabledGroup();

                if (fileName != "")
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField("Exporting to ", fileName);
                    EditorGUI.EndDisabledGroup();

                    Rect r = GUILayoutUtility.GetLastRect();
                    r.width += EditorGUIUtility.fieldWidth;
                    if (Event.current.rawType == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                    {
                        ImageSequencer.PingOutputTexture(fileName);
                    }

                    string dir = System.IO.Path.GetDirectoryName(fileName);
                    string file = System.IO.Path.GetFileNameWithoutExtension(fileName);

                    string[] assets;

                    if(!fileName.StartsWith("Assets/"))
                    {
                        EditorGUILayout.HelpBox("The output sequence has been exported outside the project, preview will be unavailable", MessageType.Warning);
                        return;
                    }

                    if(fileName.Contains("#"))
                    {
                        if(System.IO.Directory.Exists(dir))
                        {
                            string[] guids = AssetDatabase.FindAssets(file.Replace('#', '*'), new string[] { dir });
                            assets = new string[guids.Length];
                            for(int i = 0; i < guids.Length; i++)
                            {
                                assets[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
                            }
                        }
                        else
                            assets = new string[] { };
                    }
                    else
                    {
                        assets = new string[] { fileName };
                    }

                    int outputFrameCount;
                    if (frameCount.intValue == assets.Length)
                        outputFrameCount = frameCount.intValue;
                    else
                        outputFrameCount = 0; // Something went wrong

                    if(outputFrameCount > 0)
                    {
                        if(outputFrameCount > 1)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Label("Output sequence contains " + assets.Length + " frame(s).");
                                GUILayout.FlexibleSpace();
                                m_PreviewOutput = GUILayout.Toggle(m_PreviewOutput, VFXToolboxGUIUtility.Get("Preview"), EditorStyles.miniButton);
                            }

                            if(m_PreviewOutput)
                            {
                                m_RequireConstantRepaint = true;
                                float time = (float)EditorApplication.timeSinceStartup;
                                int index = (int)Mathf.Floor((time * 30) % outputFrameCount); 
                                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assets[index]);
                                DrawAnimatedPreviewLayout(texture, ((float)index / outputFrameCount));
                            }
                            else
                            {
                                m_PreviewOutput = false;
                            }
                        }
                        else // Only one frame
                        {
                            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assets[0]);
                            if (texture != null)
                                DrawAnimatedPreviewLayout(texture, 0.0f);
                            else
                                EditorGUILayout.HelpBox("Output Texture could not be loaded, maybe the file was deleted. Please export again using the editor", MessageType.Error);
                        }
                    }
                    else
                    {
                       EditorGUILayout.HelpBox("The output sequence does not match the number of files on disk, you probably need to export your sequence again", MessageType.Warning);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("This asset has not yet been exported. Please open editor and export it to generate a sequence.",MessageType.None);
                }
            }
        }

        private void DrawAnimatedPreviewLayout(Texture2D texture, float progress)
        {
            float ratio = (float)texture.height / (float)texture.width;
            using (new EditorGUILayout.HorizontalScope())
            {
                float width = EditorGUIUtility.currentViewWidth-32;
                float height = 240;
                GUILayout.FlexibleSpace();
                Rect texture_rect;
                if(ratio >= 1)
                    texture_rect = GUILayoutUtility.GetRect(height / ratio, height);
                else
                    texture_rect = GUILayoutUtility.GetRect(width, width * ratio);

                GUILayout.FlexibleSpace();
                EditorGUI.DrawTextureTransparent(texture_rect, texture);
                EditorGUI.DrawRect(new Rect(texture_rect.x, texture_rect.y, progress * 200.0f / ratio, 4.0f), new Color(0.3f, 0.5f, 1.0f));
            }
        }

    }
}
