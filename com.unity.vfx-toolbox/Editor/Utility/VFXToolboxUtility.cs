using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace UnityEditor.VFXToolbox
{
    internal class VFXToolboxUtility
    {
        #region Readback utils

        public static Color[] ReadBack(RenderTexture renderTexture)
        {
            RenderTexture backup = RenderTexture.active;
            RenderTexture.active = renderTexture;

            bool hdr = false;
            if (renderTexture.format == RenderTextureFormat.ARGBHalf)
                hdr = true;

            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, hdr ? TextureFormat.RGBAHalf : TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            //texture.Apply(); 
            RenderTexture.active = backup;

            return texture.GetPixels();
        }

        #endregion

        #region Asset Utils

        public static bool IsDirectory(string path)
        {
            if (path.Length > 0 && Directory.Exists(path))
                return true;
            return false;
        }

        public static bool IsDirectorySelected()
        {
            var path = "";
            var obj = Selection.activeObject;
            if (obj == null) path = "Assets";
            else path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
            return IsDirectory(path);
        }

        public static string[] GetAllTexturesInPath(string path)
        {
            List<string> files = new List<string>();
            string absolutePath = Application.dataPath + "/" + path.Remove(0, 7);
            string [] fileEntries = Directory.GetFiles(absolutePath);
            int count = fileEntries.Length;
            int i = 0;
            foreach(string fileName in fileEntries)
            {
                string fname = fileName.Replace('\\', '/');
                int index = fname.LastIndexOf('/');
                string localPath = path;
                if (index > 0)
                    localPath += fname.Substring(index);
                VFXToolboxGUIUtility.DisplayProgressBar("Image Sequencer", "Discovering Assets in folder...", (float)i/count);
                Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(localPath);
                if(t != null)
                    files.Add(localPath);
                i++;
            }
            VFXToolboxGUIUtility.ClearProgressBar();
            return files.ToArray();
        }
        #endregion

        #region ReflectionUtils

        public static IEnumerable<Type> FindConcreteSubclasses<T>()
        {
            List<Type> types = new List<Type>();
            foreach (var domainAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] assemblyTypes = null;
                try
                {
                    assemblyTypes = domainAssembly.GetTypes();
                }
                catch(Exception)
                {
                    Debug.LogWarning("Cannot access assembly: " + domainAssembly);
                    assemblyTypes = null;
                }
                if (assemblyTypes != null)
                    foreach (var assemblyType in assemblyTypes)
                        if (assemblyType.IsSubclassOf(typeof(T)) && !assemblyType.IsAbstract)
                            types.Add (assemblyType);
            }
            return types;
        }

        #endregion

        #region GraphicUtils

        public static void BlitRect(Rect rect, RenderTexture target, Texture texture, Material material = null)
        {
            RenderTexture backup = RenderTexture.active;
            RenderTexture.active = target;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, target.width, target.height, 0);
            Graphics.DrawTexture(rect, texture, material);
            GL.PopMatrix();
            RenderTexture.active = backup;
        }

        #endregion
    }
}
