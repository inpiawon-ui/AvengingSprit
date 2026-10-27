using System;
using Game.Character;
using Game.Module.Common;
using Game.Module.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// RE:BORN 이벤트 판 — 「내준다 / 받는다」 두 줄(PD 2026-10-27 「지금 시안이 좋은데 시안대로」 · 시안 _exchange/mock_altar/mock_event_deal.png).
    /// 이벤트 23종은 대가 4가지(고스트 체력 · 호스트 체력 · 최대 체력 판 내내 · 먼저 싸우기) × 보상 9가지에 확률 · 거절 보상이 섞인다 —
    /// 이벤트마다 모양을 따로 만들지 않고 이 두 줄에 채운다. 옛 판은 받는 것만 한 줄로 적어 무엇을 내주는지가 안 보였다.
    /// 노드 · 자리는 RbInGameBuilder.BuildEvent.
    /// </summary>
    public sealed partial class InGameMainUI
    {
        /// <summary>거래 줄 아이콘(키트 rb_icon_*_96) — 이름으로 찾는다.</summary>
        [SerializeField] private Sprite[] _rbDealIcons = Array.Empty<Sprite>();

        // 빌더 자리(RbInGameBuilder.BuildEvent)와 같은 수 — 대가가 없으면 받는다 줄 · 버튼을 위로 당긴다
        private const float DealGiveTop = 644f, DealRowH = 124f, DealGap = 12f, DealNoCostH = 68f;
        private const string GiveRed = "#FF8A9C";
        private static readonly Color DealTitle = Color.white, DealDim = new(0.62f, 0.69f, 0.82f);

        private void RbFillDeal(EventOfferEvent e)
        {
            foreach (var n in new[] { "EventRewardPill", "EventRewardText", "EventCostPill", "EventCostText" }) _ui.SetActive(n, false);

            bool hasCost = e.CostKind != (int)EventCost.None || e.FightCount > 0;
            FillGive(e, hasCost);
            FillGet(e);

            // 대가가 없으면 내준다 줄을 낮게 · 그만큼 아래를 당긴다
            float giveH = hasCost ? DealRowH : DealNoCostH;
            SetRow("EventGiveRow", DealGiveTop, giveH);
            float getTop = DealGiveTop + giveH + DealGap;
            SetRow("EventGetRow", getTop, DealRowH);
            float buttons = getTop + DealRowH + 16f;
            SetY("EventAcceptButton", buttons + 40f);
            SetY("EventResultText", buttons + 40f);
            SetY("EventDeclineButton", buttons + 136f);

            // 거절해도 받는 것은 「지나간다」 안에(시안 「지나간다 · 골드 +10」)
            string decline = string.IsNullOrEmpty(e.DeclineLabel) ? Localize.Get("ui.ingame.event.decline") : e.DeclineLabel;
            if (!string.IsNullOrEmpty(e.DeclineRewardLabel)) decline += $" <size=75%><color=#6AF0C0>· {e.DeclineRewardLabel}</color></size>";
            _ui.SetText("EventDeclineText", decline);

            if (_ui.Get<Button>("EventCloseButton") is Button close)
            {
                close.onClick.RemoveAllListeners();
                close.onClick.AddListener(() => Resolve(false));
            }
        }

        private void FillGive(EventOfferEvent e, bool hasCost)
        {
            var key = _ui.Find("EventGiveKey") as RectTransform;
            if (key != null) key.sizeDelta = new Vector2(hasCost ? 68f : 112f, key.sizeDelta.y);
            _ui.SetText("EventGiveKeyText", Localize.Get(hasCost ? "ui.rb.event.give" : "ui.rb.event.give_flat"));
            _ui.SetText("EventGetKeyText", Localize.Get("ui.rb.event.get"));
            _ui.SetActive("EventGiveIcon", hasCost);
            _ui.SetActive("EventGiveSub", hasCost);
            _ui.SetActive("EventGiveTag", false);
            _ui.SetActive("EventGiveCands", false);
            var title = _ui.Get<TextMeshProUGUI>("EventGiveTitle");
            if (!hasCost)
            {
                _ui.SetText("EventGiveTitle", Localize.Get("ui.rb.event.no_cost"));
                if (title != null)
                {
                    title.color = DealDim;
                    // 낮은 줄 — 띠 오른쪽에 붙여 가운데 높이로
                    var rt = title.rectTransform;
                    rt.anchoredPosition = new Vector2(124f + rt.sizeDelta.x * 0.5f, -DealNoCostH * 0.5f);
                }
                return;
            }
            if (title != null)
            {
                title.color = DealTitle;
                var rt = title.rectTransform;
                rt.anchoredPosition = new Vector2(180f + rt.sizeDelta.x * 0.5f, -38f);
            }

            var cost = (EventCost)e.CostKind;
            if (cost == EventCost.Card)
            {
                // 부서진 제단(B안) — 부서질 수 있는 카드를 아이콘으로 · 「무작위」 표
                _ui.SetText("EventGiveTitle", Localize.Get("ui.rb.event.give.card"));
                SetIcon("EventGiveIcon", "sys_warning");
                bool any = e.BreakKeys != null && e.BreakKeys.Length > 0;
                _ui.SetActive("EventGiveSub", !any);
                if (!any) _ui.SetText("EventGiveSub", $"<color={GiveRed}>{e.BlockedReason}</color>");
                _ui.SetActive("EventGiveCands", any);
                for (int i = 0; i < 8; i++)
                {
                    bool on = any && i < e.BreakKeys.Length;
                    _ui.SetActive($"EventGiveCand{i}", on);
                    if (on) SetIcon($"EventGiveCand{i}Icon", CardIconOf(e.BreakKeys[i]));
                }
                Tag("EventGiveTag", Localize.Get("ui.rb.event.random"), new Color(0.56f, 0.95f, 1f));
                return;
            }
            string fight = e.FightCount <= 0 ? string.Empty
                : Localize.Format(e.FightElite ? "ui.rb.event.give.fight_elite" : "ui.rb.event.give.fight", e.FightCount);
            string head = cost switch
            {
                EventCost.GhostHp => Localize.Format("ui.rb.event.give.ghost_hp", e.CostValue),
                EventCost.HostHp => Localize.Format("ui.rb.event.give.host_hp", e.CostValue),
                EventCost.MaxHp => Localize.Format("ui.rb.event.give.max_hp", e.CostValue),
                EventCost.Gold => Localize.Format("ui.rb.event.give.gold", e.CostValue),
                _ => fight,
            };
            _ui.SetText("EventGiveTitle", head);
            SetIcon("EventGiveIcon", cost switch
            {
                EventCost.GhostHp => "heal_ghost",
                EventCost.HostHp => "heal_host",
                EventCost.MaxHp => "stat_hp",
                EventCost.Gold => "currency_gold",
                _ => e.FightElite ? "room_elite" : "room_battle",
            });

            // 아래 줄 — 못 고르는 이유가 먼저 · 지금 → 낸 뒤 · 판 내내 · 싸움
            string sub;
            if (!string.IsNullOrEmpty(e.BlockedReason)) sub = $"<color={GiveRed}>{e.BlockedReason}</color>";
            else if (cost == EventCost.MaxHp) sub = Localize.Get("ui.rb.event.max_hp_sub");
            else if (e.HpNow >= 0) sub = Localize.Format("ui.rb.event.now", e.HpNow, e.HpAfter);
            else sub = Localize.Get("ui.rb.event.win_get");
            // 대가와 싸움이 같이 있으면(교차사격) 아래 줄에 싸움을 붙인다
            if (cost != EventCost.None && e.FightCount > 0) sub += $" · {fight}";
            _ui.SetText("EventGiveSub", sub);
            if (cost == EventCost.MaxHp) Tag("EventGiveTag", Localize.Get("ui.rb.event.run_long"), new Color(1f, 0.54f, 0.61f));
        }

        private void FillGet(EventOfferEvent e)
        {
            var kind = (EventReward)e.RewardKind;
            bool up = kind == EventReward.UpgradeCard;
            string title = up ? Localize.Get("ui.rb.event.get.upgrade")
                         : kind == EventReward.ReforgeCard ? Localize.Get("ui.rb.event.get.reforge")
                         : e.RewardLabel;
            if (e.ExtraGold > 0) title += " + " + Localize.Format("ui.event.gold", e.ExtraGold);
            _ui.SetText("EventGetTitle", title);

            // 강화는 오를 카드 그림 — 제단은 무작위가 아니라 레벨이 가장 낮은 카드를 벼린다(BattleDirector.PickUpgradable)
            var upIcon = up && !string.IsNullOrEmpty(e.UpgradeKey) ? CardIconOf(e.UpgradeKey) : null;
            if (upIcon != null) SetIcon("EventGetIcon", upIcon);
            else SetIcon("EventGetIcon", kind switch
            {
                EventReward.UpgradeCard or EventReward.ReforgeCard => "card_up",
                EventReward.HostHeal => "heal_host",
                EventReward.GhostHeal => "heal_ghost",
                EventReward.Gold => "currency_gold",
                EventReward.PossessReach => "possess_range",
                EventReward.ShopDiscount => "shop_discount",
                EventReward.BossShieldBreak => "boss_shield",
                EventReward.SpawnHost => "mode_ghost",
                _ => "reward_chest",
            });

            string sub = kind switch
            {
                EventReward.UpgradeCard => string.IsNullOrEmpty(e.UpgradeKey)
                    ? Localize.Get("ui.event.result.nothing_to_upgrade")
                    : Localize.Format("ui.rb.event.get.upgrade_sub", e.UpgradeName, e.UpgradeLevel, e.UpgradeLevel + 1),
                EventReward.ReforgeCard => Localize.Get("ui.rb.event.get.reforge_sub"),
                EventReward.CardOffer => Localize.Get("ui.rb.event.get.offer_sub"),
                EventReward.CardGrant => Localize.Get("ui.rb.event.get.grant_sub"),
                _ => string.Empty,
            };
            if (e.ChancePercent > 0) sub = Localize.Get("ui.rb.event.get.fail_sub");
            _ui.SetText("EventGetSub", sub);
            _ui.SetActive("EventGetSub", !string.IsNullOrEmpty(sub));
            // 아래 줄이 없으면 제목을 줄 가운데 높이로
            if (_ui.Get<TextMeshProUGUI>("EventGetTitle") is TextMeshProUGUI t)
                t.rectTransform.anchoredPosition = new Vector2(t.rectTransform.anchoredPosition.x, string.IsNullOrEmpty(sub) ? -DealRowH * 0.5f : -38f);
            if (e.ChancePercent > 0) Tag("EventGetTag", $"{e.ChancePercent}%", new Color(1f, 0.79f, 0.27f));
            else _ui.SetActive("EventGetTag", false);
        }

        private void SetRow(string name, float top, float h)
        {
            if (_ui.Find(name) is not RectTransform rt) return;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -(top + h * 0.5f));
        }

        private void SetY(string name, float cy)
        {
            if (_ui.Find(name) is RectTransform rt) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -cy);
        }

        private void SetIcon(string node, string iconName) => SetIcon(node, DealIcon(iconName));

        private void SetIcon(string node, Sprite sprite)
        {
            if (_ui.Get<Image>(node) is not Image img) return;
            img.sprite = sprite;
            img.enabled = sprite != null;
        }

        private Sprite DealIcon(string name)
        {
            string full = $"rb_icon_{name}_96";
            for (int i = 0; i < _rbDealIcons.Length; i++)
                if (_rbDealIcons[i] != null && _rbDealIcons[i].name == full) return _rbDealIcons[i];
            return null;
        }

        /// <summary>오른쪽 위 표 — 글자 폭에 맞춰 판을 늘린다.</summary>
        private void Tag(string node, string text, Color c)
        {
            _ui.SetActive(node, true);
            if (_ui.Get<TextMeshProUGUI>(node + "Text") is not TextMeshProUGUI t) return;
            t.text = text;
            t.color = c;
            if (_ui.Find(node) is RectTransform rt)
                rt.sizeDelta = new Vector2(Mathf.Max(96f, t.GetPreferredValues(text, 9999f, 0f).x + 24f), rt.sizeDelta.y);   // 글꼴에 막 들어온 글자는 폭이 0 으로 재진다
        }
    }
}
