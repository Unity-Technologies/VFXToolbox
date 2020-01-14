using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("Common","Resize")]
    internal class ResizeProcessor : ProcessorBase
    {
        public ushort Width;
        public ushort Height;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/Resize.shader";

        public override string processorName => "Resize";

        public override string label => $"{processorName} ({Width}x{Height})";

        public override void Default()
        {
            Width = 256;
            Height = 256;
        }

        public override void UpdateOutputSize()
        {
            SetOutputSize(Width, Height);
        }

        public override bool Process(int frame)
        {
            Texture texture = RequestInputTexture(frame);
            Vector4 kernelAndSize = new Vector4((float)texture.width / (float)Width, (float)texture.height / (float)Height, (float)Width, (float)Height);
            material.SetTexture("_MainTex", texture);
            material.SetVector("_KernelAndSize", kernelAndSize);
            ProcessFrame(frame, texture);
            return true;
        }

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            var width = serializedObject.FindProperty("Width");
            var height = serializedObject.FindProperty("Height");

            EditorGUI.BeginChangeCheck();

            using (new GUILayout.HorizontalScope())
            {
                int w = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Width"), width.intValue), 1, 8192);

                if (GUILayout.Button("", EditorStyles.popup, GUILayout.Width(16)))
                {
                    GenericMenu menu = new GenericMenu();
                    for (int s = 8192; s >= 16; s /= 2)
                    {
                        menu.AddItem(VFXToolboxGUIUtility.Get(s.ToString()), false, 
                        (o) => {
                            serializedObject.Update();
                            var out_width = serializedObject.FindProperty("Width");
                            out_width.intValue = (int)o;
                            serializedObject.ApplyModifiedProperties();
                            Invalidate();
                            UpdateOutputSize();
                        }, s);
                    }
                    menu.ShowAsContext();
                }

                if (w != width.intValue)
                {
                    width.intValue = w;
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                int h = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Height"), height.intValue), 1, 8192);

                if (GUILayout.Button("", EditorStyles.popup, GUILayout.Width(16)))
                {
                    GenericMenu menu = new GenericMenu();
                    for (int s = 8192; s >= 16; s /= 2)
                    {
                        menu.AddItem(VFXToolboxGUIUtility.Get(s.ToString()), false, (o) => {
                            serializedObject.Update();
                            var out_height = serializedObject.FindProperty("Height");
                            out_height.intValue = (int)o;
                            serializedObject.ApplyModifiedProperties();
                            Invalidate();
                            UpdateOutputSize();
                        }, s);
                    }
                    menu.ShowAsContext();
                }
                if (h != height.intValue)
                {
                    height.intValue = h;
                }
            }

            if (Mathf.Log(height.intValue, 2) % 1.0f != 0 || Mathf.Log(width.intValue, 2) % 1.0f != 0)
            {
                EditorGUILayout.HelpBox("Warning: your resize resolution is not a power of two.", MessageType.Warning);
            }

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
            if (Event.current.type != EventType.Repaint)
                return false;

            Vector2 center = canvas.CanvasToScreen(Vector2.zero);

            Vector2 topRight;
            Vector2 bottomLeft;

            topRight = canvas.CanvasToScreen(new Vector2(-canvas.currentFrame.texture.width / 2, canvas.currentFrame.texture.height / 2));
            bottomLeft = canvas.CanvasToScreen(new Vector2(canvas.currentFrame.texture.width / 2, -canvas.currentFrame.texture.height / 2));

            // Arrows
            Handles.color = canvas.styles.green;
            Handles.DrawLine(new Vector3(topRight.x, topRight.y - 16), new Vector3(bottomLeft.x, topRight.y - 16));
            Handles.DrawLine(new Vector3(bottomLeft.x - 16, topRight.y), new Vector3(bottomLeft.x - 16, bottomLeft.y));
            Handles.color = Color.white;

            // Texts
            GUI.color = Color.green;
            GUI.Label(new Rect(center.x - 32, topRight.y - 32, 64, 16), Width.ToString(), canvas.styles.miniLabelCenter);
            VFXToolboxGUIUtility.GUIRotatedLabel(new Rect(bottomLeft.x - 48, center.y - 8, 64, 16), Height.ToString(), -90.0f, canvas.styles.miniLabelCenter);
            GUI.color = Color.white;
            return false;
        }



    }
}
