Shader "Custom/Visualizer"
{
	Properties
	{
		_ReflectionTex ("Base (RGB)", 2D) = "white" {}
		_Spectra ("Spectra", Vector) = (0, 0, 0, 0)

		_Center ("Center", Vector) = (0.0, 0.0, 0.0)
		_RingSrtide ("Stride", Float) = 0.2
		_RingThicknessMin ("ThicknessMin", Float) = 0.1
		_RingThicknessMax ("ThicknessMax", Float) = 0.5
		_RingEmission ("RingEmission", Float) = 10.0
		_RingSpeedMin ("RingSpeedMin", Float) = 0.2
		_RingSpeedMax ("RingSpeedMin", Float) = 0.5
		_GridColor ("GridColor", Vector) = (0.2, 0.3, 0.5)
		_GridEmission ("GridEmission", Float) = 8.0
		_ReflectionStrength ("ReflectionStrength", Float) = 0.2
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		CGPROGRAM
		#pragma surface surf Lambert
		#pragma glsl
		#pragma target 3.0

		sampler2D _ReflectionTex;
		sampler2D _ReflectionDepthTex;
		float4x4 _ViewProjectInverse;
		float4 _Spectra;
		float3 _Center;
		float _RingSrtide;
		float _RingThicknessMin;
		float _RingThicknessMax;
		float _RingEmission;
		float _RingSpeedMin;
		float _RingSpeedMax;
		float4 _GridColor;
		float _GridEmission;
		float _ReflectionStrength;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float4 screenPos;
		};

		float  iq_rand( float  p )
		{
			return frac(sin(p)*43758.5453);
		}

		float  _gl_mod(float  a, float  b) { return a - b * floor(a/b); }
		float2 _gl_mod(float2 a, float2 b) { return a - b * floor(a/b); }
		float3 _gl_mod(float3 a, float3 b) { return a - b * floor(a/b); }
		float4 _gl_mod(float4 a, float4 b) { return a - b * floor(a/b); }


		float Rings(float3 pos)
		{
			float pi = 3.14159;
			float2 wpos = pos.xz;

			float stride = _RingSrtide;
			float strine_half = stride * 0.5;
			float thickness = 1.0 - (_RingThicknessMin + length(_Spectra)*(_RingThicknessMax-_RingThicknessMin));
			float distance = abs(length(wpos) - _Time.y*0.1);
			float fra = _gl_mod(distance, stride);
			float cycle = floor((distance)/stride);

			float c = strine_half - abs(fra-strine_half) - strine_half*thickness;
			c = max(c * (1.0/(strine_half*thickness)), 0.0);

			float rs = iq_rand(cycle*cycle);
			float r = iq_rand(cycle) + _Time.y*(_RingSpeedMin+(_RingSpeedMax-_RingSpeedMin)*rs);

			float angle = atan2(wpos.y, wpos.x) / pi *0.5 + 0.5; // 0.0-1.0
			float a = 1.0-_gl_mod(angle + r, 1.0);
			a = max(a-0.7, 0.0) * c;
			return a;
		}

		float Grid(float3 pos)
		{
			float grid_size = 0.4;
			float line_thickness = 0.015;

			float2 m = _gl_mod(abs(pos.xz*sign(pos.xz)), grid_size);
			float s = 0.0;
			if(m.x-line_thickness < 0.0 || m.y-line_thickness < 0.0) {
				return 1.0;
			}
			return 0.0;
		}

		float Circle(float3 pos)
		{
			float o_radius = 5.0;
			float i_radius = 4.0;
			float d = length(pos.xz);
			float c = max(o_radius-(o_radius-_gl_mod(d-_Time.y*1.5, o_radius))-i_radius, 0.0);
			return c;
		}

		float Hex( float2 p, float2 h )
		{
			float2 q = abs(p);
			return max(q.x-h.y,max(q.x+q.y*0.57735,q.y*1.1547)-h.x);
		}

		float HexGrid(float3 p)
		{
			float scale = 1.2;
			float2 grid = float2(0.692, 0.4) * scale;
			float radius = 0.22 * scale;

			float2 p1 = _gl_mod(p.xz, grid) - grid*0.5;
			float c1 = Hex(p1, radius);

			float2 p2 = _gl_mod(p.xz+grid*0.5, grid) - grid*0.5;
			float c2 = Hex(p2, radius);
			return min(c1, c2);
		}
		float3 GuessNormal(float3 p)
		{
			const float d = 0.01;
			return normalize( float3(
				HexGrid(p+float3(  d,0.0,0.0))-HexGrid(p+float3( -d,0.0,0.0)),
				HexGrid(p+float3(0.0,  d,0.0))-HexGrid(p+float3(0.0, -d,0.0)),
				HexGrid(p+float3(0.0,0.0,  d))-HexGrid(p+float3(0.0,0.0, -d)) ));
		}

		void surf (Input IN, inout SurfaceOutput o)
		{
			float2 coord = (IN.screenPos.xy / IN.screenPos.w);

			float3 center = IN.worldPos - _Center;
			float trails = Rings(center);
			float grid_d = HexGrid(center);
			float grid = grid_d > 0.0 ? 1.0 : 0.0;
			float3 n = GuessNormal(center);
			n = mul(UNITY_MATRIX_VP, float4(n,0.0)).xyz;
			float circle = Circle(center);

			o.Albedo = 0.0;
			o.Alpha = 1.0;
			o.Emission = 0.0;
			o.Emission += trails * (0.5 + _Spectra * _RingEmission);
			//o.Albedo += _GridColor * grid * 0.1;
			o.Emission += _GridColor * (grid * circle) * _GridEmission;

			const float blur_radius = 0.005;
			float2 blur_coords[9] = {
				float2( 0.000,  0.000),
				float2( 0.1080925165271518,  -0.9546740999616308)*blur_radius,
				float2(-0.4753686437884934,  -0.8417212473681748)*blur_radius,
				float2( 0.7242715177221273,  -0.6574584801064549)*blur_radius,
				float2(-0.023355087558461607, 0.7964400038854089)*blur_radius,
				float2(-0.8308210026544296,  -0.7015103725420933)*blur_radius,
				float2( 0.3243705688309195,   0.2577797517167695)*blur_radius,
				float2( 0.31851240326305463, -0.2220789454739755)*blur_radius,
				float2(-0.36307729185097637, -0.7307245945773899)*blur_radius
			};
			float depth = 1.0;
			depth = tex2D(_ReflectionDepthTex, coord).r;
			for(int i=1; i<9; ++i) {
				depth = min(depth, tex2D(_ReflectionDepthTex, coord+blur_coords[i]).r);
			}

			float4 H = float4((coord.x) * 2 - 1, (coord.y) * 2 - 1, depth, 1.0);
			float4 D = mul(_ViewProjectInverse, H);
			float3 refpos = D.xyz / D.w;

			float fade_by_depth = 1.0;
			fade_by_depth = max(1.0-abs(refpos.y)*0.3, 0.0);
			float3 refcolor = 0.0;

			float g = saturate((grid_d+0.02)*50.0);
			coord += n.xz * (g>0.0 && g<1.0 ? 1.0 : 0.0) * 0.02;
			for(int i=0; i<9; ++i) {
				refcolor += tex2D(_ReflectionTex, coord+blur_coords[i]*((1.0-fade_by_depth)*0.75+0.25)).rgb * 0.1111;
				//refcolor += tex2D(_ReflectionTex, coord+blur_coords[i]).rgb * 0.1111;
			}

			o.Emission += refcolor * _ReflectionStrength * fade_by_depth * (1.0-grid*0.9);

			//o.Emission = abs(refpos.yyy)*0.25;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
