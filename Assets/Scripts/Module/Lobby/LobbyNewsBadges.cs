using Game.Module.Common;
using Game.Module.Common.Shop;
using Game.Module.Common.UI;
using UnityEngine;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 위 막대 시즌 배지 글자 · 아래 바 새 소식 「!」(시안 S01 · S22e, 2026-10-15).
    /// LobbyMainUI 와 따로 둔다 — 로비 화면은 여러 세션이 같이 고친다. 노드가 없으면(꽂기 전 프리팹) 아무 일 없다.
    ///
    ///   SHOP 「!」 — 오늘의 상점 무료 선물을 아직 안 받았다
    ///   SUMMON 「!」 — 오늘 무료 소환이 남았다
    ///   시즌 배지 — 「SEASON 1 · GHOST REVENGE」 · 패스 단계(패스 모듈이 붙기 전에는 Lv 1)
    /// </summary>
    public sealed class LobbyNewsBadges : MonoBehaviour
    {
        private const float TickSeconds = 1f;

        private UIBinder _ui;
        private float _tick;

        private void Awake() => _ui = new UIBinder(transform);

        private void OnEnable() => Refresh();

        private void Update()
        {
            _tick += Time.unscaledDeltaTime;
            if (_tick < TickSeconds) return;
            _tick = 0f;
            Refresh();
        }

        private void Refresh()
        {
            if (_ui == null) return;
            _ui.SetText("PassBadgeTitleText", Localize.FromTable("ui.pass.badge", "SEASON 1 · GHOST REVENGE"));
            _ui.SetText("PassBadgeLevelText", $"Lv {PassLevel}");
            _ui.SetActive("PassBadgeDot", PassClaimable);

            bool shopNews = false;
            if (GameFramework.Core.Base.CoreModule.TryGet<IDailyShopService>(out var shop) && shop.SlotCount > 0)
            {
                var s = shop.Get(0);
                shopNews = s.Kind == DailyShopKind.FreeGold && !s.Bought;
            }
            SetBadge("ShopButton", shopNews);
            SetBadge("SummonButton", FreeSummonLeft);
        }

        private static int PassLevel
            => GameFramework.Core.Base.CoreModule.TryGet<Game.Module.Common.Store.ISeasonService>(out var s) && s.IsReady ? s.Tier : 0;

        private static bool PassClaimable
            => GameFramework.Core.Base.CoreModule.TryGet<Game.Module.Common.Store.ISeasonService>(out var s) && s.IsReady
               && (s.HasClaimable || s.HasClaimableMission);
        private static bool FreeSummonLeft
            => GameFramework.Core.Base.CoreModule.TryGet<Game.Module.Common.Store.IGachaService>(out var g) && g.IsReady && g.FreeAvailable;

        private void SetBadge(string tab, bool on)
        {
            var t = _ui.Find(tab);
            var badge = t != null ? _ui.Find(t, "NavBadge") : null;
            if (badge != null && badge.gameObject.activeSelf != on) badge.gameObject.SetActive(on);
        }
    }
}
