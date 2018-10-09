using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class AssembleProcessor : GPUFrameProcessor<AssembleProcessorSettings>
    {
        public AssembleProcessor(FrameProcessorStack processorStack, ProcessorInfo info)
            : base ("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/AssembleBlit.shader", processorStack, info)
        { }

        protected override void UpdateOutputSize()
        {
            switch(settings.Mode)
            {
                    case AssembleProcessorSettings.AssembleMode.FullSpriteSheet:

                        SetOutputSize( InputSequence.width * settings.FlipbookNumU, InputSequence.height * settings.FlipbookNumV);
                        break;

                    case AssembleProcessorSettings.AssembleMode.VerticalSequence:

                        SetOutputSize( InputSequence.width, InputSequence.height * settings.FlipbookNumV);
                        break;
            }

        }

        public override string GetLabel()
        {
            string numU = (settings.Mode == AssembleProcessorSettings.AssembleMode.VerticalSequence) ? "*" : settings.FlipbookNumU.ToString();
            string numV = settings.FlipbookNumV.ToString();
            return string.Format("{0} ({1}x{2})",GetName(), numU,numV);
        }

        public override string GetName()
        {
            return "Assemble Flipbook";
        }

        public override bool Process(int frame)
        {
            int length = InputSequence.length;

            RenderTexture backup = RenderTexture.active;
                    
            switch(settings.Mode)
            {
                case AssembleProcessorSettings.AssembleMode.FullSpriteSheet:

                    // Blit Every Image inside output
                    for(int i = 0; i < (settings.FlipbookNumU*settings.FlipbookNumV); i++)
                    {
                        int u = i % settings.FlipbookNumU;
                        int v = (settings.FlipbookNumV-1)-(int)Mathf.Floor((float)i / settings.FlipbookNumU);

                        Vector2 size = new Vector2(1.0f/settings.FlipbookNumU, 1.0f/settings.FlipbookNumV);
                        int idx = Mathf.Clamp(i, 0, length - 1);

                        Texture currentTexture = InputSequence.RequestFrame(idx).texture;

                        Vector4 ClipCoordinates = new Vector4(u*size.x,v*size.y,size.x,size.y);

                        m_Material.SetTexture("_MainTex", currentTexture);
                        m_Material.SetVector("_CC", ClipCoordinates);

                        Graphics.Blit(currentTexture, (RenderTexture)m_OutputSequence.frames[0].texture, m_Material);
                    }

                    RenderTexture.active = backup;

                    break;

                case AssembleProcessorSettings.AssembleMode.VerticalSequence:

                    // Blit Every N'th Image inside output
                    int cycleLength = InputSequence.length / settings.FlipbookNumV;

                    for(int i = 0; i < settings.FlipbookNumV; i++)
                    {
                        int u = 0;
                        int v = settings.FlipbookNumV-1-i;

                        Vector2 size = new Vector2(1.0f, 1.0f/settings.FlipbookNumV);
                        int idx = Mathf.Clamp((i * cycleLength)+frame, 0, length - 1);

                        Texture currentTexture = InputSequence.RequestFrame(idx).texture;

                        Vector4 ClipCoordinates = new Vector4(u*size.x,v*size.y,size.x,size.y);

                        m_Material.SetTexture("_MainTex", currentTexture);
                        m_Material.SetVector("_CC", ClipCoordinates);

                        Graphics.Blit(currentTexture, (RenderTexture)m_OutputSequence.frames[frame].texture, m_Material);
                    }

                    RenderTexture.active = backup;
                    break;
            }
            
            return true;
        }

        public override int GetProcessorSequenceLength()
        {
            switch(settings.Mode)
            {
                case AssembleProcessorSettings.AssembleMode.FullSpriteSheet:
                return 1;

                case AssembleProcessorSettings.AssembleMode.VerticalSequence:
                return InputSequence.length / settings.FlipbookNumV;

                default:
                    return 1; // Should not happen, unless someone messes up with files...
                    
            }
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {

            var flipbookNumU = m_SerializedObject.FindProperty("FlipbookNumU");
            var flipbookNumV = m_SerializedObject.FindProperty("FlipbookNumV");
            var mode = m_SerializedObject.FindProperty("Mode");

            var assembleMode = (AssembleProcessorSettings.AssembleMode)mode.intValue;

            EditorGUI.BeginChangeCheck();

            assembleMode = (AssembleProcessorSettings.AssembleMode)EditorGUILayout.EnumPopup(VFXToolboxGUIUtility.Get("Assemble Mode"), assembleMode);
            if(assembleMode != (AssembleProcessorSettings.AssembleMode)mode.intValue )
            {
                mode.intValue = (int)assembleMode;
                hasChanged = true;
            }

            switch(assembleMode)
            {
                case AssembleProcessorSettings.AssembleMode.FullSpriteSheet:

                    int newU = EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Columns (U) : "),flipbookNumU.intValue);
                    int newV = EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Rows (V) : "), flipbookNumV.intValue);

                    if(InputSequence.length > 0)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label(VFXToolboxGUIUtility.Get("Find Best Ratios"), GUILayout.Width(EditorGUIUtility.labelWidth));
                            if (GUILayout.Button(VFXToolboxGUIUtility.Get("Get")))
                            {
                                float frameRatio = (float)InputSequence.frames[0].texture.width / (float)InputSequence.frames[0].texture.height;
                                int length = InputSequence.frames.Count;
                                List<int> ratios = new List<int>();
                                SortedDictionary<int,float> coeffs = new SortedDictionary<int,float>();
                                float rad = Mathf.Sqrt(length);
                                for(int i = (int)rad; i >=1; i--)
                                {
                                    if(((float)length / (float)i) % 1.0f == 0.0f)
                                    {
                                        float pageRatio = (float)i / (length / i);
                                        float fullRatio = frameRatio * pageRatio;
                                        float divergence = Mathf.Abs(Mathf.Log(fullRatio, 2.0f) % 1.0f);

                                        if(!ratios.Contains(i))
                                        {
                                            ratios.Add(i);
                                            coeffs.Add(i, divergence);
                                        }

                                        fullRatio = frameRatio / pageRatio;
                                        divergence = Mathf.Abs(Mathf.Log(fullRatio, 2.0f) % 1.0f);

                                        if(!ratios.Contains(length / i))
                                        {
                                            ratios.Add(length / i);
                                            coeffs.Add((length / i), divergence);
                                        }
                                    }
                                }

                                GenericMenu menu = new GenericMenu();

                                var sortedValues = coeffs.OrderBy(kvp => kvp.Value);

                                foreach(KeyValuePair<int, float> kvp in sortedValues)
                                {
                                    int value = kvp.Key;
                            
                                    menu.AddItem(new GUIContent(value + " x " + (length / value) + ((kvp.Value == 0.0f)? " (PERFECT)": "")), false, MenuSetFlipbookUV, value);
                                }
                                if (menu.GetItemCount() > 0)
                                    menu.ShowAsContext();
                            }
                        }
                        EditorGUILayout.HelpBox("Find Best Ratios will try to find matching possibilities for the current sequence, ordered by ratio pertinence, meaning that first results will have less stretch when resized to power-of-two textures.", MessageType.Info);
                    }


                    if(newU != flipbookNumU.intValue) 
                    {
                        newU = Mathf.Clamp(newU, 1,  InputSequence.length / newV);
                        flipbookNumU.intValue = newU;
                    }

                    if(newV != flipbookNumV.intValue)
                    {
                        newV = Mathf.Clamp(newV, 1, InputSequence.length / newU);
                        flipbookNumV.intValue = newV;
                    }
                    break;

                case AssembleProcessorSettings.AssembleMode.VerticalSequence:

                    int numRows = EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Rows (V) : "), flipbookNumV.intValue);

                    if(numRows != flipbookNumV.intValue)
                    {
                        numRows = Mathf.Clamp(numRows, 1, InputSequence.length);
                        flipbookNumV.intValue = numRows;
                    }

                    break;

            }

            if(EditorGUI.EndChangeCheck())
            {
                UpdateOutputSize();
                Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }

        private void MenuSetFlipbookUV(object o)
        {
            int length = InputSequence.length;
            int numU = (int)o;
            int numV = length / numU;
            m_SerializedObject.Update();
            var flipbookNumU = m_SerializedObject.FindProperty("FlipbookNumU");
            var flipbookNumV = m_SerializedObject.FindProperty("FlipbookNumV");

            flipbookNumU.intValue = numU;
            flipbookNumV.intValue = numV;

            m_SerializedObject.ApplyModifiedProperties();
            UpdateOutputSize();
            Invalidate();
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            if (Event.current.type != EventType.Repaint)
                return false;

            Vector2 topRight;
            Vector2 bottomLeft;

            topRight = canvas.CanvasToScreen(new Vector2(-canvas.currentFrame.texture.width/2 , canvas.currentFrame.texture.height/2 ));
            bottomLeft = canvas.CanvasToScreen(new Vector2(canvas.currentFrame.texture.width/2 , -canvas.currentFrame.texture.height/2 ));

            // Texts
            GUI.color = canvas.styles.green;
            for(int i = 0; i < settings.FlipbookNumU; i++)
            {
                float cw = (topRight.x - bottomLeft.x) / settings.FlipbookNumU;
                GUI.Label(new Rect(bottomLeft.x + i * cw , topRight.y - 16 , cw, 16), (i+1).ToString(), canvas.styles.miniLabelCenter);
            }

            for(int i = 0; i < settings.FlipbookNumV; i++)
            {
                float ch = (bottomLeft.y-topRight.y) / settings.FlipbookNumV;
                VFXToolboxGUIUtility.GUIRotatedLabel(new Rect(bottomLeft.x - 8, topRight.y + i * ch, 16, ch), (i+1).ToString(), -90.0f, canvas.styles.miniLabelCenter);
            }

            GUI.color = Color.white;
            return false;
        }

        protected override int GetNumU()
        {
            switch(settings.Mode)
            {
                case AssembleProcessorSettings.AssembleMode.FullSpriteSheet:
                    return settings.FlipbookNumU * InputSequence.numU;
                case AssembleProcessorSettings.AssembleMode.VerticalSequence:
                    return InputSequence.numU;
                default:
                    return InputSequence.numU;
            }

        }

        protected override int GetNumV()
        {
            return settings.FlipbookNumV * InputSequence.numV;
        }
    }
}
