using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class RemapColorProcessor : GPUFrameProcessor<RemapColorProcessorSettings>
    {
        Texture2D m_GradientTexture;

        public RemapColorProcessor(FrameProcessorStack stack, ProcessorInfo info)
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/RemapColor.shader", stack,info)
        {

            if (settings.Gradient == null)
                settings.DefaultGradient();
        }

        public override string GetName()
        {
            return "Remap Color";
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            return false;
        }

        private void InitTexture()
        {
            m_GradientTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false, false);
            m_GradientTexture.wrapMode = TextureWrapMode.Clamp;
            m_GradientTexture.filterMode = FilterMode.Bilinear;
            CurveToTextureUtility.GradientToTexture(settings.Gradient, ref m_GradientTexture);
        }

        public override bool Process(int frame)
        {
            if(m_GradientTexture == null)
            {
                InitTexture();
            }

            CurveToTextureUtility.GradientToTexture(settings.Gradient, ref m_GradientTexture);
            Texture inputFrame = InputSequence.RequestFrame(frame).texture;
            m_Material.SetTexture("_MainTex", inputFrame);

            m_Material.SetFloat("_Mode", (int)settings.ColorSource);

            m_Material.SetTexture("_Gradient", m_GradientTexture);

            ExecuteShaderAndDump(frame, inputFrame);
            return true;
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            var colorSource = m_SerializedObject.FindProperty("ColorSource");
            var gradient = m_SerializedObject.FindProperty("Gradient");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(colorSource, VFXToolboxGUIUtility.Get("Color Source"));
            EditorGUILayout.PropertyField(gradient, VFXToolboxGUIUtility.Get("Remap Gradient"));

            if(EditorGUI.EndChangeCheck())
            {
                Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }
    }
}
