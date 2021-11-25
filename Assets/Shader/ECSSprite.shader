Shader "ECSSprite"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Offset("_Offset",Vector)=(1,1,0,0)
	}
	SubShader
	{
		Tags {"IgnoreProjector"="True" "RenderType"="Opeque" }
        Blend SrcAlpha OneMinusSrcAlpha     
        //ZWrite Off
        //看情况而定还有其他优化方案，比如如果像素着色器比较复杂，可以通过PreZ的方式优化，用一个pass先写深度
        //在当前demo中，一个pass的性能比较高，而且需要开深度写入
        //虽然clip会打断earlyz，但clip也省略了一下blend的操作，所以目前这样的形式已经是最快了
        //如果用blend且不开深度写入的话会消耗更多的时间
 
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
				clip(col.a - 0.1);//会打断earlyz
				return col;
			}
			ENDCG
		}
	}
}