using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System;

namespace UnityEditor.VFXToolbox.VolumeEditor
{
    public class DummyRenderer : VolumeRendererBase
    {

        public DummyRenderer() : base()
        {

        }

        public override void OnGUI()
        {

        }

        public override void Render(PreviewRenderUtility previewUtility)
        {
            RenderOutlineCube(previewUtility);
            Handles.Label(Vector3.zero, "No Volume Loaded, please drag a Texture3D in this window.");
        }

        public override void SetTexture(Texture texture)
        {

        }

        public override string ToString()
        {
            return "Dummy";
        }
    }
}
