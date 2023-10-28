Shader "Custom/BasicShape" {
    Properties {
        _Shininess ("Shininess", float) = 10.0
        _SpecularIntensity ("Specular Intensity", float) = 0.1
        _DiffuseIntensity ("Diffuse Intensity", float) = 0.75
        _DiffuseColor ("Diffuse Color", color) = (0.518, 0.397, 0.318, 1.0)
        _AmbientColor ("Ambient Color", color) = (0.482, 0.603, 0.682, 1.0)
        _BumpMap ("Bump Map", 2D) = "white" {}
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
                f.vertex = mul(unity_MatrixVP, mul(unity_ObjectToWorld, v.vertex));
                f.normal = normalize(mul(float4(v.normal, 0.0), unity_WorldToObject).xyz);
                f.uv = v.uv;
                f.color = v.color;

                return f;
			}
			
			fixed4 frag(fragment_data f) : SV_Target {
                float4 pixel = float4(1.0, 1.0, 1.0, 1.0);
                // add bumps though normal mapping
                float3 sampledNormal = _BumpIntensity * (tex2D(_BumpMap, f.uv).rgb - 0.5) * 2.0;
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
                float3 diffuse = _DiffuseIntensity * diff * _DiffuseColor.rgb;
                // specular
                float3 reflectDir = reflect(lightDir, objectNormal);
                float spec = pow(max(dot(viewDir, reflectDir), 0.0), _Shininess);
                float3 specular = _SpecularIntensity * spec * float3(1.0, 1.0, 1.0);
                // combine lighting and apply shadow
                float3 light = (diffuse + specular + _AmbientColor.rgb) * shadow;
                pixel.xyz = pixel.xyz * light * f.color;

                return pixel;
            }
			ENDCG
		}
	}
}
