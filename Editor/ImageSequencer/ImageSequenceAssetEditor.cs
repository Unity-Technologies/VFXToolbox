using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    [CustomEditor(typeof(ImageSequence))]
    internal class ImageSequenceAssetEditor : Editor
    {
        ImageSequence sequence;


        private bool m_PreviewInput = false;
        private bool m_PreviewOutput = false;
        private bool m_RequireConstantRepaint = false;

        public override bool RequiresConstantRepaint()
        {
            return m_RequireConstantRepaint;
        }

        private void OnEnable()
        {
            sequence = serializedObject.targetObject as ImageSequence;

            InitializePreview();
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_RequireConstantRepaint = false;

            using (new EditorGUILayout.VerticalScope())
            {
                if (GUILayout.Button(VFXToolboxGUIUtility.Get("Edit Sequence"), GUILayout.Height(40)))
                {
                    ImageSequencer toolbox = EditorWindow.GetWindow<ImageSequencer>();
                    toolbox.LoadAsset((ImageSequence)Selection.activeObject);
                }

                VFXToolboxGUIUtility.ToggleableHeader(true, false, "Input Frames");
                {
                    var inputFrames = serializedObject.FindProperty("inputFrameGUIDs");
                    int inputFrameCount = inputFrames.arraySize;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Input sequence contains " + inputFrameCount + " frame(s).");
                        GUILayout.FlexibleSpace();
                        m_PreviewInput = GUILayout.Toggle(m_PreviewInput, VFXToolboxGUIUtility.Get("Preview"), EditorStyles.miniButton);
                    }

                    if (inputFrameCount > 0 && m_PreviewInput)
                    {
                        int index;

                        if (inputFrameCount > 1)
                        {
                            m_RequireConstantRepaint = true;
                            float time = (float)EditorApplication.timeSinceStartup;
                            index = (int)Mathf.Floor((time * 30) % inputFrameCount);
                        }
                        else
                        {
                            index = 0;
                        }

                        var frame = inputFrames.GetArrayElementAtIndex(index);
                        string guid = frame.stringValue;
                        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                        DrawAnimatedPreviewLayout(texture, ((float)index / inputFrameCount));
                    }
                    else
                    {
                        m_PreviewInput = false;
                    }
                }

                GUILayout.Space(24);
                VFXToolboxGUIUtility.ToggleableHeader(true, false, "Processors");
                {
                    var processors = serializedObject.FindProperty("processorInfos");
                    int processorsCount = processors.arraySize;
                    EditorGUILayout.LabelField("Asset contains " + processorsCount + " Processor (s).");
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < processorsCount; i++)
                    {
                        var item = processors.GetArrayElementAtIndex(i).objectReferenceValue as ProcessorInfo;
                        EditorGUILayout.LabelField("#" + i + " - " + item.Settings.label + (item.Enabled ? "" : " (Disabled)"));
                    }
                    EditorGUI.indentLevel--;
                }


                GUILayout.Space(24);
                VFXToolboxGUIUtility.ToggleableHeader(true, false, "Export Settings");

                var exportSettings = serializedObject.FindProperty("exportSettings");

                string fileName = exportSettings.FindPropertyRelative("fileName").stringValue;
                var mode = (ImageSequence.ExportMode)exportSettings.FindPropertyRelative("exportMode").enumValueIndex;
                var frameCount = exportSettings.FindPropertyRelative("frameCount");

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.EnumPopup(VFXToolboxGUIUtility.Get("Export Format"), mode);
                EditorGUI.EndDisabledGroup();
                if (fileName != "")
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField("Export Path", fileName);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.HelpBox("This asset has not yet been exported. Please open editor and export it to generate a sequence.", MessageType.None);
                }
            }
        }

        private void DrawAnimatedPreviewLayout(Texture texture, float progress)
        {
            float ratio = (float)texture.height / (float)texture.width;
            using (new EditorGUILayout.HorizontalScope())
            {
                float width = EditorGUIUtility.currentViewWidth - 32;
                float height = 240;
                GUILayout.FlexibleSpace();
                Rect texture_rect;
                if (ratio >= 1)
                    texture_rect = GUILayoutUtility.GetRect(height / ratio, height);
                else
                    texture_rect = GUILayoutUtility.GetRect(width, width * ratio);

                GUILayout.FlexibleSpace();
                EditorGUI.DrawTextureTransparent(texture_rect, texture);
                EditorGUI.DrawRect(new Rect(texture_rect.x, texture_rect.y, progress * 200.0f / ratio, 4.0f), new Color(0.3f, 0.5f, 1.0f));
            }
        }

        #region PREVIEW
        public int previewFrame = 0;
        Texture previewTexture;
        public Material arrayPreviewMaterial;

        static readonly int s_ShaderColorMask = Shader.PropertyToID("_ColorMask");
        static readonly int s_ShaderSliceIndex = Shader.PropertyToID("_SliceIndex");
        static readonly int s_ShaderMip = Shader.PropertyToID("_Mip");
        static readonly int s_ShaderToSrgb = Shader.PropertyToID("_ToSRGB");
        static readonly int s_ShaderIsNormalMap = Shader.PropertyToID("_IsNormalMap");

        void InitializePreview()
        {
            if (HasPreviewGUI())
                previewTexture = AssetDatabase.LoadAssetAtPath<Texture>(sequence.exportSettings.fileName);

            arrayPreviewMaterial = (Material)EditorGUIUtility.LoadRequired("Previews/Preview2DTextureArrayMaterial.mat");
            arrayPreviewMaterial.SetInt(s_ShaderColorMask, 15);
            arrayPreviewMaterial.SetInt(s_ShaderMip, 0);
            arrayPreviewMaterial.SetInt(s_ShaderToSrgb, QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
            arrayPreviewMaterial.SetInt(s_ShaderIsNormalMap, 0);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (previewTexture == null)
                InitializePreview();

            base.OnPreviewGUI(r, background);

            if (previewTexture is Texture2D)
            {
                EditorGUI.DrawTextureTransparent(r, previewTexture, ScaleMode.ScaleToFit, (float)previewTexture.width / previewTexture.height);
            }
            else if (previewTexture is Texture2DArray)
            {
                EditorGUI.DrawPreviewTexture(r, previewTexture, arrayPreviewMaterial, ScaleMode.ScaleToFit, (float)previewTexture.width/previewTexture.height, 0);
            }
        }

        public override void OnPreviewSettings()
        {
            if (previewTexture == null)
                InitializePreview();

            if (previewTexture is Texture2DArray)
            {
                Texture2DArray array = previewTexture as Texture2DArray;
                
                GUILayout.Label("Frame");
                previewFrame = EditorGUILayout.IntSlider(previewFrame, 0, array.depth-1);
                arrayPreviewMaterial.SetInt(s_ShaderSliceIndex, previewFrame);
            }
        }

        public override bool HasPreviewGUI()
        {
            if (serializedObject.targetObjects.Length > 1) // No Multiple Preview
                return false;

            ImageSequence.ExportSettings exportSettings = sequence.exportSettings;
            if (exportSettings.fileName == null                     // No Preview if not exported
                || !exportSettings.fileName.StartsWith("Assets/")   // No External Preview
                || exportSettings.fileName.Contains("#"))           // No Multiple Frame Preview
                return false;

            return true;
        }

        #endregion
    }
}
