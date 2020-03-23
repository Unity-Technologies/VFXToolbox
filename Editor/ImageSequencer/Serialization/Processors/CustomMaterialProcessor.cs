using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [Processor("","Custom Material")]
    class CustomMaterialProcessor : ProcessorBase
    {
        [FormerlySerializedAs("material")]
        public Material customMaterial;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/Editor/ImageSequencer/Shaders/Null.shader";

        public override string processorName => "Custom Material";

        public override string label => $"{processorName} ({((customMaterial == null) ? "Not Set" : customMaterial.name)})";

        public override void Default()
        {
            customMaterial = null;
        }

        public override bool Process(int frame)
        {
            Texture inputFrame = RequestInputTexture(frame);

            if (customMaterial != null)
            {
                customMaterial.SetTexture("_InputFrame", inputFrame);
                customMaterial.SetVector("_FrameData", new Vector4(inputSequenceWidth, inputSequenceHeight, frame, sequenceLength));
                customMaterial.SetVector("_FlipbookData", new Vector4(inputSequenceNumU, inputSequenceNumV, 0, 0));
                ProcessFrame(frame, inputFrame, customMaterial);
            }
            else
            {
                material.SetTexture("_MainTex", inputFrame);
                ProcessFrame(frame, inputFrame);
            }
            return true;
        }

        private Editor m_MaterialEditor;
        private Shader m_CachedShader;

        public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
        {
            EditorGUI.BeginChangeCheck();
            Material mat = (Material)EditorGUILayout.ObjectField(VFXToolboxGUIUtility.Get("Material"), customMaterial, typeof(Material), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Custom Material Change");
                customMaterial = mat;
                EditorUtility.SetDirty(this);
                Invalidate();
                changed = true;
            }

            if (customMaterial != null)
            {
                Editor.CreateCachedEditor(customMaterial, typeof(MaterialEditor), ref m_MaterialEditor);
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.DrawHeader();
                EditorGUIUtility.labelWidth = 120;
                m_MaterialEditor.OnInspectorGUI();

                if (m_CachedShader != customMaterial.shader)
                {
                    // Hack : we cache shader in order to track changes as DrawHeader does not consider shader change as a EditorGUI.changed
                    m_CachedShader = customMaterial.shader;
                    GUI.changed = true;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Invalidate();
                    changed = true;
                }

            }
            return changed;
        }

    }
}
