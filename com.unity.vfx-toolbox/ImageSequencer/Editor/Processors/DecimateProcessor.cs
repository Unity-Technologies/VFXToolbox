using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class DecimateProcessor : GPUFrameProcessor<DecimateProcessorSettings>
    {

        public DecimateProcessor(FrameProcessorStack stack, ProcessorInfo info) 
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Null.shader", stack, info)
        {
            settings.DecimateBy = 2;
        }

        public override string GetLabel()
        {
            return string.Format("{0} (1 of {1})", GetName(), settings.DecimateBy);
        }

        public override string GetName()
        {
            return "Decimate";
        }

        public override int GetProcessorSequenceLength()
        {
            return Mathf.Max(1,(int)Mathf.Floor((float)InputSequence.length/settings.DecimateBy));
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            return false;
        }

        public override bool Process(int frame)
        {
            int targetFrame = frame*settings.DecimateBy;
            Texture texture = InputSequence.RequestFrame(targetFrame).texture;
            m_Material.SetTexture("_MainTex", texture);
            ExecuteShaderAndDump(frame, texture);
            return true;
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            var decimateBy = m_SerializedObject.FindProperty("DecimateBy");

            EditorGUI.BeginChangeCheck();

            int newDecimate = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Decimate by"), (int)settings.DecimateBy),2,InputSequence.length);

            if(newDecimate != decimateBy.intValue )
            {
                decimateBy.intValue = newDecimate;
            }

            if(EditorGUI.EndChangeCheck())
            {
                hasChanged = true;
            }

            return hasChanged;
        }
    }
}
