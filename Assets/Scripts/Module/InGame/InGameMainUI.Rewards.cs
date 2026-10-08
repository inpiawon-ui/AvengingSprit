using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 보상 창 네 개의 「고른 뒤」 흐름 (PD 2026-10-08 반려 뒤 다시 짬 — memory fx-needs-story-and-purpose).
    ///
    ///   레벨업 카드 : 고른 카드가 제자리에서 커짐 · 나머지 흐려짐 → 창 닫힘 → 몸에 카드 힘이 깃듦 + 머리 위 카드 이름
    ///   회복의 제단 : 고른 칸에 포커스 → 칸에서 구슬이 고스트 몸으로 날아감 → 닿는 순간 효과 적용(HP 바가 찬다) + 힐 이펙트
    ///   악마의 거래 : 수락 → 창 닫힘 → 몸에 저주 연출 + 얻은 것(금색) · 치른 것(붉은색)
    ///   상점       : HUD 골드에서 금화가 산 칸으로 → 칸이 튕김 → 물건이 몸으로 날아감 → 창 닫힘 → 몸에 효과 + 이름
    ///
    /// 전투는 고른 순간부터 연출이 끝날 때까지 선다(BattleDirector.HoldForPresentation).
    /// </summary>
    public sealed partial class InGameMainUI
    {
        private static readonly string[] CardNodes = { "BuffCard0", "BuffCard1", "BuffCard2" };
        private static readonly string[] ShrineNodes = { "ShrineChoice0", "ShrineChoice1", "ShrineChoice2" };

        private string[] _shrineIcons;
        private string[] _shopIcons;
        private EventOfferEvent _lastOffer;
        /// <summary>연출 중에 온 다음 카드 3택1 — 끝난 뒤에 연다(포커스한 카드를 새 카드가 덮어쓰지 않게).</summary>
        private BuffOfferEvent? _pendingOffer;

        /// <summary>카드 등급색 — 카드 테두리 · 등급 글자 · 카드 흡수 이펙트가 같은 색을 쓴다.</summary>
        private static Color RarityColor(CardRarity r) => r switch
        {
            CardRarity.Rare => new Color32(92, 190, 255, 255),
            CardRarity.Epic => new Color32(190, 110, 255, 255),
            CardRarity.Legendary => new Color32(255, 200, 61, 255),
            _ => new Color32(232, 238, 230, 255),
        };

        private string TextOf(string node) => _ui.Get<TMP_Text>(node) is TMP_Text t ? t.text : string.Empty;

        private Sprite SpriteOf(string node) => _ui.Get<Image>(node) is Image i && i.enabled ? i.sprite : null;

        private static async UniTask WaitAsync(float seconds, System.Threading.CancellationToken token)
            => await UniTask.Delay(System.TimeSpan.FromSeconds(seconds), ignoreTimeScale: true, cancellationToken: token)
                            .SuppressCancellationThrow();

        private void BlockInput(string panel, bool block)
        {
            if (_ui.Find(panel) is Transform t) EnsureGroup(t).interactable = !block;
        }

        // ── 레벨업 카드 ───────────────────────────────────────────

        private async UniTaskVoid PickCardAsync(string buffKey, string slot)
        {
            if (_presenting) return;
            _presenting = true;
            var token = this.GetCancellationTokenOnDestroy();
            BlockInput("BuffChoicePanel", true);
            if (_battle != null) _battle.HoldForPresentation(4f);

            var entry = _buffTable != null ? _buffTable.Get(buffKey) : null;
            var show = new GainShow
            {
                Icon = SpriteOf(slot + "Icon"),
                Title = TextOf(slot + "Name"),
                Sub = TextOf(slot + "Desc").Replace("\n", " "),
                SubColor = entry != null ? RarityColor(entry.Rarity) : Color.white,
                Kind = GainKind.Card,
                Tint = entry != null ? RarityColor(entry.Rarity) : Color.white,
                Rarity = entry != null ? entry.Rarity : CardRarity.Common,
            };
            // 효과는 바로 넣는다 — 연출 중에 두 번 누를 수 없게 입력은 막아 두었다
            if (_battle != null) _battle.ChooseBuff(buffKey);

            // 카드 둘레로 등급색 빛살이 터진다(LevelUpFx — 시안 mock_lvpopup_free_peak)
            if (_levelUpFx != null) _levelUpFx.Pick(System.Array.IndexOf(CardNodes, slot));
            // 고른 카드는 제자리에서 커지고 나머지는 흐려진다(PD 10-08 2차 「제자리에서 커지게」).
            // 옮기지 않으니 겹칠 일이 없어 나머지도 완전히 빼지 않는다
            float home = (_ui.Find(slot) as RectTransform).anchoredPosition.x;
            await FocusAsync(slot, CardNodes, home, 0.34f, othersAlpha: 0.3f);
            await WaitAsync(0.3f, token);
            await WaitAsync(0.25f, token);   // 빛살이 다 펼쳐진 것을 보여 주고 닫는다
            SetPanel("BuffChoicePanel", false);
            if (_levelUpFx != null) _levelUpFx.HideCards();
            await WaitAsync(0.12f, token);
            await PresentGainAsync(show);
            OpenPendingOffer();
        }

        // ── 회복의 제단 ───────────────────────────────────────────

        private async UniTaskVoid PickShrineAsync(int index)
        {
            if (_presenting || _battle == null) return;
            _presenting = true;
            var token = this.GetCancellationTokenOnDestroy();
            BlockInput("ShrinePanel", true);
            _battle.HoldForPresentation(4f);

            string slot = $"ShrineChoice{index}";
            string key = _shrineIcons != null && index < _shrineIcons.Length ? _shrineIcons[index] : string.Empty;
            var kind = key switch
            {
                "shrine_full_heal" or "shrine_soul_heal" => GainKind.Heal,
                "shrine_max_hp" => GainKind.Vital,
                "shrine_atk" => GainKind.Power,
                "shrine_speed" => GainKind.Speed,
                _ => GainKind.Range,
            };
            var show = new GainShow
            {
                Icon = UiArt(key),
                Title = TextOf(slot + "Text"),
                Sub = TextOf(slot + "Desc"),
                SubColor = new Color32(159, 239, 245, 255),
                Kind = kind,
                Tint = new Color32(110, 230, 240, 255),
            };

            await FocusAsync(slot, ShrineNodes, (_ui.Find(slot) as RectTransform).anchoredPosition.x, 0.26f);

            // 고른 칸의 상징에서 회복 구슬이 몸으로 — 닿는 순간 효과가 들어간다(HP 바가 그때 찬다)
            var orb = _fx != null ? _fx.FramesOf("present_heal_orb") : null;
            // 출발 = 고른 칸 왼쪽의 상징(칸 안 GiftIcon) — 없으면 칸 가운데
            var slotT = _ui.Find(slot);
            var gift = slotT != null ? slotT.Find("GiftIcon") : null;
            var from = gift != null && EnsurePresentLayer() is RectTransform lay
                ? PopupFxPlayer.PanelPoint(lay, gift) : LayerPointOf(slot);
            var to = AvatarLayerPoint();
            const float flight = 0.62f;
            for (int i = 0; i < 6; i++)
                FlyAsync(orb, from, to + new Vector2((i - 2.5f) * 6f, 0f), 72f, i * 0.06f, flight, 120f + i * 12f,
                         Color.white, true, faceMotion: true).Forget();   // fire-and-forget: 구슬은 제 시간에 사라진다
            await WaitAsync(0.18f, token);
            SetPanel("ShrinePanel", false);
            await WaitAsync(flight - 0.18f, token);

            _battle.ChooseShrine(index);
            await PresentGainAsync(show);
            OpenPendingOffer();
        }

        // ── 악마의 거래 ───────────────────────────────────────────

        private async UniTaskVoid AcceptDevilAsync()
        {
            if (_presenting || _battle == null) return;
            _presenting = true;
            var token = this.GetCancellationTokenOnDestroy();
            BlockInput("EventPanel", true);
            _battle.HoldForPresentation(4f);

            var show = new GainShow
            {
                Icon = UiArt("obj_mark_devil"),
                Title = _lastOffer.Title,
                Sub = _lastOffer.RewardLabel,
                SubColor = new Color32(245, 230, 168, 255),
                Sub2 = _lastOffer.CostLabel,
                Sub2Color = new Color32(255, 118, 118, 255),
                Kind = GainKind.Curse,
            };
            SetPanel("EventPanel", false);
            _battle.ResolveEvent(true);
            await WaitAsync(0.2f, token);
            await PresentGainAsync(show);
            OpenPendingOffer();
        }

        // ── 상점 ─────────────────────────────────────────────────

        private async UniTaskVoid BuyAsync(int slot)
        {
            if (_presenting || _battle == null) return;
            _presenting = true;
            var token = this.GetCancellationTokenOnDestroy();
            BlockInput("ShopPanel", true);
            _battle.HoldForPresentation(4f);

            string node = $"ShopItem{slot}";
            string key = _shopIcons != null && slot < _shopIcons.Length ? _shopIcons[slot] : string.Empty;
            var kind = key == "shop_heal" ? GainKind.Heal : GainKind.Card;
            var icon = SpriteOf(node + "Icon");
            var show = new GainShow
            {
                Icon = icon,
                Title = TextOf(node + "Name"),
                Sub = TextOf(node + "Desc").Replace("\n", " "),
                SubColor = new Color32(255, 214, 120, 255),
                Kind = kind,
                Tint = new Color32(255, 200, 90, 255),
            };

            // ① 값을 치른다 — HUD 골드에서 금화가 산 칸의 값으로 날아간다(골드 숫자는 지금 줄어든다)
            var coin = SpriteOf("GoldIcon");
            var coinFrames = coin != null ? new[] { coin } : null;
            var goldAt = LayerPointOf("GoldIcon");
            var priceAt = LayerPointOf(node + "Price");
            for (int i = 0; i < 5; i++)
                FlyAsync(coinFrames, goldAt, priceAt + new Vector2((i - 2) * 5f, 0f), 30f, i * 0.06f, 0.42f, 70f,
                         Color.white, true).Forget();   // fire-and-forget: 금화는 제 시간에 사라진다
            _battle.BuyShopItem(slot);
            // 창 안 「보유 골드」도 치른 만큼 바로 줄인다 — HUD 골드와 같은 수가 보여야 한다
            _ui.SetText("ShopGoldText", Localize.Format("ui.shop.gold", _battle.RunGold));
            await WaitAsync(0.42f + 4 * 0.06f, token);

            // ② 칸이 받아서 한 번 튕긴다(OutBack)
            if (_ui.Find(node) is Transform t)
            {
                t.SetAsLastSibling();
                for (float e = 0f; e < 0.2f; e += Time.unscaledDeltaTime)
                {
                    if (this == null || t == null) return;
                    t.localScale = Vector3.one * Mathf.LerpUnclamped(1.1f, 1f, Ease.OutBack(e / 0.2f));
                    if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
                }
                t.localScale = Vector3.one;
            }

            // ③ 산 물건이 칸에서 몸으로 — 날아가는 사이 창이 닫힌다
            const float flight = 0.5f;
            if (icon != null)
                FlyAsync(new[] { icon }, LayerPointOf(node + "Icon"), AvatarLayerPoint(), 62f, 0f, flight, 140f,
                         Color.white, true).Forget();   // fire-and-forget: 물건 그림은 제 시간에 사라진다
            await WaitAsync(0.12f, token);
            SetPanel("ShopPanel", false);
            await WaitAsync(flight - 0.12f, token);
            // 닿는 순간 효과가 들어간다(회복이면 HP 바가 이때 찬다)
            _battle.ApplyShopHeal();
            await PresentGainAsync(show);
            OpenPendingOffer();
        }

        /// <summary>카드 뒤 등급색 광원 · 알갱이(LevelUpFx) — 창이 열릴 때.</summary>
        private void ShowCardFx(BuffOfferEvent e)
        {
            if (_levelUpFx == null || _buffTable == null || e.OfferedKeys == null) return;
            var cards = new RectTransform[CardNodes.Length];
            var rarities = new CardRarity[CardNodes.Length];
            var colors = new Color[CardNodes.Length];
            for (int i = 0; i < CardNodes.Length; i++)
            {
                cards[i] = _ui.Find(CardNodes[i]) as RectTransform;
                var entry = i < e.OfferedKeys.Length ? _buffTable.Get(e.OfferedKeys[i]) : null;
                rarities[i] = entry != null ? entry.Rarity : CardRarity.Common;
                colors[i] = RarityColor(rarities[i]);
            }
            _levelUpFx.ShowCards(cards, rarities, colors);
        }

        private void OpenPendingOffer()
        {
            if (!_pendingOffer.HasValue) return;
            var e = _pendingOffer.Value;
            _pendingOffer = null;
            OnBuffOffer(e);
        }
    }
}
