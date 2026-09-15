using System;
using System.Collections.Generic;
using Game.Module.Common;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>이벤트가 요구하는 대가.</summary>
    public enum EventCost
    {
        None,
        Gold,
        GhostHp,
        HostHp,

        /// <summary>
        /// **최대 체력 영구 감소(%)** — 악마 계약의 정체다.
        ///
        /// 지금 아픈 것이 아니라 **앞으로 빼앗을 모든 몸**이 작아진다. 빙의할 때마다
        /// 새 몸의 최대 체력에 곱해 들어간다 — 되돌릴 수 없고 판 끝까지 간다.
        /// 다른 대가는 「지금 한 번」이지만 이것만 「이 판 내내」다.
        /// </summary>
        MaxHp,
    }

    /// <summary>
    /// 이벤트가 돌려주는 것.
    ///
    /// 정본의 `RewardType` 문자열을 그대로 쓰지 않고 **우리가 실제로 줄 수 있는 것**으로
    /// 좁혀 둔다. 문자열을 그대로 두면 지금 못 주는 보상도 목록에 남아,
    /// 골랐는데 아무 일도 안 일어나는 선택지가 생긴다.
    /// </summary>
    public enum EventReward
    {
        HostHeal,       // 호스트 최대 체력의 Value%
        GhostHeal,      // 고스트 최대 체력의 Value%
        Gold,           // Value 골드
        CardOffer,      // 카드 3택1 — Rarity 이상으로 뽑는다
        CardGrant,      // 카드 한 장을 바로 준다

        /// <summary>가진 카드 한 장의 레벨을 올린다 (제단이 이미 지닌 것을 벼린다)</summary>
        UpgradeCard,
        /// <summary>판이 끝날 때까지 빙의 사거리가 Value% 늘어난다</summary>
        PossessReach,
        /// <summary>판이 끝날 때까지 상점 카드 값이 Value% 싸진다</summary>
        ShopDiscount,
        /// <summary>다음 보스의 방어막이 한 번 서지 않는다</summary>
        BossShieldBreak,
        /// <summary>빼앗을 수 있는 몸 하나가 나타난다 (`RewardKey` 가 어떤 몸인지 정한다)</summary>
        SpawnHost,
    }

    /// <summary>
    /// 정본 v2.3 EVENT_MASTER — 챕터마다 6개, 모두 18개.
    ///
    /// 이벤트 방은 "무엇을 내주고 무엇을 받을까"를 묻는 자리다. 전투가 없는 대신
    /// 판의 자원(골드·호스트 체력·고스트 체력)이 여기서 값을 가진다 —
    /// 이 방이 비어 있으면 자원을 아낄 이유가 사라진다.
    /// </summary>
    [CreateAssetMenu(fileName = "EventTable", menuName = "Game/Event Table")]
    public sealed class EventTable : ScriptableObject
    {
        [SerializeField] private EventEntry[] _entries = Array.Empty<EventEntry>();

        public IReadOnlyList<EventEntry> Entries => _entries;

        /// <summary>
        /// 이 판에서 아직 안 나온 이벤트 하나를 뽑는다.
        ///
        /// 정본은 전부 `OncePerRun` 이다 — 한 판에 같은 이벤트가 두 번 나오면
        /// 두 번째는 선택이 아니라 반복이 된다.
        ///
        /// ⚠ **챕터로 거르지 않는다.** 표에는 18종이 CH1~CH3 에만 6씩 들어 있어
        ///   챕터로 거르면 CH4~6 이 빈손이 된다. 임시로 챕터를 1~3 으로 잘라
        ///   막고 있었는데, 그러면 뒤 세 챕터가 CH3 것을 다시 뽑아 **재탕**이 된다.
        ///   판마다 이벤트 방은 여섯 번뿐이므로 18종을 한 통으로 두면 겹치지 않는다.
        ///   챕터별로 가를지는 나중에 정한다(기획 2026-09-08) — 그때 `Chapter` 를
        ///   다시 보면 된다. 값은 그대로 들고 있다.
        /// </summary>
        public EventEntry Draw(ICollection<string> used, System.Random rng)
        {
            EventEntry pick = null;
            int seen = 0;
            for (int i = 0; i < _entries.Length; i++)
            {
                var e = _entries[i];
                if (e == null || !e.Implemented) continue;
                if (used != null && used.Contains(e.EventId)) continue;
                // 저수지 표본 추출 — 후보 수를 미리 세지 않고 한 번에 고른다
                if (rng.Next(++seen) == 0) pick = e;
            }
            return pick;
        }
    }

    [Serializable]
    public sealed class EventEntry
    {
        [SerializeField] private string _eventId;
        [SerializeField] private int _chapter;
        [SerializeField] private string _titleKr;
        [SerializeField] private string _bodyKr;

        [SerializeField] private EventCost _costType;
        [SerializeField] private int _costValue;
        [SerializeField] private string _acceptKr = "받아들인다";
        [SerializeField] private string _declineKr = "지나간다";

        [SerializeField] private EventReward _rewardType;
        [SerializeField] private int _rewardValue;
        [SerializeField] private Game.Character.CardRarity _rewardRarity;
        /// <summary>보상이 가리키는 대상 (지금은 `SpawnHost` 의 호스트 키).</summary>
        [SerializeField] private string _rewardKey;

        /// <summary>
        /// 뜻대로 될 확률(%). 0 이면 확정이다.
        /// 빗나가면 아래 `_declineReward` 를 대신 준다 — 대가만 치르고 빈손으로
        /// 돌아서면 그건 선택이 아니라 벌이다.
        /// </summary>
        [SerializeField] private int _chancePercent;

        /// <summary>등을 돌렸을 때(또는 확률이 빗나갔을 때) 주는 것.</summary>
        [SerializeField] private bool _hasDeclineReward;
        [SerializeField] private EventReward _declineReward;
        [SerializeField] private int _declineValue;

        /// <summary>
        /// 받아들이면 **먼저 싸운다.** 보상은 방을 비운 뒤에 온다.
        /// 매복·도전·결투가 이 길을 쓴다 — 이벤트 방에는 스폰표가 없으므로
        /// 적은 그 챕터 풀에서 절차적으로 세운다.
        /// </summary>
        [SerializeField] private bool _fightFirst;
        [SerializeField] private int _fightCount = 3;
        [SerializeField] private bool _fightElite;

        /// <summary>본 보상에 얹히는 골드 (정본의 `..._PLUS_GOLD_100`).</summary>
        [SerializeField] private int _extraGold;

        [SerializeField] private bool _implemented = true;
        [SerializeField] private string _canonType;

        public string EventId => _eventId;
        public int Chapter => _chapter;
        public string TitleKr => _titleKr;
        public string BodyKr => _bodyKr;
        public EventCost CostType => _costType;
        public int CostValue => _costValue;
        public string AcceptKr => _acceptKr;
        public string DeclineKr => _declineKr;

        // 화면에 보이는 문구 — 지금 언어로. 번역이 없으면 위의 원문(*Kr)이다.
        public string DisplayTitle   => Localize.FromTable($"event.{_eventId}.title", _titleKr);
        public string DisplayBody    => Localize.FromTable($"event.{_eventId}.body", _bodyKr);
        public string DisplayAccept  => Localize.FromTable($"event.{_eventId}.accept", _acceptKr);
        public string DisplayDecline => Localize.FromTable($"event.{_eventId}.decline", _declineKr);
        public EventReward RewardType => _rewardType;
        public int RewardValue => _rewardValue;
        public Game.Character.CardRarity RewardRarity => _rewardRarity;
        public string RewardKey => _rewardKey;
        public int ChancePercent => _chancePercent;
        public bool HasDeclineReward => _hasDeclineReward;
        public EventReward DeclineReward => _declineReward;
        public int DeclineValue => _declineValue;
        public bool FightFirst => _fightFirst;
        public int FightCount => _fightCount;
        public bool FightElite => _fightElite;
        public int ExtraGold => _extraGold;
        public bool Implemented => _implemented;
        public string CanonType => _canonType;
    }
}
