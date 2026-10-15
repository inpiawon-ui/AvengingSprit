using System;
using System.Collections.Generic;
using UnityEngine;

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
            _ultQueue.Clear();
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
            for (int i = _ultQueue.Count - 1; i >= 0; i--)
            {
                var q = _ultQueue[i];
                q.Left -= dt;
                if (q.Left > 0f) { _ultQueue[i] = q; continue; }
                _ultQueue.RemoveAt(i);
                if (_host != null) q.Run();
            }
            TickUltDash(dt);
            if (_ultKind == null) return;
            var me = _host;
            if (me == null || !me.IsAlive) { StopUltimates(); return; }
            _ultT += dt;
            switch (_ultKind)
            {
                case "guru": TickSealing(me, dt); break;
                case "dragon_blue": TickThunderOrbs(me, dt); break;
                case "commando_mg": TickDeflectWall(me, dt); break;
                case "amazon_elite": TickWrath(me, dt); break;
                case "dragoon": TickDragonCharge(me, dt); break;
                case "gangster": TickContract(me); break;
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
            _contractLock = UltLoop("critlock", _contractTarget.Position, 150f);
            _contractLock?.SetSnap(2.2f, 1.4f);   // 크게 시작해 1.5초에 걸쳐 조여든다
            me.SetFacing(_contractTarget.Position - me.Position);
            UltAfter(1.5f, () =>
            {
                _ultKind = null;
                _contractLock?.Stop();
                var t = _contractTarget;
                if (t == null || !t.IsAlive) return;
                me.SetFacing(t.Position - me.Position);
                me.PlayAttack();
                UltBeam("wpbeam", me.MuzzlePosition, t.Position, 70f);
                UltHit(me, t, 14f, capBoss: true);
                CritBurst(t.Position);
                PlayFx("crit", t.Position, 220f, loop: false);
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

        // 호퍼 — 킬 포인트: 앞 적들에게 칼날 → 3초 묶음 → 차례로 치명 확정
        private void UltHopper(Unit me)
        {
            var list = UltTargets(me.Position, Meters(7f));
            if (list.Count == 0) { UltFallback(me); return; }
            if (list.Count > 8) list.RemoveRange(8, list.Count - 8);
            me.PlayAttack();
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                UltBeam("wptracer", me.MuzzlePosition, e.Position, 26f, 0.04f);
                e.ApplyRoot(3f);
                PlayFx("critlock", e.Position, 90f, loop: false);
            }
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                UltAfter(0.6f + i * 0.12f, () =>
                {
                    if (e == null || !e.IsAlive) return;
                    UltHit(me, e, 3.5f * CritMultiplier, capBoss: true);
                    CritBurst(e.Position);
                    HitStop(0.03f);
                });
            }
        }

        // 닌자(사슬) — 사슬 돌격 연격: 사슬을 걸고 **내가 날아가** 연타 → 내리찍기(보스를 끌어오지 않는다)
        private void UltNinjaChain(Unit me)
        {
            var t = UltStrongest(me.Position);
            if (t == null) { UltFallback(me); return; }
            UltBeam("chain", me.Position, t.Position, 40f, 0.06f);
            var to = t.Position - (t.Position - me.Position).normalized * Meters(0.8f);
            UltDashTo(me, to, 0.25f, new Color(1f, 0.6f, 0.85f, 1f), () =>
            {
                for (int i = 0; i < 5; i++)
                {
                    int n = i;
                    UltAfter(n * 0.1f, () =>
                    {
                        if (t == null || !t.IsAlive) return;
                        me.PlayAttack();
                        UltHit(me, t, 1.6f, capBoss: true);
                        PlayFx(n % 2 == 0 ? "rpslash" : "hxslash", t.Position, 130f, loop: false);
                        HitStop(0.025f);
                    });
                }
                UltAfter(0.6f, () =>
                {
                    var at = t != null && t.IsAlive ? t.Position : me.Position;
                    UltAoe(me, at, 2f, 4f, "slam");
                    Shake(10f);
                    HitStop(0.1f);
                });
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  불러서 같이 싸운다
        // ═══════════════════════════════════════════════════════════

        // 사신 — 망자 군단: 해골 다섯이 일어나 싸우고, 4초 뒤 혼불이 적마다 날아가 터진다
        private void UltDeath(Unit me)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = (-60f + i * 30f) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                var at = ClampedInField(me, me.Position + dir * Meters(1.4f));
                SummonSkull(at);
                PlayFx("soulripple", at, Meters(1.2f), loop: false);
            }
            UltAfter(4f, () =>
            {
                var list = UltTargets(me.Position, Meters(12f));
                for (int i = 0; i < list.Count && i < 8; i++)
                {
                    var e = list[i];
                    _pfx?.Dart(me.Position, e.Position, ParticleElement.Ice, 0.3f, 4);
                    UltAfter(0.3f, () =>
                    {
                        if (e == null || !e.IsAlive) return;
                        UltAoe(me, e.Position, 1.3f, 3f, "mdpop", 2.6f);
                    });
                }
                Shake(8f);
            });
        }

        // 로봇 — 포탑 군단: 포탑 셋이 둘레에 내려앉아 쏘고, 2.5초 뒤 미사일 8발 일제 사격
        private void UltRobot(Unit me)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + 90f) * Mathf.Deg2Rad;
                var at = ClampedInField(me, me.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * Meters(1.6f));
                SpawnDeployable(at, ghostly: false, seconds: 8f, range: Meters(8f), damage: UltDamage(me, 0.8f), fireInterval: 0.35f);
                PlayFx("turret", at, 90f, loop: false);
            }
            UltAfter(2.5f, () =>
            {
                var list = UltTargets(me.Position, Meters(12f));
                if (list.Count == 0) return;
                for (int i = 0; i < 8; i++)
                {
                    var t = list[i % list.Count];
                    float a = (-70f + i * 20f) * Mathf.Deg2Rad;
                    var from = me.Position + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * Meters(0.8f);
                    FireSkillMissile(me, from, t.Position, t, UltDamage(me, 2.2f), Meters(1.5f));
                }
                Shake(6f);
            });
        }

        // 닌자 — 그림자 삼분신: 분신 셋이 6초 동안 같이 표창을 던진다
        private void UltNinja(Unit me)
        {
            PlayFx("smoke", me.Position, Meters(2.5f), loop: false);
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + 90f) * Mathf.Deg2Rad;
                var at = ClampedInField(me, me.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Meters(1.4f));
                SpawnClone(at, 6f, UltDamage(me, 0.8f), 0.3f);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  몸이 날아다닌다
        // ═══════════════════════════════════════════════════════════

        // 호퍼(기관단총) — 돌진 난사: 가장 먼 적까지 돌파하며 지나는 길의 적을 쏘고, 도착하면 무적
        private void UltHopperSmg(Unit me)
        {
            var list = UltTargets(me.Position, Meters(14f));
            if (list.Count == 0) { UltFallback(me); return; }
            var far = list[list.Count - 1];
            var from = me.Position;
            var to = far.Position - (far.Position - from).normalized * Meters(1f);
            var dir = (to - from).normalized;
            var side = new Vector2(-dir.y, dir.x);
            // 지나는 길 1.2m 안의 적은 지나갈 때 맞는다
            var path = new List<Unit>();
            float len = Vector2.Distance(from, to);
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i].Position - from;
                float along = Vector2.Dot(d, dir);
                if (along < 0f || along > len + Meters(1f)) continue;
                if (Mathf.Abs(Vector2.Dot(d, side)) > Meters(1.2f)) continue;
                path.Add(list[i]);
            }
            for (int i = 0; i < 6; i++)
            {
                UltAfter(i * 0.06f, () =>
                {
                    FireFan(me, me.Position + side * 600f, 1, 0f, UltDamage(me, 0.6f), fromPlayer: true);
                    FireFan(me, me.Position - side * 600f, 1, 0f, UltDamage(me, 0.6f), fromPlayer: true);
                });
            }
            UltDashTo(me, to, 0.35f, new Color(1f, 0.85f, 0.55f, 1f), () =>
            {
                for (int i = 0; i < path.Count; i++)
                {
                    UltHit(me, path[i], 3f);
                    PlayFx("hit", path[i].Position, 90f, loop: false);
                }
                UltInvuln(1.2f);
                PlayFx("guard", me.Position, Meters(1.4f), loop: false);
                Shake(7f);
            });
        }

        // 흡혈귀 — 피의 폭주: 체력 10% 를 내주고 5초 폭주(가속 · 흡혈 확정 · 회복 2배), 끝에 피의 폭발
        private void UltVampire(Unit me)
        {
            int pay = Mathf.Max(1, me.HpMax / 10);
            if (me.Hp > pay + 1) me.TakeDamage(pay);
            GrantHaste(70f, 5f);
            _drainSeconds = Mathf.Max(_drainSeconds, 5f);
            _drainMul = 2.5f;
            UltLoop("rpaura", me.Position, 150f);
            PlayFx("drain", me.Position, Meters(2f), loop: false);
            UltAfter(5f, () =>
            {
                for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
                _ultFx.Clear();
                UltAoe(me, me.Position, 2.5f, 3.5f, "burst");
                me.Heal(Mathf.Max(1, me.HpMax * 15 / 100));
                Shake(8f);
            });
        }

        // 드라군 — 용 돌격: 1.2초 동안 앞으로 돌진하며 불길을 남기고, 끝에 화염구
        private Vector2 _dcDir;
        private float _dcDrop;

        private void UltDragoon(Unit me)
        {
            var near = NearestEnemy(me.Position);
            _dcDir = near != null ? (near.Position - me.Position).normalized : me.Facing;
            if (_dcDir.sqrMagnitude < 0.01f) _dcDir = Vector2.up;
            _ultKind = "dragoon";
            _ultT = 0f;
            _dcDrop = 0f;
            UltLoop("breath_fire", me.Position + _dcDir * 80f, 180f);
            UltDashTo(me, me.Position + _dcDir * Meters(7f), 1.2f, new Color(1f, 0.6f, 0.4f, 1f), () =>
            {
                _ultKind = null;
                for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
                _ultFx.Clear();
                UltAoe(me, me.Position + _dcDir * Meters(1f), 2.5f, 4f, "burst");
                Shake(12f);
                HitStop(0.1f);
            });
        }

        private void TickDragonCharge(Unit me, float dt)
        {
            if (_ultFx.Count > 0) _ultFx[0]?.MoveTo(me.Position + _dcDir * 80f);
            _dcDrop -= dt;
            if (_dcDrop > 0f) return;
            _dcDrop = 0.15f;
            SpawnField(me.Position, Meters(1f), 3f, FieldEffect.Burn, UltDamage(me, 0.3f), fromPlayer: true);
            UltAoe(me, me.Position + _dcDir * Meters(0.8f), 1.3f, 0.9f, null);
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
            UltLoop("gddome", _sealAt, Meters(5f));
            _wardSeconds = Mathf.Max(_wardSeconds, 2f);
            _wardReduce = 0.5f;
            UltAfter(1.6f, () =>
            {
                _ultKind = null;
                for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
                _ultFx.Clear();
                UltAoe(me, _sealAt, 2.6f, 5f, "burst", 2.4f);
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
            }
        }

        // 샐러맨더 — 독 늪: 앞에 독 늪(감속 · 지속 피해) 4초 → 늪 전체가 터진다
        private void UltSalamander(Unit me)
        {
            var near = NearestEnemy(me.Position);
            var at = ClampedInField(me, near != null ? near.Position : me.Position + me.Facing * Meters(2.5f));
            me.PlayAttack();
            PlayFx("venom", at, Meters(2f), loop: false);
            SpawnField(at, Meters(2.6f), 4f, FieldEffect.Slow, UltDamage(me, 0.45f), fromPlayer: true);
            UltLoop("vncloud", at, Meters(5f));
            UltAfter(4f, () =>
            {
                for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
                _ultFx.Clear();
                UltAoe(me, at, 2.6f, 4f, "goo_burst", 2.3f);
                Shake(8f);
            });
        }

        // 설녀 — 얼음 진: 반경 5m 적을 얼음 기둥에 가두고(2초) 한꺼번에 깬다
        private void UltSnowwoman(Unit me)
        {
            var list = UltTargets(me.Position, Meters(5f));
            if (list.Count == 0) { UltFallback(me); return; }
            PlayFx("freeze", me.Position, Meters(5f), loop: false);
            for (int i = 0; i < list.Count; i++)
            {
                list[i].ApplyFreeze(2f);
                PlayFx("iceblock", list[i].Position, 120f, loop: false);
            }
            UltAfter(2f, () =>
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null && list[i].IsAlive)
                        Shatter(me, list[i].Position, Meters(1.2f), UltDamage(me, 3.5f), 0);
                Shake(9f);
                HitStop(0.08f);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  쏟아붓는다
        // ═══════════════════════════════════════════════════════════

        // 코만도(미사일) — 자폭 미사일 떼: 6기가 각자 다른 적을 쫓아 터진다
        private void UltMissile(Unit me)
        {
            var list = UltTargets(me.Position, Meters(14f));
            if (list.Count == 0) { UltFallback(me); return; }
            for (int i = 0; i < 6; i++)
            {
                var t = list[i % list.Count];
                float a = (-75f + i * 30f) * Mathf.Deg2Rad;
                var from = me.Position + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * Meters(0.7f);
                FireSkillMissile(me, from, from + (from - me.Position).normalized * Meters(3f), t,
                                 UltDamage(me, 3.2f), Meters(1.6f));
                PlayFx("missile_trail", from, 32f, loop: false);
            }
        }

        // 코만도(레이저) — 충전 레이저: 0.5초 차지 → 일직선 관통 빔, 쏜 몸은 뒤로 밀린다
        private void UltLaser(Unit me)
        {
            var t = NearestEnemy(me.Position);
            var dir = t != null ? (t.Position - me.Position).normalized : me.Facing;
            me.SetFacing(dir);
            PlayFx("wfgather", me.MuzzlePosition, 140f, loop: false);
            UltAfter(0.5f, () =>
            {
                var from = me.MuzzlePosition;
                var to = from + dir * Meters(14f);
                UltBeam("laser_wide", from, to, 110f, 0.05f);
                var side = new Vector2(-dir.y, dir.x);
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (!Targetable(e)) continue;
                    var d = e.Position - from;
                    float along = Vector2.Dot(d, dir);
                    if (along < 0f || along > Meters(14f) || Mathf.Abs(Vector2.Dot(d, side)) > Meters(0.9f)) continue;
                    UltHit(me, e, 6f);
                    PlayFx("lzhit", e.Position, 100f, loop: false);
                }
                me.Position = ClampedInField(me, me.Position - dir * Meters(1f));
                Shake(10f);
                HitStop(0.1f);
            });
        }

        // 화이트 위저드 — 빛 구슬 일제 발사: 몸 둘레에서 8발이 각자 적을 쫓는다
        private void UltWizard(Unit me)
        {
            var list = UltTargets(me.Position, Meters(14f));
            if (list.Count == 0) { UltFallback(me); return; }
            PlayFx("holy_beam", me.MuzzlePosition, 140f, loop: false);
            for (int i = 0; i < 8; i++)
            {
                var t = list[i % list.Count];
                float a = i * 45f * Mathf.Deg2Rad;
                var from = me.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Meters(0.8f);
                var shot = RentShot();
                if (shot == null) break;
                shot.SetSprite(ShotSpriteOf(me), ShotKindOf(me), LoopsFrames(ShotKindOf(me)));
                shot.Fire(from, from + (from - me.Position).normalized * 300f, _config.ShotSpeedPlayer, UltDamage(me, 2.2f),
                          true, t, _config.ShotSize * 1.4f, ShotPlayerColor, _config.ShotLifeSeconds * 1.5f, pierce: true);
                shot.SetHoming(2.2f);
            }
        }

        // 청룡 — 번개 구슬: 5초 동안 구슬 셋이 몸 둘레를 돌며 번개, 끝에 낙뢰
        private float _orbZap;

        private void UltDragonBlue(Unit me)
        {
            _ultKind = "dragon_blue";
            _ultT = 0f;
            _orbZap = 0f;
            for (int i = 0; i < 3; i++) UltLoop("vmorb", me.Position, 60f);
            UltAfter(5f, () =>
            {
                _ultKind = null;
                for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
                _ultFx.Clear();
                var t = UltStrongest(me.Position) ?? NearestEnemy(me.Position);
                if (t == null) return;
                UltBeam("dsbolt", t.Position + new Vector2(0f, Meters(6f)), t.Position, 120f);
                UltAoe(me, t.Position, 2.4f, 4f, "lzhit", 2f);
                Shake(12f);
                HitStop(0.1f);
            });
        }

        private Vector2 OrbPos(Unit me, int i)
        {
            float a = _ultT * 3.2f + i * Mathf.PI * 2f / 3f;
            return me.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * Meters(1.5f) + new Vector2(0f, 20f);
        }

        private void TickThunderOrbs(Unit me, float dt)
        {
            for (int i = 0; i < _ultFx.Count && i < 3; i++) _ultFx[i]?.MoveTo(OrbPos(me, i));
            _orbZap -= dt;
            if (_orbZap > 0f) return;
            _orbZap = 0.35f;
            for (int i = 0; i < 3; i++)
            {
                var at = OrbPos(me, i);
                var t = NearestEnemy(at, Meters(4.5f));
                if (t == null) continue;
                UltBeam("dsbolt", at, t.Position, 50f, 0.04f);
                UltHit(me, t, 0.9f);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  버티고 돌려준다
        // ═══════════════════════════════════════════════════════════

        // 코만도(기관총) — 디플렉트 월: 앞에 방벽이 4초 동안 밀고 나가며 탄을 막고 적을 민다, 끝에 깨진다
        private Vector2 _wallAt, _wallDir;
        private float _wallTick;

        private void UltCommandoMg(Unit me)
        {
            var near = NearestEnemy(me.Position);
            _wallDir = near != null ? (near.Position - me.Position).normalized : me.Facing;
            if (_wallDir.sqrMagnitude < 0.01f) _wallDir = Vector2.up;
            _wallAt = me.Position + _wallDir * Meters(1.4f);
            _ultKind = "commando_mg";
            _ultT = 0f;
            _wallTick = 0f;
            UltLoop("cmwall", _wallAt, 170f);
            UltAfter(4f, () =>
            {
                _ultKind = null;
                for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
                _ultFx.Clear();
                UltAoe(me, _wallAt, 2f, 3f, "shatter", 2.2f, knockMeters: 2f);
                Shake(9f);
            });
        }

        private void TickDeflectWall(Unit me, float dt)
        {
            _wallAt = ClampedInField(me, _wallAt + _wallDir * Meters(1.4f) * dt);
            if (_ultFx.Count > 0) _ultFx[0]?.MoveTo(_wallAt);
            float r = Meters(1.3f);
            for (int i = 0; i < _shots.Count; i++)
            {
                var s = _shots[i];
                if (!s.IsActive || s.FromPlayer) continue;
                if (Vector2.Distance(s.Position, _wallAt) > r) continue;
                PlayFx("hit", s.Position, 50f, loop: false);
                s.Despawn();
            }
            _wallTick -= dt;
            bool hit = _wallTick <= 0f;
            if (hit) _wallTick = 0.4f;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (!Targetable(e) || e.IsBoss) continue;
                if (Vector2.Distance(e.Position, _wallAt) > r) continue;
                e.Position = ClampedInField(e, e.Position + _wallDir * Meters(1.4f) * dt * 1.2f);
                if (hit) UltHit(me, e, 0.5f);
            }
        }

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
            UltLoop("rpaura", me.Position, 140f);
            UltAfter(3f, () =>
            {
                _ultKind = null;
                for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.Stop();
                _ultFx.Clear();
                float mul = 3f + Mathf.Min(_wrathStacks, 12) * 0.35f;   // 최대 ×7.2
                me.PlayAttack();
                UltAoe(me, me.Position, 3f, mul, "slam", 2.2f, knockMeters: 2f);
                PlayFx("burst", me.Position, Meters(5f), loop: false);
                Shake(14f);
                HitStop(0.14f);
            });
        }

        private void TickWrath(Unit me, float dt)
        {
            if (_ultFx.Count > 0)
            {
                _ultFx[0]?.MoveTo(me.Position);
                _ultFx[0]?.SetSize(140f + Mathf.Min(_wrathStacks, 12) * 10f);   // 모을수록 붉은 기운이 커진다
            }
            for (int i = 0; i < _shots.Count; i++)
            {
                var s = _shots[i];
                if (!s.IsActive || s.FromPlayer) continue;
                if (Vector2.Distance(s.Position, me.Position) > Meters(1.2f)) continue;
                _pfx?.Dart(s.Position, me.Position, ParticleElement.Fire, 0.15f, 2);
                s.Despawn();
                _wrathStacks++;
            }
            _wrathTick -= dt;
            if (_wrathTick > 0f) return;
            _wrathTick = 0.5f;
            _wrathStacks += EnemiesInRange(me.Position, Meters(1.8f)).Count;
        }
    }
}
