Shader "FlatShadowCaster/ShadowSprite"
{
    Properties
    {
        _Texture ("Shadow Texture", 2D) = "white" {}
        _Color ("Shadow Color", Color) = (0,0,0,0.5)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _Texture;
            float4 _Texture_ST;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Texture);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 sampled = tex2D(_Texture, i.uv);
                // RenderTexture側のalphaが意図通りにならないことがあるため、
                // 影の強度はRGBの明るさから作る（背景は黒想定）。
                fixed strength = max(sampled.r, max(sampled.g, sampled.b));
                fixed alpha = strength * _Color.a;
                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}

