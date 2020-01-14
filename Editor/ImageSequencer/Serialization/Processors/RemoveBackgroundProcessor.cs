using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("Color","Remove Background")]
    class RemoveBackgroundProcessor : ProcessorBase
    {
        public Color BackgroundColor;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/Unblend.shader";

        public override string processorName => "Remove Background";

        public override void Default()
        {
            BackgroundColor = new Color(0.25f,0.25f,0.25f,0.0f);
        }

        public override bool Process(int frame)
        {
            Texture tex = RequestInputTexture(frame);
            SetOutputSize(tex.width, tex.height);
            material.SetTexture("_MainTex", tex);
            material.SetColor("_BackgroundColor", BackgroundColor);
            ProcessFrame(frame, tex);
            return true;
        }

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            var bgColor = serializedObject.FindProperty("BackgroundColor");

            EditorGUI.BeginChangeCheck();

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(bgColor, VFXToolboxGUIUtility.Get("Background Color"));
                if (GUILayout.Button(VFXToolboxGUIUtility.Get("Grab"), GUILayout.Width(40)))
                {
                    if (inputSequenceLength > 0)
                    {
                        var texture = RequestInputTexture(0);

                        Color background;

                        if (texture is RenderTexture)
                        {
                            background = VFXToolboxUtility.ReadBack((RenderTexture)texture)[0];
                        }
                        else
                        {
                            Texture2D inputFrame = (Texture2D)texture;
                            RenderTexture rt = RenderTexture.GetTemporary(inputFrame.width, inputFrame.height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                            Graphics.Blit(inputFrame, rt);
                            background = VFXToolboxUtility.ReadBack(rt)[0];
                            RenderTexture.ReleaseTemporary(rt);
                        }

                        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                            background = background.gamma;

                        bgColor.colorValue = background;
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                UpdateOutputSize();
                Invalidate();
                changed = true;
            }
            GUILayout.Space(20);
            EditorGUILayout.HelpBox("Please select a color corresponding to the solid background of the flipbook to try to reconstruct the pixel's color. \n\nThis filter will only work if your flipbook was rendered on a solid color background. Try the Grab button to fetch the upper left pixel of the first frame, or use the color picker.", MessageType.Info);

            return changed;

        }
    }
}
