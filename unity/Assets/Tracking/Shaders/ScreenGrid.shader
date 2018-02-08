Shader "Diem/ScreenGrid" {
	Properties 
	{
		_MainTex("Main Texture (RGBA)", 2D) = "white" {}
		_Color("Grid Color", Color) = (1, 0, 0, 1)
		_LineCount ("Line Count", Int) = 20
		_LineWidth ("Line Width in Pixels", Float) = 0.05
	}


	SubShader 
	{
		Pass 
		{
			Name "ScreenGrid"
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest 
			#include "UnityCG.cginc"
						
			uniform sampler2D _MainTex;

			uniform float4 _Color;
			uniform int _LineCount;
			uniform float _LineWidth;
 
			float4 frag (v2f_img i) : COLOR
			{
				float2 f = frac(i.uv * _LineCount);

				if (f.x > 0 && f.x < _LineWidth 
					||f.y > 0 && f.y < _LineWidth)
					return _Color;
				else
					return tex2D (_MainTex, i.uv);

			}
			ENDCG
		}
		
	}

	Fallback Off
} 
