Shader "Hidden/Unlit/Additive Bloom (TextureClip)"
{
	Properties
	{
		_MainTex ("Bloom (RGB)", 2D) = "black" {}
		_BloomIntensity ("Bloom Intensity", Range(0, 4)) = 0.35
		_BloomDesaturation ("Bloom Desaturation", Range(0, 1)) = 0.15
	}

	SubShader
	{
		LOD 100
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"DisableBatching" = "True"
		}

		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			AlphaTest Off
			Fog { Mode Off }
			Blend One OneMinusSrcColor
			ColorMask RGB

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _ClipTex;
			float4 _MainTex_ST;
			float4 _ClipRange0 = float4(0.0, 0.0, 1.0, 1.0);
			half _BloomIntensity;
			half _BloomDesaturation;

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				half4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float2 clipUV : TEXCOORD1;
				half4 color : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			half3 ApplyBloomMap(half3 bloomRgb)
			{
				half3 hdr = max(bloomRgb * _BloomIntensity, 0.0);
				half3 mapped = saturate((hdr * (2.51 * hdr + 0.03)) / (hdr * (2.43 * hdr + 0.59) + 0.14));
				half luminance = dot(mapped, half3(0.2126, 0.7152, 0.0722));
				half3 adjusted = lerp(mapped, luminance.xxx, _BloomDesaturation);
				#if UNITY_COLORSPACE_GAMMA
					adjusted = LinearToGammaSpace(adjusted);
				#endif
				return adjusted;
			}

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				o.color = v.color;
				o.clipUV = (v.vertex.xy * _ClipRange0.zw + _ClipRange0.xy) * 0.5 + float2(0.5, 0.5);
				return o;
			}

			half4 frag (v2f IN) : SV_Target
			{
				half clipAlpha = tex2D(_ClipTex, IN.clipUV).a;
				half3 bloomRgb = tex2D(_MainTex, IN.texcoord).rgb;
				bloomRgb = ApplyBloomMap(bloomRgb) * IN.color.rgb * clipAlpha;
				return half4(bloomRgb, 0);
			}
			ENDCG
		}
	}
	Fallback "Unlit/Additive Bloom"
}
