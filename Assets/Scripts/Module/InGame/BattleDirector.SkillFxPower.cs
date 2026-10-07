using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 파워 스킬 연출 퀄업 (2026-10-07) — 첫 샘플 코만도(수류탄) 「융단 폭격」.
    /// 시안 `Projects/AVSR/_exchange/in/mock_skill_grenade_v1.png`(던짐 → 연쇄 착탄 → 여운).
    ///
    /// PD 손질: 「포물선으로 굳이 알려 줄 필요 없다 — 그냥 날아가면 된다」 · 「여운도 투머치 — 간단하게 끝내면 된다」.
    /// 그래서 시안의 점선 궤적 · 착탄 표시 · 그을린 구덩이 · 연기 기둥은 넣지 않는다.
    ///   던짐   — 수류탄은 그대로 날아간다. 시전 때 화면 전체 섬광은 끈다(`QualityFxHosts`)
    ///   착탄   — 한쪽 끝부터 짧은 간격으로 **차례로** 떨어져, 피격과 같은 그림체의 큰 도트 폭발(`fx_dotcrit`)이 터진다 · 터질 때마다 짧은 떨림
    ///   여운   — 흙먼지 조금, 끝
    /// 폭발 그림은 시안 크기(피해 지름의 약 6할) — 피해 반경 그대로면 화면을 덮었다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>한쪽 끝부터 한 발씩 늦게 떨어지는 간격(초).</summary>
        private const float PwCarpetChainSeconds = 0.07f;
        /// <summary>
        /// 폭발 상자 = 피해 지름 × 이 값. 피해 지름(1.8 m → 약 260 px)에 그대로 맞췄더니 시안(약 110 px)보다 두 배 넘게 컸다 —
        /// 「이펙트가 너무 크다」(PD)를 따라 시안 크기 쪽으로 줄였다(녹화 비교 2026-10-07).
        /// </summary>
        private const float PwBlastBoxPerDiameter = 0.62f;
        private const float PwBlastFrameSeconds = 0.05f;
        private const float PwBlastShake = 2.5f;

        private readonly HashSet<Projectile> _pwCarpet = new();

        /// <summary>융단 폭격 한 발을 적어 둔다. <paramref name="order"/> 번째(줄의 한쪽 끝부터)일수록 늦게 떨어진다.</summary>
        private void PwCarpetThrown(Projectile shot, int order)
        {
            if (shot == null || !shot.IsLob) return;
            shot.DelayLanding(order * PwCarpetChainSeconds);
            _pwCarpet.Add(shot);
        }

        /// <summary>떨어진 융단 폭격 탄이면 큰 도트 폭발로 터뜨린다. 다뤘으면 true(예전 폭발 그림을 안 띄운다).</summary>
        private bool PwExplode(Projectile shot, Vector2 at, float radius)
        {
            // 미사일(다중 유도 · 미사일 몸) · 포탑 탄(pulse)도 내 것이면 같은 도트 폭발로 — 퀄업 13종(2026-10-07)
            bool mine = shot.FromPlayer && (shot.Kind == "missile" || shot.Kind == "pulse");
            if (!(_pwCarpet.Remove(shot) || mine) || !DotHitReady) return false;
            var im = PlayFx("dotcrit", at, radius * 2f * PwBlastBoxPerDiameter, loop: false);
            im?.SetFrameSeconds(PwBlastFrameSeconds);
            _pfx?.Puff(at, ParticleElement.Dust, 0.5f);
            Shake(PwBlastShake);
            return true;
        }

        private void ClearPw() => _pwCarpet.Clear();
    }
}
