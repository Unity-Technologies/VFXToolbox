using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEditor.VFXToolbox.Workbench
{
    public class Workbench : EditorWindow
    {
        Splitter m_Splitter;
        WorkbenchImageCanvas m_Canvas;
        Texture2D m_Texture;
        private bool m_Dirty;

        WorkbenchBehaviour m_Asset;

        [MenuItem("Window/Visual Effects/Workbench")]
        public static void OpenEditor()
        {
            GetWindow(typeof(Workbench));
        }

        public void OnEnable()
        {
            if(m_Asset != null)
            {
                LoadAsset(m_Asset);
            }

            Selection.selectionChanged -= OnSelectionChange;
            Selection.selectionChanged += OnSelectionChange;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public void OnDisable()
        {
            if(m_Asset != null)
            {
                UnloadAsset();
            }
            Selection.selectionChanged -= OnSelectionChange;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public void OnUndoRedo()
        {
            if(m_Asset != null)
            {
                ReloadAsset();
            }
        }

        public void OnSelectionChange()
        {
            if (Selection.activeObject is WorkbenchBehaviour)
            {
                LoadAsset(Selection.activeObject as WorkbenchBehaviour);
            }
        }

        public void ReloadAsset()
        {
            m_Asset.tool.Dispose();
            m_Asset.tool.Initialize();
            Invalidate();
        }

        public void LoadAsset(WorkbenchBehaviour asset)
        {
            if (m_Asset == asset)
                return; // Not gonna reload the same asset

            if (m_Asset != null && m_Asset != asset)
            {
                UnloadAsset();
                wantsMouseMove = false;
            }

            m_Asset = asset;

            if(m_Asset.tool != null)
            {
                m_Asset.tool.Initialize();
                wantsMouseMove = true;
            }

            Invalidate();
        }

        public void UnloadAsset()
        {
            if(m_Asset.tool != null)
                m_Asset.tool.Dispose();
            m_Asset = null;
        }

        public void OnGUI()
        {
            m_Dirty = false;
            string title = "Workbench";
            if(m_Asset != null && m_Asset.tool != null)
            {
                title = m_Asset.tool.name;
            }
            titleContent = VFXToolboxGUIUtility.GetTextAndIcon(title, "SettingsIcon");

            Rect toolbarRect = new Rect(0, 0, position.width, 18);
            Rect splitterRect = new Rect(0,18,position.width , position.height-18);

            if (m_Splitter == null)
                m_Splitter = new Splitter(320.0f, OnDrawInspector, OnDrawCanvas, Splitter.SplitLockMode.LeftMinMax, new Vector2(320,480));

            if (m_Canvas == null)
                m_Canvas = new WorkbenchImageCanvas(splitterRect,this);

            using (new GUILayout.AreaScope(toolbarRect, GUIContent.none, EditorStyles.toolbar))
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(m_Splitter.value);
                OnDrawToolbar();
                GUILayout.FlexibleSpace();
            }

            m_Splitter.DoSplitter(splitterRect);

            HandleDropData();

            if (m_Dirty)
                Repaint();
        }

        private void OnDrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                bool prev;

                bool bMaskR = m_Canvas.maskR;
                bool bMaskG = m_Canvas.maskG;
                bool bMaskB = m_Canvas.maskB;
                bool bMaskA = m_Canvas.maskA;
                bool bMaskRGB = bMaskR && bMaskG && bMaskB;

                bMaskRGB = GUILayout.Toggle(bMaskRGB, styles.iconRGB, EditorStyles.toolbarButton);

                if(bMaskRGB != (bMaskR && bMaskG && bMaskB))
                {
                    bMaskR = bMaskG = bMaskB = bMaskRGB;

                    m_Canvas.maskR = bMaskR;
                    m_Canvas.maskG = bMaskG;
                    m_Canvas.maskB = bMaskB;

                }

                prev = bMaskR;
                bMaskR = GUILayout.Toggle(bMaskR, VFXToolboxGUIUtility.Get("R"),styles.MaskRToggle);

                if (bMaskR != prev)
                    m_Canvas.maskR = bMaskR;

                prev = bMaskG;
                bMaskG = GUILayout.Toggle(bMaskG, VFXToolboxGUIUtility.Get("G"),styles.MaskGToggle);
                if (bMaskG != prev)
                    m_Canvas.maskG = bMaskG;

                prev = bMaskB;
                bMaskB = GUILayout.Toggle(bMaskB, VFXToolboxGUIUtility.Get("B"),styles.MaskBToggle);
                if (bMaskB != prev)
                    m_Canvas.maskB = bMaskB;

                prev = bMaskA;
                bMaskA = GUILayout.Toggle(bMaskA, VFXToolboxGUIUtility.Get("A"),styles.MaskAToggle);
                if (bMaskA != prev)
                    m_Canvas.maskA = bMaskA;


                GUILayout.Space(20);
                {
                    Rect brightnessRect = GUILayoutUtility.GetRect(160, 24);
                    GUI.Box(brightnessRect, GUIContent.none, EditorStyles.toolbarButton);
                    GUI.Label(new RectOffset(4, 0, 0, 0).Remove(brightnessRect), VFXToolboxGUIUtility.GetTextAndIcon("Background|Sets the Background Brightness", "CheckerFloor"), EditorStyles.miniLabel);

                    float newBrightness = GUI.HorizontalSlider(new RectOffset(82, 4, 0, 0).Remove(brightnessRect), m_Canvas.BackgroundBrightness, 0.0f, 1.0f);
                    if (m_Canvas.BackgroundBrightness != newBrightness)
                        m_Canvas.BackgroundBrightness = newBrightness;
                }

                GUILayout.FlexibleSpace();
            }
            
        }

        private void OnDrawCanvas(Rect rect)
        {
            m_Canvas.displayRect = rect;

            if (m_Asset != null)
                m_Canvas.OnGUI(m_Asset.tool);
            else
                m_Canvas.OnGUI();
        }

        private void OnDrawInspector(Rect rect)
        {
            if(m_Asset != null)
            {
                using (new GUILayout.AreaScope(rect, GUIContent.none, styles.inspector))
                {
                    if(m_Asset.tool != null)
                    {
                        EditorGUILayout.InspectorTitlebar(false, m_Asset.tool);
                        m_Asset.tool.OnInspectorGUI();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No Script Currently Assigned, please select one using the menu", MessageType.Warning);
                        using(new GUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("New Script", GUILayout.Width(EditorGUIUtility.labelWidth));
                
                            if (GUILayout.Button("Select", EditorStyles.popup))
                            {
                                GenericMenu menu = new GenericMenu();
                                var types = VFXToolboxUtility.FindConcreteSubclasses<WorkbenchToolBase>();

                                foreach(Type t in types)
                                {
                                    string category = WorkbenchToolBase.GetCategory(t);
                                    string name = WorkbenchToolBase.GetName(t);
                                    string path = (category.Length > 0 ? category + "/" : "") + name;
                                    menu.AddItem(VFXToolboxGUIUtility.Get(path), false, AddObject, t);
                                }
                                menu.ShowAsContext();
                            }
                        }
                    }
                }
            }
            else
            {
                using (new GUILayout.AreaScope(rect, GUIContent.none, styles.inspector))
                {
                    EditorGUILayout.HelpBox("No Asset Selected, please create an asset inside the Project Window to Edit It", MessageType.Warning);
                }
            }
        }

        public void AddObject(object o)
        {
            WorkbenchInspector inspector = (WorkbenchInspector)Editor.CreateEditor(m_Asset);
            inspector.AddObject(o as Type);
        }

        public void Invalidate()
        {
            m_Dirty = true;
        }

        private bool HandleDropData()
        {
            if(DragAndDrop.paths.Length > 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if( Event.current.type == EventType.DragExited)
                {
                    foreach(string path in DragAndDrop.paths)
                    {
                      Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        if(t != null)
                        {
                            m_Canvas.texture = t;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        #region styles
        public static Styles styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new Styles();
                return 
                    s_Styles;
            }
        }

        private static Styles s_Styles;

        public class Styles
        {
            public readonly GUIStyle separator = (GUIStyle)"sv_iconselector_sep";
            public GUIStyle inspector { get { return m_Inspector; } }
            private GUIStyle m_Inspector;

            public readonly GUIContent iconRGB = EditorGUIUtility.IconContent("PreTextureRGB", "Toggle RGB/Alpha only");
            public readonly GUIContent iconMipMapUp = EditorGUIUtility.IconContent("PreTextureMipMapLow", "Go one MipMap up (smaller size)");
            public readonly GUIContent iconMipMapDown = EditorGUIUtility.IconContent("PreTextureMipMapHigh", "Go one MipMap down (higher size)");

            public GUIStyle MaskRToggle { get { if (EditorGUIUtility.isProSkin) return m_MaskRTogglePro; else return m_MaskRToggle; } }
            public GUIStyle MaskGToggle { get { if (EditorGUIUtility.isProSkin) return m_MaskGTogglePro; else return m_MaskGToggle; } }
            public GUIStyle MaskBToggle { get { if (EditorGUIUtility.isProSkin) return m_MaskBTogglePro; else return m_MaskBToggle; } }
            public GUIStyle MaskAToggle { get { if (EditorGUIUtility.isProSkin) return m_MaskATogglePro; else return m_MaskAToggle; } }

            private GUIStyle m_MaskRToggle;
            private GUIStyle m_MaskRTogglePro;
            private GUIStyle m_MaskGToggle;
            private GUIStyle m_MaskGTogglePro;
            private GUIStyle m_MaskBToggle;
            private GUIStyle m_MaskBTogglePro;
            private GUIStyle m_MaskAToggle;
            private GUIStyle m_MaskATogglePro;

            public Styles()
            {
                m_Inspector = new GUIStyle(EditorStyles.inspectorDefaultMargins);

                m_MaskRToggle = new GUIStyle(EditorStyles.toolbarButton);
                m_MaskGToggle = new GUIStyle(EditorStyles.toolbarButton);
                m_MaskBToggle= new GUIStyle(EditorStyles.toolbarButton);
                m_MaskAToggle= new GUIStyle(EditorStyles.toolbarButton);

                m_MaskRToggle.onNormal.textColor = new Color(1.0f, 0.0f, 0.0f);
                m_MaskGToggle.onNormal.textColor = new Color(0.0f, 0.6f, 0.2f);
                m_MaskBToggle.onNormal.textColor = new Color(0.0f, 0.2f, 1.0f);
                m_MaskAToggle.onNormal.textColor = new Color(0.5f, 0.5f, 0.5f);

                m_MaskRTogglePro = new GUIStyle(EditorStyles.toolbarButton);
                m_MaskGTogglePro= new GUIStyle(EditorStyles.toolbarButton);
                m_MaskBTogglePro= new GUIStyle(EditorStyles.toolbarButton);
                m_MaskATogglePro= new GUIStyle(EditorStyles.toolbarButton);

                m_MaskRTogglePro.onNormal.textColor = new Color(2.0f, 0.3f, 0.3f);
                m_MaskGTogglePro.onNormal.textColor = new Color(0.5f, 2.0f, 0.1f);
                m_MaskBTogglePro.onNormal.textColor = new Color(0.2f, 0.6f, 2.0f);
                m_MaskATogglePro.onNormal.textColor = new Color(2.0f, 2.0f, 2.0f);
            }
        }

        #endregion
    }
}
