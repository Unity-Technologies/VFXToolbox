Shader "Hidden/VFXToolbox/ImageSequencer/RemapColor"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Gradient("_Gradient", 2D) = "white" {}
		_Mode("_Mode", Int) = 0
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
	sampler2D _Gradient;
	int _Mode;

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
	}


	half4 frag(v2f i) : SV_Target
	{
		float4 input = tex2D(_MainTex, i.uv);
		float g;
		if (_Mode < 2)
		{
			float3 w = input.rgb * half3(0.2126, 0.7152, 0.0722);
			g = w.r + w.g + w.b;
			if (!IsGammaSpace() && _Mode == 0)
			{
				g = LinearToGammaSpaceExact(g);
			}
		}
		else
		{
			switch (_Mode)
			{
				case 2:	g = input.a; break;
				case 3:	g = input.r; break;
				case 4:	g = input.g; break;
				case 5:	g = input.b; break;
			}

		}

		return tex2Dlod(_Gradient, float4(g, 0, 0, 0));
	}
		ENDCG
	}
	}
}
