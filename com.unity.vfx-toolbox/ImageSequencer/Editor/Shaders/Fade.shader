Shader "Hidden/VFXToolbox/ImageSequencer/Fade"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_FadeToColor ("FadeToColor", Color) = (1.0,1.0,1.0,0.0)
		_Ratio("Ratio", Float) = 0.5
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
			float4 _FadeToColor;
			float _Ratio;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return lerp(_FadeToColor,tex2D(_MainTex, i.uv),_Ratio);
			}
			ENDCG
		}
	}
}
