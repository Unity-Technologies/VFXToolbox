using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("Color","Premultiply Alpha")]
    class PremultiplyAlphaProcessor : ProcessorBase
    {
        public bool RemoveAlpha;
        public float AlphaValue;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/PremultiplyAlpha.shader";

        public override string processorName => "Premultiply Alpha";

        public override void Default()
        {
            RemoveAlpha = false;
            AlphaValue = 1.0f;
        }
        public override bool Process(int frame)
        {
            Texture inputFrame = RequestInputTexture(frame);
            material.SetTexture("_MainTex", inputFrame);
            material.SetInt("_RemoveAlpha", RemoveAlpha ? 1 : 0);
            material.SetFloat("_AlphaValue", AlphaValue);
            ProcessFrame(frame, inputFrame);
            return true;
        }
        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            var removeAlpha = serializedObject.FindProperty("RemoveAlpha");
            var alphaValue = serializedObject.FindProperty("AlphaValue");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(removeAlpha, VFXToolboxGUIUtility.Get("Remove Alpha"));
            EditorGUI.BeginDisabledGroup(!removeAlpha.boolValue);
            EditorGUILayout.PropertyField(alphaValue, VFXToolboxGUIUtility.Get("Alpha Value"));
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                Invalidate();
                changed = true;
            }

            return changed;
        }

    }
}
