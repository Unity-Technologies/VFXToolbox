Shader "VFXToolbox/ImageSequencer/LightNormals"
{
	Properties
	{
		_Orientation("Orientation", Range(0.0,360.0)) = 0.0
		_GrazingAngle("Grazing Angle", Range(0.0,90.0)) = 0.0
		_BaseColor("Base Color", Color) = (1.0,1.0,1.0,1.0)
		_AmbientColor("Ambient Color", Color) = (0.1,0.15,0.2,1.0)			
		_LightColor("Light Color", Color) = (1.0,1.0,1.0,1.0)
		_LightBrightness("Light Brightness", Range(0.0,10.0)) = 1.0
		_LightExponent("Light Exponent", Range(1.0, 10.0)) = 2.0
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

			float _Orientation;
			float _GrazingAngle;
			float4 _BaseColor;
			float4 _LightColor;
			float _LightBrightness;
			float _LightExponent;
			float4 _AmbientColor;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			half3 latLonToXYZ(float lat, float lon)
			{
				lat = (lat / 360) * 2 * UNITY_PI;
				lon = (lon / 360) * 2 * UNITY_PI;

				float x = cos(lat)*cos(lon);
				float y = cos(lat)*sin(lon);
				float z = sin(lat);	

				return half3(x,y,z);		
			}

			half4 frag (v2f i) : SV_Target
			{
				half4 tex = tex2D(_InputFrame, i.uv);
				half3 nrm = tex.rgb*2-1;

				half3 lightdir = latLonToXYZ(_GrazingAngle, _Orientation);

				half3 color = _BaseColor*((pow(saturate(dot(nrm,lightdir)*0.5+0.5),_LightExponent) * _LightColor * _LightBrightness) + _AmbientColor);
				
				return half4(color,tex.a);
			}
			ENDCG
		}
	}
}
