Shader "VFXToolbox/ImageSequencer/Negative"
{
	Properties
	{
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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				half4 col = tex2D(_InputFrame, i.uv);
				half3 rgb = LinearToGammaSpace(col.rgb);
				return half4(GammaToLinearSpace(1-rgb),col.a);
			}
			ENDCG
		}
	}
}
