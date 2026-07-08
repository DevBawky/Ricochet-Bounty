Shader "UI/Cyberpunk Score Overheat"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Heat Control)]
        _Heat ("Heat", Range(0, 1)) = 0
        _Alpha ("Alpha", Range(0, 1)) = 1

        [Header(Colors)]
        _ColdColor ("Cold Neon", Color) = (0.0, 0.55, 1.4, 1)
        [HDR] _WarmColor ("Warm Neon", Color) = (1.8, 0.0, 1.3, 1)
        [HDR] _HotColor ("Overheat", Color) = (2.4, 0.35, 0.02, 1)
        [HDR] _WhiteHotColor ("White Hot", Color) = (3.0, 2.0, 0.75, 1)
        _CoreColor ("Dark Core", Color) = (0.008, 0.006, 0.018, 1)

        [Header(Effects)]
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.2
        _PulseIntensity ("Pulse Intensity", Range(0, 2)) = 0.5
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.18
        _SparkIntensity ("Spark Intensity", Range(0, 2)) = 0.4
        _WarningBandIntensity ("Warning Band Intensity", Range(0, 2)) = 0.65
        _Distortion ("Heat Distortion", Range(0, 0.08)) = 0.015

        [Header(Shape)]
        _EdgeGlowWidth ("Edge Glow Width", Range(0.01, 0.6)) = 0.18
        _ScanlineCount ("Scanline Count", Range(8, 256)) = 96
        _FlowSpeed ("Flow Speed", Vector) = (0.035, 0.12, 0, 0)
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.35

        [Header(UI)]
        [HideInInspector] _TextureSampleAdd ("Texture Sample Add", Vector) = (0, 0, 0, 0)
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "CyberpunkScoreOverheat"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Heat;
            float _Alpha;
            fixed4 _ColdColor;
            fixed4 _WarmColor;
            fixed4 _HotColor;
            fixed4 _WhiteHotColor;
            fixed4 _CoreColor;
            float _GlowIntensity;
            float _PulseIntensity;
            float _ScanlineIntensity;
            float _SparkIntensity;
            float _WarningBandIntensity;
            float _Distortion;
            float _EdgeGlowWidth;
            float _ScanlineCount;
            float4 _FlowSpeed;
            float _VignetteStrength;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float EdgeMask(float2 uv, float width)
            {
                float2 edgeDistance = min(uv, 1.0 - uv);
                float nearestEdge = min(edgeDistance.x, edgeDistance.y);
                return 1.0 - smoothstep(0.0, max(width, 0.0001), nearestEdge);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float heat = saturate(_Heat);
                float time = _Time.y;
                float2 uv = input.uv;
                float2 centeredUv = uv - 0.5;

                float agitation = heat * heat;
                float noise = ValueNoise(uv * 18.0 + time * _FlowSpeed.xy * (1.0 + heat * 5.0));
                float distortionWave = sin((uv.y * 18.0 + time * 8.0) + noise * 4.0);
                uv.x += distortionWave * _Distortion * agitation;

                half4 spriteSample = tex2D(_MainTex, uv) + _TextureSampleAdd;

                float pulse = 1.0 + sin(time * lerp(2.0, 12.0, heat) + noise * 4.0) * _PulseIntensity * (0.15 + heat * 0.85);
                float edgeGlow = EdgeMask(input.uv, _EdgeGlowWidth) * _GlowIntensity * pulse;
                float radialHeat = 1.0 - saturate(length(centeredUv * float2(1.15, 1.0)) * 1.55);

                float scan = frac((input.uv.y + time * lerp(0.04, 0.22, heat)) * _ScanlineCount);
                float scanline = (1.0 - smoothstep(0.0, 0.55, scan)) * _ScanlineIntensity;

                float warningBand = frac((input.uv.x - input.uv.y * 0.65 + time * lerp(0.25, 1.3, heat)) * 8.0);
                warningBand = (1.0 - smoothstep(0.0, 0.18, warningBand)) * smoothstep(0.45, 0.85, heat) * _WarningBandIntensity;

                float sparkGrid = Hash21(floor(input.uv * float2(90.0, 54.0)) + floor(time * lerp(8.0, 32.0, heat)));
                float sparks = step(1.0 - _SparkIntensity * heat * 0.08, sparkGrid);

                float3 heatRampA = lerp(_ColdColor.rgb, _WarmColor.rgb, smoothstep(0.0, 0.55, heat));
                float3 heatRampB = lerp(_HotColor.rgb, _WhiteHotColor.rgb, smoothstep(0.72, 1.0, heat));
                float3 neonColor = lerp(heatRampA, heatRampB, smoothstep(0.48, 0.95, heat));

                float3 color = _CoreColor.rgb;
                color += neonColor * (radialHeat * 0.22 + edgeGlow * 0.85 + scanline * 0.35);
                color += _WarmColor.rgb * warningBand * 0.45;
                color += _WhiteHotColor.rgb * sparks * heat;
                color += neonColor * noise * heat * 0.18;

                float vignette = 1.0 - smoothstep(0.45, 1.1, length(centeredUv * float2(1.2, 1.0)) * 1.5);
                vignette = lerp(1.0, vignette, _VignetteStrength);

                half alpha = spriteSample.a * input.color.a * _Alpha * saturate(0.45 + edgeGlow + radialHeat * 0.35 + heat * 0.25);

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return fixed4(color * input.color.rgb * vignette, alpha);
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
