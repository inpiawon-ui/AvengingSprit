using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 보상 연출 동안 전투를 잠깐 세운다 (PD 2026-10-08).
    ///
    /// 레벨업 카드 · 제단 · 악마의 거래 · 상점에서 무언가를 얻으면 창을 닫고 캐릭터 위에 「무엇을 얻었는지」를 보여 준다.
    /// 그동안 적이 움직이면 연출을 볼 수가 없다 — 「잠시 멈춰 있어서 연출을 제대로 보여줘」.
    /// 시간은 실제 시간(unscaled)으로 잰다. 창 · 연출이 unscaled 로 돌기 때문이다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private float _presentHoldUntil;

        /// <summary>지금부터 <paramref name="seconds"/> 동안 전투를 세운다. 더 긴 쪽이 이긴다.</summary>
        public void HoldForPresentation(float seconds)
            => _presentHoldUntil = Mathf.Max(_presentHoldUntil, Time.unscaledTime + seconds);

        /// <summary>세운 것을 바로 푼다(연출이 일찍 끝났을 때).</summary>
        public void ReleasePresentation() => _presentHoldUntil = 0f;

        /// <summary>보상 연출로 전투가 서 있는가.</summary>
        public bool IsPresentationHeld => Time.unscaledTime < _presentHoldUntil;

        private bool _shopHealPending;

        /// <summary>상점에서 산 회복을 지금 넣는다 — 산 물건이 몸에 닿는 순간. 값은 살 때 이미 치렀다.</summary>
        public void ApplyShopHeal()
        {
            if (!_shopHealPending || _shopRules == null) return;
            _shopHealPending = false;
            int before = _ghostHp;
            _ghostHp = Mathf.Min(GhostHpMax, _ghostHp + GhostHpMax * _shopRules.GhostHealPct / 100);
            if (_host != null) _host.Heal(Mathf.Max(1, _host.HpMax * _shopRules.HostHealPct / 100));
            PublishHp();
            if (_ghostHp > before) ShowHeal(Avatar != null ? Avatar.Position : Vector2.zero, _ghostHp - before);
        }
    }
}
