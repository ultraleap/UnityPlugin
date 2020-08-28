﻿Shader "LeapMotion/Geometry/Wireframe"
{
    Properties
    {
        [PowerSlider(3.0)]
        _WireframeVal("Wireframe width", Range(0., 0.5)) = 0.1
        _FrontColor("Front color", color) = (1., 1., 1., 1.)
        _BackColor("Back color", color) = (1., 1., 1., 1.)
        [Toggle] _RemoveDiag("Remove diagonals?", Float) = 0.
    }
        SubShader
        {
            Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

            Pass
            {
                Cull Front
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma geometry geom

            // Change "shader_feature" with "pragma_compile" if you want set this keyword from c# code
            #pragma shader_feature __ _REMOVEDIAG_ON

            #include "UnityCG.cginc"

            struct v2g {
                float4 worldPos : SV_POSITION; 
            };

            struct g2f {
                float4 pos : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            v2g vert(appdata_base v) {

                v2g o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream) {
                float3 param = float3(0., 0., 0.);

                #if _REMOVEDIAG_ON
                float EdgeA = length(IN[0].worldPos - IN[1].worldPos);
                float EdgeB = length(IN[1].worldPos - IN[2].worldPos);
                float EdgeC = length(IN[2].worldPos - IN[0].worldPos);

                if (EdgeA > EdgeB && EdgeA > EdgeC)
                    param.y = 1.;
                else if (EdgeB > EdgeC && EdgeB > EdgeA)
                    param.x = 1.;
                else
                    param.z = 1.;
                #endif

                

                g2f o;
                o.pos = mul(UNITY_MATRIX_VP, IN[0].worldPos);
                o.bary = float3(1., 0., 0.) + param;
                triStream.Append(o);
                o.pos = mul(UNITY_MATRIX_VP, IN[1].worldPos);
                o.bary = float3(0., 0., 1.) + param;
                triStream.Append(o);
                o.pos = mul(UNITY_MATRIX_VP, IN[2].worldPos);
                o.bary = float3(0., 1., 0.) + param;
                triStream.Append(o);
            }

            float _WireframeVal;
            fixed4 _BackColor;

            fixed4 frag(g2f i) : SV_Target {

                float minBary = min(i.bary.x, min(i.bary.y, i.bary.z));
                float delta = fwidth(minBary);
                minBary = smoothstep(0, delta, minBary);

            if (!any(bool3(i.bary.x < _WireframeVal, i.bary.y < _WireframeVal, i.bary.z < _WireframeVal)))
                 discard;
            discard;
                return 1.0 - minBary;
            }

            ENDCG
        }

        Pass
        {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

                // Change "shader_feature" with "pragma_compile" if you want set this keyword from c# code
                #pragma shader_feature __ _REMOVEDIAG_ON

                #include "UnityCG.cginc"

                struct v2g {
                    float4 worldPos : SV_POSITION; 
                    float  cameraToSurfaceNormalAngle : PSIZE;

                };

                struct g2f {
                    float4 pos : SV_POSITION;
                    float3 bary : TEXCOORD0;
                    float4 lineColour : TEXCOORD1;
                };

                v2g vert(appdata_base v) {
                    v2g o;
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                    float3 worldNorm = UnityObjectToWorldNormal(v.normal);
                    float3 cameraForwardVector = mul((float3x3)unity_CameraToWorld, float3(0, 0, 1));

                    o.cameraToSurfaceNormalAngle = degrees(acos(dot(normalize(worldNorm), normalize(cameraForwardVector))));
                    return o;
                }

                [maxvertexcount(3)]
                void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream) {
                    float3 param = float3(0., 0., 0.);

                    #if _REMOVEDIAG_ON
                    float EdgeA = length(IN[0].worldPos - IN[1].worldPos);
                    float EdgeB = length(IN[1].worldPos - IN[2].worldPos);
                    float EdgeC = length(IN[2].worldPos - IN[0].worldPos);

                    if (EdgeA > EdgeB && EdgeA > EdgeC)
                        param.y = 1.;
                    else if (EdgeB > EdgeC && EdgeB > EdgeA)
                        param.x = 1.;
                    else
                        param.z = 1.;
                    #endif

                    g2f o;
                    o.lineColour = (float4) ((IN[0].cameraToSurfaceNormalAngle / 90) * float4(1, 1, 1, 1));

                    o.pos = mul(UNITY_MATRIX_VP, IN[0].worldPos);
                    o.bary = float3(1., 0., 0.) + param;
                    triStream.Append(o);
                    o.pos = mul(UNITY_MATRIX_VP, IN[1].worldPos);
                    o.bary = float3(0., 0., 1.) + param;
                    triStream.Append(o);
                    o.pos = mul(UNITY_MATRIX_VP, IN[2].worldPos);
                    o.bary = float3(0., 1., 0.) + param;
                    triStream.Append(o);

                    
                }

                float _WireframeVal;
                fixed4 _FrontColor;

                fixed4 frag(g2f i) : SV_Target {

                    float minBary = min(i.bary.x, min(i.bary.y, i.bary.z));
                    float delta = fwidth(minBary);
                    float4 minBrightness = float4(0.01, 0.01, 0.01, 0.01);
                    float maxIntensity = 0.6;

                    // Apply an antialiased look to the lines
                    minBary = smoothstep(0, 1.5*delta, minBary);
                 
                    // Discard anything that isn't part of the wireframe
                    if (minBary > 0.5)
                        discard;

                    if (!any(bool3(i.bary.x <= _WireframeVal, i.bary.y <= _WireframeVal, i.bary.z <= _WireframeVal)))
                         discard;

                    return ((1.0 - minBary) * i.lineColour * maxIntensity); // +minBrightness;

                }

                ENDCG
            }
        }
}