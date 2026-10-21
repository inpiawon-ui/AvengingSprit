using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Module.InGame
{
    /// <summary>
    /// 필살기 18종 — 컨셉 기획 2차(PD 2026-10-15 「나머지 필살기도 진행 · 밸런스는 너가 생각하는 대로」).
    /// 시안: `Projects/AVSR/_exchange/doc_skill_concept` · 컨셉 단일 출처 `Projects/AVSR/Tools/skill_concepts.py`.
    ///
    /// 연출은 10-07 퀄업 때 만든 부품(fx_*)과 파티클을 다시 쓴다 — 시안과 나란히 맞추는 다듬기는 PD 확인 뒤 몸마다.
    ///
    /// 밸런스(피해 = 몸 한 대 × 배율 × 성급 · 등급 필살기 배율, `UltDamage`):
    ///   한 방 정리형(단일 · 처형)      한 놈에 ×10~14 — 보스는 최대 체력 18% 상한
    ///   광역 마무리형(폭발 · 파쇄)     둘레 ×3~5
    ///   지속형(소환 · 장판 · 위성)     초당 ×1 안팎, 끝에 ×3~4 한 번
    ///   빙의 1회라 액티브(×1~3)의 서너 배가 기준이다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float UltBossCapPercent = 0.18f;   // 한 방 정리형이 보스에게 주는 상한(최대 체력 비율)

        // ── 예약 · 공통 ──────────────────────────────────────

        private readonly List<(float Left, Action Run)> _ultQueue = new();
        private string _ultKind;     // 지금 돌고 있는 지속형 필살기(위성 · 방벽 · 흡수 · 돌진 …)
        private float _ultT;
        private float _ultDur;

        private void UltAfter(float seconds, Action run) => _ultQueue.Add((seconds, run));

        private void StopUltimates()
        {
            StopSrMissiles();
            _ultQueue.Clear();
            ClearWraiths();
            _ultKind = null;
            for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
            _ultFx.Clear();
            _udActive = false;
        }

        private readonly List<Impact> _ultFx = new();

        private Impact UltLoop(string name, Vector2 at, float size)
        {
            var im = TakeLoopFx(name, at, size);
            if (im != null) _ultFx.Add(im);
            return im;
        }

        private void TickUltimates(float dt)
        {
            TickUltReadyMarks();
            for (int i = _ultQueue.Count - 1; i >= 0; i--)
            {
                var q = _ultQueue[i];
                q.Left -= dt;
                if (q.Left > 0f) { _ultQueue[i] = q; continue; }
                _ultQueue.RemoveAt(i);
                if (_host != null) q.Run();
            }
            TickUltDash(dt);
            TickSrMissiles(dt);
            if (_ultKind == null) return;
            var me = _host;
            if (me == null || !me.IsAlive) { StopUltimates(); return; }
            _ultT += dt;
            switch (_ultKind)
            {
                case "guru": TickSealing(me, dt); break;
                case "amazon_elite": TickWrath(me, dt); break;
                case "gangster": TickContract(me); break;
                case "hopper_smg": TickSrHopperSmg(me); break;
            }
        }

        /// <summary>화면 안의 공격할 수 있는 적(시전 순간 한 번 — 새 목록).</summary>
        private List<Unit> UltTargets(Vector2 from, float radius)
        {
            var list = new List<Unit>();
            float r2 = radius * radius;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (!Targetable(e) || !IsOnScreen(e)) continue;
                if ((e.Position - from).sqrMagnitude > r2) continue;
                list.Add(e);
            }
            list.Sort((a, b) => (a.Position - from).sqrMagnitude.CompareTo((b.Position - from).sqrMagnitude));
            return list;
        }

        /// <summary>화면 안에서 가장 센 적 — 보스 · 엘리트 먼저, 그다음 최대 체력.</summary>
        private Unit UltStrongest(Vector2 from)
        {
            Unit best = null;
            float score = -1f;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (!Targetable(e) || !IsOnScreen(e)) continue;
                float s = e.HpMax * (e.IsBoss ? 100f : e.IsElite ? 10f : 1f);
                if (s > score) { score = s; best = e; }
            }
            return best;
        }

        private void UltHit(Unit me, Unit e, float mul, bool capBoss = false)
        {
            if (e == null || !e.IsAlive) return;
            int dmg = UltDamage(me, mul);
            if (capBoss && e.IsBoss) dmg = Mathf.Min(dmg, Mathf.Max(1, Mathf.RoundToInt(e.HpMax * UltBossCapPercent)));
            HitEnemyWith(e, dmg, me.Profile);
        }

        private void UltAoe(Unit me, Vector2 at, float radiusMeters, float mul, string fx, float fxScale = 2.2f,
                            float knockMeters = 0f)
        {
            float r = Meters(radiusMeters) * _buffs.AoeMul;
            var list = new List<Unit>(EnemiesInRange(at, r));
            for (int i = 0; i < list.Count; i++)
            {
                UltHit(me, list[i], mul);
                if (knockMeters > 0f && list[i].IsAlive && !list[i].IsBoss) Knockback(list[i], knockMeters);
            }
            if (fx != null) PlayFx(fx, at, r * fxScale, loop: false)?.SetFrameSeconds(0.05f);
        }

        private void UltBeam(string frames, Vector2 from, Vector2 to, float thickness, float frameSeconds = 0.05f)
        {
            var f = FxFrames(frames);
            if (f == null) return;
            var im = FreeImpact(thickness);
            im?.PlayBeam(from, to, f, thickness);
            im?.SetFrameSeconds(frameSeconds);
        }

        private void UltInvuln(float seconds)
        {
            _invuln = Mathf.Max(_invuln, seconds);
            _skillInvuln = Mathf.Max(_skillInvuln, seconds);
        }

        // ── 돌진(닌자 사슬 · 호퍼 기관단총 · 드라군) ──────────────

        private bool _udActive;
        private Vector2 _udFrom, _udTo;
        private float _udT, _udDur, _udTrail;
        private Action _udArrive;
        private Color _udTint = Color.white;

        private void UltDashTo(Unit me, Vector2 to, float seconds, Color tint, Action arrive)
        {
            _udFrom = me.Position;
            _udTo = ClampedInField(me, to);
            _udT = 0f;
            _udDur = Mathf.Max(0.05f, seconds);
            _udTrail = 0f;
            _udArrive = arrive;
            _udTint = tint;
            _udActive = true;
            me.SetFacing(_udTo - _udFrom);
            UltInvuln(seconds + 0.2f);
        }

        private void TickUltDash(float dt)
        {
            if (!_udActive) return;
            var me = _host;
            if (me == null || !me.IsAlive) { _udActive = false; return; }
            _udT += dt;
            float k = Mathf.Clamp01(_udT / _udDur);
            me.Position = Vector2.Lerp(_udFrom, _udTo, Game.Module.Common.UI.Ease.OutCubic(k));
            _udTrail -= dt;
            if (_udTrail <= 0f)
            {
                _udTrail = 0.03f;
                var sprite = me.BodySprite;
                var img = sprite != null ? RentAfterimage() : null;
                img?.Play(sprite, me.Position, me.GetComponent<RectTransform>().sizeDelta, me.BodyFlipX, 0.6f, 0.3f, _udTint);
            }
            if (k < 1f) return;
            _udActive = false;
            var a = _udArrive;
            _udArrive = null;
            a?.Invoke();
        }

        // ═══════════════════════════════════════════════════════════
        //  한 놈을 세게
        // ═══════════════════════════════════════════════════════════

        // 갱스터 — 청부 처형: 가장 센 적을 1.5초 조준 → 초대형 한 발(보스 상한 18%)
        private Unit _contractTarget;
        private Impact _contractLock;

        private void UltGangster(Unit me)
        {
            _contractTarget = UltStrongest(me.Position);
            if (_contractTarget == null) { UltFallback(me); return; }
            _ultKind = "gangster";
            _ultT = 0f;
            // R 시안(ult_gangster) — 붉은 저격 조준경이 조여든다 · 총구에 붉은 빛이 모인다
            _contractLock = UltLoop("gslock", _contractTarget.Position, SsrBody * 2f)
                            ?? UltLoop("critlock", _contractTarget.Position, 150f);
            // 새 조준경은 그림 6컷이 스스로 조여든다(1.56초 — 1.5초 안에 다시 처음으로 안 돌게). 옛 그림만 크기로 조인다
            if (FxFrames("gslock") != null) _contractLock?.SetFrameSeconds(0.26f);
            else _contractLock?.SetSnap(2.2f, 1.4f);
            _pfx?.Spray(ParticleFxKind.Streak, me.MuzzlePosition, Vector2.up, 180f, 10, 60f, 140f, new Vector2(18f, 4f), 0.4f,
                        new Color(1f, 0.25f, 0.3f, 1f), spread: 26f);
            me.SetFacing(_contractTarget.Position - me.Position);
            UltAfter(1.5f, () =>
            {
                _ultKind = null;
                _contractLock?.Stop();
                var t = _contractTarget;
                if (t == null || !t.IsAlive) return;
                me.SetFacing(t.Position - me.Position);
                me.PlayAttack();
                if (FxFrames("gsbeam") != null) UltBeam("gsbeam", me.MuzzlePosition, t.Position, 130f, 0.04f);
                else UltBeam("wpbeam", me.MuzzlePosition, t.Position, 70f);
                UltHit(me, t, 14f, capBoss: true);
                CritBurst(t.Position);
                (PlayFx("gsburst", t.Position, SsrBody * 2.6f, loop: false) ?? PlayFx("crit", t.Position, 220f, loop: false))
                    ?.SetFrameSeconds(0.05f);
                _pfx?.Spray(ParticleFxKind.Spark, t.Position, (t.Position - me.Position).normalized, 50f, 14, 200f, 420f,
                            new Vector2(10f, 10f), 0.4f, new Color(0.85f, 0.05f, 0.1f, 1f));
                // 총구 연기 — 쏘고 난 뒤의 한 박자
                _pfx?.Spray(ParticleFxKind.Smoke, me.MuzzlePosition, Vector2.up, 25f, 5, 20f, 50f, new Vector2(30f, 30f), 0.9f,
                            Color.white, element: ParticleElement.Ice);
                Shake(12f);
                HitStop(0.14f);
            });
        }

        private void TickContract(Unit me)
        {
            if (_contractTarget == null || !_contractTarget.IsAlive) return;
            _contractLock?.MoveTo(_contractTarget.Position);
            me.SetFacing(_contractTarget.Position - me.Position);
        }

        // 호퍼 — 킬 포인트: 앞 적들에게 붉은 칼날 → 붉은 사슬 고리로 묶음 → 적 사이를 돌진하며 X 베기(R 시안 ult_hopper)
        private void UltHopper(Unit me)
        {
            var list = UltTargets(me.Position, Meters(7f));
            if (list.Count == 0) { UltFallback(me); return; }
            if (list.Count > 8) list.RemoveRange(8, list.Count - 8);
            me.PlayAttack();
            UltInvuln(0.8f + list.Count * 0.2f);
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                UltBeam("wptracer", me.MuzzlePosition, e.Position, 26f, 0.04f);
                _pfx?.Spray(ParticleFxKind.Streak, me.MuzzlePosition, (e.Position - me.Position).normalized, 4f, 2, 900f, 1100f,
                            new Vector2(40f, 5f), 0.18f, new Color(1f, 0.3f, 0.35f, 1f));
                e.ApplyRoot(3f);
                var bind = PlayFloorFx("hpbind", e.Position, SsrBody * 1.5f, loop: true);
                if (bind != null) { bind.SetFrameSeconds(0.08f); bind.SetLife(0.6f + list.Count * 0.2f + 0.4f, 0.3f); }
                else PlayFx("critlock", e.Position, 90f, loop: false);
            }
            UltAfter(0.6f, () => HpDashNext(me, list, 0));
        }

        /// <summary>묶인 적 사이를 차례로 돌진하며 X 자로 벤다 — 시안 마지막 컷(잔상 · 붉은 X 칼빛).</summary>
        private void HpDashNext(Unit me, List<Unit> list, int i)
        {
            while (i < list.Count && (list[i] == null || !list[i].IsAlive)) i++;
            if (i >= list.Count) return;
            var e = list[i];
            var to = e.Position + (e.Position - me.Position).normalized * Meters(0.6f);
            UltDashTo(me, to, 0.1f, new Color(0.55f, 0.65f, 1f, 1f), () =>
            {
                if (e != null && e.IsAlive)
                {
                    UltHit(me, e, 3.5f * CritMultiplier, capBoss: true);
                    CritBurst(e.Position);
                    PlayFx("hpslash", e.Position, SsrBody * 2.4f, loop: false)?.SetFrameSeconds(0.045f);
                    _pfx?.Spray(ParticleFxKind.Spark, e.Position, Random.insideUnitCircle.normalized, 70f, 8, 160f, 340f,
                                new Vector2(9f, 9f), 0.35f, new Color(0.85f, 0.05f, 0.1f, 1f));
                    HitStop(0.03f);
                }
                UltAfter(0.06f, () => HpDashNext(me, list, i + 1));
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  불러서 같이 싸운다
        // ═══════════════════════════════════════════════════════════

        // 사신 — 망자 군단: 해골 다섯이 일어나 싸우고(6초), 원혼이 두 번 적마다 날아가 터진다
        // PD 2026-10-21 「해골 및 유령 군단이라는데 그냥 해골 5마리」 — 예전 유령은 빛 알갱이(파티클)뿐이라 안 보였다.
        //   이제 원혼 그림(`mdwraith`)이 직접 날아간다(BattleDirector.Rework1021 `LaunchWraith`)
        private const float DeathSkullSeconds = 6f;

        private void UltDeath(Unit me)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = (-60f + i * 30f) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                var at = ClampedInField(me, me.Position + dir * Meters(1.4f));
                SummonSkull(at, DeathSkullSeconds);
                PlayFx("soulripple", at, Meters(1.2f), loop: false);
            }
            // 원혼은 해골과 함께 **바로** 떠오른다 — 1.6초 뒤엔 해골이 방을 비워 원혼이 한 번도 안 떴다(PD 2026-10-21)
            const int Wraiths = 6;
            for (int wave = 0; wave < 2; wave++)
            {
                UltAfter(0.3f + wave * 2.2f, () =>
                {
                    var list = UltTargets(me.Position, Meters(12f));
                    for (int i = 0; i < Wraiths; i++)
                    {
                        // 적이 여섯보다 적으면 돌려 쓰고, 없으면 대상 없이 떠올라 머무는 사이 다시 찾는다
                        var e = list.Count > 0 ? list[i % list.Count] : null;
                        int slot = i;
                        UltAfter(i * 0.06f, () => LaunchWraith(me, e, slot, Wraiths));
                    }
                    PlayFx("soulripple", me.Position, Meters(2.4f), loop: false);
                    Shake(5f);
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  끌어모아 한 방 · 깔아 두고 터진다
        // ═══════════════════════════════════════════════════════════

        // 구루 — 결계 가두기: 앞에 결계 → 1.5초 끌어모아 묶음(보스 제외) → 내폭. 그동안 받는 피해 −50%
        private Vector2 _sealAt;

        private void UltGuru(Unit me)
        {
            var near = NearestEnemy(me.Position);
            _sealAt = ClampedInField(me, near != null ? near.Position : me.Position + me.Facing * Meters(2.5f));
            _ultKind = "guru";
            _ultT = 0f;
            // R 시안(ult_guru) — 바닥에 금빛 만다라 · 가장자리 빛기둥 → 적이 끌려 들어 묶임 → 금빛 폭발
            var mandala = PlayFloorFx("grmandala", _sealAt, Meters(5.6f), loop: true);
            if (mandala != null) { mandala.SetFrameSeconds(0.09f); mandala.SetLife(1.7f, 0.3f); }
            else UltLoop("gddome", _sealAt, Meters(5f));
            _wardSeconds = Mathf.Max(_wardSeconds, 2f);
            _wardReduce = 0.5f;
            UltAfter(1.6f, () =>
            {
                _ultKind = null;
                for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
                _ultFx.Clear();
                var boom = PlayFx("grburst", _sealAt, Meters(5.2f), loop: false);
                boom?.SetFrameSeconds(0.05f);
                UltAoe(me, _sealAt, 2.6f, 5f, boom == null ? "burst" : null, 2.4f);
                _pfx?.Burst(ParticleFxKind.Shard, ParticleElement.Dust, _sealAt, 18, 380f, 28f, 0.7f, 360f);
                Shake(12f);
                HitStop(0.12f);
            });
        }

        private void TickSealing(Unit me, float dt)
        {
            float pull = Meters(3.5f), step = Meters(6f) * dt;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (!Targetable(e) || e.IsBoss) continue;
                var d = _sealAt - e.Position;
                float dist = d.magnitude;
                if (dist > pull || dist < Meters(0.4f)) continue;
                e.Position = ClampedInField(e, e.Position + d / dist * Mathf.Min(step, dist - Meters(0.4f)));
                e.ApplyRoot(0.3f);
                // 시안 가운데 컷 — 끌려 드는 적에게서 결계 쪽으로 금빛 줄기
                if (Random.value < dt * 6f)
                    _pfx?.Spray(ParticleFxKind.Streak, e.Position, d / dist, 6f, 1, dist * 2.5f, dist * 3f, new Vector2(36f, 5f), 0.3f,
                                new Color(1f, 0.82f, 0.35f, 1f));
            }
        }

        private const float VenomPoolMeters = 3.4f;

        /// <summary>늪 안의 적은 늘 중독 — 독이 남아 있어도 시간을 다시 채운다.</summary>
        private void PoisonInPool(Vector2 at)
        {
            float r = Meters(VenomPoolMeters) * _buffs.AoeMul;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying) continue;
                if ((e.Position - at).sqrMagnitude > r * r) continue;
                e.ApplyPoison(PassiveAilmentSeconds);
            }
        }

        // 샐러맨더 — 독 늪: 앞에 독 늪(감속 · 지속 피해 · 100% 중독) 4초 → 늪 전체가 터진다
        private void UltSalamander(Unit me)
        {
            var near = NearestEnemy(me.Position);
            var at = ClampedInField(me, near != null ? near.Position : me.Position + me.Facing * Meters(2.5f));
            me.PlayAttack();
            // R 시안(ult_salamander) — 입에서 독 줄기 → 넓은 독 늪(거품) → 늪 곳곳에서 독이 솟구친다
            var spit = FxFrames("smspit");
            if (spit != null)
            {
                var beam = FreeImpact(150f);
                beam?.PlayBeam(me.MuzzlePosition, at, spit, 150f);
                beam?.SetLoop(true);
                beam?.SetFrameSeconds(0.05f);
                beam?.SetLife(0.5f, 0.2f);
            }
            else PlayFx("venom", at, Meters(2f), loop: false);
            UltAfter(0.35f, () =>
            {
                // 범위를 키웠다(2.6 → 3.4 m) · 늪 안에 있으면 100% 중독(PD 2026-10-21)
                var field = SpawnField(at, Meters(VenomPoolMeters), 4f, FieldEffect.Slow, UltDamage(me, 0.45f), fromPlayer: true);
                for (int k = 0; k <= 13; k++) UltAfter(k * 0.3f, () => PoisonInPool(at));
                var pool = PlayFloorFx("smpool", at, Meters(VenomPoolMeters * 2.3f), loop: true);
                if (pool != null)
                {
                    pool.SetFrameSeconds(0.1f);
                    pool.SetLife(4.1f, 0.4f);
                    // 감속 장판 제 그림(파란 소용돌이)이 독 늪 둘레에 겹쳐 보였다(녹화 r_sm1) — 판정만 쓰고 그림은 끈다
                    if (field != null) field.SetSprite(null);
                }
                else UltLoop("vncloud", at, Meters(5f));
                _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Venom, at, 8, 140f, 50f, 0.6f, 0f);
            });
            UltAfter(4.2f, () =>
            {
                for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
                _ultFx.Clear();
                UltAoe(me, at, VenomPoolMeters, 4f, FxFrames("smgeyser") == null ? "goo_burst" : null, 2.3f);
                for (int k = 0; k < 9; k++)
                {
                    var p = at + Random.insideUnitCircle * Meters(VenomPoolMeters - 0.4f);
                    UltAfter(k * 0.06f, () => PlayFx("smgeyser", p + new Vector2(0f, SsrBody * 0.7f), SsrBody * 2.2f, loop: false)
                        ?.SetFrameSeconds(0.05f));
                }
                Shake(8f);
            });
        }

        // 설녀 — 얼음 진: 반경 5m 적을 얼음 기둥에 가두고(2초) 한꺼번에 깬다
        private void UltSnowwoman(Unit me)
        {
            var list = UltTargets(me.Position, Meters(5f));
            if (list.Count == 0) { UltFallback(me); return; }
            // R 시안(ult_snowwoman) — 바닥에 서리 마법진 → 적마다 얼음 결정 감옥 → 한꺼번에 깨진다
            var cryst = new List<Impact>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                var ring = PlayFloorFx("swcircle", e.Position, SsrBody * 2f, loop: true);
                if (ring != null) { ring.SetFrameSeconds(0.08f); ring.SetLife(0.55f, 0.25f); }
                _pfx?.Spray(ParticleFxKind.Star4, e.Position, Vector2.up, 40f, 3, 40f, 100f, new Vector2(10f, 10f), 0.6f,
                            new Color(0.75f, 0.95f, 1f, 1f), element: ParticleElement.Ice);
            }
            if (FxFrames("swcircle") == null) PlayFx("freeze", me.Position, Meters(5f), loop: false);
            UltAfter(0.35f, () =>
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == null || !e.IsAlive) continue;
                    e.ApplyFreeze(2f);
                    var c = PlayFx("swcrystal", e.Position + new Vector2(0f, SsrBody * 0.15f), SsrBody * 1.6f, loop: true);
                    if (c != null) { c.SetFrameSeconds(0.12f); c.SetLife(1.7f, 0.1f); cryst.Add(c); }
                    else PlayFx("iceblock", e.Position, 120f, loop: false);
                }
            });
            UltAfter(2.05f, () =>
            {
                for (int i = 0; i < cryst.Count; i++) cryst[i]?.Stop();
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null && list[i].IsAlive)
                    {
                        var at = list[i].Position;
                        Shatter(me, at, Meters(1.2f), UltDamage(me, 3.5f), 0);
                        PlayFx("swshatter", at, SsrBody * 2.4f, loop: false)?.SetFrameSeconds(0.05f);
                        _pfx?.Burst(ParticleFxKind.Star4, ParticleElement.Ice, at, 8, 260f, 14f, 0.7f, 0f);
                    }
                Shake(9f);
                HitStop(0.08f);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  버티고 돌려준다
        // ═══════════════════════════════════════════════════════════

        // 아마존 정예 — 역린: 3초 버티며 날아온 탄 · 붙은 적을 몸에 모으고, 모은 만큼 둘레로 되돌려 터뜨린다
        private int _wrathStacks;
        private float _wrathTick;

        private void UltAmazonElite(Unit me)
        {
            _ultKind = "amazon_elite";
            _ultT = 0f;
            _wrathStacks = 0;
            _wrathTick = 0f;
            UltInvuln(3f);
            SsrWrathOpen(me);
            UltAfter(3f, () =>
            {
                _ultKind = null;
                for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
                _ultFx.Clear();
                float mul = 3f + Mathf.Min(_wrathStacks, 12) * 0.35f;   // 최대 ×7.2
                me.PlayAttack();
                SsrWrathBurst(me, mul);
                Shake(14f);
                HitStop(0.14f);
            });
        }

        private void TickWrath(Unit me, float dt)
        {
            if (_ultFx.Count > 0)
            {
                _ultFx[0]?.MoveTo(me.Position);
                _ultFx[0]?.SetSize(SsrBody * 2f + Mathf.Min(_wrathStacks, 12) * 8f);   // 모을수록 붉은 기운이 커진다
            }
            for (int i = 0; i < _shots.Count; i++)
            {
                var s = _shots[i];
                if (!s.IsActive || s.FromPlayer) continue;
                if (Vector2.Distance(s.Position, me.Position) > Meters(1.2f)) continue;
                // 시안 「피격 빛 조각이 몸으로 흡수」 — 붉은 빛줄이 몸으로 빨려 든다
                _pfx?.Spray(ParticleFxKind.Streak, s.Position, (me.Position - s.Position).normalized, 6f, 3,
                            Vector2.Distance(s.Position, me.Position) * 4f, Vector2.Distance(s.Position, me.Position) * 5f,
                            new Vector2(30f, 5f), 0.2f, new Color(1f, 0.25f, 0.35f, 1f));
                s.Despawn();
                _wrathStacks++;
            }
            _wrathTick -= dt;
            if (_wrathTick > 0f) return;
            _wrathTick = 0.5f;
            _wrathStacks += EnemiesInRange(me.Position, Meters(1.8f)).Count;
            // 둘레 적에게서도 붉은 빛 조각이 몸으로 빨려 든다 — 시안 가운데 컷(「흡수」가 읽혀야 마지막 폭발에 이유가 생긴다)
            var near = EnemiesInRange(me.Position, Meters(3.5f));
            for (int i = 0; i < near.Count && i < 8; i++)
            {
                var from = near[i].Position;
                float d = Vector2.Distance(from, me.Position);
                if (d < 1f) continue;
                _pfx?.Spray(ParticleFxKind.Streak, from, (me.Position - from) / d, 8f, 2, d * 3.2f, d * 4f,
                            new Vector2(26f, 5f), 0.25f, new Color(1f, 0.25f, 0.35f, 1f));
            }
        }
    }
}
