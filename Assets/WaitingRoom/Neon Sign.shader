Shader "Custom/Neon Sign"
{
    Properties
    {
        _MainTex("Texture", 2D) = ""{}
        _Color("Color", Color) = (1, 1, 1, 1)
        _Amplitude("Amplitude", Float) = 1
    }

    CGINCLUDE

#include "UnityCG.cginc"

struct v2f
{
    float4 position : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

sampler2D _MainTex;
float4 _MainTex_ST;
float4 _Color;
float _Amplitude;

v2f vert(appdata_base v)
{
    v2f o;
    o.position = mul(UNITY_MATRIX_MVP, v.vertex);
    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
    return o;
}

float4 frag(v2f i) : COLOR
{
    float a = tex2D(_MainTex, i.texcoord).a;
    return float4(_Color.rgb * _Amplitude, a);
}

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    } 
    FallBack "Transparent/Diffuse"
}
