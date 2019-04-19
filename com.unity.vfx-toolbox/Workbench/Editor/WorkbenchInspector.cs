using UnityEngine;
using UnityEditor.ProjectWindowCallback;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace UnityEditor.VFXToolbox.Workbench
{
    [CustomEditor(typeof(WorkbenchBehaviour))]
    public class WorkbenchInspector : Editor
    {

        public SerializedProperty tool
        {
            get
            {
                if (m_Tool == null)
                    m_Tool = serializedObject.FindProperty("tool");
                return m_Tool;
            }
        }

        private SerializedProperty m_Tool;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if(GUILayout.Button("Open Editor", GUILayout.Height(48)))
            {
                Workbench window = EditorWindow.GetWindow<Workbench>();
                window.LoadAsset((WorkbenchBehaviour)serializedObject.targetObject);
            }

            string toolName = tool.objectReferenceValue == null ? "(No Tool Installed)" : (tool.objectReferenceValue as WorkbenchToolBase).GetType().Name;

            using(new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Workbench Tool", GUILayout.Width(EditorGUIUtility.labelWidth));
                
                if (GUILayout.Button(toolName, EditorStyles.popup))
                {
                    GenericMenu menu = new GenericMenu();
                    var types = VFXToolboxUtility.FindConcreteSubclasses<WorkbenchToolBase>();

                    foreach(Type t in types)
                    {
                        string category = WorkbenchToolBase.GetCategory(t);
                        string name = WorkbenchToolBase.GetName(t);
                        string path = (category.Length > 0 ? category + "/" : "") + name;
                        menu.AddItem(VFXToolboxGUIUtility.Get(path), false, AddObject, t);
                    }
                    menu.ShowAsContext();
                }
            }
            using (new EditorGUI.DisabledGroupScope(true))
            {
                if (tool.objectReferenceValue != null)
                {
                    EditorGUILayout.Foldout(true, toolName + " Properties");
                    (tool.objectReferenceValue as WorkbenchToolBase).OnInspectorGUI();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        public void AddObject(object type)
        {
            if((Type)type != null)
            {
                if (tool != null)
                {
                    if( EditorUtility.DisplayDialog("Changing tools", "You are about to change the tool set up in this asset, this action cannot be undone, are you sure you want to continue?", "Yes", "No") == false)
                    {
                        // Cancel
                        return;
                    }

                } 
                DestroyImmediate(tool.objectReferenceValue,true);
                var newTool = CreateInstance((Type)type);
                AssetDatabase.AddObjectToAsset(newTool, serializedObject.targetObject);
                (newTool as WorkbenchToolBase).AttachToBehaviour((WorkbenchBehaviour)serializedObject.targetObject);
                newTool.name = ((Type)type).Name;
                newTool.hideFlags = HideFlags.HideInHierarchy;
                tool.objectReferenceValue = newTool;
                EditorUtility.SetDirty(serializedObject.targetObject);
                serializedObject.ApplyModifiedProperties();
                (newTool as WorkbenchToolBase).InitializeRuntime();
            }
        }

    }
}
