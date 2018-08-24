using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class AlphaFromRGBProcessor : GPUFrameProcessor<AlphaFromRGBProcessorSettings>
    {
        public AlphaFromRGBProcessor(FrameProcessorStack stack, ProcessorInfo info)
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/AlphaFromRGB.shader", stack,info)
        { }

        public override string GetName()
        {
            return "Alpha From RGB";
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            return false;
        }

        public override bool Process(int frame)
        {
            Texture inputFrame = InputSequence.RequestFrame(frame).texture;
            m_Material.SetTexture("_MainTex", inputFrame);
            m_Material.SetVector("_RGBTint", settings.BWFilterTint);

            ExecuteShaderAndDump(frame, inputFrame);
            return true;
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            var tint = m_SerializedObject.FindProperty("BWFilterTint");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(tint, VFXToolboxGUIUtility.Get("Color Filter"));
            EditorGUILayout.HelpBox("Color Filter serves as a tint before applying the black and white desaturation, just like in black and white photography. This way you can filter color weighting.",MessageType.Info);

            if (EditorGUI.EndChangeCheck())
            {
                Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }
    }
}
