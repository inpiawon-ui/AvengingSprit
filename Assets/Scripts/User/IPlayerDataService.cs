using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;

namespace Game.User
{
    /// <summary>
    /// 재화·진행도 갱신의 **단일 집약 지점** (constants.md 6절 제약 4).
    ///
    /// 게임 로직이 `UserData` 필드를 직접 건드리지 않고 전부 여기를 거친다.
    /// 서버 전환 시 이 메서드들이 그대로 서버 권위 검증 지점이 된다.
    /// </summary>
    public interface IPlayerDataService
    {
        bool IsReady { get; }

        int Stamina { get; }
        int StaminaMax { get; }
        int Gold { get; }
        int Gem { get; }
        /// <summary>보스를 잡아 얻는 영구 재화. 유령 본체를 강화한다 (정본 Growth Runtime).</summary>
        int SpiritCore { get; }
        /// <summary>호스트를 써서 쌓이는 영구 재화. 그 몸을 더 능숙하게 만든다.</summary>
        int HostMemory { get; }

        int GhostLevel { get; }
        int GhostExp { get; }
        int GhostExpMax { get; }

        int CurrentChapter { get; }
        int ReachedStage { get; }
        int ClearedChapter { get; }

        /// <summary>도전할 수 있는 가장 높은 챕터 — 깬 챕터 + 1 (1 ~ 6).</summary>
        int UnlockedChapter { get; }

        /// <summary>
        /// 이번 판에 들어갈 챕터. 챕터 선택 화면이 정하고 전투가 읽는다.
        /// 판 한정 — 저장하지 않는다. 안 골랐으면 열린 챕터 중 가장 높은 것.
        /// </summary>
        int SelectedChapter { get; set; }

        /// <summary>현재 선택된 호스트 키. 없으면 시작 보유 호스트로 대체된다.</summary>
        string SelectedHostId { get; }

        UniTask LoadAsync();
        UniTask SaveAsync();

        /// <summary>
        /// 봉인에서 풀렸는가 — **숙련도 ≥ 1 이 곧 해제다.** 저장하지 않고 매번 평가한다.
        /// 로비에서 고를 수 있는가를 뜻하며, 전장에서의 빙의 가능 여부와는 다르다.
        /// </summary>
        bool IsHostUnlocked(HostEntry host);

        /// <summary>그 몸의 숙련도 Lv (0 = 봉인). 스킬은 이 값만 본다.</summary>
        int GetMastery(string hostKey);

        /// <summary>
        /// 그 몸의 **스킬이** 봉인돼 있는가(숙련도 0). 몸을 쓰는 것 자체는 막지 않는다.
        /// </summary>
        bool IsSkillSealed(string hostKey);

        /// <summary>이번 판을 유령으로 시작하는가. 판 한정 — 저장하지 않는다.</summary>
        bool StartAsGhost { get; set; }

        /// <summary>이 몸을 데려갈 골드가 있는가.</summary>
        bool CanAffordHost(HostEntry host);

        /// <summary>몸값을 치른다. 판을 시작하는 순간 한 번만 부른다.</summary>
        bool PayHostEntry(HostEntry host);

        /// <summary>다음 고스트 레벨을 살 골드가 있는가.</summary>
        bool CanBuyGhostLevel { get; }

        /// <summary>골드로 다음 고스트 레벨을 산다.</summary>
        bool BuyGhostLevel();

        /// <summary>그 몸의 파편 수.</summary>
        int GetShards(string hostKey);

        /// <summary>그 몸을 잡거나 잃었을 때 나오는 파편 수. 등급이 값을 정한다.</summary>
        int ShardDropFor(string hostKey, bool lostWhilePossessing);

        /// <summary>숙련도 상한. `GameConfig` 의 파편 곡선 길이가 곧 상한이다.</summary>
        int MasteryMax { get; }

        /// <summary>이 몸의 다음 단계에 드는 파편 (등급 배수 포함). 0 이면 만렙.</summary>
        int MasteryCost(string hostKey);

        /// <summary>고스트 레벨 상한.</summary>
        int GhostLevelMax { get; }

        /// <summary>Lv → 다음 레벨 골드. 0 이면 상한.</summary>
        int GhostLevelCost(int level);

        /// <summary>파편을 준다.</summary>
        void AddShards(string hostKey, int amount);

        /// <summary>파편을 쓰고 숙련도를 한 단계 올린다. 봉인 해제도 같은 동작이다.</summary>
        bool SpendShards(string hostKey, int cost);

        IReadOnlyList<HostEntry> AllHosts { get; }

        /// <summary>호스트 선택 화면에 내보낼 몸. 전투 전용 배우(방패병·센서드론·엘리트)는 빠진다.</summary>
        IReadOnlyList<HostEntry> PlayableHosts { get; }
        HostEntry GetHost(string hostKey);
        ActiveSkillEntry GetActiveSkill(string activeSkillKey);

        /// <summary>패시브 스킬 조회. 없으면 null — 23명 중 12명은 패시브가 없다.</summary>
        PassiveSkillEntry GetPassiveSkill(string passiveSkillKey);

        /// <summary>보유 호스트 수 / 전체. 하단 `보유 HOST n/12` 표시용.</summary>
        int OwnedHostCount { get; }

        /// <summary>선택 호스트 변경. 잠금 호스트도 선택은 가능하다(목표 확인용).</summary>
        void SelectHost(string hostKey);

        /// <summary>스테이지 진입 비용 차감. 부족하면 false 를 돌려주고 아무것도 바꾸지 않는다.</summary>
        bool TrySpendStamina(int amount);

        void AddCurrency(int gold, int gem);

        /// <summary>영구 재화 네 가지를 한 번에 더한다. 상자를 열 때 쓴다.</summary>
        void AddGrowthCurrency(int gold, int gem, int spiritCore, int hostMemory);

        void SetProgress(int chapter, int stage);

        // ── 보물상자 칸 ──────────────────────────────────────
        //
        // 저장 필드를 직접 건드리지 않게 여기를 거친다(constants.md 6절 제약 4).
        // `ChestModule` 전용이다 — 다른 곳에서 부르지 않는다.

        int ChestSlotCount { get; }
        /// <summary>그 칸의 상자 키. 빈 칸이면 빈 문자열.</summary>
        string GetChestKey(int slot);
        /// <summary>해제 완료 시각 (Unix ms, UTC).</summary>
        long GetChestUnlockAt(int slot);
        /// <summary>총 소요 초. 남은 시간을 자를 때 쓴다.</summary>
        int GetChestSeconds(int slot);
        void SetChestSlot(int slot, string chestKey, long unlockAt, int seconds);

        /// <summary>
        /// 스테이지 종료 보상. 골드·고스트 EXP 지급 후 저장까지 한 번에 처리한다.
        /// `cleared` 일 때만 스테이지를 전진시킨다 — 실패는 진행도를 건드리지 않는다.
        /// </summary>
        UniTask GrantStageRewardAsync(int gold, int ghostExp, bool cleared);

        /// <summary>
        /// 챕터 클리어 보상 — 골드를 주고 격파 기록을 올려 다음 챕터를 연다. 저장까지 한다.
        /// 이미 깬 챕터를 다시 깨도 골드는 준다(기획 2026-09-18).
        /// </summary>
        UniTask GrantChapterClearAsync(int chapter, int gold);

        /// <summary>정본 REWARD_DB 를 반영하는 확장형. 영구 재화는 실패해도 남는다.</summary>
        UniTask GrantStageRewardAsync(int gold, int ghostExp, bool cleared,
                                      int spiritCore, int hostMemory, int gem);
    }
}
