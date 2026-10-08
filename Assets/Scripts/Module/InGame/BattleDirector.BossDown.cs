using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 최종 보스를 잡은 뒤의 순서 — 막타 슬로우 → 풀리면 큰 폭발 → 잠깐 뒤 결과 팝업(PD 2026-10-08).
    ///
    /// 예전에는 보스가 죽는 프레임에 방이 비어 곧바로 `Finish(true)` 가 불렸다. 슬로우(0.18초)는
    /// 결과 창이 열리면서 끊기고, 폭발 · 먼지와 결과 창이 한 화면에 겹쳐 「같이 이상하게 뜬다」였다.
    /// 이제 슬로우가 끝까지 흐르고, 슬로우가 풀리는 순간 폭발이 터지고, 폭발이 다 보인 뒤 창이 열린다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>막타 슬로우(실제 시간, 초). 짧으면 「느려졌나?」 하고 지나간다.</summary>
        private const float BossDownSlowSeconds = 0.6f;
        /// <summary>폭발이 터진 뒤 결과 창까지(초).</summary>
        private const float BossDownPopupDelay = 0.9f;   // 0.55 → 0.9 — 폭발이 다 안 걷힌 채 결과 창이 떴다(2026-10-08 녹화)
        private const float BossDownBurstSize = 320f;

        private Vector2 _bossDownAt;
        private bool _bossDownPending;
        /// <summary>
        /// 순서가 이미 돌고 있다. ⚠ 보스방이 비면 출구를 열 때까지 `OnRoomCleared` 가 **매 프레임** 다시 불린다 —
        /// 이 표시가 없으면 다음 프레임 호출이 기다림 없이 `Finish` 로 가 결과 창이 곧바로 떴다(2026-10-08 녹화 2챕터).
        /// </summary>
        private bool _bossDownFinishing;

        /// <summary>보스가 쓰러진 자리를 적어 둔다 — 폭발은 슬로우가 풀린 뒤 이 자리에서 터진다.</summary>
        private void NoteBossDown(Unit boss)
        {
            _bossDownAt = boss.Position;
            _bossDownPending = true;
            _bossDownFinishing = false;
            HitStop(BossDownSlowSeconds);
        }

        private async UniTaskVoid FinishAfterBossDownAsync()
        {
            if (_bossDownFinishing) return;
            _bossDownFinishing = true;
            var token = this.GetCancellationTokenOnDestroy();
            if (_bossDownPending)
            {
                _bossDownPending = false;
                // 슬로우는 실제 시간으로 흐른다(`TickHitStop`) — 끝날 때까지 실제 시간으로 기다린다
                bool cancelled = await UniTask.Delay(TimeSpan.FromSeconds(BossDownSlowSeconds), ignoreTimeScale: true,
                                                     cancellationToken: token).SuppressCancellationThrow();
                if (cancelled || this == null || !_running) return;
                PlayFx("burst", _bossDownAt, BossDownBurstSize, loop: false);
                Shake(ShakeMaxPixels);
                cancelled = await UniTask.Delay(TimeSpan.FromSeconds(BossDownPopupDelay), ignoreTimeScale: true,
                                                cancellationToken: token).SuppressCancellationThrow();
                if (cancelled || this == null) return;
            }
            Finish(true);
        }
    }
}
