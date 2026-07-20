Shader "UI/Cyberpunk City Background Loop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Background Texture", 2D) = "white" {}
        _Color ("Image Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)

        [Header(Neon)]
        [HDR] _NeonColorA ("Neon Color A", Color) = (0.0, 1.4, 2.8, 1)
        [HDR] _NeonColorB ("Neon Color B", Color) = (2.6, 0.0, 1.8, 1)
        _NeonIntensity ("Neon Intensity", Range(0, 5)) = 1.35
        _NeonThreshold ("Neon Brightness Threshold", Range(0, 1)) = 0.58
        _NeonSoftness ("Neon Threshold Softness", Range(0.01, 0.5)) = 0.14
        _GlowRadius ("Glow Radius (Pixels)", Range(0, 12)) = 3
        _NeonPulseAmount ("Neon Pulse Amount", Range(0, 1)) = 0.12
        [IntRange] _NeonPulseCycles ("Neon Pulse Cycles Per Loop", Range(0, 8)) = 2

        [Header(Fog)]
        [HDR] _FogColor ("Fog Color", Color) = (0.12, 0.45, 0.8, 1)
        _FogIntensity ("Fog Intensity", Range(0, 2)) = 0.38
        _FogScale ("Fog Scale", Range(1, 16)) = 4.5
        _FogCoverage ("Fog Coverage", Range(0, 1)) = 0.48
        _FogHeight ("Fog Height", Range(0, 1)) = 0.58
        _FogMotionRadius ("Fog Motion Distance", Range(0, 2)) = 0.55

        [Header(Noise)]
        _NoiseScale ("Noise Scale", Range(4, 128)) = 42
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.08
        _NoiseMotionRadius ("Noise Motion Distance", Range(0, 2)) = 0.8

        [Header(Seamless Motion)]
        _AnimationSpeed ("Animation Speed", Range(0, 2)) = 1
        _LoopDuration ("Loop Duration (Seconds)", Range(2, 60)) = 24

        [Header(Finishing)]
        _Saturation ("Saturation", Range(0, 2)) = 1.15
        _Contrast ("Contrast", Range(0, 2)) = 1.08
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.32
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
            Name "CyberpunkCityBackgroundLoop"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #define TWO_PI 6.28318530718

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _RendererColor;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _NeonColorA;
            fixed4 _NeonColorB;
            float _NeonIntensity;
            float _NeonThreshold;
            float _NeonSoftness;
            float _GlowRadius;
            float _NeonPulseAmount;
            float _NeonPulseCycles;

            fixed4 _FogColor;
            float _FogIntensity;
            float _FogScale;
            float _FogCoverage;
            float _FogHeight;
            float _FogMotionRadius;

            float _NoiseScale;
            float _NoiseIntensity;
            float _NoiseMotionRadius;

            float _AnimationSpeed;
            float _LoopDuration;
            float _Saturation;
            float _Contrast;
            float _VignetteStrength;
            float _Alpha;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color * _RendererColor;
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                float2 blend = local * local * (3.0 - 2.0 * local);

                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));
                return lerp(lerp(a, b, blend.x), lerp(c, d, blend.x), blend.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                [unroll]
                for (int octave = 0; octave < 3; octave++)
                {
                    value += ValueNoise(p) * amplitude;
                    p = p * 2.03 + float2(17.13, 9.71);
                    amplitude *= 0.5;
                }

                return value;
            }

            float Luminance(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.uv;
                half4 source = tex2D(_MainTex, uv) + _TextureSampleAdd;
                float3 baseColor = source.rgb * input.color.rgb;

                // The phase always travels around a circle, so the last frame joins the first.
                float duration = max(_LoopDuration, 0.01);
                float loopPhase = frac((_Time.y * _AnimationSpeed) / duration);
                float angle = loopPhase * TWO_PI;
                float2 loopPath = float2(cos(angle), sin(angle));
                float2 doubleLoopPath = float2(cos(angle * 2.0 + 1.7), sin(angle * 2.0 + 1.7));

                float3 grayscale = Luminance(baseColor).xxx;
                baseColor = lerp(grayscale, baseColor, _Saturation);
                baseColor = (baseColor - 0.5) * _Contrast + 0.5;

                // Four nearby samples turn bright city lights into a texture-aware soft halo.
                float2 glowOffset = _MainTex_TexelSize.xy * _GlowRadius;
                float neighborLight = 0.0;
                neighborLight += Luminance(tex2D(_MainTex, uv + float2(glowOffset.x, 0.0)).rgb);
                neighborLight += Luminance(tex2D(_MainTex, uv - float2(glowOffset.x, 0.0)).rgb);
                neighborLight += Luminance(tex2D(_MainTex, uv + float2(0.0, glowOffset.y)).rgb);
                neighborLight += Luminance(tex2D(_MainTex, uv - float2(0.0, glowOffset.y)).rgb);
                neighborLight *= 0.25;

                float sourceLight = Luminance(source.rgb);
                float neonSource = max(sourceLight, neighborLight);
                float neonMask = smoothstep(
                    _NeonThreshold - _NeonSoftness,
                    _NeonThreshold + _NeonSoftness,
                    neonSource);
                float haloMask = saturate(neighborLight - sourceLight * 0.35) * neonMask;

                float fogNoiseA = Fbm(uv * _FogScale + loopPath * _FogMotionRadius);
                float fogNoiseB = Fbm(uv * (_FogScale * 1.83) + doubleLoopPath * (_FogMotionRadius * 0.7) + 23.4);
                float fogNoise = saturate(fogNoiseA * 0.68 + fogNoiseB * 0.42);
                float fogMask = smoothstep(_FogCoverage, min(_FogCoverage + 0.28, 1.0), fogNoise);
                float heightFade = 1.0 - smoothstep(_FogHeight, 1.0, uv.y);
                fogMask *= lerp(0.35, 1.0, heightFade);

                float fineNoise = ValueNoise(uv * _NoiseScale + doubleLoopPath * _NoiseMotionRadius + 47.2);
                float noiseSigned = (fineNoise - 0.5) * 2.0;

                float pulseCycles = floor(_NeonPulseCycles + 0.5);
                float pulse = 1.0 + sin(angle * pulseCycles + sourceLight * 5.0) * _NeonPulseAmount;
                float neonBlend = saturate(fogNoiseA * 0.65 + uv.y * 0.35);
                float3 neonColor = lerp(_NeonColorA.rgb, _NeonColorB.rgb, neonBlend);

                float3 color = baseColor;
                color += neonColor * (neonMask * 0.28 + haloMask * 0.72) * _NeonIntensity * pulse;
                color += _FogColor.rgb * fogMask * _FogIntensity * (0.55 + neonMask * 0.45);
                color += noiseSigned * _NoiseIntensity * (0.08 + baseColor * 0.2);

                float2 centeredUv = uv - 0.5;
                float vignette = 1.0 - smoothstep(0.35, 0.78, length(centeredUv * float2(1.15, 1.0)));
                color *= lerp(1.0, vignette, _VignetteStrength);

                half alpha = source.a * input.color.a * _Alpha;

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return half4(max(color, 0.0), alpha);
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
