Shader "Custom/AsteroidRing" {
    Properties {
        _SunColor ("Sun Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _RingMap ("Map", 2D) = "White" {}
        _PlanetRadius ("Planet Radius", Float) = 100
        _PlanetRadiusOffsetFactor ("Planet Radius Offset Factor", Float) = 1.5
        _PlanetRadiusMaxSpreadFactor ("Planet Radius Max Spread Factor", Float) = 0.05
    }

    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Opaque" }
        Cull Off
        ZWrite Off

        Blend SrcAlpha One
        LOD 100

        Pass {
            CGPROGRAM
            #include "UnityCG.cginc"

            #define PI 3.1415926535897932384626433832795

            #pragma vertex vert
            #pragma fragment frag

            struct vertexData {
                float4 vertex : POSITION;
            };

            struct fragmentData {
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD0;
                float4 objPos : TEXCOORD1;
            };

            uniform float4 _PlanetSunLightDirection;
            float4 _SunColor;
            sampler2D _RingMap;
            float _PlanetRadius;
            float _PlanetRadiusOffsetFactor;
            float _PlanetRadiusMaxSpreadFactor;

            fragmentData vert(vertexData vertex) {
                fragmentData fragment;
                fragment.vertex = UnityObjectToClipPos(vertex.vertex);
                fragment.worldPos = mul(unity_ObjectToWorld, vertex.vertex);
                fragment.objPos = vertex.vertex;
                return fragment;
            }

            float3x3 CreateOrthonormalBasis(float3 baseVectorX, float3 inputVectorY) {
                baseVectorX = normalize(baseVectorX);
                inputVectorY = normalize(inputVectorY);
                float3 orthogonalVectorZ = normalize(cross(baseVectorX, inputVectorY));
                inputVectorY = normalize(cross(orthogonalVectorZ, baseVectorX));

                return float3x3(baseVectorX, inputVectorY, orthogonalVectorZ);
            }

            void CalculateClosestPoints(in float3 startPointA, in float3 directionA, in float3 startPointB, in float3 directionB, out float3 closestPointA, out float3 closestPointB) {
                float3x3 basisMatrix = CreateOrthonormalBasis(directionA, directionB);
                startPointA = mul(basisMatrix, startPointA);
                directionA = mul(basisMatrix, directionA);
                startPointB = mul(basisMatrix, startPointB);
                directionB = mul(basisMatrix, directionB);

                float2 results = mul(
                    float2(startPointA.x * directionA.y - startPointA.y * directionA.x, startPointB.x * directionB.y - startPointB.y * directionB.x),
                    float2x2(directionB.xy, -directionA.xy)
                );
                results /= directionB.x * directionA.y - directionA.x * directionB.y;

                startPointA.xy = results;
                startPointB.xy = results;

                closestPointA = mul(startPointA, basisMatrix);
                closestPointB = mul(startPointB, basisMatrix);
            }

            float3 ApplyAcesFilmicTonemap(float3 hdrColor) {
                // ACES tone mapping coefficients
                const float exposureBias = 2.51f;
                const float linearSectionStart = 0.03f;
                const float linearWhitePoint = 2.43f;
                const float linearWhiteLevel = 0.59f;
                const float darkCompression = 0.14f;
                const float gamma = 1.3f;

                // channel mixing to simulate cone response overlap
                const float redGreenOverlap = 0.1f * 0.2f;
                const float redBlueOverlap = 0.01f * 0.2f;
                const float greenBlueOverlap = 0.04f * 0.2f;

                const float3x3 coneResponseOverlap = float3x3(
                    float3(1.0f, redGreenOverlap, redBlueOverlap),
                    float3(redGreenOverlap, 1.0f, greenBlueOverlap),
                    float3(redBlueOverlap, redGreenOverlap, 1.0f)
                );

                const float3x3 coneResponseInverse = float3x3(
                    float3(1.0f + (redGreenOverlap + redBlueOverlap), -redGreenOverlap, -redBlueOverlap),
                    float3(-redGreenOverlap, 1.0f + (redGreenOverlap + greenBlueOverlap), -greenBlueOverlap),
                    float3(-redBlueOverlap, -redGreenOverlap, 1.0f + (redBlueOverlap + redGreenOverlap))
                );

                // apply cone response matrix
                hdrColor = mul(coneResponseOverlap, hdrColor);
                // apply gamma correction
                hdrColor = pow(hdrColor, float3(gamma, gamma, gamma));
                // apply the ACES tone mapping curve
                hdrColor = (hdrColor * (exposureBias * hdrColor + linearSectionStart)) / 
                        (hdrColor * (linearWhitePoint * hdrColor + linearWhiteLevel) + darkCompression);
                // inverse gamma correction
                hdrColor = pow(hdrColor, float3(1.0f, 1.0f, 1.0f) / gamma);
                // reverse the cone response matrix effect
                hdrColor = mul(coneResponseInverse, hdrColor);
                // clamp the final color to the standard dynamic range
                hdrColor = clamp(hdrColor, float3(0.0f, 0.0f, 0.0f), float3(1.0f, 1.0f, 1.0f));

                return hdrColor;
            }

            fixed4 frag(fragmentData fragment) : SV_Target {
                // normalize light and view directions
                float3 lightDirection = normalize(-_PlanetSunLightDirection.xyz);
                float3 viewerDirection = normalize(_WorldSpaceCameraPos - fragment.worldPos);
                float3 sphereCenter = float3(0, 0, 0);

                // compute UV coordinates based on sphere distance
                float ringStart = _PlanetRadius * _PlanetRadiusOffsetFactor - (_PlanetRadius * _PlanetRadiusMaxSpreadFactor);
                float ringlength = (_PlanetRadius * _PlanetRadiusMaxSpreadFactor) * 2.0;
                float uv = (distance(fragment.worldPos, sphereCenter) - ringStart) / ringlength;

                // fetch texture color with UV and apply cutoff via step function
                float4 outputColor = tex2Dlod(_RingMap, float4(uv, 0.0, 0.0, 0.0)) * step(0.0, uv) * step(uv, 1.0);
                if (outputColor.w <= 0.0) discard;

                // calculate closest points for shadow calculations
                float3 closestPointToViewer;
                float3 closestPointToLight;
                CalculateClosestPoints(_WorldSpaceCameraPos, viewerDirection, sphereCenter, lightDirection, closestPointToViewer, closestPointToLight);
                float distanceBetweenPoints = length(closestPointToViewer - closestPointToLight);

                // shadow determination
                float shadowIntensity = 1.0;
                if (distanceBetweenPoints < _PlanetRadius && dot(fragment.worldPos, lightDirection) < 0.0) {
                    float shadowFade = sqrt(_PlanetRadius * _PlanetRadius - distanceBetweenPoints * distanceBetweenPoints) / length(cross(lightDirection, viewerDirection));
                    shadowIntensity = lerp(0.5, 1.0, step(shadowFade, distance(closestPointToViewer, fragment.worldPos)));
                }

                // apply shadow to pixel color and scale by sun color
                outputColor.xyz *= shadowIntensity * _SunColor.xyz;

                // apply tone mapping
                outputColor.xyz = ApplyAcesFilmicTonemap(outputColor.xyz);

                // calculate transparency based on camera distance
                float cameraDistance = distance(_WorldSpaceCameraPos, fragment.worldPos);
                float fadeStartDistance = 450.0;
                float fadeEndDistance = 150.0;
                float alphaFade = saturate((cameraDistance - fadeEndDistance) / (fadeStartDistance - fadeEndDistance));
                outputColor.w *= alphaFade;

                return outputColor;
            }
            ENDCG
        }
    }
}
