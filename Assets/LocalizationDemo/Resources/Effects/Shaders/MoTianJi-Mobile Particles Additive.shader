// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "MoTianJi/Mobile Particles Additive"
{

	Properties
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		//_Value ("Add Value", Range(1.0, 10.0)) = 2
		_WorldPanelRange ("_WorldPanelRange " , Vector) = (0,0,0,0)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_Clip("_Clip",Float) = 0 // �Ƿ񱻲ü� 0Ϊ���ü� 1Ϊ�ü�
		[HideInInspector]_ClipRange("Clip Range", Vector) = (0,0,0,0)
	}

	Category
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend SrcAlpha One, Zero One
		//ColorMask RGB
		Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
	
		SubShader
		{
			Pass
			{
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_local __ _UIClipOn
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _WorldPanelRange;
			fixed4 _TintColor;
			float _Clip;
			//half _Value;
			float4 _ClipRange;//渲染范围 左上到右下的矩形范围显示
			
			struct appdata_t
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;	
				float4 worldPos :TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				o.worldPos = mul(unity_ObjectToWorld , v.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				float a = _Clip == 0 || (i.worldPos.x > _WorldPanelRange.x && i.worldPos.y > _WorldPanelRange.y &&  i.worldPos.x < _WorldPanelRange.z && i.worldPos.y < _WorldPanelRange.w)  ? i.color.a : 0 ;
				i.color.a =  a ;

#if _UIClipOn
                //范围裁剪
                float inLeft = lerp(-1, 1, step(_ClipRange.x, i.worldPos.x));
                float inRight = lerp(-1, 1, step(i.worldPos.x, _ClipRange.z));
                float inBottom = lerp(-1, 1, step(_ClipRange.w, i.worldPos.y));
                float inTop = lerp(-1, 1, step(i.worldPos.y, _ClipRange.y));

                float sign = min(inLeft, inRight);
                sign = min(sign, inBottom);
                sign = min(sign, inTop);
                clip(sign);
#endif
				return 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
				//return _Value * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
			}
			ENDCG 
		}
	}	
}
}
