Shader "DTPD/Stype Distortion" 
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

	float AR;
	float PA_w;
	float CSX;
	float CSY;
	float K1;
	float K2;
	float Oversize;

	
	v2f vert( appdata_img v ) 
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		return o;
	}

	float4 frag(v2f i) : COLOR 
	{
		float PA_h = PA_w / AR;
		float2 tc = i.uv;

		float Xd = (tc[0] - 0.5) * PA_w;  // tc->mm
		float Yd = (tc[1] - 0.5) * PA_h;  // tc->mm

		float r2 = pow(Xd + CSX, 2) + pow(Yd + CSY, 2);
		float f = 1.0 + (r2 * K1) + (r2 * r2 * K2);

		float Xp = f * (Xd + CSX);
		float Yp = f * (Yd + CSY);

		float Xpi = ((0.5 + (Xp / PA_w)) + ((Oversize - 1.0) / 2.0)) * (1.0 / Oversize);
		float Ypi = ((0.5 + (Yp / PA_h)) + ((Oversize - 1.0) / 2.0)) * (1.0 / Oversize);

		if ( //if within defined oversize boundaries
			((Xpi >= 0.0) && (Xpi <= 1.0)) &&
			((Ypi >= 0.0) && (Ypi <= 1.0))
			) 
		{
			float2 tcd = float2(Xpi, Ypi);
			return tex2D(_MainTex, tcd);		//draw sampled pixel
		}
		else 
		{
			return float4(1.0, 1.0, 0.0, 1.0);  //draw empty pixel
		}

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