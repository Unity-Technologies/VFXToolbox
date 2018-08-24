Shader "VFXToolbox/ImageSequencer/Blur"
{
	Properties
	{
		_Radius("Radius", range(0.0,10.0)) = 3
		[Enum(RGBA,0,Alpha,1,RGB,2)] _ApplyBlur("Apply Blur", Int) = 0
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
			
			float _Radius;
			int _ApplyBlur;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			half4 blur(sampler2D s, float2 t, float2 size, float r)
			{
				int rad = r;
				half4 col = half4(0,0,0,0);

				float2 invSize = 1.0f/size;

				for(float i = -rad; i <= rad; i++)
				{
					for(float j = -rad; j <= rad; j++)
					{
						col += tex2D(s,t+(invSize*float2(i,j)));
					}				
				}
				return col / (((2*rad)+1)*((2*rad)+1));
			}
			
			half4 frag (v2f input) : SV_Target
			{
				half4 src = tex2D(_InputFrame,input.uv);
				half4 col = blur(_InputFrame, input.uv, _FrameData.xy, _Radius);

				half3 dstRGB = col.rgb;
				half dstA = col.a;

				if(_ApplyBlur == 1) dstRGB = src.rgb;
				if(_ApplyBlur == 2) dstA = src.a;

				return half4(dstRGB,dstA);
			}
			ENDCG
		}
	}
}
