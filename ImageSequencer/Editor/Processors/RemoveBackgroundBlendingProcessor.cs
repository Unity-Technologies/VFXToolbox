using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class RemoveBackgroundBlendingProcessor : GPUFrameProcessor<RemoveBackgroundSettings>
    {

        public RemoveBackgroundBlendingProcessor(FrameProcessorStack processorStack, ProcessorInfo info)
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Unblend.shader", processorStack, info)
        { }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            return false; 
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            var bgColor = m_SerializedObject.FindProperty("BackgroundColor");

            EditorGUI.BeginChangeCheck();

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(bgColor, VFXToolboxGUIUtility.Get("Background Color"));
                if(GUILayout.Button(VFXToolboxGUIUtility.Get("Grab"),GUILayout.Width(40)))
                {
                    if(InputSequence.length > 0)
                    {
                        InputSequence.RequestFrame(0);

                        Color background;

                        if (InputSequence.frames[0].texture is RenderTexture)
                        {
                            background = VFXToolboxUtility.ReadBack((RenderTexture)InputSequence.frames[0].texture)[0];
                        }
                        else
                        {
                            Texture2D inputFrame = (Texture2D)InputSequence.frames[0].texture;
                            RenderTexture rt = RenderTexture.GetTemporary(inputFrame.width, inputFrame.height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                            Graphics.Blit(inputFrame,rt);
                            background = VFXToolboxUtility.ReadBack(rt)[0];
                            RenderTexture.ReleaseTemporary(rt);
                        }

                        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                            background = background.gamma;

                        bgColor.colorValue = background;
                    }
                }
            }

            if(EditorGUI.EndChangeCheck())
            {
                UpdateOutputSize();
                Invalidate();
                hasChanged = true;
            }
            GUILayout.Space(20);
            EditorGUILayout.HelpBox("Please select a color corresponding to the solid background of the flipbook to try to reconstruct the pixel's color. \n\nThis filter will only work if your flipbook was rendered on a solid color background. Try the Grab button to fetch the upper left pixel of the first frame, or use the color picker.", MessageType.Info);

            return hasChanged;

        }

        public override bool Process(int frame)
        {
            Texture tex = InputSequence.RequestFrame(frame).texture;
            SetOutputSize(tex.width, tex.height);
            m_Material.SetTexture("_MainTex", tex);
            m_Material.SetColor("_BackgroundColor", settings.BackgroundColor);
            ExecuteShaderAndDump(frame, tex);
            return true;
        }

        public override string GetName()
        {
            return "Remove Background";
        }
    }
}
