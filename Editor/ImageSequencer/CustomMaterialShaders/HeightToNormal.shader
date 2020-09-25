Shader "VFXToolbox/ImageSequencer/HeightToNormal"
{
	Properties
	{
		[Enum(Red,0,Green,1,Blue,2,Alpha,3)] _HeightSource("Input Channel for Height Source", Int) = 3
		[Enum(Red,0,Green,1,Blue,2,Alpha,3,None,4)] _AlphaChannel("Input Channel for Alpha Output", Int) = 3			
		_HeightScale("Height Scale", Range(0.0,150.0)) = 25
		_Radius("Radius", Range(1.0,5.0)) = 1.0
		_Spherize("Spherize", Range(0.0,1.0)) = 0.0
		[Enum(Low,4,Medium,8,High,16)] _NumSamples("Sample Count", Int) = 8
	
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

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

			// ImageSequencer CustomMaterial uniforms
			// ======================================
			//
			// sampler2D _InputFrame;			// Input Frame (from previous sequence)
			//
			// float4 _FrameData;				// Frame Data
			//									//		x, y : width (x) and height (y) in pixels
			//									//		z, w : sequence index (z) and length (w)
			//
			// float4 _FlipbookData;			// Flipbook Data
			//									//		x, y : number of columns (x) and rows (y)
			//									//		z, w : (unused)

			sampler2D _InputFrame;
			float4 _FrameData;
			float4 _FlipbookData;

			int _HeightSource;
			int _AlphaChannel;
			int _NumSamples;
			float _HeightScale;
			float _Radius;
			float _Spherize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			half3 position(sampler2D s, float2 t)
			{
				half4 w = tex2D(s,t);
				float h;
				if(_HeightSource == 0) h = w.r;
				if(_HeightSource == 1) h = w.g;
				if(_HeightSource == 2) h = w.b;
				if(_HeightSource == 3) h = w.a;

				float x = t.x *_FrameData.x;
				float y = t.y *_FrameData.y;
				float z = h *_HeightScale;

				// Spherize
				float2 ssc = float2(t.x - 0.5f , t.y - 0.5f) * 1.414213562373095f;
				z += _Spherize * (1.0f - dot(ssc, ssc)) * 200;

				return float3(x, y, z);
			}

			half3 normal(sampler2D s, float2 t, float r)
			{
				float2 invSize = 1.0f/_FrameData.xy;
				float3 pos = position(s,t);
				float3 nrm = float3(0,0,0);

				float step = UNITY_PI*2*(1.0/(_NumSamples*2));
				int numRings = ceil(r);

				for(int j = 1; j <= numRings ; j++)
				{
					float rad = r * ((float)j/numRings);

					for(int i = 0; i < (_NumSamples*2); i+=2)
					{
						float angle = i*step;
						float2 offset = float2(cos(angle),sin(angle)) * invSize * rad;
						float3 sampleA = position(s, t+offset);

						angle = (i+1)*step;
						offset = float2(cos(angle),sin(angle)) * invSize * rad;
						float3 sampleB = position(s, t+offset);

						nrm += normalize(cross(sampleA-pos, sampleB-pos));
					}	
				}
				nrm = normalize(nrm);
				//nrm += float3(_Spherize * pow(float2(t - 0.5f)*2.0f,5.0f),0.0f);
				return nrm;
			}
			
			half4 frag (v2f input) : SV_Target
			{
				float alpha;
				half4 col = tex2D(_InputFrame,input.uv);
				if(_AlphaChannel == 0) alpha = col.r;
				if(_AlphaChannel == 1) alpha = col.g;
				if(_AlphaChannel == 2) alpha = col.b;
				if(_AlphaChannel == 3) alpha = col.a;
				if(_AlphaChannel == 4) alpha = 1.0f;

				half3 nrml = normal(_InputFrame, input.uv, _Radius);
				return half4((nrml * 0.5)+0.5,alpha);
			}
			ENDCG
		}
	}
}
