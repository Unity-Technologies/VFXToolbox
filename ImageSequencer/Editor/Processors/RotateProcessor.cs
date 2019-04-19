using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class RotateProcessor : GPUFrameProcessor<RotateProcessorSettings>
    {

        public RotateProcessor(FrameProcessorStack processorStack, ProcessorInfo info)
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Rotate.shader", processorStack, info)
        { }

        public override bool Process(int frame)
        {
            UpdateOutputSize();
            Texture texture = InputSequence.RequestFrame(frame).texture;
            m_Material.SetTexture("_MainTex", texture);
            m_Material.SetInt("_Mode", (int)settings.FrameRotateMode);
            ExecuteShaderAndDump(frame, texture);
            return true;
        }

        public override string GetLabel()
        {
            return string.Format("{0} ({1})", GetName(), settings.FrameRotateMode);
        }

        public override string GetName()
        {
            return "Rotate";
        }

        protected override void UpdateOutputSize()
        {
            if(settings.FrameRotateMode == RotateProcessorSettings.RotateMode.None || settings.FrameRotateMode == RotateProcessorSettings.RotateMode.Rotate180)
            {
                SetOutputSize(InputSequence.width, InputSequence.height);
            }
            else
            {
                SetOutputSize(InputSequence.height, InputSequence.width);
            }
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            var rotatemode = m_SerializedObject.FindProperty("FrameRotateMode");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(rotatemode, VFXToolboxGUIUtility.Get("Rotation Mode"));

            if(EditorGUI.EndChangeCheck())
            {
                UpdateOutputSize();
                Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            Vector2 pos = canvas.CanvasToScreen(Vector2.zero + (new Vector2(canvas.currentFrame.texture.width, canvas.currentFrame.texture.height) /2));
            Rect r = new Rect(pos.x, pos.y-16, 150, 16);
            GUI.Label(r, VFXToolboxGUIUtility.Get("CropRotateProcesssor"));
            return false;
        }

        protected override int GetOutputWidth()
        {
            UpdateOutputSize();
            return base.GetOutputWidth();
        }

        protected override int GetOutputHeight()
        {
            UpdateOutputSize();
            return base.GetOutputHeight();
        }

    }
}
