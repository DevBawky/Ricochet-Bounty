Shader "UI/Cyberpunk Neon Grid"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Grid)]
        _BackgroundColor ("Background Color", Color) = (0.015, 0.01, 0.05, 1)
        [HDR] _VerticalLineColor ("Vertical Line Color", Color) = (0.0, 0.9, 1.8, 1)
        [HDR] _HorizontalLineColor ("Horizontal Line Color", Color) = (0.0, 0.55, 1.6, 1)
        [HDR] _AccentColor ("Accent Color", Color) = (2.0, 0.0, 1.4, 1)
        _VerticalLineCount ("Vertical Line Count", Range(1, 64)) = 8
        _HorizontalLineCount ("Horizontal Line Count", Range(1, 64)) = 5
        _LineWidth ("Line Width", Range(0.002, 0.12)) = 0.025
        _LineSoftness ("Line Softness", Range(0, 4)) = 0.75
        _MajorLineEvery ("Major Line Every", Range(2, 12)) = 5
        _MajorLineBoost ("Major Line Boost", Range(0, 4)) = 1.5

        [Header(Motion)]
        _ScrollSpeed ("Scroll Speed", Vector) = (0.015, 0.035, 0, 0)
        _PulseSpeed ("Pulse Speed", Range(0, 8)) = 2
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.25
        _GlitchStrength ("Glitch Strength", Range(0, 0.08)) = 0

        [Header(Vignette)]
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.65
        _VignetteSoftness ("Vignette Softness", Range(0.01, 1)) = 0.45
        _EdgeFade ("Edge Fade", Range(0, 1)) = 0.15
        _Alpha ("Alpha", Range(0, 1)) = 1

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
            "RenderPipeline" = "UniversalPipeline"
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
            Name "CyberpunkNeonGrid"

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
            fixed4 _BackgroundColor;
            fixed4 _VerticalLineColor;
            fixed4 _HorizontalLineColor;
            fixed4 _AccentColor;
            float _VerticalLineCount;
            float _HorizontalLineCount;
            float _LineWidth;
            float _LineSoftness;
            float _MajorLineEvery;
            float _MajorLineBoost;
            float4 _ScrollSpeed;
            float _PulseSpeed;
            float _PulseStrength;
            float _GlitchStrength;
            float _VignetteStrength;
            float _VignetteSoftness;
            float _EdgeFade;
            float _Alpha;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            float2 GridLines(float2 gridUv, float lineWidth)
            {
                float2 gridFraction = frac(gridUv);
                float2 distanceToLine = min(gridFraction, 1.0 - gridFraction);
                float2 aa = max(fwidth(gridUv) * max(_LineSoftness, 0.001), float2(0.0001, 0.0001));
                float2 width = float2(lineWidth, lineWidth);
                return 1.0 - smoothstep(width, width + aa, distanceToLine);
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.uv;
                float2 centeredUv = uv - 0.5;
                float time = _Time.y;

                float scanBand = floor((uv.y + time * 0.7) * 24.0);
                float glitch = (Hash21(float2(scanBand, floor(time * 8.0))) - 0.5) * _GlitchStrength;
                float2 animatedUv = uv + _ScrollSpeed.xy * time + float2(glitch, 0.0);

                float2 lineCounts = max(float2(_VerticalLineCount, _HorizontalLineCount), float2(1.0, 1.0));
                float2 gridUv = animatedUv * lineCounts;
                float2 minorGrid = GridLines(gridUv, _LineWidth);
                float2 majorGridLines = GridLines(gridUv / max(_MajorLineEvery, 1.0), _LineWidth * 1.35);
                float majorGrid = saturate(max(majorGridLines.x, majorGridLines.y));

                float diagonalSweep = smoothstep(0.02, 0.0, abs(frac((uv.x + uv.y + time * 0.35) * 3.0) - 0.5));
                float pulse = 1.0 + sin(time * _PulseSpeed + uv.y * 12.0) * _PulseStrength;

                float accentMask = saturate(majorGrid + diagonalSweep * 0.25);

                float3 color = _BackgroundColor.rgb;
                color += _VerticalLineColor.rgb * minorGrid.x * pulse;
                color += _HorizontalLineColor.rgb * minorGrid.y * pulse;
                color += (_VerticalLineColor.rgb + _HorizontalLineColor.rgb) * 0.5 * majorGrid * _MajorLineBoost * pulse;
                color += _AccentColor.rgb * accentMask * 0.45;

                float dist = length(centeredUv * float2(1.15, 1.0));
                float vignette = 1.0 - smoothstep(1.0 - _VignetteSoftness, 1.0, dist * 2.0);
                vignette = lerp(1.0, vignette, _VignetteStrength);

                float2 edgeDistance = min(uv, 1.0 - uv);
                float edgeFade = smoothstep(0.0, max(_EdgeFade, 0.0001), min(edgeDistance.x, edgeDistance.y));

                half4 spriteSample = tex2D(_MainTex, uv) + _TextureSampleAdd;
                half alpha = spriteSample.a * input.color.a * _Alpha * vignette * edgeFade;

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return fixed4(color * input.color.rgb, alpha);
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
