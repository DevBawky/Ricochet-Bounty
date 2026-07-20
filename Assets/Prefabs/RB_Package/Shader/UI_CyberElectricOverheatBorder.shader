Shader "UI/Cyber Electric Overheat Border"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _ElectricColor ("Electric Color", Color) = (0.05,1.5,3.5,1)
        [HDR] _HotColor ("Hot Color", Color) = (3.5,0.15,1.2,1)
        _BorderWidth ("Border Width", Range(0.005,0.5)) = 0.1
        _ElectricScale ("Electric Scale", Range(1,100)) = 28
        _ElectricSpeed ("Electric Speed", Range(0,20)) = 7
        _ElectricStrength ("Electric Strength", Range(0,1)) = 0.55
        _PulseSpeed ("Pulse Speed", Range(0,20)) = 5
        _GlowIntensity ("Glow Intensity", Range(0,5)) = 1.4
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
            Name "CyberElectricBorder"
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
            sampler2D _MainTex; fixed4 _TextureSampleAdd, _Color, _ElectricColor, _HotColor; float4 _ClipRect;
            float _BorderWidth,_ElectricScale,_ElectricSpeed,_ElectricStrength,_PulseSpeed,_GlowIntensity,_Alpha;
            v2f vert(appdata_t v){ v2f o; o.worldPosition=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o; }
            float hash21(float2 p){ p=frac(p*float2(123.34,345.45)); p+=dot(p,p+34.345); return frac(p.x*p.y); }
            float noise21(float2 p){ float2 q=floor(p),f=frac(p); f=f*f*(3-2*f); return lerp(lerp(hash21(q),hash21(q+float2(1,0)),f.x),lerp(hash21(q+float2(0,1)),hash21(q+1),f.x),f.y); }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 sprite=tex2D(_MainTex,i.uv)+_TextureSampleAdd;
                float edge=min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y));
                float n=noise21(i.uv*_ElectricScale+_Time.y*float2(_ElectricSpeed,-_ElectricSpeed*0.63));
                float jitter=(n-0.5)*_BorderWidth*_ElectricStrength;
                float border=1-smoothstep(_BorderWidth*0.35+jitter,_BorderWidth+jitter,edge);
                float spark=step(0.92,noise21(i.uv*_ElectricScale*2.3-_Time.y*_ElectricSpeed*1.8));
                float pulse=0.65+0.35*sin(_Time.y*_PulseSpeed+n*6.283);
                float hot=saturate(spark+pow(n,5));
                float3 rgb=lerp(_ElectricColor.rgb,_HotColor.rgb,hot)*_GlowIntensity*border*(1+pulse);
                rgb*=i.color.rgb*max(sprite.rgb,0.25);
                float alpha=sprite.a*i.color.a*_Alpha*border*saturate(0.7+pulse+spark);
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
