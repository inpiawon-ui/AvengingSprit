// 그림의 **모양만** 쓰고 색은 통째로 갈아끼운다.
//
// ── 왜 필요한가 ─────────────────────────────────────────────
// 무적 윤곽은 「같은 그림을 조금 키워 몸 뒤에 깔고 밝게 칠한다」로 만든다.
// 그런데 `Image.color` 는 **곱하기**라, 캐릭터의 어두운 옷은 어떤 색을 곱해도
// 어둡게 남는다 — 뒤에 깔아 봐야 빛나는 테두리가 아니라 그림자가 된다(실측 2026-09-15).
//
// 이 셰이더는 텍스처의 **알파만** 읽고 RGB 는 버린다. 그래서 어떤 그림을 넣어도
// 단색 실루엣이 나오고, 키워서 뒤에 깔면 그것이 곧 윤곽선이다.
//
// UI 기본 셰이더의 스텐실·클립 설정을 그대로 따른다 — 마스크(RectMask2D) 안에서도
// 잘려야 하기 때문이다.
Shader "UI/Silhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

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
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ⚠ **알파만 읽는다.** 그림의 RGB 는 버린다 — 그래야 단색 실루엣이 된다.
                fixed a = tex2D(_MainTex, i.texcoord).a * i.color.a;
                clip(a - 0.01);
                return fixed4(i.color.rgb, a);
            }
            ENDCG
        }
    }
}
