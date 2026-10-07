using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 유령 빛 — 빙의할 몸 하나에 **하늘에서 빛이 내려온다** (2026-10-07).
    /// 승인 시안 `Projects/AVSR/_exchange/in/mock_ghostlight_v2.png` · 앵커 `ref/gl_anchor_mock.png`.
    ///
    /// ── 어떻게 그리나 ────────────────────────────────────────
    /// 빛은 **그림이 아니라 알파로** 그린다(PD — 「보통 빛은 알파로 연출하거나 파티클로 한다」).
    ///   · 광막 두 장 · 빛심 · 바닥 고리 = `UI/AdditiveLight` 셰이더(더하기 섞기 · 그라데이션 · 아래로 흐르는 결)
    ///   · 떨어지는 빛 알갱이 = 파티클(`ParticleFxPool.Drift`, 시안에서 잘라 낸 십자별)
    ///   · 몸 테두리 = 몸 실루엣 빛(`Unit.SetLightRim`)
    /// 예전 방식(그림 여러 장 넘기기)은 「이미지 하나를 얹은 것 같고 밋밋하다」로 반려됐다.
    ///
    /// ── 박자 ────────────────────────────────────────────────
    ///   내려옴(0.24초)  빛심이 위에서 뻗고 → 광막 두 장이 0.04초 어긋나 따라 내려오고 → 고리가 열린다
    ///   머묾(반복)      광막 두 장이 서로 반대로 진하기 · 폭을 숨쉬고, 결이 아래로 흐르고, 알갱이가 흘러내린다
    ///   거둠(0.2초)     빙의가 시작되면 광막 · 고리가 오므라들고, 빛심이 위로 회수되고, 알갱이가 가슴으로 빨려 든다
    ///
    /// 광막 · 빛심 · 고리는 몸 **뒤**(장판 레이어). 방을 어둡게 하지 않는다 · 빛은 대상 하나만.
    /// 빛 재질(`ParticleFx/glight`)이 없으면 예전 원뿔 한 장으로 돌아간다(`TickSpotlight`).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float GlDescendSeconds = 0.24f;
        private const float GlRetractSeconds = 0.2f;

        // 크기(px) — 승인 시안을 재서 맞춘 뒤 PD 「빛이 너무 두꺼워, 얇게」(2026-10-07)로 좁혔다
        // 코덱스 시안 대비 검수(review_ghostlight_v6.md) — 폭을 먼저 줄이고 더하기 합성의 누적 밝기를 낮췄다
        private const float GlVeilWidthA = 70f, GlVeilWidthB = 58f, GlVeilHeight = 186f;
        private const float GlVeilSideShift = 10f;
        private const float GlRayWidth = 20f, GlRayHeight = 272f, GlRayBelowFeet = 18f;
        private const float GlRingWidth = 94f, GlRingHeight = 24f;
        private const float GlRimAlpha = 0.9f;   // 0.62 는 시안의 파란 몸 테두리보다 흐렸다
        private const float GlBreatheSeconds = 0.72f;

        // 알갱이 — 머리 위에서 태어나 아래로 흐른다(시안: 머리 위 · 양옆의 작은 흰 십자별 서너 개)
        // 시안 대비(2026-10-07): 20 px · 0.09초 간격은 별이 크고 많았다
        private const float GlMoteGap = 0.22f;
        private const float GlMoteSpeed = 82f;
        private const float GlMoteLife = 0.95f;
        private const float GlMoteSize = 12f;

        // 시안 대비 — 더하기 합성이라 밝으면 하얗게 포화돼 색이 사라진다 — 알파를 낮추고 청색을 남긴다
        private static readonly Color GlVeilColor = new(0.68f, 0.86f, 1f, 1f);
        private static readonly Color GlRayColor = new(0.32f, 0.67f, 1f, 1f);
        private static readonly Color GlRingColor = new(0.27f, 0.62f, 1f, 1f);

        private enum GlPhase { Off, Descend, Stay, Retract }

        private GlPhase _glPhase;
        private float _glTime;
        private float _glMoteTimer;
        private Unit _glTarget;
        private Vector2 _glAt;          // 마지막으로 비춘 몸 자리(대상이 사라져도 거두는 동안 쓴다)

        /// <summary>빛 셰이더 재질 원본(번들 `ParticleFx/glight`). 조각마다 복제해 값을 따로 준다.</summary>
        private Material _glMaterial;

        private Image _glVeilA, _glVeilB, _glRay, _glRing;

        /// <summary>테두리를 켠 몸 — 대상이 바뀌거나 빛을 거두면 끈다.</summary>
        private Unit _glRimUnit;

        private bool GlReady => _glMaterial != null;

        private void SetGlRim(Unit u, float alpha)
        {
            if (_glRimUnit != null && _glRimUnit != u) _glRimUnit.SetLightRim(0f);
            _glRimUnit = alpha > 0.01f ? u : null;
            if (u != null) u.SetLightRim(alpha);
        }

        /// <summary>
        /// 유령 빛 한 프레임. <paramref name="target"/> 은 지금 비출 몸(없으면 null).
        /// 빙의가 시작됐으면(<paramref name="channeling"/>) 빨려 들어가듯 거둔다.
        /// </summary>
        private void TickGhostLight(Unit target, bool channeling, float dt)
        {
            if (_glVeilA == null && !MakeGhostLight()) return;

            if (channeling)
            {
                if (_glPhase == GlPhase.Descend || _glPhase == GlPhase.Stay)
                {
                    _glPhase = GlPhase.Retract;
                    _glTime = 0f;
                    if (_channelBody != null) _glAt = _channelBody.Position;
                    GlSuckMotes();
                }
            }
            else if (target == null)
            {
                if (_glPhase != GlPhase.Off && _glPhase != GlPhase.Retract) { _glPhase = GlPhase.Retract; _glTime = 0f; }
            }
            else if (target != _glTarget || _glPhase == GlPhase.Off || _glPhase == GlPhase.Retract)
            {
                // 대상이 바뀌면 새 몸 위로 다시 내려온다 — 빛이 미끄러져 가면 「고른 몸」이 흐려진다
                _glTarget = target;
                _glPhase = GlPhase.Descend;
                _glTime = 0f;
            }
            if (target != null && !channeling) _glAt = target.Position;

            _glTime += dt;
            switch (_glPhase)
            {
                case GlPhase.Descend: DrawGlDescend(dt); break;
                case GlPhase.Stay: DrawGlStay(dt); break;
                case GlPhase.Retract: DrawGlRetract(); break;
                default: ShowGl(false); break;
            }
        }

        private void DrawGlDescend(float dt)
        {
            float t = _glTime;
            if (t >= GlDescendSeconds) { _glPhase = GlPhase.Stay; _glTime = 0f; DrawGlStay(dt); return; }
            var feet = _glAt + Vector2.down * SoulFootDrop;
            // 빛심이 먼저 위에서 뻗는다(0.08초)
            float rayK = Mathf.Clamp01(t / 0.08f);
            PlaceColumn(_glRay, feet + Vector2.down * GlRayBelowFeet, GlRayWidth, GlRayHeight * rayK, GlRayHeight, GlRayColor, 1f);
            // 광막 두 장이 0.04초 어긋나 따라 내려온다
            float a = Mathf.Clamp01((t - 0.04f) / 0.12f), b = Mathf.Clamp01((t - 0.08f) / 0.12f);
            PlaceColumn(_glVeilA, feet + Vector2.left * GlVeilSideShift, GlVeilWidthA, GlVeilHeight * a, GlVeilHeight, GlVeilColor, 1f);
            PlaceColumn(_glVeilB, feet + Vector2.right * GlVeilSideShift, GlVeilWidthB, GlVeilHeight * b, GlVeilHeight, GlVeilColor, 0.8f);
            // 마지막 0.08초에 고리가 열린다
            float ringK = Mathf.Clamp01((t - 0.16f) / 0.08f);
            PlaceRing(feet, Mathf.Lerp(30f, GlRingWidth, ringK), ringK);
            SetGlRim(_glTarget, GlRimAlpha * ringK);
            GlEmitMotes(dt);
        }

        private void DrawGlStay(float dt)
        {
            float t = _glTime;
            var feet = _glAt + Vector2.down * SoulFootDrop;
            // 0 → 1 → 0 을 0.72초에 한 번. 광막 두 장은 서로 반대로 숨쉰다(진하기 · 폭)
            float b = Mathf.PingPong(t / GlBreatheSeconds * 2f, 1f);
            PlaceColumn(_glVeilA, feet + Vector2.left * GlVeilSideShift, GlVeilWidthA * Mathf.Lerp(0.94f, 1.04f, b),
                        GlVeilHeight, GlVeilHeight, GlVeilColor, Mathf.Lerp(0.75f, 1f, b));
            PlaceColumn(_glVeilB, feet + Vector2.right * GlVeilSideShift, GlVeilWidthB * Mathf.Lerp(1.04f, 0.94f, b),
                        GlVeilHeight, GlVeilHeight, GlVeilColor, Mathf.Lerp(0.8f, 0.6f, b));
            PlaceColumn(_glRay, feet + Vector2.down * GlRayBelowFeet, GlRayWidth, GlRayHeight, GlRayHeight, GlRayColor,
                        Mathf.Lerp(0.85f, 1f, b));
            PlaceRing(feet, GlRingWidth * Mathf.Lerp(0.95f, 1.03f, b), Mathf.Lerp(0.8f, 1f, b));
            SetGlRim(_glTarget, GlRimAlpha * Mathf.Lerp(0.75f, 1f, b));
            GlEmitMotes(dt);
        }

        private void DrawGlRetract()
        {
            float t = _glTime;
            if (t >= GlRetractSeconds) { _glPhase = GlPhase.Off; ShowGl(false); return; }
            float k = t / GlRetractSeconds;
            var feet = _glAt + Vector2.down * SoulFootDrop;
            // 광막이 오므라들며 아래 끝이 몸 가운데 쪽으로 18 px 올라간다
            float veilK = Mathf.Clamp01((t - 0.04f) / 0.10f);
            var lift = Vector2.up * (18f * veilK);
            PlaceColumn(_glVeilA, feet + lift, Mathf.Lerp(GlVeilWidthA, 20f, veilK), GlVeilHeight, GlVeilHeight, GlVeilColor, 1f - k);
            PlaceColumn(_glVeilB, feet + lift, Mathf.Lerp(GlVeilWidthB, 16f, veilK), GlVeilHeight, GlVeilHeight, GlVeilColor, 0.8f * (1f - k));
            // 빛심은 위로 70 px 회수되며 꺼진다
            float rayK = Mathf.Clamp01((t - 0.14f) / 0.06f);
            PlaceColumn(_glRay, feet + Vector2.up * (70f * rayK - GlRayBelowFeet), GlRayWidth, GlRayHeight, GlRayHeight, GlRayColor, 1f - rayK);
            // 고리가 가슴 쪽으로 접혀 들어간다
            float ringK = Mathf.Clamp01((t - 0.10f) / 0.08f);
            PlaceRing(feet, Mathf.Lerp(GlRingWidth, 24f, ringK), 1f - ringK);
            SetGlRim(_glRimUnit, GlRimAlpha * (1f - k));
        }

        /// <summary>머리 위 · 양옆에서 빛 알갱이가 태어나 아래로 흘러내린다(파티클).</summary>
        private void GlEmitMotes(float dt)
        {
            if (_pfx == null) return;
            _glMoteTimer -= dt;
            if (_glMoteTimer > 0f) return;
            _glMoteTimer = GlMoteGap;
            // 몸 위 60~200 px · 좌우 ±34 px 에서 — 얼굴 앞은 피한다(시안)
            float side = Random.value < 0.5f ? -1f : 1f;
            var at = _glAt + new Vector2(side * Random.Range(16f, 38f), Random.Range(90f, 210f));
            _pfx.Drift(ParticleFxKind.GhostMote, ParticleElement.Fire, at, Vector2.zero,
                       Vector2.down * GlMoteSpeed, GlMoteSize, GlMoteLife);
        }

        /// <summary>빙의가 시작되면 — 알갱이가 가슴 바깥 12 px 지점으로 빨려 든다.</summary>
        private void GlSuckMotes()
        {
            if (_pfx == null) return;
            var chest = _glAt + Vector2.up * 12f;
            for (int i = 0; i < 5; i++)
            {
                float ang = i / 5f * Mathf.PI * 2f;
                var from = chest + new Vector2(Mathf.Cos(ang) * 46f, Mathf.Sin(ang) * 60f + 40f);
                _pfx.Drift(ParticleFxKind.GhostMote, ParticleElement.Fire, from, Vector2.zero,
                           (chest - from) / 0.16f, GlMoteSize * 0.8f, 0.16f);
            }
        }

        /// <summary>세로 빛 — 아래 끝을 <paramref name="feet"/> 에 두고 위로(자라는 중이면 짧다). 윗변은 처음부터 제자리.</summary>
        private static void PlaceColumn(Image img, Vector2 feet, float width, float height, float fullHeight, Color color, float alpha)
        {
            img.enabled = height > 1f && alpha > 0.01f;
            if (!img.enabled) return;
            var rt = img.rectTransform;
            rt.anchoredPosition = feet + Vector2.up * (fullHeight - height);
            rt.sizeDelta = new Vector2(width, height);
            img.color = new Color(color.r, color.g, color.b, alpha);
        }

        private void PlaceRing(Vector2 feet, float width, float alpha)
        {
            _glRing.enabled = alpha > 0.01f;
            if (!_glRing.enabled) return;
            _glRing.rectTransform.anchoredPosition = feet;
            _glRing.rectTransform.sizeDelta = new Vector2(width, width * (GlRingHeight / GlRingWidth));
            _glRing.color = new Color(GlRingColor.r, GlRingColor.g, GlRingColor.b, alpha);
        }

        private void ShowGl(bool on)
        {
            if (_glVeilA == null || on) return;
            _glVeilA.enabled = _glVeilB.enabled = _glRay.enabled = _glRing.enabled = false;
            SetGlRim(null, 0f);
        }

        private bool MakeGhostLight()
        {
            if (_fieldLayer == null || _glMaterial == null) return false;
            // 몸 뒤 — 넓은 것이 먼저(아래)
            var ring = GlMat();
            ring.SetFloat("_Shape", 1f);
            ring.SetFloat("_RingRadius", 0.8f); ring.SetFloat("_RingWidth", 0.035f); ring.SetFloat("_RingGlow", 0.22f);
            _glRing = MakeGlImage("GhostLightRing", new Vector2(0.5f, 0.5f), ring);

            var veilB = GlMat(); SetVeil(veilB, 0.26f, 0.7f);
            _glVeilB = MakeGlImage("GhostLightVeilB", new Vector2(0.5f, 0f), veilB);
            var veilA = GlMat(); SetVeil(veilA, 0.32f, 0.5f);
            _glVeilA = MakeGlImage("GhostLightVeilA", new Vector2(0.5f, 0f), veilA);

            var ray = GlMat();
            ray.SetFloat("_Shape", 0f);
            // 가는 흰 심 + 파란 번짐, 마디 길이가 제각각인 점선(시안) — 납작한 흰 막대로 보였던 것을 고쳤다
            // 마디는 양 끝이 뾰족한 방추형(셰이더) — 3배 확대 비교로 시안과 맞춘 값(2026-10-07)
            ray.SetFloat("_BottomWidth", 1f); ray.SetFloat("_TopWidth", 1f); ray.SetFloat("_EdgeSoft", 0.42f);
            // 코덱스 값(0.32 · 1.05)은 시안보다 흐렸다 — 폭은 얇게 두고 밝기만 시안대로(3배 비교 2026-10-07)
            ray.SetFloat("_Body", 0.48f); ray.SetFloat("_CoreWidth", 0.055f); ray.SetFloat("_CoreBoost", 1.35f);
            ray.SetFloat("_FadeTop", 0.24f); ray.SetFloat("_FadeBottom", 0.1f);
            ray.SetFloat("_Streak", 0f); ray.SetFloat("_Scroll", 0.35f);
            ray.SetFloat("_DashCount", 6f); ray.SetFloat("_DashDuty", 0.43f);
            _glRay = MakeGlImage("GhostLightRay", new Vector2(0.5f, 0f), ray);
            ShowGl(false);
            return true;
        }

        /// <summary>광막 — 위가 좁고 아래가 넓은 아주 옅은 판, 결이 아래로 흐른다.</summary>
        private static void SetVeil(Material m, float topWidth, float scroll)
        {
            m.SetFloat("_Shape", 0f);
            // 시안 대비 — 위가 납작한 사다리꼴이 끝까지 읽히고 가장자리 선이 살짝 보인다
            // ⚠ 안쪽에 결 무늬(_Streak)를 넣었더니 세로 얼룩으로 지저분했다(PD) — 시안처럼 매끈하게
            m.SetFloat("_BottomWidth", 1f); m.SetFloat("_TopWidth", topWidth); m.SetFloat("_EdgeSoft", 0.11f);
            m.SetFloat("_Body", 0.32f); m.SetFloat("_CoreWidth", 0.001f); m.SetFloat("_CoreBoost", 0f);
            m.SetFloat("_FadeTop", 0.22f); m.SetFloat("_FadeBottom", 0.09f);
            m.SetFloat("_EdgeLine", 0.18f);
            m.SetFloat("_Streak", 0f); m.SetFloat("_Scroll", scroll);
            m.SetFloat("_DashCount", 0f);
        }

        /// <summary>조각마다 값을 따로 주려고 원본 재질을 복제한다(한 판에 네 장 — 방이 바뀌어도 다시 안 만든다).</summary>
        private Material GlMat() => new(_glMaterial) { hideFlags = HideFlags.DontSave };

        private Image MakeGlImage(string name, Vector2 pivot, Material material)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_fieldLayer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = pivot;
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.material = material;   // 스프라이트 없이 — 셰이더가 상자(uv 0~1) 안에 빛을 그린다
            img.enabled = false;
            return img;
        }

        /// <summary>방을 넘어갈 때 — 지난 방 자리에서 빛이 남지 않게.</summary>
        private void ClearGhostLight()
        {
            _glPhase = GlPhase.Off;
            _glTarget = null;
            ShowGl(false);
        }
    }
}
