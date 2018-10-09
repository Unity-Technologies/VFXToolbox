using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFXToolbox
{
    internal abstract class VFXToolboxCanvas
    {
        public Rect displayRect
        {
            get { return m_Rect; }
            set { m_Rect = value; }
        }

        public bool maskR
        {
            get { return m_Material.GetColor("_RGBAMask").r == 1.0f; }
            set
            {
                Color c = m_Material.GetColor("_RGBAMask");
                if (value) c.r = 1.0f; else c.r = 0.0f;
                m_Material.SetColor("_RGBAMask", c);
                Invalidate(true);
            }
        }

        public bool maskG
        {
            get { return m_Material.GetColor("_RGBAMask").g == 1.0f; }
            set
            {
                Color c = m_Material.GetColor("_RGBAMask");
                if (value) c.g = 1.0f; else c.g = 0.0f;
                m_Material.SetColor("_RGBAMask", c);
                Invalidate(true);
            }
        }

        public bool maskB
        {
            get { return m_Material.GetColor("_RGBAMask").b == 1.0f; }
            set
            {
                Color c = m_Material.GetColor("_RGBAMask");
                if (value) c.b = 1.0f; else c.b = 0.0f;
                m_Material.SetColor("_RGBAMask", c);
                Invalidate(true);
            }

        }

        public bool maskA
        {
            get { return m_Material.GetColor("_RGBAMask").a == 1.0f; }
            set
            {
                Color c = m_Material.GetColor("_RGBAMask");
                if (value) c.a = 1.0f; else c.a = 0.0f;
                m_Material.SetColor("_RGBAMask", c);
                Invalidate(true);
            }
        }

        public bool filter
        {
            get {   return m_bFilter; }
            set {
                    if(m_bFilter!=value)
                    {
                        m_bFilter = value;
                        InvalidateRenderTarget();
                    }
                    
                }
        }
    
        public int mipMap
        {
            get {
                    return m_MipMap;
            }
            set {
                    if(m_MipMap != value)
                    {
                        m_MipMap = value;
                        m_Material.SetFloat("_MipMap", (float)value);
                        InvalidateRenderTarget();
                    }
                    else
                    {
                        m_MipMap = value;
                    }
            }
        }

        public int mipMapCount
        {
            get { return GetMipMapCount(); }
        }

        public bool showGrid
        {
            get
            {
                return m_bShowGrid;
            }
            set
            {
                m_bShowGrid = value;
            }
        }

        public float BackgroundBrightness
        {
            get
            {
                return m_bgBrightness;
            }
            set
            {
                SetBGBrightness(value);
            }
        }

        public Styles styles
        {
            get
            {
                if (m_Styles == null)
                    m_Styles = new Styles(this);
                return m_Styles;
            }
        }

        public float zoom
        {
            get
            {
                return m_Zoom;
            }
            set
            {
                m_Zoom = value;
                Invalidate(false);
            }
        }

        private Vector2 m_CameraPosition = Vector2.zero;
        private float m_Zoom = 1.0f;
        private bool m_DragPreview = false;
        private bool m_ZoomPreview = false;
        private Vector2 m_ZoomPreviewCenter;
        private Vector2 m_PreviousMousePosition;

        private Styles m_Styles;

        private Vector2 m_ZoomMinMax = new Vector2(0.2f, 10.0f);
        private bool m_bFilter = true;
        private int m_MipMap = 0;
        private bool m_bShowGrid = true;

        protected Rect m_Rect;

        private Shader m_Shader;
        private Material m_Material;
        private RenderTexture m_RenderTexture;

        private bool m_IsDirtyRenderTarget;
        private bool m_bNeedRedraw;
        private float m_bgBrightness = -1.0f;

        public Texture texture
        {
            get { return GetTexture(); }
            set { SetTexture(value); }
        }
        private Texture m_Texture;

        public CanvasGUIDelegate onCanvasGUI;
        public delegate void CanvasGUIDelegate();

        public VFXToolboxCanvas(Rect displayRect, string shaderName = "Packages/com.unity.vfx-toolbox/Editor/Canvas/Shaders/VFXToolboxCanvas.shader") 
        {
            m_Rect = displayRect;

            m_IsDirtyRenderTarget = true;
            m_bNeedRedraw = true;
            m_Shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderName);
            m_Material = new Material(m_Shader) { hideFlags = HideFlags.DontSave };
            m_RenderTexture = RenderTexture.GetTemporary(1,1,0);
        }

        public virtual void Invalidate(bool needRedraw)
        {
            m_bNeedRedraw = m_bNeedRedraw | needRedraw;
        }

        protected abstract void SetTexture(Texture tex);
        protected abstract Texture GetTexture();

        protected virtual int GetMipMapCount()
        {
            if (texture != null)
            {
                if (texture is Texture2D)
                {
                    return (texture as Texture2D).mipmapCount;
                }
                else
                    return 0;
            }
            else
                return 0;
        }

        public void InvalidateRenderTarget()
        {
            m_IsDirtyRenderTarget = true;
        }

        private void UpdateRenderTarget()
        {
            int width = Mathf.Max(1, texture.width / (int)Mathf.Pow(2, (mipMap)));
            int height = Mathf.Max(1, texture.height / (int)Mathf.Pow(2, (mipMap)));

            if(m_RenderTexture.width != width || m_RenderTexture.height != height)
            {
                RenderTexture.ReleaseTemporary(m_RenderTexture);
                m_RenderTexture = RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGBHalf);
            }

            if (filter)
                m_RenderTexture.filterMode = FilterMode.Bilinear;
            else
                m_RenderTexture.filterMode = FilterMode.Point;

            m_IsDirtyRenderTarget = false;
            Invalidate(true);
        }

        public void Recenter(bool Refit)
        {
            m_CameraPosition = Vector2.zero;
            if(Refit)
            {
                float hZoom = (m_Rect.height - 70) / texture.height;
                float wZoom = (m_Rect.width - 70) / texture.width;

                m_Zoom = Mathf.Min(hZoom, wZoom);
            }
            else
            {
                m_Zoom = 1.0f;
            }
        }

        private void Zoom(float ZoomDelta, Vector2 zoomCenter)
        {
            Vector2 centerPos = - new Vector2(zoomCenter.x - m_Rect.width / 2, zoomCenter.y - m_Rect.height / 2) - m_CameraPosition;

            float prevZoom = m_Zoom;

            m_Zoom -= ZoomDelta;
            if(m_Zoom < m_ZoomMinMax.x)
                m_Zoom = m_ZoomMinMax.x;
            else if(m_Zoom > m_ZoomMinMax.y)
                m_Zoom = m_ZoomMinMax.y;
            else
            {
                m_CameraPosition += centerPos - ((m_Zoom / prevZoom) * centerPos);
            }
        }

        protected virtual void HandleKeyboardEvents()
        {
            if(Event.current.type == EventType.KeyDown)
            {
                switch(Event.current.keyCode)
                {
                    // Viewport Toggles
                    case KeyCode.F:
                        Recenter(!Event.current.shift);
                        break;
                    case KeyCode.G:
                        showGrid = !showGrid;
                        break;
                    case KeyCode.J:
                        filter = !filter;
                        Invalidate(true);
                        break;
                    // Brightness Control
                    case KeyCode.V:
                        BrightnessDown(0.1f);
                        break;
                    case KeyCode.B:
                        ResetBrightness();
                        break;
                    case KeyCode.N:
                        BrightnessUp(0.1f);
                        break;
                    default:
                        return; // Return without using event.
                }
                Invalidate(false);
                Event.current.Use();
            }
        }

        private void DrawCurrentTexture()
        {
            Rect rect = new Rect(0, 0, m_Rect.width, m_Rect.height);

            // Pan : use Middle Mouse button or Alt+Click
            if(Event.current.type == EventType.MouseDown && (Event.current.button == 2 || (Event.current.button == 0 && Event.current.alt)))
            {
                m_DragPreview = true;
            }

            if((Event.current.rawType == EventType.MouseUp || Event.current.rawType == EventType.DragExited) && (Event.current.button == 2 || Event.current.button == 0))
            {
                m_DragPreview = false;
                Invalidate(false);
            }

            if((!m_DragPreview && Event.current.alt) || m_DragPreview)
            {
               EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);
               Invalidate(false);
            }

            if(m_DragPreview && Event.current.type == EventType.MouseDrag)
            {
                m_CameraPosition -= Event.current.delta;
                Invalidate(false);
            }

            // Zoom : using MouseWheel
            if (Event.current.type == EventType.ScrollWheel && rect.Contains(Event.current.mousePosition) )
            {
                // Delta negative when zooming In, Positive when zooming out
                Zoom(Event.current.delta.y * 0.05f, Event.current.mousePosition);
                Invalidate(false);
            }


            // Zoom : using Alt + RightClick
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1  && Event.current.alt)
            {
                m_ZoomPreview = true;
                m_ZoomPreviewCenter = Event.current.mousePosition;
                m_PreviousMousePosition = m_ZoomPreviewCenter;
            }

            if (Event.current.rawType == EventType.MouseUp && Event.current.button == 1)
            {
                m_ZoomPreview = false;
            }

            if(m_ZoomPreview)
            {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Zoom);
                Vector2 mouseDelta = Event.current.mousePosition - m_PreviousMousePosition;
                Zoom((mouseDelta.x + mouseDelta.y) * -0.002f, m_ZoomPreviewCenter);
                Invalidate(false);
                m_PreviousMousePosition = Event.current.mousePosition;
            }

            // Draw Texture
            if(Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture
                    (
                        new Rect(
                        (rect.width/2) - m_CameraPosition.x - (texture.width*m_Zoom*0.5f),
                        (rect.height/2) - m_CameraPosition.y - (texture.height*m_Zoom*0.5f),
                        texture.width*m_Zoom,
                        texture.height*m_Zoom
                        ),
                    m_RenderTexture,
                    ScaleMode.ScaleToFit
                    );
            }

        }

        public Vector2 CanvasToScreen(Vector2 Position)
        {
            return new Vector2(
                (m_Rect.width / 2) - m_CameraPosition.x - (Position.x * m_Zoom),
                (m_Rect.height / 2) - m_CameraPosition.y - (Position.y * m_Zoom)
                );
        }

        protected virtual void DrawGrid()
        {
            Vector2 src, dst;
            if(BackgroundBrightness < 0.5f)
                Handles.color = new Color(1.0f,1.0f,1.0f,0.33333f);
            else
                Handles.color = new Color(0.0f,0.0f,0.0f,0.66666f);

            src = CanvasToScreen(new Vector2(-texture.width/2, -texture.height/2));
            dst = CanvasToScreen(new Vector2(texture.width/2, texture.height/2));

            Handles.DrawLine(new Vector2(src.x,src.y), new Vector2(dst.x,src.y));
            Handles.DrawLine(new Vector2(dst.x,src.y), new Vector2(dst.x,dst.y));
            Handles.DrawLine(new Vector2(dst.x,dst.y), new Vector2(src.x,dst.y));
            Handles.DrawLine(new Vector2(src.x,dst.y), new Vector2(src.x,src.y));

            Handles.color = Color.white;
        }

        private void BlitIntoRenderTarget()
        {
            // Backup GUI RenderTarget
            var oldrendertarget = RenderTexture.active;

            Graphics.Blit(texture, m_RenderTexture, m_Material);

            // Restore GUI RenderTarget
            RenderTexture.active = oldrendertarget;
        }
       
        public virtual void OnGUI()
        {
            
            if(m_bgBrightness < 0.0f)
            {
                ResetBrightness();
            }
            
            // Focus taken when clicked in viewport
            if (Event.current.type == EventType.MouseDown && m_Rect.Contains(Event.current.mousePosition))
            {
                GUI.FocusControl("");
            }

            if(texture != null && Event.current.type == EventType.Repaint)
            {
                if (m_IsDirtyRenderTarget)
                    UpdateRenderTarget();

                if(m_bNeedRedraw)
                {
                    BlitIntoRenderTarget();
                    m_bNeedRedraw = false;
                }
            }
            
            GUI.BeginGroup(m_Rect);
            Rect LocalRect = new Rect(Vector2.zero, m_Rect.size);

#if !UNITY_2018_2_OR_NEWER
            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
#endif
            GUI.DrawTextureWithTexCoords(LocalRect, BackgroundTexture, new Rect(0, 0, m_Rect.width / 64, m_Rect.height / 64));

#if !UNITY_2018_2_OR_NEWER
            //GL.sRGBWrite = false;
#endif
            if (texture != null)
            {
                HandleKeyboardEvents();

#if !UNITY_2018_2_OR_NEWER
            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
#endif
                DrawCurrentTexture();
#if !UNITY_2018_2_OR_NEWER
            //GL.sRGBWrite = false;
#endif

                if (showGrid) DrawGrid();
                if (onCanvasGUI != null)
                    onCanvasGUI();
            }
            else
                GUI.Label(LocalRect, VFXToolboxGUIUtility.Get("No Texture"), EditorStyles.centeredGreyMiniLabel);

#if !UNITY_2018_2_OR_NEWER
            //GL.sRGBWrite = false;
#endif

            GUI.EndGroup();
        }

#region BRIGHTNESS CONTROLS

        public Texture2D BackgroundTexture { get { return m_BackgroundTexture; } }
        private Texture2D m_BackgroundTexture;

        public void SetBGBrightness(float value)
        {
            m_bgBrightness = value;
            m_BackgroundTexture = Styles.GetBGTexture(value);
        }

        public void ResetBrightness()
        {
            if (EditorGUIUtility.isProSkin)
                BackgroundBrightness = 0.2f;
            else
                BackgroundBrightness = 0.4f;
        }

        public void BrightnessUp(float value)
        {
            BackgroundBrightness = Mathf.Min(BackgroundBrightness + value,1.0f);
        }

        public void BrightnessDown(float value)
        {
            BackgroundBrightness = Mathf.Max(0.0f, BackgroundBrightness - value);
        }

#endregion

#region STYLES



        public class Styles
        {
            public GUIStyle miniLabel
            {
                get { return m_Canvas.BackgroundBrightness < 0.5f ? m_ViewportMiniLabel : m_ViewportMiniLabelDark;  } 
            }

            public GUIStyle miniLabelRight
            {
                get { return m_Canvas.BackgroundBrightness < 0.5f ? m_ViewportMiniLabelRight : m_ViewportMiniLabelRightDark;  } 
            }

            public GUIStyle miniLabelCenter
            {
                get { return m_Canvas.BackgroundBrightness < 0.5f ? m_ViewportMiniLabelCenter : m_ViewportMiniLabelCenterDark;  } 
            }

            public GUIStyle label
            {
                get { return m_Canvas.BackgroundBrightness < 0.5f ? m_ViewportLabel : m_ViewportLabelDark;  } 
            }

            public GUIStyle largeLabel
            {
                get { return m_Canvas.BackgroundBrightness < 0.5f ? m_ViewportLargeLabel : m_ViewportLargeLabelDark;  } 
            }

            public Color backgroundPanelColor
            {
                get { return m_Canvas.BackgroundBrightness < 0.5f ? m_BackgroundPanelColor : m_BackgroundPanelColorDark; }
            }

            public Color red { get { return m_Canvas.BackgroundBrightness < 0.5f ? new Color(1, 0, 0, 1) : new Color(0.7f, 0, 0, 1); } }
            public Color green { get { return m_Canvas.BackgroundBrightness < 0.5f ? new Color(0, 1, 0, 1) : new Color(0, 0.5f, 0, 1); } }
            public Color blue { get { return m_Canvas.BackgroundBrightness < 0.5f ? new Color(0, 0, 1, 1) : new Color(0, 0, 0.5f, 1); } }
            public Color white { get { return m_Canvas.BackgroundBrightness < 0.5f ? new Color(1, 1, 1, 1) : new Color(0, 0, 0, 1); } }
            public Color black { get { return m_Canvas.BackgroundBrightness < 0.5f ? new Color(0, 0, 0, 1) : new Color(1, 1, 1, 1); } }
            public Color yellow { get { return m_Canvas.BackgroundBrightness < 0.5f ? new Color(1.0f, 0.8f, 0.25f) : new Color(0.5f, 0.4f, 0.1f); } }
            public Color cyan { get { return m_Canvas.BackgroundBrightness < 0.5f ? new Color(0.25f, 0.8f, 1.0f) : new Color(0.1f, 0.4f, 0.5f); } }
            public Color fadewhite  { get { return m_Canvas.BackgroundBrightness < 0.5f ? new Color(1, 1, 1, 0.25f) : new Color(0, 0, 0, 0.25f); } }

            private GUIStyle m_ViewportMiniLabel;
            private GUIStyle m_ViewportMiniLabelDark;
            private GUIStyle m_ViewportMiniLabelRight;
            private GUIStyle m_ViewportMiniLabelRightDark;
            private GUIStyle m_ViewportMiniLabelCenter;
            private GUIStyle m_ViewportMiniLabelCenterDark;
            private GUIStyle m_ViewportLabel;
            private GUIStyle m_ViewportLabelDark;
            private GUIStyle m_ViewportLargeLabel;
            private GUIStyle m_ViewportLargeLabelDark;

            private VFXToolboxCanvas m_Canvas;

            private Color m_BackgroundPanelColor;
            private Color m_BackgroundPanelColorDark;

            public Styles(VFXToolboxCanvas canvas)
            {
                m_Canvas = canvas;

                m_ViewportMiniLabel = new GUIStyle(EditorStyles.miniLabel);
                m_ViewportMiniLabel.normal.textColor = Color.white;
                m_ViewportMiniLabelDark = new GUIStyle(EditorStyles.miniLabel);
                m_ViewportMiniLabelDark.normal.textColor = Color.black;

                m_ViewportMiniLabelRight = new GUIStyle(m_ViewportMiniLabel);
                m_ViewportMiniLabelRight.alignment = TextAnchor.MiddleRight;
                m_ViewportMiniLabelRightDark = new GUIStyle(m_ViewportMiniLabelDark);
                m_ViewportMiniLabelRightDark.alignment = TextAnchor.MiddleRight;

                m_ViewportMiniLabelCenter = new GUIStyle(m_ViewportMiniLabel);
                m_ViewportMiniLabelCenter.alignment = TextAnchor.MiddleCenter;
                m_ViewportMiniLabelCenterDark = new GUIStyle(m_ViewportMiniLabelDark);
                m_ViewportMiniLabelCenterDark.alignment = TextAnchor.MiddleCenter;

                m_ViewportLabel = new GUIStyle(EditorStyles.largeLabel);
                m_ViewportLabel.normal.textColor = Color.white;

                m_ViewportLabelDark = new GUIStyle(EditorStyles.largeLabel);
                m_ViewportLabelDark.normal.textColor = Color.black;

                m_ViewportLargeLabel = new GUIStyle(EditorStyles.largeLabel);
                m_ViewportLargeLabel.fontSize = 24;
                m_ViewportLargeLabel.normal.textColor = Color.white;

                m_ViewportLargeLabelDark = new GUIStyle(EditorStyles.largeLabel);
                m_ViewportLargeLabelDark.fontSize = 24;
                m_ViewportLargeLabelDark.normal.textColor = Color.black;

                m_BackgroundPanelColor = new Color(0.02f, 0.02f, 0.02f, 0.85f);
                m_BackgroundPanelColorDark = new Color(0.25f, 0.25f, 0.25f, 0.85f);

            }

            public static Texture2D GetBGTexture(float brightness)
            {
                Texture2D out_tex = new Texture2D(2, 2) { hideFlags = HideFlags.DontSave };
                Color[] bgcolors = new Color[4];
                brightness *= 0.95f;
                bgcolors[0] = new Color(brightness+0.05f, brightness+0.05f, brightness+0.05f);
                bgcolors[1] = new Color(brightness, brightness, brightness);
                bgcolors[2] = new Color(brightness, brightness, brightness);
                bgcolors[3] = new Color(brightness+0.05f, brightness+0.05f, brightness+0.05f);
                out_tex.SetPixels(bgcolors);
                out_tex.wrapMode = TextureWrapMode.Repeat;
                out_tex.filterMode = FilterMode.Point;
                out_tex.Apply();
                return out_tex;
            }


        }
#endregion

    }
}
