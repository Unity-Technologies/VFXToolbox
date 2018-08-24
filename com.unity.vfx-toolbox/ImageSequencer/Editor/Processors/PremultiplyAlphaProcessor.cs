using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class PremultiplyAlphaProcessor : GPUFrameProcessor<PremultiplyAlphaProcessorSettings>
    {
        public PremultiplyAlphaProcessor(FrameProcessorStack stack, ProcessorInfo info)
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/PremultiplyAlpha.shader", stack,info)
        { }

        public override string GetName()
        {
            return "Premultiply Alpha";
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            return false;
        }

        public override bool Process(int frame)
        {
            Texture inputFrame = InputSequence.RequestFrame(frame).texture;
            m_Material.SetTexture("_MainTex", inputFrame);
            m_Material.SetInt("_RemoveAlpha", settings.RemoveAlpha?1:0);
            m_Material.SetFloat("_AlphaValue", settings.AlphaValue);
            ExecuteShaderAndDump(frame, inputFrame);
            return true;
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            var removeAlpha = m_SerializedObject.FindProperty("RemoveAlpha");
            var alphaValue = m_SerializedObject.FindProperty("AlphaValue");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(removeAlpha, VFXToolboxGUIUtility.Get("Remove Alpha"));
            EditorGUI.BeginDisabledGroup(!removeAlpha.boolValue);
            EditorGUILayout.PropertyField(alphaValue, VFXToolboxGUIUtility.Get("Alpha Value"));
            EditorGUI.EndDisabledGroup();

            if(EditorGUI.EndChangeCheck())
            {
                Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }

    }
}
