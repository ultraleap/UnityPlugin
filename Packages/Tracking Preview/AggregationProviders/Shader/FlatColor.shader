Shader "FlatColor"{
	Properties
	{
		_Color("Color", Color) = (1.0,1.0,1.0,1.0)
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Pass {
			CGPROGRAM

			// pragmas
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"

			//// user defined variables
			//uniform float4 _Color;


			// base structs
			struct vertexInput
			{
				float4 vertex: POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct vertexOutput
			{
				float4 pos: SV_POSITION;
				float2 depth : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

			// vertex function
			vertexOutput vert(vertexInput v)
			{
				vertexOutput o;

				UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.pos = UnityObjectToClipPos(v.vertex);

				//o.depth = -mul(UNITY_MATRIX_MV, v.vertex).z * _ProjectionParams.w;
				o.depth = o.pos.z / o.pos.w;

				return o;
			}

			// fragment function
			float4 frag(vertexOutput i) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(i);
				fixed invert = (i.depth);
				//return fixed4(invert, invert, invert,1);
                return UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
			}

			ENDCG
		}
	}

}