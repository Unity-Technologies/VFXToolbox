Shader "VFXToolbox/ImageScripter/FlowMapPaint"
{
	Properties 
	{
	 _MainTex ("Texture", any) = "" {} 
	 _Direction ("Direction", Vector) = (0.0,0.0,0.0,0.0)
	 _BrushOpacity ("BrushOpacity", Float) = 1.0	 
	} 

	SubShader {

		Tags { "ForceSupported" = "True" "RenderType"="Overlay" } 
		ColorMask RGB
		Lighting Off 
		Blend SrcAlpha OneMinusSrcAlpha 
		Cull Off 
		ZWrite Off 
		ZTest Always 
		
		Pass {	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _Direction;
			float _BrushOpacity;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 c = tex2D(_MainTex, i.texcoord);
				float2 noise = c.rg - 0.5f;
				return float4(saturate(_Direction.x+noise.x+0.5f),saturate(_Direction.y+noise.y+0.5f),0.0f,saturate(c.a * _BrushOpacity));
			}
			ENDCG 
		}
	} 	
	Fallback off 
}
