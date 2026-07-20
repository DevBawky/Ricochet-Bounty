Shader "UI/Cyber Panel Dissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _NoiseScale ("Noise Scale", Range(1,100)) = 28
        _NoiseSpeed ("Noise Speed", Vector) = (0.4,-0.25,0,0)
        _BlockCount ("Digital Block Count", Range(1,100)) = 32
        _ScanlineCount ("Scanline Count", Range(1,300)) = 120
        _ScanlineSpeed ("Scanline Speed", Range(-10,10)) = 1.5
        _GlitchStrength ("Glitch Strength", Range(0,0.2)) = 0.025
        _Softness ("Dissolve Softness", Range(0.001,0.25)) = 0.035
        [HDR] _EdgeColor ("Edge Color", Color) = (0.05,1.8,3.5,1)
        _EdgeWidth ("Edge Width", Range(0.001,0.25)) = 0.055
        _EdgeIntensity ("Edge Intensity", Range(0,5)) = 1.6
        [HideInInspector] _UseSDF ("Use SDF", Float) = 0
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
            Name "CyberPanelDissolve"
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
            sampler2D _MainTex; fixed4 _TextureSampleAdd,_Color,_EdgeColor; float4 _ClipRect,_NoiseSpeed;
            float _DissolveAmount,_NoiseScale,_BlockCount,_ScanlineCount,_ScanlineSpeed,_GlitchStrength,_Softness,_EdgeWidth,_EdgeIntensity,_UseSDF;
            v2f vert(appdata_t v){ v2f o; o.worldPosition=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o; }
            float hash21(float2 p){ p=frac(p*float2(127.1,311.7)); p+=dot(p,p+19.19); return frac(p.x*p.y); }
            float noise21(float2 p){ float2 q=floor(p),f=frac(p); f=f*f*(3-2*f); return lerp(lerp(hash21(q),hash21(q+float2(1,0)),f.x),lerp(hash21(q+float2(0,1)),hash21(q+1),f.x),f.y); }
            fixed4 frag(v2f i):SV_Target
            {
                float2 block=floor(i.uv*_BlockCount)/max(_BlockCount,1);
                float scan=sin((i.uv.y*_ScanlineCount+_Time.y*_ScanlineSpeed)*6.283);
                float gate=step(0.7,hash21(float2(floor(i.uv.y*_BlockCount),floor(_Time.y*12))));
                float2 uv=i.uv; uv.x+=(scan*gate)*_GlitchStrength*smoothstep(0.02,0.98,_DissolveAmount);
                fixed4 sample=tex2D(_MainTex,uv)+_TextureSampleAdd;
                float sdfAlpha=smoothstep(0.43,0.57,sample.a);
                float baseAlpha=lerp(sample.a,sdfAlpha,saturate(_UseSDF));
                float n=noise21(i.uv*_NoiseScale+_Time.y*_NoiseSpeed.xy);
                n=lerp(n,hash21(block+floor(_Time.y*8)),0.55);
                n=saturate(n+scan*0.06);
                float visible=1-smoothstep(n-_Softness,n+_Softness,_DissolveAmount);
                visible=lerp(visible,1,step(_DissolveAmount,0.0001));
                visible*=1-step(0.9999,_DissolveAmount);
                float edge=(1-smoothstep(_EdgeWidth,_EdgeWidth+_Softness,abs(n-_DissolveAmount)))*step(0.001,_DissolveAmount)*(1-step(0.999,_DissolveAmount));
                float alpha=baseAlpha*i.color.a*visible;
                // TMP SDF atlases store coverage in alpha; their RGB is commonly black.
                // Preserve the original TMP vertex/inline color instead of multiplying by atlas RGB.
                float3 sourceRgb=lerp(sample.rgb*i.color.rgb,i.color.rgb,saturate(_UseSDF));
                float3 rgb=sourceRgb + _EdgeColor.rgb*edge*_EdgeIntensity*baseAlpha;
                #ifdef UNITY_UI_CLIP_RECT
                alpha*=UnityGet2DClipping(i.worldPosition.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha-0.001);
                #endif
                return fixed4(rgb,alpha);
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
