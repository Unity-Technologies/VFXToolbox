Shader "Hidden/VFXToolbox/ImageSequencer/Blend"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_AltTex ("_AltTex", 2D) = "white" {}
		_BlendFactor("_BlendFactor", float) = 0
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
			sampler2D _AltTex;
			float _BlendFactor;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return lerp(tex2D(_MainTex, i.uv),tex2D(_AltTex,i.uv),_BlendFactor);
			}
			ENDCG
		}
	}
}
