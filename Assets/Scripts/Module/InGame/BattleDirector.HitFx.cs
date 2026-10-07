using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 피격 이펙트 두 겹 (2026-10-07) — 시안 `Projects/AVSR/_exchange/in/mock_hitfx_v2.png` · 설계 `spec_hitfx_ghostlight.md` A.
    ///
    /// 「무엇이 맞았나」(투사체 · 근접 고유 터짐) + 「얼마나 세게」(치명 · 상성 유리) 로 나눈다.
    ///   · 공통 노란 별(`fx_hit`) · 붉은 불티는 뺀다 — 무엇에 맞았는지를 덮었다.
    ///   · 상성 유리는 파란 얼음(`fx_weakhit`) 대신 **때린 몸의 속성** 겹(파워 · 무기 · 마법) — 표창에 얼음이 나왔다(PD).
    ///   · 치명타는 큰 노란 별(`fx_crit`) 대신 한쪽이 긴 비대칭 스파이크(hxcrit).
    ///   · 근접은 베기(hxslash) · 타격(hxblunt) · 사슬(hxline) — 예전엔 주황 원 한 장이 깜빡였다.
    /// 연사 몸은 초당 수십 번 맞는다 — 갈래마다 간격을 두고, 세기 겹은 적마다 0.12초에 한 번.
    /// 그림(`fx_hx*`)이 없으면 예전 그림으로 돌아간다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // 그림 칸(256)의 왼쪽 절반에 부채 · 호가 뻗는다 — 시안의 부채 길이(약 60 px) · 호 거리에 맞춘 상자
        // 시안 대비 1차 비교(2026-10-07): 금속 별 · 호가 시안(약 40 · 70 px)보다 작았다 — 키웠다
        private const float HxCritSize = 140f;
        private const float HxAffinitySize = 160f;
        private const float HxMeleeSize = 74f;
        private const float HxLayerGap = 0.12f;        // 같은 적에게 세기 겹을 다시 그리기까지
        private const float HxRapidGap = 0.05f;        // 연사 탄(mg · smg) 고유 터짐 간격
        private const float HxCoreAlpha = 0.85f;       // 보통 타격 — 고유 터짐만 70~85%
        private const float HxAffinityAlpha = 0.75f;

        private readonly Dictionary<Unit, float> _hxCritAt = new();
        private readonly Dictionary<Unit, float> _hxAffinityAt = new();
        private float _hxRapidAt = -1f;

        /// <summary>근접 한 번을 치는 중 — 이 동안 `HitEnemyWith` 의 공통 별을 그리지 않는다(근접 겹이 대신).</summary>
        private bool _hxMeleeSwing;

        /// <summary>
        /// ⚠ 꺼 둔다(2026-10-07) — 이 그림 겹 방식은 PD 반려(「일반 투사체에 표창 모양」 · 「불리면 튕겨 나가야」 ·
        ///   「전체적으로 이상하다」). 시안 5차(`Projects/AVSR/_exchange/in/mock_hitfx_v5.png`)대로
        ///   빛 셰이더 + 파티클로 다시 만들 때까지 예전 피격으로 돌아간다.
        /// </summary>
        private const bool HxEnabled = false;

        private bool HxReady => HxEnabled && FxFrames("hxcore") != null && FxFrames("hxcrit") != null && FxFrames("hxweap") != null
                                && FxFrames("hxpow") != null && FxFrames("hxmagic") != null;

        /// <summary>
        /// **특별한 투사체**(폭발 · 원소)만 제 터짐 그림을 쓴다 — 크기(px, 그림 칸 48).
        /// 나머지는 전부 공통 코어(hxcore) 하나다. 투사체마다 따로 그리지 않는다(PD 2026-10-07).
        /// </summary>
        private static float HxSpecialSize(string kind) => kind switch
        {
            "laser" => 48f,
            "thunder" => 54f,
            "grenade" => 64f,
            "missile" => 60f,
            "flame" or "dragoon" => 48f,
            "venom" => 42f,
            "frost" => 50f,
            _ => 0f,
        };

        /// <summary>공통 코어 크기 — 시안의 금속 별(약 40 px)이 칸의 40% 라 상자는 그 2.5배.</summary>
        private const float HxCommonCoreSize = 124f;

        /// <summary>터지며 불꽃이 튀어야 맞는 종류만 불티를 얹는다(파워 · 폭발).</summary>
        private static bool HxFiery(string kind)
            => kind == "grenade" || kind == "missile" || kind == "ball" || kind == "flame" || kind == "dragoon";

        /// <summary>
        /// 투사체 고유 터짐. 다뤘으면 true — `SpawnImpact` 가 더 하지 않는다.
        /// 크기를 정해 부른 것(폭발 반경)은 예전 길로 둔다.
        /// </summary>
        private bool HxImpact(Vector2 at, string kind, float size)
        {
            if (!HxReady || size > 0f || kind == null) return false;
            if (kind == "mg" || kind == "smg")
            {
                float now = Time.time;
                if (now - _hxRapidAt < HxRapidGap) return true;   // 사이 탄은 그리지 않는다
                _hxRapidAt = now;
            }
            if (HxFiery(kind)) _pfx?.Hit(at, ParticleElement.Fire, 0.6f);
            float special = HxSpecialSize(kind);
            var frames = special > 0f ? ImpactFrames(kind) : FxFrames("hxcore");
            if (frames == null) return true;
            float box = special > 0f ? special : HxCommonCoreSize;
            var im = FreeImpact(box);
            if (im == null) return true;
            im.Play(at, frames, box, loop: false);
            im.SetFrameSeconds(kind == "grenade" || kind == "missile" ? 0.035f : 0.03f);
            im.SetTint(new Color(1f, 1f, 1f, HxCoreAlpha));
            if (special <= 0f) HxFaceAway(im, at);   // 공통 코어의 불티가 날아온 반대쪽으로
            return true;
        }

        /// <summary>탄 명중의 세기 겹 — 치명이면 hxcrit, 보통이면 아무것도(고유 터짐이 이미 났다). 다뤘으면 true.</summary>
        private bool HxShotHit(Unit victim, bool crit)
        {
            if (!HxReady) return false;
            if (crit && HxDue(_hxCritAt, victim))
                HxFaceAway(PlayLz("hxcrit", victim.Position, HxCritSize, 0.025f), victim.Position);
            return true;
        }

        /// <summary>근접 · 스킬 명중의 공통 별을 그릴 차례인가. 근접 겹 · 퀄업 스킬이 대신하면 그리지 않는다.</summary>
        private bool HxSkipCommonHit => HxReady && (_hxMeleeSwing || HasQualityFx(_host));

        /// <summary>상성 유리 겹 — 때린 몸의 속성으로 고른다. 다뤘으면 true.</summary>
        private bool HxAffinityHit(Unit victim)
        {
            if (!HxReady) return false;
            if (!HxDue(_hxAffinityAt, victim)) return true;
            string name = MyKind switch
            {
                Affinity.Force => "hxpow",
                Affinity.Magic => "hxmagic",
                _ => "hxweap",
            };
            var im = PlayLz(name, victim.Position, HxAffinitySize, name == "hxmagic" ? 0.04f : 0.03f);
            im?.SetTint(new Color(1f, 1f, 1f, HxAffinityAlpha));
            HxFaceAway(im, victim.Position);
            return true;
        }

        /// <summary>
        /// 세기 겹은 **맞은 반대쪽으로만** 뻗는다(승인 시안 — 오른쪽에서 맞으면 왼쪽으로).
        /// 그림은 「오른쪽에서 맞아 왼쪽으로 뻗는」 방향으로 그려져 있다 — 날아온 방향(내 몸 → 맞은 자리)에 맞춰 돌린다.
        /// </summary>
        private void HxFaceAway(Impact im, Vector2 at)
        {
            if (im == null || Avatar == null) return;
            var d = at - Avatar.Position;
            if (d.sqrMagnitude < 1f) return;
            float deg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 180f;
            im.transform.localEulerAngles = new Vector3(0f, 0f, deg);
        }

        /// <summary>근접 한 대 — 몸마다 베기 · 타격 · 사슬. 다뤘으면 true(주황 원을 띄우지 않는다).</summary>
        private bool HxMeleeHit(Unit attacker, Unit victim)
        {
            if (!HxReady || attacker == null) return false;
            string name = attacker.Key switch
            {
                "baseball" or "guru" => "hxblunt",
                "ninja_chain" => "hxline",
                _ => "hxslash",
            };
            if (FxFrames(name) == null) return false;
            // 맞은 쪽 가장자리 — 몸 한가운데에 얹으면 얼굴이 가린다
            var d = victim.Position - attacker.Position;
            var at = victim.Position - (d.sqrMagnitude > 1f ? d.normalized : Vector2.zero) * (victim.BodyRadius * 0.4f);
            var im = PlayLz(name, at, HxMeleeSize, name == "hxblunt" ? 0.04f : 0.03f);
            im?.SetTint(new Color(1f, 1f, 1f, HxCoreAlpha));
            return true;
        }

        private static bool HxDue(Dictionary<Unit, float> at, Unit u)
        {
            float now = Time.time;
            if (at.TryGetValue(u, out float last) && now - last < HxLayerGap) return false;
            at[u] = now;
            return true;
        }

        /// <summary>방을 넘어갈 때 — 죽은 적이 사전에 쌓이지 않게.</summary>
        private void ClearHx()
        {
            _hxCritAt.Clear();
            _hxAffinityAt.Clear();
            _hxMeleeSwing = false;
        }
    }
}
