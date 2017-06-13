Shader "Diem/Xcito Distortion" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "" {}

	}
	
	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	struct v2f 
	{
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};
	
	sampler2D _MainTex;

	float2 distParams; 
	float2 chipSize; 
	float2 centerShift; 
	float2 texCoordScale; 
	float opacity; 

	
	v2f vert( appdata_img v ) 
	{
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv = v.texcoord.xy;
		return o;
	}

	float4 frag(v2f i) : COLOR 
	{
		float2 theta = texCoordScale * chipSize * (i.uv - float2(0.5,0.5)) + centerShift; 
		float rsq = theta.x * theta.x + theta.y * theta.y; 
		float2 rvector = theta * (1.0f + distParams.x * rsq + distParams.y * rsq * rsq) / texCoordScale - centerShift; 
		float2 tc = rvector / chipSize + float2(0.5f, 0.5f); 

		if(tc.x < 0.0 || tc.x > 1.0 || tc.y < 0.0 || tc.y > 1.0) 
			return float4(0.2,0.0,0.0,opacity); 
		else
			return tex2D(_MainTex, tc); 
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
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
  }
  
}

Fallback off
	
} // shader