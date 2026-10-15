using GameFramework.Core.Common;

namespace Game.Module.Events
{
    /// <summary>탑 · 밤 진행(입장 · 이번 주 층 · 소탕)이 바뀌었다 — 로비 칸 · 입장 패널이 다시 그린다.</summary>
    public struct ModeProgressChangedEvent : IEvent
    {
        public int WeekClearedFloor;
        public int TowerEntriesLeft;
        public int NightEntriesLeft;
        public bool IsSweptToday;
    }

    /// <summary>탑 층에 들어갔다 — 인게임 탑 판 · 조건 칩 · 소개 띠.</summary>
    public struct TowerFloorEnteredEvent : IEvent
    {
        public int EnteredFloor;
        public int RuleKind;             // Game.Module.Common.Modes.TowerRule
        public string FloorNameKey;
        public string RuleDescKey;
        public int EnemyAffinityKind;    // Game.Module.InGame.Affinity — 적 속성(없으면 None)
        public int AdvantageKind;        // 유리한 몸 속성(없으면 None)
        public int PossessLimit;         // 0 = 제한 없음
        public bool IsLeaveLocked;
    }

    /// <summary>탑 — 남은 적 수.</summary>
    public struct TowerEnemiesLeftEvent : IEvent
    {
        public int RemainingEnemies;
    }

    /// <summary>탑 「빙의는 한 번」 층 — 남은 빙의 횟수.</summary>
    public struct TowerPossessLeftEvent : IEvent
    {
        public int RemainingPossess;
    }

    /// <summary>탑 판이 끝났다 → 탑 결과 창.</summary>
    public struct TowerFinishedEvent : IEvent
    {
        public bool IsFloorCleared;
        public int PlayedFloor;
        public int WeekClearedAfter;
        public string PlayedNameKey;
        public int NextFloor;
        public string NextNameKey;
        public string NextDescKey;
        public int FailFitTrained;
        public int FailFitTotal;
        public string[] SuggestHostKeys;
        public string[] ShardHostKeys;
        public int[] ShardCounts;
        public int RewardGold;
        public int EntriesLeft;
        public int PossessCount;
        public int LostCount;
        public float ElapsedSeconds;
    }

    /// <summary>밤 — 1초마다 · 처치할 때. 매 프레임 발행하지 않는다.</summary>
    public struct NightStateEvent : IEvent
    {
        public float ElapsedSeconds;
        public int KillCount;
        public int WaveAffinityKind;
        public int NextEventKind;        // 0 없음 · 1 대형 호스트 · 2 보스
        public float NextEventSeconds;
        public bool IsCounterHintShown;
        public int CounterAffinityKind;
    }

    /// <summary>밤 — 화면 가운데 띠(대형 등장 · 속성 전환 · 보스 · 시작 안내).</summary>
    public struct NightBannerEvent : IEvent
    {
        public string BannerKey;
        public string BannerArg;
        public string SubKey;
        public string SubArg;
        public float Seconds;
    }

    /// <summary>밤 판이 끝났다 → 밤 결과 창.</summary>
    public struct NightFinishedEvent : IEvent
    {
        public bool IsCleared;
        public int NightDayIndex;
        public string BossKey;
        public int FinalKills;
        public int PreviousBest;
        public bool IsNewBest;
        public float SurvivedSeconds;
        public int ReachedLevel;
        public string[] ShardHostKeys;
        public int[] ShardCounts;
        public int EntriesLeft;
        public int PossessCount;
        public int LostCount;
    }
}
