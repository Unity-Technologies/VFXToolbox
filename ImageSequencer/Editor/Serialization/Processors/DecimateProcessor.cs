using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Sequence","Decimate")]
    internal class DecimateProcessor : ProcessorBase
    {
        public ushort DecimateBy;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Null.shader";

        public override string label => $"{name} (1 of {DecimateBy})";

        public override string processorName => "Decimate";

        public override int sequenceLength => Mathf.Max(1, (int)Mathf.Floor((float)processor.InputSequence.length / DecimateBy));

        public override void Default()
        {
            DecimateBy = 3;
        }

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            var decimateBy = serializedObject.FindProperty("DecimateBy");

            EditorGUI.BeginChangeCheck();

            int newDecimate = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Decimate by"), (int)DecimateBy), 2, processor.InputSequence.length);

            if (newDecimate != decimateBy.intValue)
            {
                decimateBy.intValue = newDecimate;
            }

            if (EditorGUI.EndChangeCheck())
            {
                changed = true;
            }

            return changed;
        }

        public override bool Process(int frame)
        {
            int targetFrame = frame * DecimateBy;
            Texture texture = processor.InputSequence.RequestFrame(targetFrame).texture;
            processor.material.SetTexture("_MainTex", texture);
            processor.ExecuteShaderAndDump(frame, texture);
            return true;
        }
    }
}


