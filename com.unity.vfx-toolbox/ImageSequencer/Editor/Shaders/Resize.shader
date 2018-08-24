Shader "Hidden/VFXToolbox/ImageSequencer/Resize"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_KernelAndSize("_KernelAndSize", Vector) = (1.0,1.0,1.0,1.0)
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
			#define MAX_KERNEL_SAMPLES 16

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
			float4 _KernelAndSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 ratio = _KernelAndSize.xy;
				float2 outSize = _KernelAndSize.zw;
				float2 inSize = ratio * outSize;
				float2 kernelSize = 1.0f / outSize;
				float2 texcoord = i.uv;

				int NUM_U = (int)ceil(ratio.x);
				int NUM_V = (int)ceil(ratio.y);

				float4 c = float4(0, 0, 0, 0);

				for (int i = 0; i < NUM_U; i++)
					for (int j = 0; j < NUM_V; j++)
					{
						float2 offset = float2((float)(i+1) / (NUM_U+1),(float)(j+1) / (NUM_V+1))-0.5f;
						float2 samplePos = texcoord + (kernelSize * offset);
						c += tex2Dlod(_MainTex, float4(samplePos, 0, 0)); 
					}

				c /= (NUM_U * NUM_V);

				return c;
			}
			ENDCG
		}
	}
}
