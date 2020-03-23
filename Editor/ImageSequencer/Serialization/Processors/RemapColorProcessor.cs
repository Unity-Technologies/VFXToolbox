using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("Color","Remap Color")]
    class RemapColorProcessor: ProcessorBase
    {
        public enum RemapColorSource
        {
            sRGBLuminance = 0,
            LinearRGBLuminance = 1,
            Alpha = 2,
            LinearR = 3,
            LinearG = 4,
            LinearB = 5
        }

        public Gradient Gradient;
        public RemapColorSource ColorSource;


        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/RemapColor.shader";

        public override string processorName => "Remap Color";

        public override void Default()
        {
            ColorSource = RemapColorSource.sRGBLuminance;
            DefaultGradient();
        }

        public void DefaultGradient()
        {
            Gradient = new Gradient();
            GradientColorKey[] colors = new GradientColorKey[2] { new GradientColorKey(Color.black, 0),new GradientColorKey(Color.white, 1) };
            GradientAlphaKey[] alpha = new GradientAlphaKey[2] { new GradientAlphaKey(0,0), new GradientAlphaKey(1,1) };
            Gradient.SetKeys(colors, alpha);
        }

        public override bool Process(int frame)
        {
            if (m_GradientTexture == null)
            {
                InitTexture();
            }

            CurveToTextureUtility.GradientToTexture(Gradient, ref m_GradientTexture);
            Texture inputFrame = RequestInputTexture(frame);
            material.SetTexture("_MainTex", inputFrame);

            material.SetFloat("_Mode", (int)ColorSource);

            material.SetTexture("_Gradient", m_GradientTexture);

            ProcessFrame(frame, inputFrame);
            return true;
        }

        Texture2D m_GradientTexture;
        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            var colorSource = serializedObject.FindProperty("ColorSource");
            var gradient = serializedObject.FindProperty("Gradient");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(colorSource, VFXToolboxGUIUtility.Get("Color Source"));
            EditorGUILayout.PropertyField(gradient, VFXToolboxGUIUtility.Get("Remap Gradient"));

            if (EditorGUI.EndChangeCheck())
            {
                Invalidate();
                changed = true;
            }

            return changed;
        }

        private void InitTexture()
        {
            m_GradientTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false, false);
            m_GradientTexture.wrapMode = TextureWrapMode.Clamp;
            m_GradientTexture.filterMode = FilterMode.Bilinear;
            CurveToTextureUtility.GradientToTexture(Gradient, ref m_GradientTexture);
        }
    }
}
