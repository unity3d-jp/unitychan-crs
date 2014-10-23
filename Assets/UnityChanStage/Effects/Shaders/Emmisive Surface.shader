Shader "Custom/Untextured Emmisive Surface"
{
    Properties
    {
        _Color("Base", Color) = (0.5, 0.5, 0.5, 0.5)
        _Emission("Emission", Color) = (0, 0, 0, 0)
        _Amplitude("Amplitude", Float) = 1

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        CGPROGRAM

        #pragma surface surf Lambert

        float4 _Color;
        float3 _Emission;
        float _Amplitude;

        struct Input
        {
            float dummy;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Color.rgb;
            o.Alpha = _Color.a;
            o.Emission = _Emission * _Amplitude;
        }

        ENDCG
    } 
    FallBack "Diffuse"
}
