Shader "Hidden/VFXToolbox/ImageSequencer/ImageSequencerCanvas"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_RGBAMask("RGBAMask", Color) = (1.0,1.0,1.0,1.0)
		_MipMap("MipMap", float) = 0.0
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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			float4 _RGBAMask;
			float _MipMap;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2Dlod(_MainTex, float4(i.uv,0.0f,_MipMap));

				if(_RGBAMask.r + _RGBAMask.g + _RGBAMask.b == 0.0f)
				{
					// As we preview alpha, we need to linearize
					float a = GammaToLinearSpaceExact(col.a);
					col = float4(a,a,a,1.0f);
				} 
				else 
				{
					if(_RGBAMask.a == 0.0f)
					{
						col.a = 1.0f;
					}
					col.rgb *= _RGBAMask.rgb;
				}
				return col;
			}
			ENDCG
		}
	}
}
