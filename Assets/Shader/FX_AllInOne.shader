Shader "Particles/FX_AllInOne"
{
	Properties{
		_MainTex("MainTex", 2D) = "white" {}

		//调色
		[MaterialToggle] _SetColor("Set Color", Float) = 0
		_MainColor("Main Color", Color) = (1,1,1,1)
		_MainColorIntensity("MainColor Intensity", Float) = 2

		//MainTex uv滚动
		[MaterialToggle] _MainUvScroll("MainUvScroll", Float) = 0
		_MainTexUV_speed("MainTex UV_speed", Vector) = (0,0,0,0)

		//使用Mask图
		[MaterialToggle] _UseMask("Use Mask", Float) = 0
		_MaskTex("MaskTex", 2D) = "white" {}

		//使用噪点图
		[MaterialToggle] _UseNoise("Use Noise", Float) = 0
		_NoiesTex("NoiesTex", 2D) = "black" {}
		_Noise_speed("Noise Speed", Vector) = (0,0,0,0)
		_NioesIntensity("Nioes Intensity", Float) = 0.1

		//使用溶解
		[MaterialToggle] _DissolveFactor("Use Disso", Float) = 0
		_DissolveTex("Dissolve Tex", 2D) = "white" {}

		//使用菲尼尔
		[MaterialToggle] _UseFresnal("Use Fresnal", Float) = 0
		_FresnalColor("Fresnal Color", Color) = (1,1,1,1)
		_FresnalPow("Fresnal Pow", Range(0, 10)) = 2
		_FresnalIntensity("Fresnal Intensity", Range(0, 10)) = 2

		[HideInInspector] _Mode("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 5.0
		[HideInInspector] _DestBlend("__dst", Float) = 10.0
		[HideInInspector][MaterialToggle] _ZWriteA("__zwt", Float) = 0

		//使用customData里的Uv
		[MaterialToggle] _UseCustomUv("Use CustomData Uv", Float) = 0
	}
	SubShader
	{
		Tags
		{
			"IgnoreProjector" = "True"
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}
		LOD 200
		Pass
		{
			Name "FORWARD"
			Blend [_SrcBlend] [_DestBlend]
			Cull Off
			ZWrite [_ZWriteA]
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ _USEFRESNAL_ON   
			#pragma multi_compile __ _MAINUVSCROLL_ON
			#pragma multi_compile __ _USENOISE_ON
			#pragma multi_compile __ _SETCOLOR_ON
			#pragma multi_compile __ _USEMASK_ON
			#pragma multi_compile __ _DISSOLVEFACTOR_ON
			#pragma multi_compile __ _USECUSTOMUV_ON

			#define UNITY_PASS_FORWARDBASE
			#include "UnityCG.cginc"
			#pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x 
			#pragma target 3.0
			uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
			uniform float4 _MainTexUV_speed;
			uniform float4 _MainColor;
			uniform float _MainColorIntensity;

			uniform sampler2D _MaskTex; uniform float4 _MaskTex_ST;

			uniform sampler2D _NoiesTex; uniform float4 _NoiesTex_ST;
			uniform float4 _Noise_speed;
			uniform float _NioesIntensity;

			uniform sampler2D _DissolveTex; uniform float4 _DissolveTex_ST;

			uniform float _FresnalPow;
			uniform float _FresnalIntensity;
			uniform float4 _FresnalColor;

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord0 : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 vertexColor : COLOR;
			};
			struct VertexOutput
			{
				fixed4 pos : SV_POSITION;
				fixed2 uv0 : TEXCOORD0;
				fixed4 uv1 : TEXCOORD1;
		#ifdef _USEFRESNAL_ON
				fixed4 posWorld : TEXCOORD2;
				fixed3 normalDir : TEXCOORD3;
		#endif
				fixed4 vertexColor : COLOR;
			};


			VertexOutput vert(VertexInput v)
			{
				VertexOutput o = (VertexOutput)0;
				o.uv0 = v.texcoord0;
				o.uv1.xy = v.texcoord0.zw;
				o.uv1.zw = v.texcoord1.xy;
				o.vertexColor = v.vertexColor;
		#ifdef _USEFRESNAL_ON
				o.normalDir = UnityObjectToWorldNormal(v.normal);
				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
		#endif
				o.pos = UnityObjectToClipPos(v.vertex);

		#ifdef _USECUSTOMUV_ON
				o.uv0 = v.texcoord0.xy * v.texcoord0.zw + v.texcoord1.xy;
		#endif
				return o;
			}

			fixed4 frag(VertexOutput i) : SV_Target
			{
				half2 mianTexUv = i.uv0;

		#ifdef _MAINUVSCROLL_ON
				mianTexUv += _MainTexUV_speed.xy * _Time.g + i.uv1.xy;
		#endif

		#ifdef _USENOISE_ON
				half2 noiseUv = i.uv0 + _Noise_speed.xy * _Time.g;
				mianTexUv += tex2D(_NoiesTex, TRANSFORM_TEX(noiseUv, _NoiesTex)).r * _NioesIntensity;
		#endif 

				fixed4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(mianTexUv, _MainTex));

				fixed3 emissive = _MainTex_var.rgb * i.vertexColor.rgb;

		#ifdef _SETCOLOR_ON
				emissive *= _MainColor.rgb * _MainColorIntensity;
		#endif

		#ifdef _USEFRESNAL_ON
				i.normalDir = normalize(i.normalDir);
				fixed3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
				emissive += pow(1.0 - max(0, dot(i.normalDir, viewDirection)), _FresnalPow)*_FresnalIntensity*_FresnalColor.rgb;
		#endif

				fixed alpha = _MainTex_var.a * _MainColor.a * i.vertexColor.a;

		#ifdef _USEMASK_ON
				fixed4 _MaskTex_var = tex2D(_MaskTex, TRANSFORM_TEX(i.uv0, _MaskTex));
				alpha *= _MaskTex_var.r;
		#endif 

		#ifdef _DISSOLVEFACTOR_ON
				fixed4 _DissolveTex_var = tex2D(_DissolveTex, TRANSFORM_TEX(i.uv0, _DissolveTex));
				alpha *= step(i.uv1.b, _DissolveTex_var.r);
		#endif 

				return fixed4(emissive, alpha);
			}
			ENDCG
		}
	}
	FallBack Off
	CustomEditor "CustomShaderGUI"
}
