Shader "Hidden/VFXToolbox/ImageScripter/FlowMapPainter.DisplayFlow"
{
	Properties 
	{
	 _MainTex ("Texture", any) = "" {} 
	 _FlowTex ("FlowTex", any) = "" {}
	 _EdTime ("_EdTime", float) = 0.0
	 _Intensity("_Intensity", Float) = 0.2
	 _Cycle("_Cycle", Float) = 2.0
	 _Tile("Tile", Float) = 4.0
	} 

	SubShader {

		Tags { "ForceSupported" = "True" "RenderType"="Overlay" } 
		Lighting Off 
		Blend SrcAlpha OneMinusSrcAlpha 
		Cull Off 
		ZWrite Off 
		ZTest Always 
		
		Pass {	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			sampler2D _MainTex;
			sampler2D _FlowTex;
			float _EdTime;
			float _Intensity;
			float _Cycle;
			float _Tile;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				return o;
			}

			float triangleWave(float x, float phase)
			{
				return abs(frac(x/phase)-0.5)*2;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float tA = (_EdTime % _Cycle)-1.0f;
				float tB = ((_EdTime+(_Cycle/2)) % _Cycle)-1.0f;
				float tT = triangleWave(_EdTime, _Cycle);

				float2 flow = tex2Dlod(_FlowTex, float4(i.texcoord.x,i.texcoord.y,0,0)).rg - 0.5f;
				flow *= float2(1,-1) * (_Intensity / _Cycle);
				float3 colA = tex2Dlod(_MainTex, float4((i.texcoord-0.5) * _Tile - flow * tA,0,0)).rgb;
				float3 colB = tex2Dlod(_MainTex, float4((i.texcoord-0.5) * _Tile - flow * tB,0,0)).rgb;
				return float4(lerp(colA, colB, smoothstep(0,1,tT)),1);
			}
			ENDCG 
		}
	} 	
	Fallback off 
}
