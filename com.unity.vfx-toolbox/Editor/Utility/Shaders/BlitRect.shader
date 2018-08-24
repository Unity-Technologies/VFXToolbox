Shader "Hidden/VFXToolbox/BlitRect"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Rect ("Rect", Vector) = (0.0,0.0,1.0,1.0)
	}
	SubShader 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 100
		
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha 
		
		Pass {  
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				
				#include "UnityCG.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					half2 texcoord : TEXCOORD0;
				};

				sampler2D _MainTex;
				float4 _Rect;
				
				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex = v.vertex;
					o.vertex.xy = (v.vertex.xy / _Rect.zw) - _Rect.xy;
					o.texcoord = o.vertex;
					o.vertex.y = 1.0f - o.vertex.y;
					o.vertex = o.vertex * 2 - 1;
					return o;
				}
				
				fixed4 frag (v2f i) : SV_Target
				{
					fixed4 col = tex2D(_MainTex, i.texcoord);
					return col;
				}
			ENDCG
		}
	}

}
