Shader "Unlit/MapGroundSimple"
{
    Properties
    {
        _Color0 ("Color0", Color) = (1,1,1,1)
        _Color1 ("Color1", Color) = (1,1,1,1)
        _Color2 ("Color2", Color) = (1,1,1,1)
        _Color3 ("Color3", Color) = (1,1,1,1)
		_Splat0 ("Layer 1", 2D) = "white" {}
		_Splat1 ("Layer 2", 2D) = "white" {}
		_Splat2 ("Layer 3", 2D) = "white" {}
		_Splat3 ("Layer 4", 2D) = "white" {}
        _SpriteColor ("SpriteColor", Color) = (1,1,1,1)
        _Splat ("Mask", 2D) = "gray" { }
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
				float2 uv : TEXCOORD0;
				float2 uv0 : TEXCOORD1;
				float2 uv1 : TEXCOORD2;
				float2 uv2 : TEXCOORD3;
				float2 uv3 : TEXCOORD4;
				float4 vertex : SV_POSITION;
            };

            uniform sampler2D _Splat;
            float4 _Splat_ST;

			sampler2D _Splat0;
			float4 _Splat0_ST;
			
			sampler2D _Splat1;
			float4 _Splat1_ST;
			
			sampler2D _Splat2;
			float4 _Splat2_ST;
			
			sampler2D _Splat3;
			float4 _Splat3_ST;

            uniform 	float4 _SpriteColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Splat);
                o.uv0 = TRANSFORM_TEX(v.uv, _Splat0);
                o.uv1 = TRANSFORM_TEX(v.uv, _Splat1);
                o.uv2 = TRANSFORM_TEX(v.uv, _Splat2);
                o.uv3 = TRANSFORM_TEX(v.uv, _Splat3);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 _Color0 = tex2D(_Splat0, i.uv0);
                fixed4 _Color1 = tex2D(_Splat1, i.uv1);
                fixed4 _Color2 = tex2D(_Splat2, i.uv2);
                fixed4 _Color3 = tex2D(_Splat3, i.uv3);
                
                fixed3 u_xlat10_0 = tex2D(_Splat, i.uv).xyz;
                fixed4 u_xlat16_1 = u_xlat10_0.xxxx * _Color0 + u_xlat10_0.yyyy * _Color1 + u_xlat10_0.zzzz * _Color2;
                fixed4 u_xlat16_2 = 1.0 - u_xlat10_0.x - u_xlat10_0.y - u_xlat10_0.z;
                fixed4 u_xlat16_0 = u_xlat16_2 * _Color3 + u_xlat16_1;
                return u_xlat16_0 * _SpriteColor;
            }
            ENDCG
        }
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            uniform sampler2D _Splat;
            float4 _Splat_ST;
            uniform 	float4 _Color0;
            uniform 	float4 _Color1;
            uniform 	float4 _Color2;
            uniform 	float4 _Color3;
            uniform 	float4 _SpriteColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Splat);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 u_xlat10_0 = tex2D(_Splat, i.uv).xyz;
                fixed4 u_xlat16_1 = u_xlat10_0.xxxx * _Color0 + u_xlat10_0.yyyy * _Color1 + u_xlat10_0.zzzz * _Color2;
                fixed4 u_xlat16_2 = 1.0 - u_xlat10_0.x - u_xlat10_0.y - u_xlat10_0.z;
                fixed4 u_xlat16_0 = u_xlat16_2 * _Color3 + u_xlat16_1;
                return u_xlat16_0 * _SpriteColor;
            }
            ENDCG
        }
    }

}
