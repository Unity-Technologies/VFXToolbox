using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    internal class ProcessingFrame
    {
        public Texture texture
        {
            get {
                if (m_Texture == null)
                {
                    if(m_ProcessingNode == null) // For input frames, either our input asset has been deleted, or something went wrong with the meta's, let's replace by a dummy
                    {
                        m_Texture = Missing.texture;
                        m_Texture.name = @"/!\ MISSING /!\";
                    }
                    else // For processor's outputs, Should not happen, unless reset by changing Linear/Gamma or Graphics API
                        ResetTexture();
                }
                return m_Texture;
            }
        }

        public bool isInputFrame
        {
            get { return m_ProcessingNode == null; }
        }

        public int mipmapCount
        {
            get
            {
                if (isInputFrame)
                    return ((Texture2D)texture).mipmapCount;
                else
                {
                    return (int)Mathf.Max(1,(Mathf.Log(Mathf.Max(texture.width, texture.height), 2) - 1));
                }
            }
        }

        public bool dirty;

        private Texture m_Texture;
        private ProcessingNode m_ProcessingNode;

        public ProcessingFrame(Texture texture)
        {
            m_Texture = texture;
            dirty = false;
            m_ProcessingNode = null;
        }

        public ProcessingFrame(ProcessingNode node)
        {
            dirty = true;
            m_ProcessingNode = node;
            ResetTexture();
        }

        public void SyncSize()
        {
            if(texture.width != m_ProcessingNode.OutputWidth || texture.height != m_ProcessingNode.OutputHeight )
            {
                ResetTexture();
            }
        }

        private void ResetTexture()
        {
            if(m_Texture == null || (m_ProcessingNode != null && (m_Texture.width != m_ProcessingNode.OutputWidth || m_Texture.height != m_ProcessingNode.OutputHeight)))
            {
                RenderTexture.DestroyImmediate(m_Texture);
                m_Texture = new RenderTexture(m_ProcessingNode.OutputWidth, m_ProcessingNode.OutputHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                ((RenderTexture)m_Texture).autoGenerateMips = true;
            }
        }

        public bool Process()
        {
            if(dirty && m_ProcessingNode != null)
            {
                SyncSize();
                if(m_ProcessingNode.Process(this))
                {
                    dirty = false;
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            Texture2D.DestroyImmediate(m_Texture);
            dirty = true;
        }

        public override string ToString()
        {
            return texture.name.ToString();
        }

        public static ProcessingFrame Missing
        {
            get
            {
                if(s_Missing == null)
                {
                    Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.vfx-toolbox/Editor/Common/Textures/MissingTexture.png");
                    if(t == null)
                    {
                        Debug.LogError("Could not find VFXToolbox Missing Texture, using white texture instead. Make sure you imported all the VFXToolbox files.");
                        t = Texture2D.whiteTexture;
                    }
                    s_Missing = new ProcessingFrame(t);
                }
                return s_Missing;
            }
        }

        private static ProcessingFrame s_Missing;
    }
}
