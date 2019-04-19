using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class BreakFlipbookProcessor : GPUFrameProcessor<BreakFilpbookProcessorSettings>
    {
        private bool m_BypassSecurityCheck = false;

        public BreakFlipbookProcessor(FrameProcessorStack stack, ProcessorInfo info)
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/GetSubUV.shader", stack, info)
        { }

        protected override void UpdateOutputSize()
        {
            int width = (int) Mathf.Ceil((float)InputSequence.RequestFrame(0).texture.width / settings.FlipbookNumU);
            int height = (int) Mathf.Ceil((float)InputSequence.RequestFrame(0).texture.height / settings.FlipbookNumV);
            SetOutputSize(width, height);
        }

        public override string GetLabel()
        {
            return string.Format("{0} ({1}x{2}): {3} frame(s).",GetName(), settings.FlipbookNumU,settings.FlipbookNumV, settings.FlipbookNumU * settings.FlipbookNumV);
        }

        public override string GetName()
        {
            return "Break Flipbook";
        }

        public override int GetProcessorSequenceLength()
        {
            return Mathf.Min(settings.FlipbookNumU,InputSequence.width) * Mathf.Min(settings.FlipbookNumV,InputSequence.height);
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            return false;
        }

        public override bool Process(int frame)
        {
            Texture texture = InputSequence.RequestFrame(0).texture;
            m_Material.SetTexture("_MainTex", texture);
            Vector4 rect = new Vector4();

            int u = Mathf.Min(settings.FlipbookNumU,texture.width);
            int v = Mathf.Min(settings.FlipbookNumV,texture.height);

            int x = frame % settings.FlipbookNumU;
            int y = (int)Mathf.Floor((float)frame / u);
            rect.x = (float)x;
            rect.y = (float)(v-1) - y;
            rect.z = 1.0f / u;
            rect.w = 1.0f / v;

            m_Material.SetVector("_Rect", rect);
            ExecuteShaderAndDump(frame, texture);
            return true;
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            var flipbookNumU = m_SerializedObject.FindProperty("FlipbookNumU");
            var flipbookNumV = m_SerializedObject.FindProperty("FlipbookNumV");

            EditorGUI.BeginChangeCheck();

            int newU = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Columns (U) : "),flipbookNumU.intValue),1,InputSequence.width);
            int newV = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Rows (V) : "), flipbookNumV.intValue),1,InputSequence.height);

            if (newU != flipbookNumU.intValue || flipbookNumV.intValue != newV)
                GUI.changed = true;

            if (m_BypassSecurityCheck)
                EditorGUILayout.HelpBox("Warning: you are currently bypassing frame count limits. Proceed with caution when entering values, as it can take a long time to process and stall your editor.", MessageType.Warning);

            if(EditorGUI.EndChangeCheck())
            {
                Debug.Log("Updated");

                if(newU * newV <= 4096)
                {
                    flipbookNumU.intValue = newU;
                    flipbookNumV.intValue = newV;
                }
                else
                {
                    if (!m_BypassSecurityCheck && EditorUtility.DisplayDialog("VFX Toolbox", "CAUTION : You are going to generate a sequence of "+newU * newV+" frames. This could take a long time to process, stall your editor, and consume a large amount of memory. Are you SURE you want to Continue?", "I know what I am doing, proceed", "Cancel"))
                        m_BypassSecurityCheck = true;

                    if(m_BypassSecurityCheck)
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

        protected override int GetNumU()
        {
            return 1;
        }

        protected override int GetNumV()
        {
            return 1;
        }
    }
}
