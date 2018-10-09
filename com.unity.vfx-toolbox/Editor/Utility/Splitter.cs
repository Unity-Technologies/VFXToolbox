using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace UnityEditor.VFXToolbox
{
    internal class Splitter
    {
        public enum SplitLockMode
        {
            None = 0,
            BothMinSize = 1,
            LeftMinMax = 2,
            RightMinMax = 3
        }

        public float value
        {
            get { return m_SplitterValue; }
            set { SetSplitterValue(value); }
        }

        public delegate void SplitViewOnGUIDelegate(Rect drawRect);

        private SplitViewOnGUIDelegate m_onDrawLeftDelegate;
        private SplitViewOnGUIDelegate m_onDrawRightDelegate;

        private float m_SplitterValue;
        private bool m_Resize;
        private SplitLockMode m_LockMode;
        private Vector2 m_LockValues;

        public Splitter(float initialLeftWidth, SplitViewOnGUIDelegate onDrawLeftDelegate, SplitViewOnGUIDelegate onDrawRightDelegate, SplitLockMode lockMode, Vector2 lockValues)
        {
            m_SplitterValue = initialLeftWidth;
            m_onDrawLeftDelegate = onDrawLeftDelegate;
            m_onDrawRightDelegate = onDrawRightDelegate;
            m_LockMode = lockMode;

            if (((int)lockMode > 1) && (lockValues.y < lockValues.x))
                m_LockValues = new Vector2(lockValues.y, lockValues.x);
            else
                m_LockValues = lockValues;

        }

        public bool DoSplitter(Rect rect)
        {
            if(m_onDrawLeftDelegate != null)
            {
                m_onDrawLeftDelegate(new Rect(rect.x, rect.y, m_SplitterValue, rect.height));
            }

            if(m_onDrawRightDelegate != null)
            {
                m_onDrawRightDelegate(new Rect(rect.x + m_SplitterValue, rect.y, rect.width - m_SplitterValue, rect.height));
            }

            HandlePanelResize(rect);

            return m_Resize;
        }

        private void SetSplitterValue(float Value)
        {
            m_SplitterValue = Value;
        }

        private void HandlePanelResize(Rect rect)
        {
            Rect resizeActiveArea = new Rect(rect.x + m_SplitterValue - 8, rect.y, 16, rect.height);

            EditorGUIUtility.AddCursorRect(resizeActiveArea, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown && resizeActiveArea.Contains(Event.current.mousePosition))
                m_Resize = true;

            if (m_Resize)
            {
                value = Event.current.mousePosition.x;
            }

            switch(m_LockMode)
            {
                case SplitLockMode.BothMinSize:
                    m_SplitterValue = Mathf.Clamp(m_SplitterValue, m_LockValues.x, rect.width - m_LockValues.y);
                    break;
                case SplitLockMode.LeftMinMax:
                    m_SplitterValue = Mathf.Clamp(m_SplitterValue, m_LockValues.x, m_LockValues.y);
                    break;
                case SplitLockMode.RightMinMax:
                    m_SplitterValue = Mathf.Clamp(m_SplitterValue, rect.width - m_LockValues.y, rect.width - m_LockValues.x);
                    break;
                default:
                    break;
            }

            RectOffset o = new RectOffset(7, 8, 0, 0);
            EditorGUI.DrawRect(o.Remove(resizeActiveArea), new Color(0,0,0,1.0f));
            if (Event.current.type == EventType.MouseUp)
                m_Resize = false;
        }



    }
}
