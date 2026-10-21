using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// SSR 필살기 시안 다듬기(PD 2026-10-15 「SSR 부터 시안 퀄로 · 연출이 별로면 새로 다 발주」).
    /// 시안: `Projects/AVSR/_exchange/in/mock_skill_{amazon_elite_v2, dragoon_v2, medium_golem, vampire}.png`
    /// 부품: `_exchange/in/part_{el,dg,md,vp}_*.png` → `fx_{elaura, elburst, elcrack, dgdragon, dgbreath, dgblast, dgrise,
    ///        mdrune, mdslam, mddust, vpclaw, vporb, vpburst}` (자르기 `Tools/cut_skill_rework_parts.py`)
    /// 부품이 없으면 예전 그림으로 떨어진다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // 크기는 몸(약 90) 기준 배수 — 시안 실측(시안 몸 높이 대비)으로 맞춘다
        private const float SsrBody = 90f;

        // ═══════════════════════════════════════════════════════════
        //  흡혈귀 — 피의 폭주: 적 사이를 잔상으로 날아다니며 할퀴고, 피 구슬을 빨아들여 마지막에 터뜨린다
        // ═══════════════════════════════════════════════════════════

        private const int VpMaxTargets = 6;
        private const float VpDashSeconds = 0.12f;
        private const float VpPauseSeconds = 0.1f;
        private static readonly Color VpTrail = new(1f, 0.35f, 0.4f, 1f);

        private void SsrVampire(Unit me)
        {
            int pay = Mathf.Max(1, me.HpMax / 10);
            if (me.Hp > pay + 1) me.TakeDamage(pay);
            var aura = UltLoop("rpaura", me.Position, SsrBody * 1.7f);
            aura?.SetTint(new Color(1f, 0.5f, 0.55f, 1f));
            var list = UltTargets(me.Position, Meters(9f));
            if (list.Count > VpMaxTargets) list.RemoveRange(VpMaxTargets, list.Count - VpMaxTargets);
            UltInvuln(0.3f + list.Count * (VpDashSeconds + VpPauseSeconds) + 0.6f);
            _vpFollowAura = aura;
            // 시안 첫 컷 「망토에서 붉은 오라」 — 출발 전에 한 박자 피 기운을 뿜는다
            _pfx?.Spray(ParticleFxKind.Streak, me.Position, Vector2.up, 50f, 16, 220f, 420f, new Vector2(48f, 7f), 0.45f,
                        new Color(1f, 0.15f, 0.25f, 1f));
            UltAfter(0.4f, () => VpNext(me, list, 0));
        }

        private Impact _vpFollowAura;

        private void VpNext(Unit me, List<Unit> list, int i)
        {
            while (i < list.Count && (list[i] == null || !list[i].IsAlive)) i++;
            if (i >= list.Count) { UltAfter(0.15f, () => VpFinish(me)); return; }
            var t = list[i];
            var to = t.Position - (t.Position - me.Position).normalized * Meters(0.7f);
            VpTrailLine(me.Position, to, i);
            UltDashTo(me, to, VpDashSeconds, VpTrail, () =>
            {
                _vpFollowAura?.MoveTo(me.Position);
                if (t != null && t.IsAlive)
                {
                    me.SetFacing(t.Position - me.Position);
                    me.PlayAttack();
                    UltHit(me, t, 1.8f);
                    var claw = PlayFx("vpclaw", t.Position, SsrBody * 2.8f, loop: false) ?? PlayFx("rpslash", t.Position, 130f, loop: false);
                    claw?.SetFrameSeconds(0.045f);
                    _pfx?.Spray(ParticleFxKind.Spark, t.Position, (t.Position - me.Position).normalized, 50f, 8,
                                150f, 320f, new Vector2(10f, 10f), 0.35f, new Color(0.85f, 0.05f, 0.12f, 1f));
                    for (int k = 0; k < 4; k++) VpOrb(me, t.Position + Random.insideUnitCircle * 44f, 0.6f + k * 0.12f);
                    HitStop(0.035f);
                }
                UltAfter(VpPauseSeconds, () => VpNext(me, list, i + 1));
            });
        }

        /// <summary>
        /// 돌진 자국 — 시안의 「적 사이를 잇는 진홍 호」. 지나간 길을 따라 붉은 빛줄을 깔고 살짝 휘게 한다(번갈아 좌우로).
        /// </summary>
        private void VpTrailLine(Vector2 from, Vector2 to, int index)
        {
            var d = to - from;
            float len = d.magnitude;
            if (len < 4f) return;
            var dir = d / len;
            var side = new Vector2(-dir.y, dir.x) * ((index & 1) == 0 ? 1f : -1f);
            int steps = Mathf.Clamp(Mathf.RoundToInt(len / 9f), 6, 60);
            for (int k = 0; k <= steps; k++)
            {
                float u = k / (float)steps;
                // 호 모양 — 가운데가 옆으로 부푼다. 접선 방향으로 빛줄을 눕힌다
                var at = from + d * u + side * (Mathf.Sin(u * Mathf.PI) * len * 0.18f);
                var tan = (dir + side * (Mathf.Cos(u * Mathf.PI) * 0.56f)).normalized;
                _pfx?.Spray(ParticleFxKind.Streak, at, tan, 2f, 1, 10f, 30f, new Vector2(76f, 10f), 0.65f,
                            new Color(1f, 0.12f, 0.22f, 1f));
            }
            _pfx?.Spray(ParticleFxKind.Spark, from, -dir, 40f, 6, 80f, 200f, new Vector2(8f, 8f), 0.35f,
                        new Color(0.8f, 0.05f, 0.1f, 1f));
        }

        /// <summary>피 구슬 — 적 자리에서 흡혈귀에게 날아와 닿으면 1% 회복.</summary>
        private void VpOrb(Unit me, Vector2 from, float seconds)
        {
            var orb = TakeLoopFx("vporb", from, 64f);
            if (orb == null) { _pfx?.Dart(from, me.Position, ParticleElement.Fire, seconds, 2); return; }
            orb.SetFrameSeconds(0.07f);
            _vpOrbs.Add((orb, from, 0f, seconds));
        }

        private readonly List<(Impact Fx, Vector2 From, float T, float Dur)> _vpOrbs = new();

        private void TickVpOrbs(float dt)
        {
            var me = _host;
            for (int i = _vpOrbs.Count - 1; i >= 0; i--)
            {
                var o = _vpOrbs[i];
                o.T += dt;
                float k = Mathf.Clamp01(o.T / o.Dur);
                if (me == null) { o.Fx?.Stop(); _vpOrbs.RemoveAt(i); continue; }
                var at = Vector2.Lerp(o.From, me.Position + new Vector2(0f, 20f), k * k)
                       + Vector2.up * (Mathf.Sin(k * Mathf.PI) * 70f);
                o.Fx?.MoveTo(at);
                // 구슬 그림은 꼬리가 오른쪽 — 날아가는 쪽 반대로 꼬리가 끌리게 돌린다
                var to = me.Position - at;
                if (o.Fx != null && to.sqrMagnitude > 1f)
                    o.Fx.transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg - 180f);
                if (k < 1f) { _vpOrbs[i] = o; continue; }
                o.Fx?.Stop();
                _vpOrbs.RemoveAt(i);
                Leech(Mathf.Max(1, me.HpMax / 100));   // 숫자 · 체력바까지(예전 Unit.Heal 은 발밑 막대만 고쳤다)
            }
        }

        private void VpFinish(Unit me)
        {
            for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
            _ultFx.Clear();
            _vpFollowAura = null;
            var burst = PlayFx("vpburst", me.Position, SsrBody * 4.8f, loop: false);
            burst?.SetFrameSeconds(0.07f);
            UltAoe(me, me.Position, 2.5f, 3.5f, burst == null ? "burst" : null);
            Leech(Mathf.Max(1, me.HpMax * 10 / 100));
            Shake(10f);
            HitStop(0.1f);
        }

        // ═══════════════════════════════════════════════════════════
        //  드라군 — 용 돌격: 불기둥 속에서 용이 솟고, 드라군이 올라타 불을 뿜으며 돌진, 끝에 대형 화염구
        // ═══════════════════════════════════════════════════════════

        private Impact _dgMount, _dgBreath;
        // 용 그림 칸 192x160 — preserveAspect 라 폭 = 크기, 높이 = 크기 × 160/192
        private const float DgMountW = SsrBody * 2.4f;      // 시안: 용 몸이 드라군의 약 2배
        private const float DgMountH = DgMountW * 160f / 192f;
        // 칸 안 자리(가운데 기준, 오른쪽 · 위가 +) — 납품 그림(part_dg_dragon)을 재서 옮겼다
        private static readonly Vector2 DgMouth = new(0.42f, 0.20f);
        private static readonly Vector2 DgSaddle = new(0.05f, 0.13f);
        // 옆모습은 ±40° 까지만 — 85° 로 세우면 용으로 안 읽혔다(녹화 ssr_dg6).
        // 무리가 가파르게 위에 있으면(대개 그렇다) 뒷모습 그림(`dgdragonup`, 칸 160x192)으로 바꿔 위로 솟구친다
        private const float DgMaxTilt = 40f;
        private const float DgUpFrom = 50f;                  // 이보다 가파르게 위면 뒷모습
        private const float DgUpSway = 25f;                  // 뒷모습은 세로에서 이만큼만 기운다
        private const float DgUpH = SsrBody * 2.4f;
        private const float DgUpW = DgUpH * 160f / 192f;
        private static readonly Vector2 DgUpMouth = new(0f, 0.42f);
        private static readonly Vector2 DgUpSaddle = new(0f, 0.10f);
        private bool _dgUp;
        private const float DgBreathLen = 300f;
        private const float DgBreathThick = 130f;
        private float _dgTilt;
        private bool _dgLeft;
        private float _dgTrailT;

        private void SsrDragoon(Unit me)
        {
            DgPickDir(me);
            var rise = PlayFx("dgrise", me.Position + new Vector2(0f, SsrBody * 0.6f), SsrBody * 2.6f, loop: false);
            rise?.SetFrameSeconds(0.05f);
            _pfx?.Burst(ParticleFxKind.Shard, ParticleElement.Dust, me.Position, 14, 260f, 18f, 0.6f, 360f);
            Shake(6f);
            UltInvuln(1.9f);
            UltAfter(0.35f, () => DgCharge(me));
        }

        private void DgCharge(Unit me)
        {
            _ultKind = "dragoon";
            _ultT = 0f;
            _dcDrop = 0f;
            me.SetFacing(_dcDir);
            // 용은 몸 **뒤**(바닥 층)에 — 드라군이 등에 탄 것으로 보인다
            _dgTrailT = 0f;
            _dgMount = _dgUp ? PlayFloorFx("dgdragonup", me.Position, DgUpH, loop: true)
                             : PlayFloorFx("dgdragon", me.Position, DgMountW, loop: true);
            if (_dgMount != null)
            {
                _dgMount.SetFrameSeconds(0.08f);
                DgOrient(_dgMount);
                _dgMount.MoveTo(DgCenter(me));
                _ultFx.Add(_dgMount);
            }
            var frames = FxFrames("dgbreath") ?? FxFrames("breath_fire");
            if (frames != null)
            {
                _dgBreath = FreeImpact(DgBreathThick);
                var mouth = DgMouthAt(me);
                _dgBreath?.PlayBeam(mouth, mouth + _dcDir * DgBreathLen, frames, DgBreathThick);
                _dgBreath?.SetLoop(true);
                _dgBreath?.SetFrameSeconds(0.05f);
                if (_dgBreath != null) _ultFx.Add(_dgBreath);
            }
            UltDashTo(me, me.Position + _dcDir * _dgDashLen, 1.2f, new Color(1f, 0.55f, 0.3f, 1f), () =>
            {
                _ultKind = null;
                for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
                _ultFx.Clear();
                _dgMount = _dgBreath = null;
                var at = me.Position + _dcDir * Meters(1.5f);
                var blast = PlayFx("dgblast", at, Meters(2.6f) * 2.4f, loop: false);
                blast?.SetFrameSeconds(0.06f);
                UltAoe(me, at, 2.6f, 4.5f, blast == null ? "burst" : null);
                _pfx?.Burst(ParticleFxKind.Ember, ParticleElement.Fire, at, 24, 320f, 22f, 0.9f, 0f);
                Shake(13f);
                HitStop(0.12f);
            });
        }

        private float _dgDashLen;

        /// <summary>
        /// 화면 안 적 무리의 한가운데로 돌진한다(방향은 `DgClampDir` 이 그림에 맞게 묶는다).
        /// 가장 가까운 적 · 길에 드는 수로 고르면 벽 쪽 한두 마리로 달려 무리를 비켜 갔다(녹화 ssr_dg1~4).
        /// </summary>
        private void DgPickDir(Unit me)
        {
            var bestDir = me.Facing.x < 0f ? Vector2.left : Vector2.right;
            float reach = Meters(7f);
            Vector2 sum = Vector2.zero;
            int n = 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (!Targetable(e) || !IsOnScreen(e)) continue;
                sum += e.Position;
                n++;
            }
            if (n > 0)
            {
                var d = sum / n - me.Position;
                if (d.sqrMagnitude > 1f) bestDir = d.normalized;
            }
            _dcDir = bestDir;
            DgClampDir();
            // 벽에 처박히지 않게 — 방 안에서 갈 수 있는 만큼만
            var end = ClampedInField(me, me.Position + _dcDir * reach);
            _dgDashLen = Mathf.Max(Meters(2f), Vector2.Distance(me.Position, end) - Meters(0.6f));
        }

        /// <summary>
        /// 옆모습이면 가로 ±40° · 뒷모습이면 세로 ±25° 안으로 — 용 그림 · 불줄기 · 몸이 한 줄로 맞는다.
        /// </summary>
        private void DgClampDir()
        {
            float up = Mathf.Atan2(_dcDir.y, Mathf.Abs(_dcDir.x)) * Mathf.Rad2Deg;
            _dgUp = up > DgUpFrom && FxFrames("dgdragonup") != null;
            if (_dgUp)
            {
                // 세로(90°)에서 좌우로 조금만 — _dgTilt 는 세로 기준 기울기(+ 가 왼쪽)
                float fromUp = Mathf.Atan2(-_dcDir.x, _dcDir.y) * Mathf.Rad2Deg;
                _dgTilt = Mathf.Clamp(fromUp, -DgUpSway, DgUpSway);
                _dgLeft = false;
                float ur = _dgTilt * Mathf.Deg2Rad;
                _dcDir = new Vector2(-Mathf.Sin(ur), Mathf.Cos(ur));
                return;
            }
            _dgLeft = _dcDir.x < 0f;
            float deg = Mathf.Atan2(_dcDir.y, Mathf.Abs(_dcDir.x)) * Mathf.Rad2Deg;
            _dgTilt = Mathf.Clamp(deg, -DgMaxTilt, DgMaxTilt);
            float rad = _dgTilt * Mathf.Deg2Rad;
            _dcDir = new Vector2(Mathf.Cos(rad) * (_dgLeft ? -1f : 1f), Mathf.Sin(rad));
        }

        /// <summary>용 그림(오른쪽을 보는 옆모습)을 돌진 방향으로 — 왼쪽이면 뒤집고 기울기를 반대로.</summary>
        private void DgOrient(Impact im)
        {
            if (_dgUp)
            {
                im.transform.localEulerAngles = new Vector3(0f, 0f, _dgTilt);
                im.transform.localScale = Vector3.one;
                return;
            }
            im.transform.localEulerAngles = new Vector3(0f, 0f, _dgLeft ? -_dgTilt : _dgTilt);
            im.transform.localScale = new Vector3(_dgLeft ? -1f : 1f, 1f, 1f);
        }

        /// <summary>그림 칸 안 한 점(가운데 기준)을 화면 자리로 — 뒤집기 · 기울기를 따라간다.</summary>
        private Vector2 DgLocal(Vector2 cell)
        {
            var v = _dgUp ? new Vector2(cell.x * DgUpW, cell.y * DgUpH)
                          : new Vector2(cell.x * DgMountW * (_dgLeft ? -1f : 1f), cell.y * DgMountH);
            float rad = (_dgUp ? _dgTilt : _dgLeft ? -_dgTilt : _dgTilt) * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), sn = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * sn, v.x * sn + v.y * c);
        }

        // 드라군 몸 자리 = 안장 — 용 가운데는 거기서 안장만큼 뺀 자리
        private Vector2 DgCenter(Unit me) => me.Position - DgLocal(_dgUp ? DgUpSaddle : DgSaddle);

        private Vector2 DgMouthAt(Unit me) => DgCenter(me) + DgLocal(_dgUp ? DgUpMouth : DgMouth);

        private void TickSsrDragoon(Unit me, float dt)
        {
            _dgMount?.MoveTo(DgCenter(me));
            var mouth = DgMouthAt(me);
            _dgBreath?.MoveTo(mouth + _dcDir * (DgBreathLen * 0.5f));
            // 시안 「지나간 길에 불꽃 줄」 — 용 뒤로 불빛 줄 · 불씨를 흘린다
            _dgTrailT -= dt;
            if (_dgTrailT > 0f) return;
            _dgTrailT = 0.04f;
            var tail = DgCenter(me) - _dcDir * ((_dgUp ? DgUpH : DgMountW) * 0.45f);
            _pfx?.Spray(ParticleFxKind.Streak, tail, -_dcDir, 14f, 3, 120f, 260f, new Vector2(64f, 8f), 0.35f,
                        new Color(1f, 0.55f, 0.15f, 1f));
            _pfx?.Burst(ParticleFxKind.Ember, ParticleElement.Fire, tail, 3, 90f, 14f, 0.6f, 0f);
        }

        // ═══════════════════════════════════════════════════════════
        //  영매 — 골렘 강령: 푸른 강령 문양에서 골렘(1.5배)이 솟아 8초 동안 2초마다 내리찍고, 빛 가루로 사라진다
        // ═══════════════════════════════════════════════════════════

        private const float GolemUltSeconds = 8f;
        private const float GolemUltScale = 2.2f;   // 시안: 골렘이 영매의 2.5배쯤 — 1.5 는 잡몹과 비슷해 보였다(녹화 ssr_md1)
        private const float GolemSlamEvery = 2f;
        private Unit _golemUlt;
        private bool _golemUltOn;
        private Vector2 _golemUltPos;
        private float _golemSlamT;

        private void SsrGolem(Unit me)
        {
            var at = ClampedInField(me, me.Position + me.Facing * Meters(1.5f));
            var rune = PlayFloorFx("mdrune", at, SsrBody * 3.4f, loop: true);
            if (rune != null) { rune.SetFrameSeconds(0.07f); rune.SetLife(1.6f, 0.4f); }
            else QxGolemRune(at);
            me.PlayAttack();
            UltAfter(0.5f, () =>
            {
                SpawnSummon(GolemKey, "골렘", GolemHpPercent, GolemAtkPercent, GolemUltSeconds,
                            attacks: true, mobile: true, taunt: true, at: at, scale: GolemUltScale, profile: GolemProfile);
                _golemUlt = _summons.Count > 0 ? _summons[_summons.Count - 1].U : null;
                _golemUltOn = _golemUlt != null;
                _golemUltPos = at;
                _golemSlamT = 0.6f;
                Shake(9f);
                HitStop(0.06f);
                // 시안 첫 컷 — 땅이 갈라지며 돌덩이가 솟구친다(균열은 역린과 같은 부품을 같이 쓴다)
                // 시안 「지팡이로 바닥을 짚어 골렘 등장」의 푸른 빛기둥 — 문양을 한 번 더 크게 터뜨린다
                PlayFx("mdslam", at, SsrBody * 4.4f, loop: false)?.SetFrameSeconds(0.05f);
                var crack = PlayFloorFx("elcrack", at, SsrBody * 3.2f, loop: true);
                crack?.SetLife(2f, 0.6f);
                _pfx?.Burst(ParticleFxKind.Shard, ParticleElement.Dust, at, 12, 300f, 30f, 0.7f, 360f);
                _pfx?.Spray(ParticleFxKind.Streak, at, Vector2.up, 30f, 26, 320f, 640f, new Vector2(84f, 11f), 0.55f,
                            new Color(0.55f, 0.85f, 1f, 1f));
            });
        }

        private void TickSsrGolem(float dt)
        {
            if (!_golemUltOn) return;
            var g = _golemUlt;
            var me = _host;
            if (me == null || g == null || !g.IsAlive)
            {
                // 수명이 다했다(소환 목록이 Destroy 한다) — 마지막 자리에서 빛 가루로 흩어진다
                var dust = PlayFx("mddust", _golemUltPos + new Vector2(0f, SsrBody * 0.5f), SsrBody * 2.6f, loop: false);
                dust?.SetFrameSeconds(0.07f);
                _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Ice, _golemUltPos, 18, 160f, 24f, 0.9f, 0f);
                _golemUltOn = false;
                _golemUlt = null;
                return;
            }
            _golemUltPos = g.Position;
            _golemSlamT -= dt;
            if (_golemSlamT > 0f) return;
            _golemSlamT = GolemSlamEvery;
            g.PlayAttack();
            var slam = PlayFloorFx("mdslam", g.Position, SsrBody * 5.2f, loop: false);
            if (slam != null) slam.SetFrameSeconds(0.06f);
            UltAoe(me, g.Position, 2.2f, 2.5f, slam == null ? "slam" : null, 2.2f, knockMeters: 1.5f);
            _pfx?.Burst(ParticleFxKind.Shard, ParticleElement.Dust, g.Position, 8, 260f, 26f, 0.6f, 360f);
            Shake(7f);
        }

        // ═══════════════════════════════════════════════════════════
        //  아마존 정예 — 역린 (흡수 틱은 BattleDirector.Ultimates `TickWrath`) · 붉은 기운 · 균열 · 방사 섬광
        // ═══════════════════════════════════════════════════════════

        private void SsrWrathOpen(Unit me)
        {
            var aura = UltLoop("elaura", me.Position, SsrBody * 2f) ?? UltLoop("rpaura", me.Position, 140f);
            aura?.SetFrameSeconds(0.06f);
        }

        private void SsrWrathBurst(Unit me, float mul)
        {
            var crack = PlayFloorFx("elcrack", me.Position, SsrBody * 4.6f, loop: true);
            crack?.SetLife(1.8f, 0.6f);
            var burst = PlayFx("elburst", me.Position, SsrBody * 5.6f, loop: false);
            burst?.SetFrameSeconds(0.05f);
            UltAoe(me, me.Position, 3f, mul, burst == null ? "slam" : null, 2.2f, knockMeters: 2f);
            _pfx?.Burst(ParticleFxKind.Shard, ParticleElement.Dust, me.Position, 18, 420f, 34f, 0.8f, 360f);
            _pfx?.Spray(ParticleFxKind.Streak, me.Position, Vector2.up, 180f, 24, 300f, 520f, new Vector2(46f, 5f), 0.35f,
                        new Color(1f, 0.3f, 0.35f, 1f));
        }
    }
}
