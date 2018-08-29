using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor.VFXToolbox.VolumeEditor
{
    public class VolumeEditor : EditorWindow
    {

        [MenuItem("Window/Visual Effects/Volume Editor")]
        public static void OpenVolumeEditor()
        {
            EditorWindow.GetWindow<VolumeEditor>();
        }

        // Debug, temporary
        [MenuItem("Window/Visual Effects/VF File Importer (Texture3D)")]
        public static void Create3DTexture()
        {
            string filename = EditorUtility.OpenFilePanel("Open VF File", "", "vf");
            if(filename != null && System.IO.File.Exists(filename))
            {
                Texture3D texture = VFFileImporter.LoadVFFile(filename, TextureFormat.ARGB32);
                string assetFileName = EditorUtility.SaveFilePanelInProject("Save 3D Texture", System.IO.Path.GetFileNameWithoutExtension(filename) + ".asset", "asset", "");
                if(assetFileName != null)
                {
                    AssetDatabase.CreateAsset(texture, assetFileName);
                }
            }
        }

        private VolumeEditorPreview m_Preview;

        private void Initialize()
        {
            if (m_Preview == null)
            {
                m_Preview = new VolumeEditorPreview();
                m_Preview.Initialize();
            }
        }

        private void HandleDropData()
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            if( Event.current.type == EventType.DragExited)
            {
                if(DragAndDrop.paths.Length > 0)
                {
                    m_Preview.LoadTexture(DragAndDrop.paths[0]);
                }
            }
        }

        public void OnGUI()
        {
            Initialize();
            HandleDropData();
            titleContent = VFXToolboxGUIUtility.Get("Volume Editor");

            if (Event.current.type == EventType.Repaint)
            {
                m_Preview.Render(position);
                GUI.DrawTexture(new Rect(0,0,position.width, position.height), m_Preview.texture);
            }

            bool bValueChanged = false;

            Rect previewWindowRect = new Rect(16, 16, 320, 360);

            BeginWindows();
            GUI.Window(0, previewWindowRect, DrawPreviewRendererGUIWindow, "Preview Options");
            EndWindows();

            if(!bValueChanged)
            {
                m_Preview.HandleMouse(position);
            }

            Repaint();
        }

        public void DrawPreviewRendererGUIWindow(int id)
        {
            m_Preview.activeRendererIndex = EditorGUILayout.IntPopup("Preview Mode",m_Preview.activeRendererIndex, m_Preview.GetRendererNames(), m_Preview.GetRendererValues());
            EditorGUILayout.Space();
            m_Preview.OnRendererGUI();
        }
    }
}
