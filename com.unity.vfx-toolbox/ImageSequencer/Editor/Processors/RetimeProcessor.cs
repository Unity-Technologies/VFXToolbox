using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class RetimeProcessor : GPUFrameProcessor<RetimeProcessorSettings>
    {
        CurveDrawer m_CurveDrawer;

        public RetimeProcessor(FrameProcessorStack processorStack, ProcessorInfo info) 
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Blend.shader", processorStack, info)
        {
            if(m_CurveDrawer == null)
            {
                m_CurveDrawer = new CurveDrawer("Retime Curve", 0.0f, 1.0f, 0.0f, InputSequence.length, 140, false);
                m_CurveDrawer.AddCurve(m_SerializedObject.FindProperty("curve"), new Color(0.5f, 0.75f, 1.0f), "Retime Curve");
                m_CurveDrawer.OnPostGUI = OnCurveFieldGUI;
            }

            if (settings.curve.keys.Length < 2)
            {
                settings.curve.AddKey(new Keyframe(0, 0));
                settings.curve.AddKey(new Keyframe(1, 24));
            }
        }

        public bool CurveEquals(AnimationCurve target)
        {
            for (int i = 0; i < target.keys.Length; i++)
            {
                if (target[i].time != settings.curve[i].time ||
                    target[i].value != settings.curve[i].value ||
                    target[i].inTangent != settings.curve[i].inTangent ||
                    target[i].outTangent != settings.curve[i].outTangent)
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetProcessorSequenceLength()
        {
            if (InputSequence.length > 0)
                return m_SerializedObject.FindProperty("outputSequenceLength").intValue;
            else
                return 0;
        }

        public override bool Process(int frame)
        {

            int inputlength = InputSequence.length;
            int outputlength = GetProcessorSequenceLength();
            float t = (float)frame / outputlength;

            float Frame;

            if (settings.useCurve)
                Frame = settings.curve.Evaluate(t);
            else
                Frame = t * inputlength;

            float blendFactor = Frame % 1.0f;
            int Prev = Mathf.Clamp((int)Mathf.Floor(Frame),0,inputlength-1);
            int Next = Mathf.Clamp((int)Mathf.Ceil(Frame),0,inputlength-1);

            Texture prevtex = InputSequence.RequestFrame(Prev).texture;
            Texture nexttex = InputSequence.RequestFrame(Next).texture;

            m_Material.SetTexture("_MainTex", prevtex);
            m_Material.SetTexture("_AltTex", nexttex);
            m_Material.SetFloat("_BlendFactor", blendFactor);

            ExecuteShaderAndDump(frame, prevtex);
            return true;
        }

        public override string GetLabel()
        {
            return string.Format("{0} ({1} frames)",GetName(), settings.outputSequenceLength);
        }

        public override string GetName()
        {
            return "Retime";
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            var useCurve = m_SerializedObject.FindProperty("useCurve");
            var outputSequenceLength = m_SerializedObject.FindProperty("outputSequenceLength");

            EditorGUI.BeginChangeCheck();

            int length = outputSequenceLength.intValue;
            int newlength = EditorGUILayout.IntSlider(VFXToolboxGUIUtility.Get("Sequence Length"), length,1,InputSequence.length);
            if(newlength != length)
            {
                outputSequenceLength.intValue = newlength;
            }

            EditorGUILayout.PropertyField(useCurve, VFXToolboxGUIUtility.Get("Use Retiming Curve"));

            if(settings.useCurve)
            {
                m_CurveDrawer.SetBounds(new Rect(0,0,1,InputSequence.length - 1));

                if(m_CurveDrawer.OnGUILayout())
                {
                    hasChanged = true;
                }
            }

            if(EditorGUI.EndChangeCheck())
            {
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
