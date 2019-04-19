using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    internal partial class ImageSequencer : EditorWindow
    {
        static Styles s_Styles = null;
        public static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }

        public class Styles
        {
            public GUIStyle scrollView;

            public GUIStyle playbackControlWindow;

            public GUIContent title;

            public readonly GUIContent iconPlay = EditorGUIUtility.IconContent("Animation.Play", "Play the sequence"); 
            public readonly GUIContent iconBack = EditorGUIUtility.IconContent("Animation.PrevKey", "Go back one Frame");
            public readonly GUIContent iconForward = EditorGUIUtility.IconContent("Animation.NextKey", "Advance one Frame");
            public readonly GUIContent iconFirst = EditorGUIUtility.IconContent("Animation.FirstKey", "Go to first Frame");
            public readonly GUIContent iconLast = EditorGUIUtility.IconContent("Animation.LastKey", "Go to last Frame");

            public readonly GUIContent iconRGB = EditorGUIUtility.IconContent("PreTextureRGB", "Toggle RGB/Alpha only");
            public readonly GUIContent iconMipMapUp = EditorGUIUtility.IconContent("PreTextureMipMapLow", "Go one MipMap up (smaller size)");
            public readonly GUIContent iconMipMapDown = EditorGUIUtility.IconContent("PreTextureMipMapHigh", "Go one MipMap down (higher size)");

            public Color CookBarDirty { get { if(EditorGUIUtility.isProSkin) return m_CookBarDirtyPro; else return m_CookBarDirty; } }
            public Color CookBarCooked { get { if(EditorGUIUtility.isProSkin) return m_CookBarCookedPro; else return m_CookBarCooked; } }

            private Color m_CookBarDirty;
            private Color m_CookBarDirtyPro;
            private Color m_CookBarCooked;
            private Color m_CookBarCookedPro;

            public GUIStyle MaskRToggle { get { if (EditorGUIUtility.isProSkin) return m_MaskRTogglePro; else return m_MaskRToggle; } }
            public GUIStyle MaskGToggle { get { if (EditorGUIUtility.isProSkin) return m_MaskGTogglePro; else return m_MaskGToggle; } }
            public GUIStyle MaskBToggle { get { if (EditorGUIUtility.isProSkin) return m_MaskBTogglePro; else return m_MaskBToggle; } }
            public GUIStyle MaskAToggle { get { if (EditorGUIUtility.isProSkin) return m_MaskATogglePro; else return m_MaskAToggle; } }

            private GUIStyle m_MaskRToggle;
            private GUIStyle m_MaskRTogglePro;
            private GUIStyle m_MaskGToggle;
            private GUIStyle m_MaskGTogglePro;
            private GUIStyle m_MaskBToggle;
            private GUIStyle m_MaskBTogglePro;
            private GUIStyle m_MaskAToggle;
            private GUIStyle m_MaskATogglePro;


            public GUIStyle LockToggle { get { if (EditorGUIUtility.isProSkin) return m_LockTogglePro; else return m_LockToggle; } }
            private GUIStyle m_LockToggle;
            private GUIStyle m_LockTogglePro;

            public Styles()
            {
                title = new GUIContent("Image Sequencer");

                scrollView = new GUIStyle();
                scrollView.padding = new RectOffset(8, 8, 0, 0);

                playbackControlWindow = new GUIStyle(EditorStyles.toolbar);
                playbackControlWindow.border = new RectOffset(4, 4, 4, 4);
                playbackControlWindow.padding = new RectOffset(16, 16, 16, 16);
                playbackControlWindow.stretchHeight = true;
                playbackControlWindow.fixedHeight = 0;
                playbackControlWindow.contentOffset = new Vector2();

                m_CookBarCooked = new Color(0.25f,0.6f,1.0f,1.0f);
                m_CookBarCookedPro = new Color(0.25f,0.4f,0.65f,1.0f);
                m_CookBarDirty = new Color(1.0f,1.0f,1.0f,0.5f);
                m_CookBarDirtyPro = new Color(0.5f,0.5f,0.5f,0.5f);

                m_MaskRToggle = new GUIStyle(EditorStyles.toolbarButton);
                m_MaskGToggle = new GUIStyle(EditorStyles.toolbarButton);
                m_MaskBToggle= new GUIStyle(EditorStyles.toolbarButton);
                m_MaskAToggle= new GUIStyle(EditorStyles.toolbarButton);

                m_MaskRToggle.onNormal.textColor = new Color(1.0f, 0.0f, 0.0f);
                m_MaskGToggle.onNormal.textColor = new Color(0.0f, 0.6f, 0.2f);
                m_MaskBToggle.onNormal.textColor = new Color(0.0f, 0.2f, 1.0f);
                m_MaskAToggle.onNormal.textColor = new Color(0.5f, 0.5f, 0.5f);


                m_MaskRTogglePro = new GUIStyle(EditorStyles.toolbarButton);
                m_MaskGTogglePro= new GUIStyle(EditorStyles.toolbarButton);
                m_MaskBTogglePro= new GUIStyle(EditorStyles.toolbarButton);
                m_MaskATogglePro= new GUIStyle(EditorStyles.toolbarButton);

                m_MaskRTogglePro.onNormal.textColor = new Color(2.0f, 0.3f, 0.3f);
                m_MaskGTogglePro.onNormal.textColor = new Color(0.5f, 2.0f, 0.1f);
                m_MaskBTogglePro.onNormal.textColor = new Color(0.2f, 0.6f, 2.0f);
                m_MaskATogglePro.onNormal.textColor = new Color(2.0f, 2.0f, 2.0f);

                m_LockToggle = new GUIStyle("IN LockButton");
                m_LockTogglePro = new GUIStyle("IN LockButton");
            }
        }
    }
}
