Shader "Custom/BasicShape" {
    Properties {
        _Shininess ("Shininess", float) = 0.1
        _SpecularIntensity ("Specular Intensity", float) = 0.1
        _DiffuseIntensity ("Diffuse Intensity", float) = 0.75
        _DiffuseColor ("Diffuse Color", color) = (0.518, 0.397, 0.318, 1.0)
        _AmbientColor ("Ambient Color", color) = (0.482, 0.603, 0.682, 1.0)
        _BumpMap ("Bump Map", 2D) = "blue" {}
        _BumpIntensity ("Bump Intensity", float) = 1.0
    }

    SubShader {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        
        Pass {
            CGPROGRAM
            #include "UnityCG.cginc"

            #define PI 3.1415926535897932384626433832795

            #pragma vertex vert
            #pragma fragment frag

            struct vertexData {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 color : COLOR;
            };

            struct fragmentData {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 color : COLOR;
                float4 worldPos : TEXCOORD1;
                float4 objPos : TEXCOORD2;
            };

            uniform float4 _PlanetSunLightDirection;
            float _Shininess;
            float _SpecularIntensity;
            float _DiffuseIntensity;
            float4 _DiffuseColor;
            float4 _AmbientColor;
            sampler2D _BumpMap;
            float _BumpIntensity;

            fragmentData vert(vertexData vertex) {
                fragmentData fragment;
                fragment.worldPos = mul(unity_ObjectToWorld, vertex.vertex);
                fragment.objPos = vertex.vertex;
                fragment.vertex = UnityObjectToClipPos(vertex.vertex);
                fragment.normal = normalize(UnityObjectToWorldNormal(vertex.normal));
                fragment.tangent.xyz = normalize(UnityObjectToWorldNormal(vertex.tangent.xyz));
                fragment.tangent.w = vertex.tangent.w;
                fragment.color = vertex.color;

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

            float2 PositionToUVCoordinates(float3 position) {
                position = normalize(position);
                float2 uvCoordinates = float2(atan2(position.z, position.x), acos(clamp(-position.y, -1.0, 1.0)));
                uvCoordinates /= float2(PI * 2.0, PI);
                // uvCoordinates = clamp(uvCoordinates, 0.0, 1.0);
                return uvCoordinates;
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
                float4 outputColor = float4(1.0, 1.0, 1.0, 1.0);

                // normalize and compute bi-normal and tangent
                fragment.normal = normalize(fragment.normal);
                float3 biNormal = normalize(cross(fragment.normal, normalize(fragment.tangent.xyz)));
                fragment.tangent.xyz = normalize(cross(biNormal, fragment.normal));
                float3x3 tangentSpaceMatrix = float3x3(fragment.tangent.xyz, biNormal * fragment.tangent.w, fragment.normal);

                // map object position to UV coordinates and apply bump mapping
                float2 uvCoordinates = PositionToUVCoordinates(fragment.objPos.xyz);
                float3 mappedNormal = _BumpIntensity * (tex2Dlod(_BumpMap, float4(uvCoordinates.x, uvCoordinates.y, 0.0, 0.0)).xyz * 2.0 - float3(1.0, 1.0, 1.0));
                mappedNormal = mul(mappedNormal, tangentSpaceMatrix);

                // calculate lighting direction and viewer direction
                float3 lightDirection = normalize(-_PlanetSunLightDirection.xyz);
                float3 viewerDirection = normalize(_WorldSpaceCameraPos - fragment.worldPos);

                // compute closest points for shadow calculations
                float3 closestPointToViewer;
                float3 closestPointToLight;
                CalculateClosestPoints(_WorldSpaceCameraPos, viewerDirection, float3(0, 0, 0), lightDirection, closestPointToViewer, closestPointToLight);
                float distanceBetweenPoints = length(closestPointToViewer - closestPointToLight);

                // shadow determination
                float shadowIntensity = 1.0;
                float planetRadius = 100.0;
                if (distanceBetweenPoints < planetRadius && dot(fragment.worldPos, lightDirection) < 0.0) {
                    float shadowFade = sqrt(planetRadius * planetRadius - distanceBetweenPoints * distanceBetweenPoints) / length(cross(lightDirection, viewerDirection));
                    shadowIntensity = lerp(0.5, 1.0, step(shadowFade, distance(closestPointToViewer, fragment.worldPos)));
                }

                // diffuse lighting calculation
                float diffuseIntensity = max(dot(mappedNormal, lightDirection), 0.0);
                float3 diffuseLight = _DiffuseIntensity * diffuseIntensity * _DiffuseColor.rgb;

                // specular lighting calculation
                float3 reflectionDirection = reflect(lightDirection, mappedNormal);
                float specularIntensity = pow(max(dot(-viewerDirection, reflectionDirection), 0.0), _Shininess);
                float3 specularLight = (_SpecularIntensity * specularIntensity).xxx;

                // combine lighting components and apply shadow
                float3 ambientLight = _AmbientColor.rgb * 0.7;
                float3 totalLight = (diffuseLight + specularLight + ambientLight) * shadowIntensity;
                outputColor.xyz = outputColor.xyz * totalLight * fragment.color;

                // apply tone mapping
                outputColor.xyz = ApplyAcesFilmicTonemap(outputColor.xyz);

                return outputColor;
            }
            ENDCG
        }
    }
}
