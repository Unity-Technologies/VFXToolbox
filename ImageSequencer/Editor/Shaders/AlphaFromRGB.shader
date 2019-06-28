Shader "Hidden/VFXToolbox/ImageSequencer/AlphaFromRGB"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_RGBTint("_RGBTint", Vector) = (1.0, 1.0, 1.0, 1.0)
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

			#include "UnityCG.cginc"

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
			float4 _RGBTint;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				float3 weights = float3(0.2126f, 0.7152f, 0.0722f);
				float3 color =  tex2D(_MainTex, i.uv).rgb;
				float3 w = LinearToGammaSpace(color) * _RGBTint.rgb * weights;
				float alpha = w.r + w.g + w.b;
				return float4(color,alpha);
			}
			ENDCG
		}
	}
}
