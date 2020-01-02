using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Texture Sheet", "Break Flipbook")]
    internal class BreakFlipbookProcessor : ProcessorBase
    {
        public int FlipbookNumU;
        public int FlipbookNumV;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/GetSubUV.shader";

        public override string processorName => "Break Flipbook";

        public override string label => $"{name} ({FlipbookNumU}x{FlipbookNumV}): {FlipbookNumU * FlipbookNumV} frame(s).";

        private bool m_BypassSecurityCheck = false;

        public override void Default()
        {
            FlipbookNumU = 5;
            FlipbookNumV = 5;
        }

        public override void UpdateOutputSize()
        {
            int width = (int)Mathf.Ceil((float)processor.InputSequence.RequestFrame(0).texture.width / FlipbookNumU);
            int height = (int)Mathf.Ceil((float)processor.InputSequence.RequestFrame(0).texture.height / FlipbookNumV);
            processor.SetOutputSize(width, height);
        }

        public override int sequenceLength => Mathf.Min(FlipbookNumU, processor.InputSequence.width) * Mathf.Min(FlipbookNumV, processor.InputSequence.height);

        public override bool Process(int frame)
        {
            Texture texture = processor.InputSequence.RequestFrame(0).texture;
            processor.material.SetTexture("_MainTex", texture);
            Vector4 rect = new Vector4();

            int u = Mathf.Min(FlipbookNumU, texture.width);
            int v = Mathf.Min(FlipbookNumV, texture.height);

            int x = frame % FlipbookNumU;
            int y = (int)Mathf.Floor((float)frame / u);
            rect.x = (float)x;
            rect.y = (float)(v - 1) - y;
            rect.z = 1.0f / u;
            rect.w = 1.0f / v;

            processor.material.SetVector("_Rect", rect);
            processor.ExecuteShaderAndDump(frame, texture);
            return true;
        }

        public override bool OnInspectorGUI(bool hasChanged, SerializedObject serializedObject)
        {
            var flipbookNumU = serializedObject.FindProperty("FlipbookNumU");
            var flipbookNumV = serializedObject.FindProperty("FlipbookNumV");

            EditorGUI.BeginChangeCheck();

            int newU = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Columns (U) : "), flipbookNumU.intValue), 1, processor.InputSequence.width);
            int newV = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Rows (V) : "), flipbookNumV.intValue), 1, processor.InputSequence.height);

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

                processor.Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }

    }
}

