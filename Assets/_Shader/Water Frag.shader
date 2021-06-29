Shader "Custom/Water Unlit"
{
    Properties
    {
//        _ShoreFadeStrength("Shore fade strength", Range(0.01, 1)) = 1
        _ColorDeep("Deep Color", Color) = (1, 1, 1)
        _ColorShallow("Shallow Color", Color) = (1, 1, 1)
        _ColorDepthCoef("Color depth", Range(0.01, 1)) = 1
        _AlphaFresnelPow("Alpha Fresnel Pow", float) = 1
        _Smoothness("Smoothness", Range(0.01, 1)) = 1
    	_EdgeFade("Geometry edge fade", Range(0.001, 10)) = 1
    	
        _NormalA ("Wave Normal Map A", 2D) = "bump" {}
        _NormalB ("Wave Normal Map B", 2D) = "bump" {}
    }
    SubShader
    {
        Tags 
        { 
            "Queue" = "AlphaTest" "RenderType"="Transparent" 
        }
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                float2 uv : TEXCOORD4;
            };

            float4 _ColorDeep;
            float4 _ColorShallow;
            float _ColorDepthCoef;
            // float _ShoreFadeStrength;
            float _Smoothness;
            float _AlphaFresnelPow;
            float _EdgeFade;

            sampler2D _NormalA;
            sampler2D _NormalB;

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            float _DepthPow;

            v2f vert(appdata_base v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz);

                o.worldNormal = worldNormal;
				o.worldPos = worldPos;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
				o.screenPos = ComputeScreenPos(o.vertex);
				o.viewDir = -WorldSpaceViewDir(v.vertex);

                return o;
            }

            float calculateSpecular(float3 normal, float3 viewDir, float smoothness)
            {
                float3 dirToSun = _WorldSpaceLightPos0.xyz;
				float specularAngle = acos(dot(normalize(dirToSun - viewDir), normal));
				float specularExponent = specularAngle / smoothness;
				float specularHighlight = exp(-specularExponent * specularExponent);
				return specularHighlight;
			}

            // Reoriented Normal Mapping
			// http://blog.selfshadow.com/publications/blending-in-detail/
			// Altered to take normals (-1 to 1 ranges) rather than unsigned normal maps (0 to 1 ranges)
			float3 blend_rnm(float3 n1, float3 n2)
			{
				n1.z += 1;
				n2.xy = -n2.xy;

				return n1 * dot(n1, n2) / n1.z - n2;
			}

			// Sample normal map with triplanar coordinates
			// Returned normal will be in obj/world space (depending whether pos/normal are given in obj or world space)
			// Based on: medium.com/@bgolus/normal-mapping-for-a-triplanar-shader-10bf39dca05a
			float3 triplanarNormal(float3 vertPos, float3 normal, float3 scale, float2 offset, sampler2D normalMap) {
				float3 absNormal = abs(normal);

				// Calculate triplanar blend
				float3 blendWeight = saturate(pow(normal, 4));
				// Divide blend weight by the sum of its components. This will make x + y + z = 1
				blendWeight /= dot(blendWeight, 1);

				// Calculate triplanar coordinates
				float2 uvX = vertPos.zy * scale + offset;
				float2 uvY = vertPos.xz * scale + offset;
				float2 uvZ = vertPos.xy * scale + offset;

				// Sample tangent space normal maps
				// UnpackNormal puts values in range [-1, 1] (and accounts for DXT5nm compression)
				float3 tangentNormalX = UnpackNormal(tex2D(normalMap, uvX));
				float3 tangentNormalY = UnpackNormal(tex2D(normalMap, uvY));
				float3 tangentNormalZ = UnpackNormal(tex2D(normalMap, uvZ));

				// Swizzle normals to match tangent space and apply reoriented normal mapping blend
				tangentNormalX = blend_rnm(half3(normal.zy, absNormal.x), tangentNormalX);
				tangentNormalY = blend_rnm(half3(normal.xz, absNormal.y), tangentNormalY);
				tangentNormalZ = blend_rnm(half3(normal.xy, absNormal.z), tangentNormalZ);

				// Apply input normal sign to tangent space Z
				float3 axisSign = sign(normal);
				tangentNormalX.z *= axisSign.x;
				tangentNormalY.z *= axisSign.y;
				tangentNormalZ.z *= axisSign.z;

				// Swizzle tangent normals to match input normal and blend together
				float3 outputNormal = normalize(
					tangentNormalX.zyx * blendWeight.x +
					tangentNormalY.xzy * blendWeight.y +
					tangentNormalZ.xyz * blendWeight.z
				);

				return outputNormal;
			}

            fixed4 frag(v2f i) : SV_Target
            {
            	// return float4(0.2, 0.2, 0.5, 0.3);
                float3 viewDir = normalize(i.viewDir);

				// Specular normal
				float waveSpeed = 0.25;
				float waveNormalScale = 0.03;
				float waveStrength = 0.2;

            	float2 waveOffsetA = float2(_Time.x * waveSpeed, _Time.x * waveSpeed * 0.8);
				float2 waveOffsetB = float2(_Time.x * waveSpeed * - 0.8, _Time.x * waveSpeed * -0.3);
				float3 waveNormal1 = triplanarNormal(i.worldPos, i.worldNormal, waveNormalScale, waveOffsetA, _NormalA);
				float3 waveNormal2 = triplanarNormal(i.worldPos, i.worldNormal, waveNormalScale, waveOffsetB, _NormalB);
				float3 waveNormal = triplanarNormal(i.worldPos, waveNormal1, waveNormalScale, waveOffsetB, _NormalB);
				float3 specWaveNormal = normalize(lerp(i.worldNormal, waveNormal, waveStrength));

            	float3 dirToSun = _WorldSpaceLightPos0.xyz;
				float f2 = dot(i.worldNormal, dirToSun);
				f2 = smoothstep(0,0.2,f2);
            	float g = 1-((pow(dot(waveNormal1,i.worldNormal), 0.9)) > 0.93);
				float g2 = 1-((pow(dot(waveNormal2,i.worldNormal),0.9)) > 0.93);
				float glitter = g * g2 * 0.3 * f2;

            	// Specular highlight
				float specularHighlight = calculateSpecular(specWaveNormal, viewDir, _Smoothness);

            	// Water depth
            	float dstToTerrain = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, i.screenPos));
				float dstToWater = i.screenPos.w;
				float waterViewDepth = dstToTerrain - dstToWater;
				// float waterDensityMap = density * 500; // no huensity oke
            	
            	// Fade water at intersection with geometry
				float alphaEdge = 1 - exp(-waterViewDepth * _EdgeFade);

            	// Calculate final alpha
				float opaqueWater = max(0, specularHighlight > 0.5);
				float alpha = saturate(max(opaqueWater, alphaEdge));

            	// -------- Lighting and colour output --------
				float lighting = saturate(dot(i.worldNormal, dirToSun));
            	
            	fixed4 col = lerp(_ColorShallow, _ColorDeep, 1 - exp(-waterViewDepth * _ColorDepthCoef));
                col.rgb = saturate(col * lighting + unity_AmbientSky) + specularHighlight;
            	col.rgb += glitter;
                col.a = alpha;

                // return half4(i.worldNormal.xyz, 1);
                return col;
            }
            ENDCG
        }
    }
}