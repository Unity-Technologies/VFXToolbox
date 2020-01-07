Shader "Hidden/VFXToolbox/ImageSequencer/PremultiplyAlpha"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_RemoveAlpha("_RemoveAlpha", Int) = 0
		_AlphaValue("_AlphaValue", Float) = 1.0
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
	int _RemoveAlpha;
	float _AlphaValue;

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
	}

	float4 frag(v2f i) : SV_Target
	{
		float4 color = tex2D(_MainTex, i.uv);
		color.rgb *= color.a;
		
		if(_RemoveAlpha)
			color.a = _AlphaValue;

		return color;
	}
		ENDCG
	}
	}
}
