using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class ColorCorrectionProcessor : GPUFrameProcessor<ColorCorrectionProcessorSettings>
    {
        Texture2D m_CurveTexture;
        CurveDrawer m_CurveDrawer;

        public ColorCorrectionProcessor(FrameProcessorStack stack, ProcessorInfo info)
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/ColorCorrection.shader", stack,info)
        {
            if(m_CurveDrawer == null)
            {
                m_CurveDrawer = new CurveDrawer(null, 0.0f, 1.0f, 0.0f, 1.0f, 140, false);
                m_CurveDrawer.AddCurve(m_SerializedObject.FindProperty("AlphaCurve"), new Color(1.0f, 0.55f, 0.1f), "Alpha Curve");
            }

            if (settings.AlphaCurve == null)
                settings.DefaultCurve();
        }

        public override string GetName()
        {
            return "Color Correction";
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            return false;
        }

        private void InitTexture()
        {
            m_CurveTexture = new Texture2D(256, 1, TextureFormat.RGBAHalf, false, true);
            m_CurveTexture.wrapMode = TextureWrapMode.Clamp;
            m_CurveTexture.filterMode = FilterMode.Bilinear;
            CurveToTextureUtility.CurveToTexture(settings.AlphaCurve, ref m_CurveTexture);
        }

        public override bool Process(int frame)
        {
            if(m_CurveTexture == null)
            {
                InitTexture();
            }

            CurveToTextureUtility.CurveToTexture(settings.AlphaCurve, ref m_CurveTexture);
            Texture inputFrame = InputSequence.RequestFrame(frame).texture;
            m_Material.SetTexture("_MainTex", inputFrame);
            m_Material.SetFloat("_Brightness", settings.Brightness);
            m_Material.SetFloat("_Contrast", settings.Contrast);
            m_Material.SetFloat("_Saturation", settings.Saturation);

            m_Material.SetTexture("_AlphaCurve", m_CurveTexture);

            ExecuteShaderAndDump(frame, inputFrame);
            return true;
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            var brightness = m_SerializedObject.FindProperty("Brightness");
            var contrast = m_SerializedObject.FindProperty("Contrast");
            var saturation = m_SerializedObject.FindProperty("Saturation");
            var alphaCurve =  m_SerializedObject.FindProperty("AlphaCurve");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(brightness, VFXToolboxGUIUtility.Get("Brightness"));
            EditorGUILayout.PropertyField(contrast, VFXToolboxGUIUtility.Get("Contrast"));
            EditorGUILayout.PropertyField(saturation, VFXToolboxGUIUtility.Get("Saturation"));

            bool curveChanged = false;

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(VFXToolboxGUIUtility.Get("Alpha Curve"), GUILayout.Width(EditorGUIUtility.labelWidth));
                if(GUILayout.Button(VFXToolboxGUIUtility.Get("Reset")))
                {
                    alphaCurve.animationCurveValue = AnimationCurve.Linear(0, 0, 1, 1);
                    m_CurveDrawer.ClearSelection();
                    curveChanged = true;
                }
            }
            if(!curveChanged)
            curveChanged = m_CurveDrawer.OnGUILayout();

            if(EditorGUI.EndChangeCheck() || curveChanged)
            {
                Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }
    }
}
