using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.VFXToolbox;

namespace UnityEditor.VFXToolbox.Workbench
{
    public abstract class WorkbenchCanvasBase
    {
        protected Workbench m_Window;

        public WorkbenchCanvasBase(Workbench window)
        {
            m_Window = window;
        }

        public abstract void Invalidate(bool needRedraw);

        public abstract void OnGUI(Rect displayRect, WorkbenchToolBase currentTool);

        public abstract void OnToolbarGUI();

    }
}

