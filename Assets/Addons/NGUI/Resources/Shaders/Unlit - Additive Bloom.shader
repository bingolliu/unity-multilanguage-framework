Shader "Unlit/Additive Bloom"
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
			float4 _MainTex_ST;
			half _BloomIntensity;
			half _BloomDesaturation;

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				half4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				half4 color : COLOR;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				o.color = v.color;
				return o;
			}

			half4 frag (v2f IN) : SV_Target
			{
				half4 bloom = tex2D(_MainTex, IN.texcoord);
				half3 hdr = max(bloom.rgb * _BloomIntensity, 0.0);
				half3 mapped = saturate((hdr * (2.51 * hdr + 0.03)) / (hdr * (2.43 * hdr + 0.59) + 0.14));
				half luminance = dot(mapped, half3(0.2126, 0.7152, 0.0722));
				bloom.rgb = lerp(mapped, luminance.xxx, _BloomDesaturation);
				#if UNITY_COLORSPACE_GAMMA
					bloom.rgb = LinearToGammaSpace(bloom.rgb);
				#endif
				return half4(bloom.rgb * IN.color.rgb, 0);
			}
			ENDCG
		}
	}
}