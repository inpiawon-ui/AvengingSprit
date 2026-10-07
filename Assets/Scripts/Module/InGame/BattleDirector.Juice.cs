using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 손맛 — 화면 흔들림 · 히트스톱 · 업그레이드 번쩍임 (2026-09-10).
    ///
    /// 때리고 맞는 것이 숫자로만 보이고 **몸으로 안 느껴졌다.** 큰 것이 터져도 화면은
    /// 가만히 있어서, 잔챙이를 긁는 것과 보스를 크게 때리는 것이 같아 보인다.
    ///
    /// ── 왜 화면을 흔드나 ────────────────────────────────────
    /// 이 게임은 카메라가 따라다니는 탑뷰라, 흔들 수 있는 것이 화면뿐이다.
    /// `ApplyScroll` 이 모든 레이어를 한 값으로 옮기고 있어서 거기에 **얹기만 하면**
    /// 바닥·유닛·탄·글자가 통째로 같이 흔들린다 — 레이어마다 따로 흔들면 어긋난다.
    ///
    /// ⚠ **세게 흔들지 않는다.** 픽셀아트라 몇 픽셀만 흔들려도 충분히 읽히고,
    ///   크게 흔들면 조준이 안 된다. 최대 6 px 이다.
    ///
    /// ── 히트스톱 ────────────────────────────────────────────
    /// 큰 한 방에 아주 잠깐 시간을 늦춘다. 멈추면 「끊겼다」로 보이므로
    /// **0 으로 세우지 않고** 0.25 배까지만 늦춘다.
    ///
    /// ⚠ `Time.timeScale` 을 건드리므로 **되돌리는 책임이 여기 있다.** 방을 나가거나
    ///   판이 끝날 때 `ClearJuice` 가 원상복구한다 — 안 그러면 느려진 채로 굳는다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 화면 흔들림 ──────────────────────────────────────────

        private const float ShakeMaxPixels = 6f;
        private const float ShakeDecayPerSecond = 22f;

        private float _shake;          // 남은 세기(px)
        private Vector2 _shakeOffset;  // 이번 프레임 흔들린 양

        /// <summary>
        /// 화면을 흔든다. 세기는 픽셀이고, 이미 흔들리는 중이면 **큰 쪽만** 남긴다 —
        /// 더하면 잔챙이 여럿이 보스보다 크게 흔든다.
        /// </summary>
        private void Shake(float pixels)
            => _shake = Mathf.Min(ShakeMaxPixels, Mathf.Max(_shake, pixels));

        private void TickShake(float dt)
        {
            TickKick(dt);
            if (_shake <= 0f)
            {
                if (_shakeOffset != _kick) { _shakeOffset = _kick; ApplyScroll(); }
                return;
            }

            _shake = Mathf.Max(0f, _shake - ShakeDecayPerSecond * dt);
            // 매 프레임 방향을 새로 뽑는다. 한 축으로만 떨면 흔들림이 아니라 미끄러짐이다.
            float a = (float)_rng.NextDouble() * Mathf.PI * 2f;
            _shakeOffset = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * _shake + _kick;
            ApplyScroll();
        }

        // ── 반동(kick) — 한 방향으로 밀렸다 부드럽게 돌아온다 (2026-10-07) ──────
        //
        // 떨림(Shake)은 매 프레임 방향이 바뀌어 크면 어지럽다. 치명타 · 낙뢰 같은 「한 방」은
        // **맞은 방향으로 한 번 밀렸다가 돌아오는** 반동이 자연스럽다(코덱스 진단 — 1~2 px, 3~5 프레임).
        // 연사 몸은 치명타가 자주 터지므로 간격을 둔다 — 계속 밀리면 화면이 미끄러진다.

        private const float KickReturnPerSecond = 18f;   // 클수록 빨리 돌아온다(지수 감쇠)
        // 2.5 → 4 — 「치명타가 터지면 팍팍 강하게 들어가는 느낌」(PD 2026-10-07). 떨림도 짧게 얹는다(CritShake)
        private const float CritKickPixels = 4f;
        private const float CritShake = 3f;
        private const float CritKickGap = 0.35f;         // 치명타 반동 사이 최소 간격(실제 시간)

        private Vector2 _kick;
        private float _lastCritKickAt = -1f;

        /// <summary><paramref name="dir"/> 방향으로 <paramref name="pixels"/> 만큼 밀었다 돌려놓는다.</summary>
        private void Kick(Vector2 dir, float pixels)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            var k = dir.normalized * Mathf.Min(ShakeMaxPixels, pixels);
            if (k.sqrMagnitude > _kick.sqrMagnitude) _kick = k;   // 큰 쪽만 — 겹쳐 더하면 밀려 나간다
        }

        /// <summary>치명타 반동 — 간격 안이면 평타 떨림만. 연사 몸이 화면을 계속 밀지 않게.</summary>
        private void CritKick(Vector2 dir)
        {
            float now = Time.unscaledTime;
            if (now - _lastCritKickAt < CritKickGap) { Shake(ShakeOnHit); return; }
            _lastCritKickAt = now;
            Kick(dir, CritKickPixels);
            Shake(CritShake);
        }

        // ── 맞는 표시 — 도트 폭발 (2026-10-07) ─────────────────────
        //
        // PD 가 고른 시안 「C 도트 폭발」(`Projects/AVSR/_exchange/in/hit_options_v1.png` C줄) — 원작풍 작은 픽셀 폭발.
        // 그림 `fx_dothit_1~5`(보통) · `fx_dotcrit_1~6`(치명), 코덱스 납품 `in/hit_dot_frames.png`.
        // 반려 이력(같은 날): 노란 뾰족 별(fx_hit · fx_crit)은 표창 같았고, 얼음(fx_weakhit)은 무엇으로 때리든 얼음이었고,
        //   불 고리 · 연기는 크고 과했고, 작은 노란 섬광은 「터지는 느낌이 아니다」.
        //   보통 · 유리 — 작은 도트 폭발(탄 고유 터짐 대신). 유리는 ▲ · 주황 숫자가 말한다
        //   치명 — 큰 도트 폭발 + 반동 · 멈칫
        //   불리 — 작은 도트 폭발을 더 작게 + 회색 먼지 조금(덜 들어갔다)
        // 그림이 없으면 예전 불티로 돌아간다.

        private const float DotHitSize = 128f;            // 그림 칸 128 = 화면 128 — 보통 최대 지름 약 44
        private const float DotCritSize = 128f;           // 치명 최대 약 120
        private const float DotHitFrameSeconds = 0.035f;
        private const float DotCritFrameSeconds = 0.045f;
        private const float DotDullScale = 0.7f;
        // 연사 몸은 치명타가 자주 터진다 — 간격 안의 치명은 작은 폭발만(숫자는 크게 뜬다)
        private const float CritBurstGap = 0.12f;
        private float _lastCritBurstAt = -1f;

        private bool DotHitReady => FxFrames("dothit") != null && FxFrames("dotcrit") != null;

        /// <summary>탄 고유 터짐(impact_*)을 도트 폭발이 대신하는가 — 폭발탄 · 난사 예광탄은 제 터짐을 쓴다.</summary>
        private bool DotHitCovers(string kind)
            => DotHitReady && kind != WpTracerKind && kind != "grenade" && kind != "missile";

        /// <summary>보통 타격 한 번. <paramref name="dull"/> 이면 작게 + 회색 먼지.</summary>
        private void HitPop(Vector2 at, bool dull = false)
        {
            if (dull) _pfx?.Puff(at, ParticleElement.Dust, 0.3f);
            var im = DotHitReady ? PlayFx("dothit", at, dull ? DotHitSize * DotDullScale : DotHitSize, loop: false) : null;
            if (im != null) im.SetFrameSeconds(DotHitFrameSeconds);
            else _pfx?.Hit(at, dull ? ParticleElement.Dust : ParticleElement.Fire, dull ? 0.4f : 0.9f);
        }

        private void CritBurst(Vector2 at)
        {
            float now = Time.unscaledTime;
            if (now - _lastCritBurstAt < CritBurstGap) { HitPop(at); return; }
            _lastCritBurstAt = now;
            var im = DotHitReady ? PlayFx("dotcrit", at, DotCritSize, loop: false) : null;
            if (im != null) im.SetFrameSeconds(DotCritFrameSeconds);
            else _pfx?.Hit(at, ParticleElement.Fire, 1.4f);
        }

        private void TickKick(float dt)
        {
            if (_kick == Vector2.zero) return;
            _kick *= Mathf.Exp(-KickReturnPerSecond * dt);
            if (_kick.sqrMagnitude < 0.01f) _kick = Vector2.zero;
        }

        // ── 히트스톱 ─────────────────────────────────────────────

        private const float HitStopScale = 0.25f;
        private float _hitStopLeft;

        /// <summary>큰 한 방에 아주 잠깐 시간을 늦춘다.</summary>
        private void HitStop(float seconds)
        {
            _hitStopLeft = Mathf.Max(_hitStopLeft, seconds);
            Time.timeScale = HitStopScale;
        }

        /// <summary>⚠ **실제 시간으로 잰다.** 스케일된 시간으로 재면 스스로 못 풀린다.</summary>
        private void TickHitStop()
        {
            if (_hitStopLeft <= 0f) return;
            _hitStopLeft -= Time.unscaledDeltaTime;
            if (_hitStopLeft > 0f) return;
            _hitStopLeft = 0f;
            Time.timeScale = 1f;
        }

        /// <summary>방을 나가거나 판이 끝날 때. 늦춘 시간을 되돌린다.</summary>
        private void ClearJuice()
        {
            _shake = 0f;
            _kick = Vector2.zero;
            _shakeOffset = Vector2.zero;
            if (_hitStopLeft > 0f) { _hitStopLeft = 0f; Time.timeScale = 1f; }
            ClearBigJuice();
        }

        // ── 어느 타격이 얼마나 흔드는가 ──────────────────────────
        //
        // 세기를 **피해량이 아니라 사건의 종류**로 정한다. 피해량에 비례시키면
        // 후반 챕터에서 평타 하나가 화면을 뒤흔든다 — 수치는 계속 커지기 때문이다.

        private const float ShakeOnHit = 1.6f;      // 평타. 있는지 없는지 모를 정도
        // 치명타는 떨림(4.5 px)에서 반동(`CritKick` 2.5 px, 0.35초 간격)으로 바꿨다 — 연사에서 어지러웠다(2026-10-07)
        private const float ShakeOnKill = 3.0f;     // 잡았을 때
        private const float ShakeOnBossHurt = 5.5f; // 보스를 때렸을 때
        private const float ShakeOnPlayerHurt = 4f; // 내가 맞았을 때

        private const float HitStopOnCrit = 0.09f;   // 0.06 → 0.09 — 치명타 「팍」(PD 2026-10-07)
        private const float HitStopOnBossKill = 0.18f;

        // ── 업그레이드 번쩍임 ────────────────────────────────────

        /// <summary>
        /// 능력이 오르는 순간(레벨업 카드·제단) 몸에서 빛이 퍼진다.
        /// 무엇이 좋아졌는지는 창이 말해 주므로, 여기서는 **일어났다는 사실**만 알린다.
        /// </summary>
        private void PlayUpgradeFx()
        {
            var me = Avatar;
            if (me == null) return;
            PlayFx("burst", me.Position, 190f, loop: false);
            SpawnImpact(me.Position, "pulse");
            Shake(2.5f);
        }
    }
}

