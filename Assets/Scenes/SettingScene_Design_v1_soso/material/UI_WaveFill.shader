Shader "UI/WaveFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FillColor ("[Wave 1] Fill Color", Color) = (0.78, 0.91, 0.93, 1)
        _OverlayStrength ("[Wave 1] Overlay Strength", Range(0,1)) = 0.8
        _LineColor ("[Wave 1] Line Color", Color) = (1,1,1,1)
        _LineY ("[Wave 1] Base Height", Range(0,1)) = 0.8
        _LineThickness ("[Wave 1] Line Thickness", Range(0.001,0.1)) = 0.01
        _Amplitude ("[Wave 1] Wave Height", Range(0,0.2)) = 0.1
        _Frequency ("[Wave 1] Frequency", Float) = 10
        _Speed ("[Wave 1] Horizontal Flow Speed", Float) = 1
        _AmplitudePulseSpeed ("[Wave 1] Wave Height Pulse Speed", Float) = 0.3
        _ShapeFlipAmount ("[Wave 1] Crest Trough Flip Amount", Range(0,1)) = 0
        _ShapeFlipSpeed ("[Wave 1] Crest Trough Flip Speed", Float) = 1
        _VerticalBobAmount ("[Wave 1] Vertical Bob Amount", Range(0,0.2)) = 0
        _VerticalBobSpeed ("[Wave 1] Vertical Bob Speed", Float) = 0
        _EdgeSoftness ("[Wave 1] Edge Softness", Range(0.001,0.05)) = 0.01

        _FillColor2 ("[Wave 2] Fill Color", Color) = (0.65, 0.85, 0.90, 1)
        _OverlayStrength2 ("[Wave 2] Overlay Strength", Range(0,1)) = 0.5
        _LineColor2 ("[Wave 2] Line Color", Color) = (1,1,1,0.8)
        _LineY2 ("[Wave 2] Base Height", Range(0,1)) = 0.6
        _LineThickness2 ("[Wave 2] Line Thickness", Range(0.001,0.1)) = 0.01
        _Amplitude2 ("[Wave 2] Wave Height", Range(0,0.2)) = 0.05
        _Frequency2 ("[Wave 2] Frequency", Float) = 7
        _Speed2 ("[Wave 2] Horizontal Flow Speed", Float) = 0.8
        _AmplitudePulseSpeed2 ("[Wave 2] Wave Height Pulse Speed", Float) = 0.25
        _ShapeFlipAmount2 ("[Wave 2] Crest Trough Flip Amount", Range(0,1)) = 0
        _ShapeFlipSpeed2 ("[Wave 2] Crest Trough Flip Speed", Float) = 1
        _VerticalBobAmount2 ("[Wave 2] Vertical Bob Amount", Range(0,0.2)) = 0
        _VerticalBobSpeed2 ("[Wave 2] Vertical Bob Speed", Float) = 0
        _EdgeSoftness2 ("[Wave 2] Edge Softness", Range(0.001,0.05)) = 0.01

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
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            Name "Default"

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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            fixed4 _FillColor;
            float _OverlayStrength;
            fixed4 _LineColor;
            float _LineY;
            float _LineThickness;
            float _Amplitude;
            float _Frequency;
            float _Speed;
            float _AmplitudePulseSpeed;
            float _ShapeFlipAmount;
            float _ShapeFlipSpeed;
            float _VerticalBobAmount;
            float _VerticalBobSpeed;
            float _EdgeSoftness;

            fixed4 _FillColor2;
            float _OverlayStrength2;
            fixed4 _LineColor2;
            float _LineY2;
            float _LineThickness2;
            float _Amplitude2;
            float _Frequency2;
            float _Speed2;
            float _AmplitudePulseSpeed2;
            float _ShapeFlipAmount2;
            float _ShapeFlipSpeed2;
            float _VerticalBobAmount2;
            float _VerticalBobSpeed2;
            float _EdgeSoftness2;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPosition = v.vertex;
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 sprite = tex2D(_MainTex, i.uv) + _TextureSampleAdd;

                float x = i.uv.x;
                float y = i.uv.y;
                float t = _Time.y;

                const float TAU = 6.28318530718;
                float xWarp = x
                    + sin(x * TAU * 1.3 + t * 0.9) * 0.015
                    + sin(x * TAU * 2.1 - t * 0.6) * 0.008;
                float ampMod = 0.85 + sin(t * _AmplitudePulseSpeed + x * TAU * 0.4) * 0.15;
                float travelingWave = (
                    sin(xWarp * _Frequency * TAU + t * _Speed) * 0.7 +
                    sin(xWarp * (_Frequency * 0.55) * TAU - t * (_Speed * 0.6)) * 0.3
                );
                float standingWave = (
                    sin(xWarp * _Frequency * TAU) * 0.7 +
                    sin(xWarp * (_Frequency * 0.55) * TAU) * 0.3
                ) * cos(t * _ShapeFlipSpeed);
                float wave = lerp(travelingWave, standingWave, _ShapeFlipAmount) * (_Amplitude * ampMod);
                float verticalBob = sin(t * _VerticalBobSpeed) * _VerticalBobAmount;
                float lineY = _LineY + verticalBob + wave;

                float fillMask = 1.0 - smoothstep(lineY - _EdgeSoftness, lineY + _EdgeSoftness, y);
                float dist = abs(y - lineY);
                float aa = max(fwidth(dist), 0.001);
                float lineMask = 1.0 - smoothstep(_LineThickness, _LineThickness + aa, dist);

                fixed4 overlayCol = lerp(fixed4(0, 0, 0, 0), _FillColor, fillMask);
                overlayCol = lerp(overlayCol, _LineColor, lineMask);

                float xWarp2 = x
                    + sin(x * TAU * 1.7 - t * 0.7) * 0.012
                    + sin(x * TAU * 2.8 + t * 0.5) * 0.006;
                float ampMod2 = 0.9 + sin(t * _AmplitudePulseSpeed2 + x * TAU * 0.3) * 0.1;
                float travelingWave2 = (
                    sin(xWarp2 * _Frequency2 * TAU + t * _Speed2) * 0.72 +
                    sin(xWarp2 * (_Frequency2 * 0.6) * TAU - t * (_Speed2 * 0.5)) * 0.28
                );
                float standingWave2 = (
                    sin(xWarp2 * _Frequency2 * TAU) * 0.72 +
                    sin(xWarp2 * (_Frequency2 * 0.6) * TAU) * 0.28
                ) * cos(t * _ShapeFlipSpeed2);
                float wave2 = lerp(travelingWave2, standingWave2, _ShapeFlipAmount2) * (_Amplitude2 * ampMod2);
                float verticalBob2 = sin(t * _VerticalBobSpeed2) * _VerticalBobAmount2;
                float lineY2 = _LineY2 + verticalBob2 + wave2;

                float fillMask2 = 1.0 - smoothstep(lineY2 - _EdgeSoftness2, lineY2 + _EdgeSoftness2, y);
                float dist2 = abs(y - lineY2);
                float aa2 = max(fwidth(dist2), 0.001);
                float lineMask2 = 1.0 - smoothstep(_LineThickness2, _LineThickness2 + aa2, dist2);

                fixed4 overlayCol2 = lerp(fixed4(0, 0, 0, 0), _FillColor2, fillMask2);
                overlayCol2 = lerp(overlayCol2, _LineColor2, lineMask2);

                fixed4 finalCol = fixed4(0,0,0,0);
                finalCol.rgb = lerp(finalCol.rgb, overlayCol.rgb, saturate(overlayCol.a * _OverlayStrength));
                finalCol.rgb = lerp(finalCol.rgb, overlayCol2.rgb, saturate(overlayCol2.a * _OverlayStrength2));
                finalCol.a = max(overlayCol.a * _OverlayStrength, overlayCol2.a * _OverlayStrength2);

                // 元スプライトの形（マスク用途）だけ使う
                finalCol.a *= sprite.a;
                finalCol *= i.color;

                #ifdef UNITY_UI_CLIP_RECT
                finalCol.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalCol.a - 0.001);
                #endif

                return finalCol;
            }
            ENDCG
        }
    }
}
