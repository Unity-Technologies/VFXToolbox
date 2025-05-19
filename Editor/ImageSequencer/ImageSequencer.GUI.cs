using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    internal partial class ImageSequencer : EditorWindow
    {
        private Splitter m_Splitter;
        private ReorderableList m_InputFramesReorderableList;
        private ReorderableList m_ProcessorsReorderableList;
        private Vector2 m_OptionsViewScroll = Vector2.zero;

        private SidePanelMode m_SidePanelViewMode = 0;
        private bool m_Dirty = true;
        private bool m_NeedRedraw = false;

        public void InitializeGUI()
        {
            if(m_Splitter == null)
            {
                m_Splitter = new Splitter(360, DrawEditPanelGUI, DrawCanvasGUI, Splitter.SplitLockMode.LeftMinMax, new Vector2(320.0f, 480.0f));
            }

            if(m_PreviewCanvas == null)
            {
                m_PreviewCanvas = new ImageSequencerCanvas(new Rect(0, Styles.ToolbarHeight, position.width - m_Splitter.value, position.height - Styles.ToolbarHeight),this);
            }

            CheckGraphicsSettings();
        }

        public void OnGUI()
        {
            InitializeGUI();

            if(m_CurrentAsset == null)
            if(m_CurrentAsset == null)
            {
                OnNoAssetGUI();
                return;
            }

            m_Dirty = false;

            m_CurrentAssetSerializedObject.Update();

            UpdateCanvasRect();

            if(HandleDropData()) return;

            DrawToolbar();

            Rect rect = new Rect(0,18, position.width, position.height-18);
            if (m_Splitter.DoSplitter(rect))
                Invalidate();

            // Processing Play Mode, Cooking & Autocooking
            if (previewCanvas.isPlaying && previewCanvas.sequence.length > 1)
                Invalidate();
            else
            {
                if (Event.current.type == EventType.Repaint)
                {
                    if(m_NeedRedraw)
                    {
                        previewCanvas.UpdateCanvasSequence();
                        Invalidate();
                        m_NeedRedraw = false;
                    }
                    else if((m_AutoCook && m_CurrentProcessingNode != null))
                    {
                        m_CurrentProcessingNode.RequestProcessOneFrame(previewCanvas.currentFrameIndex);
                        Invalidate();
                    }
                }
            }

            // if Invalidated this frame, repaint.
            if (m_Dirty) Repaint();
        }

        public void OnNoAssetGUI()
        {
            UpdateCanvasRect();

            if(HandleDropData()) return;

            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.HelpBox("No Image Sequence is currently selected.\nPlease create one within your Assets then select It in the project view.", MessageType.Info);
                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(8);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Create Image Sequence", GUILayout.Width(160)))
                    {
                        string file = EditorUtility.SaveFilePanelInProject("Create Image Sequence", "New Image Sequence", "asset", "Create Image Sequence?");
                        if (file != string.Empty)
                        {
                            var sequence = ImageSequenceAssetFactory.CreateImageSequenceAtPath(file);
                            AssetDatabase.ImportAsset(file);
                            LoadAsset(sequence);
                        }
                    }
                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }

        }

        private bool HandleDropData()
        {
            if(m_CurrentAsset == null)
                return false;

            if(sidePanelViewMode == SidePanelMode.InputFrames && DragAndDrop.paths.Length > 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if( Event.current.type == EventType.DragExited)
                {
                    List<string> texturePaths = new List<string>();
                    foreach(string path in DragAndDrop.paths)
                    {
                        if (VFXToolboxUtility.IsDirectory(path))
                            texturePaths.AddRange(VFXToolboxUtility.GetAllTexturesInPath(path));
                        else
                        {
                            VFXToolboxGUIUtility.DisplayProgressBar("Image Sequencer", "Discovering Assets...", 0.5f);
                            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                            if(t != null)
                                texturePaths.Add(path);
                        }
                    }
                    AddInputFrame(m_InputFramesReorderableList, texturePaths);
                    VFXToolboxGUIUtility.ClearProgressBar();
                    return true;
                }
            }
            return false;
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(Styles.ToolbarHeight)))
            {
                EditorGUI.BeginChangeCheck();
                bool prev;

                bool bMaskR = m_PreviewCanvas.maskR;
                bool bMaskG = m_PreviewCanvas.maskG;
                bool bMaskB = m_PreviewCanvas.maskB;
                bool bMaskA = m_PreviewCanvas.maskA;
                bool bMaskRGB = bMaskR && bMaskG && bMaskB;

                //GUILayout.Space(m_Splitter.value);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Width(m_Splitter.value-1)))
                {
                    if (GUILayout.Button(VFXToolboxGUIUtility.Get("Current Sequence: "+m_CurrentAsset.name), EditorStyles.toolbarButton))
                    {
                        PingCurrentAsset();
                    }
                    GUILayout.FlexibleSpace();
                }

                Rect r = GUILayoutUtility.GetRect(VFXToolboxGUIUtility.GetTextAndIcon(" ", "SceneviewFx"), EditorStyles.toolbarPopup);
                if (GUI.Button(r, VFXToolboxGUIUtility.GetTextAndIcon(" ", "SceneviewFx"), EditorStyles.toolbarPopup))
                {
                    PopupWindow.Show(r, (PopupWindowContent) new CanvasConfigPopupWindowContent(this));
                }

                GUILayout.Space(20);

                bMaskRGB = GUILayout.Toggle(bMaskRGB, styles.iconRGB, EditorStyles.toolbarButton);

                if(bMaskRGB != (bMaskR && bMaskG && bMaskB))
                {
                    bMaskR = bMaskG = bMaskB = bMaskRGB;

                    m_PreviewCanvas.maskR = bMaskR;
                    m_PreviewCanvas.maskG = bMaskG;
                    m_PreviewCanvas.maskB = bMaskB;

                }

                prev = bMaskR;
                bMaskR = GUILayout.Toggle(bMaskR, VFXToolboxGUIUtility.Get("R"),styles.MaskRToggle, GUILayout.Width(20));

                if (bMaskR != prev)
                    m_PreviewCanvas.maskR = bMaskR;

                prev = bMaskG;
                bMaskG = GUILayout.Toggle(bMaskG, VFXToolboxGUIUtility.Get("G"),styles.MaskGToggle, GUILayout.Width(20));
                if (bMaskG != prev)
                    m_PreviewCanvas.maskG = bMaskG;

                prev = bMaskB;
                bMaskB = GUILayout.Toggle(bMaskB, VFXToolboxGUIUtility.Get("B"),styles.MaskBToggle, GUILayout.Width(20));
                if (bMaskB != prev)
                    m_PreviewCanvas.maskB = bMaskB;

                prev = bMaskA;
                bMaskA = GUILayout.Toggle(bMaskA, VFXToolboxGUIUtility.Get("A"),styles.MaskAToggle, GUILayout.Width(20));
                if (bMaskA != prev)
                    m_PreviewCanvas.maskA = bMaskA;

                if(m_PreviewCanvas.sequence != null && m_PreviewCanvas.numFrames > 0 && m_PreviewCanvas.currentFrame != null)
                {
                    GUILayout.Space(20.0f);

                    if(m_PreviewCanvas.mipMapCount > 0)
                    {
                        int currentMip = m_PreviewCanvas.mipMap;
                        int newMip = currentMip;

                        {
                            Rect mipRect = GUILayoutUtility.GetRect(164, 24);
                            GUI.Box(mipRect, GUIContent.none, VFXToolboxStyles.toolbarButton);

                            GUI.Label(new RectOffset(0, 0, -6, 0).Remove(mipRect), styles.iconMipMapDown);
                            newMip = (int)Mathf.Round(GUI.HorizontalSlider(new RectOffset(24,64,1,0).Remove(mipRect), (float)newMip, 0.0f, (float)m_PreviewCanvas.mipMapCount-1));
                            GUI.Label(new RectOffset(100, 0, -6, 0).Remove(mipRect), styles.iconMipMapUp);
                            if (newMip != currentMip)
                            {
                                m_PreviewCanvas.mipMap = newMip;
                            }
                            GUI.Label(new RectOffset(124, 0, 3, 0).Remove(mipRect), (m_PreviewCanvas.mipMap+1)+"/"+m_PreviewCanvas.mipMapCount, VFXToolboxStyles.toolbarLabelLeft);
                        }
                    }
                }

                if(EditorGUI.EndChangeCheck())
                {
                    m_PreviewCanvas.UpdateCanvasSequence(); // Reblit if changed the flags.
                }

                GUILayout.Space(20);

                {
                    Rect brightnessRect = GUILayoutUtility.GetRect(160, 24);
                    GUI.Box(brightnessRect, GUIContent.none, VFXToolboxStyles.toolbarButton);
                    GUI.Label(new RectOffset(4, 0, 2, 0).Remove(brightnessRect), VFXToolboxGUIUtility.GetTextAndIcon("Background|Sets the Background Brightness", "CheckerFloor"), VFXToolboxStyles.toolbarLabelLeft);

                    float newBrightness = GUI.HorizontalSlider(new RectOffset(82, 6, 1, 0).Remove(brightnessRect), previewCanvas.BackgroundBrightness, 0.0f, 1.0f);
                    if (previewCanvas.BackgroundBrightness != newBrightness)
                        previewCanvas.BackgroundBrightness = newBrightness;
                }

                GUILayout.FlexibleSpace();
            }
            
        }

        private void DrawCanvasGUI(Rect rect)
        {

            if (previewCanvas.sequence.length > 1)
                previewCanvas.displayRect = new Rect(m_Splitter.value, Styles.ToolbarHeight, position.width - m_Splitter.value, position.height - 100 - Styles.ToolbarHeight);
            else
                previewCanvas.displayRect = new Rect(m_Splitter.value, Styles.ToolbarHeight, position.width - m_Splitter.value, position.height - Styles.ToolbarHeight);

            previewCanvas.OnGUI(this);

            // Draw Update Button
            if(m_CurrentAsset.exportSettings.fileName != "")
            {
                Rect exportButtonRect = new Rect(position.width - 100, Styles.ToolbarHeight + 12, 74, 24);
                if (GUI.Button(exportButtonRect, VFXToolboxGUIUtility.GetTextAndIcon("Update", "SaveActive"), VFXToolboxStyles.TabButtonSingle))
                {
                    UpdateExportedAssets();
                }
            }
        }

        private void DrawEditPanelGUI(Rect rect)
        {
            using (new GUILayout.AreaScope(rect))
            {
                m_OptionsViewScroll = EditorGUILayout.BeginScrollView(m_OptionsViewScroll, styles.scrollView, GUILayout.Width(m_Splitter.value));

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(16);
                    // Three Button Tabs : Mode Selection
                    DrawTabbedPanelSelector();
                    GUILayout.Space(16);

                    switch (m_SidePanelViewMode)
                    {
                        case SidePanelMode.InputFrames:
                            // Draw Input Frames Panel
                            DrawInputFramesPanelContent();
                            break;
                        case SidePanelMode.Processors:
                            // Draw Processors Edit Panel
                            DrawProcessorsPanelContent();
                            break;
                        case SidePanelMode.Export:
                            // Draw Export Panel
                            DrawExportPanelContent();
                            break;
                        default:
                            break;
                    }
                    GUILayout.Space(32);
                }
                EditorGUILayout.EndScrollView();

                if(QualitySettings.activeColorSpace == ColorSpace.Gamma)
                {
                    EditorGUILayout.HelpBox("Your project is configured to use Gamma color space. While this is not a breaking setting for the Image Sequencer to work, it will produce a different and unexpected results than when used in Linear color space.", MessageType.Warning);
                    GUILayout.Space(8);
                }
            }
        }

        private void DrawTabbedPanelSelector()
        {
            SidePanelMode prevMode = m_SidePanelViewMode;
            bool hasInputFrames = m_ProcessingNodeStack.inputSequence.frames.Count > 0;
            SidePanelMode newMode = (SidePanelMode)VFXToolboxGUIUtility.TabbedButtonsGUILayout(
                    (int)prevMode,
                    new string[] { "Input Frames", "Processors", "Export"},
                    new bool [] { true, hasInputFrames, hasInputFrames}
                );

            if(prevMode != newMode)
            {
                m_SidePanelViewMode = newMode;

                switch(m_SidePanelViewMode)
                {
                    case SidePanelMode.InputFrames:

                        m_PreviewCanvas.sequence = m_ProcessingNodeStack.inputSequence;

                        break;

                    case SidePanelMode.Processors:

                        if (m_LockedPreviewProcessor != null)
                            m_PreviewCanvas.sequence = m_LockedPreviewProcessor.OutputSequence;
                        else
                        {
                            if(m_CurrentProcessingNode != null)
                                m_PreviewCanvas.sequence = m_CurrentProcessingNode.OutputSequence;
                            else
                            {
                                if (m_ProcessingNodeStack.nodes.Count > 0)
                                    m_PreviewCanvas.sequence = m_ProcessingNodeStack.nodes[m_ProcessingNodeStack.nodes.Count - 1].OutputSequence;
                                else
                                    m_PreviewCanvas.sequence = m_ProcessingNodeStack.inputSequence;
                            }
                        }

                        break;

                    case SidePanelMode.Export:

                        m_PreviewCanvas.sequence = m_ProcessingNodeStack.outputSequence;

                        break;
                }

                m_PreviewCanvas.InvalidateRenderTarget();
                m_PreviewCanvas.UpdateCanvasSequence();
                m_PreviewCanvas.Invalidate(true);
            }

        }

        private void DrawInputFramesPanelContent()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(VFXToolboxGUIUtility.Get("Input Frames"),EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if(GUILayout.Button(VFXToolboxGUIUtility.Get("Actions"), EditorStyles.popup, GUILayout.Width(80), GUILayout.Height(20)))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(VFXToolboxGUIUtility.Get("Clear"), false, MenuClearInputFrames);
                    menu.AddItem(VFXToolboxGUIUtility.Get("Sort All"), false, MenuSortInputFrames);
                    menu.AddItem(VFXToolboxGUIUtility.Get("Reverse Oder"), false, MenuReverseInputFrames);
                    menu.ShowAsContext();
                }

            }
            GUILayout.Space(8);
            m_InputFramesReorderableList.DoLayoutList();

            if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && m_ProcessingNodeStack.inputSequence.length > 0)
            {
                RemoveInputFrame(m_InputFramesReorderableList);
                Event.current.Use();
            }
        }

        private void DrawProcessorsPanelContent()
        {
            ImageSequence seq = (ImageSequence)EditorGUILayout.ObjectField(VFXToolboxGUIUtility.Get("Inherit processors from"), m_CurrentAsset.inheritSettingsReference, typeof(ImageSequence), false);

            if (m_IgnoreInheritSettings)
                EditorGUILayout.HelpBox("Warning : Dependency Loop found when inheriting these settings, ignoring...", MessageType.Warning);

            if(seq != m_CurrentAsset.inheritSettingsReference && m_CurrentAsset != seq)
            {
                Undo.RecordObject(m_CurrentAsset, "use processor settings from other ImageSequence");
                m_CurrentAsset.inheritSettingsReference = seq;
                if(seq != null)
                {
                    m_CurrentAsset.editSettings.selectedProcessor = seq.editSettings.selectedProcessor;
                    m_CurrentAsset.editSettings.lockedProcessor = -1;
                }
                EditorUtility.SetDirty(m_CurrentAsset);
                LoadAsset(m_CurrentAsset);
            }
            GUILayout.Space(10);

            using (new EditorGUI.DisabledScope(m_CurrentAsset.inheritSettingsReference != null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(VFXToolboxGUIUtility.Get("Frame Processors"),EditorStyles.boldLabel,GUILayout.Width(180));
                    GUILayout.FlexibleSpace();
                    if(GUILayout.Button(VFXToolboxGUIUtility.Get("Clear"), GUILayout.Width(80)))
                    {
                        // Delete everything
                        Undo.RecordObject(m_CurrentAsset, "Clear All Processors");
                        m_ProcessingNodeStack.RemoveAllProcessors(m_CurrentAsset);
                        // Update UI
                        m_ProcessorsReorderableList.index = -1;
                        m_CurrentProcessingNode = null;
                        m_LockedPreviewProcessor = null;
                        m_CurrentAsset.editSettings.lockedProcessor = -1;
                        m_CurrentAsset.editSettings.selectedProcessor = -1;
                        m_PreviewCanvas.sequence = m_ProcessingNodeStack.inputSequence;
                        EditorUtility.SetDirty(m_CurrentAsset);
                        // Request Repaint
                        Invalidate();
                        RefreshCanvas();
                        return;
                    }
                }
                GUILayout.Space(8);
            }

            m_ProcessorsReorderableList.DoLayoutList();

            if(m_IgnoreInheritSettings || m_CurrentAsset.inheritSettingsReference == null)
            {
                GUILayout.Space(10);

                // Draw inspector and Invalidates whatever needs to.
                for(int i = 0; i < m_ProcessingNodeStack.nodes.Count; i++)
                {
                    if(m_ProcessorsReorderableList.index == i)
                    {
                        bool changed = m_ProcessingNodeStack.nodes[i].OnSidePanelGUI(m_CurrentAsset,i);
                        if (changed)
                        {
                            m_ProcessingNodeStack.nodes[i].Invalidate();
                            UpdateViewport();
                        }
                        m_Dirty = m_Dirty || changed;
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Settings cannot be accessed when linked from external Image Sequence", MessageType.Info);
            }


            // Handle final keyboard events (delete)
            if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && m_ProcessingNodeStack.nodes.Count > 0)
            {
                MenuRemoveProcessor(m_ProcessorsReorderableList);
                Event.current.Use();
            }
        }

        private void DrawExportPanelContent()
        {
            int length = m_ProcessingNodeStack.outputSequence.length;

            if(length > 0)
            {
                m_CurrentAssetSerializedObject.Update();
                EditorGUI.BeginChangeCheck();

                ImageSequence.ExportSettings prevState = m_CurrentAsset.exportSettings;

                using (new VFXToolboxGUIUtility.HeaderSectionScope("File Export Options"))
                {

                    ImageSequence.ExportMode prevMode = m_CurrentAsset.exportSettings.exportMode;

                    m_CurrentAsset.exportSettings.exportMode = (ImageSequence.ExportMode)EditorGUILayout.Popup(VFXToolboxGUIUtility.Get("Export Format"), (int)m_CurrentAsset.exportSettings.exportMode, GetExportModeFriendlyNames());

                    if (prevMode != m_CurrentAsset.exportSettings.exportMode)
                    {
                        m_CurrentAsset.exportSettings.fileName = "";
                    }

                    switch(m_CurrentAsset.exportSettings.exportMode)
                    {

                        case ImageSequence.ExportMode.EXR:
                            m_CurrentAsset.exportSettings.highDynamicRange = true;
                            m_CurrentAsset.exportSettings.sRGB = false;
                            break;
                        case ImageSequence.ExportMode.PNG:
                        case ImageSequence.ExportMode.Targa:
                            m_CurrentAsset.exportSettings.highDynamicRange = false;
                            break;
                    }


                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(VFXToolboxGUIUtility.Get("File Name|File name or pattern of the export sequence, using # characters will add frame number to the file name, use multiple ### to ensure leading zeroes."), m_CurrentAsset.exportSettings.fileName);
                    EditorGUI.EndDisabledGroup();

                    Rect r = GUILayoutUtility.GetLastRect();
                    r.width += EditorGUIUtility.fieldWidth;

                    if (Event.current.rawType == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                    {
                    PingOutputTexture(m_CurrentAsset.exportSettings.fileName);
                    }

                if(!m_CurrentAsset.exportSettings.highDynamicRange)
                    m_CurrentAsset.exportSettings.sRGB = EditorGUILayout.Toggle(VFXToolboxGUIUtility.Get("sRGB (Color Data)|Whether the texture contains color (or not), HDR Data is always non sRGB."), m_CurrentAsset.exportSettings.sRGB);

                    EditorGUI.BeginDisabledGroup(m_CurrentAsset.exportSettings.compress && m_CurrentAsset.exportSettings.highDynamicRange);
                    m_CurrentAsset.exportSettings.exportAlpha = EditorGUILayout.Toggle(VFXToolboxGUIUtility.Get("Export Alpha|Whether to export the alpha channel"), m_CurrentAsset.exportSettings.exportAlpha);
                    EditorGUI.EndDisabledGroup();

                    m_CurrentAsset.exportSettings.exportSeparateAlpha = EditorGUILayout.Toggle(VFXToolboxGUIUtility.Get("Separate Alpha|Export the alpha channel as a separate TGA Grayscale file with a \"_alpha\" suffix."), m_CurrentAsset.exportSettings.exportSeparateAlpha);

                }

                using (new VFXToolboxGUIUtility.HeaderSectionScope("Texture Import Options"))
                {
                    m_CurrentAsset.exportSettings.outputShape = (ImageSequence.OutputMode)EditorGUILayout.EnumPopup(VFXToolboxGUIUtility.Get("Output Shape|Selects whether export as simple 2D Texture, flipbook as 2D texture, or full sequence as 2D texture"), m_CurrentAsset.exportSettings.outputShape);
                    if(m_CurrentAsset.exportSettings.outputShape == ImageSequence.OutputMode.Texture2DArray)
                    {
                        ProcessingNode n = m_ProcessingNodeStack.nodes[m_ProcessingNodeStack.nodes.Count - 1];
                        if(((float)n.OutputWidth / n.NumU)%1.0f > 0.0f || ((float)n.OutputHeight / n.NumV) % 1.0f > 0.0f)
                        {
                            EditorGUILayout.HelpBox("Row and Column Counts are not exact multiples of the resolution, some padding will occur in the export", MessageType.Warning);
                        } else if ((!Mathf.IsPowerOfTwo(n.OutputWidth / n.NumU) || !Mathf.IsPowerOfTwo(n.OutputHeight / n.NumV)) && m_CurrentAsset.exportSettings.compress && m_CurrentAsset.exportSettings.generateMipMaps)
                        {
                            EditorGUILayout.HelpBox("Texture 2D Arrays with mip maps cannot be compressed if U and V dimensions are not power of two", MessageType.Warning);
                        }
                    }

                    m_CurrentAsset.exportSettings.dataContents = (ImageSequence.DataContents)EditorGUILayout.EnumPopup(VFXToolboxGUIUtility.Get("Import as|Sets the importer mode"), m_CurrentAsset.exportSettings.dataContents);
                    if(m_CurrentAsset.exportSettings.dataContents == ImageSequence.DataContents.Sprite)
                    {
                        ProcessingNode n = m_ProcessingNodeStack.nodes[m_ProcessingNodeStack.nodes.Count - 1];
                        if (((float)n.OutputWidth % n.NumU) != 0 || ((float)n.OutputHeight % n.NumV) != 0)
                            EditorGUILayout.HelpBox("Warning : texture size is not a multiplier of rows ("+n.NumU+") and columns ("+n.NumV+") count, this will lead to incorrect rendering of the sprite animation", MessageType.Warning);
                    }

                    switch(m_CurrentAsset.exportSettings.dataContents)
                    {
                        case ImageSequence.DataContents.NormalMapFromGrayscale:
                        case ImageSequence.DataContents.NormalMap:
                            m_CurrentAsset.exportSettings.sRGB = false;
                            m_CurrentAsset.exportSettings.exportAlpha = false;
                            break;
                        default: break;
                    }

                    if(!m_CurrentAsset.exportSettings.highDynamicRange)
                        m_CurrentAsset.exportSettings.sRGB = EditorGUILayout.Toggle(VFXToolboxGUIUtility.Get("sRGB (Color Data)|Whether the texture contains color (or not), HDR Data is always non sRGB."), m_CurrentAsset.exportSettings.sRGB);

                    m_CurrentAsset.exportSettings.compress = EditorGUILayout.Toggle(VFXToolboxGUIUtility.Get("Compress|Whether to apply texture compression (HDR Compressed Data does not support alpha channel)"), m_CurrentAsset.exportSettings.compress);
                    m_CurrentAsset.exportSettings.generateMipMaps = EditorGUILayout.Toggle(VFXToolboxGUIUtility.Get("Generate MipMaps|Whether generate mipmaps."), m_CurrentAsset.exportSettings.generateMipMaps);
                    m_CurrentAsset.exportSettings.wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup(VFXToolboxGUIUtility.Get("Wrap Mode|Texture Wrap mode"), m_CurrentAsset.exportSettings.wrapMode);
                    m_CurrentAsset.exportSettings.filterMode = (FilterMode)EditorGUILayout.EnumPopup(VFXToolboxGUIUtility.Get("Filter Mode|Texture Filter mode"), m_CurrentAsset.exportSettings.filterMode);

                    if(m_CurrentAsset.exportSettings.compress && m_CurrentAsset.exportSettings.highDynamicRange)
                    {
                        m_CurrentAsset.exportSettings.exportAlpha = false;
                    }

                }

                if(GUILayout.Button("Export as New...", GUILayout.Height(24)))
                {
                    string fileName = "";

                    fileName = ExportToFile(false);

                    if (fileName != "")
                    {
                        m_CurrentAsset.exportSettings.fileName = fileName;
                        m_CurrentAsset.exportSettings.frameCount = (ushort)m_ProcessingNodeStack.outputSequence.frames.Count;
                    }
                }
                // Export Again
                if( m_CurrentAsset.exportSettings.fileName != null &&
                    ((m_CurrentAsset.exportSettings.fileName.EndsWith(".tga") && m_CurrentAsset.exportSettings.exportMode == ImageSequence.ExportMode.Targa)
                    ||   (m_CurrentAsset.exportSettings.fileName.EndsWith(".exr") && m_CurrentAsset.exportSettings.exportMode == ImageSequence.ExportMode.EXR)
                    ||   (m_CurrentAsset.exportSettings.fileName.EndsWith(".png") && m_CurrentAsset.exportSettings.exportMode == ImageSequence.ExportMode.PNG)
                    ))
                {
                    if(GUILayout.Button("Update Exported Assets", GUILayout.Height(24)))
                    {
                        UpdateExportedAssets();
                    }
                }

                if (m_CurrentAsset.exportSettings.dataContents == ImageSequence.DataContents.NormalMap)
                    EditorGUILayout.HelpBox("The selected import mode assumes that the frame data is a normal map. To generate a normal map from grayscale, use Normal Map From Grayscale instead.",MessageType.Info);

                if(EditorGUI.EndChangeCheck())
                {
                    ImageSequence.ExportSettings curState = m_CurrentAsset.exportSettings;
                    m_CurrentAsset.exportSettings = prevState;
                    Undo.RecordObject(m_CurrentAsset, "Update Export Settings");
                    m_CurrentAsset.exportSettings = curState;
                    m_CurrentAssetSerializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(m_CurrentAsset);

                    AssetDatabase.Refresh();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("You do not have any frames to export.", MessageType.Warning);
            }

        }

        private void UpdateCanvasRect()
        {
            previewCanvas.displayRect = new Rect(m_Splitter.value, Styles.ToolbarHeight , position.width - m_Splitter.value, position.height - Styles.ToolbarHeight);
        }

        public void Invalidate()
        {
            m_Dirty = true;
        }

        public void UpdateViewport()
        {
            m_NeedRedraw = true;
        }

        private class CanvasConfigPopupWindowContent : PopupWindowContent
        {
            private static Styles s_Styles;
            private ImageSequencer m_Window;
            
            public CanvasConfigPopupWindowContent(ImageSequencer window)
            {
                m_Window = window;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(200, 300);
            }

            public override void OnGUI(Rect rect)
            {
                if (s_Styles == null)
                    s_Styles = new Styles();

                using (new GUILayout.AreaScope(rect))
                {
                    bool needRepaint = false;

                    using (new GUILayout.VerticalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        DoHeaderLayout("Viewport Options");
                        m_Window.previewCanvas.showGrid = GUILayout.Toggle(m_Window.previewCanvas.showGrid, VFXToolboxGUIUtility.Get("Grid Outline"), s_Styles.menuItem );
                        m_Window.previewCanvas.showExtraInfo = GUILayout.Toggle(m_Window.previewCanvas.showExtraInfo, VFXToolboxGUIUtility.Get("Frame Processor Overlays"), s_Styles.menuItem );
                        m_Window.previewCanvas.filter = GUILayout.Toggle(m_Window.previewCanvas.filter, VFXToolboxGUIUtility.Get("Texture Filtering"), s_Styles.menuItem );

                        DoHeaderLayout("Center View");
                        if(GUILayout.Button(VFXToolboxGUIUtility.Get("Fit to Window"),s_Styles.menuItem))
                        {
                            m_Window.previewCanvas.Recenter(true);
                            needRepaint = true;
                        }

                        if(GUILayout.Button(VFXToolboxGUIUtility.Get("Reset Zoom"),s_Styles.menuItem))
                        {
                            m_Window.previewCanvas.Recenter(false);
                            needRepaint = true;
                        }

                        DoHeaderLayout("Background Options");

                        if(GUILayout.Button(VFXToolboxGUIUtility.Get("Reset Brightness"),s_Styles.menuItem))
                        {
                            m_Window.previewCanvas.ResetBrightness();
                            needRepaint = true;
                        }

                        DoHeaderLayout("Processing Options");
                        m_Window.m_AutoCook = GUILayout.Toggle(m_Window.m_AutoCook, VFXToolboxGUIUtility.Get("AutoCook"), s_Styles.menuItem );

                        DoHeaderLayout("Find in Project...");

                        if(GUILayout.Button(VFXToolboxGUIUtility.Get("This Image Sequence"),s_Styles.menuItem))
                        {
                            m_Window.PingCurrentAsset();
                            
                        }

                        if(m_Window.m_CurrentAsset.exportSettings.fileName != "")
                        {
                            if(GUILayout.Button(VFXToolboxGUIUtility.Get("Exported Texture"),s_Styles.menuItem))
                            {
                                PingOutputTexture(m_Window.m_CurrentAsset.exportSettings.fileName);
                            }
                        }
                        else
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                GUILayout.Button(VFXToolboxGUIUtility.Get("Exported Texture"),s_Styles.menuItem);
                            }
                        }

                        DoHeaderLayout("Help and Feedback");
                        if(GUILayout.Button(VFXToolboxGUIUtility.Get("GitHub Repository"),s_Styles.menuItem))
                        {
                            Application.OpenURL("https://github.com/Unity-Technologies/VFXToolbox");
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            needRepaint = true;
                        }
                    }

                    if (needRepaint)
                        m_Window.Repaint();
                }

                if (Event.current.type == EventType.MouseMove)
                    Event.current.Use();

                if (Event.current.type != EventType.KeyDown || Event.current.keyCode != KeyCode.Escape)
                    return;
                this.editorWindow.Close();
                GUIUtility.ExitGUI();
            }

            private void DoHeaderLayout(string headerText)
            {
                GUILayout.Label(GUIContent.none, s_Styles.separator);
                GUILayout.Label(VFXToolboxGUIUtility.Get(headerText), EditorStyles.boldLabel);
            }

            private class Styles
            {
                public readonly GUIStyle menuItem;
                public readonly GUIStyle separator;

                public Styles()
                {
                    menuItem = new GUIStyle((GUIStyle)"MenuItem");
                    menuItem.fixedHeight += 2;
                    separator = new GUIStyle((GUIStyle)"sv_iconselector_sep");
                }
            }
        }

    }
}
