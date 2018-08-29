// Based on 3d Value noise (iQ https://www.shadertoy.com/view/4sfGzS)
Shader "VFXToolbox/ImageScripter/NoiseGen"
{
	Properties
	{
		_depth("Noise Depth", Int) = 5
		_width("Width", Int) = 256
		_height("Height", Int) = 256
		_phase("Phase", Float) = 0.0
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		ColorMask RGB
		Lighting Off
		Cull Off
		ZWrite Off
		ZTest Always
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "SimplexNoise2D.hlsl"

			int _depth;
			int _width;
			int _height;
			float _phase;

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
			
			half4 frag (v2f i) : SV_Target
			{
		        const float epsilon = 0.0001;

		        float2 uv = i.uv * 4.0 + float2(0.2, 1) * _phase;
		        float2 o = 0.5;
				float s = 1.0;
				float w = 0.5;

		        for (int i = 0; i < _depth + 3; i++)
		        {

	                float2 coord = uv * s;
	                float2 period = s * 2.0;

                    float v0 = snoise(coord);
                    float vx = snoise(coord + float2(epsilon, 0));
                    float vy = snoise(coord + float2(0, epsilon));

                    o += w * float2(vx - v0, vy - v0) / epsilon;

		            s *= 2.0;
		            w *= 0.5;
			    }
            	return float4(o.x, o.y, 0, 1);
			}
			ENDCG
		}
	}
}
