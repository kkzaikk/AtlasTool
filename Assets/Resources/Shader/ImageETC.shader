Shader "Common/ImageETC"
{
    Properties 
	{
		 _MainTex ("Base (RGB)", 2D) = "white" { }
		 _ETCTex("AlphaTex",2D) = "white"{}

		 // required for UI.Mask
         _StencilComp ("Stencil Comparison", Float) = 8
         _Stencil ("Stencil ID", Float) = 0
         _StencilOp ("Stencil Operation", Float) = 0
         _StencilWriteMask ("Stencil Write Mask", Float) = 255
         _StencilReadMask ("Stencil Read Mask", Float) = 255
         _ColorMask ("Color Mask", Float) = 15
		 }
    SubShader
	{

		 Tags
		 {
			"Queue" = "Transparent+1"
		 }

		 // required for UI.Mask
         Stencil
         {
             Ref [_Stencil]
             Comp [_StencilComp]
             Pass [_StencilOp] 
             ReadMask [_StencilReadMask]
             WriteMask [_StencilWriteMask]
         }
         ColorMask [_ColorMask]         

         Pass
		 {
			Lighting Off
			ZTest Off
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _ETCTex;

			float _AlphaFactor;
		
			struct v2f 
			{
				float4  pos : SV_POSITION;
				float2  uv : TEXCOORD0;
				float4 color :COLOR;
			};

			half4 _MainTex_ST;
			half4 _ETCTex_ST;

			v2f vert (appdata_full v)
			{
				v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				o.uv =  v.texcoord;
				o.color = v.color;
				return o;
			}

			half4 frag (v2f i) : COLOR
			{
				half4 texcol = tex2D (_MainTex, i.uv);

				half4 result = texcol;

				result.a = tex2D(_ETCTex,i.uv)*i.color.a ;

				return result;
			}
			ENDCG
		 }
    }
}