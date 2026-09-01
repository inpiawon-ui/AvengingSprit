using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 정본 v2.3 SHOP_MASTER + SHOP_INVENTORY.
    ///
    /// 상점은 이벤트와 성격이 다르다. 이벤트가 "운을 걸겠는가" 라면
    /// 상점은 **"모아 둔 것을 지금 쓰겠는가"** 다. 그래서 값이 정확히 붙어 있고
    /// 살 수 있는 수가 정해져 있다 — 한 판에 두 번, 카드는 한 장까지.
    /// 무제한이면 골드를 아낄 이유가 사라지고 방마다 들르는 정산소가 된다.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopTable", menuName = "Game/Shop Table")]
    public sealed class ShopTable : ScriptableObject
    {
        [SerializeField] private ShopChapter[] _chapters = Array.Empty<ShopChapter>();
        [SerializeField] private ShopOffer[] _offers = Array.Empty<ShopOffer>();

        public IReadOnlyList<ShopOffer> Offers => _offers;

        public ShopChapter ForChapter(int chapter)
        {
            for (int i = 0; i < _chapters.Length; i++)
                if (_chapters[i].Chapter == chapter) return _chapters[i];
            return _chapters.Length > 0 ? _chapters[0] : null;
        }

        /// <summary>
        /// 그 챕터에서 팔 카드를 `count` 장 뽑는다.
        /// 이미 최대 레벨이라 더 못 올리는 카드는 빼고 뽑는다 — 살 수는 있는데
        /// 아무 일도 안 일어나는 물건을 진열해 두면 값이 거짓이 된다.
        /// </summary>
        public void Draw(List<ShopOffer> into, int chapter, int count,
                         ICollection<string> exclude, System.Random rng)
        {
            into.Clear();
            _pick.Clear();
            for (int i = 0; i < _offers.Length; i++)
            {
                var o = _offers[i];
                if (o == null || o.Chapter != chapter) continue;
                if (exclude != null && exclude.Contains(o.BuffKey)) continue;
                _pick.Add(o);
            }

            for (int n = 0; n < count && _pick.Count > 0; n++)
            {
                int k = rng.Next(_pick.Count);
                into.Add(_pick[k]);
                _pick.RemoveAt(k);
            }
        }

        private readonly List<ShopOffer> _pick = new();
    }

    [Serializable]
    public sealed class ShopChapter
    {
        [SerializeField] private int _chapter;
        [SerializeField] private int _offerCount = 3;
        [SerializeField] private int _hostHealPct;
        [SerializeField] private int _ghostHealPct;
        [SerializeField] private int _healPrice;
        [SerializeField] private int _totalPurchaseLimit = 2;
        [SerializeField] private int _cardPurchaseLimit = 1;

        public int Chapter => _chapter;
        public int OfferCount => _offerCount;
        public int HostHealPct => _hostHealPct;
        public int GhostHealPct => _ghostHealPct;
        public int HealPrice => _healPrice;
        public int TotalPurchaseLimit => _totalPurchaseLimit;
        public int CardPurchaseLimit => _cardPurchaseLimit;
    }

    [Serializable]
    public sealed class ShopOffer
    {
        [SerializeField] private int _chapter;
        [SerializeField] private string _cardId;
        [SerializeField] private string _buffKey;
        [SerializeField] private int _price;

        public int Chapter => _chapter;
        public string CardId => _cardId;
        public string BuffKey => _buffKey;
        public int Price => _price;
    }
}
