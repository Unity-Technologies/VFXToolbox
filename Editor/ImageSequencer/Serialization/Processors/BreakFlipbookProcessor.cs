using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("Texture Sheet", "Break Flipbook")]
    internal class BreakFlipbookProcessor : ProcessorBase
    {
        public int FlipbookNumU;
        public int FlipbookNumV;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/GetSubUV.shader";

        public override string processorName => "Break Flipbook";

        public override string label => $"{processorName} ({FlipbookNumU}x{FlipbookNumV}): {FlipbookNumU * FlipbookNumV} frame(s).";

        private bool m_BypassSecurityCheck = false;

        public override void Default()
        {
            FlipbookNumU = 5;
            FlipbookNumV = 5;
        }

        public override void UpdateOutputSize()
        {
            int width = (int)Mathf.Ceil((float)inputSequenceWidth / FlipbookNumU);
            int height = (int)Mathf.Ceil((float)inputSequenceHeight / FlipbookNumV);
            SetOutputSize(width, height);
        }

        public override int sequenceLength => Mathf.Min(FlipbookNumU, inputSequenceWidth) * Mathf.Min(FlipbookNumV, inputSequenceHeight);

        public override bool Process(int frame)
        {
            Texture texture = RequestInputTexture(0);
            material.SetTexture("_MainTex", texture);
            Vector4 rect = new Vector4();

            int u = Mathf.Min(FlipbookNumU, texture.width);
            int v = Mathf.Min(FlipbookNumV, texture.height);

            int x = frame % FlipbookNumU;
            int y = (int)Mathf.Floor((float)frame / u);
            rect.x = (float)x;
            rect.y = (float)(v - 1) - y;
            rect.z = 1.0f / u;
            rect.w = 1.0f / v;

            material.SetVector("_Rect", rect);
            ProcessFrame(frame, texture);
            return true;
        }

        public override bool OnInspectorGUI(bool hasChanged, SerializedObject serializedObject)
        {
            var flipbookNumU = serializedObject.FindProperty("FlipbookNumU");
            var flipbookNumV = serializedObject.FindProperty("FlipbookNumV");

            EditorGUI.BeginChangeCheck();

            int newU = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Columns (U) : "), flipbookNumU.intValue), 1, inputSequenceWidth);
            int newV = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Rows (V) : "), flipbookNumV.intValue), 1, inputSequenceHeight);

            if (newU != flipbookNumU.intValue || flipbookNumV.intValue != newV)
                GUI.changed = true;

            if (m_BypassSecurityCheck)
                EditorGUILayout.HelpBox("Warning: you are currently bypassing frame count limits. Proceed with caution when entering values, as it can take a long time to process and stall your editor.", MessageType.Warning);

            if (EditorGUI.EndChangeCheck())
            {
                Debug.Log("Updated");

                if (newU * newV <= 4096)
                {
                    flipbookNumU.intValue = newU;
                    flipbookNumV.intValue = newV;
                }
                else
                {
                    if (!m_BypassSecurityCheck && EditorUtility.DisplayDialog("VFX Toolbox", "CAUTION : You are going to generate a sequence of " + newU * newV + " frames. This could take a long time to process, stall your editor, and consume a large amount of memory. Are you SURE you want to Continue?", "I know what I am doing, proceed", "Cancel"))
                        m_BypassSecurityCheck = true;

                    if (m_BypassSecurityCheck)
                    {
                        flipbookNumU.intValue = newU;
                        flipbookNumV.intValue = newV;
                    }
                }

                Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }

    }
}

