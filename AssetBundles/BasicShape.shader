Shader "Custom/BasicShape" {
    Properties {
        _Shininess ("Shininess", float) = 10.0
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
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #define PI 3.1415926535897932384626433832795

            struct vertex_data {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 color : COLOR;
            };

            struct fragment_data {
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
            
            fragment_data vert(vertex_data v) {
                fragment_data f;
                f.worldPos = mul(unity_ObjectToWorld, v.vertex);
                f.objPos = v.vertex;
                f.vertex = UnityObjectToClipPos(v.vertex);
                f.normal = normalize(UnityObjectToWorldNormal(v.normal));
                f.tangent.xyz = normalize(UnityObjectToWorldNormal(v.tangent.xyz));
                f.tangent.w = v.tangent.w;
                f.color = v.color;

                return f;
            }

            float3x3 Crossfloat3x3_W2L(float3 d1, float3 d2) //d1,d2 on x-y plane, x axis lock on d1
            {
                d1 = normalize(d1);
                d2 = normalize(d2);
                float3 d3 = normalize(cross(d1,d2));
                d2 = normalize(cross(d3,d1));
                
                return float3x3(d1,d2,d3);
            }
            
            
            void clossestPoint(in float3 srcA,in float3 dirA,in float3 srcB,in float3 dirB, out float3 posA, out float3 posB)
            {
                float3x3 proj = Crossfloat3x3_W2L(dirA,dirB);
                srcA = mul(proj,srcA);
                dirA = mul(proj,dirA);
                srcB = mul(proj,srcB);
                dirB = mul(proj,dirB);
                float2 r = mul(float2(srcA.x*dirA.y-srcA.y*dirA.x,srcB.x*dirB.y-srcB.y*dirB.x),float2x2(dirB.xy,-dirA.xy));
                r /= dirB.x*dirA.y-dirA.x*dirB.y;
                srcA.xy = r;
                srcB.xy = r;
                posA = mul(srcA,proj);
                posB = mul(srcB,proj);
            }

            float2 pos2UV(float3 pos)
            {
                pos = normalize(pos);
                float2 uv = float2(atan2(pos.z, pos.x), acos(clamp(-pos.y,-1.0,1.0)));
                uv /= float2(PI * 2.0, PI);
                // uv = clamp(uv,0.0,1.0);
                return uv;
            }
            
            fixed4 frag(fragment_data f) : SV_Target {
                float4 pixel = float4(1.0, 1.0, 1.0, 1.0);
                // triplanar normal mapping
                f.normal = normalize(f.normal);
                float3 binormal = normalize(cross(f.normal, normalize(f.tangent.xyz)));
                f.tangent.xyz = normalize(cross(binormal, f.normal));
                float3x3 rotation = float3x3(f.tangent.xyz, binormal * f.tangent.w,f.normal);

                float2 uv = pos2UV(f.objPos.xyz);
                float3 objectNormal = _BumpIntensity * (tex2Dlod(_BumpMap, float4(uv.x,uv.y,0.0,0.0)).xyz * 2.0 - float3(1.0,1.0,1.0));
                // float3 objectNormal = float3(0.0,0.0,1.0);
                objectNormal = mul(objectNormal,rotation);
                // pixel.xyz = objectNormal;
                // return pixel;

                // lighting calculations
                float3 lightDir = normalize(_PlanetSunLightDirection.xyz);
                // float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir = normalize(_WorldSpaceCameraPos - f.worldPos);
                // depth = f.vertex.w;
                // shadow calculations
                float3 sphereCenter = float3(0, 0, 0);
                float sphereRadius = 100.0;

                
                float3 viewClossestPoint;
                float3 lightClossestPoint;
                clossestPoint(_WorldSpaceCameraPos,viewDir,sphereCenter,lightDir, viewClossestPoint, lightClossestPoint);
                float l = length(viewClossestPoint-lightClossestPoint);


                float shadow = 1.0;
                if(l < sphereRadius)
                {
                    float d = sqrt(sphereRadius * sphereRadius - l * l) / length(cross(lightDir,viewDir));
                    shadow = lerp(0.8,shadow,step(d,distance(viewClossestPoint,f.worldPos)));
                }
                // diffuse
                float diff = max(dot(objectNormal, lightDir), 0.0);
                float3 diffuse = _DiffuseIntensity * diff * _DiffuseColor.rgb;
                // specular
                float3 reflectDir = reflect(lightDir, objectNormal);
                float spec = pow(max(dot(-viewDir, reflectDir), 0.0), _Shininess);
                float3 specular = (_SpecularIntensity * spec).xxx;
                // combine lighting and apply shadow
                float3 light = (diffuse + specular + _AmbientColor.rgb) * shadow;
                pixel.xyz = pixel.xyz * light * f.color;

                return pixel;
            }
            ENDCG
        }
    }
}
