using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    internal class ImageSequencerCustomShaderTemplateFactory
    {
        [MenuItem("Assets/Create/Visual Effects/Custom Material (Shader)", priority = 323)]
        static void CreateStaticEditorShaderTemplate()
        {
            var icon = EditorGUIUtility.FindTexture("Shader Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateImageSequencerCustomShader>(), "New Custom Shader.shader", icon, null);
        }

        public static Shader CreateShaderAtPath(string path)
        {
            string name = path.Split('/').Last().Replace(".shader", "");

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Shader \"ImageSequencer/{0}\"", name);
            sb.Append(@"
{
    Properties
    {
    }
    SubShader
    {
        Tags { ""RenderType"" = ""Opaque"" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include ""UnityCG.cginc""

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

            // ImageSequencer CustomMaterial uniforms
            // ======================================
            //
            // sampler2D _InputFrame;			// Input Frame (from previous sequence)
            //
            // float4 _FrameData;				// Frame Data
            //									//		x, y : width (x) and height (y) in pixels
            //									//		z, w : sequence index (z) and length (w)
            //
            // float4 _FlipbookData;			// Flipbook Data
            //									//		x, y : number of columns (x) and rows (y)
            //	

            sampler2D _InputFrame;
            float4 _FrameData;
            float4 _FlipbookData;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Process the Color
                fixed4 col = tex2D(_InputFrame, i.uv);
                return col;
            }
            ENDCG
        }
    }
}");
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Shader>(path);
        }


    }
    internal class DoCreateImageSequencerCustomShader : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            Shader asset = ImageSequencerCustomShaderTemplateFactory.CreateShaderAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }
    }

}

