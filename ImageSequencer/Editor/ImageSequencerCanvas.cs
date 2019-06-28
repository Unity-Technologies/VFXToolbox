using UnityEngine;
using System.Linq;
using System;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    internal class ImageSequencerCanvas : VFXToolboxCanvas
    {

        public bool showExtraInfo
        {
            get
            {
                return m_bShowExtraInfo;
            }
            set
            {
                m_bShowExtraInfo = value;
            }
        }

        public int numFrames
        {
            get
            {
                return m_PreviewSequence.length;
            }
        }
       
        public ProcessingFrameSequence sequence
        {
            get
            {
                return m_PreviewSequence;
            }
            set
            {
                m_PreviewSequence = value;
            }
        }

        public ProcessingFrame currentFrame
        {
            get {
                return m_ProcessingFrame;
            }
            set {
                m_ProcessingFrame = value;
                if(value != null)
                    InvalidateRenderTarget();
            }
        }

        public int currentFrameIndex
        {
            get
            {
                return m_CurrentFrame;
            }
            set
            {
                if (m_CurrentFrame != value)
                {
                    m_CurrentFrame = value;
                    UpdateCanvasSequence();
                }
            } 
        }

        public bool isPlaying
        {
            get
            {
                return m_IsPlaying;
            }
        }

        private bool m_bShowExtraInfo = false;

        private int m_CurrentFrame = 0;

        private ProcessingFrameSequence m_PreviewSequence;
        private ProcessingFrame m_ProcessingFrame;
        private Rect m_PlayControlsRect;
        private ImageSequencer m_ImageSequencerWindow;

        private bool m_IsPlaying = false;
        private float m_PlayFramerate = 30.0f;
        private float m_PlayTime = 0.0f;
        private double m_EditorTime;

        private bool m_IsScrobbing;

        public ImageSequencerCanvas(Rect displayRect, ImageSequencer editorWindow)
            :base(displayRect)
        {
            m_ImageSequencerWindow = editorWindow;

            m_IsScrobbing = false;
            m_PlayControlsRect = new Rect(16, 16, 420, 26);
        }

        public override void Invalidate(bool needRedraw)
        {
            base.Invalidate(needRedraw);
            m_ImageSequencerWindow.Invalidate();
        }

        protected override void SetTexture(Texture tex)
        {
            // Never should Happen
            throw new NotImplementedException();
        }

        protected override Texture GetTexture()
        {
            if (currentFrame != null)
            {
                return currentFrame.texture;
            }
            else
                return null;
        }

        protected override int GetMipMapCount()
        {
            if (currentFrame != null)
                return currentFrame.mipmapCount;
            else
                return 0;
        }

        protected override void HandleKeyboardEvents()
        {
            base.HandleKeyboardEvents();

            if(Event.current.type == EventType.KeyDown)
            {
                switch(Event.current.keyCode)
                {
                    // Viewport Toggles
                    case KeyCode.H:
                        showExtraInfo = !showExtraInfo;
                        break;
                    // Play Controls
                    case KeyCode.Space:
                        TogglePlaySequence();
                        break;
                    case KeyCode.LeftArrow:
                        if (Event.current.shift)
                            FirstFrame();
                        else
                            PreviousFrame();
                        break;
                    case KeyCode.RightArrow:
                        if (Event.current.shift)
                            LastFrame();
                        else
                            NextFrame();
                        break;
                    default:
                        return; // Return without using event.
                }
                Invalidate(false);
                Event.current.Use();
            }
        }

        protected override void DrawGrid()
        {
            int GridNumU = m_PreviewSequence.numU;
            int GridNumV =  m_PreviewSequence.numV;

            Vector2 src, dst;
            float v;
            Texture texture = currentFrame.texture;
            if(BackgroundBrightness < 0.5f)
                Handles.color = new Color(1.0f,1.0f,1.0f,0.33333f);
            else
                Handles.color = new Color(0.0f,0.0f,0.0f,0.66666f);

            for(int i = 0; i <= GridNumV; i++)
            {
                v = -(texture.height * 0.5f) + (float)i / GridNumV * texture.height;
                src = CanvasToScreen(new Vector2(-texture.width * 0.5f, v));
                dst = CanvasToScreen(new Vector2(texture.width * 0.5f, v));
                Handles.DrawLine(src, dst);
            }
            for(int j = 0; j <= GridNumU; j++)
            {
                v = -(texture.width * 0.5f) + (float)j / GridNumU * texture.width;
                src = CanvasToScreen(new Vector2(v,-texture.height * 0.5f));
                dst = CanvasToScreen(new Vector2(v, texture.height * 0.5f));
                
                Handles.DrawLine(src, dst);
            }
            Handles.color = Color.white;
        }

        public void OnGUI(ImageSequencer editor)
        {
            OnGUI();

            // Processor extra info
            GUI.BeginGroup(m_Rect);
            if (editor.currentProcessor != null && editor.currentProcessor.Enabled && m_bShowExtraInfo && editor.sidePanelViewMode == ImageSequencer.SidePanelMode.Processors)
                editor.currentProcessor.OnCanvasGUI(this);
            GUI.EndGroup();

            // Everytime text
            string procName = (editor.sidePanelViewMode == ImageSequencer.SidePanelMode.Export) ? "Export" : (sequence.processor == null ? "Input Frames" : sequence.processor.ToString());
            GUI.Label(new RectOffset(24,24,24,24).Remove(m_Rect), procName , styles.largeLabel);
            GUI.Label(new RectOffset(24,24,64,24).Remove(m_Rect), GetDebugInfoString() , styles.label);
            //EditorGUI.DrawRect(m_Rect, Color.red);

            // Play Controls
            if(sequence != null && sequence.length > 1)
            {
                DrawSequenceControls(displayRect, editor);
            }

            if (m_IsPlaying)
                UpdatePlay();
        }

        private GUIContent GetDebugInfoString()
        {
            string output = "";
            if(m_ProcessingFrame != null)
            {
                output += "Frame Size : " + m_ProcessingFrame.texture.width + " x " + m_ProcessingFrame.texture.height;
                output += "\nMipMaps : " + m_ProcessingFrame.mipmapCount;
            }
            return new GUIContent(output);

        }

        public void DrawSequenceControls(Rect ViewportArea, ImageSequencer editor)
        {
            m_PlayControlsRect = new Rect(ViewportArea.x , (ViewportArea.y + ViewportArea.height), ViewportArea.width , 100);

            using (new GUILayout.AreaScope(m_PlayControlsRect, GUIContent.none, ImageSequencer.styles.playbackControlWindow))
            {
                Rect area = new Rect(16,16,m_PlayControlsRect.width-32,m_PlayControlsRect.height-32);
                //GUILayout.BeginArea(area);
                using (new GUILayout.VerticalScope())
                {
                    // TRACKBAR
                    int count = sequence.length;

                    GUILayout.Space(16); // Reserve Layout for labels
                    Rect bar_rect = GUILayoutUtility.GetRect(area.width, 16);

                    EditorGUIUtility.AddCursorRect(bar_rect, MouseCursor.ResizeHorizontal);
                    if(Event.current.type == EventType.MouseDown && bar_rect.Contains(Event.current.mousePosition))
                    {
                        m_IsScrobbing = true;
                    }

                    if(Event.current.type == EventType.MouseUp || Event.current.rawType == EventType.MouseUp)
                    {
                        m_IsScrobbing = false;
                    }

                    if(m_IsScrobbing && (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown))
                    {
                        float pos = (Event.current.mousePosition.x - bar_rect.x) / bar_rect.width;
                        int frame = (int)Mathf.Round(pos * numFrames);
                        if (frame != currentFrameIndex)
                        {
                            currentFrameIndex = frame;
                            Invalidate(true);
                        }
                    }
                    
                    EditorGUI.DrawRect(bar_rect, ImageSequencer.styles.CookBarDirty);

                    float width = bar_rect.width / count;

                    Rect textpos;

                    for (int i = 0; i < count; i++)
                    {
                        if(!sequence.frames[i].dirty)
                        {
                            Rect cell = new Rect(bar_rect.x + i * width, bar_rect.y, width, bar_rect.height);
                            EditorGUI.DrawRect(cell, ImageSequencer.styles.CookBarCooked);
                        }

                        if(i == currentFrameIndex)
                        {
                            Rect cursor = new Rect(bar_rect.x + i * width, bar_rect.y, width, bar_rect.height);
                            EditorGUI.DrawRect(cursor, new Color(1.0f,1.0f,1.0f,0.5f));
                        }

                        // Labels : Every multiple of 10 based on homemade formula
                        int step = 10 * (int)Mathf.Max(1,Mathf.Floor(8*(float)count / bar_rect.width));

                        if( ((i+1) % step) == 0 )
                        {
                            textpos = new Rect(bar_rect.x + i * width, bar_rect.y - 16, 32, 16);
                            GUI.Label(textpos, (i+1).ToString(), EditorStyles.largeLabel);
                            Rect cursor = new Rect(bar_rect.x + i * width, bar_rect.y, 1, bar_rect.height);
                            EditorGUI.DrawRect(cursor, new Color(1.0f,1.0f,1.0f,0.2f));
                        }
                    }

                    // Labels : First 
                    textpos = new Rect(bar_rect.x, bar_rect.y - 16, 32, 16);
                    GUI.Label(textpos, VFXToolboxGUIUtility.Get("1"), EditorStyles.largeLabel);
                    GUILayout.Space(16);

                    // PLAY CONTROLS

                    bool lastplay;
                    using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
                    {
                        lastplay = m_IsPlaying;
                        if(GUILayout.Button(ImageSequencer.styles.iconFirst, VFXToolboxStyles.toolbarButton, GUILayout.Width(32)))
                        {
                            FirstFrame();
                        }

                        if(GUILayout.Button(ImageSequencer.styles.iconBack, VFXToolboxStyles.toolbarButton, GUILayout.Width(24)))
                        {
                            PreviousFrame();
                        }

                        bool playing = GUILayout.Toggle(m_IsPlaying,ImageSequencer.styles.iconPlay, VFXToolboxStyles.toolbarButton, GUILayout.Width(24));
                        if(m_IsPlaying != playing)
                        {
                            TogglePlaySequence();
                        }

                        if(GUILayout.Button(ImageSequencer.styles.iconForward, VFXToolboxStyles.toolbarButton, GUILayout.Width(24)))
                        {
                            NextFrame();
                        }

                        if(GUILayout.Button(ImageSequencer.styles.iconLast, VFXToolboxStyles.toolbarButton, GUILayout.Width(32)))
                        {
                            LastFrame();
                        }

                        if (lastplay != m_IsPlaying)
                        {
                            m_EditorTime = EditorApplication.timeSinceStartup;
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(VFXToolboxGUIUtility.GetTextAndIcon("Frame : ","Profiler.Record"), VFXToolboxStyles.toolbarButton);
                        m_CurrentFrame = Mathf.Clamp(EditorGUILayout.IntField(m_CurrentFrame+1, VFXToolboxStyles.toolbarTextField, GUILayout.Width(42))-1,0,numFrames-1);
                        GUILayout.Label(" on " + numFrames + " ( TCR : " + GetTCR(m_CurrentFrame, (int)m_PlayFramerate) + " ) " , VFXToolboxStyles.toolbarButton);
                        GUILayout.FlexibleSpace();
                        ShowFrameratePopup();
                    }
                }
                //GUILayout.EndArea();
            }
        }

        public void UpdateCanvasSequence()
        {
            int length;
            if (sequence.processor != null)
                length = sequence.processor.GetProcessorSequenceLength();
            else
                length = sequence.length;

            if (length > 0)
            {
                currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, length - 1);
                currentFrame = sequence.RequestFrame(currentFrameIndex);
            }
            else
                currentFrame = null;
        }

        #region PLAY CONTROLS

        private void TogglePlaySequence()
        {
            if(m_IsPlaying)
                StopSequence();
            else
                PlaySequence();
        }

        private void PlaySequence()
        {
            m_IsPlaying = true;
            m_PlayTime = currentFrameIndex / m_PlayFramerate;
            m_EditorTime = EditorApplication.timeSinceStartup;
        }

        private void StopSequence()
        {
            m_IsPlaying = false;
        }

        private void NextFrame()
        {
            int frame = currentFrameIndex + 1;
            if (frame >= numFrames) frame = 0;
            currentFrameIndex = frame;
        }

        private void PreviousFrame()
        {
            int frame = currentFrameIndex - 1;
            if (frame < 0) frame = numFrames - 1;
            currentFrameIndex = frame;
        }

        private void FirstFrame()
        {
            currentFrameIndex = 0;
            m_PlayTime = 0;
        }

        private void LastFrame()
        {
            currentFrameIndex = numFrames - 1;
            m_PlayTime =  (numFrames - 1) / m_PlayFramerate ;
        }

        private string GetTCR(int frame, int framerate)
        {
            int frames = frame % framerate;
            int seconds = frame / framerate;
            int minutes = seconds / 60;
            seconds %= 60;
            minutes %= 60;
            int numbering = (int)Mathf.Max(2,Mathf.Floor(Mathf.Log10(framerate))+1); // Minimum 2 digits
            return minutes.ToString("D2") + ":" + seconds.ToString("D2") + ":" + frames.ToString("D"+numbering);
        }

        private void ShowFrameratePopup()
        {
            if(GUILayout.Button(VFXToolboxGUIUtility.GetTextAndIcon("Speed","SpeedScale"),EditorStyles.toolbarDropDown))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(VFXToolboxGUIUtility.Get("5 fps"),false, () =>{ m_PlayFramerate = 5; });
                menu.AddItem(VFXToolboxGUIUtility.Get("10 fps"),false, () =>{ m_PlayFramerate = 10; });
                menu.AddItem(VFXToolboxGUIUtility.Get("15 fps"),false, () =>{ m_PlayFramerate = 15; });
                menu.AddItem(VFXToolboxGUIUtility.Get("20 fps"),false, () =>{ m_PlayFramerate = 20; });
                menu.AddItem(VFXToolboxGUIUtility.Get("24 fps (Cine)"),false, () =>{ m_PlayFramerate = 24; });
                menu.AddItem(VFXToolboxGUIUtility.Get("25 fps (PAL)"),false, () =>{ m_PlayFramerate = 25; });
                menu.AddItem(VFXToolboxGUIUtility.Get("29.97 fps (NTSC)"),false, () =>{ m_PlayFramerate = 29.97f; });
                menu.AddItem(VFXToolboxGUIUtility.Get("30 fps"),false, () =>{ m_PlayFramerate = 30; });
                menu.AddItem(VFXToolboxGUIUtility.Get("50 fps"),false, () =>{ m_PlayFramerate = 50; });
                menu.AddItem(VFXToolboxGUIUtility.Get("60 fps"),false, () =>{ m_PlayFramerate = 60; });
                menu.ShowAsContext();
            }
            m_PlayFramerate = EditorGUILayout.FloatField(m_PlayFramerate, VFXToolboxStyles.toolbarTextField,GUILayout.Width(24));
            EditorGUILayout.LabelField(VFXToolboxGUIUtility.Get("fps"),GUILayout.Width(24));
        }

        private void UpdatePlay()
        {
            double deltaTime = EditorApplication.timeSinceStartup - m_EditorTime;
            m_PlayTime += (float)deltaTime;
            m_PlayTime = m_PlayTime % ((1.0f / m_PlayFramerate) * numFrames);
            currentFrameIndex = (int)Mathf.Floor(m_PlayTime*m_PlayFramerate);
            m_EditorTime = EditorApplication.timeSinceStartup;
        }

        #endregion

        #region STYLES

        public new class Styles
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

            private ImageSequencerCanvas m_Canvas;

            private Color m_BackgroundPanelColor;
            private Color m_BackgroundPanelColorDark;

            public Styles(ImageSequencerCanvas canvas)
            {
                m_Canvas = canvas;

                Color lightGray = new Color(0.8f, 0.8f, 0.8f, 1.0f);
                Color darkGray = new Color(0.2f, 0.2f, 0.2f, 1.0f);

                m_ViewportMiniLabel = new GUIStyle(EditorStyles.miniLabel);
                m_ViewportMiniLabel.normal.textColor = lightGray;
                m_ViewportMiniLabelDark = new GUIStyle(EditorStyles.miniLabel);
                m_ViewportMiniLabelDark.normal.textColor = darkGray;

                m_ViewportMiniLabelRight = new GUIStyle(m_ViewportMiniLabel);
                m_ViewportMiniLabelRight.alignment = TextAnchor.MiddleRight;
                m_ViewportMiniLabelRightDark = new GUIStyle(m_ViewportMiniLabelDark);
                m_ViewportMiniLabelRightDark.alignment = TextAnchor.MiddleRight;

                m_ViewportMiniLabelCenter = new GUIStyle(m_ViewportMiniLabel);
                m_ViewportMiniLabelCenter.alignment = TextAnchor.MiddleCenter;
                m_ViewportMiniLabelCenterDark = new GUIStyle(m_ViewportMiniLabelDark);
                m_ViewportMiniLabelCenterDark.alignment = TextAnchor.MiddleCenter;

                m_ViewportLabel = new GUIStyle(EditorStyles.largeLabel);
                m_ViewportLabel.normal.textColor = lightGray;

                m_ViewportLabelDark = new GUIStyle(EditorStyles.largeLabel);
                m_ViewportLabelDark.normal.textColor = darkGray;

                m_ViewportLargeLabel = new GUIStyle(EditorStyles.largeLabel);
                m_ViewportLargeLabel.fontSize = 24;
                m_ViewportLargeLabel.normal.textColor = lightGray;

                m_ViewportLargeLabelDark = new GUIStyle(EditorStyles.largeLabel);
                m_ViewportLargeLabelDark.fontSize = 24;
                m_ViewportLargeLabelDark.normal.textColor = darkGray;

                m_BackgroundPanelColor = new Color(0.02f, 0.02f, 0.02f, 0.85f);
                m_BackgroundPanelColorDark = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
        #endregion

    }
}
