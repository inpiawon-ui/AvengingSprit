using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;

namespace Game.User
{
    /// <summary>호스트 스킬 레벨 칸. 저장 배열 첨자와 같다 — 순서를 바꾸지 않는다.</summary>
    public enum HostSkillSlot { Active = 0, Passive = 1, Ult = 2 }

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

        /// <summary>그 몸의 지금 전투력 — 고스트 Lv · 골드 강화 · 숙련도를 판과 같은 자(`HostCombat`)로 잰다.</summary>
        int PowerOf(string hostKey);

        /// <summary>그 챕터의 권장 전투력. 챕터 표에 값이 없으면 0.</summary>
        int RecommendedPower(int chapter);

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

        // ── 유령 강화 — 스탯 6줄 (육성 개편 PD 2026-10-29) ─────
        //
        // 공격력 · 호스트 체력 · 치명타 · 공격 속도 · 이동 속도 · 받는 피해 감소. 골드로 한 단계씩, 상한 = 유령 레벨.
        // **모든 몸**에 붙는다. 호스트 탭 능력치 강화는 없앴다 — 몸은 스킬 레벨로 키운다.

        int GhostStatMax { get; }
        /// <summary>지금 유령 레벨이 여는 유령 강화 상한(레벨 1 당 1, 최대 GhostStatMax).</summary>
        int GhostStatCap { get; }
        /// <summary>챕터 수(챕터 표의 줄 수).</summary>
        int ChapterCount { get; }
        /// <summary>테스트 모드 — 전부 해금 · 무료 입장 · 젬 100만 · 상자 빨리. 라이브는 false.</summary>
        bool TestMode { get; }
        /// <summary>그 챕터의 줄(이름 · 보상 · 빌려 쓰는 그림 칸 …). 표가 없으면 기본값.</summary>
        Game.Character.GameConfig.ChapterDef ChapterInfo(int chapter);
        /// <summary>게임 설정. 부트 초기에는 null 일 수 있다.</summary>
        Game.Character.GameConfig Config { get; }
        int GhostStatLevel(HostStat stat);
        /// <summary>다음 단계 골드. 0 이면 상한(또는 줄이 없는 능력치).</summary>
        int GhostStatCost(HostStat stat);
        /// <summary>그 레벨까지 오른 값 — 치명타 · 받는 피해 감소는 %p, 나머지는 %.</summary>
        float GhostStatValue(HostStat stat, int level);
        /// <summary>골드를 쓰고 한 단계 올린다. 모자라거나 상한이면 false. 저장은 부르는 쪽이.</summary>
        bool BuyGhostStat(HostStat stat);
        /// <summary>곱하는 능력치의 판 배율 = 1 + 유령 % / 100. 더하는 능력치는 1.</summary>
        float StatBonusMul(string hostKey, HostStat stat);

        /// <summary>더하는 능력치(치명타 · 치명타 피해 · 받는 피해 감소)의 유령 강화 합(%p).</summary>
        float StatBonusFlat(string hostKey, HostStat stat);

        /// <summary>사거리 강화 레벨 — 사거리는 이제 키우지 않아 늘 0(2026-10-29).</summary>
        float StatLevelEffective(string hostKey, HostStat stat);

        // ── 호스트 스킬 레벨 (육성 개편 PD 2026-10-29) ─────────
        //
        // 성급이 상한을 연다(액티브 = 성급 · 패시브 · 필살기 = 1 + 성급 ÷ 2). 레벨은 골드로 셋을 따로 올린다.
        // 못 가진 몸(판에서 빙의)은 전부 Lv1.

        /// <summary>그 칸의 끝 레벨(액티브 10 · 패시브 5 · 필살기 5).</summary>
        int SkillLevelMax(HostSkillSlot slot);
        /// <summary>지금 성급이 여는 상한. 못 가진 몸은 1.</summary>
        int SkillLevelCap(string hostKey, HostSkillSlot slot);
        /// <summary>그 몸의 스킬 레벨(1~). 못 가진 몸 · 유령은 1.</summary>
        int SkillLevel(string hostKey, HostSkillSlot slot);
        /// <summary>다음 레벨 골드(등급 배율 포함). 0 이면 상한이거나 못 가진 몸.</summary>
        int SkillLevelCost(string hostKey, HostSkillSlot slot);
        /// <summary>골드를 쓰고 한 레벨 올린다. 모자라거나 상한이면 false. 저장은 부르는 쪽이.</summary>
        bool BuySkillLevel(string hostKey, HostSkillSlot slot);

        /// <summary>그 인연 짝의 첫 켜짐 보상을 이미 받았는가.</summary>
        bool IsInyeonClaimed(string mainKey, string partnerKey);
        /// <summary>처음이면 기록하고 true(조각은 부르는 쪽이 준다). 이미 받았으면 false. 저장은 부르는 쪽이.</summary>
        bool TryClaimInyeon(string mainKey, string partnerKey);

        // ── 유령 성장 경로 ──────────────────────────────────

        int PathCount { get; }
        int PathLevel(int index);
        bool IsPathClaimed(int index);
        /// <summary>그 칸의 보상(골드 · 젬 · 영혼 핵).</summary>
        void GetPathReward(int index, out int gold, out int gem, out int spiritCore);
        /// <summary>레벨이 닿았고 아직 안 받았으면 보상(골드 · 젬 · 영혼 핵)을 준다. 저장은 부르는 쪽이.</summary>
        bool ClaimPath(int index);

        /// <summary>파편을 준다. 숙련도를 다 올린 몸이면 정수로 바뀐다.</summary>
        void AddShards(string hostKey, int amount);

        /// <summary>정수 — 다 키운 몸에게 온 조각이 바뀐 것.</summary>
        int Essence { get; }

        /// <summary>그 몸의 조각 하나를 정수로 살 때의 값(등급별).</summary>
        int EssencePriceOf(string hostKey);

        /// <summary>정수로 그 몸의 조각을 산다. 모자라거나 다 키운 몸이면 false.</summary>
        bool ExchangeEssence(string hostKey, int shards);

        /// <summary>일일 상점 상태(저장에 들어 있다). 진열을 짜고 사는 것은 `IDailyShopService` 가 한다.</summary>
        DailyShopData DailyShop { get; }

        /// <summary>젬을 낸다. 모자라면 아무 일도 없다.</summary>
        bool TrySpendGem(int amount);

        /// <summary>로비 상점 기록(저장에 들어 있다). 규칙은 `IStoreService` 가 갖는다.</summary>
        StoreSaveData Store { get; }

        /// <summary>소환권.</summary>
        int Tickets { get; }
        void AddTickets(int amount);
        bool TrySpendTickets(int amount);

        /// <summary>정수를 더한다(상점 · 보상).</summary>
        void AddEssence(int amount);

        /// <summary>파편을 쓰고 숙련도를 한 단계 올린다. 봉인 해제도 같은 동작이다.</summary>
        bool SpendShards(string hostKey, int cost);

        /// <summary>다음 성급에 드는 골드(등급 배수 포함). 봉인 해제 · 만렙이면 0.</summary>
        int StarUpGoldCost(string hostKey);

        /// <summary>조각 + 골드를 내고 성급(숙련도)을 한 단계 올린다. 하나라도 모자라면 아무것도 안 쓴다.</summary>
        bool TryStarUp(string hostKey);

        /// <summary>조각이 해금 수에 닿아 저절로 풀린 호스트를 하나 꺼낸다(로비가 해금 연출을 띄운다). 없으면 false.</summary>
        bool TryTakeNewUnlock(out string hostKey);

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

        /// <summary>행동력을 그 값으로 둔다(0 ~ 표의 한도). 회복 · 반환 · 채우기는 `IStaminaService` 가 계산해 부른다.</summary>
        void SetStamina(int value);

        /// <summary>행동력 회복 시계 · 젬 채우기 횟수 · 오늘의 몸(저장에 들어 있다).</summary>
        RunEntryData RunEntry { get; }

        /// <summary>
        /// 이번 판의 출전 방식. 저장하지 않는다 — 로비가 정하고, 클리어 보너스(`GrantChapterClearAsync`)가 읽는다.
        /// </summary>
        RunStartMode RunStartMode { get; set; }

        /// <summary>망령의 탑 · 망령의 밤 진행 값(저장 v8). 규칙은 `Game.Module.Common.Modes.IModeProgress`.</summary>
        ModeProgressData Modes { get; }

        /// <summary>
        /// 이번 판을 시작한 몸(랜덤이면 뽑힌 몸). 저장하지 않는다 — 안 가진 몸은 `SelectHost` 가 저장에 안 남겨서
        /// 행운 보너스 조각이 엉뚱한 몸에 가지 않게 따로 든다.
        /// </summary>
        string RunHostKey { get; set; }

        /// <summary>깼을 때 출전 방식이 더 주는 것(골드 · 조각 · 유령 경험치 합계). 지급과 같은 계산이다.</summary>
        void ClearBonusOf(int runGold, int chapter, out int bonusGold, out int bonusShards, out int exp);

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
