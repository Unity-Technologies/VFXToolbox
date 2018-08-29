using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.VFXToolbox.Workbench
{
    public class PaintBrush : ScriptableObject
    {
        public Texture2D Texture;
        [Range(1.0f,512.0f)]
        public float Size;
        [Range(0.0f,5.0f)]
        public float Opacity;
        [Range(0.1f,1.0f)]
        public float Spacing;

        public virtual void Default()
        {
            Size = 4.0f;
            Size = 64.0f;
            Opacity = 0.5f;
            Spacing = 0.1f;
        }

        public virtual bool DrawBrushCanvasPreview(Vector2 screenPosition, float radius)
        {
            Handles.DrawWireDisc(screenPosition, Vector3.forward, radius);
            return true;
        }
    }
}
