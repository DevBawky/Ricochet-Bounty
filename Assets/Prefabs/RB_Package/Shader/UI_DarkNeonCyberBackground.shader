Shader "UI/Dark Neon Cyber Background"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Colors)]
        _BaseColor ("Base Color", Color) = (0.006, 0.008, 0.025, 1)
        _SecondaryColor ("Secondary Color", Color) = (0.015, 0.025, 0.08, 1)
        [HDR] _NeonCyan ("Neon Cyan", Color) = (0.0, 0.9, 1.8, 1)
        [HDR] _NeonMagenta ("Neon Magenta", Color) = (1.8, 0.0, 1.2, 1)
        [HDR] _NeonViolet ("Neon Violet", Color) = (0.5, 0.1, 1.6, 1)

        [Header(Shape)]
        _CityIntensity ("City Intensity", Range(0, 2)) = 1
        _WindowIntensity ("Window Intensity", Range(0, 4)) = 1.6
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.35
        _AtmosphereIntensity ("Atmosphere Intensity", Range(0, 2)) = 0.8
        _NoiseSparkle ("Noise Sparkle", Range(0, 1)) = 0.18

        [Header(Motion)]
        _FlowSpeed ("Flow Speed", Vector) = (0.006, 0.012, 0, 0)
        _PulseSpeed ("Pulse Speed", Range(0, 6)) = 0.8
        _NoiseScale ("Noise Scale", Range(1, 32)) = 8
        _ScanlineCount ("Scanline Count", Range(8, 512)) = 180
        _BuildingColumns ("Building Columns", Range(12, 96)) = 46
        _WindowRows ("Window Rows", Range(8, 96)) = 42

        [Header(Fade)]
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.55
        _EdgeFade ("Edge Fade", Range(0, 1)) = 0.08
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
            Name "DarkNeonCyberBackground"

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
            fixed4 _BaseColor;
            fixed4 _SecondaryColor;
            fixed4 _NeonCyan;
            fixed4 _NeonMagenta;
            fixed4 _NeonViolet;
            float _CityIntensity;
            float _WindowIntensity;
            float _ScanlineIntensity;
            float _AtmosphereIntensity;
            float _NoiseSparkle;
            float4 _FlowSpeed;
            float _PulseSpeed;
            float _NoiseScale;
            float _ScanlineCount;
            float _BuildingColumns;
            float _WindowRows;
            float _VignetteStrength;
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

            float Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
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

            float Fbm(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += ValueNoise(uv) * amplitude;
                    uv = uv * 2.03 + 17.13;
                    amplitude *= 0.5;
                }
                return value;
            }

            float BuildingHeight(float columnIndex, float layerSeed)
            {
                float h = Hash21(float2(columnIndex, layerSeed));
                float h2 = Hash21(float2(columnIndex * 2.17 + 4.1, layerSeed + 31.0));
                return 0.12 + h * 0.52 + h2 * h2 * 0.28;
            }

            float BuildingMask(float2 uv, float columns, float layerSeed, float baseY, float maxHeight, out float columnIndex)
            {
                float x = uv.x * columns;
                columnIndex = floor(x);
                float localX = frac(x);
                float columnWidth = 0.72 + Hash21(float2(columnIndex, layerSeed + 9.0)) * 0.24;
                float height = BuildingHeight(columnIndex, layerSeed) * maxHeight;
                float horizontalShape = step((1.0 - columnWidth) * 0.5, localX) * step(localX, 1.0 - (1.0 - columnWidth) * 0.5);
                float verticalShape = step(baseY, uv.y) * step(uv.y, baseY + height);
                return horizontalShape * verticalShape;
            }

            float WindowMask(float2 uv, float columnIndex, float rows, float layerSeed)
            {
                float2 windowGrid = float2(frac(uv.x * _BuildingColumns), frac((uv.y + layerSeed * 0.013) * rows));
                float windowCore = step(0.22, windowGrid.x) * step(windowGrid.x, 0.78) * step(0.38, windowGrid.y) * step(windowGrid.y, 0.58);
                float randomOn = step(0.78, Hash21(float2(columnIndex * 3.1 + floor(uv.y * rows), layerSeed)));
                float broken = step(0.35, Hash21(float2(floor(uv.x * 140.0), floor(uv.y * 120.0) + layerSeed)));
                return windowCore * randomOn * broken;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.uv;
                float2 centeredUv = uv - 0.5;
                float time = _Time.y;
                float pulse = 0.75 + 0.25 * sin(time * _PulseSpeed);

                float2 flowingUv = uv + _FlowSpeed.xy * time;
                float nebula = Fbm(flowingUv * _NoiseScale);
                float fineNebula = Fbm(flowingUv * (_NoiseScale * 2.6) + 31.7);
                float cloud = saturate(nebula * 0.75 + fineNebula * 0.35);

                float scan = frac((uv.y + time * 0.08) * _ScanlineCount);
                float scanline = 1.0 - smoothstep(0.02, 0.65, scan);
                float scanDarken = 0.72 + scanline * 0.28;

                float backColumn;
                float midColumn;
                float frontColumn;
                float backCity = BuildingMask(uv + float2(time * -0.002, 0.0), _BuildingColumns * 0.65, 21.0, 0.1, 0.68, backColumn);
                float midCity = BuildingMask(uv + float2(time * -0.004, 0.0), _BuildingColumns * 0.9, 57.0, 0.02, 0.82, midColumn);
                float frontCity = BuildingMask(uv + float2(time * -0.006, 0.0), _BuildingColumns * 1.2, 91.0, -0.08, 0.92, frontColumn);

                float cityMask = saturate(backCity * 0.45 + midCity * 0.75 + frontCity);
                float backWindows = WindowMask(uv, backColumn, _WindowRows * 0.75, 21.0) * backCity;
                float midWindows = WindowMask(uv, midColumn, _WindowRows, 57.0) * midCity;
                float frontWindows = WindowMask(uv, frontColumn, _WindowRows * 1.2, 91.0) * frontCity;
                float windowMask = saturate(backWindows * 0.45 + midWindows * 0.75 + frontWindows);

                float skylineGlow = smoothstep(0.0, 0.85, cityMask) * (0.45 + cloud * 0.35);
                float horizonGlow = exp(-abs(uv.y - 0.42) * 8.0) * (1.0 - abs(centeredUv.x) * 0.8);
                float tinySparkles = step(1.0 - _NoiseSparkle * 0.08, Hash21(floor(uv * float2(180.0, 90.0)) + floor(time * 2.0))) * step(0.42, uv.y);

                float3 color = lerp(_BaseColor.rgb, _SecondaryColor.rgb, saturate(cloud * 0.28 + horizonGlow * 0.3));
                color += _NeonViolet.rgb * cloud * _AtmosphereIntensity * 0.18;
                color += _NeonCyan.rgb * backCity * 0.12 * _CityIntensity;
                color += _NeonViolet.rgb * midCity * 0.16 * _CityIntensity;
                color += float3(0.004, 0.006, 0.02) * frontCity * _CityIntensity;
                color += _NeonCyan.rgb * skylineGlow * 0.08 * _CityIntensity;
                color += _NeonMagenta.rgb * horizonGlow * 0.12 * _AtmosphereIntensity;
                color += lerp(_NeonCyan.rgb, _NeonMagenta.rgb, Hash21(float2(floor(uv.x * 60.0), floor(uv.y * _WindowRows)))) * windowMask * _WindowIntensity * pulse;
                color += _NeonMagenta.rgb * tinySparkles * _WindowIntensity * 0.55;
                color *= scanDarken;
                color += _NeonCyan.rgb * scanline * _ScanlineIntensity * 0.05;

                float vignette = 1.0 - smoothstep(0.45, 1.15, length(centeredUv * float2(1.2, 1.0)) * 1.7);
                vignette = lerp(1.0, vignette, _VignetteStrength);

                float2 edgeDistance = min(uv, 1.0 - uv);
                float edgeFade = smoothstep(0.0, max(_EdgeFade, 0.0001), min(edgeDistance.x, edgeDistance.y));

                half4 spriteSample = tex2D(_MainTex, uv) + _TextureSampleAdd;
                half alpha = spriteSample.a * input.color.a * _Alpha * edgeFade;

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
