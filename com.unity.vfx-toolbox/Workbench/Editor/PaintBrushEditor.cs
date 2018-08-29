using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.VFXToolbox.Workbench
{

    [CustomEditor(typeof(FlowPaintBrush))]
    public class FlowPaintBrushEditor : PaintBrushEditor
    {

    }

    [CustomEditor(typeof(PaintBrush))]
    public class PaintBrushEditor : Editor
    {
        SerializedProperty m_BrushTexture;
        SerializedProperty m_BrushSize;
        SerializedProperty m_BrushOpacity;
        SerializedProperty m_BrushSpacing;

        public virtual void OnEnable()
        {
            m_BrushTexture = serializedObject.FindProperty("Texture");
            m_BrushSize = serializedObject.FindProperty("Size");
            m_BrushOpacity = serializedObject.FindProperty("Opacity");
            m_BrushSpacing = serializedObject.FindProperty("Spacing");
        }

        public override void OnInspectorGUI()
        {
            using (new GUILayout.HorizontalScope())
            {

                Texture2D texture = m_BrushTexture.objectReferenceValue as Texture2D;
                if(texture != null)
                {
                    GUILayout.FlexibleSpace();
                    Rect r = GUILayoutUtility.GetRect(texture.width, texture.height);
                    GUILayout.FlexibleSpace();
                    EditorGUI.DrawTextureTransparent(r, texture);
                }
            }

            EditorGUILayout.PropertyField(m_BrushSize);
            EditorGUILayout.PropertyField(m_BrushOpacity);
            EditorGUILayout.PropertyField(m_BrushSpacing);

            base.OnInspectorGUI();
        }
    }
}
