using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEditor.VFXToolbox.Workbench
{
    public class Workbench : EditorWindow
    {
        Splitter m_Splitter;
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
            m_Asset.tool.InitializeRuntime();
            m_Asset.tool.InitializeEditor(this);
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
                m_Asset.tool.InitializeRuntime();
                m_Asset.tool.InitializeEditor(this);
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

            using (new GUILayout.AreaScope(toolbarRect, GUIContent.none, EditorStyles.toolbar))
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(m_Splitter.value);
                OnDrawToolbar();
                GUILayout.FlexibleSpace();
            }

            m_Splitter.DoSplitter(splitterRect);

            if (m_Dirty)
                Repaint();
        }

        private void OnDrawToolbar()
        {
            // Get the toolbar from the current asset
            if (m_Asset != null)
            {
                m_Asset.tool.canvas.OnToolbarGUI();
            }
        }

        private void OnDrawCanvas(Rect rect)
        {
            // Get the canvas from the current asset
            if (m_Asset != null)
            {
                m_Asset.tool.canvas.OnGUI(rect, m_Asset.tool);
            }
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

            public Styles()
            {
                m_Inspector = new GUIStyle(EditorStyles.inspectorDefaultMargins);
            }
        }

        #endregion
    }
}
