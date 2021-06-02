Shader "Custom/Water"
{
    Properties
    {
        testTexture("Texture", 2D) = "white" {}
        testScale("Scale", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        float minHeight;
        float maxHeight;

        const static int maxLayerCount = 8;
        const static float epsilon = 1E-4;

        float base_Blends[maxLayerCount];
        float base_color_strength[maxLayerCount];
        float base_texture_scale[maxLayerCount];
        float3 base_colors[maxLayerCount];
        float base_start_heights[maxLayerCount];
        int layer_count;

        sampler2D testTexture;
        float testScale;
        UNITY_DECLARE_TEX2DARRAY(base_textures);

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float3 screenPos;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float nonLinearDepth = SAMPLE_DEPTH_TEXTURE_PROJ(IN.screenPos);
            // float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
            // float3 blendAxes = abs(IN.worldNormal);
            // blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
            //
            // for (int i = 0; i < layer_count; i++)
            // {
            //     float drawStrength = inverseLerp(-base_Blends[i]/2 - epsilon, base_Blends[i]/2, heightPercent - base_start_heights[i]);
            //
            //     float3 baseColor = base_colors[i] * base_color_strength[i];
            //     
            //     o.Albedo = o.Albedo * (1 - drawStrength) + (baseColor + textureColor) * drawStrength;
            // }
            //o.Albedo = xProjection + yProjection + zProjection;
        }
        ENDCG
    }
    FallBack "Diffuse"
}