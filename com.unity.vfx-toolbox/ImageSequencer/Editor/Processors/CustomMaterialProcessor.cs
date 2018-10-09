using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    class CustomMaterialProcessor : GPUFrameProcessor<CustomMaterialProcessorSettings>
    {
        private Editor m_MaterialEditor;
        private Shader m_CachedShader; 
        public CustomMaterialProcessor(FrameProcessorStack stack, ProcessorInfo info) 
            : base("Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Null.shader", stack,info)
        {
            
        }

        public override string GetLabel()
        {
            return string.Format("{0} ({1})",GetName(), (settings.material == null)? "Not Set": settings.material.name);
        }

        public override string GetName()
        {
            return "Custom Material"; 
        }

        public override bool OnCanvasGUI(ImageSequencerCanvas canvas)
        {
            return false;
        }

        public override bool Process(int frame)
        {
            Texture inputFrame = InputSequence.RequestFrame(frame).texture;

            if (settings.material != null)
            {
                settings.material.SetTexture("_InputFrame", inputFrame);
                settings.material.SetVector("_FrameData", new Vector4(OutputWidth,OutputHeight,frame,GetProcessorSequenceLength()));
                settings.material.SetVector("_FlipbookData", new Vector4(NumU,NumV,0,0));
                ExecuteShaderAndDump(frame, inputFrame, settings.material);
            }
            else
            {
                m_Material.SetTexture("_MainTex", inputFrame);
                ExecuteShaderAndDump(frame, inputFrame);
            }
            return true;
        }

        protected override bool DrawSidePanelContent(bool hasChanged)
        {
            EditorGUI.BeginChangeCheck();
            Material mat = (Material)EditorGUILayout.ObjectField(VFXToolboxGUIUtility.Get("Material"), settings.material, typeof(Material),false);
            if(EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Custom Material Change");
                settings.material = mat;
                EditorUtility.SetDirty(settings);
                Invalidate();
                hasChanged = true;
            }

            if(settings.material != null)
            {
                Editor.CreateCachedEditor(settings.material, typeof(MaterialEditor), ref m_MaterialEditor);
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.DrawHeader();
                EditorGUIUtility.labelWidth = 120;
                m_MaterialEditor.OnInspectorGUI();

                if(m_CachedShader != settings.material.shader)
                {
                    // Hack : we cache shader in order to track changes as DrawHeader does not consider shader change as a EditorGUI.changed
                    m_CachedShader = settings.material.shader;
                    GUI.changed = true;
                }

                if(EditorGUI.EndChangeCheck())
                {
                    Invalidate();
                    hasChanged = true;
                }


            }
            return hasChanged;
        }
    }
}
