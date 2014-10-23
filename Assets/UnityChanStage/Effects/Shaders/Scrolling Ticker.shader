Shader "Custom/Scrolling Ticker"
{
    Properties
    {
        _MainTex("Base", 2D) = ""{}
        _Amplitude("Amplitude", Float) = 1
        _Speed("Scroll Speed (U, V)", Vector) = (1, 1, 0, 0)

    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        CGPROGRAM

        #pragma surface surf Lambert alpha

        sampler2D _MainTex;
        float _Amplitude;
        float2 _Speed;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            float2 d = _Speed * _Time.y;
            float4 c = tex2D(_MainTex, IN.uv_MainTex + d);
            o.Alpha = c.a;
            o.Emission = c.rgb * _Amplitude;
        }

        ENDCG
    } 
    FallBack "Diffuse"
}
