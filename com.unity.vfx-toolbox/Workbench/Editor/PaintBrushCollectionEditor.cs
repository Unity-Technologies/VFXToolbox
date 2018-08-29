using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.VFXToolbox.Workbench
{
    [CustomEditor(typeof(PaintBrushCollection))]
    public class PaintBrushCollectionEditor : Editor
    {
        SerializedProperty m_Brushes;

        UnityEngine.Object m_OjbectToAdd;
        bool debug = false;

        public virtual void OnEnable()
        {
            m_Brushes = serializedObject.FindProperty("Brushes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();




            debug = EditorGUILayout.Toggle(VFXToolboxGUIUtility.Get("Debug"), debug); 
            if (debug)
                OnDebugInspectorGUI(); 

            serializedObject.ApplyModifiedProperties();




        }

        #region TODELETE
        private void OnDebugInspectorGUI()
        {
            int count = m_Brushes.arraySize;
            int todelete = -1;
            for(int i = 0; i < count; i++)
            {
                using(new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(m_Brushes.GetArrayElementAtIndex(i));
                    if(GUILayout.Button(VFXToolboxGUIUtility.Get("-"),GUILayout.Width(24)))
                    {
                        todelete = i;
                    }
                }
            }

            if (todelete >= 0)
            {
                // Twice... seriously...
                m_Brushes.DeleteArrayElementAtIndex(todelete);
                m_Brushes.DeleteArrayElementAtIndex(todelete);

            }


            EditorGUILayout.Space();

            using(new GUILayout.HorizontalScope())
            {
                m_OjbectToAdd = EditorGUILayout.ObjectField(VFXToolboxGUIUtility.Get("Brush to Add"), m_OjbectToAdd, typeof(PaintBrush),false);
                if(GUILayout.Button(VFXToolboxGUIUtility.Get("+"),GUILayout.Width(24)))
                {
                    m_Brushes.InsertArrayElementAtIndex(count);
                    m_Brushes.GetArrayElementAtIndex(count).objectReferenceValue = m_OjbectToAdd;
                    m_OjbectToAdd = null;
                }
            }
        }
        #endregion
    }
}
