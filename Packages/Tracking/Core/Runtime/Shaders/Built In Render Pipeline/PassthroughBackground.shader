Shader "Ultraleap/Passthrough/Background"
{
    Properties
    {
        [MaterialToggle] _MirrorImageHorizontally ("MirrorImageHorizontally", Float) = 0
        _DeviceID ("DeviceID", Int) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "IgnoreProjector" = "True"
        }

        Cull Off
        Zwrite Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"
            #include "Packages/com.ultraleap.tracking/Core/Runtime/Resources/LeapCG.cginc"

            #pragma shader_feature _MIRROR_IMAGE_HORIZONTALLY_ON
            
            #pragma vertex vert
            #pragma fragment frag
            
            uniform float _LeapGlobalColorSpaceGamma;
            float _MirrorImageHorizontally;
            int _DeviceID;

            struct v2f
            {
                float4 position : SV_POSITION;
                float4 screenPos : TEXCOORD1;
                int stereoEyeIndex : TEXCOORD2;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_img v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.position = UnityObjectToClipPos(v.vertex);

                #if _MIRROR_IMAGE_HORIZONTALLY_ON
                o.screenPos = LeapGetWarpedAndHorizontallyMirroredScreenPos(o.position);
                #else
                o.screenPos = LeapGetWarpedScreenPos(o.position);
                #endif

                // set z as the index for the texture array
                o.screenPos.z = _DeviceID + 0.1;
                o.stereoEyeIndex = unity_StereoEyeIndex;

                return o;
            }

            float4 frag(v2f i) : COLOR
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                return float4(i.stereoEyeIndex == 0 ? LeapGetLeftColor(i.screenPos) : LeapGetRightColor(i.screenPos), 1);
            }
            ENDCG
        }
    }

    Fallback Off
}