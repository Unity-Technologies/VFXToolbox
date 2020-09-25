using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System;

namespace UnityEditor.VFXToolbox.VolumeEditor
{
    public class DensityVolumeRenderer : VolumeRendererBase
    {
        public float m_DensityScale = 1.0f;
        public float m_ScatteringScale = 1.0f;

        public float m_LightAngle = 0.0f;

        protected Mesh m_PreviewMesh;
        protected Material m_PreviewMeshMaterial;

        public DensityVolumeRenderer() : base ()
        {
            m_PreviewMeshMaterial = new Material(Shader.Find("Hidden/VFXToolbox/VolumeEditor/VolumeCloud"));
            CreateMesh();
        }

        public override void SetTexture(Texture texture)
        {
            m_PreviewMeshMaterial.SetTexture("_Volume", texture);
        }

        public override void Render(PreviewRenderUtility previewUtility)
        {
            float time = (float)EditorApplication.timeSinceStartup;
            
            m_PreviewMeshMaterial.SetVector("_CameraWorldPosition", previewUtility.camera.transform.position);
            m_PreviewMeshMaterial.SetVector("_LightDirection", new Vector3(Mathf.Sin(m_LightAngle), -1f, Mathf.Cos(m_LightAngle)).normalized);
            m_PreviewMeshMaterial.SetFloat("_DensityScale", m_DensityScale);
            m_PreviewMeshMaterial.SetFloat("_ScatteringScale", m_ScatteringScale);
            m_PreviewMeshMaterial.SetFloat("_EditorTime", (float)EditorApplication.timeSinceStartup);
            previewUtility.DrawMesh(m_PreviewMesh, Matrix4x4.identity, m_PreviewMeshMaterial, 0);
            RenderOutlineCube(previewUtility);
        }

        public override void OnGUI()
        {
            m_DensityScale = EditorGUILayout.Slider("Density Scale", m_DensityScale, 0.0f, 5.0f);
            m_ScatteringScale = EditorGUILayout.Slider("Scattering Scale", m_ScatteringScale, 0.0f, 2.0f);
            m_LightAngle = EditorGUILayout.Slider("Light Direction", m_LightAngle, -Mathf.PI, Mathf.PI);
        }

        protected void CreateMesh()
        {
            if(m_PreviewMesh == null)
                m_PreviewMesh = new Mesh();

            m_PreviewMesh.Clear();

            Vector3[] pos = new Vector3[24]
            {
                new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f,-0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f) , new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f)
            };

            Vector3[] nrm = new Vector3[24]
            {
                new Vector3(0, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0),
                new Vector3(0, 0, -1), new Vector3(0, 0, -1), new Vector3(0, 0, -1), new Vector3(0, 0, -1), 
                new Vector3(-1, 0, 0), new Vector3(-1, 0, 0), new Vector3(-1, 0, 0), new Vector3(-1, 0, 0), 
                new Vector3(0, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 0, 1), 
                new Vector3(1, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0), 
                new Vector3(0, -1, 0), new Vector3(0, -1, 0), new Vector3(0, -1, 0), new Vector3(0, -1, 0)
            };

            List<Vector3> positions = new List<Vector3>(pos);
            List<Vector3> normals = new List<Vector3>(nrm);

            int[] triangles = new int[36]
            {
                0,1,2,
                0,2,3,
                4,5,6,
                4,6,7,
                8,9,10,
                8,10,11,
                12,13,14,
                12,14,15,
                16,17,18,
                16,18,19,
                20,21,22,
                20,22,23
            };

            m_PreviewMesh.SetVertices(positions);
            m_PreviewMesh.SetNormals(normals);
            m_PreviewMesh.SetTriangles(triangles, 0);
            m_PreviewMesh.RecalculateBounds();
        }

        public override string ToString()
        {
            return "Density";
        }
    }
}
