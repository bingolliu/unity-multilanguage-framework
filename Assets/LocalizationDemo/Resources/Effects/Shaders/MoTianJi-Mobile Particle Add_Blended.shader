Shader "MoTianJi/Mobile Particles Add_Blend"
{

	Properties
	{
		[HDR]_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_WorldPanelRange ("_WorldPanelRange " , Vector) = (0,0,0,0)
		_MainTex ("Particle Texture", 2D) = "white" {}
		[MaterialToggle]_UV_Revert90("UV_Revert90",Float) = 0
		[MaterialToggle] _UseRtoA ("Use R  to A ?", Float ) = 0	
		_RGBA("RGBA select One",Vector) = (1,0,0,0)		
		//[Enum(UnityEngine.Rendering.BlendMode,1,10)] DstBlend("DstBlend", Float) = 1//One		
		[Enum(Add,1,Blended,10)] _DstBlend("DstBlend", Float) = 1//One
		[Enum(Add,0,Blended,1)] _SrcAlphaBlend("SrcAlphaBlend", Float) = 1//One
		_DepthOffset("Depth Offset", Float) = 0

		_Clip("_Clip",Float) = 0 //
		[HideInInspector]_ClipRange("Clip Range", Vector) = (0,0,0,0)
	}

	Category
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend One [_DstBlend], [_SrcAlphaBlend] [_DstBlend]
		//AlphaTest Greater .01
		//ColorMask RGB
		Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
	
		SubShader
		{
			Pass
			{
			Offset [_DepthOffset], [_DepthOffset]
			CGPROGRAM
			#pragma multi_compile_local __ _UIClipOn
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _WorldPanelRange;
			fixed4 _TintColor,_RGBA;
			float _Clip; 
			fixed _UseRtoA,_UV_Revert90;
			//half _Value;
			float4 _ClipRange;
			
			struct appdata_t
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;				
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;	
				float4 worldPos :TEXCOORD1;
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(lerp(v.texcoord.xy,v.texcoord.yx,_UV_Revert90),_MainTex);
				o.worldPos = mul(unity_ObjectToWorld , v.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float a = _Clip == 0 || (i.worldPos.x > _WorldPanelRange.x && i.worldPos.y > _WorldPanelRange.y &&  i.worldPos.x < _WorldPanelRange.z && i.worldPos.y < _WorldPanelRange.w)  ? i.color.a : 0 ;				
				i.color.a =  a ;
				float4 col = tex2D(_MainTex, i.texcoord);
				float4 rgba = _RGBA * col;
				float  Use_which = max(max(max(rgba.r,rgba.g),rgba.b),rgba.a);
				
				float  Alpha = 2 * i.color.a * _TintColor.a * lerp( col.a, Use_which, _UseRtoA );
				float3  emission = 2 * i.color.rgb * _TintColor.rgb * col.rgb *i.color.a *_TintColor.a*2 *col.a ;

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
				return float4(emission,Alpha);
				//return _Value * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
			}
			ENDCG 
		}
	}	
}
}
