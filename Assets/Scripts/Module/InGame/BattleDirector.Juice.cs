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
            if (_shake <= 0f)
            {
                if (_shakeOffset != Vector2.zero) { _shakeOffset = Vector2.zero; ApplyScroll(); }
                return;
            }

            _shake = Mathf.Max(0f, _shake - ShakeDecayPerSecond * dt);
            // 매 프레임 방향을 새로 뽑는다. 한 축으로만 떨면 흔들림이 아니라 미끄러짐이다.
            float a = (float)_rng.NextDouble() * Mathf.PI * 2f;
            _shakeOffset = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * _shake;
            ApplyScroll();
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
            _shakeOffset = Vector2.zero;
            if (_hitStopLeft > 0f) { _hitStopLeft = 0f; Time.timeScale = 1f; }
        }

        // ── 어느 타격이 얼마나 흔드는가 ──────────────────────────
        //
        // 세기를 **피해량이 아니라 사건의 종류**로 정한다. 피해량에 비례시키면
        // 후반 챕터에서 평타 하나가 화면을 뒤흔든다 — 수치는 계속 커지기 때문이다.

        private const float ShakeOnHit = 1.6f;      // 평타. 있는지 없는지 모를 정도
        private const float ShakeOnCrit = 4.5f;     // 치명타
        private const float ShakeOnKill = 3.0f;     // 잡았을 때
        private const float ShakeOnBossHurt = 5.5f; // 보스를 때렸을 때
        private const float ShakeOnPlayerHurt = 4f; // 내가 맞았을 때

        private const float HitStopOnCrit = 0.06f;
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
