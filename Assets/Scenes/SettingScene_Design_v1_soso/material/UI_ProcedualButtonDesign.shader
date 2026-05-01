Shader "UI/ProceduralButton"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _FillColor ("Fill Color", Color) = (0.18, 0.85, 0.55, 1)
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)

        _Radius ("Base Radius", Range(0.0, 0.5)) = 0.33
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.02
        _Softness ("Softness", Range(0.0001, 0.1)) = 0.01

        _LobeCount ("Lobe Count", Range(1.0, 12.0)) = 8.0
        _LobeRadius ("Lobe Radius", Range(0.0, 0.2)) = 0.065
        _LobeRadialScale ("Lobe Radial Scale", Range(0.5, 3.0)) = 1.35
        _LobeTangentialScale ("Lobe Tangential Scale", Range(0.5, 3.0)) = 1.0
        _LobeOuterBias ("Lobe Outer Bias", Range(0.0, 1.0)) = 0.5
        _LobeInnerBias ("Lobe Inner Bias", Range(0.0, 1.0)) = 0.5
        _LobeOffset ("Lobe Offset", Range(0.0, 0.3)) = 0.04
        _SmoothJoin ("Smooth Join", Range(0.001, 0.15)) = 0.05
        _LobeJitter ("Lobe Jitter", Range(0.0, 0.08)) = 0.015
        _LobeRotationSpeed ("Lobe Rotation Speed", Range(0.0, 10.0)) = 1.0

        _BlobInnerFade ("Blob Inner Fade", Range(0.0, 1.0)) = 0.25

        _PulseAmount ("Pulse Amount", Range(0.0, 0.1)) = 0.0
        _PulseSpeed ("Pulse Speed", Range(0.0, 20.0)) = 6.0

        // uGUI用
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            fixed4 _FillColor;
            fixed4 _OutlineColor;

            float _Radius;
            float _OutlineWidth;
            float _Softness;

            float _LobeCount;
            float _LobeRadius;
            float _LobeRadialScale;
            float _LobeTangentialScale;
            float _LobeOuterBias;
            float _LobeInnerBias;
            float _LobeOffset;
            float _SmoothJoin;
            float _LobeJitter;
            float _LobeRotationSpeed;

            float _BlobInnerFade;

            float _PulseAmount;
            float _PulseSpeed;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float circleMask(float2 uv, float radius, float softness)
            {
                float2 p = uv - 0.5;
                float dist = length(p);
                return 1.0 - smoothstep(radius, radius + softness, dist);
            }

            float sdCircle(float2 p, float r)
            {
                return length(p) - r;
            }

            float sdEllipseOriented(float2 p, float2 center, float2 radialDir, float radius, float radialScale, float tangentialScale, float outerBias, float innerBias)
            {
                float2 local = p - center;
                float2 tangentDir = float2(-radialDir.y, radialDir.x);

                float radial = dot(local, radialDir);
                float tangential = dot(local, tangentDir);

                float outerScale = lerp(1.0, radialScale, saturate(outerBias));
                float innerScale = lerp(1.0, radialScale, saturate(innerBias));

                float radialScaleSigned = (radial >= 0.0) ? outerScale : innerScale;

                float2 q = float2(
                    radial / max(radialScaleSigned, 0.0001),
                    tangential / max(tangentialScale, 0.0001)
                );

                return length(q) - radius;
            }

            float smoothUnion(float d1, float d2, float k)
            {
                float h = saturate(0.5 + 0.5 * (d2 - d1) / max(k, 0.0001));
                return lerp(d2, d1, h) - k * h * (1.0 - h);
            }

            float hash11(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453123);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 p = uv - 0.5;

                float dist = length(p);

                float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmount;
                float centerRadius = _Radius + pulse;

                float shape = sdCircle(p, centerRadius);

                int lobeCount = max(1, (int)round(_LobeCount));
                float rotation = _Time.y * _LobeRotationSpeed;

                for (int idx = 0; idx < 12; idx++)
                {
                    if (idx >= lobeCount)
                    {
                        break;
                    }

                    float fi = (float)idx;
                    float t = fi / max(_LobeCount, 1.0);
                    float angle = t * 6.2831853 + rotation;

                    float jitterA = (hash11(fi + 1.37) - 0.5) * 2.0;
                    float jitterB = (hash11(fi + 8.91) - 0.5) * 2.0;

                    float lobeOffset = centerRadius + _LobeOffset + jitterA * _LobeJitter;
                    float lobeRadius = _LobeRadius + jitterB * (_LobeJitter * 0.5);
                    lobeRadius = max(lobeRadius, 0.0001);

                    float2 dir = float2(cos(angle), sin(angle));
                    float2 lobeCenter = dir * lobeOffset;
                    float lobe = sdEllipseOriented(
                        p,
                        lobeCenter,
                        dir,
                        lobeRadius,
                        _LobeRadialScale,
                        _LobeTangentialScale,
                        _LobeOuterBias,
                        _LobeInnerBias
                    );

                    shape = smoothUnion(shape, lobe, _SmoothJoin);
                }

                float aa = max(fwidth(shape) * 1.5, _Softness);

                float fill = 1.0 - smoothstep(-aa, aa, shape);

                float outlineOuter = 1.0 - smoothstep(-aa, aa, shape);
                float outlineInner = 1.0 - smoothstep(-_OutlineWidth - aa, -_OutlineWidth + aa, shape);
                float outline = saturate(outlineOuter - outlineInner);

                fixed4 col = 0;
                col.rgb = _FillColor.rgb * fill + _OutlineColor.rgb * outline;
                col.a = max(fill, outline) * _FillColor.a;

                col *= i.color;

                return col;
            }
            ENDCG
        }
    }
}
