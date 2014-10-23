Shader "Custom/Tree Leaf"
{
    Properties
    {
        _MainTex   ("Base Texture",  2D)    = ""{}
        _Amplitude ("Amplitude",     Float) = 1
        _WaveScale ("Wave Scale",    Float) = 1
        _WaveSpeed ("Wave Speed",    Float) = 1
        _WaveExp   ("Eave Exponent", Float) = 8

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        CGPROGRAM

        #pragma surface surf Lambert

        sampler2D _MainTex;
        float _Amplitude;
        float _WaveScale;
        float _WaveSpeed;
        float _WaveExp;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            float4 c = tex2D(_MainTex, IN.uv_MainTex);

            o.Albedo = c.rgb;
            o.Alpha = c.a;

            float t = _WaveScale * IN.worldPos.y + _WaveSpeed * _Time.y;
            float amp = pow((1.0f + sin(t)) * 0.5f, _WaveExp) * _Amplitude;
            o.Emission = c.rgb * amp;
        }

        ENDCG
    } 
    FallBack "Diffuse"
}
