using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System;

namespace UnityEditor.VFXToolbox.VolumeEditor
{
    public class VectorFieldVolumeRenderer : VolumeRendererBase
    {
        public enum ViewMode
        {
            VolumeFloaters,
            SliceFloaters
        }

        public enum ViewModeSliceAxis
        {
            XY,
            XZ,
            YZ
        }

        public ViewMode viewMode { get { return m_ViewMode; } set { SetViewMode(value); } }
        public ViewModeSliceAxis sliceAxis { get { return m_ViewModeSliceAxis; } set { SetSliceAxis(value); } }
        public float slicePosition { get { return m_SlicePosition; } set { SetSlicePosition(value); } }
        public uint reduceFactor { get { return m_ReduceFactor; } set { SetReduceFactor(value); } }
        public float floaterStep { get { return m_FloaterStep; } set { m_FloaterStep =value; } }
        public float heatMapScale { get { return m_HeatMapScale; } set { m_HeatMapScale =value; } }

        private uint m_ReduceFactor;
        private float m_FloaterStep;

        private uint m_VolumeSizeX;
        private uint m_VolumeSizeY;
        private uint m_VolumeSizeZ;

        private List<Texture2D> m_HeatMap;
        private int m_CurrentHeatMap;
        private float m_HeatMapScale;

        private ViewMode m_ViewMode;
        private ViewModeSliceAxis m_ViewModeSliceAxis;
        private float m_SlicePosition;

        protected List<Mesh> m_PreviewMesh;

        protected Material m_PreviewMeshMaterial;

        private const int NUM_POINTS_PER_TRAIL = 32;
        private const uint MAX_SIZE = 64;

        public VectorFieldVolumeRenderer() : base()
        {
            m_VolumeSizeX = 0;
            m_VolumeSizeY = 0;
            m_VolumeSizeZ = 0;
            m_ReduceFactor = 1;
            m_FloaterStep = 0.03f;
            m_HeatMapScale = 3.5f;
            m_ViewMode = ViewMode.VolumeFloaters;
            m_ViewModeSliceAxis = ViewModeSliceAxis.XY;
            m_SlicePosition = 0.5f;
            m_PreviewMeshMaterial = new Material(Shader.Find("Hidden/VFXToolbox/VolumeEditor/VolumeTrail"));
            m_HeatMap = new List<Texture2D>();
            m_HeatMap.Add(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFXToolbox/VolumeEditor/Editor/Textures/heatmap.tga"));
            m_HeatMap.Add(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFXToolbox/VolumeEditor/Editor/Textures/heatmap2.tga"));

            m_PreviewMeshMaterial.SetInt("_NumPointsPerTrail", NUM_POINTS_PER_TRAIL);
        }

        public override void OnGUI()
        {
            viewMode = (ViewMode)EditorGUILayout.EnumPopup("View Mode", viewMode);
            if(viewMode == ViewMode.SliceFloaters)
            {
                sliceAxis = (ViewModeSliceAxis)EditorGUILayout.EnumPopup("Slice Axis", sliceAxis);
                slicePosition = EditorGUILayout.Slider("Slice Position", slicePosition, 0, 1);
            }

            reduceFactor = (uint)EditorGUILayout.IntSlider("Reduce By",(int)reduceFactor, 0, 4);
            floaterStep = EditorGUILayout.Slider("Simulation Step: ", floaterStep, 0.001f, 0.05f);
            heatMapScale = EditorGUILayout.Slider("HeatMap Scale", heatMapScale, 0.1f, 5.0f);
            m_CurrentHeatMap = EditorGUILayout.IntPopup("Heat Map Template", m_CurrentHeatMap, new string[] { "Thermal", "Rainbow" }, new int[] { 0, 1 });
        }


        public override void Render(PreviewRenderUtility previewUtility)
        {
            if (m_VolumeSizeX > 0 && m_VolumeSizeY > 0 && m_VolumeSizeZ > 0)
            {
                if (m_PreviewMesh == null)
                    RegenerateGeometry();
                m_PreviewMeshMaterial.SetFloat("_Length", m_FloaterStep);

                m_PreviewMeshMaterial.SetTexture("_HeatMap", m_HeatMap[m_CurrentHeatMap]);
                m_PreviewMeshMaterial.SetFloat("_HeatMapScale", m_HeatMapScale);

                foreach(Mesh mesh in m_PreviewMesh)
                    previewUtility.DrawMesh(mesh, Matrix4x4.identity, m_PreviewMeshMaterial, 0);

                RenderOutlineCube(previewUtility);
            }
        }

        public override void SetTexture(Texture texture)
        {
            if(texture == null)
            {
                m_VolumeSizeX = 0;
                m_VolumeSizeY = 0;
                m_VolumeSizeZ = 0;
            }
            else
            {
                Texture3D volume = (Texture3D)texture;
                 m_PreviewMeshMaterial.SetTexture("_Volume", volume);
                if(m_VolumeSizeX != (uint)volume.width || m_VolumeSizeY != (uint)volume.height || m_VolumeSizeZ != (uint)volume.depth)
                {
                    m_VolumeSizeX = (uint)volume.width;
                    m_VolumeSizeY = (uint)volume.height;
                    m_VolumeSizeZ = (uint)volume.depth;
                    RegenerateGeometry();
                    m_PreviewMeshMaterial.SetVector("_Dimensions", new Vector4(m_VolumeSizeX, m_VolumeSizeY, m_VolumeSizeZ, 1.0f));
                }
            }
        }

        private void SetReduceFactor(uint factor)
        {
            if(factor != m_ReduceFactor)
            {
                m_ReduceFactor = factor;
                m_PreviewMeshMaterial.SetInt("_ReduceFactor", (int)Mathf.Pow(2,reduceFactor));
            }
        }

        private void SetViewMode(ViewMode newMode)
        {
            if(newMode != m_ViewMode)
            {
                m_ViewMode = newMode;
                RegenerateGeometry();

                switch(m_ViewMode)
                {
                    case ViewMode.VolumeFloaters:
                        SetSlicePosition(0.0f);
                        break;
                    default: break;
                }
            }
        }

        private void SetSliceAxis(ViewModeSliceAxis axis)
        {
            if(m_ViewModeSliceAxis != axis)
            {
                m_ViewModeSliceAxis = axis;
                RegenerateGeometry();
            }
        }

        private void SetSlicePosition(float value)
        {
            Vector4 offset = Vector4.zero;
            m_SlicePosition = value;
            switch(sliceAxis)
            {
                case ViewModeSliceAxis.XY:
                    offset = new Vector4(0, 0, value, 0);
                    break;
                case ViewModeSliceAxis.YZ:
                    offset = new Vector4(value, 0, 0, 0);
                    break;
                case ViewModeSliceAxis.XZ:
                    offset = new Vector4(0, value, 0, 0);
                    break;
            }
            m_PreviewMeshMaterial.SetVector("_Offset", offset);
        }

        public void RegenerateGeometry()
        {
            switch(m_ViewMode)
            {
                case ViewMode.VolumeFloaters:
                    CreateVolumeFloaterMesh(false, ViewModeSliceAxis.XY);
                    break;
                case ViewMode.SliceFloaters:
                    CreateVolumeFloaterMesh(true, m_ViewModeSliceAxis);
                    break;
            }
        }

        protected void CreateVolumeFloaterMesh(bool bSlice, ViewModeSliceAxis axis)
        {
            if(m_PreviewMesh == null)
                m_PreviewMesh = new List<Mesh>();

            if (m_VolumeSizeX > 0 && m_VolumeSizeY > 0 && m_VolumeSizeZ > 0)
            {
                uint indexStride = (NUM_POINTS_PER_TRAIL-1) * 2;
                uint vertexStride = NUM_POINTS_PER_TRAIL;

                uint numFloatersX = (bSlice && axis == ViewModeSliceAxis.YZ)? 1 : m_VolumeSizeX;
                uint numFloatersY = (bSlice && axis == ViewModeSliceAxis.XZ)? 1 : m_VolumeSizeY;
                uint numFloatersZ = (bSlice && axis == ViewModeSliceAxis.XY)? 1 : m_VolumeSizeZ;

                uint numFloaters = numFloatersX * numFloatersY * numFloatersZ;

                uint numVertices = vertexStride * numFloaters;
                uint numSplits = (numVertices+65535) / 65536;

                if (m_PreviewMesh == null)
                    m_PreviewMesh = new List<Mesh>();

                m_PreviewMesh.Clear();

                uint maxChunksPerSplit = 65535 / vertexStride;
                uint chunksLeft = numFloaters;

                Vector3 meshCenter = new Vector3(0.5f, 0.5f, 0.5f);

                for(int i = 0; i < numSplits; i++)
                {
                    EditorUtility.DisplayProgressBar("Volume Viewer", "Creating Mesh", (float)i / numSplits);
                    Mesh mesh = new Mesh();
                    uint chunkOffset = ((uint)i * maxChunksPerSplit);

                    List<Vector3> vertices = new List<Vector3>();

                    // How many chunks this time?
                    uint numChunks = (chunksLeft > maxChunksPerSplit) ? maxChunksPerSplit : chunksLeft;
                    chunksLeft -= numChunks;

                    int[] indices = new int[numChunks * indexStride];

                    // Foreach Chunk
                    for(uint j = 0; j < numChunks; j++)
                    {
                        uint vertexOffset = j * vertexStride;
                        uint indexOffset =  j * indexStride;

                        // Vertices & UVs
                        for(uint k = 0; k < vertexStride; k++)
                        {
                            Vector3 pos = PositionFromIndex(j + chunkOffset, numFloatersX, numFloatersY, numFloatersZ);
                            vertices.Add(pos - meshCenter);
                        }

                        // indices
                        for(uint k = 0; k < indexStride/2; k++)
                        {
                            indices[indexOffset + (k * 2)] = (int)(vertexOffset + k);
                            indices[indexOffset + (k * 2) + 1] = (int)(vertexOffset + k + 1);
                        }
                    }

                    mesh.SetVertices(vertices);
                    mesh.SetIndices(indices, MeshTopology.Lines, 0);

                    mesh.bounds = new Bounds(Vector3.zero, new Vector3(1, 1, 1));
                    m_PreviewMesh.Add(mesh);
                }
                EditorUtility.ClearProgressBar();
            }
        }

        private Vector3 PositionFromIndex(uint index, uint numX, uint numY, uint numZ)
        {
            uint x = index % numX;
            uint y = (index / numX) % numY;
            uint z = (index / (numX * numY)); 
            return new Vector3((float)x/numX, (float)y/numY, (float)z/numZ);
        }

        public override string ToString()
        {
            return "Vector Field";
        }
    }
}
