using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Color","Alpha From RGB")]
    class AlphaFromRGBProcessor : ProcessorBase
    {
        public Color BWFilterTint;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/AlphaFromRGB.shader";

        public override string processorName => "Alpha From RGB";

        public override void Default()
        {
            BWFilterTint = Color.white;
        }

        public override bool Process(int frame)
        {
            Texture inputFrame = processor.InputSequence.RequestFrame(frame).texture;
            processor.material.SetTexture("_MainTex", inputFrame);
            processor.material.SetVector("_RGBTint", BWFilterTint);

            processor.ExecuteShaderAndDump(frame, inputFrame);
            return true;
        }

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            var tint = serializedObject.FindProperty("BWFilterTint");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(tint, VFXToolboxGUIUtility.Get("Color Filter"));
            EditorGUILayout.HelpBox("Color Filter serves as a tint before applying the black and white desaturation, just like in black and white photography. This way you can filter color weighting.", MessageType.Info);

            if (EditorGUI.EndChangeCheck())
            {
                processor.Invalidate();
                changed = true;
            }

            return changed;
        }


    }
}
