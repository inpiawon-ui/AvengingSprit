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

        /// <summary>
        /// 그 몸을 **가지고 있는가** — 고르는 목록에 내보낼지. 유령은 언제나 true.
        /// 해금(<see cref="IsHostUnlocked"/>)과 다르다 — 그쪽은 지금 전부 열려 있다.
        /// </summary>
        bool IsHostOwned(HostEntry host);

        /// <summary>그 몸의 전투력. ⚠ 지금은 등급 기준 임시값이다(2026-09-29).</summary>
        int PowerOf(string hostKey);

        /// <summary>골드를 낸다. 모자라면 아무 일도 없다. 몸값이 아닌 지불(랜덤 선택 등)에 쓴다.</summary>
        bool TrySpendGold(int amount);

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

        /// <summary>지금 고른 챕터에 이 몸을 데려가며 실제로 내는 값(유령 0 · 테스트 모드 0).</summary>
        int EntryCostOf(HostEntry host);

        /// <summary>그 챕터에 이 몸을 데려가는 진짜 값 — 화면에 적는 값(테스트 모드를 안 본다).</summary>
        int EntryPriceOf(HostEntry host, int chapter);

        /// <summary>그 챕터의 랜덤 선택 값.</summary>
        int RandomEntryPrice(int chapter);

        /// <summary>몸값을 치른다. 판을 시작하는 순간 한 번만 부른다.</summary>
        bool PayHostEntry(HostEntry host);

        /// <summary>그 몸의 파편 수.</summary>
        int GetShards(string hostKey);

        /// <summary>그 몸을 잡거나 잃었을 때 나오는 파편 수. 등급이 값을 정한다.</summary>
        int ShardDropFor(string hostKey, bool lostWhilePossessing);

        /// <summary>숙련도 상한. `GameConfig` 의 파편 곡선 길이가 곧 상한이다.</summary>
        int MasteryMax { get; }

        /// <summary>이 몸의 다음 단계에 드는 파편 (등급 배수 포함). 0 이면 만렙.</summary>
        int MasteryCost(string hostKey);

        /// <summary>고스트(유저) 레벨 상한. 레벨은 챕터를 깨서 받는 경험치로만 오른다.</summary>
        int GhostLevelMax { get; }

        /// <summary>별 — 숙련도 2단계마다 하나(0~5). 조각을 모아 올린다.</summary>
        int StarsOf(string hostKey);

        // ── 능력치 골드 강화 ─────────────────────────────────
        //
        // 유령 탭에서 올린 값은 **모든 몸**에, 호스트 탭에서 올린 값은 그 몸에만 붙는다.
        // 판에서는 두 값을 더한 % 가 빙의한 몸의 능력치에 곱해진다.

        int GhostStatMax { get; }
        int HostStatMax { get; }
        /// <summary>이 몸의 강화 상한 — 성급이 연다(1성 10 · 2성 20 …).</summary>
        int HostStatCap(string hostKey);
        /// <summary>챕터 수(챕터 표의 줄 수).</summary>
        int ChapterCount { get; }
        /// <summary>테스트 모드 — 전부 해금 · 무료 입장 · 젬 100만 · 상자 빨리. 라이브는 false.</summary>
        bool TestMode { get; }
        /// <summary>그 챕터의 줄(이름 · 보상 · 빌려 쓰는 그림 칸 …). 표가 없으면 기본값.</summary>
        Game.Character.GameConfig.ChapterDef ChapterInfo(int chapter);
        /// <summary>게임 설정. 부트 초기에는 null 일 수 있다.</summary>
        Game.Character.GameConfig Config { get; }
        int GhostStatLevel(HostStat stat);
        int HostStatLevel(string hostKey, HostStat stat);
        /// <summary>다음 단계 골드. 0 이면 상한.</summary>
        int GhostStatCost(HostStat stat);
        int HostStatCost(string hostKey, HostStat stat);
        /// <summary>그 레벨까지 오른 % (예: Lv15 → 30).</summary>
        float StatPercent(HostStat stat, int level);
        /// <summary>골드를 쓰고 한 단계 올린다. 모자라거나 상한이면 false. 저장은 부르는 쪽이.</summary>
        bool BuyGhostStat(HostStat stat);
        bool BuyHostStat(string hostKey, HostStat stat);
        /// <summary>그 몸이 판에서 받는 배율 = 1 + (유령 % + 호스트 %) / 100.</summary>
        float StatBonusMul(string hostKey, HostStat stat);

        // ── 유령 성장 경로 ──────────────────────────────────

        int PathCount { get; }
        int PathLevel(int index);
        bool IsPathClaimed(int index);
        /// <summary>그 칸의 보상(골드 · 젬 · 영혼 핵).</summary>
        void GetPathReward(int index, out int gold, out int gem, out int spiritCore);
        /// <summary>레벨이 닿았고 아직 안 받았으면 보상(골드 · 젬 · 영혼 핵)을 준다. 저장은 부르는 쪽이.</summary>
        bool ClaimPath(int index);

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
        /// 챕터 클리어 보상 — 골드 · 유저 경험치를 주고 격파 기록을 올려 다음 챕터를 연다. 저장까지 한다.
        /// 이미 깬 챕터를 다시 깨도 골드 · 경험치는 준다(기획 2026-09-18 · 09-21).
        /// </summary>
        UniTask GrantChapterClearAsync(int chapter, int gold);

        /// <summary>
        /// 판이 클리어 없이 끝났을 때(사망 · 포기) — 판에서 주운 골드만 계정에 넣고 저장한다.
        /// 「죽으면 판에서 번 골드만 준다」(기획 2026-10-01). 상자 · 경험치 · 격파 기록은 없다.
        /// </summary>
        UniTask GrantRunGoldAsync(int gold);

        /// <summary>정본 REWARD_DB 를 반영하는 확장형. 영구 재화는 실패해도 남는다.</summary>
        UniTask GrantStageRewardAsync(int gold, int ghostExp, bool cleared,
                                      int spiritCore, int hostMemory, int gem);
    }
}
