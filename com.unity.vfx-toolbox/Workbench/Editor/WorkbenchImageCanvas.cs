using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.VFXToolbox;

namespace UnityEditor.VFXToolbox.Workbench
{
    public class WorkbenchImageCanvas : VFXToolboxCanvas
    {
        private Texture m_Texture;
        private Workbench m_Editor;

        public WorkbenchImageCanvas(Rect displayRect, Workbench editorWindow)
            : base (displayRect)
        {
            m_Editor = editorWindow;
        }

        public override void Invalidate(bool needRedraw)
        {
            base.Invalidate(needRedraw);
            m_Editor.Invalidate();
        }

        protected override Texture GetTexture()
        {
            return m_Texture;
        }

        protected override void SetTexture(Texture tex)
        {
            m_Texture = tex;
            Invalidate(true);
        }

        public override void OnGUI()
        {
            texture = null;
            base.OnGUI();
        }

        public void OnGUI(WorkbenchToolBase currentTool)
        {
            base.OnGUI();

            if (currentTool != null)
            {
                using (new GUI.ClipScope(displayRect))
                {
                    if (currentTool.OnCanvasGUI(this))
                        Invalidate(false);
                }
            }
        }
    }
}
