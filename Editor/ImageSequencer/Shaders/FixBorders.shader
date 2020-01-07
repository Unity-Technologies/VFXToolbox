Shader "Hidden/VFXToolbox/ImageSequencer/FixBorders"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_FixFactors("FixFactors" , Vector) = (0.0,0.0,0.0,0.0)
		_FadeToColor("FadeToColor", Color) = (0.0,0.0,0.0,1.0)
		_FadeToAlpha("FadeToAlpha", Float) = 0.0
		_Exponent("_Exponent", Float) = 1.0
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
			float4 _FixFactors;
			float4 _FadeToColor;
			float _FadeToAlpha;
			float _Exponent;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float mask = 1.0f;

				if (_FixFactors.r > 0.0f)
				{
					mask *= saturate(i.uv.x / _FixFactors.r);
				}

				if (_FixFactors.g > 0.0f)
				{
					mask *= saturate((1.0f-i.uv.x) / _FixFactors.g);
				}

				if (_FixFactors.b > 0.0f)
				{
					mask *= saturate((1.0f - i.uv.y) / _FixFactors.b);
				}

				if (_FixFactors.a > 0.0f)
				{
					mask *= saturate(i.uv.y / _FixFactors.a);
				}

				mask = pow(mask, _Exponent);
				mask = smoothstep(0, 1, mask);

				float4 incolor = tex2D(_MainTex, i.uv);

				float4 outColor = float4(0.0f,0.0f,0.0f,0.0f);
				outColor.rgb = lerp(_FadeToColor.rgb, incolor.rgb, lerp(1.0f,mask,_FadeToColor.a));
				outColor.a = lerp( incolor.a * mask, incolor.a, _FadeToAlpha);

				return outColor;
			}
			ENDCG
		}
	}
}
