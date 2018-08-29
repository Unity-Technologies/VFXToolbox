using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace UnityEditor.VFXToolbox.VolumeEditor
{
    public class VolumeEditorPreview
    {

        private enum NavMode
        {
            None = 0,
            Zooming = 1,
            Rotating = 2
        }

        public Texture texture { get { return m_PreviewTexture; } }
        public List<VolumeRendererBase> renderers { get { return m_Renderers; } }
        public int activeRendererIndex { get { return GetPreviewMode(); } set { SetPreviewMode(value); } }

        private PreviewRenderUtility m_previewUtility;
        private Texture m_PreviewTexture;
        private Texture3D m_Volume;
        private Material m_BackgroundMaterial;

        private List<VolumeRendererBase> m_Renderers;
        private VolumeRendererBase m_ActiveRenderer;

        private float m_CameraPhi = 0.75f;
        private float m_CameraTheta = 0.5f;
        private float m_CameraDistance = 2.0f;

        private NavMode m_NavMode = NavMode.None;
        private Vector2 m_PreviousMousePosition = Vector2.zero;
        private DummyRenderer m_DummyRenderer;

        public VolumeEditorPreview()
        {

        }

        public void Initialize()
        {
            if(m_BackgroundMaterial == null)
            {
                m_BackgroundMaterial = new Material(Shader.Find("Hidden/VFXToolbox/Skybox/Gradient"));
                m_BackgroundMaterial.SetFloat("_VerticalFalloff", 4);
                m_BackgroundMaterial.SetFloat("_DitherIntensity", 0.45f);
            }
            if(m_previewUtility == null)
            {
                m_previewUtility = new PreviewRenderUtility(false);
                m_previewUtility.cameraFieldOfView = 50.0f;
                m_previewUtility.camera.nearClipPlane = 0.1f;
                m_previewUtility.camera.farClipPlane = 100.0f;
                m_previewUtility.camera.transform.position = new Vector3(3, 2, 3);
                m_previewUtility.camera.transform.LookAt(Vector3.zero);
                m_previewUtility.camera.renderingPath = RenderingPath.Forward;
                m_previewUtility.camera.allowHDR = false;
                m_previewUtility.camera.clearFlags = CameraClearFlags.Skybox;
                m_previewUtility.lights[0].shadows = LightShadows.None;
            }

            if(m_Renderers == null)
            {
                m_Renderers = new List<VolumeRendererBase>();
                m_Renderers.Add(new DensityVolumeRenderer());
                m_Renderers.Add(new VectorFieldVolumeRenderer());
                m_ActiveRenderer = m_Renderers[0];
            }

            if(m_DummyRenderer == null)
            {
                m_DummyRenderer = new DummyRenderer();
            }
        }

        public void LoadTexture(string path)
        {
            Texture3D texture = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
            if(texture != null)
            {
                m_Volume = texture;
                m_ActiveRenderer.SetTexture(m_Volume);
            }
        }

        public void OnRendererGUI()
        {
            Initialize();
            m_ActiveRenderer.OnGUI();
        }

        private void UpdateCamera()
        {
            Vector3 pos = new Vector3( Mathf.Sin(m_CameraPhi)* Mathf.Cos(m_CameraTheta), Mathf.Cos(m_CameraPhi), Mathf.Sin(m_CameraPhi) * Mathf.Sin(m_CameraTheta)) * m_CameraDistance;
            m_previewUtility.camera.transform.position = pos;
            m_previewUtility.camera.transform.LookAt(Vector3.zero);
        }

        public void HandleMouse(Rect Viewport)
        {
            if(Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 0)
                    m_NavMode = NavMode.Rotating;
                else if (Event.current.button == 1)
                    m_NavMode = NavMode.Zooming;

                m_PreviousMousePosition = Event.current.mousePosition;
            }
            if (Event.current.type == EventType.MouseUp || Event.current.rawType == EventType.MouseUp)
                m_NavMode = NavMode.None;

            if(m_NavMode != NavMode.None)
            {
                Vector2 mouseDelta = Event.current.mousePosition - m_PreviousMousePosition;
                switch(m_NavMode)
                {
                    case NavMode.Rotating:
                        m_CameraTheta = (m_CameraTheta - mouseDelta.x * 0.003f) % (Mathf.PI * 2);
                        m_CameraPhi = Mathf.Clamp(m_CameraPhi - mouseDelta.y *0.003f, 0.2f, Mathf.PI - 0.2f);
                        break;
                    case NavMode.Zooming:
                        m_CameraDistance = Mathf.Clamp(mouseDelta.y * 0.01f + m_CameraDistance, 1, 10);
                        break;
                }
            }

            m_PreviousMousePosition = Event.current.mousePosition;
        }

        public void Render(Rect viewportRect)
        {
            Initialize();

            UpdateCamera();
            Material backupSkybox = RenderSettings.skybox;
            RenderSettings.skybox = m_BackgroundMaterial;
            m_previewUtility.BeginPreview(viewportRect, GUIStyle.none);
            
            if(m_Volume != null)
            {
                m_ActiveRenderer.Render(m_previewUtility);
            }
            else
            {
                m_DummyRenderer.Render(m_previewUtility);
            }

            m_previewUtility.camera.Render();
            m_PreviewTexture = m_previewUtility.EndPreview();

            RenderSettings.skybox = backupSkybox;
        }

        public int GetPreviewMode()
        {
            return m_Renderers.IndexOf(m_ActiveRenderer);
        }

        public void SetPreviewMode(int mode)
        {
            if(mode != m_Renderers.IndexOf(m_ActiveRenderer))
            {
                m_ActiveRenderer = m_Renderers[mode];
                if(m_Volume != null)
                    m_ActiveRenderer.SetTexture(m_Volume);
            }
        }

        public string[] GetRendererNames()
        {
            string[] names = new string[m_Renderers.Count];
            for(int i =0; i < m_Renderers.Count; i++)
            {
                names[i] = m_Renderers[i].ToString();
            }
            return names;
        }

        public int[] GetRendererValues()
        {
            int[] values = new int[m_Renderers.Count];
            for(int i =0; i < m_Renderers.Count; i++)
            {
                values[i] = i;
            }
            return values;
        }
    }
}
