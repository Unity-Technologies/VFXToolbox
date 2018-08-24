Shader "Hidden/VFXToolbox/ImageSequencer/AssembleBlit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_CC("Clipcoords", Vector) = (0.0,0.0,1.0,1.0)
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
				o.uv = v.uv;

				return o;
			}
			float4 _CC;

			fixed4 frag (v2f i) : SV_Target
			{
				float2 TexCoord = (i.uv - float2(_CC.x,_CC.y)) / float2(_CC.z, _CC.w);

				if(TexCoord.x < 1.0f && TexCoord.x > 0.0f && TexCoord.y < 1.0f && TexCoord.y > 0.0f)
				{
					float4 col = tex2D(_MainTex,TexCoord);	
					return col;
				}
				else clip(-1.0f);
				return float4(1.0f,0.0f,1.0f,1.0f);
				
			}
			ENDCG
		}
	}
}
