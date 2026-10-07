using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 무기 속성 7종 스킬 퀄업 연출 (2026-10-07) — 설계 `Projects/AVSR/_exchange/spec_skillfx_weapon.md`.
    ///
    /// 「연쇄 방전」과 같은 결이다 — 네 박자(예비 → 발동 → 타격 → 여운) · 원인과 결과가 이어진다 ·
    /// 밝기 왕은 하나 · 적과 몸을 덮지 않는다 · 화면 전체를 색으로 덮지 않는다.
    /// 무기 속성 언어: 청록 + 황금, 직선 · 절삭선 · 동심 조준, 빠른 이동과 짧은 소멸.
    ///
    ///   아마존      조준(고리 · 선) → 뛰어든 궤적 → 착지 절삭 · 바닥 갈라짐 · 범위 펄스
    ///   폭력배      총구 응축 · 세 갈래 조준 → 난사 탄이 가는 예광탄으로 → 맞은 자리 작은 코어
    ///   호퍼(기관단총) 조준 → 도약 궤적 · 접촉 절삭 → 무적 동안 몸 바깥 윤곽(hsinv)
    ///   코만도(기관총) 발밑 수축 → 분절 방벽이 세워진다 → 깎일 때 접촉 굴절 · 남은 쉴드만큼 옅어진다
    ///   갱스터      총구 → 적마다 빠른 조준 · 예광 → 모두 동시에 표식(gamark) · 즉사엔 절삭 봉인(gaexec)
    ///   호퍼        발밑 수축 → 몸 바깥 괄호가 조립되고 황금 눈금 점등 → 지속 내내 괄호(hopaim)
    ///   닌자        방 한가운데 조준 → 훑는 선 · 절삭 → 반투명 분신 · 둘레 세그먼트
    ///
    /// 그림은 발주본(`fx_wp*` · `fx_am*` 등 — `Projects/AVSR/Tools/wp_parts_fit.py`).
    /// 공통 부품이 하나라도 없으면 예전 연출로 돌아간다(`WpReady`).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private enum WpStep { SprayAim, AmazonLand, SmgLeap, BarrierUp, BarrierLoop, GangAim, GangFire, GangMark, HopBuild, HopLoop, CloneBeam, CloneUp }

        /// <summary>조준이 걸리고 실제 동작이 나갈 때까지(예비 박자).</summary>
        private const float WpWindup = 0.10f;

        /// <summary>
        /// 몸 둘레에 감기는 지속 표시의 상자 크기. 그림 가운데가 비어 있어(몸 자리)
        /// 몸(화면 약 144 px)보다 커야 몸을 덮지 않는다.
        /// </summary>
        private const float WpAuraSize = 176f;

        private const float WpTrailThickness = 64f;
        private const float WpHitSize = 60f;
        // 파편 그림은 칸 가운데가 비어 있다 — 상자 지름의 절반쯤이 파편이 흩어지는 반경이다
        private const float WpDebrisSize = 72f;
        private const float WpCutSize = 120f;
        private const float WpLockRingSize = 104f;
        private const float WpMuzzleSize = 44f;

        /// <summary>
        /// 난사 예광탄이 맞은 자리 — 연사라 몇 발에 한 번만 그린다.
        /// 0.05초로는 코어가 쉬지 않고 터져 맞은 몸을 덮는 덩어리가 됐다(검수 2026-10-07).
        /// </summary>
        private const float WpTracerHitGap = 0.12f;
        private const float WpTracerMuzzleGap = 0.07f;
        private const string WpTracerKind = "wptracer";

        private readonly List<(float Left, WpStep Step, Unit Target, int Index)> _wpPending = new();

        /// <summary>짧은 그림을 몸에 붙여 따라가게 한다(조립되는 괄호 등) — 남은 시간이 다하면 놓는다.</summary>
        private readonly List<(Impact Fx, Unit U, float Left)> _wpFollow = new();

        private bool WpReady => LzReady && FxFrames("wpbeam") != null && FxFrames("wpcut") != null
                                && FxFrames("wppulse") != null && FxFrames("wpstate") != null;

        // ─────────────────────────────────────────
        //  공통
        // ─────────────────────────────────────────

        /// <summary>
        /// 조준을 건다 — 대상 발밑에 끊어진 고리가 조여들고, <paramref name="from"/> 에서 가는 선이 걸린다.
        /// </summary>
        private void WpLock(Vector2 from, Vector2 at, float ringSize, float aimAlpha)
        {
            var feet = at - Vector2.up * LzFootDrop;
            var ringFrames = FxFrames("lzring");
            var ring = ringFrames != null ? TakeFloorFx(ringSize) : null;
            if (ring != null)
            {
                ring.PlayOver(feet, ringFrames, ringSize, WpWindup + 0.06f);
                ring.SetSquash(LzRingSquash);
                ring.SetLife(WpWindup + 0.3f, 0.2f);
            }
            var aim = FreeImpact(LzAimThickness);
            if (aim != null)
            {
                aim.PlayBeam(from, at, FxFrames("lzaim"), LzAimThickness);
                aim.SetTint(new Color(1f, 1f, 1f, aimAlpha));
                aim.SetLife(WpWindup + 0.06f, 0.08f);
            }
        }

        private Impact WpBeam(string name, Vector2 from, Vector2 to, float thickness, float step, float alpha)
        {
            var frames = FxFrames(name);
            if (frames == null) return null;
            var im = FreeImpact(thickness);
            if (im == null) return null;
            im.PlayBeam(from, to, frames, thickness);
            im.SetFrameSeconds(step);
            if (alpha < 1f) im.SetTint(new Color(1f, 1f, 1f, alpha));
            return im;
        }

        /// <summary>바닥에 눕혀 깐다(몸 아래 층) — 펄스 · 갈라짐 · 조준 고리.</summary>
        private Impact WpFloor(string name, Vector2 feet, float size, float step, float alpha, float squash)
        {
            var im = PlayFloorFx(name, feet, size, loop: false);
            if (im == null) return null;
            im.SetFrameSeconds(step);
            if (squash < 1f) im.SetSquash(squash);
            if (alpha < 1f) im.SetTint(new Color(1f, 1f, 1f, alpha));
            return im;
        }

        /// <summary>그림 한 벌 중 일부 장만 — 조립(앞 장)과 반복(뒤 장)을 따로 돌린다. 처음 한 번만 만든다.</summary>
        private Sprite[] WpSlice(ref Sprite[] cache, string name, int from, int count)
        {
            if (cache != null) return cache;
            var all = FxFrames(name);
            if (all == null || all.Length < from + count) return null;
            cache = new Sprite[count];
            for (int i = 0; i < count; i++) cache[i] = all[from + i];
            return cache;
        }

        /// <summary>몸을 따라다니는 한 번짜리 그림.</summary>
        private Impact WpOnBody(string name, Unit u, float size, float step, float alpha, float follow)
        {
            var im = PlayLz(name, u.Position, size, step);
            if (im == null) return null;
            if (alpha < 1f) im.SetTint(new Color(1f, 1f, 1f, alpha));
            _wpFollow.Add((im, u, follow));
            return im;
        }

        private Impact WpOnBody(Sprite[] frames, Unit u, float size, float step, float alpha, float follow)
        {
            if (frames == null) return null;
            var im = FreeImpact(size);
            if (im == null) return null;
            im.Play(u.Position, frames, size, loop: false);
            im.SetFrameSeconds(step);
            if (alpha < 1f) im.SetTint(new Color(1f, 1f, 1f, alpha));
            _wpFollow.Add((im, u, follow));
            return im;
        }

        /// <summary>반복해서 도는 지속 표시. 거두는 것은 부른 쪽의 몫이다.</summary>
        private Impact WpLoop(Sprite[] frames, Vector2 at, float size, float step, float alpha)
        {
            if (frames == null) return null;
            var im = FreeImpact(size);
            if (im == null) return null;
            im.Play(at, frames, size, loop: true);
            im.SetFrameSeconds(step);
            if (alpha < 1f) im.SetTint(new Color(1f, 1f, 1f, alpha));
            return im;
        }

        /// <summary>지속 표시를 짧게 흐리며 놓는다. 놓은 뒤에는 들고 있지 않는다(풀에서 다른 데 쓰인다).</summary>
        private static void WpRelease(ref Impact fx, float fade)
        {
            if (fx == null) return;
            if (fx.IsActive) fx.SetLife(fade, fade);
            fx = null;
        }

        /// <summary>뛰어든 자리 — 지나간 궤적, 몸 뒤 절삭, 접촉 코어 · 파편, 작은 반동.</summary>
        private void WpLeapContact(Vector2 from, Vector2 to, float hitSize, float kick)
        {
            WpBeam("wpbeam", from, to, WpTrailThickness, 0.035f, 0.85f);
            var d = to - from;
            var dir = d.sqrMagnitude > 1f ? d.normalized : Vector2.up;
            var feet = to - Vector2.up * LzFootDrop;
            PlayLz("wpcut", feet, WpCutSize, 0.035f);
            PlayLz("lzhit", feet, hitSize, 0.025f);
            // 파편은 착지 한 번 · 작게 · 옅게 — 착지점 바깥 반경 72 px 안(검수)
            var debris = PlayLz("lzdebris", feet, WpDebrisSize * 2f, 0.06f);
            debris?.SetTint(new Color(1f, 1f, 1f, 0.35f));
            Kick(-dir, kick);
        }

        // ─────────────────────────────────────────
        //  아마존 · 도약 강타
        // ─────────────────────────────────────────

        /// <summary>조준을 건다 — 0.1초 뒤 뛰어든다(<see cref="AmazonLeapLand"/>). 그림이 없으면 false.</summary>
        private bool WpAmazonWindup(Unit me, Unit target)
        {
            if (!WpReady || target == null) return false;
            WpLock(me.Position - Vector2.up * LzFootDrop, target.Position - Vector2.up * LzFootDrop, WpLockRingSize, 0.5f);
            _wpPending.Add((WpWindup, WpStep.AmazonLand, target, 0));
            return true;
        }

        /// <summary>
        /// 착지 — 절삭 · 바닥 갈라짐 · 발밑 펄스.
        /// 펄스를 판정 반경(지름 약 2.5 m)만큼 깔았더니 착지 하중보다 큰 원이 먼저 읽혔다(검수 2026-10-07) —
        /// 발밑 168×56 타원 · 0.33초로 줄인다.
        /// </summary>
        private void WpAmazonImpact(Vector2 from, Vector2 to, float radius)
        {
            WpLeapContact(from, to, WpHitSize, 2f);
            var feet = to - Vector2.up * LzFootDrop;
            WpFloor("wppulse", feet, AmPulseSize, 0.08f, 0.55f, AmPulseSquash);
            WpFloor("amland", feet, AmLandSize, 0.07f, 0.5f, 1f);
        }

        private const float AmPulseSize = 168f;
        private const float AmPulseSquash = 56f / 168f;
        private const float AmLandSize = 128f;

        // ─────────────────────────────────────────
        //  폭력배 · 난사
        // ─────────────────────────────────────────

        private float _wpTracerHitAt = -1f;
        private float _wpTracerMuzzleAt = -1f;

        private bool WpTracerReady => WpReady && FxFrames(WpTracerKind) != null;

        /// <summary>총구에 빛이 모이고 세 갈래 조준선이 벌어진다. 탄은 예광탄으로 나간다(<see cref="FireShot"/>).</summary>
        private bool WpSprayOpen(Unit me, float degrees)
        {
            if (!WpTracerReady) return false;
            PlayLz("lzmuzzle", me.MuzzlePosition, WpMuzzleSize, 0.025f);
            var target = NearestEnemy(me.Position);
            var from = me.MuzzlePosition;
            _sprayAim = target != null && (target.Position - from).sqrMagnitude > 1f
                      ? target.Position - from : me.Facing * 320f;
            _sprayHalfDeg = degrees * 0.5f;
            // 세 갈래를 한꺼번에 켜면 한 덩어리로 읽힌다 — 가운데 → 좌 → 우 를 0.03초씩(검수 2026-10-07)
            for (int i = 0; i < 3; i++) _wpPending.Add((SprayAimGap * i, WpStep.SprayAim, null, i));
            return true;
        }

        private const float SprayAimGap = 0.03f;
        private Vector2 _sprayAim;
        private float _sprayHalfDeg;

        private void WpSprayAim(Unit me, int index)
        {
            int side = index == 0 ? 0 : index == 1 ? -1 : 1;
            var from = me.MuzzlePosition;
            var line = WpBeam("wpbeam", from, from + Rotate(_sprayAim, _sprayHalfDeg * side), WpTrailThickness * 0.75f, 0.025f, 0.45f);
            line?.SetLife(0.1f, 0.05f);
        }

        /// <summary>난사 중이면 탄 그림을 예광탄으로 바꾼다. 바꿨으면 그 종류 이름을 돌려준다.</summary>
        private string WpTracerShot(Unit attacker, Projectile shot, bool fromPlayer, Vector2 muzzle, string kind)
        {
            if (!fromPlayer || SprayExtraShots <= 0 || attacker != _host || !WpTracerReady) return kind;
            shot.SetSprite(FxFrames(WpTracerKind), WpTracerKind);
            float now = Time.time;
            if (now - _wpTracerMuzzleAt >= WpTracerMuzzleGap)
            {
                _wpTracerMuzzleAt = now;
                var flash = PlayLz("lzmuzzle", muzzle, WpMuzzleSize * 0.6f, 0.02f);
                flash?.SetTint(new Color(1f, 1f, 1f, 0.7f));
            }
            return WpTracerKind;
        }

        /// <summary>예광탄이 맞은 자리 — 작은 코어와 파편. 큰 노란 별은 쓰지 않는다.</summary>
        private void WpTracerHit(Vector2 at)
        {
            float now = Time.time;
            if (now - _wpTracerHitAt < WpTracerHitGap) return;
            _wpTracerHitAt = now;
            var hit = PlayLz("lzhit", at, 38f, 0.02f);
            hit?.SetTint(new Color(1f, 1f, 1f, 0.85f));
            var debris = PlayLz("lzdebris", at, 44f, 0.045f);
            debris?.SetTint(new Color(1f, 1f, 1f, 0.35f));
        }

        // ─────────────────────────────────────────
        //  호퍼(기관단총) · 도약 강습
        // ─────────────────────────────────────────

        private Impact _hsInvFx;
        private float _hsArcTimer;

        private bool WpSmgWindup(Unit me, Unit far)
        {
            if (!WpReady || far == null) return false;
            WpLock(me.Position - Vector2.up * LzFootDrop, far.Position - Vector2.up * LzFootDrop, WpLockRingSize * 0.95f, 0.5f);
            _wpPending.Add((WpWindup, WpStep.SmgLeap, far, 0));
            return true;
        }

        /// <summary>뛰어든 뒤 — 접촉 절삭, 그리고 무적 동안 몸 바깥 윤곽이 돈다.</summary>
        private void WpSmgLanded(Unit me, Vector2 from, Vector2 to)
        {
            WpLeapContact(from, to, WpHitSize * 0.85f, 1f);
            _hsInvFx?.Stop();
            _hsInvFx = WpLoop(FxFrames("hsinv"), me.Position, WpAuraSize, 0.07f, 0.9f);
            _hsArcTimer = 0.25f;
        }

        private void TickSmgInvuln(float dt)
        {
            if (_hsInvFx == null) return;
            if (_skillInvuln <= 0f || _host == null || _host.Key != "hopper_smg") { WpRelease(ref _hsInvFx, 0.12f); return; }
            _hsInvFx.MoveTo(_host.Position);
            _hsArcTimer -= dt;
            if (_hsArcTimer > 0f) return;
            _hsArcTimer = 0.25f;
            // 몸 바깥에서만 — 몸 위에 얹으면 무적이 아니라 맞은 것으로 읽힌다
            var side = Rotate(Vector2.right, Random.Range(0f, 360f)) * (WpAuraSize * 0.42f);
            var arc = PlayLz("lzarc", _host.Position + side, 56f, 0.04f);
            arc?.SetTint(new Color(1f, 1f, 1f, 0.7f));
        }

        // ─────────────────────────────────────────
        //  코만도(기관총) · 방벽 전개
        // ─────────────────────────────────────────

        private Impact _cmWallFx;
        private Sprite[] _cmWallBuild, _cmWallLoop, _cmWallHitLeft, _cmWallHitRight;
        private int _cmWallFull;
        private int _cmWallLast;
        private float _cmWallHitGap;
        private bool _cmWallHitFlip;

        /// <summary>
        /// 방벽이 서 있는 동안은 보통 쉴드 거품을 그리지 않는다 — 두 겹이 된다.
        /// 방벽이 세워지는 0.2초(예비 · 발동) 동안에도 거품이 먼저 떴다(검수) — 방벽 시간 전체로 잰다.
        /// </summary>
        private bool IsCmWallShowing => _cmWallFx != null
            || (_barrierSeconds > 0f && _host != null && _host.Key == "commando_mg" && WpReady && FxFrames("cmwall") != null);

        private bool WpBarrierOpen(Unit me)
        {
            if (!WpReady || FxFrames("cmwall") == null) return false;
            WpFloor("lzring", me.Position - Vector2.up * LzFootDrop, WpLockRingSize, 0.04f, 1f, LzRingSquash);
            WpOnBody("wpstate", me, WpAuraSize * 0.9f, 0.06f, 0.45f, 0.24f);
            _wpPending.Add((0.12f, WpStep.BarrierUp, me, 0));
            return true;
        }

        private void WpBarrierUp(Unit me)
        {
            WpFloor("wppulse", me.Position - Vector2.up * LzFootDrop, WpAuraSize * 1.1f, 0.025f, 0.9f, LzRingSquash);
            WpOnBody(WpSlice(ref _cmWallBuild, "cmwall", 0, 4), me, WpAuraSize, 0.025f, 1f, 0.12f);
            _wpPending.Add((0.1f, WpStep.BarrierLoop, me, 0));
        }

        private void WpBarrierLoop(Unit me)
        {
            if (_barrierSeconds <= 0f || me.Shield <= 0) return;
            _cmWallFx?.Stop();
            _cmWallFx = WpLoop(WpSlice(ref _cmWallLoop, "cmwall", 6, 2), me.Position, WpAuraSize, 0.08f, 0.7f);
            _cmWallFull = Mathf.Max(1, me.Shield);
            _cmWallLast = me.Shield;
        }

        private void TickCmWall(float dt)
        {
            if (_cmWallFx == null) return;
            var me = _host;
            if (me == null || me.Key != "commando_mg" || _barrierSeconds <= 0f || me.Shield <= 0)
            { WpRelease(ref _cmWallFx, 0.12f); return; }
            _cmWallFx.MoveTo(me.Position);
            // 남은 쉴드만큼 옅어진다 — 얼마나 버틸지가 진하기로 읽힌다
            float left = Mathf.Clamp01((float)me.Shield / _cmWallFull);
            _cmWallFx.SetTint(new Color(1f, 1f, 1f, Mathf.Lerp(0.25f, 0.7f, left)));

            _cmWallHitGap -= dt;
            if (me.Shield < _cmWallLast && _cmWallHitGap <= 0f)
            {
                // 깎였다 — 방벽이 접촉 쪽으로 굴절한다. 몸 위에는 코어를 놓지 않는다(표면에만)
                _cmWallHitGap = 0.15f;
                _cmWallHitFlip = !_cmWallHitFlip;
                var frames = _cmWallHitFlip ? WpSlice(ref _cmWallHitLeft, "cmwall", 4, 1)
                                            : WpSlice(ref _cmWallHitRight, "cmwall", 5, 1);
                var flash = WpOnBody(frames, me, WpAuraSize, 0.1f, 0.9f, 0.1f);
                flash?.SetLife(0.1f, 0.06f);
                var side = (_cmWallHitFlip ? Vector2.left : Vector2.right) * (WpAuraSize * 0.4f);
                var arc = PlayLz("lzarc", me.Position + side, 48f, 0.03f);
                arc?.SetTint(new Color(1f, 1f, 1f, 0.75f));
            }
            _cmWallLast = me.Shield;
        }

        // ─────────────────────────────────────────
        //  갱스터 · 일제 표식
        // ─────────────────────────────────────────

        private readonly List<Unit> _gangTargets = new();
        private float _gangSeconds;
        private int _gangPercent;
        private Sprite[] _gaMarkBuild, _gaMarkLoop;
        private readonly List<(Unit U, Impact Fx, float Life)> _gaMarks = new();

        /// <summary>표식이 걸린 몸의 진하기 · 끝날 때 흐려지는 시간.</summary>
        private const float GaMarkAlpha = 0.6f;
        private const float GaMarkFade = 0.15f;

        private bool GaReady => WpReady && FxFrames("gamark") != null;

        /// <summary>
        /// 총구에서 적마다 빠르게 조준이 훑고(0.1초) → 한 번 쏘아 예광이 차례로 닿고 →
        /// 모두 **같은 순간** 표식이 박힌다. 예전처럼 하나씩 0.11초 간격으로 찍지 않는다(설계).
        /// </summary>
        private bool WpGangOpen(Unit me, float seconds, int percent)
        {
            if (!GaReady) return false;
            _gangTargets.Clear();
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e != null && e.IsAlive && !e.IsDying) _gangTargets.Add(e);
            }
            var from = me.Position;
            _gangTargets.Sort((a, b) => (a.Position - from).sqrMagnitude.CompareTo((b.Position - from).sqrMagnitude));
            _gangSeconds = seconds;
            _gangPercent = percent;
            PlayLz("lzmuzzle", me.MuzzlePosition, WpMuzzleSize * 0.8f, 0.025f);
            int n = _gangTargets.Count;
            float aimGap = n > 0 ? Mathf.Min(0.025f, WpWindup / n) : 0f;
            for (int i = 0; i < n; i++)
            {
                _wpPending.Add((aimGap * i, WpStep.GangAim, _gangTargets[i], i));
                _wpPending.Add((WpWindup + 0.02f * i, WpStep.GangFire, _gangTargets[i], i));
            }
            _wpPending.Add((WpWindup + 0.02f * n + 0.03f, WpStep.GangMark, null, 0));
            return true;
        }

        private void WpGangAim(Unit e)
        {
            if (_host == null) return;
            var line = FreeImpact(LzAimThickness);
            if (line == null) return;
            line.PlayBeam(_host.MuzzlePosition, e.Position, FxFrames("lzaim"), LzAimThickness);
            line.SetTint(new Color(1f, 1f, 1f, 0.35f));
            line.SetLife(0.1f, 0.05f);
        }

        private void WpGangFire(Unit e, int index)
        {
            if (_host == null) return;
            if (index == 0) PlayLz("lzmuzzle", _host.MuzzlePosition, WpMuzzleSize, 0.02f);
            var beam = WpBeam("wpbeam", _host.MuzzlePosition, e.Position, WpTrailThickness * 0.75f, 0.025f, 1f);
            beam?.SetLife(0.1f, 0.05f);
        }

        private void WpGangMark()
        {
            for (int i = 0; i < _gangTargets.Count; i++)
            {
                var e = _gangTargets[i];
                if (e == null || !e.IsAlive || e.IsDying) continue;
                e.ApplyAmp(_gangPercent, _gangSeconds);
                e.SetMark(_gangSeconds);
                ShowGaMark(e, _gangSeconds);
            }
            _gangTargets.Clear();
            if (_host != null) global::Game.Module.Common.GameSound.HostAttack(_host.Key);
        }

        /// <summary>표식 — 괄호가 조여 붙고(1~3장) 걸린 내내 돈다(4~6장). 그림이 없으면 false.</summary>
        private bool ShowGaMark(Unit target, float seconds)
        {
            if (!GaReady || target == null || !target.IsAlive) return false;
            for (int i = 0; i < _gaMarks.Count; i++)
                if (_gaMarks[i].U == target)
                {
                    var (u, fx, life) = _gaMarks[i];
                    _gaMarks[i] = (u, fx, Mathf.Max(life, seconds));
                    return true;
                }
            float size = ReticleSizeFor(target.BodyRadius) * 1.25f;
            WpOnBody(WpSlice(ref _gaMarkBuild, "gamark", 0, 3), target, size, 0.03f, 1f, 0.09f);
            var loop = WpLoop(WpSlice(ref _gaMarkLoop, "gamark", 3, 3), target.Position, size, 0.1f, GaMarkAlpha);
            if (loop != null) _gaMarks.Add((target, loop, seconds));
            return true;
        }

        private void TickGaMarks(float dt)
        {
            for (int i = _gaMarks.Count - 1; i >= 0; i--)
            {
                var (u, fx, life) = _gaMarks[i];
                life -= dt;
                if (u == null || !u.IsAlive || life <= 0f) { fx?.Stop(); _gaMarks.RemoveAt(i); continue; }
                fx.MoveTo(u.Position);
                if (life < GaMarkFade) fx.SetTint(new Color(1f, 1f, 1f, GaMarkAlpha * life / GaMarkFade));
                _gaMarks[i] = (u, fx, life);
            }
        }

        /// <summary>즉사 — 표식 위로 짧은 절삭 봉인. 화면은 1 px 만 민다.</summary>
        private bool WpExecute(Unit victim)
        {
            if (!GaReady || FxFrames("gaexec") == null) return false;
            PlayLz("gaexec", victim.Position, ReticleSizeFor(victim.BodyRadius) * 1.3f, 0.04f);
            Kick(Vector2.down, 1f);
            return true;
        }

        // ─────────────────────────────────────────
        //  호퍼 · 정조준
        // ─────────────────────────────────────────

        private Impact _hopAimFx;
        private Sprite[] _hopAimBuild, _hopAimLoop;

        private bool WpHopOpen(Unit me)
        {
            if (!WpReady || FxFrames("hopaim") == null) return false;
            var ring = WpFloor("lzring", me.Position - Vector2.up * LzFootDrop, WpLockRingSize * 1.2f, 0.04f, 1f, LzRingSquash);
            ring?.SetSnap(1.6f, 0.12f);
            WpOnBody("wpstate", me, WpAuraSize * 0.9f, 0.06f, 0.35f, 0.24f);
            _wpPending.Add((0.12f, WpStep.HopBuild, me, 0));
            return true;
        }

        private void WpHopBuild(Unit me)
        {
            // 괄호 넷이 모이고 다섯째 장에 황금 눈금 — 이 순간이 밝기 왕이다
            WpOnBody(WpSlice(ref _hopAimBuild, "hopaim", 0, 5), me, WpAuraSize, 0.03f, 1f, 0.15f);
            PlayLz("lzmuzzle", me.MuzzlePosition, WpMuzzleSize * 0.7f, 0.025f);
            _wpPending.Add((0.15f, WpStep.HopLoop, me, 0));
        }

        private void WpHopLoop(Unit me)
        {
            if (_critLockSeconds <= 0f) return;
            WpFloor("wppulse", me.Position - Vector2.up * LzFootDrop, WpAuraSize * 0.8f, 0.03f, 0.8f, LzRingSquash);
            _hopAimFx?.Stop();
            _hopAimFx = WpLoop(WpSlice(ref _hopAimLoop, "hopaim", 5, 3), me.Position, WpAuraSize, 0.09f, 0.6f);
        }

        private void TickHopAim()
        {
            if (_hopAimFx == null) return;
            if (_critLockSeconds <= 0f || _host == null || _host.Key != "hopper") { WpRelease(ref _hopAimFx, 0.12f); return; }
            _hopAimFx.MoveTo(_host.Position);
        }

        // ─────────────────────────────────────────
        //  닌자 · 그림자 분신
        // ─────────────────────────────────────────

        private Impact _cloneStateFx;
        private Unit _cloneUnit;
        private Vector2 _cloneAt;

        /// <summary>분신을 세울 때 연기 · 폭발을 띄우지 않는다(퀄업 연출이 대신한다).</summary>
        private bool _wpQuietSummon;

        private bool WpCloneOpen(Unit me, Vector2 center)
        {
            if (!WpReady) return false;
            _cloneAt = center;
            WpLock(me.Position, center, WpLockRingSize * 1.1f, 0.35f);
            _wpPending.Add((WpWindup, WpStep.CloneBeam, me, 0));
            return true;
        }

        private void WpCloneBeam(Unit me)
        {
            WpBeam("wpbeam", me.Position, _cloneAt, WpTrailThickness, 0.03f, 1f);
            PlayLz("wpcut", _cloneAt - Vector2.up * LzFootDrop, WpCutSize * 0.9f, 0.03f);
            _wpPending.Add((0.12f, WpStep.CloneUp, me, 0));
        }

        private void WpCloneUp()
        {
            _wpQuietSummon = true;
            SummonClone(_cloneAt);
            _wpQuietSummon = false;
            var feet = _cloneAt - Vector2.up * LzFootDrop;
            WpFloor("wppulse", feet, WpAuraSize * 0.9f, 0.04f, 0.9f, LzRingSquash);
            var debris = PlayLz("lzdebris", feet, WpDebrisSize * 0.8f, 0.05f);
            debris?.SetTint(new Color(1f, 1f, 1f, 0.4f));
            _cloneUnit = _summons.Count > 0 ? _summons[_summons.Count - 1].U : null;
            _cloneStateFx?.Stop();
            _cloneStateFx = _cloneUnit != null
                ? WpLoop(FxFrames("wpstate"), _cloneUnit.Position, WpAuraSize, 0.1f, 0.35f) : null;
        }

        private void TickCloneState()
        {
            if (_cloneStateFx == null) return;
            if (_cloneUnit == null || !_cloneUnit.IsAlive) { WpRelease(ref _cloneStateFx, 0.15f); _cloneUnit = null; return; }
            _cloneStateFx.MoveTo(_cloneUnit.Position);
        }

        // ─────────────────────────────────────────
        //  시간 흐르기 · 정리
        // ─────────────────────────────────────────

        /// <summary>`TickNewSkills` 에서 부른다.</summary>
        private void TickWp(float dt)
        {
            for (int i = _wpPending.Count - 1; i >= 0; i--)
            {
                var p = _wpPending[i];
                p.Left -= dt;
                if (p.Left > 0f) { _wpPending[i] = p; continue; }
                _wpPending.RemoveAt(i);
                RunWpStep(p.Step, p.Target, p.Index);
            }
            for (int i = _wpFollow.Count - 1; i >= 0; i--)
            {
                var (fx, u, left) = _wpFollow[i];
                left -= dt;
                if (left <= 0f || u == null || !u.IsAlive) { _wpFollow.RemoveAt(i); continue; }
                fx.MoveTo(u.Position);
                _wpFollow[i] = (fx, u, left);
            }
            TickSmgInvuln(dt);
            TickCmWall(dt);
            TickGaMarks(dt);
            TickHopAim();
            TickCloneState();
        }

        private void RunWpStep(WpStep step, Unit target, int index)
        {
            var me = _host;
            if (me == null || !me.IsAlive) return;
            bool targetGone = target == null || !target.IsAlive || target.IsDying;
            switch (step)
            {
                case WpStep.SprayAim:    if (me.Key == "thug") WpSprayAim(me, index); break;
                case WpStep.AmazonLand:
                    if (me.Key != "amazon") return;
                    AmazonLeapLand(me, targetGone ? NearestEnemy(me.Position) : target);
                    break;
                case WpStep.SmgLeap:
                    if (me.Key != "hopper_smg") return;
                    LeapFarGo(me, targetGone ? null : target);
                    break;
                case WpStep.BarrierUp:   if (me.Key == "commando_mg") WpBarrierUp(me); break;
                case WpStep.BarrierLoop: if (me.Key == "commando_mg") WpBarrierLoop(me); break;
                case WpStep.GangAim:     if (me.Key == "gangster" && !targetGone) WpGangAim(target); break;
                case WpStep.GangFire:    if (me.Key == "gangster" && !targetGone) WpGangFire(target, index); break;
                case WpStep.GangMark:    if (me.Key == "gangster") WpGangMark(); break;
                case WpStep.HopBuild:    if (me.Key == "hopper") WpHopBuild(me); break;
                case WpStep.HopLoop:     if (me.Key == "hopper") WpHopLoop(me); break;
                case WpStep.CloneBeam:   if (me.Key == "ninja") WpCloneBeam(me); break;
                case WpStep.CloneUp:     if (me.Key == "ninja") WpCloneUp(); break;
            }
        }

        private void ClearWp()
        {
            _wpPending.Clear();
            _wpFollow.Clear();
            _gangTargets.Clear();
            _hsInvFx?.Stop(); _hsInvFx = null;
            _cmWallFx?.Stop(); _cmWallFx = null;
            _hopAimFx?.Stop(); _hopAimFx = null;
            _cloneStateFx?.Stop(); _cloneStateFx = null;
            _cloneUnit = null;
            // 표식(_gaMarks)은 몸을 갈아타도 남는다 — 판에 걸린 것이다. 적이 죽으면 스스로 거둔다
        }
    }
}
