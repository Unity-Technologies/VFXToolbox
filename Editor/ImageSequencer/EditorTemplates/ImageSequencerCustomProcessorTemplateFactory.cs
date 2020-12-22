using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    internal class ImageSequencerCustomProcessorTemplateFactory
    {
        [MenuItem("Assets/Create/Visual Effects/Custom Processor (C#, Shader)", priority = 322)]
        static void CreateStaticEditorShaderTemplate()
        {
            var icon = EditorGUIUtility.FindTexture("Shader Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateImageSequencerCustomProcessor>(), "New Custom Processor.cs", icon, null);
        }

        public static MonoScript CreateAssetsAtPath(string path)
        {
            string name = path.Split('/').Last().Replace(".cs", "");

            string shaderPath = path.Replace(".cs", ".shader");

            StringBuilder sb_shader = new StringBuilder();
            sb_shader.AppendFormat("Shader \"Hidden/VFXToolbox/ImageSequencer/{0}\"", name);
            sb_shader.Append(@"
{
    Properties
    {
        _MainTex(""Texture"", 2D) = ""white"" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            # include ""UnityCG.cginc""

            struct appdata
        {
            float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

        struct v2f
        {
            float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

        sampler2D _MainTex;

        v2f vert(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
    }
    ENDCG
}
    }
}");
            File.WriteAllText(shaderPath, sb_shader.ToString());
            AssetDatabase.ImportAsset(shaderPath);


            StringBuilder sb_monoScript = new StringBuilder();

            string friendlyName = ObjectNames.NicifyVariableName(name);
            string className = Regex.Replace(name, @"[^0-9a-zA-Z_]+", "");
            
            Debug.Log(className);
            sb_monoScript.Append(@"using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.VFX.Toolbox.ImageSequencer;
using UnityEngine;

");
            sb_monoScript.AppendFormat("[Processor(\"Category\",\"{0}\")]\n", friendlyName);
            sb_monoScript.AppendFormat("public class {0} : ProcessorBase\n", className);
            sb_monoScript.Append(@"{
    /// <summary>
    /// The Shader Path (in the project) of the Shader File
    /// </summary>
");
            sb_monoScript.AppendFormat("    public override string shaderPath => \"{0}\";", shaderPath);

            sb_monoScript.Append(@"

    /// <summary>
    /// The Processor Name (as it will appear in the list)
    /// </summary>
");
            sb_monoScript.AppendFormat("    public override string processorName => \"{0}\";", friendlyName);
            sb_monoScript.Append(@"

    /// <summary>
    /// Default() configures a default instance of the processor
    /// of the processor (e.g: when it will be added from the menu)
    /// </summary>
    public override void Default()
    {
    }

    /// <summary>
    ///  OnInspectorGUI() displays the properties in the left pane editor.
    /// use the serializedObject to fetch serializedProperties
    /// </summary>
    public override bool OnInspectorGUI(bool changed, SerializedObject serializedObject)
    {
        return changed;
    }

    /// <summary>
    /// Process(int frame) method is called when the frame needs to be processed.
    /// ProcessFrame(int, texture) needs to be called in order to run the shader on the full frame. 
    /// You can still perform Graphics.Blit() if you need to perform multiple blits.
    /// </summary>
    public override bool Process(int frame)
    {
        Texture inputFrame = RequestInputTexture(frame);
        ProcessFrame(frame, inputFrame);
        return true;
    }
}");

            File.WriteAllText(path, sb_monoScript.ToString());
            AssetDatabase.ImportAsset(path);

            return AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        }


    }
    internal class DoCreateImageSequencerCustomProcessor : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            MonoScript asset = ImageSequencerCustomProcessorTemplateFactory.CreateAssetsAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }
    }

}

