//https://learn.unity.com/tutorial/custom-render-passes-with-urp#

Shader "Toon/Basic Outline" 
{
	Properties 
	{
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline width", Range (.0001, 0.03)) = .005
        _MainTex("Color (RGB) Alpha (A)", 2D) = "white"
	}
	SubShader 
	{
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		
		Cull Front
        ZWrite On
        Blend One OneMinusSrcAlpha
		
		Pass 
		{
			Name "OUTLINE"
			
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			
            CBUFFER_START(UnityPerMaterial)
            float _Outline;
            float4 _OutlineColor;
            sampler2D _MainTex;
            CBUFFER_END
			
            struct Attributes 
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
        
            struct Varyings 
            {
                float4 positionCS : SV_POSITION;
                half fogCoord : TEXCOORD0;
                float4 color : COLOR;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings vert(Attributes input) 
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
	
                input.positionOS.xyz += input.normalOS.xyz * _Outline;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                
                float4 color = tex2Dlod(_MainTex, float4(input.uv, 0, 1));
                output.color = _OutlineColor * color.a;
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                return output;
            }
			
            float4 frag(Varyings i) : SV_Target
            {
                i.color.rgb = MixFog(i.color.rgb, i.fogCoord);

				return i.color;

			}
            ENDHLSL
		}
	}
}
