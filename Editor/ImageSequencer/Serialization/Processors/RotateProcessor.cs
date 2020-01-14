using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("Common","Rotate")]
    internal class RotateProcessor : ProcessorBase
    {
        public enum RotateMode
        {
            None = 0,
            Rotate90 = 1,
            Rotate180 = 2,
            Rotate270 = 3
        }

        public RotateMode FrameRotateMode;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/Rotate.shader";

        public override string processorName => "Rotate";

        public override string label => $"{processorName} ({FrameRotateMode})";

        public override int numU => (FrameRotateMode == RotateMode.None || FrameRotateMode == RotateMode.Rotate180) ? base.numU : base.numV;

        public override int numV => (FrameRotateMode == RotateMode.None || FrameRotateMode == RotateMode.Rotate180) ? base.numV : base.numU;

        public override void UpdateOutputSize()
        {
            if (FrameRotateMode == RotateMode.None || FrameRotateMode == RotateMode.Rotate180)
                SetOutputSize(inputSequenceWidth, inputSequenceHeight);
            else
                SetOutputSize(inputSequenceHeight, inputSequenceWidth);
        }

        public override void Default()
        {
            FrameRotateMode = 0;
        }

        public override bool Process(int frame)
        {
            UpdateOutputSize();
            Texture texture = RequestInputTexture(frame);
            material.SetTexture("_MainTex", texture);
            material.SetInt("_Mode", (int)FrameRotateMode);
            ProcessFrame(frame, texture);
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
                Invalidate();
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
