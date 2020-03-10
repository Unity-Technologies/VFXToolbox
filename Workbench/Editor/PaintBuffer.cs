using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.VFXToolbox.Workbench
{
    public class PaintBuffer : ScriptableObject
    {
        public int Width;
        public int Height;
        public Color[] data;

        public void FromRenderTexture(RenderTexture texture)
        {
            if(Width != texture.width || Height != texture.height)
            {
                Width = texture.width;
                Height = texture.height;
                data = VFXToolboxUtility.ReadBack(texture);
            }
            else
            {
                Color[] newdata = VFXToolboxUtility.ReadBack(texture);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = newdata[i];
                }
            }

        }

    }
}
