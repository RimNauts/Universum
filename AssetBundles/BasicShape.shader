Shader "Custom/BasicShape" {
    Properties {
        _BumpMap ("Bump Map", 2D) = "white" {}
    }

	SubShader {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

            struct vertex_data {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

			struct fragment_data {
                float4 worldPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
				float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
			};
			
            float4 _PlanetSunLightDirection;
            sampler2D _BumpMap;
			 
			fragment_data vert(vertex_data v) {
                fragment_data f;
                f.worldPos = mul(unity_ObjectToWorld, v.vertex);
                f.vertex = mul(unity_MatrixVP, mul(unity_ObjectToWorld, v.vertex));
                f.normal = normalize(mul(float4(v.normal, 0.0), unity_WorldToObject).xyz);
                f.uv = v.uv;
                f.color = v.color;

                return f;
			}
			
			fixed4 frag(fragment_data f) : SV_Target {
                float4 pixel = float4(1.0, 1.0, 1.0, 1.0);
                float shininess = 10.0;
                float specularIntensity = 0.1;
                float bumpIntensity = 1.0f;
                // add bumps though normal mapping
                float3 sampledNormal = bumpIntensity * (tex2D(_BumpMap, f.uv).rgb - 0.5) * 2.0;
                float3 finalNormal = normalize(f.normal + sampledNormal);
                float3 objectNormal = normalize(-finalNormal);

                float3 lightDir = normalize(_PlanetSunLightDirection.xyz);
                float3 viewDir = normalize(_WorldSpaceCameraPos - f.worldPos);
                // shadow calculations
                float3 sphereCenter = float3(0, 0, 0);
                float sphereRadius = 100.0;

                float3 toCenter = sphereCenter - f.worldPos;
                float tca = dot(toCenter, -lightDir);
                float d2 = dot(toCenter, toCenter) - tca * tca;
                float radius2 = sphereRadius * sphereRadius;

                float shadow = 1.0;
                if (dot(toCenter, lightDir) < 0.0) {
                    if (d2 < radius2) {
                        shadow = 0.8;
                    }
                }
                // diffuse
                float diff = max(dot(objectNormal, lightDir), 0.0);
                float3 diffuse = 0.75 * diff * float3(0.518, 0.397, 0.318);
                // specular
                float3 reflectDir = reflect(lightDir, objectNormal);
                float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess);
                float3 specular = specularIntensity * spec * float3(1.0, 1.0, 1.0);
                // combine lighting and apply shadow
                float3 light = (diffuse + specular + float3(0.482, 0.603, 0.682)) * shadow;
                pixel.xyz = pixel.xyz * light * f.color;

                return pixel;
            }
			ENDCG
		}
	}
}
