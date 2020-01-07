using UnityEngine;
using UnityEngine.VFXToolbox;
using System.Collections.Generic;

namespace UnityEditor.VFXToolbox
{
    internal class CurveDrawer
    {
        private string m_CurveEditName;
        private int m_WidgetDefaultHeight;
        private bool m_WidgetShowToolbar;
        private readonly RectOffset m_CurvePadding = new RectOffset(40, 16, 16, 16);
        private CurveEditor m_Editor;
        private Dictionary<string, SerializedProperty> m_Curves;

        public delegate void CurveDrawEventDelegate(Rect renderArea, Rect curveArea);
        public CurveDrawEventDelegate OnPostGUI;

        private float lineBrightnessValue { get { return EditorGUIUtility.isProSkin ? 1.0f : 0.0f; } }

        public CurveDrawer(string curveEditName, float minInput, float maxInput, float minOutput, float maxOutput, int height, bool showToolbar)
            :this(curveEditName, minInput, maxInput, minOutput, maxOutput)
        {
            m_WidgetDefaultHeight = height;
            m_WidgetShowToolbar = showToolbar;
        }

        public CurveDrawer(string curveEditName, float minInput, float maxInput, float minOutput, float maxOutput)
        {
            var settings = CurveEditor.Settings.defaultSettings;
            settings.bounds = new Rect(minInput, minOutput, maxInput - minInput, maxOutput - minOutput);
            settings.padding = m_CurvePadding;
            m_Editor = new CurveEditor(settings);
            m_CurveEditName = curveEditName;
            m_WidgetDefaultHeight = 240;
            m_WidgetShowToolbar = true;
            m_Curves = new Dictionary<string, SerializedProperty>();
        }

        public void SetBounds(Rect bounds)
        {
            m_Editor.SetBounds(bounds);
        }

        public void ClearSelection()
        {
            m_Editor.ClearSelection();
        }

        public void AddCurve(SerializedProperty curveProperty, Color curveColor, string name, bool visible = true)
        {
            if (m_Curves.ContainsKey(name))
                return;

            var state = CurveEditor.CurveState.defaultState;
            state.color = curveColor;
            state.minPointCount = 2;

            m_Curves.Add(name, curveProperty);
            m_Editor.Add(curveProperty, state);
        }

        public void RemoveCurve(string name)
        {
            if (!m_Curves.ContainsKey(name))
                return;
            m_Editor.Remove(m_Curves[name]);
        }

        public void Clear()
        {
            m_Curves.Clear();
            m_Editor.RemoveAll();
        }

        public bool OnGUI(Rect drawRect)
        {
            return m_Editor.OnGUI(drawRect);
        }

        public bool OnGUILayout()
        {
            return OnGUILayout(m_WidgetDefaultHeight, m_WidgetShowToolbar);
        }

        public bool OnGUILayout(bool showToolbar)
        {
            return OnGUILayout(m_WidgetDefaultHeight, showToolbar);
        }

        public bool OnGUILayout(float height, bool showToolbar)
        {
            bool dirty = false;
            using (new GUILayout.VerticalScope())
            {
                // Header
                if(m_CurveEditName != null || m_CurveEditName == "")
                    GUILayout.Label(m_CurveEditName);

                GUILayout.Space(4.0f);

                // Curve Area
                if(showToolbar)
                {
                    using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
                    {
                        foreach(KeyValuePair<string,SerializedProperty> kvp in m_Curves)
                        {
                            string name = kvp.Key;
                            SerializedProperty curve = kvp.Value;
                            CurveEditor.CurveState state = m_Editor.GetCurveState(curve);
                            bool b = GUILayout.Toggle(state.visible, name, EditorStyles.toolbarButton);
                            if (b != state.visible)
                            {
                                state.visible = b;
                                m_Editor.SetCurveState(curve, state);
                            }
                        }
                        GUILayout.FlexibleSpace();
                    }
                }

                Rect lastRect = GUILayoutUtility.GetLastRect();
                Rect curveArea = GUILayoutUtility.GetRect(lastRect.width, height);

                // Selection
                CurveEditor.Selection selection = m_Editor.GetSelection();
                if(selection.curve != null && selection.keyframe != null)
                {
                    EditorGUI.indentLevel ++;
                    var key = selection.keyframe.Value;
                    Rect range = m_Editor.settings.bounds;
                    float t = EditorGUILayout.Slider("Time", key.time, range.xMin, range.xMax);
                    float v = EditorGUILayout.Slider("Value", key.value, range.yMin, range.yMax);
                    float inTgt = EditorGUILayout.FloatField("In Tangent", key.inTangent);
                    float outTgt = EditorGUILayout.FloatField("Out Tangent", key.outTangent);

                    if(t != key.time || v != key.value || inTgt != key.inTangent || outTgt != key.outTangent)
                    {
                        Keyframe newkey = new Keyframe(t,v,inTgt, outTgt);
                        m_Editor.SetKeyframe(selection.curve, selection.keyframeIndex, newkey);
                    }
                    EditorGUI.indentLevel--;
                }

                // Canvas
                DrawCurveCanvas(curveArea);
                dirty = m_Editor.OnGUI(curveArea);


            }
            return dirty;
        }

        public void DrawCurveCanvas(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.25f));
            if(Event.current.type == EventType.Layout)
                return;

            GUI.BeginClip(rect);

            Rect area = new Rect(Vector2.zero, rect.size);
            rect = m_CurvePadding.Remove(area);

            Rect bounds = m_Editor.settings.bounds;
            float minInput = bounds.xMin;
            float maxInput = bounds.xMax;
            float minOutput = bounds.yMin;
            float maxOutput = bounds.yMax;

            //////////////////////////////////////////////////////////////////////////////
            // Draw Origins
            //////////////////////////////////////////////////////////////////////////////

            float l = lineBrightnessValue;

            Handles.color = new Color(l, l, l, 0.2f);
            Handles.DrawLine(new Vector2(rect.xMin, area.yMin), new Vector2(rect.xMin, area.yMax));
            Handles.DrawLine(new Vector2(area.xMin, rect.yMax), new Vector2(area.xMax, rect.yMax));
            Handles.DrawLine(new Vector2(rect.xMax, area.yMin), new Vector2(rect.xMax, area.yMax));
            Handles.DrawLine(new Vector2(area.xMin, rect.yMin), new Vector2(area.xMax, rect.yMin));

            //////////////////////////////////////////////////////////////////////////////
            // Draw Zero Axis'es
            //////////////////////////////////////////////////////////////////////////////

            if(minInput < 0 && maxInput > 0)
            {
                Handles.color = new Color(l, l, l, 0.6f);
                Handles.DrawLine(new Vector2(0,rect.yMin),new Vector2(0,rect.yMax));
            }

            if(minOutput < 0 && maxOutput > 0)
            {
                Handles.color = new Color(l, l, l, 0.6f);
                Handles.DrawLine(new Vector2(rect.xMin,0),new Vector2(rect.xMax, 0));
            }

            //////////////////////////////////////////////////////////////////////////////
            // Draw Grid By Step
            //////////////////////////////////////////////////////////////////////////////

            Handles.color = new Color(l, l, l, 0.05f);

            for(int i = 1; i < 8; i++) //  Verticals
            {
                float step = Mathf.Lerp(rect.xMin, rect.xMax,(float)i / 8); 
                Handles.DrawLine(new Vector2(step, area.yMin), new Vector2(step, area.yMax));
            }

            for(int i = 1; i < 4; i++) //  Horizontals
            {
                float step = Mathf.Lerp(rect.yMin, rect.yMax,(float)i / 4); 
                Handles.DrawLine(new Vector2(area.xMin, step), new Vector2(area.xMax, step));
            }

            //////////////////////////////////////////////////////////////////////////////
            // Texts
            //////////////////////////////////////////////////////////////////////////////

            Rect minInRect = new Rect(rect.xMin, rect.yMax, 40, 12);
            Rect maxInRect = new Rect(rect.xMax-40, rect.yMax, 40, 12);
            Rect minOutRect = new Rect(rect.xMin-40, rect.yMax-12, 40, 12);
            Rect maxOutRect = new Rect(rect.xMin-40, rect.yMin, 40, 12);

            GUI.Label(minInRect, minInput.ToString("F2"), styles.smallLabelLeftAlign);
            GUI.Label(maxInRect, maxInput.ToString("F2"), styles.smallLabelRightAlign);
            GUI.Label(minOutRect, minOutput.ToString("F2"), styles.smallLabelRightAlign);
            GUI.Label(maxOutRect, maxOutput.ToString("F2"), styles.smallLabelRightAlign);

            //////////////////////////////////////////////////////////////////////////////
            // Text on Zero Axis'es
            //////////////////////////////////////////////////////////////////////////////

            if(minInput < 0 && maxInput > 0)
            {
                Handles.color = new Color(l, l, l, 0.6f);
                Handles.DrawLine(new Vector2(0,rect.yMin), new Vector2(0,rect.yMax));
            }

            if(minOutput < 0 && maxOutput > 0)
            {
                Handles.color = new Color(l, l, l, 0.6f);
                Handles.DrawLine(new Vector2(rect.xMin,0),new Vector2(rect.xMax, 0));
            }

            // Custom delegate
            if (OnPostGUI != null)
                OnPostGUI(area, rect);

            GUI.EndClip();
        }

        #region Styles
        public Styles styles { get { if (m_Styles == null) m_Styles = new Styles(); return m_Styles; } }
        private Styles m_Styles;

        public class Styles
        {
            public GUIStyle smallLabelLeftAlign;
            public GUIStyle smallLabelRightAlign;

            public Styles()
            {
                smallLabelLeftAlign = new GUIStyle(EditorStyles.miniLabel);
                smallLabelLeftAlign.alignment = TextAnchor.MiddleLeft;
                smallLabelRightAlign = new GUIStyle(EditorStyles.miniLabel);
                smallLabelRightAlign.alignment = TextAnchor.MiddleRight;
            }
        }
        #endregion
    }
}

