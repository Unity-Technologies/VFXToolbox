using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("Color","Alpha From RGB")]
    internal class AlphaFromRGBProcessor : ProcessorBase
    {
        public Color BWFilterTint;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/AlphaFromRGB.shader";

        public override string processorName => "Alpha From RGB";

        public override void Default()
        {
            BWFilterTint = Color.white;
        }

        public override bool Process(int frame)
        {
            Texture inputFrame = RequestInputTexture(frame);
            material.SetTexture("_MainTex", inputFrame);
            material.SetVector("_RGBTint", BWFilterTint);

            ProcessFrame(frame, inputFrame);
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
                Invalidate();
                changed = true;
            }

            return changed;
        }


    }
}
