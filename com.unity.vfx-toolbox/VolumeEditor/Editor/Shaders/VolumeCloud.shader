// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Hidden/VFXToolbox/VolumeEditor/VolumeCloud"
{
	Properties
	{
		_Volume ("_Volume", 3D) = "white" {}
		_CameraWorldPosition("_CameraWorldPosition", Vector) = (1,1,1,1)
		_LightDirection ("_LightDirection", Vector) = (0,-1,0,1)
		_DensityScale ("_DensityScale", Float) = 0.1
		_ScatteringScale ("_ScatteringScale", Float) = 1.0
		_EditorTime("_EditorTime", Float) = 0.0
	}
	SubShader
	{
		Tags{"RenderType" = "Transparent" "Queue" = "Transparent"}
		LOD 100
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Back
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			#define NB_VOLUME_SAMPLES 64
			#define NB_LIGHT_SAMPLES 64
			#define DISPLAY_NB_SAMPLES 0
			#define	DITHER_NOISE 0

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 worldpos : TEXCOORD0;
			};

			sampler3D _Volume;
			float3 _CameraWorldPosition;
			float3 _LightDirection;
			float _DensityScale;
			float _ScatteringScale;
			float _EditorTime;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldpos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

			float simpleNoise(float2 uv)
			{
				return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
			}

			float dither4x4(float2 position, float time)
			{
				const float kernel[16] = {0.0625, 0.5625, 0.1875, 0.6875, 0.8125, 0.3125, 0.9375, 0.4375, 0.25, 0.75, 0.125, 0.625, 1.0, 0.5, 0.875, 0.375};
				float v = kernel[(((int)position.y & 3) << 2) + ((int)position.x & 3)];
#if DITHER_NOISE
				return frac(v + simpleNoise(position) + time);
#else
				return v;
#endif
			}

			float invGaussian(float x, float H)
			{
				return 1.0 - (1.0 / pow(H*x + 1.0,2.0));
			}

			float LightMarch(sampler3D volume, float3 position, int maxSteps)
			{
				float3 dir = -normalize(_LightDirection);
				float acc = 1.0f;
				float step = 3.4641 / maxSteps;

				for (int i = 0; i < maxSteps; i++)
				{
					float f = 3.4641 * (float)i / maxSteps;
					float3 sampleWorldPos = position + f * dir;
					float3 samplePos = sampleWorldPos  + 0.5;

					acc *= 1.0f - saturate(tex3Dlod(volume, float4(samplePos, 0.0)) * _DensityScale * _ScatteringScale * step);

					if (abs(samplePos.x - 0.5) > 0.5 || abs(samplePos.y - 0.5) > 0.5 || abs(samplePos.z - 0.5) > 0.5) break;
					if (acc < 0.1f) return 0.1f;
				}
				return acc;
			}

			float4 RayMarch(sampler3D volume, float3 dir, float3 position, int maxSteps, float dither)
			{
				float3 outColor = float3(-1.0f, -1.0f, -1.0f);
				float step = 3.4641 / maxSteps;

				int i = 0;
				float acc = 0.0f;

				for (i = 0; i < maxSteps; i++)
				{
					float f = 3.4641 * ((float)i + dither) / (float)maxSteps;

					float3 sampleWorldPos = position + f * dir;
					float3 samplePos = sampleWorldPos + 0.5;

					if (abs(samplePos.x - 0.5) > 0.5 || abs(samplePos.y - 0.5) > 0.5 || abs(samplePos.z - 0.5) > 0.5) break;

					float a = tex3Dlod(volume, float4(samplePos, 4.0)) * _DensityScale * step;

					if (a > 0.001f)
					{
						float l = LightMarch(volume, sampleWorldPos, NB_LIGHT_SAMPLES);
						if (outColor.r < 0.0f)
							outColor = float3(l, l, l);
						else
							outColor = lerp( outColor, float3(l, l, l), a * saturate(1.0f-acc));
						acc += a;
					}

					if (acc > 1.0f) break;

				}

#if DISPLAY_NB_SAMPLES
					return (float4(1, 1, 1, 1) / NB_VOLUME_SAMPLES) * (float)i;
#else
					return float4(outColor, acc);
#endif
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float f = dither4x4(i.vertex.xy, _EditorTime);
				fixed4 col = RayMarch(_Volume, -normalize(_CameraWorldPosition - i.worldpos), i.worldpos, NB_VOLUME_SAMPLES, f);
				return col;
			}
			ENDCG
		}
	}
}
