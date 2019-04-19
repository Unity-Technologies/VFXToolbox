using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class FixBordersProcessor : GPUFrameProcessor<FixBordersProcessorSettings>
    {
        public FixBordersProcessor(FrameProcessorStack stack, ProcessorInfo info)
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/FixBorders.shader", stack,info)
        { }

        public override string GetName()
        {
            return "Fix Borders";
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            if (Event.current.type != EventType.Repaint)
                return false;

            Vector2 center = canvas.CanvasToScreen(Vector2.zero);

            int width = canvas.currentFrame.texture.width;
            int height = canvas.currentFrame.texture.height;

            // (left, right, top, bottom)
            float left = settings.FixFactors.x * width;
            float right = settings.FixFactors.y * width;
            float top = settings.FixFactors.z * height;
            float bottom = settings.FixFactors.w * height;

            Vector2 topRight = canvas.CanvasToScreen(new Vector2(-width/2, height/2 ));
            Vector2 bottomLeft = canvas.CanvasToScreen(new Vector2(width/2 , -height/2));
            Vector2 topRightCrop = canvas.CanvasToScreen(new Vector2(-width/2 + right, height/2 - top));
            Vector2 bottomLeftCrop = canvas.CanvasToScreen(new Vector2(width/2 - left, -height/2 + bottom));

            // Arrows
            Handles.color = canvas.styles.green;
            Handles.DrawLine(new Vector3(center.x, topRight.y), new Vector3(center.x, topRightCrop.y));
            Handles.DrawLine(new Vector3(center.x, bottomLeft.y), new Vector3(center.x, bottomLeftCrop.y));
            Handles.DrawLine(new Vector3(topRight.x, center.y), new Vector3(topRightCrop.x, center.y));
            Handles.DrawLine(new Vector3(bottomLeft.x, center.y), new Vector3(bottomLeftCrop.x, center.y));

            // Limits
            Handles.color = canvas.styles.fadewhite;
            Handles.DrawLine(new Vector3(topRight.x, topRightCrop.y), new Vector3(bottomLeft.x, topRightCrop.y));
            Handles.DrawLine(new Vector3(topRight.x, bottomLeftCrop.y), new Vector3(bottomLeft.x, bottomLeftCrop.y));
            Handles.DrawLine(new Vector3(topRightCrop.x, topRight.y), new Vector3(topRightCrop.x, bottomLeft.y));
            Handles.DrawLine(new Vector3(bottomLeftCrop.x, topRight.y), new Vector3(bottomLeftCrop.x, bottomLeft.y));

            // Texts
            int labelwidth = 36;
            GUI.color = canvas.styles.green;
            GUI.Label(new Rect(center.x - labelwidth/2, topRight.y - 20 , labelwidth, 16), settings.FixFactors.z.ToString(), canvas.styles.miniLabel);
            GUI.Label(new Rect(center.x - labelwidth/2, bottomLeft.y + 4, labelwidth, 16), settings.FixFactors.w.ToString(), canvas.styles.miniLabel);
            GUI.Label(new Rect(topRight.x + 4, center.y-8, labelwidth, 16), settings.FixFactors.y.ToString(), canvas.styles.miniLabel);
            GUI.Label(new Rect(bottomLeft.x - labelwidth - 4, center.y-8, labelwidth, 16), settings.FixFactors.x.ToString(), canvas.styles.miniLabelRight);

            Handles.color = Color.white;
            GUI.color = Color.white;

            return false;
        }

        public override bool Process(int frame)
        {
            Texture inputFrame = InputSequence.RequestFrame(frame).texture;
            m_Material.SetTexture("_MainTex", inputFrame);
            m_Material.SetVector("_FixFactors", settings.FixFactors);
            m_Material.SetColor("_FadeToColor", settings.FadeToColor);
            m_Material.SetFloat("_FadeToAlpha", settings.FadeToAlpha);
            m_Material.SetFloat("_Exponent", settings.Exponent);
            ExecuteShaderAndDump(frame, inputFrame);
            return true;
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            var fixFactors = m_SerializedObject.FindProperty("FixFactors");
            var fadeToColor = m_SerializedObject.FindProperty("FadeToColor");
            var fadeToAlpha = m_SerializedObject.FindProperty("FadeToAlpha");
            var exponent = m_SerializedObject.FindProperty("Exponent");
            Vector4 value = fixFactors.vector4Value;

            EditorGUI.BeginChangeCheck();

            float left =    EditorGUILayout.Slider(VFXToolboxGUIUtility.Get("Left"),       value.x,      0.0f,   1.0f);
            float right =   EditorGUILayout.Slider(VFXToolboxGUIUtility.Get("Right"),      value.y,      0.0f,   1.0f);
            float top =     EditorGUILayout.Slider(VFXToolboxGUIUtility.Get("Top"),        value.z,      0.0f,   1.0f);
            float bottom =  EditorGUILayout.Slider(VFXToolboxGUIUtility.Get("Bottom"),     value.w,      0.0f,   1.0f);

            if(
                    left !=     value.x 
                ||  right !=    value.y
                ||  top !=      value.z
                ||  bottom !=   value.w
                )
            {
                fixFactors.vector4Value = new Vector4(left, right, top, bottom);
            }


            Color c = EditorGUILayout.ColorField(new GUIContent("Fade to Color"), fadeToColor.colorValue, true, true, true);
            if(c != fadeToColor.colorValue)
            {
                fadeToColor.colorValue = c;
            }

            float a = EditorGUILayout.Slider("Fade to Alpha", fadeToAlpha.floatValue, 0.0f, 1.0f);
            if(a != fadeToAlpha.floatValue)
            {
                fadeToAlpha.floatValue = a;
            }

            EditorGUILayout.PropertyField(exponent, VFXToolboxGUIUtility.Get("Exponent"));

            if(EditorGUI.EndChangeCheck())
            {
                Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }

    }
}
