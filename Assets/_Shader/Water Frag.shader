Shader "Custom/Water Unlit"
{
    Properties
    {
        _ColorDeep("Deep Color", Color) = (1, 1, 1)
        _ColorShallow("Shallow Color", Color) = (1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent" "IgnoreProjector"="True" "Queue" = "Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            Tags
            {
                "LightMode" = "ForwardBase"
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "UnityStandardBRDF.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 worldNormal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
                float4 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                float3 worldNormal : NORMAL;
            };

            float4 _ColorDeep;
            float4 _ColorShallow;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            float4 _LightColor, _LightDir;

            float _DepthFactor;
            float _DepthPow;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                COMPUTE_EYEDEPTH(o.screenPos.w);

                o.worldNormal = UnityObjectToWorldNormal(v.worldNormal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));

                return o;
            }

            float calculateSpecularHighlight(float3 normal, float3 viewDir, float smoothness)
            {
                float3 dirToSun = _WorldSpaceLightPos0.xyz;
                float specularAngle = acos(dot(normalize(dirToSun - viewDir), normal));
                float specularExponent = specularAngle / smoothness;
                float specularHighlight = exp(-specularExponent * specularExponent);

                return specularHighlight;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col;

                float dstToTerrain = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, i.screenPos));
                float depth = dstToTerrain - i.screenPos.w;

                float3 waterCol = lerp(_ColorShallow, _ColorDeep, 1 - exp(-depth * 0.5));
                float fresnel = 1 - min(0.2, pow(saturate(dot(-i.viewDir, i.worldNormal)), 1));
                float shoreFade = 1 - exp(-depth * 0.15);
                float waterAlpha = fresnel * shoreFade;

                float specularHighlight = calculateSpecularHighlight(i.worldNormal, i.viewDir, 0.1);
                waterCol += specularHighlight;

                col.rgb = waterCol;
                col.a = waterAlpha;

                return col;
            }
            ENDCG
        }
    }
}