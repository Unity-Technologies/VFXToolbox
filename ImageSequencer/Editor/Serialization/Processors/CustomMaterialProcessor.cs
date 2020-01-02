using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("","Custom Material")]
    class CustomMaterialProcessor : ProcessorBase
    {
        public Material material;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Null.shader";

        public override string processorName => "Custom Material";

        public override string label => $"{processorName} ({((material == null) ? "Not Set" : material.name)})";

        public override void Default()
        {
            material = null;
        }

        public override bool Process(int frame)
        {
            Texture inputFrame = processor.InputSequence.RequestFrame(frame).texture;

            if (material != null)
            {
                material.SetTexture("_InputFrame", inputFrame);
                material.SetVector("_FrameData", new Vector4(processor.OutputWidth, processor.OutputHeight, frame, sequenceLength));
                material.SetVector("_FlipbookData", new Vector4(processor.NumU, processor.NumV, 0, 0));
                processor.ExecuteShaderAndDump(frame, inputFrame, material);
            }
            else
            {
                processor.material.SetTexture("_MainTex", inputFrame);
                processor.ExecuteShaderAndDump(frame, inputFrame);
            }
            return true;
        }

        private Editor m_MaterialEditor;
        private Shader m_CachedShader;

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            EditorGUI.BeginChangeCheck();
            Material mat = (Material)EditorGUILayout.ObjectField(VFXToolboxGUIUtility.Get("Material"), material, typeof(Material), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Custom Material Change");
                material = mat;
                EditorUtility.SetDirty(this);
                processor.Invalidate();
                changed = true;
            }

            if (material != null)
            {
                Editor.CreateCachedEditor(material, typeof(MaterialEditor), ref m_MaterialEditor);
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.DrawHeader();
                EditorGUIUtility.labelWidth = 120;
                m_MaterialEditor.OnInspectorGUI();

                if (m_CachedShader != material.shader)
                {
                    // Hack : we cache shader in order to track changes as DrawHeader does not consider shader change as a EditorGUI.changed
                    m_CachedShader = material.shader;
                    GUI.changed = true;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    processor.Invalidate();
                    changed = true;
                }

            }
            return changed;
        }

    }
}
