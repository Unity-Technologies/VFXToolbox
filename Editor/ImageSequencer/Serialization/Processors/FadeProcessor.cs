using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("Sequence","Fade")]
    internal class FadeProcessor : ProcessorBase
    {
        public AnimationCurve FadeCurve;
        public Color FadeToColor;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/Fade.shader";

        public override string processorName => "Fade";

        public override void Default()
        {
            FadeCurve = new AnimationCurve();
            FadeCurve.AddKey(new Keyframe(0.85f, 1f));
            FadeCurve.AddKey(new Keyframe(1f, 0f));
            FadeToColor = new Color(0.25f,0.25f,0.25f,0.0f);
        }

        public override bool Process(int frame)
        {
            Texture inputFrame = RequestInputTexture(frame);
            material.SetTexture("_MainTex", inputFrame);
            material.SetColor("_FadeToColor", FadeToColor);
            material.SetFloat("_Ratio", FadeCurve.Evaluate(((float)frame) / sequenceLength));
            ProcessFrame(frame, inputFrame);
            return true;
        }

        CurveDrawer m_CurveDrawer;

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            if (m_CurveDrawer == null)
            {
                m_CurveDrawer = new CurveDrawer("Fade Curve", 0.0f, 1.0f, 0.0f, 1.0f, 140, false);
                m_CurveDrawer.AddCurve(serializedObject.FindProperty("FadeCurve"), new Color(0.75f, 0.5f, 1.0f), "Fade Curve");
                m_CurveDrawer.OnPostGUI = OnCurveFieldGUI;
            }
            var fadeToColor = serializedObject.FindProperty("FadeToColor");

            EditorGUI.BeginChangeCheck();

            Color c = EditorGUILayout.ColorField(VFXToolboxGUIUtility.Get("Fade To Color"), fadeToColor.colorValue);

            if (c != fadeToColor.colorValue)
            {
                fadeToColor.colorValue = c;
            }

            if (m_CurveDrawer.OnGUILayout())
            {
                changed = true;
            }

            if (EditorGUI.EndChangeCheck())
            {
                Invalidate();
                changed = true;
            }

            return changed;

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

        bool CurveEquals(AnimationCurve target)
        {
            for (int i = 0; i < target.keys.Length; i++)
            {
                if (target[i].time != FadeCurve[i].time ||
                    target[i].value != FadeCurve[i].value ||
                    target[i].inTangent != FadeCurve[i].inTangent ||
                    target[i].outTangent != FadeCurve[i].outTangent)
                {
                    return false;
                }
            }
            return true;
        }

    }
}
