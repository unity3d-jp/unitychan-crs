Shader "Custom/Confetti"
{
    Properties
    {
        _Color("Base", Color) = (0.5, 0.5, 0.5, 0.5)
        _Emission("Emission", Color) = (0, 0, 0, 0)

    }
    SubShader
    {
        Tags { "RenderType"="OpaqueDoubleSided" }

        Cull off
        
        CGPROGRAM

        #pragma surface surf Lambert

        float4 _Color;
        float3 _Emission;

        struct Input
        {
            float dummy;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Color.rgb;
            o.Alpha = _Color.a;
            o.Emission = _Emission;
        }

        ENDCG
    } 
    FallBack "Diffuse"
}
