using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.VFXToolbox.Workbench
{
    public class PaintBrushUtility
    {
        public static PaintBrush[] paintBrushes
        {
            get { if (s_CachedPaintBrushes == null) UpdatePaintBrushes(); return s_CachedPaintBrushes; }
        }
        private static PaintBrush[] s_CachedPaintBrushes;

        public static void UpdatePaintBrushes()
        {
            List<PaintBrush> brushes = new List<PaintBrush>();
            var guids = AssetDatabase.FindAssets("t:PaintBrush");
            foreach(string guid in guids)
            {
                brushes.Add(AssetDatabase.LoadAssetAtPath<PaintBrush>(AssetDatabase.GUIDToAssetPath(guid)));
            }
            s_CachedPaintBrushes = brushes.ToArray();
        }

        public class BrushListEditor
        {
            public PaintBrush selectedBrush;
            public Type m_FilteredType;
            private PaintBrush[] m_CachedBrushes;
            private Vector2 m_ScrollData = new Vector2(0,0);
            public BrushListEditor(Type filteredType)
            {
                m_FilteredType = filteredType;
                selectedBrush = null;
                CacheBrushes();
            }

            public void UpdatePaintBrushes()
            {
                PaintBrushUtility.UpdatePaintBrushes();
                CacheBrushes();
            }

            private void CacheBrushes()
            {
                List<PaintBrush> filtered = new List<PaintBrush>();

                foreach(PaintBrush brush in PaintBrushUtility.paintBrushes)
                {
                    if (brush.GetType() == m_FilteredType)
                        filtered.Add(brush);
                }

                m_CachedBrushes = filtered.ToArray();
            }

            public void DoGUILayout()
            {
                if (m_CachedBrushes == null)
                    CacheBrushes();

                using (new EditorGUILayout.ScrollViewScope(m_ScrollData,false,false))
                {
                    foreach (PaintBrush brush in m_CachedBrushes)
                    {

                    }
                }



            }
        }

    }
}


