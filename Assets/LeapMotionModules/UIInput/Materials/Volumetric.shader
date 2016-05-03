Shader "Volumetric Light/Uniform Density" {
	Properties {
    _Color ("Color", Color) = (1,1,1,1)
    _Power ("Brightness", Float) = 1
	}

  CGINCLUDE
  #include "UnityCG.cginc"

  uniform sampler2D _CameraDepthTexture;

  struct vert_in {
    float4 vertex : POSITION;
  };

  struct fragment_input{
		float4 position : SV_POSITION;
    float4 screenPos : TEXCOORD0;
	};

	fragment_input vert(vert_in v) {
		fragment_input o;
		o.position = mul(UNITY_MATRIX_MVP, v.vertex);
    o.screenPos = ComputeScreenPos(o.position);
		return o;
	}

  uniform float4 _Color;
  uniform float _Power;

	float4 frag(fragment_input input) : COLOR {
    float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(input.screenPos)));
    float distanceToCamera = min(sceneZ, input.screenPos.z);
		return distanceToCamera * _Color * _Power;
	}

  ENDCG

  SubShader {
    Tags {"Queue"="Transparent"}

    Pass{
      Cull Back
      ZWrite Off
      ZTest Off
      BlendOp RevSub
      Blend One One

      CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
      ENDCG
    }

    Pass{
      Cull Front
      ZWrite Off
      ZTest Off
      BlendOp Add
      Blend One One

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
    }
  } 

  Fallback Off
}
