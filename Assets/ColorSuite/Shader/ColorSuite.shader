//
// Copyright (C) 2014 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

Shader "Hidden/ColorSuite"
{
    Properties
    {
        _MainTex("Base", 2D) = ""{}
        _Curves("Curves", 2D) = ""{}
    }
    
    CGINCLUDE

    // Multi compilation options (vignette/tonemapping)
    #pragma multi_compile TONEMAPPING_OFF TONEMAPPING_ON
    #pragma multi_compile VIGNETTE_OFF VIGNETTE_ON

    #include "UnityCG.cginc"
    
    sampler2D _MainTex;
    sampler2D _Curves;
    float _Saturation;

#if TONEMAPPING_ON
    // Reinhard tonemapping operator
    float _Exposure;
    float3 reinhard(float3 s)
    {
        float l = Luminance(s); 
        float lT = l * _Exposure;
        return s * (lT / ((1 + lT) * l));
    }
#endif

#if VIGNETTE_ON
    // Pseudo vignette function
    float _Vignette;
    float vignette(float2 uv)
    {
        float2 cuv = (uv - 0.5) * 2;
        return 1 - dot(cuv, cuv) * _Vignette * 0.1;
    }
#endif

    // Color adjustment function.
    float3 adjust_color(float3 s)
    {
        float3 r = tex2D(_Curves, float2(s.r, 0)) * float3(1, 0, 0);
        float3 g = tex2D(_Curves, float2(s.g, 0)) * float3(0, 1, 0);
        float3 b = tex2D(_Curves, float2(s.b, 0)) * float3(0, 0, 1);
        float3 c = r + g + b;
        float l = Luminance(c);
        return lerp(float3(l, l, l), c, _Saturation);
    }

    float4 frag(v2f_img i) : SV_Target 
    {
        float4 source = tex2D(_MainTex, i.uv); 
        float3 rgb = source.rgb;
#if TONEMAPPING_ON
        rgb = reinhard(rgb);
#endif
#if VIGNETTE_ON
        rgb *= vignette(i.uv);
#endif
        return float4(adjust_color(rgb), source.a);
    }

    ENDCG 
    
    Subshader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            Fog { Mode off }      
            CGPROGRAM
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}
