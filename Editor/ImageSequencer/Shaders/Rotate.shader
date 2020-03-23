Shader "Hidden/VFXToolbox/ImageSequencer/Rotate"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Mode("Mode", Int) = 0
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
			int _Mode;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				float U = v.uv.x;
				float V = v.uv.y;
				o.uv = v.uv;
				if(_Mode == 1) {o.uv = float2(1.0f-V,U); }
				else if(_Mode == 2) {o.uv = float2(1.0f-U,1.0f-V); }
				else if(_Mode == 3) {o.uv = float2(V,1.0f-U); }
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				//return float4(i.uv.x, i.uv.y, 0.0f,1.0f);
				return tex2D(_MainTex, i.uv);
			}
			ENDCG
		}
	}
}
