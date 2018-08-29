using UnityEngine;
using UnityEditor.ProjectWindowCallback;
using System.IO;

namespace UnityEditor.VFXToolbox.Workbench
{
    public class WorkbenchBehaviourFactory
    {
        [MenuItem("Assets/Create/Visual Effects/Workbench/Empty Workbench", priority = 301)]
        private static void MenuCreateAsset()
        {
            WorkbenchBehaviourFactory.CreateWorkbenchAsset("New Workbench");
        }

        public static void CreateWorkbenchAsset(string defaultName, WorkbenchToolBase defaultTool = null)
        {
            var icon = (Texture2D)null;
            var action = ScriptableObject.CreateInstance<DoCreateWorkbenchBehaviour>();
            action.SetTool(defaultTool);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, action, defaultName + ".asset", icon, null);

        }

        [MenuItem("Assets/Create/Visual Effects/Workbench/Brushes/Flow Brush", priority = 301)]
        private static void MenuCreateFlowBrush()
        {
            var icon = (Texture2D)null;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateFlowBrush>(), "New Flow Brush.asset", icon, null);
        }
        /*
        [MenuItem("Assets/Create/Visual Effects/Tools/Brush Collection", priority = 301)]
        private static void MenuCreatePaintBrushCollection()
        {
            var icon = (Texture2D)null;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreatePaintBrushCollectionAsset>(), "New Brush Collection.asset", icon, null);
        }*/

        internal static WorkbenchBehaviour CreateWorkbenchAssetAtPath(string path, WorkbenchToolBase tool)
        {
            WorkbenchBehaviour asset = ScriptableObject.CreateInstance<WorkbenchBehaviour>();
            asset.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(asset, path);
            if(tool != null)
            {
                AssetDatabase.AddObjectToAsset(tool, asset);
                tool.Default(asset);
                tool.name = tool.GetType().Name;
                tool.hideFlags = HideFlags.HideInHierarchy;
                EditorUtility.SetDirty(asset);
                tool.Initialize();
                AssetDatabase.SaveAssets();
            }

            return asset;
        }

        internal static FlowPaintBrush CreateFlowPaintBrushAssetAtPath(string path)
        {
            FlowPaintBrush asset = ScriptableObject.CreateInstance<FlowPaintBrush>();
            asset.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

       /* internal static PaintBrushCollection CreatePaintBrushCollectionAssetAtPath(string path)
        {
            PaintBrushCollection asset = ScriptableObject.CreateInstance<PaintBrushCollection>();
            asset.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }*/
    }

    internal class DoCreateWorkbenchBehaviour : EndNameEditAction
    {
        private WorkbenchToolBase m_Tool;

        public void SetTool(WorkbenchToolBase tool)
        {
            m_Tool = tool;
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            WorkbenchBehaviour asset = WorkbenchBehaviourFactory.CreateWorkbenchAssetAtPath(pathName, m_Tool);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }
    }


    internal class DoCreateFlowBrush : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            FlowPaintBrush asset = WorkbenchBehaviourFactory.CreateFlowPaintBrushAssetAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }
    }
    /*
    internal class DoCreatePaintBrushCollectionAsset : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            PaintBrushCollection asset = ImageScriptAssetFactory.CreatePaintBrushCollectionAssetAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }
    }*/

}
