using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace UnityEditor.VFXToolbox.VolumeEditor
{
    public abstract class VolumeRendererBase
    {
        private Mesh m_OutlineCubeMesh;
        private Material m_OutlineCubeMaterial;

        public VolumeRendererBase()
        {
            m_OutlineCubeMaterial = new Material(Shader.Find("Unlit/Color"));
            GenerateOutlineCube();
        }

        protected void RenderOutlineCube(PreviewRenderUtility previewUtility)
        {
            RenderOutlineCube(previewUtility, new Color(1, 1, 1, 0.25f));
        }

        protected void RenderOutlineCube(PreviewRenderUtility previewUtility, Color color)
        {
            m_OutlineCubeMaterial.SetColor ("_Color", color);
            previewUtility.DrawMesh(m_OutlineCubeMesh, Matrix4x4.identity, m_OutlineCubeMaterial, 0);

        }

        public abstract void Render(PreviewRenderUtility previewUtility);

        public abstract void OnGUI();

        public abstract void SetTexture(Texture texture);

        private void GenerateOutlineCube()
        {
            if (m_OutlineCubeMesh == null)
                m_OutlineCubeMesh = new Mesh();

            m_OutlineCubeMesh.Clear();
            List<Vector3> vertices = new List<Vector3>();
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f));
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));

            int[] indices = new int[]
             {
                0,1,1,3,3,2,2,0,
                4,5,5,7,7,6,6,4,
                0,4,1,5,2,6,3,7
             };

            m_OutlineCubeMesh.SetVertices(vertices);
            m_OutlineCubeMesh.SetIndices(indices,MeshTopology.Lines,0);
        }

    }
}
