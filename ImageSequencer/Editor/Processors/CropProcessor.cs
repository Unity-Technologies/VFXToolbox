using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class CropProcessor : GPUFrameProcessor<CropProcessorSettings>
    {
        public CropProcessor(FrameProcessorStack stack, ProcessorInfo info) 
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Crop.shader", stack, info)
        { }

        public override string GetName()
        {
            return "Crop";
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            if (Event.current.type != EventType.Repaint)
                return false;

            Vector2 center = canvas.CanvasToScreen(Vector2.zero);

            Vector2 topRight;
            Vector2 bottomLeft;

            topRight = canvas.CanvasToScreen(new Vector2(-canvas.currentFrame.texture.width/2 - settings.Crop_Right, canvas.currentFrame.texture.height/2 + settings.Crop_Top));
            bottomLeft = canvas.CanvasToScreen(new Vector2(canvas.currentFrame.texture.width/2 + settings.Crop_Left, -canvas.currentFrame.texture.height/2 - settings.Crop_Bottom));

            Handles.DrawSolidRectangleWithOutline(new Rect(topRight, bottomLeft - topRight), Color.clear, canvas.styles.green);

            Vector2 topRightCrop;
            Vector2 bottomLeftCrop;

            topRightCrop = canvas.CanvasToScreen(new Vector2(-canvas.currentFrame.texture.width/2, canvas.currentFrame.texture.height/2));
            bottomLeftCrop = canvas.CanvasToScreen(new Vector2(canvas.currentFrame.texture.width/2, -canvas.currentFrame.texture.height/2));

            Handles.DrawSolidRectangleWithOutline(new Rect(topRightCrop, bottomLeftCrop - topRightCrop), Color.clear, canvas.styles.red);

            // Arrows
            Handles.color = canvas.styles.white;
            Handles.DrawLine(new Vector3(center.x, topRight.y), new Vector3(center.x, topRightCrop.y));
            Handles.DrawLine(new Vector3(center.x, bottomLeft.y), new Vector3(center.x, bottomLeftCrop.y));
            Handles.DrawLine(new Vector3(topRight.x, center.y), new Vector3(topRightCrop.x, center.y));
            Handles.DrawLine(new Vector3(bottomLeft.x, center.y), new Vector3(bottomLeftCrop.x, center.y));
            Handles.color = Color.white;

            // Texts
            GUI.Label(new Rect(center.x, topRightCrop.y - 16 , 64, 16), settings.Crop_Top.ToString(), canvas.styles.miniLabel);
            GUI.Label(new Rect(center.x, bottomLeftCrop.y, 64, 16), settings.Crop_Bottom.ToString(), canvas.styles.miniLabel);
            GUI.Label(new Rect(topRightCrop.x, center.y, 64, 16), settings.Crop_Right.ToString(), canvas.styles.miniLabel);
            GUI.Label(new Rect(bottomLeftCrop.x - 64, center.y, 64, 16), settings.Crop_Left.ToString(), canvas.styles.miniLabelRight);

            return false;
        }
        
        protected override void UpdateOutputSize()
        {
            int width = (InputSequence.width - (int)settings.Crop_Left) - (int)settings.Crop_Right;
            int height = (InputSequence.height - (int)settings.Crop_Top) - (int)settings.Crop_Bottom;
            SetOutputSize(width, height);
        }

        public override bool Process(int frame)
        {
            UpdateOutputSize();
            Texture texture = InputSequence.RequestFrame(frame).texture;
            m_Material.SetTexture("_MainTex", texture);
            m_Material.SetVector("_CropFactors", new Vector4(
                (float)settings.Crop_Left / texture.width,
                (float)settings.Crop_Right / texture.width,
                (float)settings.Crop_Top / texture.height,
                (float)settings.Crop_Bottom / texture.height
                ));

            ExecuteShaderAndDump(frame, texture);
            return true;
        }

        private void FindProperValues(float threshold, ref SerializedProperty top, ref SerializedProperty bottom, ref SerializedProperty left, ref SerializedProperty right)
        {
            int width = InputSequence.width;
            int height = InputSequence.height;

            int minX = width;
            int maxX = 0;
            int minY = height;
            int maxY = 0;

            Color[] colors;
            RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                    
            for (int i = 0; i < InputSequence.frames.Count; i++)
            {
                ProcessingFrame f = InputSequence.frames[i];

                VFXToolboxGUIUtility.DisplayProgressBar("Crop processor","Evaluating closest bound (Frame #"+i+" on "+ InputSequence.frames.Count+"...)", (float)i/InputSequence.frames.Count);
                if(InputSequence.processor != null)
                {
                    f.Process();
                    colors = VFXToolboxUtility.ReadBack(f.texture as RenderTexture);
                }
                else
                {
                    Graphics.Blit(f.texture, tempRT);
                    colors = VFXToolboxUtility.ReadBack( tempRT );
                }

                // Check frame
                for(int j = 0; j < colors.Length; j++)
                {
                    int x = j % width;
                    int y = j / width;
                    if (colors[j].a >= threshold)
                    {
                        minX = Mathf.Min(minX, x);
                        maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
            }
            VFXToolboxGUIUtility.ClearProgressBar();

            bottom.intValue = minY;
            top.intValue = height - maxY - 1;
            left.intValue = minX;
            right.intValue = width - maxX - 1;

            RenderTexture.ReleaseTemporary(tempRT);
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            int sourceWidth = InputSequence.width;
            int sourceHeight = InputSequence.height;

            var crop_top = m_SerializedObject.FindProperty("Crop_Top");
            var crop_bottom = m_SerializedObject.FindProperty("Crop_Bottom");
            var crop_left = m_SerializedObject.FindProperty("Crop_Left");
            var crop_right = m_SerializedObject.FindProperty("Crop_Right");
            var threshold = m_SerializedObject.FindProperty("AutoCropThreshold");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.IntSlider(crop_top, 0, (sourceHeight / 2) - 1, VFXToolboxGUIUtility.Get("Top"));
            EditorGUILayout.IntSlider(crop_bottom, 0, (sourceHeight / 2) - 1, VFXToolboxGUIUtility.Get("Bottom"));
            EditorGUILayout.IntSlider(crop_left, 0, (sourceWidth / 2)  - 1, VFXToolboxGUIUtility.Get("Left"));
            EditorGUILayout.IntSlider(crop_right, 0, (sourceWidth / 2) - 1, VFXToolboxGUIUtility.Get("Right"));

            GUILayout.Space(20);
            GUILayout.Label(VFXToolboxGUIUtility.Get("Automatic Crop Values"),EditorStyles.boldLabel);
            EditorGUI.indentLevel += 2;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Slider(threshold, 0.0f, 1.0f, VFXToolboxGUIUtility.Get("Alpha Threshold"));
                if(GUILayout.Button(VFXToolboxGUIUtility.Get("Find")))
                {
                    FindProperValues(threshold.floatValue, ref crop_top, ref crop_bottom, ref crop_left, ref crop_right);
                    GUI.changed = true;
                }
            }
            EditorGUI.indentLevel -= 2;

            GUILayout.Space(20);

            Rect preview_rect;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                preview_rect = GUILayoutUtility.GetRect(200, 200);
                GUILayout.FlexibleSpace();
            }

            EditorGUI.DrawRect(preview_rect, new Color(0, 0, 0, 0.1f));

            GUI.BeginClip(preview_rect);

            GUI.Label(new Rect(0, 0, 200, 16), "Preview");

            int top = 40;
            int left = 40;
            int right = 160;
            int bottom = 160;

            Handles.color = Color.green;
            Handles.DrawLine(new Vector3(left,top), new Vector3(left,bottom));
            Handles.DrawLine(new Vector3(left,bottom), new Vector3(right,bottom));
            Handles.DrawLine(new Vector3(right,bottom), new Vector3(right,top));
            Handles.DrawLine(new Vector3(right,top), new Vector3(left,top));

            top = (int)(40 + 120*(float)crop_top.intValue / sourceHeight);
            bottom = (int)(160 - 120*(float)crop_bottom.intValue / sourceHeight);
            left = (int)(40 + 120*(float)crop_left.intValue / sourceWidth);
            right = (int)(160 - 120*(float)crop_right.intValue / sourceWidth);

            Handles.color = Color.red;

            Handles.DrawLine(new Vector3(left,top), new Vector3(left,bottom));
            Handles.DrawLine(new Vector3(left,bottom), new Vector3(right,bottom));
            Handles.DrawLine(new Vector3(right,bottom), new Vector3(right,top));
            Handles.DrawLine(new Vector3(right,top), new Vector3(left,top));

            GUI.EndClip();

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
