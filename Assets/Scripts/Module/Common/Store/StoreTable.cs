using System;
using UnityEngine;

namespace Game.Module.Common.Store
{
    /// <summary>
    /// 로비 상점 표(기획서 `AVSR_Content_Store.md` B-2). 원본은 `Projects/AVSR/Store/*.tsv` —
    /// `Tools/Game/로비 상점 표 임포트 (Store)` 가 굽는다. 주소 `TableData/StoreTable` · 라벨 `label_tabledata`.
    /// ⚠ 값 · 보상은 여기만 — 코드에 적지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Store Table", fileName = "StoreTable")]
    public sealed class StoreTable : ScriptableObject
    {
        [SerializeField] private StoreProduct[] _products = Array.Empty<StoreProduct>();
        [SerializeField] private PriceTier[] _tiers = Array.Empty<PriceTier>();
        [SerializeField] private StoreCard[] _cards = Array.Empty<StoreCard>();

        public StoreProduct[] Products => _products;
        public StoreCard[] Cards => _cards;

        public StoreProduct Find(string id)
        {
            for (int i = 0; i < _products.Length; i++)
                if (_products[i].Id == id) return _products[i];
            return null;
        }

        public StoreCard Card(string key)
        {
            for (int i = 0; i < _cards.Length; i++)
                if (_cards[i].Key == key) return _cards[i];
            return null;
        }

        /// <summary>가격 등급의 한국 기준값(원). 스토어가 지역 가격을 주기 전까지 화면에 이 값을 쓴다.</summary>
        public int Krw(string tier)
        {
            for (int i = 0; i < _tiers.Length; i++)
                if (_tiers[i].Tier == tier) return _tiers[i].Krw;
            return 0;
        }

#if UNITY_EDITOR
        public void Fill(StoreProduct[] products, PriceTier[] tiers, StoreCard[] cards)
        {
            _products = products;
            _tiers = tiers;
            _cards = cards;
        }
#endif
    }

    [Serializable]
    public sealed class StoreProduct
    {
        public string Id;
        public string Tab;          // chest · currency · pass
        public string Kind;         // package · card · chest · gold · gem · stamina · pass
        public string PriceType;    // iap · gem · free
        public string Price;        // iap 면 가격 등급(P1…), gem 이면 젬 수
        public string Limit;        // once · daily:N · weekly:N · season · none
        public string Show;         // always · days_since_install<=N · chapter_cleared>=N · season_active
        public string Rewards;      // 보상 문자열(RewardGranter)
        public string Badge;        // best · first_double
        public int Order;
        public string StoreId;
        public string NameKey;
        public string DescKey;

        public bool IsIap => PriceType == "iap";
        public int GemPrice => PriceType == "gem" && int.TryParse(Price, out var g) ? g : 0;
    }

    [Serializable]
    public sealed class PriceTier
    {
        public string Tier;
        public int Krw;
        public float Usd;
    }

    /// <summary>월정액(계약) — 산 날 즉시 젬, 그날부터 days 일 동안 매일 받기.</summary>
    [Serializable]
    public sealed class StoreCard
    {
        public string Key;
        public string ProductId;
        public int Days;
        public int InstantGem;
        public int DailyGem;
        public string Perks;        // free_refresh:1 · run_gold:+10 · card_reroll:1 · stamina_max:+20 · daily_stamina:N
    }
}
