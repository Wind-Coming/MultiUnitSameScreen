Shader "ECSSprite"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Offset("_Offset",Vector)=(1,1,0,0)
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
 
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
			#include "UnityCG.cginc"
            #pragma target 3.0
 
			struct appdata
			{
				float4 vertex : POSITION;
			    UNITY_VERTEX_INPUT_INSTANCE_ID
				float2 uv : TEXCOORD0;
            };
 
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};
 
			sampler2D _MainTex;
			float4 _MainTex_ST;
        			
            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Offset) //渲染区域
            UNITY_INSTANCING_BUFFER_END(Props)
			
			v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				o.vertex = UnityObjectToClipPos(v.vertex);
                fixed4 l =  UNITY_ACCESS_INSTANCED_PROP(Props, _Offset);
                o.uv = v.uv.xy * l.xy + l.zw;
                return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				clip(col.a - 0.1);
				return col;
			}
			ENDCG
		}
	}
}