using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.VFXToolbox;

namespace UnityEditor.VFXToolbox.Workbench
{
    public class ProgressionMapGenerator : WorkbenchToolBase
    {
        // Serializable
        public int PaintDataWidth;
        public int PaintDataHeight;
        public PaintBuffer PaintData;

        public FlowPaintBrush Brush;

        public ViewMode PreviewMode;
        public Texture2D BaseTexture;
        public Texture2D FinalTexture;

        [Range(0.0f,5.0f)]
        public float PreviewIntensity;
        [Range(1.0f,15.0f)]
        public float PreviewCycleDuration;
        [Range(0.1f,15.0f)]
        public float PreviewTile;

        // Runtime
        private RenderTexture m_RenderTexture;
        private RenderTexture m_PreviewRT;
        private Material m_Material;
        private Material m_PreviewMaterial;

        private SerializedObject m_Object;

        private SerializedProperty m_Width;
        private SerializedProperty m_Height;

        private SerializedProperty m_Brush;
        private SerializedProperty m_BrushTexture;
        private SerializedProperty m_BrushMotionIntensity;
        private SerializedProperty m_BrushTextureIntensity;
        private SerializedProperty m_BrushSize;
        private SerializedProperty m_BrushOpacity;
        private SerializedProperty m_BrushSpacing;

        private SerializedProperty m_ViewMode;
        private SerializedProperty m_BaseTexture;
        private SerializedProperty m_FinalTexture;

        private SerializedProperty m_PreviewIntensity;
        private SerializedProperty m_PreviewCycleDuration;
        private SerializedProperty m_PreviewTile;

        private Vector2 m_PrevBrushPos;
        private Vector2 m_PrevBrushDirection;

        public enum ViewMode
        {
            BaseTexture = 0,
            OutputProgressionMap = 1,
            Animated = 2
        }

        public void OnEnable()
        {
            m_Object = new SerializedObject(this);
            m_Width = m_Object.FindProperty("PaintDataWidth");
            m_Height = m_Object.FindProperty("PaintDataHeight");

            m_Brush = m_Object.FindProperty("Brush");

            m_ViewMode = m_Object.FindProperty("PreviewMode");
            m_BaseTexture = m_Object.FindProperty("BaseTexture");
            m_FinalTexture = m_Object.FindProperty("FinalTexture");

            m_PreviewIntensity = m_Object.FindProperty("PreviewIntensity");
            m_PreviewCycleDuration = m_Object.FindProperty("PreviewCycleDuration");
            m_PreviewTile = m_Object.FindProperty("PreviewTile");
        }

        public override bool OnInspectorGUI()
        {
            bool bchanged = false;
            m_Object.Update();
            EditorGUI.BeginChangeCheck();

            using (new VFXToolboxGUIUtility.HeaderSectionScope("Output Image Options"))
            {
                EditorGUILayout.PropertyField(m_Width);
                EditorGUILayout.PropertyField(m_Height);
            }

            using (new VFXToolboxGUIUtility.HeaderSectionScope("Output Image Options"))
            {
                EditorGUILayout.PropertyField(m_Brush);
            }

            using (new VFXToolboxGUIUtility.HeaderSectionScope("Output Image Options"))
            {
                EditorGUILayout.PropertyField(m_ViewMode);
                switch(PreviewMode)
                {
                    case ViewMode.BaseTexture:
                        EditorGUILayout.PropertyField(m_BaseTexture);
                        break;
                    case ViewMode.OutputProgressionMap:

                        break;
                    case ViewMode.Animated:
                        EditorGUILayout.PropertyField(m_FinalTexture);
                        EditorGUILayout.PropertyField(m_PreviewIntensity);
                        EditorGUILayout.PropertyField(m_PreviewCycleDuration);
                        EditorGUILayout.PropertyField(m_PreviewTile);
                        break;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                bchanged = true;
            }

            m_Object.ApplyModifiedProperties();

            if(bchanged)
            {
                UpdateRenderTarget();
                UpdatePaintData();
            }

            return bchanged;
        }

        public override void AttachToBehaviour(WorkbenchBehaviour asset)
        {
            base.AttachToBehaviour(asset);
            PaintDataWidth = 256;
            PaintDataHeight = 256;
            PreviewMode = ViewMode.BaseTexture;
            PreviewIntensity = 0.2f;
            PreviewCycleDuration = 2.0f;
            PreviewTile = 4.0f;
        }

        public void UpdatePaintData()
        {
            if (PaintData == null)
            {
                PaintData = CreateInstance<PaintBuffer>();
                PaintData.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(PaintData, this);
                UpdateRenderTarget();
                Clear();
                SavePaintData();
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
            else if(PaintData.Width != PaintDataWidth || PaintData.Height != PaintDataHeight)
            {
                PaintData.FromRenderTexture(m_RenderTexture);
                EditorUtility.SetDirty(this);
            }
        }

        protected override WorkbenchCanvasBase GetCanvas(Workbench window)
        {
            return new WorkbenchImageCanvas(window);
        }

        public void UpdateRenderTarget()
        {
            if (m_RenderTexture.width != PaintDataWidth || m_RenderTexture.height != PaintDataHeight)
            {
                RenderTexture newRT = RenderTexture.GetTemporary(PaintDataWidth, PaintDataHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                RenderTexture.ReleaseTemporary(m_RenderTexture);
                m_RenderTexture = newRT;
                Clear();
            }
        }

        public override void InitializeRuntime()
        {
            m_RenderTexture = RenderTexture.GetTemporary(PaintDataWidth, PaintDataHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            m_Material = new Material(Shader.Find("Hidden/VFXToolbox/ImageScripter/FlowMapPaint"));
            if (PaintData != null)
            {
                Texture2D temp = new Texture2D(PaintDataWidth, PaintDataHeight, TextureFormat.ARGB32, false, true);
                temp.SetPixels(PaintData.data);
                temp.Apply();
                Graphics.Blit(temp, m_RenderTexture);
            }
            else
            {
                Clear();
                UpdatePaintData();
                SavePaintData();
            }
        }

        private void SavePaintData()
        {
            PaintData.FromRenderTexture(m_RenderTexture);
            EditorUtility.SetDirty(PaintData);
        }

        private void Clear()
        {
            RenderTexture backup = RenderTexture.active;
            RenderTexture.active = m_RenderTexture;
            GL.Clear(false, true, new Color(0.5f, 0.5f, 0.0f, 1.0f));
            RenderTexture.active = backup;
        }

        public override void Dispose()
        {
            RenderTexture.ReleaseTemporary(m_RenderTexture);
        }

        public override void Update()
        {
            UpdateRenderTarget();
        }

        public override bool OnCanvasGUI(WorkbenchImageCanvas canvas)
        {
            bool needRepaint = false;

            if(Event.current.type == EventType.Repaint)
            {
                switch(PreviewMode)
                {
                    case ViewMode.BaseTexture:
                        canvas.texture = BaseTexture;
                        break;
                    case ViewMode.OutputProgressionMap:
                        canvas.texture = m_RenderTexture;
                        break;
                    case ViewMode.Animated:

                        if (m_PreviewRT == null)
                            m_PreviewRT = RenderTexture.GetTemporary(PaintDataWidth, PaintDataHeight, 0, RenderTextureFormat.ARGB32);
                
                        if(m_PreviewRT.width != PaintDataWidth || m_PreviewRT.height != PaintDataHeight)
                        {
                            RenderTexture.ReleaseTemporary(m_PreviewRT);
                            m_PreviewRT = RenderTexture.GetTemporary(PaintDataWidth, PaintDataHeight, 0, RenderTextureFormat.ARGB32);
                        }
                        if (m_PreviewMaterial == null)
                            m_PreviewMaterial = new Material(Shader.Find("Hidden/VFXToolbox/ImageScripter/FlowMapPainter.DisplayFlow"));

                        m_PreviewMaterial.SetTexture("_MainTex", BaseTexture);
                        m_PreviewMaterial.SetTexture("_FlowTex", m_RenderTexture);
                        m_PreviewMaterial.SetFloat("_EdTime", (float)(EditorApplication.timeSinceStartup%PreviewCycleDuration));
                        m_PreviewMaterial.SetFloat("_Intensity", PreviewIntensity);
                        m_PreviewMaterial.SetFloat("_Cycle", PreviewCycleDuration);
                        m_PreviewMaterial.SetFloat("_Tile", PreviewTile);

                        RenderTexture bkp = RenderTexture.active;
                        RenderTexture.active = m_PreviewRT;
                        GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                        Graphics.Blit(BaseTexture, m_PreviewRT, m_PreviewMaterial);
                        GL.sRGBWrite = false;
                        RenderTexture.active = bkp;

                        canvas.texture = m_PreviewRT;
                        needRepaint = true;
                        break;
                }
            }

            Vector2 TopLeft = canvas.CanvasToScreen(new Vector2(m_RenderTexture.width / 2, m_RenderTexture.height / 2));
            Vector2 BottomRight = canvas.CanvasToScreen(new Vector2(-m_RenderTexture.width / 2, -m_RenderTexture.height / 2));

            if (Brush != null)
            {
                if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.button == 0)
                {
                    Vector2 localMousePos = Event.current.mousePosition;
                    Vector2 brushPos = new Vector2((localMousePos.x - TopLeft.x) / (BottomRight.x - TopLeft.x), (localMousePos.y - TopLeft.y) / (BottomRight.y - TopLeft.y));

                    if (Event.current.type == EventType.MouseDown)
                    {
                        m_PrevBrushPos = brushPos;
                        m_PrevBrushDirection = Vector2.zero;
                    }

                    Vector2 pos = brushPos;
                    pos.Scale(new Vector2(m_RenderTexture.width, m_RenderTexture.height));

                    Vector2 brushDirection = (brushPos - m_PrevBrushPos) * Brush.MotionIntensity;

                    PaintSegment(brushPos, m_PrevBrushPos, brushDirection, m_PrevBrushDirection);
                    m_PrevBrushPos = brushPos;
                    m_PrevBrushDirection = brushDirection;

                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    Undo.RegisterCompleteObjectUndo(PaintData, "Paint");
                    UpdatePaintData();
                    SavePaintData();
                }

                needRepaint |= Brush.DrawBrushCanvasPreview(Event.current.mousePosition, Brush.Size * canvas.zoom * 0.5f);
            }

            return needRepaint;
        }

        public void PaintSegment(Vector2 position, Vector2 prevPosition, Vector2 direction, Vector2 prevDirection)
        {
            Vector2 size = new Vector2(PaintDataWidth, PaintDataHeight);

            Vector2 absPosition = position;
            Vector2 absPrevPosition = prevPosition;
            absPosition.Scale(size);
            absPrevPosition.Scale(size);

            float dist = Vector2.Distance(absPosition, absPrevPosition);
            float space = Brush.Spacing * Brush.Size * 0.5f;
            int n = (int)Mathf.Floor(dist/space) + 2;

            m_Material.SetTexture("_MainTex", Brush.Texture);
            m_Material.SetFloat("_BrushOpacity", Brush.Opacity);

            for(int i = 0; i < n; i++)
            {
                float t = (float)(i) / (float)(n-1);
                Vector2 curPos = Vector2.Lerp(prevPosition, position, t);
                Vector2 curDir = Vector2.Lerp(prevDirection, direction, t);
                curPos.Scale(size);
                m_Material.SetVector("_Direction", curDir);
                VFXToolboxUtility.BlitRect(new Rect(curPos.x - Brush.Size / 2, curPos.y - Brush.Size / 2, Brush.Size, Brush.Size), m_RenderTexture, Brush.Texture, m_Material);
            }
        }

        public static string GetCategory()
        { return "Other VFX Tools"; }
        public static string GetName()
        { return "Progression Map Generator"; }


    }
}
