Shader "Ultraleap/Passthrough/Background"
{
    Properties
    {
        [MaterialToggle] _MirrorImageHorizontally ("MirrorImageHorizontally", Float) = 0
        _DeviceID ("DeviceID", Int) = 0
    }

    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal"
        }

        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Background"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            #define MAX_NUMBER_OF_GLOBAL_TEXTURES 6 // Same as MAX_NUMBER_OF_GLOBAL_TEXTURES in LeapImageRetriever.cs

            TEXTURE2D_ARRAY(_LeapGlobalRawTexture);
            SAMPLER(sampler_LeapGlobalRawTexture);

            TEXTURE2D_ARRAY(_LeapGlobalDistortion);
            SAMPLER(sampler_LeapGlobalDistortion);
            
            CBUFFER_START(UnityPerMaterial)
                float _LeapGlobalColorSpaceGamma;
                bool _MirrorImageHorizontally;
                int _DeviceID;
            CBUFFER_END

            CBUFFER_START(UnityPerFrame)
                float4 _LeapGlobalTextureSizes[MAX_NUMBER_OF_GLOBAL_TEXTURES];
                float2 _LeapGlobalRawPixelSize;
                float _LeapGlobalGammaCorrectionExponent;
                float4x4 _LeapGlobalWarpedOffset;
                float2 _LeapImageLeftUVOffset;
                float2 _LeapImageRightUVOffset;
                float4x4 _LeapHandTransforms[2];
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 positionSS : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            inline float DecodeFloatRG(float2 enc)
            {
                float2 kDecodeDot = float2(1.0, 1 / 255.0);
                return dot(enc, kDecodeDot);
            }

            float2 LeapGetUndistortedUVWithOffset(float4 positionSS, float2 uvOffset)
            {
                float2 screenUV = positionSS.xy / positionSS.w;
                
                #if UNITY_SINGLE_PASS_STEREO
	            float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
	            screenUV = (screenUV - scaleOffset.zw) / scaleOffset.xy;
                #endif

                screenUV = screenUV * 2 - float2(1, 1);
                
                float4 projection = float4(
                    UNITY_MATRIX_P[0][2],
                    UNITY_MATRIX_P[1][2],
                    UNITY_MATRIX_P[0][0],
                    UNITY_MATRIX_P[1][1]
                );

                // Flip vertically (seems to only be active on certain graphics pipelines)
                // Multiplication by _ProjectionParams.x is here for the case where this is negative
                projection.y = -projection.y;
                projection.w = -projection.w * _ProjectionParams.x;

                float2 tangent = (screenUV + projection.xy) / projection.zw;

                // Magic number alert: 0.125 == 1/8 is due to the fact that the distortion ray angles
                // in the distortion texture are also multiplied by 1/8.
                // It is also possible to query for the rayscale in LeapC, but platform doesn't do this,
                // so apparently we won't either.
                float2 distortionUV = saturate(0.125 * tangent + float2(0.5, 0.5)) * float2(1, 0.5) + uvOffset;

                // Invert the V coordinate on each distortion map to 'unflip' the IR image in the rendered view
                // (so it's the right way up)
                if (distortionUV.y <= 0.5)
                {
                    // Camera zero image sits in the lower half of the texture (V => 0-0.5)
                    distortionUV.y = 0.5 - distortionUV.y;
                }
                else if (distortionUV.y > 0.5 && distortionUV.y <= 1.0)
                {
                    // Camera one image sits in the upper half of the texture (V=> 0.5-1.0)
                    distortionUV.y = 1.0 - distortionUV.y + 0.5;
                }

                float4 distortionAmount = SAMPLE_TEXTURE2D_ARRAY(
                    _LeapGlobalDistortion,
                    sampler_LeapGlobalDistortion,
                    distortionUV,
                    positionSS.z
                );
                
                // A (32-bit) pixel from the distortion texture contains the X and Y (distortion lookup) coordinates
                // packed into a pair of bytes. DecodeFloatRG decodes the two 8-bit/channel RG values into a single
                // float between [0..1).
                //  
                // Note, the commit history for the magic numbers '2.3 - float2(0.6, 0.6)' is now lost, but Owen Morgen
                // suspects they are specific values used for the LMC to generate a zoom + crop + shift so it looks
                // decent in the VR Viz
                float2 leapUV = float2(
                    DecodeFloatRG(distortionAmount.xy),
                    DecodeFloatRG(distortionAmount.zw)) * 2.3 - float2(0.6, 0.6
                );
                return saturate(leapUV) * float2(1.0, 0.5 - _LeapGlobalRawPixelSize.y) + uvOffset;
            }

            float2 LeapGetLeftUndistortedUV(float4 positionSS)
            {
                return LeapGetUndistortedUVWithOffset(positionSS, float2(0.0, 0.0));
            }

            float2 LeapGetRightUndistortedUV(float4 positionSS)
            {
                return LeapGetUndistortedUVWithOffset(positionSS, float2(0.0, 0.5));
            }

            float2 LeapGetStereoUndistortedUV(float4 positionSS)
            {
                float offset = unity_StereoEyeIndex == 0 ? 0 : 0.5;
                return LeapGetUndistortedUVWithOffset(positionSS, float2(0.0, offset));
            }

            float4 LeapGetWarpedScreenPos(float4 transformedVertex)
            {
                float4 warpedPosition = mul(_LeapGlobalWarpedOffset, transformedVertex);
                return ComputeScreenPos(warpedPosition);
            }

            float4 LeapGetWarpedAndHorizontallyMirroredScreenPos(float4 transformedVertex)
            {
                // Clip space is from -1 to 1. We remap this value to 0-2, flip the value, then map it back to -1 to 1.
                transformedVertex.x = transformedVertex.x + 1;
                transformedVertex.x = 2 - transformedVertex.x;
                transformedVertex.x = transformedVertex.x - 1;
                return LeapGetWarpedScreenPos(transformedVertex);
            }

            float2 MapUVInPaddedTexture(float2 uv, int index)
            {
                if (uv.x > (_LeapGlobalTextureSizes[index].z - 1) / _LeapGlobalTextureSizes[index].z + 1 / (2 *
                    _LeapGlobalTextureSizes[index].z))
                {
                    uv.x = (_LeapGlobalTextureSizes[index].z - 1) / _LeapGlobalTextureSizes[index].z + 1 / (2 *
                        _LeapGlobalTextureSizes[index].z);
                }

                float u = (uv.x - 1 / (2 * _LeapGlobalTextureSizes[index].z))
                    * (_LeapGlobalTextureSizes[index].x - 1) / (_LeapGlobalTextureSizes[index].z - 1)
                    + 1 / (2 * _LeapGlobalTextureSizes[index].z);


                if (uv.y > (_LeapGlobalTextureSizes[index].w - 1) / _LeapGlobalTextureSizes[index].w + 1 / (2 *
                    _LeapGlobalTextureSizes[index].w))
                {
                    uv.y = (_LeapGlobalTextureSizes[index].w - 1) / _LeapGlobalTextureSizes[index].w + 1 / (2 *
                        _LeapGlobalTextureSizes[index].w);
                }

                float v = (uv.y - 1 / (2 * _LeapGlobalTextureSizes[index].w))
                    * (_LeapGlobalTextureSizes[index].y - 1) / (_LeapGlobalTextureSizes[index].w - 1)
                    + 1 / (2 * _LeapGlobalTextureSizes[index].w);

                return float2(u, v);
            }

            float3 LeapGetUVRawColor(float2 uv, float z)
            {
                // The textures have padding around them (to be able to use texture2DArray), 
                // that's why we need to adjust the uv value accordingly
                uv = MapUVInPaddedTexture(uv, int(z));
                float color = SAMPLE_TEXTURE2D_ARRAY(_LeapGlobalRawTexture, sampler_LeapGlobalRawTexture, uv, z).a;
                return color.xxx;
            }


            inline float3 LeapGetLeftRawColor(float4 positionSS)
            {
                return LeapGetUVRawColor(LeapGetLeftUndistortedUV(positionSS), positionSS.z);
            }

            inline float3 LeapGetRightRawColor(float4 positionSS)
            {
                return LeapGetUVRawColor(LeapGetRightUndistortedUV(positionSS), positionSS.z);
            }

            inline float3 LeapGetStereoRawColor(float4 positionSS)
            {
                return LeapGetUVRawColor(LeapGetStereoUndistortedUV(positionSS), positionSS.z);
            }

            inline float3 LeapGetUVColor(float2 uv, float z)
            {
                return pow(LeapGetUVRawColor(uv, z), _LeapGlobalGammaCorrectionExponent);
            }

            inline float3 LeapGetLeftColor(float4 positionSS)
            {
                return pow(LeapGetLeftRawColor(positionSS), _LeapGlobalGammaCorrectionExponent);
            }

            inline float3 LeapGetRightColor(float4 positionSS)
            {
                return pow(LeapGetRightRawColor(positionSS), _LeapGlobalGammaCorrectionExponent);
            }

            inline float3 LeapGetStereoColor(float4 positionSS)
            {
                return pow(LeapGetStereoRawColor(positionSS), _LeapGlobalGammaCorrectionExponent);
            }


            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);

                if (_MirrorImageHorizontally)
                {
                    o.positionSS = LeapGetWarpedAndHorizontallyMirroredScreenPos(o.positionCS);
                }
                else
                {
                    o.positionSS = LeapGetWarpedScreenPos(o.positionCS);
                }

                // Use the Z component as the index for the texture array.
                o.positionSS.z = _DeviceID + 0.1;

                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                if (unity_StereoEyeIndex == 0)
                {
                    return half4(LeapGetLeftColor(i.positionSS), 1);
                }
                else
                {
                    return half4(LeapGetRightColor(i.positionSS), 1);
                }
            }
            ENDHLSL
        }

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

        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"
            #include "Packages/com.ultraleap.tracking/Core/Runtime/Resources/LeapCG.cginc"

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

                if (_MirrorImageHorizontally)
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

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                return float4(i.stereoEyeIndex == 0 ? LeapGetLeftColor(i.screenPos) : LeapGetRightColor(i.screenPos),
          1);
            }
            ENDCG
        }
    }

    Fallback Off
}