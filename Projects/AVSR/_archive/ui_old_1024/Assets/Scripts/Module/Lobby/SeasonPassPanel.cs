using System;
using System.Collections.Generic;
using Game.Module.Common;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.Module.Events;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 시즌 패스 — 위 막대 시즌 배지로 연다(기획서 B-6, 시안 S16 보상 · S17 미션). 단계 30줄은 스크롤.
    /// 노드는 `SeasonPassBuilder` 가 세운다.
    /// </summary>
    public sealed class SeasonPassPanel : MonoBehaviour
    {
        [SerializeField] private Sprite _gem, _gold, _essence, _ticket, _stamina, _chestSilver, _chestGold, _chestPlatinum, _frame, _title;
        [SerializeField] private LobbyShopPanel.KeyedSprite[] _missionIcons = Array.Empty<LobbyShopPanel.KeyedSprite>();
        [SerializeField] private Sprite _tab, _tabSelected;

        private const int Rows = 30;
        private const int MissionRows = 8;

        private static readonly Color Gold = new(1f, 0.85f, 0.32f);

        private UIBinder _ui;
        private IDisposable _token;
        private bool _missions;
        private float _tick;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            _ui.OnClick("PassTabRewards", () => ShowTab(false));
            _ui.OnClick("PassTabMissions", () => ShowTab(true));
            _ui.OnClick("PassClaimAll", ClaimAll);
            _ui.OnClick("PassBuyPremium", () => Buy("pass_premium"));
            _ui.OnClick("PassBuyBundle", () => Buy("pass_bundle"));
            _ui.OnClick("PassClose", Close);
            for (int i = 0; i < Rows; i++)
            {
                int level = i + 1;
                _ui.OnClick($"PassRow{i}FreeClaim", () => Claim(level, false));
                _ui.OnClick($"PassRow{i}PremClaim", () => Claim(level, true));
            }
            for (int k = 0; k < 2; k++)
                for (int i = 0; i < MissionRows; i++)
                {
                    int kk = k, ii = i;
                    _ui.OnClick($"Mission{k}_{i}Claim", () => ClaimMission(kk, ii));
                }
        }

        private void OnEnable()
        {
            if (CoreModule.TryGet<IEventBus>(out var bus)) _token = bus.Subscribe<SeasonChangedEvent>(_ => Refresh());
        }

        private void OnDisable()
        {
            _token?.Dispose();
            _token = null;
        }

        private void Update()
        {
            _tick += Time.unscaledDeltaTime;
            if (_tick < 1f) return;
            _tick = 0f;
            RefreshTimes();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            var nav = transform.parent != null ? transform.parent.Find("BottomNav") : null;
            if (nav != null && transform.GetSiblingIndex() > nav.GetSiblingIndex()) transform.SetSiblingIndex(nav.GetSiblingIndex());
            ShowTab(false);
        }

        public void Close() => gameObject.SetActive(false);

        private void ShowTab(bool missions)
        {
            _missions = missions;
            SetTab("PassTabRewards", !missions);
            SetTab("PassTabMissions", missions);
            _ui.SetActive("PassRewardsPage", !missions);
            _ui.SetActive("PassMissionsPage", missions);
            Refresh();
        }

        private void SetTab(string node, bool on)
        {
            var img = _ui.Get<Image>(node);
            if (img != null) img.sprite = on ? _tabSelected : _tab;
            var t = _ui.Find(node);
            var label = t != null ? t.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (label != null) label.color = on ? new Color(1f, 0.85f, 0.32f) : Color.white;  // 고른 탭은 금테 · 어두운 속 — 시안처럼 금색 글자
        }

        // ── 그리기 ───────────────────────────────────────────

        private void Refresh()
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s) || !s.IsReady) return;
            _ui.SetText("PassSeasonText", "SEASON 1");
            _ui.SetText("PassNameText", "GHOST REVENGE");
            _ui.SetText("PassSloganText", Localize.FromTable("ui.pass.slogan", "지금, 복수의 시간이 시작된다."));
            _ui.SetText("PassLevelText", $"Lv {s.Tier}");
            _ui.SetText("PassXpText", s.Tier >= s.TierMax ? "MAX" : $"{s.XpInTier} / {s.XpPerTier}");
            var fill = _ui.Get<Image>("PassXpFill");
            if (fill != null) fill.fillAmount = s.Tier >= s.TierMax ? 1f : (float)s.XpInTier / s.XpPerTier;
            _ui.SetText("PassTabRewardsText", Localize.FromTable("ui.pass.tab.rewards", "보상"));
            _ui.SetText("PassTabMissionsText", Localize.FromTable("ui.pass.tab.missions", "미션"));
            _ui.SetText("PassClaimAllText", Localize.FromTable("ui.pass.claim_all", "모두 받기"));
            _ui.SetActive("PassMissionDot", s.HasClaimableMission);
            _ui.SetActive("PassBuyPremium", !s.HasPremium);
            _ui.SetActive("PassBuyBundle", !s.HasPremium);
            if (CoreModule.TryGet<IStoreService>(out var store))
            {
                var a = store.Find("pass_premium");
                var b = store.Find("pass_bundle");
                _ui.SetText("PassBuyPremiumText", Localize.FromTable("ui.pass.premium", "프리미엄 열기") + (a != null ? $"\n{store.PriceLabel(a)}" : string.Empty));
                _ui.SetText("PassBuyBundleText", Localize.FromTable("ui.pass.premium_plus", "프리미엄 + 15단계") + (b != null ? $"\n{store.PriceLabel(b)}" : string.Empty));
            }
            RefreshTimes();
            if (_missions) RefreshMissions(s);
            else RefreshRewards(s);
        }

        private void RefreshTimes()
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s)) return;
            var t = s.UntilEnd;
            _ui.SetText("PassTimeText", string.Format(Localize.FromTable("ui.pass.left", "남은 {0}일 {1}시간"), t.Days, t.Hours));
            var d = s.UntilDayReset;
            var w = s.UntilWeekReset;
            _ui.SetText("MissionDailyHead", Localize.FromTable("ui.pass.daily", "매일") + "  <color=#FFD94A>· "
                + string.Format(Localize.FromTable("ui.pass.reset_in", "초기화까지 {0}"), $"{(int)d.TotalHours:00}:{d.Minutes:00}:{d.Seconds:00}") + "</color>");
            _ui.SetText("MissionWeeklyHead", Localize.FromTable("ui.pass.weekly", "매주") + "  <color=#FFD94A>· "
                + string.Format(Localize.FromTable("ui.pass.reset_days", "초기화까지 {0}일"), Math.Max(1, (int)Math.Ceiling(w.TotalDays))) + "</color>");
        }

        private void RefreshRewards(ISeasonService s)
        {
            _ui.SetText("PassHeadTier", Localize.FromTable("ui.pass.tier", "단계"));
            _ui.SetText("PassHeadFree", Localize.FromTable("ui.pass.free", "무료 보상"));
            _ui.SetText("PassHeadPremium", Localize.FromTable("ui.pass.premium_rewards", "프리미엄 보상"));
            var tiers = s.Tiers;
            for (int i = 0; i < Rows; i++)
            {
                var row = _ui.Find($"PassRow{i}");
                if (row == null) continue;
                bool on = i < tiers.Count;
                row.gameObject.SetActive(on);
                if (!on) continue;
                int level = i + 1;
                Txt(row, "TierText", level.ToString());
                bool reached = level <= s.Tier;
                Act(row, "TierNow", level == Math.Max(1, s.Tier));
                Cell(row, "Free", tiers[i].Free, reached, s.IsClaimed(level, false), true, s.CanClaim(level, false));
                Cell(row, "Prem", tiers[i].Premium, reached, s.IsClaimed(level, true), s.HasPremium, s.CanClaim(level, true));
            }
        }

        private void Cell(Transform row, string side, string rewards, bool reached, bool claimed, bool unlocked, bool claimable)
        {
            var (icon, name, amount) = First(rewards);
            var img = Img(row, $"{side}Icon");
            if (img != null) { img.sprite = icon; img.enabled = icon != null; img.preserveAspect = true; }
            Txt(row, $"{side}Name", name);
            Txt(row, $"{side}Amount", amount);
            Act(row, $"PassRow{row.name.Substring(7)}{side}Claim", claimable);
            Act(row, $"{side}Check", claimed);
            Act(row, $"{side}Lock", !unlocked);
            Act(row, $"{side}Glow", claimable);
            Txt(row, $"{side}ClaimText", Localize.FromTable("ui.pass.claim", "받기"));
            Txt(row, $"{side}LockText", Localize.FromTable("ui.pass.locked", "잠김"));
        }

        private void RefreshMissions(ISeasonService s)
        {
            for (int k = 0; k < 2; k++)
            {
                var list = s.Missions(k == 0 ? MissionPeriod.Daily : MissionPeriod.Weekly);
                for (int i = 0; i < MissionRows; i++)
                {
                    var row = _ui.Find($"Mission{k}_{i}");
                    if (row == null) continue;
                    bool on = i < list.Count;
                    row.gameObject.SetActive(on);
                    if (!on) continue;
                    var m = list[i];
                    var icon = Img(row, "MIcon");
                    if (icon != null) { icon.sprite = Keyed(_missionIcons, m.Def.Type); icon.enabled = icon.sprite != null; }
                    Txt(row, "MName", Localize.Get($"mission.{m.Def.Type}"));
                    bool all = m.Def.Type.EndsWith("_all_done");
                    Act(row, "MBar", !all);
                    Txt(row, "MCount", all ? string.Empty : $"{Math.Min(m.Progress, m.Def.Target)} / {m.Def.Target}");
                    var fill = Img(row, "MFill");
                    if (fill != null) fill.fillAmount = m.Def.Target > 0 ? Mathf.Clamp01((float)m.Progress / m.Def.Target) : 0f;
                    string reward = all ? Describe(m.Def.Rewards)
                                        : string.Format(Localize.FromTable("ui.pass.xp", "패스 경험치 +{0}"), m.Def.PassXp);
                    Txt(row, "MReward", reward);
                    var rewardIcon = Img(row, "MRewardIcon");
                    if (rewardIcon != null) rewardIcon.sprite = all ? _gem : _essence;
                    Act(row, $"Mission{k}_{i}Claim", m.Claimable);
                    Act(row, "MDoing", !m.Claimable && !m.Claimed && !m.Locked);
                    Act(row, "MDone", m.Claimed);
                    Act(row, "MLock", m.Locked);
                    Txt(row, "MClaimText", Localize.FromTable("ui.pass.claim", "받기"));
                    Txt(row, "MDoingText", Localize.FromTable("ui.pass.doing", "진행 중"));
                    Txt(row, "MDoneText", Localize.FromTable("ui.pass.claimed", "받음"));
                    Txt(row, "MLockText", Localize.FromTable("ui.pass.locked", "잠김"));
                }
            }
        }

        // ── 받기 · 사기 ───────────────────────────────────────

        private void Claim(int level, bool premium)
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s)) return;
            if (s.TryClaim(level, premium, out var r)) { GameSound.Cue("run.gold"); Toast(r); }
        }

        private void ClaimAll()
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s)) return;
            bool any = false;
            if (_missions)
            {
                for (int k = 0; k < 2; k++)
                    foreach (var m in s.Missions(k == 0 ? MissionPeriod.Daily : MissionPeriod.Weekly))
                        if (m.Claimable && s.TryClaimMission(m.Def.Id, out _)) any = true;
            }
            else any = s.TryClaimAll(out _);
            if (any) GameSound.Cue("run.gold");
            Refresh();
        }

        private void ClaimMission(int k, int i)
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s)) return;
            var list = s.Missions(k == 0 ? MissionPeriod.Daily : MissionPeriod.Weekly);
            if (i >= list.Count) return;
            if (s.TryClaimMission(list[i].Def.Id, out var r)) { GameSound.Cue("run.gold"); Toast(r); }
        }

        private void Buy(string productId)
        {
            if (!CoreModule.TryGet<IStoreService>(out var store)) return;
            var p = store.Find(productId);
            if (p == null) return;
            SystemPopup.Show(string.Format(Localize.FromTable("ui.pass.buy_confirm", "{0}\n{1} 로 살까요?"),
                    Localize.Get(p.NameKey), store.PriceLabel(p)),
                () => BuyAsync(productId).Forget(),   // fire-and-forget: 결과는 패스 화면이 다시 그린다
                Localize.FromTable("ui.store.confirm.title", "구매"), Localize.Get("ui.common.cancel"));
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid BuyAsync(string id)
        {
            if (!CoreModule.TryGet<IStoreService>(out var store)) return;
            var (outcome, _) = await store.BuyAsync(id);
            if (outcome == PurchaseOutcome.Granted) GameSound.Cue("run.card");
            Refresh();
        }

        private void Toast(RewardResult r)
        {
            if (r == null || r.Lines.Count == 0) return;
            var parts = new List<string>();
            foreach (var l in r.Lines)
                if (l.Kind != RewardKind.PassXp) parts.Add(Label(l));
            if (parts.Count > 0)
                SystemPopup.Show(Localize.FromTable("ui.store.reward.title", "획득!") + "\n" + string.Join("\n", parts), null,
                    Localize.Get("ui.common.ok"), null);
        }

        // ── 보상 글 · 그림 ───────────────────────────────────

        private (Sprite, string, string) First(string rewards)
        {
            if (string.IsNullOrEmpty(rewards)) return (null, string.Empty, string.Empty);
            var part = rewards.Split(';')[0];
            int c = part.IndexOf(':');
            string kind = c < 0 ? part : part.Substring(0, c), val = c < 0 ? string.Empty : part.Substring(c + 1);
            int.TryParse(val, out var n);
            switch (kind)
            {
                case "gem": return (_gem, Localize.FromTable("ui.store.r.gem", "젬"), n.ToString("N0"));
                case "gold": return (_gold, Localize.FromTable("ui.store.r.gold", "골드"), n.ToString("N0"));
                case "goldrun": return (_gold, Localize.FromTable("ui.store.r.gold", "골드"), string.Format(Localize.FromTable("ui.pass.goldrun", "{0}판분"), n));
                case "essence": return (_essence, Localize.FromTable("ui.store.r.essence", "정수"), n.ToString());
                case "ticket": return (_ticket, Localize.FromTable("ui.store.r.ticket", "소환권"), n.ToString());
                case "stamina": return (_stamina, Localize.FromTable("ui.store.r.stamina", "행동력"), n.ToString());
                case "chest":
                    string key = val.Split('*')[0];
                    var art = key == "gold" ? _chestGold : key == "platinum" ? _chestPlatinum : _chestSilver;
                    return (art, Localize.Get($"chest.{key}.name"), val.Contains("*") ? val.Split('*')[1] : "1");
                case "profile":
                    bool frame = val.StartsWith("frame");
                    return (frame ? _frame : _title, Localize.Get($"profile.{val}.name"), string.Empty);
            }
            return (null, part, string.Empty);
        }

        private string Describe(string rewards)
        {
            var parts = new List<string>();
            foreach (var p in (rewards ?? string.Empty).Split(';'))
            {
                if (p.Length == 0) continue;
                var (_, name, amount) = First(p);
                parts.Add($"{name} {amount}");
            }
            return string.Join(" · ", parts);
        }

        private static string Label(RewardLine l) => l.Kind switch
        {
            RewardKind.Gem => $"{Localize.FromTable("ui.store.r.gem", "젬")} {l.Amount:N0}",
            RewardKind.Gold => $"{Localize.FromTable("ui.store.r.gold", "골드")} {l.Amount:N0}",
            RewardKind.Essence => $"{Localize.FromTable("ui.store.r.essence", "정수")} {l.Amount}",
            RewardKind.Ticket => $"{Localize.FromTable("ui.store.r.ticket", "소환권")} {l.Amount}",
            RewardKind.Stamina => $"{Localize.FromTable("ui.store.r.stamina", "행동력")} {l.Amount}",
            RewardKind.Chest => Localize.Get($"chest.{l.Key}.name"),
            RewardKind.Profile => Localize.Get($"profile.{l.Key}.name"),
            _ => string.Empty,
        };

        private static Sprite Keyed(LobbyShopPanel.KeyedSprite[] list, string key)
        {
            if (list == null || string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < list.Length; i++) if (list[i].Key == key) return list[i].Sprite;
            return null;
        }

        private Image Img(Transform root, string name)
        {
            var t = _ui.Find(root, name);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private void Txt(Transform root, string name, string value)
        {
            var t = _ui.Find(root, name);
            var tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
            if (tmp != null) tmp.text = value;
        }

        private void Act(Transform root, string name, bool on)
        {
            var t = _ui.Find(root, name);
            if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }
    }
}
