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


			// base structs
			struct vertexInput
			{
				float4 vertex: POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct vertexOutput
			{
				float4 pos: SV_POSITION;
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

				return o;
			}

			// fragment function
			float4 frag(vertexOutput i) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(i);
                return UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
			}

			ENDCG
		}
	}

}