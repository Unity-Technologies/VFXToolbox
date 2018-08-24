Shader "Hidden/VFXToolbox/ImageSequencer/ColorCorrection"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Brightness("_Brightness", Float) = 1.0
		_Contrast("_Contrast", Float) = 1.0
		_Saturation("_Saturation", Float) = 1.0
		_AlphaCurve("_AlphaCurve", 2D) = "white" {}
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
	sampler2D _AlphaCurve;
	float _Brightness;
	float _Contrast;
	float _Saturation;

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
	}

	float3 Contrast(float3 color, float contrast)
	{
		return lerp(0.5f, color, contrast);
	}

	// Saturation (should be used after offset/power/slope)
	// Recommended workspace: ACEScc (log)
	// Optimal range: [0.0, 2.0]
	//
	float3 Saturation(float3 c, float sat)
	{
		float luma = dot(c, half3(0.2126, 0.7152, 0.0722));
		return max(0.0, luma.xxx + sat * (c - luma.xxx));
	}


	half4 frag(v2f i) : SV_Target
	{
		float4 input = tex2D(_MainTex, i.uv);

		float3 color = input.rgb * _Brightness;

		color = Contrast(color, _Contrast);

		float alpha = input.a;

		color = Saturation(color, _Saturation);

		alpha = tex2Dlod(_AlphaCurve, float4(alpha, 0, 0, 0));

		return float4(color,alpha);
	}
		ENDCG
	}
	}
}
