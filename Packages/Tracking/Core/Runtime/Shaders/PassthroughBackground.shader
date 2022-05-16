Shader "LeapMotion/Passthrough/Background" {
	Properties
	{
		[Toggle] _MirrorImageHorizontally ("MirrorImageHorizontally", Float) = 0
		_DeviceID ("DeviceID", Int) = 0
	}
	SubShader{
	  Tags {"Queue" = "Background" "IgnoreProjector" = "True"}

	  Cull Off
	  Zwrite Off
	  Blend One Zero

	  Pass{
	  CGPROGRAM
	  #include "../Resources/LeapCG.cginc"
	  #include "UnityCG.cginc"

	  #pragma target 3.0

	  #pragma vertex vert
	  #pragma fragment frag

	  uniform float _LeapGlobalColorSpaceGamma;
	  float _MirrorImageHorizontally;
	  int _DeviceID;

	  struct frag_in {
		float4 position : SV_POSITION;
		float4 screenPos  : TEXCOORD1;
		int stereoEyeIndex : TEXCOORD2;

		UNITY_VERTEX_OUTPUT_STEREO
	  };

	  frag_in vert(appdata_img v) {
		frag_in o;

		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_INITIALIZE_OUTPUT(frag_in, o);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		o.position = UnityObjectToClipPos(v.vertex);
		if(_MirrorImageHorizontally)
		{
			o.screenPos = LeapGetWarpedAndHorizontallyMirroredScreenPos(o.position);
		}
		else
		{
			o.screenPos = LeapGetWarpedScreenPos(o.position);
		}

		// set z as the index for the texture array
		o.screenPos.z = _DeviceID + 0.1;

		o.stereoEyeIndex = unity_StereoEyeIndex;

		return o;
	  }

	  float4 frag(frag_in i) : COLOR {

		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		return float4(i.stereoEyeIndex == 0 ? LeapGetLeftColor(i.screenPos) : LeapGetRightColor(i.screenPos), 1);
	  }

	  ENDCG
	  }
	}
		Fallback off
}
