﻿Shader "Hidden/VFXToolbox/ImageSequencer/Crop"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_CropFactors("CropFactors", Vector) = (0.0,0.0,0.0,0.0)
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
			float4 _CropFactors;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = float2(
					v.uv.x * (1.0f - _CropFactors.y - _CropFactors.x) + _CropFactors.x,
					v.uv.y * (1.0f - _CropFactors.z - _CropFactors.w) + _CropFactors.w
					);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return tex2D(_MainTex, i.uv);
			}
			ENDCG
		}
	}
}
