using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Common","Rotate")]
    class RotateProcessor : ProcessorBase
    {
        public enum RotateMode
        {
            None = 0,
            Rotate90 = 1,
            Rotate180 = 2,
            Rotate270 = 3
        }

        public RotateMode FrameRotateMode;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Rotate.shader";

        public override string processorName => "Rotate";

        public override string label => $"{processorName} ({FrameRotateMode})";

        public override void UpdateOutputSize()
        {
            if (FrameRotateMode == RotateMode.None || FrameRotateMode == RotateMode.Rotate180)
                processor.SetOutputSize(processor.InputSequence.width, processor.InputSequence.height);
            else
                processor.SetOutputSize(processor.InputSequence.height, processor.InputSequence.width);
        }

        public override void Default()
        {
            FrameRotateMode = 0;
        }

        public override bool Process(int frame)
        {
            UpdateOutputSize();
            Texture texture = processor.InputSequence.RequestFrame(frame).texture;
            processor.material.SetTexture("_MainTex", texture);
            processor.material.SetInt("_Mode", (int)FrameRotateMode);
            processor.ExecuteShaderAndDump(frame, texture);
            return true;
        }

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            var rotatemode = serializedObject.FindProperty("FrameRotateMode");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(rotatemode, VFXToolboxGUIUtility.Get("Rotation Mode"));

            if (EditorGUI.EndChangeCheck())
            {
                UpdateOutputSize();
                processor.Invalidate();
                changed = true;
            }

            return changed;
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            Vector2 pos = canvas.CanvasToScreen(Vector2.zero + (new Vector2(canvas.currentFrame.texture.width, canvas.currentFrame.texture.height) / 2));
            Rect r = new Rect(pos.x, pos.y - 16, 150, 16);
            GUI.Label(r, VFXToolboxGUIUtility.Get($"Rotation : {ObjectNames.NicifyVariableName(FrameRotateMode.ToString())}"));
            return false;
        }
    }
}
