using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class ResizeProcessor : GPUFrameProcessor<ResizeProcessorSettings>
    {

        public ResizeProcessor(FrameProcessorStack stack, ProcessorInfo info)
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Resize.shader", stack,info)
        { }

        protected override void UpdateOutputSize()
        {
            SetOutputSize(settings.Width, settings.Height);
        }

        public override string GetLabel()
        {
            return string.Format("{0} ({1}x{2})",GetName(), settings.Width,settings.Height);
        }

        public override string GetName()
        {
            return "Resize";
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            if (Event.current.type != EventType.Repaint)
                return false;

            Vector2 center = canvas.CanvasToScreen(Vector2.zero);

            Vector2 topRight;
            Vector2 bottomLeft;

            topRight = canvas.CanvasToScreen(new Vector2(-canvas.currentFrame.texture.width/2 , canvas.currentFrame.texture.height/2 ));
            bottomLeft = canvas.CanvasToScreen(new Vector2(canvas.currentFrame.texture.width/2 , -canvas.currentFrame.texture.height/2 ));

            // Arrows
            Handles.color = canvas.styles.green;
            Handles.DrawLine(new Vector3(topRight.x, topRight.y - 16), new Vector3(bottomLeft.x, topRight.y - 16));
            Handles.DrawLine(new Vector3(bottomLeft.x - 16, topRight.y), new Vector3(bottomLeft.x - 16, bottomLeft.y));
            Handles.color = Color.white;

            // Texts
            GUI.color = Color.green;
            GUI.Label(new Rect(center.x - 32 , topRight.y - 32 , 64, 16), settings.Width.ToString(), canvas.styles.miniLabelCenter);
            VFXToolboxGUIUtility.GUIRotatedLabel(new Rect(bottomLeft.x - 48, center.y - 8, 64, 16), settings.Height.ToString(), -90.0f, canvas.styles.miniLabelCenter);
            GUI.color = Color.white;
            return false;
        }

        public override bool Process(int frame)
        {
            Texture texture = InputSequence.RequestFrame(frame).texture;
            Vector4 kernelAndSize = new Vector4((float)texture.width / (float)settings.Width, (float)texture.height / (float)settings.Height, (float)settings.Width, (float)settings.Height);
            m_Material.SetTexture("_MainTex", texture);
            m_Material.SetVector("_KernelAndSize", kernelAndSize);
            ExecuteShaderAndDump(frame, texture);
            return true;
        }

        private void MenuSetWidth(object o)
        {
            m_SerializedObject.Update();
            var width = m_SerializedObject.FindProperty("Width");
            width.intValue = (int)o;
            m_SerializedObject.ApplyModifiedProperties();
            Invalidate();
            UpdateOutputSize();
        }

        private void MenuSetHeight(object o)
        {
            m_SerializedObject.Update();
            var height = m_SerializedObject.FindProperty("Height");
            height.intValue = (int)o;
            m_SerializedObject.ApplyModifiedProperties();
            Invalidate();
            UpdateOutputSize();
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            var width = m_SerializedObject.FindProperty("Width");
            var height = m_SerializedObject.FindProperty("Height");

            EditorGUI.BeginChangeCheck();

            using (new GUILayout.HorizontalScope())
            {
                int w = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Width"), width.intValue), 1, 8192);

                if(GUILayout.Button("",EditorStyles.popup,GUILayout.Width(16)))
                {
                    GenericMenu menu = new GenericMenu();
                    for(int s = 8192; s >= 16; s /=2)
                    {
                        menu.AddItem(VFXToolboxGUIUtility.Get(s.ToString()), false, MenuSetWidth, s);
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

                if(GUILayout.Button("",EditorStyles.popup,GUILayout.Width(16)))
                {
                    GenericMenu menu = new GenericMenu();
                    for(int s = 8192; s >= 16; s /=2)
                    {
                        menu.AddItem(VFXToolboxGUIUtility.Get(s.ToString()), false, MenuSetHeight, s);
                    }
                    menu.ShowAsContext();
                }
                if (h != height.intValue)
                {
                    height.intValue = h;
                }
            }

            if(Mathf.Log(height.intValue,2)% 1.0f != 0 || Mathf.Log(width.intValue,2)% 1.0f != 0 )
            {
                EditorGUILayout.HelpBox("Warning: your resize resolution is not a power of two.", MessageType.Warning);
            }

            if(EditorGUI.EndChangeCheck())
            {
                UpdateOutputSize();
                Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }

    }
}
