using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// SR 필살기 시안 다듬기(PD 2026-10-15 「SR 등급 필살기도 시안 퀄로」).
    /// 시안: `Projects/AVSR/_exchange/doc_skill_concept/img/ult_{호스트}.jpg`(세 컷 — 시작 · 전개 · 마무리)
    /// 부품: `_exchange/in/part_{hs,lz,nj,nc,rb,cm}_*.png` → `fx_{hslaunch, hstrail, hsshield, lzcharge, lzbeam, lzblast,
    ///        njsmoke, njslash, ncslam, nchook, rbdrop, cmlock}` · 폭발은 드라군 화염구(`dgblast`)를 같이 쓴다.
    /// 부품이 없으면 예전 그림으로 떨어진다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ═══════════════════════════════════════════════════════════
        //  호퍼(기관단총) — 돌진 난사: 도약 → 잔상 · 자국을 끌며 가장 먼 적까지 돌파, 양옆 난사 → 도착하면 보호막
        // ═══════════════════════════════════════════════════════════

        private Impact _hsTrail;
        private Vector2 _hsFrom;
        private static readonly Color HsGold = new(1f, 0.82f, 0.45f, 1f);

        private void SrHopperSmg(Unit me)
        {
            var list = UltTargets(me.Position, Meters(14f));
            if (list.Count == 0) { UltFallback(me); return; }
            var far = list[list.Count - 1];
            var from = me.Position;
            var to = far.Position - (far.Position - from).normalized * Meters(1f);
            var dir = (to - from).normalized;
            var side = new Vector2(-dir.y, dir.x);
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

            // 시안 첫 컷 — 발밑에서 빛기둥 · 흙먼지가 솟으며 도약
            PlayFx("hslaunch", from + new Vector2(0f, SsrBody * 0.35f), SsrBody * 2.6f, loop: false)?.SetFrameSeconds(0.04f);
            _pfx?.Burst(ParticleFxKind.Shard, ParticleElement.Dust, from, 10, 240f, 22f, 0.5f, 360f);
            Shake(5f);
            UltInvuln(1.6f);

            UltAfter(0.16f, () =>
            {
                _hsFrom = me.Position;
                var frames = FxFrames("hstrail");
                if (frames != null)
                {
                    _hsTrail = FreeImpact(96f);
                    _hsTrail?.PlayBeam(_hsFrom, _hsFrom + dir * 8f, frames, 96f);
                    _hsTrail?.SetLoop(true);
                    _hsTrail?.SetFrameSeconds(0.05f);
                }
                // 시안 가운데 컷 — 돌진하며 양옆으로 쏟아붓는다(탄 · 총구 불 · 불티)
                for (int i = 0; i < 10; i++)
                {
                    UltAfter(i * 0.035f, () =>
                    {
                        FireFan(me, me.Position + side * 600f, 1, 0f, UltDamage(me, 0.6f), fromPlayer: true);
                        FireFan(me, me.Position - side * 600f, 1, 0f, UltDamage(me, 0.6f), fromPlayer: true);
                        _pfx?.Spray(ParticleFxKind.Streak, me.Position, side, 10f, 1, 700f, 900f, new Vector2(46f, 5f), 0.18f, HsGold);
                        _pfx?.Spray(ParticleFxKind.Streak, me.Position, -side, 10f, 1, 700f, 900f, new Vector2(46f, 5f), 0.18f, HsGold);
                    });
                }
                _ultKind = "hopper_smg";
                UltDashTo(me, to, 0.35f, new Color(1f, 0.85f, 0.55f, 1f), () =>
                {
                    _ultKind = null;
                    if (_hsTrail != null) { _hsTrail.SetLife(0.35f, 0.3f); _hsTrail = null; }
                    for (int i = 0; i < path.Count; i++)
                    {
                        UltHit(me, path[i], 3f);
                        PlayFx("hit", path[i].Position, 90f, loop: false);
                    }
                    // 시안 마지막 컷 — 몸을 감싼 하늘색 보호막 · 둘레로 튀는 불꽃
                    UltInvuln(1.2f);
                    var shield = UltLoop("hsshield", me.Position, SsrBody * 1.6f) ?? PlayFx("guard", me.Position, Meters(1.4f), loop: false);
                    shield?.SetFrameSeconds(0.07f);
                    shield?.SetLife(1.2f, 0.3f);
                    _pfx?.Burst(ParticleFxKind.Spark, ParticleElement.Fire, me.Position, 18, 360f, 12f, 0.5f, 0f);
                    Shake(7f);
                });
            });
        }

        private void TickSrHopperSmg(Unit me)
        {
            _hsTrail?.Restretch(_hsFrom, me.Position);
            for (int i = 0; i < _ultFx.Count; i++) _ultFx[i]?.MoveTo(me.Position);
        }

        // ═══════════════════════════════════════════════════════════
        //  코만도(레이저) — 충전 레이저: 총구에 하늘색 기운이 모였다가 굵은 관통 빔, 지나간 자리마다 전기 폭발, 반동
        // ═══════════════════════════════════════════════════════════

        private const float LzBeamDraw = 260f;   // 그림 칸 높이 — 빛줄이 칸의 약 1/4 이라 실제 굵기는 약 65 px(시안 실측)

        private void SrLaser(Unit me)
        {
            var t = NearestEnemy(me.Position);
            var dir = t != null ? (t.Position - me.Position).normalized : me.Facing;
            me.SetFacing(dir);
            var charge = PlayFx("lzcharge", me.MuzzlePosition, SsrBody * 1.6f, loop: false) ?? PlayFx("wfgather", me.MuzzlePosition, 140f, loop: false);
            charge?.SetFrameSeconds(0.085f);
            _pfx?.Spray(ParticleFxKind.Streak, me.MuzzlePosition + dir * 60f, -dir, 70f, 14, 120f, 260f, new Vector2(30f, 4f), 0.4f,
                        new Color(0.5f, 0.9f, 1f, 1f));
            UltInvuln(1.2f);
            UltAfter(0.5f, () =>
            {
                var from = me.MuzzlePosition;
                float reach = Meters(14f);
                var to = from + dir * reach;
                var frames = FxFrames("lzbeam");
                if (frames != null)
                {
                    var beam = FreeImpact(LzBeamDraw);
                    beam?.PlayBeam(from, to, frames, LzBeamDraw);
                    beam?.SetLoop(true);
                    beam?.SetFrameSeconds(0.04f);
                    beam?.SetLife(0.55f, 0.2f);
                }
                else UltBeam("laser_wide", from, to, 110f, 0.05f);
                var side = new Vector2(-dir.y, dir.x);
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (!Targetable(e)) continue;
                    var d = e.Position - from;
                    float along = Vector2.Dot(d, dir);
                    if (along < 0f || along > reach || Mathf.Abs(Vector2.Dot(d, side)) > Meters(0.9f)) continue;
                    UltHit(me, e, 6f);
                    // 시안 마지막 컷 — 빔이 지나간 적마다 차례로 전기 폭발(가까운 쪽부터)
                    var at = e.Position;
                    UltAfter(0.05f + along / reach * 0.3f, () =>
                    {
                        var boom = PlayFx("lzblast", at, SsrBody * 2.2f, loop: false) ?? PlayFx("lzhit", at, 100f, loop: false);
                        boom?.SetFrameSeconds(0.05f);
                    });
                }
                // 반동 — 쏜 몸이 뒤로 밀리며 발밑 먼지
                me.Position = ClampedInField(me, me.Position - dir * Meters(1f));
                _pfx?.Burst(ParticleFxKind.Smoke, ParticleElement.Dust, me.Position, 8, 120f, 40f, 0.6f, 0f);
                Shake(10f);
                HitStop(0.1f);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  닌자 — 그림자 삼분신: 보라 연막 속에서 보라빛 분신 셋이 서서 같이 표창을 던지고, 마지막에 분신이 뛰어들어 벤다
        // ═══════════════════════════════════════════════════════════

        private static readonly Color NjCloneTint = new(0.78f, 0.55f, 1f, 0.82f);
        private const float NjSeconds = 6f;

        private void SrNinja(Unit me)
        {
            PlayFx("njsmoke", me.Position, SsrBody * 4.2f, loop: false)?.SetFrameSeconds(0.08f);
            // ⚠ 분신 시계를 같이 켠다 — 안 켜면 `TickClones` 가 다음 틱에 분신을 거둔다(녹화 sr_nj1 — 분신이 안 보였다)
            _cloneSeconds = Mathf.Max(_cloneSeconds, NjSeconds);
            // 연막 그림은 더하기라 밝은 바닥에서 옅다(녹화 sr_nj2) — 보라 빛을 겹친다.
            //   연기 입자(Smoke · Curse)는 회색 흙먼지 그림이라 화면을 잿빛으로 덮었다(녹화 sr_nj3) — 쓰지 않는다
            _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Curse, me.Position, 6, 90f, 80f, 0.6f, 0f);
            var clones = new List<Vector2>(3);
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + 90f) * Mathf.Deg2Rad;
                var at = ClampedInField(me, me.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Meters(1.4f));
                SpawnClone(at, NjSeconds, UltDamage(me, 0.8f), 0.3f);
                // 시안 — 분신은 유령이 아니라 **닌자 몸의 보라빛 그림자**다. 던지는 것도 표창
                if (_clones.Count > 0)
                {
                    var c = _clones[_clones.Count - 1];
                    var body = me.BodySprite;
                    if (body != null) c.SetSprite(body);
                    c.SetSize(me.GetComponent<RectTransform>().sizeDelta);
                    c.SetTint(NjCloneTint);
                    c.ShotKey = "shuriken";
                }
                PlayFx("njsmoke", at, SsrBody * 2.6f, loop: false)?.SetFrameSeconds(0.07f);
                _pfx?.Burst(ParticleFxKind.Glow, ParticleElement.Curse, at, 3, 60f, 60f, 0.5f, 0f);
                clones.Add(at);
            }
            // 시안 마지막 컷 — 분신이 적에게 뛰어들어 보라 칼빛으로 벤다(2초마다 한 번씩 셋)
            for (int wave = 0; wave < 2; wave++)
                for (int i = 0; i < clones.Count; i++)
                {
                    int ci = i;
                    UltAfter(1.8f + wave * 2.2f + i * 0.18f, () => NjCloneSlash(me, clones[ci]));
                }
        }

        private void NjCloneSlash(Unit me, Vector2 from)
        {
            var t = NearestEnemy(from, Meters(6f));
            if (t == null) return;
            var at = t.Position;
            _pfx?.Spray(ParticleFxKind.Streak, from, (at - from).normalized, 4f, 3, 900f, 1200f, new Vector2(70f, 7f), 0.2f,
                        new Color(0.75f, 0.45f, 1f, 1f));
            UltAfter(0.08f, () =>
            {
                if (t == null || !t.IsAlive) return;
                PlayFx("njslash", at, SsrBody * 2f, loop: false)?.SetFrameSeconds(0.045f);
                UltHit(me, t, 1.4f);
                _pfx?.Burst(ParticleFxKind.Star4, ParticleElement.Curse, at, 6, 160f, 14f, 0.4f, 0f);
                HitStop(0.02f);
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  닌자(사슬) — 사슬 돌격 연격: 사슬을 걸고(분홍 섬광) 날아가 연타, 분홍 빛기둥으로 내려찍는다
        // ═══════════════════════════════════════════════════════════

        private static readonly Color NcPink = new(1f, 0.45f, 0.75f, 1f);

        /// <summary>
        /// 가장 센 적 — 비슷하게 세면(최대 체력 90% 안) **먼 쪽**을 고른다. 「사슬을 걸고 내가 날아간다」가 읽히게
        /// (붙은 적을 고르면 사슬도 돌진도 안 보였다 — 녹화 sr_nc1). 보스 · 엘리트는 늘 먼저다.
        /// </summary>
        private Unit NcPickTarget(Unit me)
        {
            var best = UltStrongest(me.Position);
            if (best == null || best.IsBoss || best.IsElite) return best;
            float bar = best.HpMax * 0.9f, far = Vector2.Distance(me.Position, best.Position);
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (!Targetable(e) || !IsOnScreen(e) || e.HpMax < bar) continue;
                float d = Vector2.Distance(me.Position, e.Position);
                if (d > far) { far = d; best = e; }
            }
            return best;
        }

        private void SrNinjaChain(Unit me)
        {
            var t = NcPickTarget(me);
            if (t == null) { UltFallback(me); return; }
            UltBeam("chain", me.Position, t.Position, 40f, 0.06f);
            PlayFx("nchook", t.Position, SsrBody * 1.4f, loop: false)?.SetFrameSeconds(0.05f);
            UltInvuln(1.6f);
            var to = t.Position - (t.Position - me.Position).normalized * Meters(0.8f);
            UltAfter(0.15f, () => UltDashTo(me, to, 0.25f, NcPink, () =>
            {
                for (int i = 0; i < 5; i++)
                {
                    int n = i;
                    UltAfter(n * 0.1f, () =>
                    {
                        if (t == null || !t.IsAlive) return;
                        me.PlayAttack();
                        UltHit(me, t, 1.6f, capBoss: true);
                        var slash = PlayFx(n % 2 == 0 ? "rpslash" : "hxslash", t.Position, 130f, loop: false);
                        slash?.SetTint(NcPink);
                        _pfx?.Spray(ParticleFxKind.Spark, t.Position, Random.insideUnitCircle.normalized, 60f, 5, 150f, 300f,
                                    new Vector2(9f, 9f), 0.3f, NcPink);
                        HitStop(0.025f);
                    });
                }
                UltAfter(0.6f, () =>
                {
                    var at = t != null && t.IsAlive ? t.Position : me.Position;
                    // 시안 마지막 컷 — 위에서 꽂히는 분홍 빛기둥 · 충격 고리 · 갈라진 땅
                    var slam = PlayFx("ncslam", at + new Vector2(0f, SsrBody * 0.6f), SsrBody * 3.4f, loop: false);
                    slam?.SetFrameSeconds(0.05f);
                    var crack = PlayFloorFx("elcrack", at, SsrBody * 2.8f, loop: true);
                    crack?.SetLife(1.4f, 0.5f);
                    UltAoe(me, at, 2f, 4f, slam == null ? "slam" : null);
                    _pfx?.Burst(ParticleFxKind.Shard, ParticleElement.Dust, at, 12, 320f, 26f, 0.6f, 360f);
                    Shake(10f);
                    HitStop(0.1f);
                });
            }));
        }

        // 로봇 특공대 · 코만도(미사일) 집중 포격은 BattleDirector.Rework1021 (PD 2026-10-21)

        // ── 미사일 공용 — 연기 꼬리 · 조준 표식 · 맞으면 화염구 ──────────────

        private sealed class SrMissileTrack
        {
            public Projectile Shot;
            public Unit Target;
            public Impact Lock;
            public Vector2 Last;
            public Vector2 Prev;
            public float Smoke;
            public bool Light;      // 빛 구슬(화이트 위저드) — 연기 대신 금빛 빛꼬리
            public string Boom;     // 맞은 자리 그림(없으면 화염구)
            public float BoomSize;
        }

        private readonly List<SrMissileTrack> _srMissiles = new();

        private void SrMissile(Unit me, Vector2 from, Vector2 at, Unit target, int damage, float radius, bool lockMark)
        {
            var shot = RentShot();
            if (shot == null) return;
            shot.SetSprite(ShotFrames("missile"), "missile", LoopsFrames("missile"));
            shot.Fire(from, at, _config.ShotSpeedPlayer, damage, true, target,
                      _config.ShotSize, ShotPlayerColor, _config.ShotLifeSeconds);
            shot.SetBlastRadius(radius);
            if (target != null) shot.SetHoming(MissileFanHoming);
            Impact mark = null;
            if (lockMark && target != null)
            {
                mark = TakeLoopFx("cmlock", target.Position, SsrBody * 0.9f);
                mark?.SetFrameSeconds(0.1f);
            }
            _srMissiles.Add(new SrMissileTrack { Shot = shot, Target = target, Lock = mark, Last = from, Prev = from });
        }

        private void TickSrMissiles(float dt)
        {
            for (int i = _srMissiles.Count - 1; i >= 0; i--)
            {
                var m = _srMissiles[i];
                if (m.Shot != null && m.Shot.IsActive)
                {
                    m.Last = m.Shot.Position;
                    if (m.Lock != null && m.Target != null && m.Target.IsAlive) m.Lock.MoveTo(m.Target.Position);
                    // 시안 가운데 컷 — 미사일마다 흰 연기 꼬리
                    var now = m.Shot.Position;
                    var fly = now - m.Prev;
                    float dist = fly.magnitude;
                    var back = dist > 0.01f ? -fly / dist : Vector2.down;
                    // 시안 가운데 컷 — 끊기지 않는 흰 연기 꼬리. 미사일이 빨라 프레임마다 한 점이면 점선이 됐다(녹화 sr_cm4) —
                    //   지난 자리부터 지금 자리까지 12 px 마다 한 덩이씩 채운다
                    m.Smoke += dist;
                    float gap = m.Light ? 9f : 12f;
                    while (m.Smoke >= gap)
                    {
                        m.Smoke -= gap;
                        var at = now + back * m.Smoke;
                        if (m.Light)
                            // 시안 가운데 컷(화이트 위저드) — 휘어 나가는 금빛 빛꼬리
                            _pfx?.Spray(ParticleFxKind.Glow, at, back, 20f, 1, 5f, 20f, new Vector2(20f, 20f), 0.55f,
                                        new Color(1f, 0.9f, 0.55f, 0.9f));
                        else
                            _pfx?.Spray(ParticleFxKind.Smoke, at, back, 30f, 1, 5f, 25f, new Vector2(40f, 40f), 0.9f,
                                        Color.white, element: ParticleElement.Ice);
                    }
                    if (!m.Light)
                        _pfx?.Spray(ParticleFxKind.Streak, now, back, 8f, 1, 60f, 120f, new Vector2(22f, 6f), 0.12f,
                                    new Color(1f, 0.75f, 0.3f, 1f));
                    m.Prev = now;
                    continue;
                }
                // 맞았다(또는 사라졌다) — 그 자리에서 화염구
                m.Lock?.Stop();
                var boom = PlayFx(m.Boom ?? "dgblast", m.Last, m.BoomSize > 0f ? m.BoomSize : SsrBody * 1.9f, loop: false);
                boom?.SetFrameSeconds(0.05f);
                _srMissiles.RemoveAt(i);
            }
        }

        private void StopSrMissiles()
        {
            for (int i = 0; i < _srMissiles.Count; i++) _srMissiles[i].Lock?.Stop();
            _srMissiles.Clear();
            _hsTrail?.Stop();
            _hsTrail = null;
        }
    }
}
