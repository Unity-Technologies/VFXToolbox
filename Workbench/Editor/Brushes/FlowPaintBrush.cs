using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.VFXToolbox.Workbench
{
    public class FlowPaintBrush : PaintBrush
    {
        [Range(0.0f,10.0f)]
        public float MotionIntensity;
        [Range(-1.0f,1.0f)]
        public float TextureIntensity;

        public override void Default()
        {
            base.Default();
            MotionIntensity = 1.0f;
            TextureIntensity = 1.0f;
        }
    }
}
