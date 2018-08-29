Shader "Hidden/VFXToolbox/VolumeEditor/VolumeTrail"
{
	Properties
	{
		_Volume("_Volume", 3D) = "white" {}
		_NumPointsPerTrail("_NumPointsPerTrail", Int) = 2
		_ReduceFactor("_ReduceFactor", Int) = 1
		_Length("_Length", Float) = 0.1
		_HeatMap("_HeatMap", 2D) = "white" {}
		_HeatMapScale("_HeatMapScale", Float) = 1.0
		_Dimensions("_Dimensions", Vector) = (1.0,1.0,1.0,1.0)
		_Offset("_Offset", Vector) = (0.0,0.0,0.0,0.0)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR0;
			};

			sampler3D _Volume;
			sampler2D _HeatMap;
			uint _NumPointsPerTrail;
			uint _ReduceFactor;
			float _Length;
			float _HeatMapScale;
			float4 _Dimensions;
			float4 _Offset;

			bool Reduce(float3 pos)
			{
				uint x = floor(pos.x * _Dimensions.x);
				uint y = floor(pos.y * _Dimensions.y);
				uint z = floor(pos.z * _Dimensions.z);
				return ((x % _ReduceFactor) > 0) || ((y % _ReduceFactor) > 0) || ((z % _ReduceFactor) > 0);
			}

			v2f vert (appdata v, uint id : SV_VertexID)
			{
				v2f o;
				float3 pos = v.vertex + 0.5f;
				float4 mag;
				if (!Reduce(pos))
				{
					pos += _Offset;
					pos += (_ReduceFactor-1) / (_Dimensions * 2);
					for (uint i = 0; i < id % _NumPointsPerTrail; i++)
					{
						float3 val = tex3Dlod(_Volume, float4(pos, 0.0f) + 0.5f/_Dimensions) - 0.5f;
						pos += val * _Length;
						mag = tex2Dlod(_HeatMap, float4(pow(length(val), 2)*_HeatMapScale, 0.0f, 0, 0));
					}
					o.color = mag;
					o.vertex = UnityObjectToClipPos(float4(pos - 0.5, v.vertex.w));
				}
				else
				{
					o.vertex = float4(2, 2, 0, 1);
					o.color = float4(0, 0, 0, 0);
				}

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return i.color;
			}
			ENDCG
		}
	}
}
