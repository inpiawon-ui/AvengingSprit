using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 코만도(레이저) 「연쇄 방전」 퀄업 연출 (2026-10-07) — 시안 `Projects/AVSR/_exchange/in/skillfx_sample_laser_v2.png`.
    ///
    /// 한 번 튈 때마다 네 박자다(코덱스 진단 `review_skillfx_now.md`):
    ///   ① 예비   총구에 빛이 모이고, 가는 조준선이 적까지 걸리고, 적 발밑에 끊어진 조준 고리가 조여든다
    ///   ② 발동   적 머리 위 상공에서 낙뢰가 내리꽂힌다(총구에서 올라가는 빔이 아니다)
    ///   ③ 타격   작은 흰 코어 → 청록 별 → 보라 림, 직선 스파크 · 사각 파편, 아주 작은 반동
    ///   (적이 여럿이면 첫 적부터 0.06초 간격으로 차례로 — 밝기 왕은 첫 타격 하나, 뒤는 작고 옅게)
    ///   ④ 여운   적 둘레에 짧은 각진 전기, 바닥 고리가 반 바퀴 돌며 흐려진다
    /// 이어지는 적은 앞 적에서 조준선이 이어진다 — 「타고 흐르는」 것이 이 스킬이다.
    /// 화면 전체를 색으로 덮지 않는다(시전 섬광도 이 스킬은 끈다 — `HasQualityFx`).
    ///
    /// 그림은 발주본(`fx_lz*` — `Projects/AVSR/Tools/lz_parts_fit.py`). 하나라도 없으면 예전 줄기 연출로 돌아간다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>조준이 걸린 뒤 첫 낙뢰가 떨어질 때까지(예비 박자). 피해도 타격과 함께 들어간다.</summary>
        private const float LzWindup = 0.12f;
        /// <summary>
        /// 다음 적으로 넘어가는 간격. 셋에 한꺼번에 떨어지면 「연쇄」가 아니라 「동시 낙뢰」로 읽혔다(검수 2026-10-07).
        /// </summary>
        private const float LzHopGap = 0.06f;
        /// <summary>낙뢰가 시작하는 높이(적 위로 px). 화면 밖에서 들어와야 「하늘에서」로 읽힌다.</summary>
        private const float LzSkyHeight = 460f;
        // 그림 칸(512×96) 안 줄기가 가늘다 — 칸을 이만큼 깔아야 시안 굵기(코어 4 px · 외피 20 px)가 된다
        private const float LzBoltThickness = 150f;
        /// <summary>적에서 적으로 타고 넘어가는 줄기 — 낙뢰보다 가늘다(주인공은 첫 낙뢰).</summary>
        private const float LzHopThickness = 96f;
        private const float LzAimThickness = 16f;
        // 코어 · 파편이 적을 덮으면 맞은 몸이 안 보인다(검수 2026-10-07) — 몸(약 90 px)보다 작게
        private const float LzHitSize = 76f;
        private const float LzDebrisSize = 72f;
        private const float LzArcSize = 132f;
        private const float LzRingSize = 120f;
        private const float LzRingSquash = 0.42f;
        private const float LzMuzzleSize = 56f;
        /// <summary>적 중심에서 발밑까지. 고리는 바닥에 깐다.</summary>
        private const float LzFootDrop = 34f;
        private const float LzHitShake = 2f;
        /// <summary>뒤 순번 타격의 진하기 · 크기 — 밝기 왕은 첫 타격 하나(검수).</summary>
        private const float LzFollowAlpha = 0.45f;
        private const float LzFollowScale = 0.8f;

        /// <summary>퀄업 연출이 들어간 스킬 — 시전 때 화면 전체 섬광 · 불티를 끈다(국소 빛만 쓴다).</summary>
        private static readonly HashSet<string> QualityFxHosts = new()
        {
            "commando_laser",
            // 무기 7종 (`BattleDirector.SkillFxWeapon.cs`)
            "amazon", "thug", "hopper_smg", "commando_mg", "gangster", "hopper", "ninja",
        };

        private bool HasQualityFx(Unit me) => me != null && me.Key != null && QualityFxHosts.Contains(me.Key);

        /// <summary>떨어질 타격 — 남은 시간 · 대상 · 피해 · 어디서 왔나(첫 타격은 하늘, 뒤는 앞 적) · 순번 · 첫 튐인가.</summary>
        private readonly List<(float Left, Unit Target, int Damage, Vector2 From, int Order, bool Opening)> _lzPending = new();

        /// <summary>이번 시전에서 몇 번째 튐인가. 첫 튐만 하늘에서 낙뢰가 떨어진다.</summary>
        private int _lzTicks;

        private bool LzReady => FxFrames("lzbolt") != null && FxFrames("lzhit") != null && FxFrames("lzaim") != null;

        private void ResetLz() => _lzTicks = 0;

        /// <summary>
        /// 한 번 튄다 — 나 → 적1 → 적2 → 적3 으로 조준선이 이어지고, 조금 뒤 적1 부터 차례로 맞는다.
        /// 첫 튐은 적1 머리 위에서 낙뢰가 떨어지고, 그다음은 앞 적에서 전기가 타고 넘어간다.
        /// 그림이 없으면 false 를 돌려준다(부르는 쪽이 예전 연출로).
        /// </summary>
        private bool LzChain(Unit me, IReadOnlyList<Unit> targets, int count, int damage)
        {
            if (!LzReady) return false;
            bool opening = _lzTicks++ == 0;
            if (opening) PlayLz("lzmuzzle", me.MuzzlePosition, LzMuzzleSize, 0.03f);
            Vector2 link = me.MuzzlePosition;
            for (int i = 0; i < count; i++)
            {
                var t = targets[i];
                float hitAt = (opening ? LzWindup : LzHopGap) + LzHopGap * i;
                var aim = FreeImpact(LzAimThickness);
                if (aim != null)
                {
                    aim.PlayBeam(link, t.Position, FxFrames("lzaim"), LzAimThickness);
                    aim.SetLife(hitAt + 0.18f, 0.18f);   // 맞은 뒤에도 잠깐 남아 흐려진다 — 경로가 읽히게
                    if (i > 0) aim.SetTint(new Color(1f, 1f, 1f, 0.7f));
                }
                var ringFrames = FxFrames("lzring");
                if (opening && ringFrames != null)
                {
                    var ring = FreeImpact(LzRingSize);
                    if (ring != null)
                    {
                        // 벌어진 채 나타나 조여들고(1~3장) 반 바퀴 돌며 흐려진다(4장) — 바닥에 남는다
                        ring.PlayOver(t.Position - Vector2.up * LzFootDrop, ringFrames, LzRingSize, 0.42f);
                        ring.SetSquash(LzRingSquash);
                        ring.SetLife(0.62f, 0.24f);
                        if (i > 0) ring.SetTint(new Color(1f, 1f, 1f, 0.6f));
                    }
                }
                _lzPending.Add((hitAt, t, damage, link, i, opening));
                link = t.Position;
            }
            return true;
        }

        /// <summary>때가 된 타격을 떨어뜨린다. `TickNewSkills` 에서 부른다.</summary>
        private void TickLz(float dt)
        {
            for (int i = _lzPending.Count - 1; i >= 0; i--)
            {
                var p = _lzPending[i];
                p.Left -= dt;
                if (p.Left > 0f) { _lzPending[i] = p; continue; }
                _lzPending.RemoveAt(i);
                if (p.Target == null || !p.Target.IsAlive || p.Target.IsDying || _host == null) continue;
                LzStrike(p);
            }
        }

        private void LzStrike((float Left, Unit Target, int Damage, Vector2 From, int Order, bool Opening) p)
        {
            var at = p.Target.Position;
            bool lead = p.Order == 0;
            bool sky = lead && p.Opening;
            var bolt = FreeImpact(sky ? LzBoltThickness : LzHopThickness);
            if (bolt != null)
            {
                // 첫 튐의 첫 적만 하늘에서 — 그 뒤는 앞 적(첫 적은 총구)에서 타고 넘어간다
                var from = sky ? at + Vector2.up * LzSkyHeight : p.From;
                bolt.PlayBeam(from, at, FxFrames("lzbolt"), sky ? LzBoltThickness : LzHopThickness);
                bolt.SetFrameSeconds(0.03f);   // 굵은 줄기는 0.12초 안에 사라진다 — 오래 서 있으면 멈춘 그림이다
                if (!lead) bolt.SetTint(new Color(1f, 1f, 1f, 0.75f));
            }
            float scale = lead ? 1f : LzFollowScale;
            float alpha = lead ? 1f : LzFollowAlpha;
            var hit = PlayLz("lzhit", at, LzHitSize * scale, 0.04f);
            if (hit != null && !lead) hit.SetTint(new Color(1f, 1f, 1f, alpha));
            // 파편은 반투명 — 맞은 몸이 비쳐 보여야 한다(검수)
            var debris = PlayLz("lzdebris", at, LzDebrisSize * scale, 0.06f);
            if (debris != null) debris.SetTint(new Color(1f, 1f, 1f, 0.4f * (lead ? 1f : 0.7f)));
            var arc = PlayLz("lzarc", at, LzArcSize * scale, 0.08f);
            if (arc != null && !lead) arc.SetTint(new Color(1f, 1f, 1f, 0.7f));
            if (sky) Kick(Vector2.down, LzHitShake);   // 내리꽂힌 쪽으로 한 번 밀렸다 돌아온다 — 떨지 않는다
            HitEnemyWith(p.Target, p.Damage, _host.Profile);
        }

        private Impact PlayLz(string name, Vector2 at, float size, float step)
        {
            var frames = FxFrames(name);
            if (frames == null) return null;
            var im = FreeImpact(size);
            if (im == null) return null;
            im.Play(at, frames, size, loop: false);
            im.SetFrameSeconds(step);
            return im;
        }

        private void ClearLz()
        {
            _lzPending.Clear();
            _lzTicks = 0;
        }
    }
}
