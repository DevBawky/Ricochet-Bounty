Shader "UI/Cyber Energy Projectile"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _CoreColor ("Core Color", Color) = (1.5,2.5,3.5,1)
        [HDR] _RingColor ("Ring Color", Color) = (0.05,1.5,3.5,1)
        _CoreSize ("Core Size", Range(0.01,0.6)) = 0.18
        _RingRadius ("Ring Radius", Range(0.05,0.7)) = 0.36
        _RingWidth ("Ring Width", Range(0.005,0.25)) = 0.07
        _GlowWidth ("Glow Width", Range(0.005,0.4)) = 0.16
        _GlowIntensity ("Glow Intensity", Range(0,8)) = 2.2
        _NoiseScale ("Noise Scale", Range(1,80)) = 22
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.3
        _PulseSpeed ("Pulse Speed", Range(0,20)) = 6
        _PulseStrength ("Pulse Strength", Range(0,1)) = 0.25
        _ScanlineCount ("Scanline Count", Range(1,180)) = 48
        _ScanlineSpeed ("Scanline Speed", Range(-10,10)) = 2
        _ScanlineStrength ("Scanline Strength", Range(0,1)) = 0.18
        _EdgeFade ("Edge Fade", Range(0.001,0.5)) = 0.14
        _Alpha ("Alpha", Range(0,1)) = 1

        [HideInInspector] _TextureSampleAdd ("Texture Sample Add", Vector) = (0,0,0,0)
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "CyberEnergyProjectile"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 worldPosition:TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _TextureSampleAdd, _Color, _CoreColor, _RingColor;
            float4 _ClipRect;
            float _CoreSize, _RingRadius, _RingWidth, _GlowWidth, _GlowIntensity, _NoiseScale, _NoiseStrength;
            float _PulseSpeed, _PulseStrength, _ScanlineCount, _ScanlineSpeed, _ScanlineStrength, _EdgeFade, _Alpha;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float hash21(float2 p) { p = frac(p * float2(123.34,456.21)); p += dot(p,p+45.32); return frac(p.x*p.y); }
            float noise21(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),lerp(hash21(i+float2(0,1)),hash21(i+1),f.x),f.y);
            }

            fixed4 frag(v2f i):SV_Target
            {
                fixed4 sprite = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                float2 p = i.uv - 0.5;
                float radius = length(p);
                float n = noise21(i.uv * _NoiseScale + _Time.y * float2(1.7,-1.1));
                float pulse = 1 + sin(_Time.y * _PulseSpeed + n * 6.283) * _PulseStrength;
                float core = 1 - smoothstep(_CoreSize * 0.35, _CoreSize, radius);
                float ringDistance = abs(radius - _RingRadius * pulse);
                float ring = 1 - smoothstep(_RingWidth * 0.25, _RingWidth, ringDistance);
                float glow = 1 - smoothstep(_RingWidth, _RingWidth + _GlowWidth, ringDistance);
                float scan = 0.5 + 0.5 * sin((i.uv.y * _ScanlineCount + _Time.y * _ScanlineSpeed) * 6.2831853);
                float energy = saturate(core + ring * lerp(1, n, _NoiseStrength) + glow * 0.45);
                energy *= lerp(1, scan, _ScanlineStrength);
                float squareEdge = min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y));
                float fade = smoothstep(0, _EdgeFade, squareEdge) * (1-smoothstep(0.48-_EdgeFade,0.5,radius));
                float alpha = sprite.a * i.color.a * _Alpha * saturate(energy + glow * 0.35) * fade;
                float3 rgb = (core * _CoreColor.rgb + ring * _RingColor.rgb * pulse) * sprite.rgb * i.color.rgb;
                rgb += _RingColor.rgb * n * _NoiseStrength * energy * 0.4;
                rgb += _RingColor.rgb * glow * _GlowIntensity * (0.35 + pulse * 0.3) * sprite.rgb * i.color.rgb;
                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif
                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
