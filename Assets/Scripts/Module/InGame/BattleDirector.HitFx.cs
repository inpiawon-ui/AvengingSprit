using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 공통 피격 (2026-10-07) — 승인 시안 `Projects/AVSR/_exchange/in/mock_hitfx_v7.png` · 설계 `spec_hitfx_v3.md`.
    ///
    /// ── 원칙 ────────────────────────────────────────────────
    /// 무기마다 따로 만들지 않는다. 총 · 연사 · 레이저 · 구체 · 근접 · 스킬이 **같은 네 가지**를 쓴다(PD).
    /// 무기를 떠올리는 모양(표창 · 별 · 칼자국 · 총구 화염)은 넣지 않는다 — 빛 · 고리 · 원뿔 · 알갱이만.
    /// 의미 = 색이다. 피해 숫자 · ▲▼ 와 같은 색을 쓴다.
    ///
    ///   보통  흰색      「톡」    작은 흰 코어 · 얇은 고리 · 알갱이 5개
    ///   치명  금빛 노랑 「쾅」    금빛 코어 · 고리 2겹 · 짧은 광선 8개 · 불똥 12개
    ///   유리  주황      「꿰뚫음」 앞에 작은 입구 → 몸을 지나는 가는 빛 → **등 뒤로 벌어지는 원뿔**, 알갱이도 전부 등 뒤로
    ///   불리  은빛 강철 「튕겨냄」 맞은 자리에 **얇은 은빛 충돌면** 번쩍 · 둥근 불티와 빛꼬리가 전부 **쏜 쪽으로** 튕겨 나온다,
    ///                            몸 테두리가 쇠처럼 번쩍인다 · 적은 꿈쩍 안 함
    ///   (밋밋한 회색은 「꺼진 버튼」 같아 은빛으로 바꿨다 — PD 2026-10-07)
    ///
    /// ── 어떻게 그리나 ────────────────────────────────────────
    /// 빛은 그림이 아니라 알파로(`UI/AdditiveLight` 셰이더 — 유령 빛과 같은 재질 원본에서 모양값만 바꿔 복제).
    /// 알갱이는 파티클(`ParticleFxPool.Spray`, 흰 빛 점에 색을 칠한다).
    /// 모양 · 크기 · 색 · 박자는 코덱스가 정한다(아트 판단은 코덱스 — PD 2026-10-07). 조정표 `tune_hitfx_r*.md`.
    ///
    /// ── 연사 ────────────────────────────────────────────────
    /// 같은 적을 0.07초 안에 또 맞히면 줄인다 — 코어와 방향 단서(등 뒤 원뿔 · 앞면 빛면)만 남기고 고리 · 알갱이를 뺀다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 의미 색 (spec_hitfx_v3 0절 · 불리는 은빛 강철) ─────────
        private static readonly Color HxWhite = new(0.94f, 0.957f, 0.973f, 1f);
        private static readonly Color HxWhiteDim = new(0.745f, 0.79f, 0.84f, 1f);
        private static readonly Color HxGold = new(1f, 0.76f, 0.18f, 1f);
        private static readonly Color HxGoldCore = new(1f, 0.957f, 0.682f, 1f);
        private static readonly Color HxGoldDeep = new(0.84f, 0.5f, 0.07f, 1f);
        private static readonly Color HxOrange = new(1f, 0.494f, 0.11f, 1f);
        private static readonly Color HxOrangeCore = new(1f, 0.81f, 0.41f, 1f);
        private static readonly Color HxOrangeDeep = new(0.8f, 0.28f, 0.047f, 1f);
        private static readonly Color HxSilver = new(0.667f, 0.714f, 0.776f, 1f);     // 170,182,198
        private static readonly Color HxSilverHi = new(0.9f, 0.925f, 0.95f, 1f);
        private static readonly Color HxSilverDeep = new(0.3f, 0.34f, 0.4f, 1f);

        /// <summary>설계 수치(720 화면 px)에 곱하는 배율 — 시안과 나란히 재서 맞춘다.</summary>
        private const float HxScale = 1.15f;
        private const float HxRapidGap = 0.07f;
        private const int HxMaxLights = 72;

        private Material _hxDotMat, _hxRingMat, _hxBeamMat, _hxConeMat, _hxConeGlowMat, _hxPlaneMat, _hxPlaneBackMat;
        private bool _hxBuilt;

        private sealed class HxLight
        {
            public Image Img;
            public RectTransform Rt;
            public Material Mat;
            public bool Busy;
            public float Delay, Age, Life;
            public Vector2 SizeFrom, SizeTo;
            public Color Color;
            public float Alpha;
        }

        private readonly List<HxLight> _hxLights = new();
        private readonly Dictionary<Unit, float> _hxFullAt = new();

        // 탄 명중 — `SpawnImpact` 대신 적어 두고 `ApplyShotHit` 끝(치명 · 상성이 정해진 뒤)에 그린다
        private int _hxShotFrame = -1;
        private Vector2 _hxShotAt, _hxShotDir;
        private bool _hxShotOwnFx;

        /// <summary>근접 한 번을 치는 중 — 이 동안 `HitEnemyWith` 의 방향을 때린 몸에서 잰다.</summary>
        private bool _hxMeleeSwing;
        private Vector2 _hxMeleeFrom;

        /// <summary>
        /// ⚠ 꺼 둔다(2026-10-07) — 코덱스와 7회차까지 맞췄지만 PD 반려: 「저 연출들이 다 마음에 안 든다 · 기존처럼 유지」.
        ///   예전 피격(`SpawnFx("hit" / "crit" / "weakhit")`)으로 돌아간다. 치명타는 `CritBurst` 로 세게 터뜨린다.
        /// </summary>
        private const bool HxEnabled = false;

        private bool HxReady => HxEnabled && (_hxBuilt || HxBuild());

        // ── 들어오는 곳 ──────────────────────────────────────────

        /// <summary>
        /// 탄이 적에게 맞았다 — 자리 · 방향을 적어 둔다. 다뤘으면 true(예전 터짐 그림을 안 띄운다).
        /// 폭발탄 · 난사 예광탄은 제 터짐이 따로 있다 — 그것은 그대로 두고 상성 · 치명 겹만 얹는다.
        /// </summary>
        private bool HxNoteShot(Projectile p, Vector2 at)
        {
            if (!HxReady) return false;
            _hxShotFrame = Time.frameCount;
            _hxShotAt = at;
            _hxShotDir = p.Direction.sqrMagnitude > 0.01f ? p.Direction.normalized : Vector2.up;
            _hxShotOwnFx = p.Kind == WpTracerKind || p.Kind == "grenade" || p.Kind == "missile";
            return !_hxShotOwnFx;
        }

        /// <summary>탄 피해가 정해졌다 — 네 가지 가운데 하나를 그린다. 다뤘으면 true.</summary>
        private bool HxShotHit(Unit victim, bool crit, bool weak, bool dull)
        {
            if (!HxReady || victim == null) return false;
            bool noted = _hxShotFrame == Time.frameCount;
            _hxShotFrame = -1;
            var dir = noted ? _hxShotDir : HxDirFrom(Avatar != null ? Avatar.Position : victim.Position, victim.Position);
            // 폭발에 휘말린 적(적어 둔 탄이 없다)은 폭발이 이미 「맞았다」를 말한다 — 보통이면 더 얹지 않는다
            bool quiet = !noted || _hxShotOwnFx;
            HxDraw(victim, noted ? _hxShotAt : HxFront(victim, dir), dir, crit, weak, dull, quiet);
            return true;
        }

        /// <summary>근접 한 대 — 때린 몸 자리를 적어 둔다. 다뤘으면 true(주황 원을 띄우지 않는다).</summary>
        private bool HxMeleeHit(Unit attacker, Unit victim)
        {
            if (!HxReady || attacker == null) return false;
            _hxMeleeFrom = attacker.Position;
            return true;
        }

        /// <summary>
        /// 근접 · 스킬 명중(`HitEnemyWith`). 다뤘으면 true.
        /// 퀄업 스킬은 제 이펙트가 「맞았다」를 말한다 — 보통이면 안 얹고 상성만 얹는다.
        /// </summary>
        private bool HxSkillHit(Unit victim, bool weak, bool dull)
        {
            if (!HxReady || victim == null) return false;
            bool melee = _hxMeleeSwing;
            var from = melee ? _hxMeleeFrom : Avatar != null ? Avatar.Position : victim.Position;
            var dir = HxDirFrom(from, victim.Position);
            HxDraw(victim, HxFront(victim, dir), dir, false, weak, dull, !melee && HasQualityFx(_host));
            return true;
        }

        // ── 네 가지 ──────────────────────────────────────────────

        private void HxDraw(Unit victim, Vector2 at, Vector2 dir, bool crit, bool weak, bool dull, bool quietNormal)
        {
            bool light = !HxDue(_hxFullAt, victim);
            float r = Mathf.Clamp(victim.BodyRadius, 28f, 90f);
            if (weak) HxPierce(victim, dir, r, crit, light);
            else if (dull) HxDeflect(victim, dir, r, crit, light);
            else if (crit) HxCrit(at, dir, light);
            else if (!quietNormal) HxTap(at, dir, light);
        }

        // 수치는 코덱스 조정표 `Projects/AVSR/_exchange/tune_hitfx_r1.md` ~ `r7.md` 2절 그대로(아트 판단은 코덱스 — PD 2026-10-07)
        // 색 낱알은 색마다 재질이 따로다(HxGrain*) — 금빛 · 은빛은 보통 섞기(더하기는 밝은 바닥에서 하얗게 날아간다),
        // 주황은 원뿔 위에서 묻히지 않게 더하기 + 노란 심. 흰 빛 · 긴 빛꼬리는 더하기(HxDot).

        /// <summary>보통 — 「톡」. 몸을 가리지 않는 흰 코어 · 잔광 고리 · 둥근 파편.</summary>
        private void HxTap(Vector2 at, Vector2 dir, bool light)
        {
            float ang = HxAngle(dir);
            HxPut(_hxDotMat, at, HxMid, ang, HxSize(20f, 16f), HxSize(27f, 22f), HxWhite, 0.92f, 0.09f);
            if (light)
            {
                HxSpray(ParticleFxKind.HxDot, at, -dir, 48f, 1, 120f, 190f, 15f, 15f, 0.16f, HxWhite);
                return;
            }
            HxPut(_hxRingMat, at, HxMid, 0f, HxSize(20f, 20f), HxSize(32f, 32f), HxWhite, 0.48f, 0.11f);
            var side = new Vector2(-dir.y, dir.x);
            HxSpray(ParticleFxKind.HxDot, at, -dir, 48f, 2, 120f, 190f, 15f, 15f, 0.16f, HxWhite);
            HxSpray(ParticleFxKind.HxDot, at, side, 25f, 1, 105f, 175f, 14f, 14f, 0.15f, HxWhite);
            HxSpray(ParticleFxKind.HxDot, at, -side, 25f, 1, 105f, 175f, 14f, 14f, 0.15f, HxWhiteDim);
            HxSpray(ParticleFxKind.HxDot, at, dir, 35f, 1, 110f, 180f, 15f, 15f, 0.16f, HxWhite);
        }

        /// <summary>치명 — 「쾅」. 중심과 방사광이 주인공, 고리는 보조. 화면 멈칫은 부르는 쪽(HitStop)이 맡는다.</summary>
        private void HxCrit(Vector2 at, Vector2 dir, bool light)
        {
            HxPut(_hxDotMat, at, HxMid, 0f, HxSize(32f, 32f), HxSize(50f, 50f), HxGoldCore, 0.92f, 0.11f);
            HxPut(_hxRingMat, at, HxMid, 0f, HxSize(34f, 34f), HxSize(70f, 70f), HxGold, 0.54f, 0.16f);
            HxSpray(ParticleFxKind.HxGrainGold, at, dir, 180f, light ? 6 : 10, 190f, 320f, 13f, 13f, 0.24f, HxGoldDeep);
            if (light) return;
            // 두 고리는 완전 동심원이 아니게(설계) — 조금 늦게 · 중심 ±2 px
            HxPut(_hxRingMat, at + Random.insideUnitCircle * 2f * HxScale, HxMid, 0f,
                  HxSize(30f, 30f), HxSize(64f, 64f), HxGold, 0.3f, 0.16f, 0.03f);
            // 방사광 7개 — 중심에서 뽑지 않는다: 흰 중심 가장자리 밖(10~16 px)에서 시작, 기준각을 매번 돌리고 조금씩 늦게
            // (안쪽 끝이 한 점에 모이면 십자 · 별로 읽혔다 — 코덱스 r3 · r4)
            float baseDeg = Random.Range(0f, 360f);
            for (int i = 0; i < 7; i++)
            {
                float a = (baseDeg + i * (360f / 7f) + Random.Range(-17f, 17f)) * Mathf.Deg2Rad;
                var ray = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var start = HxOffset(at, ray, Random.Range(10f, 16f), Random.Range(-3f, 3f), 257f, (i % 4) * 0.006f);
                HxSpray(ParticleFxKind.HxDot, start, ray, 0f, 1, 205f, 310f,
                        Random.Range(40f, 52f), 5f, Random.Range(0.15f, 0.18f), i % 2 == 0 ? HxGoldCore : HxGold);
            }
            // 불똥 — 맞은 쪽 너머(+D)로, 충격이 진행하는 느낌만
            HxSpray(ParticleFxKind.HxGrainGold, at, dir, 65f, 8, 190f, 320f, 13f, 13f, 0.24f, HxGold);
        }

        /// <summary>
        /// 유리 — 「꿰뚫음」. 앞(쏜 쪽)에는 작은 입구만, 빛이 몸을 지나 **등 뒤로 벌어지는 원뿔**(+ 넓은 번짐).
        /// 알갱이는 전부 등 뒤(+D)로 — 쏜 쪽으로 돌아오는 것은 하나도 없다.
        /// </summary>
        private void HxPierce(Unit victim, Vector2 dir, float r, bool crit, bool light)
        {
            var c = victim.Position;
            var entry = c - dir * (r * 0.5f);
            var exit = c + dir * (r * 0.45f);
            float ang = HxAngle(dir);
            // 입구 — 장축이 날아온 방향과 수직(상자 x = 옆, y = 진행)
            HxPut(_hxDotMat, entry, HxMid, ang, HxSize(20f, 15f), HxSize(30f, 22f), HxOrangeCore, 1f, 0.1f);
            // 원뿔 — 꼭짓점이 등 뒤 경계, 멀어질수록 넓다. 아래에 넓고 옅은 번짐을 한 겹 더 깐다
            HxPut(_hxConeGlowMat, exit, HxFoot, ang, HxSize(44f, 20f), HxSize(68f, 86f), HxOrange, 0.09f,
                  light ? 0.18f : 0.22f);
            HxPut(_hxConeMat, exit, HxFoot, ang, HxSize(40f, 18f), HxSize(62f, 84f), HxOrange, 1f,
                  light ? 0.18f : 0.24f);
            if (light)
            {
                HxEmbers(exit, dir, 3, 58f, 220f, 365f, 12f, 10f, 0.21f, HxOrange, 6f, 20f, 16f);
                victim.FlashRim(HxOrange, 0.07f);
                return;
            }
            // 몸을 지나는 빛기둥 — 입구에서 등 뒤까지 오직 +D 로
            float through = (exit - entry).magnitude + 34f * HxScale;
            HxPut(_hxBeamMat, entry, HxFoot, ang, HxSize(10f, 6.25f), new Vector2(7.5f * HxScale, through),
                  HxOrange, 0.94f, 0.15f);
            // 머리 위 한 덩어리로 뭉치지 않게 부채 · 옆 흩기를 넓혔다(코덱스 r7)
            HxEmbers(exit, dir, 15, 50f, 205f, 345f, 16f, 14f, 0.24f, HxOrange, 8f, 22f, 16f);
            HxEmbers(exit, dir, 4, 54f, 195f, 335f, 12f, 12f, 0.24f, HxOrangeDeep, 6f, 20f, 16f);
            if (crit)
            {
                // 치명 + 유리 — 관통 골격은 그대로, 출구에 금빛 고리 하나 · 금빛 불똥
                HxPut(_hxRingMat, exit + dir * (20f * HxScale), HxMid, 0f, HxSize(34f, 34f), HxSize(70f, 70f),
                      HxGold, 0.65f, 0.18f);
                HxSpray(ParticleFxKind.HxGrainGold, exit, dir, 36f, 6, 210f, 340f, 15f, 15f, 0.25f, HxGold);
            }
            victim.FlashRim(new Color(HxOrange.r, HxOrange.g, HxOrange.b, 0.9f), 0.08f);
        }

        /// <summary>
        /// 불리 — 「튕겨냄」. 맞은 자리에 **얇은 은빛 충돌면**이 번쩍(0.08초) → 둥근 은빛 불티 · 긴 빛꼬리가
        /// 전부 쏜 쪽으로 튕겨 나간다. 몸 안 · 등 뒤엔 빛이 없다 · 적은 꿈쩍 안 한다.
        /// 반구 · 반고리 · 두꺼운 판은 방패 · 유리판처럼 보여 뺐다(코덱스 조정 r1).
        /// </summary>
        private void HxDeflect(Unit victim, Vector2 dir, float r, bool crit, bool light)
        {
            var face = victim.Position - dir * (r * 0.52f);
            float faceAng = HxAngle(-dir);   // 상자 위 = 쏜 쪽 · 가로(긴 변)가 날아온 방향과 수직
            var plane = face - dir * (1f * HxScale);
            HxPut(_hxPlaneBackMat, plane, HxMid, faceAng, HxSize(72f, 12f), HxSize(86f, 15f), HxSilverDeep, 0.52f, 0.105f);
            HxPut(_hxPlaneMat, plane, HxMid, faceAng, HxSize(64f, 8f), HxSize(80f, 11f), HxSilverHi, 0.96f, 0.09f);
            // 쇠에 빛이 튀는 한 줄
            HxPut(_hxDotMat, face + dir * (3f * HxScale), HxMid, faceAng, HxSize(38f, 3f), HxSize(62f, 4f),
                  Color.white, 1f, 0.065f);
            if (light)
            {
                HxSpray(ParticleFxKind.HxGrainSilver, face, -dir, 42f, 4, 250f, 400f, 10f, 10f, 0.2f, HxSilver, 6f, 0.005f);
                HxSpray(ParticleFxKind.HxDot, face, -dir, 14f, 2, 320f, 440f, 38f, 6f, 0.13f, HxSilverHi);
                victim.FlashRim(new Color(HxSilverHi.r, HxSilverHi.g, HxSilverHi.b, 0.6f), 0.05f);
                return;
            }
            // 둥근 불티 · 긴 빛꼬리 — 전부 쏜 쪽으로(+D 는 0개)
            HxSpray(ParticleFxKind.HxGrainSilver, face, -dir, 42f, 8, 250f, 410f, 10f, 10f, 0.22f, HxSilver, 7f, 0.004f);
            HxSpray(ParticleFxKind.HxGrainSilver, face, -dir, 42f, 2, 230f, 370f, 9f, 9f, 0.21f, HxSilverDeep, 6f, 0.006f);
            HxSpray(ParticleFxKind.HxDot, face, -dir, 14f, 3, 320f, 440f, 42f, 6f, 0.14f, HxSilverHi);
            if (crit)
            {
                // 치명 + 불리 — 차단 골격은 그대로, 앞면에만 금빛 얇은 고리 · 금빛 불똥(쏜 쪽으로)
                HxPut(_hxRingMat, face, HxMid, 0f, HxSize(32f, 32f), HxSize(62f, 62f), HxGold, 0.45f, 0.15f);
                HxSpray(ParticleFxKind.HxGrainGold, face, -dir, 32f, 5, 230f, 360f, 15f, 15f, 0.23f, HxGold);
            }
            victim.FlashRim(new Color(HxSilverHi.r, HxSilverHi.g, HxSilverHi.b, 0.85f), 0.08f);
        }

        // ── 빛 조각 ──────────────────────────────────────────────

        private static readonly Vector2 HxMid = new(0.5f, 0.5f);
        private static readonly Vector2 HxFoot = new(0.5f, 0f);

        private static Vector2 HxSize(float w, float h) => new(w * HxScale, h * HxScale);

        /// <summary>상자의 위(+y)를 <paramref name="d"/> 쪽으로 돌리는 각도.</summary>
        private static float HxAngle(Vector2 d) => Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;

        private static Vector2 HxDirFrom(Vector2 from, Vector2 to)
        {
            var d = to - from;
            return d.sqrMagnitude < 1f ? Vector2.up : d.normalized;
        }

        /// <summary>몸의 앞면(쏜 쪽) — 몸 한가운데에 얹으면 얼굴을 가린다.</summary>
        private static Vector2 HxFront(Unit victim, Vector2 dir)
            => victim.Position - dir * (Mathf.Clamp(victim.BodyRadius, 28f, 90f) * 0.45f);

        private void HxSpray(ParticleFxKind kind, Vector2 at, Vector2 dir, float cone, int count,
                             float speedMin, float speedMax, float length, float width, float life, Color color,
                             float spread = 0f, float stagger = 0f)
            => _pfx?.Spray(kind, at, dir, cone, count, speedMin * HxScale, speedMax * HxScale,
                           new Vector2(length * HxScale, width * HxScale), life, color, spread * HxScale, stagger);

        /// <summary>
        /// 낱알이 태어날 자리 — 진행축으로 <paramref name="along"/>, 옆으로 <paramref name="side"/> 옮기고,
        /// 늦게 나오는 만큼(<paramref name="late"/>초 × 평균 속도) 궤적 뒤에서 태어나게 한다(파티클은 늦춰 쏘기가 없다).
        /// </summary>
        private static Vector2 HxOffset(Vector2 at, Vector2 dir, float along, float side, float speed, float late)
            => at + dir * ((along - speed * late) * HxScale) + new Vector2(-dir.y, dir.x) * (side * HxScale);

        /// <summary>유리 주황 불티 — 출구 앞 · 옆으로 흩어 태어나고 0.008초씩 늦게(원뿔 위에 겹쳐 묻히지 않게, 코덱스 r4 · r5).</summary>
        private void HxEmbers(Vector2 exit, Vector2 dir, int count, float cone, float speedMin, float speedMax,
                              float length, float width, float life, Color color,
                              float alongMin = 6f, float alongMax = 20f, float side = 8f)
        {
            float speed = (speedMin + speedMax) * 0.5f;
            for (int i = 0; i < count; i++)
                HxSpray(ParticleFxKind.HxGrainOrange,
                        HxOffset(exit, dir, Random.Range(alongMin, alongMax), Random.Range(-side, side), speed, (i % 4) * 0.008f),
                        dir, cone, 1, speedMin, speedMax, length, width, life, color);
        }

        private HxLight HxPut(Material mat, Vector2 at, Vector2 pivot, float angle, Vector2 sizeFrom, Vector2 sizeTo,
                              Color color, float alpha, float life, float delay = 0f)
        {
            var l = HxTake(mat);
            if (l == null) return null;
            HxStart(l, at, pivot, angle, sizeFrom, sizeTo, color, alpha, life, delay);
            return l;
        }

        private static void HxStart(HxLight l, Vector2 at, Vector2 pivot, float angle, Vector2 sizeFrom, Vector2 sizeTo,
                                    Color color, float alpha, float life, float delay)
        {
            l.Busy = true;
            l.Age = 0f;
            l.Delay = delay;
            l.Life = Mathf.Max(0.01f, life);
            l.SizeFrom = sizeFrom;
            l.SizeTo = sizeTo;
            l.Color = color;
            l.Alpha = alpha;
            l.Rt.pivot = pivot;
            l.Rt.anchoredPosition = at;
            l.Rt.sizeDelta = sizeFrom;
            l.Rt.localEulerAngles = new Vector3(0f, 0f, angle);
            l.Rt.SetAsLastSibling();
            l.Img.color = new Color(color.r, color.g, color.b, alpha);
            l.Img.enabled = delay <= 0f;
        }

        /// <summary>같은 재질의 쉬는 조각을 꺼낸다(재질이 같아야 한 번에 그린다). 없으면 만든다 — 상한이 차면 null.</summary>
        private HxLight HxTake(Material mat)
        {
            for (int i = 0; i < _hxLights.Count; i++)
                if (!_hxLights[i].Busy && _hxLights[i].Mat == mat) return _hxLights[i];
            if (_hxLights.Count >= HxMaxLights || _shotLayer == null) return null;
            var go = new GameObject("HitLight", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_shotLayer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.material = mat;   // 스프라이트 없이 — 셰이더가 상자(uv 0~1) 안에 빛을 그린다
            img.enabled = false;
            var l = new HxLight { Img = img, Rt = rt, Mat = mat };
            _hxLights.Add(l);
            return l;
        }

        /// <summary>한 프레임. 히트스톱이면 같이 멈춘다(부르는 쪽의 dt).</summary>
        private void TickHx(float dt)
        {
            for (int i = 0; i < _hxLights.Count; i++)
            {
                var l = _hxLights[i];
                if (!l.Busy) continue;
                if (l.Delay > 0f)
                {
                    l.Delay -= dt;
                    if (l.Delay > 0f) continue;
                    l.Img.enabled = true;
                }
                l.Age += dt;
                float t = l.Age / l.Life;
                if (t >= 1f) { l.Busy = false; l.Img.enabled = false; continue; }
                // 크기는 처음에 확 퍼지고(빛이 터진다) 진하기는 끝까지 고르게 빠진다
                float grow = 1f - (1f - t) * (1f - t);
                l.Rt.sizeDelta = Vector2.LerpUnclamped(l.SizeFrom, l.SizeTo, grow);
                l.Img.color = new Color(l.Color.r, l.Color.g, l.Color.b, l.Alpha * (1f - t));
            }
        }

        // ── 재질 ─────────────────────────────────────────────────

        /// <summary>유령 빛 재질 원본(`ParticleFx/glight`)에서 모양값만 바꿔 복제한다. 원본이 아직 없으면 false.</summary>
        private bool HxBuild()
        {
            if (_glMaterial == null || _shotLayer == null) return false;
            _hxDotMat = HxMat(1f);
            _hxDotMat.SetFloat("_RingRadius", 0f); _hxDotMat.SetFloat("_RingWidth", 0.42f); _hxDotMat.SetFloat("_RingGlow", 0.72f);
            _hxRingMat = HxMat(1f);
            _hxRingMat.SetFloat("_RingWhite", 0.62f);
            _hxRingMat.SetFloat("_RingRadius", 0.76f); _hxRingMat.SetFloat("_RingWidth", 0.065f); _hxRingMat.SetFloat("_RingGlow", 0.16f);
            // 셰이더 값은 코덱스 조정표 tune_hitfx_r1~r5.md 3절
            // 몸을 지나는 빛 — 하얀 심 · 주황 번짐
            _hxBeamMat = HxBeam(1f, 0.72f, 0.24f, 0.72f, 0.09f, 0.72f, 0.2f, 0.03f, 0f);
            _hxBeamMat.SetFloat("_RingWhite", 0.78f); _hxBeamMat.SetFloat("_CoreWhite", 0.88f);
            // 원뿔 — 꼭짓점(아래)이 좁고 멀어질수록 넓다, 끝이 둥글게 사라진다(화살촉 금지)
            _hxConeMat = HxBeam(0.34f, 1f, 0.07f, 1f, 0.18f, 1.15f, 0.28f, 0.03f, 0.12f);
            _hxConeMat.SetFloat("_RingWhite", 0.08f); _hxConeMat.SetFloat("_CoreWhite", 0.72f);   // 가운데는 밝게 탄다
            HxNormalBlend(_hxConeMat);   // 진한 주황 몸통 — 더하기면 밝은 바닥에서 하얗게 날아간다
            // 원뿔 아래 넓고 옅은 번짐 — 실처럼 가늘게 남지 않게
            _hxConeGlowMat = HxBeam(0.46f, 1f, 0.3f, 0.14f, 0.04f, 0.12f, 0.34f, 0.02f, 0f);
            _hxConeGlowMat.SetFloat("_RingWhite", 0.04f); _hxConeGlowMat.SetFloat("_CoreWhite", 0.1f);
            HxNormalBlend(_hxConeGlowMat);
            // 충돌면 — 날아온 방향과 수직으로 선 얇은 은빛 섬광(두꺼운 판은 유리판 · 방패처럼 보였다)
            _hxPlaneMat = HxBeam(1f, 0.86f, 0.2f, 0.94f, 0.2f, 0.74f, 0.12f, 0.06f, 0f);
            _hxPlaneMat.SetFloat("_RingWhite", 0.34f); _hxPlaneMat.SetFloat("_CoreWhite", 0.68f);
            _hxPlaneBackMat = HxBeam(1f, 0.82f, 0.28f, 0.78f, 0.15f, 0.52f, 0.18f, 0.08f, 0f);
            _hxPlaneBackMat.SetFloat("_RingWhite", 0.22f); _hxPlaneBackMat.SetFloat("_CoreWhite", 0.48f);
            HxNormalBlend(_hxPlaneMat);
            HxNormalBlend(_hxPlaneBackMat);
            if (_pfx != null)
            {
                _pfx.SetMaterial(ParticleFxKind.HxDot, ParticleElement.Fire, _hxDotMat);
                _pfx.SetMaterial(ParticleFxKind.HxGrainGold, ParticleElement.Fire, HxGrain(0.62f, 0.18f, 0.18f, true));
                _pfx.SetMaterial(ParticleFxKind.HxGrainSilver, ParticleElement.Fire, HxGrain(0.6f, 0.2f, 0.38f, true));
                // 주황만 더하기 + 노란 심 — 보통 섞기 · 흰 심 없음이면 주황 원뿔 위에서 같은 색이라 묻혔다(r3)
                _pfx.SetMaterial(ParticleFxKind.HxGrainOrange, ParticleElement.Fire, HxGrain(0.56f, 0.2f, 0.48f, false));
            }
            _hxBuilt = true;
            return true;
        }

        private Material HxGrain(float width, float glow, float white, bool normalBlend)
        {
            var m = HxMat(1f);
            m.SetFloat("_RingRadius", 0f); m.SetFloat("_RingWidth", width); m.SetFloat("_RingGlow", glow);
            m.SetFloat("_RingWhite", white);
            if (normalBlend) HxNormalBlend(m);
            return m;
        }

        private static void HxNormalBlend(Material m)
            => m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        private Material HxMat(float shape)
        {
            var m = new Material(_glMaterial) { hideFlags = HideFlags.DontSave };
            m.SetFloat("_Shape", shape);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_RingWhite", 0.7f);
            m.SetFloat("_CoreWhite", 0.8f);
            return m;
        }

        private Material HxBeam(float bottom, float top, float edgeSoft, float body, float core, float coreBoost,
                                float fadeTop, float fadeBottom, float edgeLine)
        {
            var m = HxMat(0f);
            m.SetFloat("_BottomWidth", bottom); m.SetFloat("_TopWidth", top); m.SetFloat("_EdgeSoft", edgeSoft);
            m.SetFloat("_Body", body); m.SetFloat("_CoreWidth", core); m.SetFloat("_CoreBoost", coreBoost);
            m.SetFloat("_FadeTop", fadeTop); m.SetFloat("_FadeBottom", fadeBottom); m.SetFloat("_EdgeLine", edgeLine);
            m.SetFloat("_Streak", 0f); m.SetFloat("_Scroll", 0f); m.SetFloat("_DashCount", 0f);
            return m;
        }

        private static bool HxDue(Dictionary<Unit, float> at, Unit u)
        {
            float now = Time.time;
            if (at.TryGetValue(u, out float last) && now - last < HxRapidGap) return false;
            at[u] = now;
            return true;
        }

        /// <summary>방을 넘어갈 때 — 남은 빛을 끄고, 죽은 적이 사전에 쌓이지 않게.</summary>
        private void ClearHx()
        {
            _hxFullAt.Clear();
            _hxMeleeSwing = false;
            _hxShotFrame = -1;
            for (int i = 0; i < _hxLights.Count; i++)
            {
                _hxLights[i].Busy = false;
                _hxLights[i].Img.enabled = false;
            }
        }
    }
}
