Shader "Hidden/VFXToolbox/Skybox/Gradient"
{
	Properties
	{
		_BottomColor("BottomColor", Color) = (0.2,0.2,0.2,1.0)
		_MiddleColor("MiddleColor", Color) = (0.5,0.5,0.5,1.0)
		_TopColor("TopColor", Color) = (0.2,0.2,0.2,1.0)
		_VerticalFalloff("Vertical Falloff", range(0.5,4.0)) = 1.0
		_DitherIntensity("Dither Intensity", range(0.0,0.5)) = 0.01
	}
	SubShader
	{
	Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
	Cull Off ZWrite Off

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
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				half3 rayDir : TEXCOORD1;	// Vector for incoming ray, normalized ( == -eyeRay )
				float4 ScreenPos : TEXCOORD2;
			};
			
			float4 _TopColor;
			float4 _MiddleColor;
			float4 _BottomColor;
			float _VerticalFalloff;
			float _DitherIntensity;


			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;

				// Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
				float3 eyeRay = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
				o.rayDir = half3(-eyeRay);
				o.ScreenPos = ComputeScreenPos(o.vertex);
				return o;
			}

			float dither4x4(float2 position)
			{
				const float kernel[16] = {0.0625, 0.5625, 0.1875, 0.6875, 0.8125, 0.3125, 0.9375, 0.4375, 0.25, 0.75, 0.125, 0.625, 1.0, 0.5, 0.875, 0.375};
				float v = kernel[(((int)position.y & 3) << 2) + ((int)position.x & 3)];
				return v;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				//float2 wcoord = (i.ScreenPos.xy/i.ScreenPos.w);
				float dither = _DitherIntensity*(dither4x4(i.ScreenPos.xy) - 0.5f);

				float a = -i.rayDir.y;
				float4 up = lerp(_MiddleColor, _TopColor, pow(saturate(a),_VerticalFalloff + dither));
				float4 down = lerp(_MiddleColor, _BottomColor, pow(saturate(-a),_VerticalFalloff + dither));
				fixed4 col;
				if (a >= 0.0f) col = up;
				else col = down;
				return col;
			}
			ENDCG
		}
	}
}
