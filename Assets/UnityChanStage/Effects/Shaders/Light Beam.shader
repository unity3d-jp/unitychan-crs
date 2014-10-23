Shader "Custom/Light Beam"
{
    Properties
    {
        _Color      ("Base Color", Color)    = (1, 1, 1, 1)
        _MainTex    ("Gradient Texture", 2D) = ""{}
        _NoiseTex1  ("Noise Texture 1", 2D)  = ""{}
        _NoiseTex2  ("Noise Texture 2", 2D)  = ""{}
        _NoiseScale ("Noise Scale", Vector)  = (1, 1, 1, 1)
        _NoiseSpeed ("Noise Speed", Vector)  = (0.1, 0.1, 0.1, 0.1)
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    struct v2f
    {
        float4 position : SV_POSITION;
        float2 uv0 : TEXCOORD0;
        float2 uv1 : TEXCOORD1;
        float2 uv2 : TEXCOORD2;
        float4 world_position : TEXCOORD3;
        float3 normal : TEXCOORD4;
    };

    float4 _Color;

    sampler2D _MainTex;
    float4 _MainTex_ST;

    sampler2D _NoiseTex1;
    sampler2D _NoiseTex2;

    float4 _NoiseScale;
    float4 _NoiseSpeed;

    v2f vert(appdata_base v)
    {
        v2f o;

        o.position = mul(UNITY_MATRIX_MVP, v.vertex);

        o.uv0 = TRANSFORM_TEX(v.texcoord, _MainTex);

        float4 wp = mul(_Object2World, v.vertex);
        o.uv1 = wp.xy * _NoiseScale.xy + _NoiseSpeed.xy * _Time.y;
        o.uv2 = wp.xy * _NoiseScale.zw + _NoiseSpeed.zw * _Time.y;
		o.world_position = v.vertex;
		o.normal = normalize(mul(_Object2World, float4(v.normal.xyz,0.0)));

        return o;
    }

    float4 frag(v2f i) : COLOR
    {
		float3 normal = i.normal;
		float3 camDir = normalize(i.world_position - _WorldSpaceCameraPos);
		float falloff = max(abs(dot(camDir, normal))-0.4, 0.0);
		falloff = falloff * falloff * 5.0;

        float4 c = _Color;

        float n1 = tex2D(_NoiseTex1, i.uv1).r;
        float n2 = tex2D(_NoiseTex2, i.uv2).r;

        c.a *= tex2D(_MainTex, i.uv0).a * n1 * n2 * falloff;

        return c;
    }

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
			Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    } 
    FallBack "Diffuse"
}
