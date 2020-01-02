using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Color","Premultiply Alpha")]
    class PremultiplyAlphaProcessor : ProcessorBase
    {
        public bool RemoveAlpha;
        public float AlphaValue;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/PremultiplyAlpha.shader";

        public override string processorName => "Premultiply Alpha";

        public override void Default()
        {
            RemoveAlpha = false;
            AlphaValue = 1.0f;
        }
        public override bool Process(int frame)
        {
            Texture inputFrame = processor.InputSequence.RequestFrame(frame).texture;
            processor.material.SetTexture("_MainTex", inputFrame);
            processor.material.SetInt("_RemoveAlpha", RemoveAlpha ? 1 : 0);
            processor.material.SetFloat("_AlphaValue", AlphaValue);
            processor.ExecuteShaderAndDump(frame, inputFrame);
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
                processor.Invalidate();
                changed = true;
            }

            return changed;
        }

    }
}
