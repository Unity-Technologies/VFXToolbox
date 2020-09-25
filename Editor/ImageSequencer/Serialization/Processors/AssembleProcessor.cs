using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("Texture Sheet", "Assemble Flipbook")]
    internal class AssembleProcessor : ProcessorBase
    {
        public enum AssembleMode
        {
            FullSpriteSheet = 0,
            VerticalSequence = 1
        }

        public int FlipbookNumU;
        public int FlipbookNumV;
        public AssembleMode Mode;

        bool hasChanged = false;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/AssembleBlit.shader";

        public override string processorName => "Assemble Flipbook";

        public override string label
        {
            get
            {
                string numU = (Mode == AssembleMode.VerticalSequence) ? "*" : FlipbookNumU.ToString();
                string numV = FlipbookNumV.ToString();
                return $"{processorName} ({numU}x{numV})";
            }
        }

        public override int numU
        {
            get
            {
                switch (Mode)
                {
                    default:
                    case AssembleMode.FullSpriteSheet:
                        return FlipbookNumU * inputSequenceNumU;
                    case AssembleMode.VerticalSequence:
                        return inputSequenceNumU;
                }
            }
        }

        public override int numV => FlipbookNumV * inputSequenceNumV;

        public override int sequenceLength
        {
            get
            {
                switch (Mode)
                {
                    default:
                    case AssembleMode.FullSpriteSheet:
                        return 1;

                    case AssembleMode.VerticalSequence:
                        return inputSequenceLength / FlipbookNumV;
                }
            }
        }

        public override void Default()
        {
            FlipbookNumU = 5;
            FlipbookNumV = 5;
            Mode = AssembleMode.FullSpriteSheet;
        }

        public override void UpdateOutputSize()
        {
            switch (Mode)
            {
                case AssembleMode.FullSpriteSheet:

                    SetOutputSize(inputSequenceWidth * FlipbookNumU, inputSequenceHeight * FlipbookNumV);
                    break;

                case AssembleMode.VerticalSequence:

                    SetOutputSize(inputSequenceWidth, inputSequenceHeight * FlipbookNumV);
                    break;
            }
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            if (Event.current.type != EventType.Repaint)
                return false;

            Vector2 topRight;
            Vector2 bottomLeft;

            topRight = canvas.CanvasToScreen(new Vector2(-canvas.currentFrame.texture.width / 2, canvas.currentFrame.texture.height / 2));
            bottomLeft = canvas.CanvasToScreen(new Vector2(canvas.currentFrame.texture.width / 2, -canvas.currentFrame.texture.height / 2));

            // Texts
            GUI.color = canvas.styles.green;
            for (int i = 0; i < FlipbookNumU; i++)
            {
                float cw = (topRight.x - bottomLeft.x) / FlipbookNumU;
                GUI.Label(new Rect(bottomLeft.x + i * cw, topRight.y - 16, cw, 16), (i + 1).ToString(), canvas.styles.miniLabelCenter);
            }

            for (int i = 0; i < FlipbookNumV; i++)
            {
                float ch = (bottomLeft.y - topRight.y) / FlipbookNumV;
                VFXToolboxGUIUtility.GUIRotatedLabel(new Rect(bottomLeft.x - 8, topRight.y + i * ch, 16, ch), (i + 1).ToString(), -90.0f, canvas.styles.miniLabelCenter);
            }

            GUI.color = Color.white;
            return false;
        }

        public override bool Process(int frame)
        {
            int length = inputSequenceLength;

            RenderTexture backup = RenderTexture.active;

            switch (Mode)
            {
                case AssembleMode.FullSpriteSheet:

                    // Blit Every Image inside output
                    for (int i = 0; i < (FlipbookNumU * FlipbookNumV); i++)
                    {
                        int u = i % FlipbookNumU;
                        int v = (FlipbookNumV - 1) - (int)Mathf.Floor((float)i / FlipbookNumU);

                        Vector2 size = new Vector2(1.0f / FlipbookNumU, 1.0f / FlipbookNumV);
                        int idx = Mathf.Clamp(i, 0, length - 1);

                        Texture currentTexture = RequestInputTexture(idx);

                        Vector4 ClipCoordinates = new Vector4(u * size.x, v * size.y, size.x, size.y);

                        material.SetTexture("_MainTex", currentTexture);
                        material.SetVector("_CC", ClipCoordinates);

                        Graphics.Blit(currentTexture, (RenderTexture)RequestOutputTexture(0), material);
                    }

                    RenderTexture.active = backup;

                    break;

                case AssembleMode.VerticalSequence:

                    // Blit Every N'th Image inside output
                    int cycleLength = inputSequenceLength / FlipbookNumV;

                    for (int i = 0; i < FlipbookNumV; i++)
                    {
                        int u = 0;
                        int v = FlipbookNumV - 1 - i;

                        Vector2 size = new Vector2(1.0f, 1.0f / FlipbookNumV);
                        int idx = Mathf.Clamp((i * cycleLength) + frame, 0, length - 1);

                        Texture currentTexture = RequestInputTexture(idx);

                        Vector4 ClipCoordinates = new Vector4(u * size.x, v * size.y, size.x, size.y);

                        material.SetTexture("_MainTex", currentTexture);
                        material.SetVector("_CC", ClipCoordinates);

                        Graphics.Blit(currentTexture, (RenderTexture)RequestOutputTexture(frame), material);
                    }

                    RenderTexture.active = backup;
                    break;
            }

            return true;
        }

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            var flipbookNumU = serializedObject.FindProperty("FlipbookNumU");
            var flipbookNumV = serializedObject.FindProperty("FlipbookNumV");
            var mode = serializedObject.FindProperty("Mode");

            var assembleMode = (AssembleMode)mode.intValue;

            EditorGUI.BeginChangeCheck();

            assembleMode = (AssembleMode)EditorGUILayout.EnumPopup(VFXToolboxGUIUtility.Get("Assemble Mode"), assembleMode);
            if (assembleMode != (AssembleMode)mode.intValue)
            {
                mode.intValue = (int)assembleMode;
                hasChanged = true;
            }

            switch (assembleMode)
            {
                case AssembleMode.FullSpriteSheet:

                    int newU = EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Columns (U) : "), flipbookNumU.intValue);
                    int newV = EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Rows (V) : "), flipbookNumV.intValue);

                    if (inputSequenceLength > 0)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label(VFXToolboxGUIUtility.Get("Find Best Ratios"), GUILayout.Width(EditorGUIUtility.labelWidth));
                            if (GUILayout.Button(VFXToolboxGUIUtility.Get("Get")))
                            {
                                float frameRatio = (float)inputSequenceWidth / (float)inputSequenceHeight;
                                int length = inputSequenceLength;
                                List<int> ratios = new List<int>();
                                SortedDictionary<int, float> coeffs = new SortedDictionary<int, float>();
                                float rad = Mathf.Sqrt(length);
                                for (int i = (int)rad; i >= 1; i--)
                                {
                                    if (((float)length / (float)i) % 1.0f == 0.0f)
                                    {
                                        float pageRatio = (float)i / (length / i);
                                        float fullRatio = frameRatio * pageRatio;
                                        float divergence = Mathf.Abs(Mathf.Log(fullRatio, 2.0f) % 1.0f);

                                        if (!ratios.Contains(i))
                                        {
                                            ratios.Add(i);
                                            coeffs.Add(i, divergence);
                                        }

                                        fullRatio = frameRatio / pageRatio;
                                        divergence = Mathf.Abs(Mathf.Log(fullRatio, 2.0f) % 1.0f);

                                        if (!ratios.Contains(length / i))
                                        {
                                            ratios.Add(length / i);
                                            coeffs.Add((length / i), divergence);
                                        }
                                    }
                                }

                                GenericMenu menu = new GenericMenu();

                                var sortedValues = coeffs.OrderBy(kvp => kvp.Value);

                                foreach (KeyValuePair<int, float> kvp in sortedValues)
                                {
                                    int value = kvp.Key;

                                    menu.AddItem(new GUIContent(value + " x " + (length / value) + ((kvp.Value == 0.0f) ? " (PERFECT)" : "")), false, 
                                        (o) => {
                                                var seq_length = inputSequenceLength;
                                                var seq_numU = (int)o;
                                                var seq_numV = seq_length / seq_numU;
                                                serializedObject.Update();
                                                var seq_flipbookNumU = serializedObject.FindProperty("FlipbookNumU");
                                                var seq_flipbookNumV = serializedObject.FindProperty("FlipbookNumV");

                                                seq_flipbookNumU.intValue = seq_numU;
                                                seq_flipbookNumV.intValue = seq_numV;

                                                serializedObject.ApplyModifiedProperties();
                                                UpdateOutputSize();
                                                Invalidate();
                                            } 
                                    , value);
                                }
                                if (menu.GetItemCount() > 0)
                                    menu.ShowAsContext();
                            }
                        }
                        EditorGUILayout.HelpBox("Find Best Ratios will try to find matching possibilities for the current sequence, ordered by ratio pertinence, meaning that first results will have less stretch when resized to power-of-two textures.", MessageType.Info);
                    }


                    if (newU != flipbookNumU.intValue)
                    {
                        newU = Mathf.Clamp(newU, 1, inputSequenceLength / newV);
                        flipbookNumU.intValue = newU;
                    }

                    if (newV != flipbookNumV.intValue)
                    {
                        newV = Mathf.Clamp(newV, 1, inputSequenceLength / newU);
                        flipbookNumV.intValue = newV;
                    }
                    break;

                case AssembleMode.VerticalSequence:

                    int numRows = EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Rows (V) : "), flipbookNumV.intValue);

                    if (numRows != flipbookNumV.intValue)
                    {
                        numRows = Mathf.Clamp(numRows, 1, inputSequenceLength);
                        flipbookNumV.intValue = numRows;
                    }

                    break;

            }

            if (EditorGUI.EndChangeCheck())
            {
                UpdateOutputSize();
                Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }

    }
}


