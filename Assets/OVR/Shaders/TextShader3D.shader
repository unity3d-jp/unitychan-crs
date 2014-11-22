Shader "GUI/3D Text Shader" 
{ 
	Properties 
	{ 
 	  _MainTex ("Font Texture", 2D) = "white" {} 
	  _Color ("Text Color", Color) = (1,1,1,1) 
	} 

	SubShader 
	{ 
   		Tags { "Queue"="Overlay+1" "IgnoreProjector"="True" "RenderType"="Transparent" } 
   		Lighting Off Cull Off ZWrite Off ZTest Off Fog { Mode Off } 
   		Blend SrcAlpha One 
   		Pass 
   		{ 
      		Color [_Color] 
      		SetTexture [_MainTex] 
      		{ 
         		combine primary, texture * primary 
      		} 
   		} 
	} 
}