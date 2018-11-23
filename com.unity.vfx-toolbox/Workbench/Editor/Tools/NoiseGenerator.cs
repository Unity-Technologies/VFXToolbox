using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.VFXToolbox;
using System;

namespace UnityEditor.VFXToolbox.Workbench
{
    public class NoiseGenerator : WorkbenchToolBase
    {
        [MenuItem("Assets/Create/Visual Effects/Workbench/Noise Generator", priority = 301)]
        private static void MenuCreateAsset()
        {
            WorkbenchBehaviourFactory.CreateWorkbenchAsset("New Noise Generator", CreateInstance<NoiseGenerator>());
        }
        // Serializable
        public int Width;
        public int Height;
        public int Depth;
        public float Phase;

        // Runtime
        private RenderTexture m_RenderTexture;
        private Material m_Material;

        // Serialziation
        private SerializedObject m_Object;
        private SerializedProperty m_Width;
        private SerializedProperty m_Height;
        private SerializedProperty m_Depth;
        private SerializedProperty m_Phase;

        public void OnEnable()
        {
            m_Object = new SerializedObject(this);
            m_Width = m_Object.FindProperty("Width");
            m_Height = m_Object.FindProperty("Height");
            m_Depth = m_Object.FindProperty("Depth");
            m_Phase = m_Object.FindProperty("Phase");
        }

        public override void Dispose()
        {
            RenderTexture.ReleaseTemporary(m_RenderTexture);
        }

        public override void Default(WorkbenchBehaviour asset) 
        {
            base.Default(asset);
            Width = 256;
            Height = 256;
            Depth = 5;
            Phase = 0.0f;
        }

        public void UpdateRenderTarget()
        {
            if (m_RenderTexture.width != Width || m_RenderTexture.height != Height)
            {
                RenderTexture newRT = RenderTexture.GetTemporary(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                RenderTexture.ReleaseTemporary(m_RenderTexture);
                m_RenderTexture = newRT;
                Clear();
            }
        }

        private void Clear()
        {
            RenderTexture backup = RenderTexture.active;
            RenderTexture.active = m_RenderTexture;
            GL.Clear(false, true, new Color(0.5f, 0.0f, 0.5f, 1.0f));
            RenderTexture.active = backup;
        }

        public override void InitializeRuntime()
        {
            m_RenderTexture = RenderTexture.GetTemporary(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            m_Material = new Material(Shader.Find("Hidden/VFXToolbox/ImageScripter/NoiseGen"));
        }

        protected override WorkbenchCanvasBase GetCanvas(Workbench window)
        {
            return new WorkbenchImageCanvas(window);
        }

        public override bool OnCanvasGUI(WorkbenchImageCanvas canvas)
        {
            canvas.texture = m_RenderTexture;
            return false;
        }

        public override bool OnInspectorGUI()
        {
            bool changed = false;
            m_Object.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_Width);
            EditorGUILayout.PropertyField(m_Height);
            EditorGUILayout.IntSlider(m_Depth, 1, 7);
            EditorGUILayout.Slider(m_Phase, 0.0f, 50.0f);
            if (EditorGUI.EndChangeCheck())
            {
                changed = true;
            }

            m_Object.ApplyModifiedProperties();

            if (changed)
            {
                UpdateRenderTarget();
                Update();
                return true;
            }

            return false;
        }

        public override void Update()
        {
            m_Material.SetInt("_depth", Depth);
            m_Material.SetInt("_width", Width);
            m_Material.SetInt("_height", Height);
            m_Material.SetFloat("_phase", Phase);

            RenderTexture bkp = RenderTexture.active;
            RenderTexture.active = m_RenderTexture;

#if !UNITY_2018_2_OR_NEWER
            GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
#endif

            Graphics.Blit(null, m_RenderTexture, m_Material);

#if !UNITY_2018_2_OR_NEWER
            GL.sRGBWrite = false;
#endif
            RenderTexture.active = bkp;
        }

        public static string GetCategory()
        { return "Misc"; }
        public static string GetName()
        { return "Noise Generator"; }

    }
}
