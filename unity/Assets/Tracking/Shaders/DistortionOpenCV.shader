Shader "dmzl/DistortionOpenCV" {
	Properties {
		_MainTex ("_MainTex", 2D) = "green" {}
		[Toggle(INVERTED_MODE)] _Inverted("Inverted", Float) = 0
		[Toggle(DEBUG_MODE)] _Debug("Debug", Float) = 0
		_Fx("Fx", Float) = 1024
		_Fy("Fy", Float) = 1024
		_K1("K1", Float) = 0
		_K2("K2", Float) = 0
		_P1("P1", Float) = 0
		_P2("P2", Float) = 0
		_K3("K3", Float) = 0
		_K4("K4", Float) = 0
		_K5("K5", Float) = 0
		_K6("K6", Float) = 0
		_Cx("Cx", Float) = 0
		_Cy("Cy", Float) = 0
	}
SubShader 
	{
	Pass
	{
		CGPROGRAM
			#include "UnityCG.cginc"

			#pragma shader_feature INVERTED_MODE
			#pragma shader_feature DEBUG_MODE
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _MainTex;
			float _Inverted;
			float _Debug;
			float _Fx, _Fy, _K1, _K2, _K3, _K4, _K5, _K6, _P1, _P2, _Cx, _Cy;

			struct VertexInput {
				float4 Position : POSITION;
				float2 uv_MainTex : TEXCOORD0;
			};
			
			struct FragInput {
				float4 Position : SV_POSITION;
				float2	uv_MainTex : TEXCOORD0;
			};
			
			
			FragInput vert(VertexInput In) {
				FragInput Out;
				Out.Position = UnityObjectToClipPos (In.Position );
				Out.uv_MainTex = In.uv_MainTex;
				return Out;
			}
			
  			// http://stackoverflow.com/questions/21615298/opencv-distort-back
			// https://github.com/Polypulse/LensCalibrator/blob/master/Shaders/Private/DistortionCorrectionMapGeneration.usf
			// https://github.com/darglein/saiga/blob/master/src/saiga/vision/cameraModel/Distortion.h
			// https://docs.opencv.org/3.4/da/d54/group__imgproc__transform.html#ga7dfb72c9cf9780a347fbe3d1c47e5d5a
			float2 Distort(float2 uv)
			{
				float inverse = lerp(1, -1, _Inverted);
				float cx = _Cx;
				float cy = _Cy;
				float k1 = _K1 * inverse;
				float k2 = _K2 * inverse;
				float k3 = _K3 * inverse;
				float k4 = _K4 * inverse;
				float k5 = _K5 * inverse;
				float k6 = _K6 * inverse;
				float p1 = _P1 * inverse;
				float p2 = _P2 * inverse;

				float x = (uv.x - 0.5 - cx) / _Fx;
				float y = (uv.y - 0.5 - cy) / _Fy;

				float x2 = x * x;
				float y2 = y * y;
				float r2 = x2 + y2;
				float _2xy = 2.0 * x * y;
				float r4 = r2 * r2;
				float r6 = r4 * r2;
				
				float radial = (1.0 + k1 * r2 + k2 * r4 + k3 * r6) / (1.0 + k4 * r2 + k5 * r4 + k6 * r6);

				float tangentialX = p1 * _2xy + p2 * (r2 + 2.0 * x2);
				float tangentialY = p1 * (r2 + 2.0 * y2) + p2 * _2xy;

				float xd = x * radial + tangentialX;
				float yd = y * radial + tangentialY;

				return float2(xd * _Fx + 0.5 + cx, yd * _Fy + 0.5 + cy);
			}
			
			
			fixed4 frag(FragInput In) : SV_Target {
				float2 uv = In.uv_MainTex;
				uv = Distort(uv);

				if ( uv.x < 0 || uv.x > 1 || uv.y  < 0 || uv.y > 1)  
					return float4(.1,0,.1,1);

				return lerp(tex2D( _MainTex, uv ), float4( uv.x, uv.y, 0.0, 1 ), _Debug);
			}

		ENDCG
	}	
	} 
}