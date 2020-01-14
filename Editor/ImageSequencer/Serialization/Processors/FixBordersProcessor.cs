using UnityEngine;
using UnityEngine.VFXToolbox;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("Common","Fix Borders")]
    class FixBordersProcessor : ProcessorBase
    {
        public Vector4 FixFactors;
        public Color FadeToColor;
        public float FadeToAlpha;
        [FloatSlider(0.5f,4.0f)]
        public float Exponent;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/FixBorders.shader";

        public override string processorName => "Fix Borders";

        public override void Default()
        {
            FixFactors = Vector4.zero;
            FadeToColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            FadeToAlpha = 0.0f;
            Exponent = 1.5f;
        }

        public override bool Process(int frame)
        {
            Texture inputFrame = RequestInputTexture(frame);
            material.SetTexture("_MainTex", inputFrame);
            material.SetVector("_FixFactors", FixFactors);
            material.SetColor("_FadeToColor", FadeToColor);
            material.SetFloat("_FadeToAlpha", FadeToAlpha);
            material.SetFloat("_Exponent", Exponent);
            ProcessFrame(frame, inputFrame);
            return true;
        }

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            var fixFactors = serializedObject.FindProperty("FixFactors");
            var fadeToColor = serializedObject.FindProperty("FadeToColor");
            var fadeToAlpha = serializedObject.FindProperty("FadeToAlpha");
            var exponent = serializedObject.FindProperty("Exponent");
            Vector4 value = fixFactors.vector4Value;

            EditorGUI.BeginChangeCheck();

            float left = EditorGUILayout.Slider(VFXToolboxGUIUtility.Get("Left"), value.x, 0.0f, 1.0f);
            float right = EditorGUILayout.Slider(VFXToolboxGUIUtility.Get("Right"), value.y, 0.0f, 1.0f);
            float top = EditorGUILayout.Slider(VFXToolboxGUIUtility.Get("Top"), value.z, 0.0f, 1.0f);
            float bottom = EditorGUILayout.Slider(VFXToolboxGUIUtility.Get("Bottom"), value.w, 0.0f, 1.0f);

            if (
                    left != value.x
                || right != value.y
                || top != value.z
                || bottom != value.w
                )
            {
                fixFactors.vector4Value = new Vector4(left, right, top, bottom);
            }


            Color c = EditorGUILayout.ColorField(new GUIContent("Fade to Color"), fadeToColor.colorValue, true, true, true);
            if (c != fadeToColor.colorValue)
            {
                fadeToColor.colorValue = c;
            }

            float a = EditorGUILayout.Slider("Fade to Alpha", fadeToAlpha.floatValue, 0.0f, 1.0f);
            if (a != fadeToAlpha.floatValue)
            {
                fadeToAlpha.floatValue = a;
            }

            EditorGUILayout.PropertyField(exponent, VFXToolboxGUIUtility.Get("Exponent"));

            if (EditorGUI.EndChangeCheck())
            {
                Invalidate();
                changed = true;
            }

            return changed;
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            if (Event.current.type != EventType.Repaint)
                return false;

            Vector2 center = canvas.CanvasToScreen(Vector2.zero);

            int width = canvas.currentFrame.texture.width;
            int height = canvas.currentFrame.texture.height;

            // (left, right, top, bottom)
            float left = FixFactors.x * width;
            float right = FixFactors.y * width;
            float top = FixFactors.z * height;
            float bottom = FixFactors.w * height;

            Vector2 topRight = canvas.CanvasToScreen(new Vector2(-width / 2, height / 2));
            Vector2 bottomLeft = canvas.CanvasToScreen(new Vector2(width / 2, -height / 2));
            Vector2 topRightCrop = canvas.CanvasToScreen(new Vector2(-width / 2 + right, height / 2 - top));
            Vector2 bottomLeftCrop = canvas.CanvasToScreen(new Vector2(width / 2 - left, -height / 2 + bottom));

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
            GUI.Label(new Rect(center.x - labelwidth / 2, topRight.y - 20, labelwidth, 16), FixFactors.z.ToString(), canvas.styles.miniLabel);
            GUI.Label(new Rect(center.x - labelwidth / 2, bottomLeft.y + 4, labelwidth, 16), FixFactors.w.ToString(), canvas.styles.miniLabel);
            GUI.Label(new Rect(topRight.x + 4, center.y - 8, labelwidth, 16), FixFactors.y.ToString(), canvas.styles.miniLabel);
            GUI.Label(new Rect(bottomLeft.x - labelwidth - 4, center.y - 8, labelwidth, 16), FixFactors.x.ToString(), canvas.styles.miniLabelRight);

            Handles.color = Color.white;
            GUI.color = Color.white;

            return false;
        }
    }
}
