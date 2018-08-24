Shader "VFXToolbox/ImageSequencer/Rotate"
{
	Properties
	{
		_Angle("Angle", Range(0.0,360.0)) = 0.0
		_RotationCenter("Center of Rotation", Vector) = (0.5,0.5,0.0,0.0)
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

			float _Angle;
			float2 _RotationCenter;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			float2 rotate2D(float2 TexCoord, float2 Center, float Angle) {
					
					float2 AngleCoords = float2(sin(Angle%(2*UNITY_PI)),cos(Angle%(2*UNITY_PI)));
					TexCoord -= Center;
					return Center +
							float2(
								TexCoord.x*AngleCoords.y - TexCoord.y * AngleCoords.x,
							 	TexCoord.x*AngleCoords.x + TexCoord.y * AngleCoords.y
							 	);
			}

			half4 frag (v2f i) : SV_Target
			{
				float2 nm = floor(i.uv*_FlipbookData.xy)/_FlipbookData.xy;

				float2 frameuv = frac(i.uv * _FlipbookData.xy);

				frameuv = saturate(rotate2D(frameuv, _RotationCenter, (_Angle / 360) * UNITY_PI * 2));

				half4 col = tex2D(_InputFrame, (frameuv/_FlipbookData.xy)+nm);
				return col;
			}
			ENDCG
		}
	}
}
