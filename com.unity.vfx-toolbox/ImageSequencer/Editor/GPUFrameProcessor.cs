using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    internal abstract class GPUFrameProcessor<T> : FrameProcessor<T> where T : ProcessorSettingsBase
    {
        protected Shader m_Shader;
        protected Material m_Material;

        public GPUFrameProcessor(string shaderPath,  FrameProcessorStack processorStack, ProcessorInfo info ) 
            : this(AssetDatabase.LoadAssetAtPath<Shader>(shaderPath),processorStack, info)
        { }

        public GPUFrameProcessor(Shader shader, FrameProcessorStack processorStack, ProcessorInfo info ) : base(processorStack, info)
        {
            m_Shader = shader;
            m_Material = new Material(m_Shader) { hideFlags = HideFlags.DontSave };
            m_Material.hideFlags = HideFlags.DontSave;
        }

        public void ExecuteShaderAndDump(int outputframe, Texture mainTex)
        {
            ExecuteShaderAndDump(outputframe, mainTex, m_Material);
        }

        public void ExecuteShaderAndDump(int outputframe, Texture mainTex, Material material)
        {
            RenderTexture backup = RenderTexture.active;
            Graphics.Blit(mainTex, (RenderTexture)m_OutputSequence.frames[outputframe].texture, material);
            RenderTexture.active = backup;
        }

        public override void Dispose()
        {
            Material.DestroyImmediate(m_Material);
            base.Dispose();
        }

        protected override int GetNumU()
        {
            if (InputSequence.processor == null)
                return 1;
            return InputSequence.numU;
        }

        protected override int GetNumV()
        {
            if (InputSequence.processor == null)
                return 1;
            return InputSequence.numV;
        }

    }
}
