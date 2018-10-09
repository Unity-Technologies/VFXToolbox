using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class FadeProcessor : GPUFrameProcessor<FadeProcessorSettings>
    {
        CurveDrawer m_CurveDrawer;

        public FadeProcessor(FrameProcessorStack processorStack, ProcessorInfo info)
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Fade.shader", processorStack, info)
        {
            if(m_CurveDrawer == null)
            {
                m_CurveDrawer = new CurveDrawer("Fade Curve", 0.0f, 1.0f, 0.0f, 1.0f, 140, false);
                m_CurveDrawer.AddCurve(m_SerializedObject.FindProperty("FadeCurve"), new Color(0.75f,0.5f,1.0f), "Fade Curve");
                m_CurveDrawer.OnPostGUI = OnCurveFieldGUI;
            }

            if (settings.FadeCurve.keys.Length < 2)
            {
                settings.FadeCurve.AddKey(new Keyframe(0.85f, 1f));
                settings.FadeCurve.AddKey(new Keyframe(1f, 0f));
            }

        }

        public bool SetCurve(AnimationCurve curve)
        {
            if(!CurveEquals(curve))
            {
                settings.FadeCurve = new AnimationCurve(curve.keys);
                m_ProcessorStack.Invalidate(this);
                return true;
            }
            else
            {
                return false;
            }
            
        }

        public bool CurveEquals(AnimationCurve target)
        {
            for (int i = 0; i < target.keys.Length; i++)
            {
                if (target[i].time != settings.FadeCurve[i].time ||
                    target[i].value != settings.FadeCurve[i].value ||
                    target[i].inTangent != settings.FadeCurve[i].inTangent ||
                    target[i].outTangent != settings.FadeCurve[i].outTangent)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Process(int frame)
        {
            Texture inputFrame = InputSequence.RequestFrame(frame).texture;
            m_Material.SetTexture("_MainTex", inputFrame);
            m_Material.SetColor("_FadeToColor", settings.FadeToColor);
            m_Material.SetFloat("_Ratio", settings.FadeCurve.Evaluate(((float)frame) / GetProcessorSequenceLength()));
            ExecuteShaderAndDump(frame, inputFrame);
            return true;
        }

        public override string GetName()
        {
            return "Fade";
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            var fadeToColor = m_SerializedObject.FindProperty("FadeToColor");

            EditorGUI.BeginChangeCheck();

            Color c = EditorGUILayout.ColorField(VFXToolboxGUIUtility.Get("Fade To Color"), fadeToColor.colorValue);

            if(c != fadeToColor.colorValue)
            {
                fadeToColor.colorValue = c;
            }

            if(m_CurveDrawer.OnGUILayout())
            {
                hasChanged = true;
            }

            if(EditorGUI.EndChangeCheck())
            {
                Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }

        void OnCurveFieldGUI(Rect renderArea, Rect curveArea)
        {
            float seqRatio = -1.0f;
            if(m_ProcessorStack.imageSequencer.previewCanvas.sequence.processor == this)
            {
                seqRatio = (m_ProcessorStack.imageSequencer.previewCanvas.numFrames > 1)? (float) m_ProcessorStack.imageSequencer.previewCanvas.currentFrameIndex /  (m_ProcessorStack.imageSequencer.previewCanvas.numFrames - 1) : 0.0f;
            }

            // If previewing current sequence : draw trackbar
            if(seqRatio >= 0.0f)
            {
                Handles.color = Color.white;
                Handles.DrawLine(new Vector3(curveArea.xMin + seqRatio * curveArea.width, renderArea.yMin), new Vector3(curveArea.xMin + seqRatio * curveArea.width, renderArea.yMax));
            }
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            // Empty, for now;
            return false;
        }
    }
}
