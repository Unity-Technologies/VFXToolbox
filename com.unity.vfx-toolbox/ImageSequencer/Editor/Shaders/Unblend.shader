Shader "Hidden/VFXToolbox/ImageSequencer/Unblend"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BackgroundColor ("_BackgroundColor", Color) = (0.5,0.5,0.5,1.0)
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
			float4 _BackgroundColor;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float3 getSourceColor(float alpha, float3 destColor, float3 backgroundColor)
			{
				if(alpha < 0.03f)
					return destColor;
					
				if(alpha < 1.0f)
				{
					return ((destColor - backgroundColor)/alpha) + backgroundColor;
				} 
				else
				{
					return destColor;
				}

			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 tex = tex2D(_MainTex, i.uv);
				return float4(getSourceColor(tex.a,tex.rgb,_BackgroundColor),tex.a);
			}
			ENDCG
		}
	}
}
