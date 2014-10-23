Shader "Custom/Back Screen"
{
    Properties
    {
        _MainTex      ("Base",          2D) = ""{}
        _StripeTex    ("Stripe",        2D) = ""{}
        _BaseLevel    ("Base Level",    Float) = 1
        _StripeLevel  ("Stripe Level",  Float) = 1
        _FlickerLevel ("Flicker Level", Float) = 1
        _FlickerFreq  ("Flicker Freq",  Float) = 1
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    struct v2f
    {
        float4 position : SV_POSITION;
        float2 uv0 : TEXCOORD0;
        float2 uv1 : TEXCOORD1;
    };

    sampler2D _MainTex;
    float4 _MainTex_ST;

    sampler2D _StripeTex;
    float4 _StripeTex_ST;

    float _BaseLevel;
    float _StripeLevel;
    float _FlickerLevel;
    float _FlickerFreq;

    v2f vert(appdata_base v)
    {
        v2f o;
        o.position = mul(UNITY_MATRIX_MVP, v.vertex);
        o.uv0 = TRANSFORM_TEX(v.texcoord, _MainTex);
        o.uv1 = TRANSFORM_TEX(v.texcoord, _StripeTex);
        return o;
    }

    float4 frag(v2f i) : COLOR
    {
        float4 color = tex2D(_MainTex, i.uv0);

        float amp = tex2D(_StripeTex, i.uv1).r;
        amp = _BaseLevel + _StripeLevel * amp;

        float time = _Time.y * 3.14f * _FlickerFreq;
        float flicker = lerp(1.0f, sin(time) * 0.5f, _FlickerLevel);

        return color * (amp * flicker);
    }

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    } 
    FallBack "Diffuse"
}
