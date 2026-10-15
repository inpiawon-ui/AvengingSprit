using System;
using Game.Module.Common;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.User;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 행동력 부족 · 채우기 알림 — 도전하기(모자랄 때)와 위 막대 「+」 가 같이 쓴다.
    /// 정식 창(`StaminaShortPopup`, 기획서 B-12)은 상점 단계에서 만든다. 그때까지 공통 알림(`SystemPopup`)으로 띄운다.
    /// </summary>
    public static class StaminaPopup
    {
        /// <param name="shortOnly">true 면 「부족해요」 줄을 앞에 붙인다(도전하기에서 막혔을 때).</param>
        public static void Show(IStaminaService st, IPlayerDataService player, bool shortOnly, Action onRefilled)
        {
            if (st == null || player == null) return;
            var until = st.UntilNext;
            string next = until > TimeSpan.Zero ? $"{(int)until.TotalMinutes:00}:{until.Seconds:00}" : "—";
            string msg = string.Format(Localize.FromTable("ui.stamina.short", "행동력이 부족해요\n필요 {0} · 보유 {1} · 다음 회복 {2}"),
                                       st.PerRun, st.Current, next);
            if (!shortOnly)
            {
                // 「+」 로 열었으면 첫 줄(부족해요)은 빼고 보유 · 다음 회복만
                int cut = msg.IndexOf('\n');
                if (cut >= 0) msg = msg.Substring(cut + 1);
            }

            int left = st.RefillLeftToday;
            int perDay = player.Config != null ? player.Config.StaminaRefillPerDay : 6;
            if (left <= 0)
            {
                SystemPopup.Show(msg + "\n" + Localize.FromTable("ui.stamina.refill_none", "오늘 젬 채우기를 다 썼어요"),
                                 null, Localize.Get("ui.common.ok"), null);
                return;
            }
            msg += "\n" + string.Format(Localize.FromTable("ui.stamina.refill_line", "젬 {0} 으로 {1} 채우기 · 오늘 {2}/{3}"),
                                        st.RefillGemCost, st.Max, perDay - left, perDay);
            if (player.Gem < st.RefillGemCost)
            {
                SystemPopup.Show(msg + "\n" + Localize.FromTable("ui.stamina.fail.gem", "젬이 모자라요"),
                                 null, Localize.Get("ui.common.ok"), null);
                return;
            }
            SystemPopup.Show(msg, () =>
            {
                if (st.TryRefillWithGem(out _)) GameSound.Cue("ui.select");
                onRefilled?.Invoke();
            }, string.Format(Localize.FromTable("ui.stamina.refill_btn", "젬 {0} 채우기"), st.RefillGemCost),
               Localize.Get("ui.common.cancel"));
        }
    }
}
