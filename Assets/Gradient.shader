Shader "Custom/TwoColorGradientStrong"
{
    Properties
    {
        _Color1 ("Color 1", Color) = (1,0,0,1) // First color
        _Color2 ("Color 2", Color) = (0,0,1,1) // Second color
        _BlendHeight ("Blend Height", Range(0,50)) = 5.0 // Controls how fast the gradient changes
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert

        struct Input
        {
            float3 worldPos;
        };

        float4 _Color1;
        float4 _Color2;
        float _BlendHeight;

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Adjust the blending factor based on the world position
            float blendFactor = saturate(IN.worldPos.y / _BlendHeight); // Adjust the blending factor
            o.Albedo = lerp(_Color1.rgb, _Color2.rgb, blendFactor); // Blend the two colors
        }
        ENDCG
    }
    FallBack "Diffuse"
}