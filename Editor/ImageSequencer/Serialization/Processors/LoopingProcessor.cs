using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("Sequence","Loop Sequence")]
    class LoopingProcessor : ProcessorBase
    {
        public AnimationCurve curve;
        public int syncFrame;
        public int outputSequenceLength;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/Blend.shader";

        public override string processorName => "Looping";

        public override string label => $"{processorName} ({outputSequenceLength} frame(s), Sync : {syncFrame + 1})";

        public override int sequenceLength
        {
            get
            {
                if (inputSequenceLength > 0)
                    return outputSequenceLength;
                else
                    return 0;
            }
        }

        public override void Default()
        {
            curve = new AnimationCurve();
            curve.AddKey(0.25f, 0.0f);
            curve.AddKey(0.75f, 1.0f);
            syncFrame = 25;
            outputSequenceLength = 25;
        }

        public override bool Process(int frame)
        {
            int inputlength = inputSequenceLength;
            int outputlength = sequenceLength;

            float t = (float)frame / outputlength;

            float blendFactor = Mathf.Clamp(curve.Evaluate(t), 0.0f, 1.0f);

            int Prev = Mathf.Clamp((int)Mathf.Ceil(syncFrame + frame), 0, inputlength - 1);
            int Next = Mathf.Clamp((int)Mathf.Floor(syncFrame - (outputlength - frame)), 0, inputlength - 1);

            Texture prevtex = RequestInputTexture(Prev);
            Texture nexttex = RequestInputTexture(Next); 

            material.SetTexture("_MainTex", prevtex);
            material.SetTexture("_AltTex", nexttex);
            material.SetFloat("_BlendFactor", blendFactor);

            ProcessFrame(frame, prevtex);
            return true;
        }

        CurveDrawer m_CurveDrawer;

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            var outputSequenceLength = serializedObject.FindProperty("outputSequenceLength");
            var syncFrame = serializedObject.FindProperty("syncFrame");

            EditorGUI.BeginChangeCheck();

            int sync = syncFrame.intValue;
            int newSync = EditorGUILayout.IntSlider(VFXToolboxGUIUtility.Get("Input Sync Frame|The frame from input sequence that will be used at start and end of the output sequence."), sync, 0 + outputSequenceLength.intValue, inputSequenceLength - outputSequenceLength.intValue);

            if (newSync != sync)
            {
                newSync = Mathf.Clamp(newSync, 0 + outputSequenceLength.intValue, inputSequenceLength - outputSequenceLength.intValue);
                syncFrame.intValue = newSync;
            }

            int length = outputSequenceLength.intValue;
            int newlength = EditorGUILayout.IntSlider(VFXToolboxGUIUtility.Get("Output Sequence Length|How many frames will be in the output sequence?"), length, 2, (inputSequenceLength / 2) + 1);

            if (newlength != length)
            {
                newlength = Mathf.Min(newlength, Mathf.Max(1, (inputSequenceLength / 2)));
                outputSequenceLength.intValue = newlength;
                syncFrame.intValue = Mathf.Clamp(syncFrame.intValue, 0 + outputSequenceLength.intValue, inputSequenceLength - outputSequenceLength.intValue);
            }

            float seqRatio = -1.0f;
            if (isCurrentlyPreviewed)
            {
                seqRatio = (previewSequenceLength > 1) ? (float)previewCurrentFrame / (previewSequenceLength - 1) : 0.0f;
            }

            // Draw Preview
            GUILayout.Label(VFXToolboxGUIUtility.Get("Mix Curve"));
            Rect preview_rect;
            using (new GUILayout.HorizontalScope())
            {
                preview_rect = GUILayoutUtility.GetRect(200, 80);
            }

            EditorGUI.DrawRect(preview_rect, new Color(0.0f, 0.0f, 0.0f, 0.25f));

            Rect gradient_rect = new RectOffset(40, 16, 0, 16).Remove(preview_rect);
            float width = gradient_rect.width;
            float height = gradient_rect.height;
            Color topTrackColor = new Color(1.0f, 0.8f, 0.25f);
            Color bottomTrackColor = new Color(0.25f, 0.8f, 1.0f);

            using (new GUI.ClipScope(preview_rect))
            {
                GUI.color = topTrackColor;
                Handles.color = topTrackColor;
                GUI.Label(new Rect(0, 0, 32, 16), "In:", VFXToolboxStyles.miniLabel);
                Handles.DrawLine(new Vector3(72, 8), new Vector3(width + 40 - 32, 8));
                GUI.color = bottomTrackColor;
                Handles.color = bottomTrackColor;
                GUI.Label(new Rect(0, height - 16, 32, 16), "In:", VFXToolboxStyles.miniLabel);
                Handles.DrawLine(new Vector3(72, height - 8), new Vector3(width + 40 - 32, height - 8));
                GUI.color = Color.white;
                Handles.color = Color.white;
                GUI.Label(new Rect(0, height, 32, 16), "Out:", VFXToolboxStyles.miniLabel);
                GUI.Label(new Rect(40, height, 32, 16), "1", VFXToolboxStyles.miniLabel);
                GUI.Label(new Rect(width + 40 - 32, height, 32, 16), length.ToString(), VFXToolboxStyles.miniLabelRight);
                Handles.DrawLine(new Vector3(72, height + 8), new Vector3(width + 40 - 32, height + 8));
            }

            AnimationCurve curve = serializedObject.FindProperty("curve").animationCurveValue;
            using (new GUI.ClipScope(gradient_rect))
            {
                int seqLen = this.outputSequenceLength;
                int syncF = syncFrame.intValue;

                float w = Mathf.Ceil((float)width / seqLen);

                for (int i = 0; i < seqLen; i++)
                {
                    float t = (float)i / seqLen;
                    Color blended = Color.Lerp(bottomTrackColor, topTrackColor, curve.Evaluate(t));
                    EditorGUI.DrawRect(new Rect(i * w, 18, w, height - 36), blended);
                }

                GUI.color = topTrackColor;
                GUI.Label(new Rect(0, 0, 32, 16), (syncF - seqLen + 1).ToString(), VFXToolboxStyles.miniLabel);
                GUI.Label(new Rect(width - 32, 0, 32, 16), (syncF).ToString(), VFXToolboxStyles.miniLabelRight);
                GUI.color = bottomTrackColor;
                GUI.Label(new Rect(0, height - 16, 32, 16), (syncF + 1).ToString(), VFXToolboxStyles.miniLabel);
                GUI.Label(new Rect(width - 32, height - 16, 32, 16), (syncF + seqLen).ToString(), VFXToolboxStyles.miniLabelRight);
                GUI.color = Color.white;

            }

            // If previewing current sequence : draw trackbar
            if (seqRatio >= 0.0f)
            {
                Handles.color = Color.white;
                Handles.DrawLine(new Vector3(gradient_rect.xMin + seqRatio * gradient_rect.width, preview_rect.yMin), new Vector3(gradient_rect.xMin + seqRatio * gradient_rect.width, preview_rect.yMax));
            }

            // Curve Drawer
            if (m_CurveDrawer == null)
            {
                m_CurveDrawer = new CurveDrawer(null, 0.0f, 1.0f, 0.0f, 1.0f, 140, false);
                m_CurveDrawer.AddCurve(serializedObject.FindProperty("curve"), new Color(0.5f, 0.75f, 1.0f), "Looping Curve");
                m_CurveDrawer.OnPostGUI = OnCurveFieldGUI;
            }

            if (m_CurveDrawer.OnGUILayout())
            {
                changed = true;
            }

            if (EditorGUI.EndChangeCheck())
            {
                changed = true;
            }

            if (curve.keys.Length < 2 || curve.keys[0].value > 0.0f || curve.keys[curve.keys.Length - 1].value < 1.0f)
                EditorGUILayout.HelpBox("Warning : Mix Curve must have first key's value equal 0 and last key's value equal 1 to achieve looping", MessageType.Warning);

            return changed;

        }

        bool CurveEquals(AnimationCurve target)
        {
            for (int i = 0; i < target.keys.Length; i++)
            {
                if (target[i].time != curve[i].time ||
                    target[i].value != curve[i].value ||
                    target[i].inTangent != curve[i].inTangent ||
                    target[i].outTangent != curve[i].outTangent)
                {
                    return false;
                }
            }
            return true;
        }

        void OnCurveFieldGUI(Rect renderArea, Rect curveArea)
        {
            float seqRatio = -1.0f;
            if (isCurrentlyPreviewed)
            {
                seqRatio = (previewSequenceLength > 1) ? (float)previewCurrentFrame / (previewSequenceLength - 1) : 0.0f;
            }

            // If previewing current sequence : draw trackbar
            if (seqRatio >= 0.0f)
            {
                Handles.color = Color.white;
                Handles.DrawLine(new Vector3(curveArea.xMin + seqRatio * curveArea.width, renderArea.yMin), new Vector3(curveArea.xMin + seqRatio * curveArea.width, renderArea.yMax));
            }
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            int inLength = inputSequenceLength;
            int outLength = sequenceLength;
            int syncFrame = this.syncFrame;

            int outCurIDX = canvas.currentFrameIndex;
            float outCurT = (float)outCurIDX / outLength;
            int inSeqAIDX = (syncFrame - outLength) + outCurIDX;
            int inSeqBIDX = syncFrame + outCurIDX;

            AnimationCurve mixCurve = curve;
            float mix = mixCurve.Evaluate(outCurT);

            Color topTrackColor = canvas.styles.yellow;
            Color bottomTrackColor = canvas.styles.cyan;

            Vector2 top = canvas.CanvasToScreen(new Vector2(-canvas.currentFrame.texture.width, canvas.currentFrame.texture.height) / 2);

            Rect rect = new Rect((int)top.x + 24, (int)top.y + 8, 260, 280);
            EditorGUI.DrawRect(new RectOffset(8, 8, 8, 8).Add(rect), canvas.styles.backgroundPanelColor);
            GUILayout.BeginArea(rect);
            GUI.color = topTrackColor;
            GUILayout.Label("Mix Chunk A (Input Range : " + (syncFrame - outLength + 1).ToString() + "-" + syncFrame.ToString() + ")", canvas.styles.label);
            using (new GUILayout.HorizontalScope())
            {
                Rect imgARect = GUILayoutUtility.GetRect(100, 100);
                imgARect.width = 100;
                imgARect.height = 100;

                GUI.color = Color.white;
#if !UNITY_2018_2_OR_NEWER
                GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
#endif
                GUI.DrawTexture(imgARect, RequestInputTexture(inSeqAIDX));
#if !UNITY_2018_2_OR_NEWER
                GL.sRGBWrite = false;
#endif
                GUI.color = canvas.styles.white;
                Handles.DrawSolidRectangleWithOutline(imgARect, Color.clear, topTrackColor);


                using (new GUILayout.VerticalScope())
                {
                    GUI.color = topTrackColor;
                    GUILayout.Label("Frame #" + (inSeqAIDX + 1).ToString(), canvas.styles.miniLabel);
                    GUILayout.Label("Mixed at : " + (int)(mix * 100) + "%", canvas.styles.miniLabel);
                }
            }

            GUILayout.Space(16);
            GUI.color = bottomTrackColor;
            GUILayout.Label("Mix Chunk B (Input Range : " + (syncFrame + 1).ToString() + "-" + (syncFrame + outLength).ToString() + ")", canvas.styles.label);
            using (new GUILayout.HorizontalScope())
            {
                Rect imgBRect = GUILayoutUtility.GetRect(100, 100);
                imgBRect.width = 100;
                imgBRect.height = 100;

                GUI.color = Color.white;
#if !UNITY_2018_2_OR_NEWER
                GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
#endif
                GUI.DrawTexture(imgBRect, RequestInputTexture(inSeqBIDX));
#if !UNITY_2018_2_OR_NEWER
                GL.sRGBWrite = false;
#endif

                GUI.color = canvas.styles.white;
                Handles.DrawSolidRectangleWithOutline(imgBRect, Color.clear, bottomTrackColor);

                using (new GUILayout.VerticalScope())
                {
                    GUI.color = bottomTrackColor;
                    GUILayout.Label("Frame #" + (inSeqBIDX + 1).ToString(), canvas.styles.miniLabel);
                    GUILayout.Label("Mixed at : " + (int)((1.0f - mix) * 100) + "%", canvas.styles.miniLabel);
                }
            }
            GUI.color = Color.white;
            GUILayout.EndArea();

            return false;
        }
    }
}
