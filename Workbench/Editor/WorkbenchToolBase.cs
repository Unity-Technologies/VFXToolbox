using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.VFXToolbox.Workbench
{
    public abstract class WorkbenchToolBase : ScriptableObject
    {
        public WorkbenchCanvasBase canvas { get { return m_Canvas; } }

        protected WorkbenchBehaviour m_Asset;
        protected WorkbenchCanvasBase m_Canvas;


        public abstract void Dispose();
        public abstract void Update();

        // Attachs the tool to the behaviour
        public virtual void AttachToBehaviour(WorkbenchBehaviour asset)
        {
            m_Asset = asset;
            asset.tool = this;
        }

        public abstract void InitializeRuntime();

        public abstract bool OnInspectorGUI();
        public abstract bool OnCanvasGUI(WorkbenchImageCanvas canvas);

        public void InitializeEditor(Workbench window)
        {
            m_Canvas = GetCanvas(window);
        }
        protected abstract WorkbenchCanvasBase GetCanvas(Workbench window);

        public static string GetCategory(Type t)
        {
            return (string)InvokeTypeStatic(t, "GetCategory");
        }

        public static string GetName(Type t)
        {
            return (string)InvokeTypeStatic(t, "GetName");
        }

        private static object InvokeTypeStatic(Type t, string MethodName, object[] parameters = null)
        {
            try
            {
                var descMethod = t.GetMethod(MethodName, BindingFlags.Public | BindingFlags.Static);
                if (descMethod != null && descMethod.ReturnType == typeof(string) && descMethod.GetParameters().Length == 0)
                {
                    return descMethod.Invoke(null,parameters);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error fetching Category from processor : "+ t.Name +" - "+ e.Message);
            }
            return "";
        }
    }
}
