// 빛을 **그림 없이** 알파로 그린다 — 더하기 섞기(겹치면 밝아진다) (2026-10-07).
//
// ── 왜 필요한가 ─────────────────────────────────────────────
// 유령 빛을 그림 여러 장으로 넘겼더니 「이미지 하나를 얹은 것 같고 밋밋하다」(PD).
// 빛은 보통 알파 그라데이션 + 더하기 섞기로 그리고, 진하기 · 결의 흐름으로 살린다.
// `Image` 에 이 재질을 꽂으면 스프라이트 없이 상자(uv 0~1) 안에 빛 모양을 그린다.
//
// 모양(_Shape)
//   0 = 세로 빛기둥(사다리꼴) — 아래 폭 · 위 폭 · 가장자리 부드러움 · 가운데 심 · 위아래 흐려짐 ·
//       아래로 흐르는 결(_Streak) · 점선(_DashCount > 0 이면 마디로 끊기며 아래로 흐른다)
//   1 = 바닥 고리(타원) — 상자 안 타원 고리, 두께 · 번짐
// 색 · 진하기는 `Image.color`(정점 색) × _Color. 진하기 숨쉬기는 코드가 정점 알파로 준다.
//
// UI 기본 셰이더의 스텐실 · 클립 설정을 따른다(RectMask2D 안에서도 잘린다).
Shader "UI/AdditiveLight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Shape ("Shape 0=beam 1=ring", Float) = 0

        _BottomWidth ("Beam bottom width (0-1)", Float) = 1
        _TopWidth ("Beam top width (0-1)", Float) = 0.3
        _EdgeSoft ("Beam edge softness", Float) = 0.12
        _Body ("Beam body alpha", Float) = 0.5
        _CoreWidth ("Beam core width", Float) = 0.04
        _CoreBoost ("Beam core alpha", Float) = 0.6
        _FadeTop ("Fade at top (0-1)", Float) = 0.3
        _FadeBottom ("Fade at bottom (0-1)", Float) = 0.08
        _EdgeLine ("Beam edge line alpha", Float) = 0
        _Streak ("Falling streak amount", Float) = 0.25
        _Scroll ("Scroll speed", Float) = 0.6
        _DashCount ("Dash count (0 = solid)", Float) = 0
        _DashDuty ("Dash duty", Float) = 0.45

        _RingRadius ("Ring radius (0-1)", Float) = 0.8
        _RingWidth ("Ring thickness", Float) = 0.06
        _RingGlow ("Ring glow width", Float) = 0.18

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
        Blend SrcAlpha One          // 더하기 — 빛은 겹칠수록 밝아진다
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
                float2 uv       : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float _Shape;
            float _BottomWidth, _TopWidth, _EdgeSoft, _Body, _CoreWidth, _CoreBoost, _FadeTop, _FadeBottom;
            float _Streak, _Scroll, _DashCount, _DashDuty, _EdgeLine;
            float _RingRadius, _RingWidth, _RingGlow;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 beam (float2 uv, fixed4 col)
            {
                float t = _Time.y * _Scroll;
                // 점선이면 마디마다 **양 끝이 뾰족한 방추형** — 끝이 뭉툭한 막대는 그림이 깨진 것처럼 보였다(PD 2026-10-07)
                float taper = 1.0;
                if (_DashCount > 0.5)
                {
                    float cellId = floor(uv.y * _DashCount + t * 3.0);
                    float len = _DashDuty * (0.45 + 0.75 * frac(sin(cellId * 12.9898) * 43758.5453));
                    float f = frac(uv.y * _DashCount + t * 3.0);
                    taper = f < len ? sin(3.14159 * f / len) : 0.0;
                }
                float halfW = lerp(_BottomWidth, _TopWidth, uv.y) * 0.5 * taper;
                float x = abs(uv.x - 0.5);
                float edge = 1.0 - smoothstep(max(halfW - _EdgeSoft * taper, 0.0), max(halfW, 1e-4), x);
                float cw = max(_CoreWidth * taper, 1e-4);
                float core = exp(-(x * x) / (cw * cw)) * step(0.001, taper);
                float vfade = smoothstep(0.0, max(_FadeBottom, 1e-3), uv.y)
                            * (1.0 - smoothstep(1.0 - max(_FadeTop, 1e-3), 1.0, uv.y));
                // 아래로 흐르는 세로 결 — 사인 몇 개를 겹쳐 규칙이 안 보이게
                float s = sin(uv.x * 37.0 + 1.7) * 0.5 + 0.5;
                float flow = sin((uv.y + t) * 9.0 + s * 6.0) * 0.5 + 0.5;
                float flow2 = sin((uv.y + t * 1.7) * 23.0 + uv.x * 61.0) * 0.5 + 0.5;
                float streak = 1.0 - _Streak + _Streak * (0.6 * flow + 0.4 * flow2);
                // 판 가장자리에 살짝 밝은 선 — 시안의 광막은 테두리가 읽힌다
                float rim = exp(-pow((x - halfW) / 0.025, 2.0)) * _EdgeLine;
                float a = (edge * _Body * streak + core * _CoreBoost + rim) * vfade;
                // 심은 하얗게 — 가운데로 갈수록 색이 흰빛으로
                fixed3 rgb = lerp(col.rgb, fixed3(1, 1, 1), saturate(core * 0.8));
                return fixed4(rgb, a * col.a);
            }

            fixed4 ring (float2 uv, fixed4 col)
            {
                float d = length((uv - 0.5) * 2.0);
                float line_ = exp(-pow((d - _RingRadius) / max(_RingWidth, 1e-4), 2.0));
                float glow = exp(-pow((d - _RingRadius) / max(_RingGlow, 1e-4), 2.0)) * 0.45;
                float a = saturate(line_ + glow);
                fixed3 rgb = lerp(col.rgb, fixed3(1, 1, 1), saturate(line_ * 0.7));
                return fixed4(rgb, a * col.a);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = _Shape < 0.5 ? beam(i.uv, i.color) : ring(i.uv, i.color);
                clip(c.a - 0.003);
                return c;
            }
            ENDCG
        }
    }
}
