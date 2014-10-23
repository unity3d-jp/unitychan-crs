Shader "Custom/Unlit Alpha Cutout"
{
    Properties
    {
        _MainTex ("Base",   2D)    = ""{}
        _Color   ("Color",  Color) = (1, 1, 1, 1)
        _Cutoff  ("Cutoff", Float) = 0.5
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
    float _Cutoff;

    v2f vert(appdata_base v)
    {
        v2f o;
        o.position = mul(UNITY_MATRIX_MVP, v.vertex);
        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
        return o;
    }

    float4 frag(v2f i) : COLOR
    {
        float4 c = tex2D(_MainTex, i.texcoord);
        clip(c.a - _Cutoff);
        return c * _Color;
    }

    ENDCG

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
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
