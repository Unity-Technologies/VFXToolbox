using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Sequence","Retime")]
    class RetimeProcessor : ProcessorBase
    {
        public AnimationCurve curve;
        public int outputSequenceLength;
        public bool useCurve;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Blend.shader";

        public override string processorName => "Retime";

        public override string label => $"{processorName} ({outputSequenceLength} frames)";

        public override int sequenceLength
        {
            get
            {
                if (processor.InputSequence.length > 0)
                    return outputSequenceLength;
                else
                    return 0;
            }
        }

        public override void Default()
        {
            curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0, 0));
            curve.AddKey(new Keyframe(1, 24));
            outputSequenceLength = 25;
            useCurve = true;
        }

        public override bool Process(int frame)
        {
            int inputlength = processor.InputSequence.length;
            int outputlength = sequenceLength;
            float t = (float)frame / outputlength;

            float Frame;

            if (useCurve)
                Frame = curve.Evaluate(t);
            else
                Frame = t * inputlength;

            float blendFactor = Frame % 1.0f;
            int Prev = Mathf.Clamp((int)Mathf.Floor(Frame), 0, inputlength - 1);
            int Next = Mathf.Clamp((int)Mathf.Ceil(Frame), 0, inputlength - 1);

            Texture prevtex = processor.InputSequence.RequestFrame(Prev).texture;
            Texture nexttex = processor.InputSequence.RequestFrame(Next).texture;

            processor.material.SetTexture("_MainTex", prevtex);
            processor.material.SetTexture("_AltTex", nexttex);
            processor.material.SetFloat("_BlendFactor", blendFactor);

            processor.ExecuteShaderAndDump(frame, prevtex);
            return true;
        }

        CurveDrawer m_CurveDrawer;

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            if (m_CurveDrawer == null)
            {
                m_CurveDrawer = new CurveDrawer("Retime Curve", 0.0f, 1.0f, 0.0f, processor.InputSequence.length, 140, false);
                m_CurveDrawer.AddCurve(serializedObject.FindProperty("curve"), new Color(0.5f, 0.75f, 1.0f), "Retime Curve");
                m_CurveDrawer.OnPostGUI = OnCurveFieldGUI;
            }


            var useCurve = serializedObject.FindProperty("useCurve");
            var outputSequenceLength = serializedObject.FindProperty("outputSequenceLength");

            EditorGUI.BeginChangeCheck();

            int length = outputSequenceLength.intValue;
            int newlength = EditorGUILayout.IntSlider(VFXToolboxGUIUtility.Get("Sequence Length"), length, 1, processor.InputSequence.length);
            if (newlength != length)
            {
                outputSequenceLength.intValue = newlength;
            }

            EditorGUILayout.PropertyField(useCurve, VFXToolboxGUIUtility.Get("Use Retiming Curve"));

            if (useCurve.boolValue)
            {
                m_CurveDrawer.SetBounds(new Rect(0, 0, 1, processor.InputSequence.length - 1));

                if (m_CurveDrawer.OnGUILayout())
                {
                    changed = true;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                changed = true;
            }

            return changed;
        }

        void OnCurveFieldGUI(Rect renderArea, Rect curveArea)
        {
            float seqRatio = -1.0f;
            if (processor.isCurrentlyPreviewed)
            {
                seqRatio = (processor.currentPreviewSequenceLength > 1) ? (float)processor.currentPreviewFrame / (processor.currentPreviewSequenceLength - 1) : 0.0f;
            }

            // If previewing current sequence : draw trackbar
            if (seqRatio >= 0.0f)
            {
                Handles.color = Color.white;
                Handles.DrawLine(new Vector3(curveArea.xMin + seqRatio * curveArea.width, renderArea.yMin), new Vector3(curveArea.xMin + seqRatio * curveArea.width, renderArea.yMax));
            }
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
    }
}
