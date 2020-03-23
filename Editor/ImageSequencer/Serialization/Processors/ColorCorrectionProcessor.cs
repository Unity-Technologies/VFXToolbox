using UnityEngine;
using UnityEngine.VFXToolbox;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("Color","Color Correction")]
    internal class ColorCorrectionProcessor : ProcessorBase
    {
        [FloatSlider(0.0f,2.0f)]
        public float Brightness;
        [FloatSlider(0.0f,2.0f)]
        public float Contrast;
        [FloatSlider(0.0f,2.0f)]
        public float Saturation;

        public AnimationCurve AlphaCurve;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/ColorCorrection.shader";

        public override string processorName => "Color Correction";

        public override void Default()
        {
            Brightness = 1.0f;
            Contrast = 1.0f;
            Saturation = 1.0f;
            DefaultCurve();
        }

        public void DefaultCurve()
        {
            AlphaCurve = AnimationCurve.Linear(0, 0, 1, 1);
        }
        public override bool Process(int frame)
        {
            if (m_CurveTexture == null)
            {
                InitTexture();
            }

            CurveToTextureUtility.CurveToTexture(AlphaCurve, ref m_CurveTexture);
            Texture inputFrame = RequestInputTexture(frame);
            material.SetTexture("_MainTex", inputFrame);
            material.SetFloat("_Brightness", Brightness);
            material.SetFloat("_Contrast", Contrast);
            material.SetFloat("_Saturation", Saturation);

            material.SetTexture("_AlphaCurve", m_CurveTexture);

            ProcessFrame(frame, inputFrame);
            return true;
        }

        private void InitTexture()
        {
            m_CurveTexture = new Texture2D(256, 1, TextureFormat.RGBAHalf, false, true);
            m_CurveTexture.wrapMode = TextureWrapMode.Clamp;
            m_CurveTexture.filterMode = FilterMode.Bilinear;
            CurveToTextureUtility.CurveToTexture(AlphaCurve, ref m_CurveTexture);
        }

        Texture2D m_CurveTexture;
        CurveDrawer m_CurveDrawer;

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            if (m_CurveDrawer == null)
            {
                m_CurveDrawer = new CurveDrawer(null, 0.0f, 1.0f, 0.0f, 1.0f, 140, false);
                m_CurveDrawer.AddCurve(serializedObject.FindProperty("AlphaCurve"), new Color(1.0f, 0.55f, 0.1f), "Alpha Curve");
            }

            if (AlphaCurve == null)
                DefaultCurve();

            var brightness = serializedObject.FindProperty("Brightness");
            var contrast = serializedObject.FindProperty("Contrast");
            var saturation = serializedObject.FindProperty("Saturation");
            var alphaCurve = serializedObject.FindProperty("AlphaCurve");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(brightness, VFXToolboxGUIUtility.Get("Brightness"));
            EditorGUILayout.PropertyField(contrast, VFXToolboxGUIUtility.Get("Contrast"));
            EditorGUILayout.PropertyField(saturation, VFXToolboxGUIUtility.Get("Saturation"));

            bool curveChanged = false;

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(VFXToolboxGUIUtility.Get("Alpha Curve"), GUILayout.Width(EditorGUIUtility.labelWidth));
                if (GUILayout.Button(VFXToolboxGUIUtility.Get("Reset")))
                {
                    alphaCurve.animationCurveValue = AnimationCurve.Linear(0, 0, 1, 1);
                    m_CurveDrawer.ClearSelection();
                    curveChanged = true;
                }
            }
            if (!curveChanged)
                curveChanged = m_CurveDrawer.OnGUILayout();

            if (EditorGUI.EndChangeCheck() || curveChanged)
            {
                Invalidate();
                changed = true;
            }

            return changed;
        }


    }
}

