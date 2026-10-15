using System;
using UnityEngine;

namespace Game.User
{
    /// <summary>
    /// 유저 저장 데이터.
    ///
    /// 설계 제약 (constants.md 6절):
    ///  · `saveVersion` 필수 — 스키마 변경 시 마이그레이션 기준
    ///  · 해금 호스트 목록을 **저장하지 않는다.** 진행도로 매번 평가한다.
    ///    (저장하면 테이블 조건이 바뀔 때 기존 유저 데이터와 어긋난다)
    ///  · JsonUtility 직렬화 대상이므로 public 필드를 쓴다 (프로퍼티 불가)
    /// </summary>
    [Serializable]
    public sealed class UserData
    {
        // v2 — 호스트 숙련도·파편 추가. 기존 저장(v1)은 두 배열이 비어 있으므로
        //      전원 봉인(숙련도 0)으로 읽힌다. 잃는 값이 없어 마이그레이션 코드가 필요 없다.
        // v3 — 보물상자 3칸 추가. 기존 저장은 세 배열이 비어 있어 **빈 칸 셋**으로 읽힌다.
        //      길이는 읽을 때 맞추므로(`NormalizeChests`) 마이그레이션 코드가 필요 없다.
        // v4 — 능력치 골드 강화(유령 공통 · 호스트별) · 성장 경로 보상 추가(2026-09-21).
        //      옛 저장은 배열이 비어 있어 **전부 Lv0 · 보상 안 받음**으로 읽힌다. 길이는 읽을 때 맞춘다.
        // v5 — 라이브 기준(2026-10-04). 시작 몸 지급 표시(`starterGranted`). 시작 젬 0.
        // v6 — 정수 · 일일 상점(2026-10-04). 옛 저장은 정수 0 · 상점 빈 칸으로 읽혀 그날 다시 짠다.
        // v7 — 행동력 회복 시계 · 오늘의 몸(2026-10-15, AVSR_Content_Store.md B-12). 옛 저장은 30/30 이라
        //      불러올 때 표의 최대(20)로 자른다(`LocalUserDataRepository`). 회복 시계 0 은 「지금부터 센다」.
        // v8 — 추가 모드 망령의 탑 · 망령의 밤(2026-10-15, AVSR_Content_Tower · NightMode). 옛 저장은 빈 진행으로 읽혀
        //      이번 주 0층 · 오늘 입장은 하루 경계를 처음 넘을 때 채운다(`IModeProgress`).
        public const int CurrentSaveVersion = 8;

        public int saveVersion = CurrentSaveVersion;

        [Header("진행도")]
        public int clearedChapter;      // 보스까지 격파한 최고 챕터 (0 = 없음)
        public int currentChapter = 1;
        public int reachedStage = 1;    // 현재 챕터에서 도달한 최고 스테이지

        [Header("고스트")]
        public int ghostLevel = 1;
        public int ghostExp;
        public int ghostExpMax = 100;

        [Header("재화")]
        public int stamina = 20;
        public int staminaMax = 20;
        public int gold;
        // 라이브 기준 0 (2026-10-04). 테스트 모드를 켜면 불러올 때 100만으로 채운다
        // (`PlayerDataService.LoadAsync`) — 저장 기본값에 테스트 값을 두지 않는다.
        public int gem;

        // 정본 성장 재화 (Growth Runtime — Gold / Spirit Core / Host Memory / Gem).
        // 런이 끝나면 빌드·호스트·아이템·시너지는 사라지고 이 둘은 남는다.
        // 남는 것이 없으면 실패한 런이 통째로 버려진 시간이 된다.
        public int spiritCore;      // 보스를 잡아 얻는다. 유령 본체를 영구 강화
        public int hostMemory;      // 호스트를 써서 쌓인다. 그 몸을 더 능숙하게 만든다

        // ── 호스트 숙련도 · 파편 ─────────────────────────────
        //
        // **능력치는 고스트, 스킬은 호스트.** 이 분리를 바꾸면 안 된다 —
        // 이 게임은 방 구성을 못 고르는데 강제로 몸을 갈아탄다. 호스트가 능력치를
        // 쥐면 "안 키운 몸 탔다가 죽는다 → 빙의를 피한다" 가 되어 핵심 재미가 죽는다.
        //
        //   숙련도 0      봉인 — **빙의는 되고 스킬만 안 나온다**(평타뿐)
        //   숙련도 1      봉인 해제 · 액티브 + 패시브 동시 개방
        //   숙련도 1~4    수치 상승
        //   숙련도 5      특수 효과 해제
        //   숙련도 6~10   특수 효과 수치 상승
        //
        // ⚠ 봉인된 몸도 빙의는 된다. 막으면 봉인된 적만 있는 방에서
        //   손쓸 도리 없이 죽는다.
        //
        // 두 배열은 **짝으로 움직인다** — `hostKeys[i]` 의 값이 같은 첨자에 들어간다.
        // JsonUtility 가 Dictionary 를 직렬화하지 못해 나란한 배열로 둔다.
        //
        // ⚠ 해금 목록(`unlockedHostKeys`)은 **저장하지 않는다.**
        //   숙련도 ≥ 1 이 곧 해제라 매번 평가한다 — 저장하면 두 값이 어긋난다.
        [Header("호스트 숙련도 · 파편")]
        public string[] hostKeys = Array.Empty<string>();
        public int[] hostMastery = Array.Empty<int>();
        public int[] hostShards = Array.Empty<int>();

        // ── 보물상자 ─────────────────────────────────────────
        //
        // 세 칸이 **동시에** 시간을 센다. 저장하는 것은 «완료 시각»(Unix ms, UTC)이라
        // 앱을 꺼도 흐른다.
        //
        // ⚠ 총 소요 초를 같이 저장한다. 기기 시각을 **뒤로** 돌리면 완료 시각까지의
        //   거리가 통째로 늘어나는데, 총 소요로 잘라야 남은 시간이 부풀지 않는다.
        //   (앞으로 돌리는 치팅은 서버가 붙기 전까지 못 막는다 — TBD-SRV)
        [Header("보물상자")]
        public string[] chestKeys = Array.Empty<string>();
        public long[] chestUnlockAt = Array.Empty<long>();
        public int[] chestSeconds = Array.Empty<int>();

        // ── 능력치 강화(골드) · 성장 경로 ─────────────────────
        //
        // 첨자는 `HostStat` 순서(Hp · Atk · Crit · AtkSpeed · Range · MoveSpeed).
        //   ghostStatLevels  유령 탭에서 올린 값 — **모든 몸**에 붙는다
        //   hostStatLevels   호스트별 — `hostKeys[i]` 의 값이 [i × 6 + 능력치] 에 들어간다
        // 성장 경로 보상은 받은 칸을 비트로 적는다(칸 i → 1 << i).
        [Header("능력치 강화 · 성장 경로")]
        public int[] ghostStatLevels = Array.Empty<int>();
        public int[] hostStatLevels = Array.Empty<int>();
        public int ghostPathClaimed;

        [Header("선택")]
        public string selectedHostId = string.Empty;

        /// <summary>시작 몸을 줬는가. 한 번만 준다 — 몸을 지워도 다시 생기지 않는다.</summary>
        public bool starterGranted;

        // ── 정수 · 일일 상점 ─────────────────────────────────
        [Header("정수 · 일일 상점")]
        /// <summary>다 키운 몸에게 온 조각이 바뀐 것. 원하는 몸의 조각으로 바꾼다.</summary>
        public int essence;

        public DailyShopData dailyShop = new DailyShopData();

        // ── 행동력 · 출전 (v7) ───────────────────────────────
        [Header("행동력 · 출전")]
        public RunEntryData runEntry = new RunEntryData();

        // ── 추가 모드 (v8) ────────────────────────────────────
        [Header("추가 모드")]
        public ModeProgressData modes = new ModeProgressData();

        // ── 로비 상점 · 소환권 (2026-10-15, 기획서 B-3) ──────────
        // 버전은 올리지 않는다 — 옛 저장은 빈 값(new)으로 읽혀 그대로 맞다(산 것 없음 · 소환권 0).
        [Header("로비 상점")]
        public StoreSaveData store = new StoreSaveData();
        public int tickets;

        /// <summary>
        /// 상자 배열 셋의 길이를 칸 수에 맞춘다. 저장을 읽은 직후에 한 번 부른다 —
        /// 옛 저장(v2)은 배열이 비어 있고, 칸 수가 바뀌면 길이가 어긋난다.
        /// </summary>
        public void NormalizeChests(int slotCount)
        {
            chestKeys = Resize(chestKeys, slotCount, string.Empty);
            chestUnlockAt = Resize(chestUnlockAt, slotCount, 0L);
            chestSeconds = Resize(chestSeconds, slotCount, 0);
        }

        /// <summary>
        /// 능력치 강화 배열 길이를 맞춘다 — 옛 저장(v3)은 비어 있고, 호스트가 늘면 짧다.
        ///
        /// ⚠ 능력치 수가 늘면(6 → 8, 2026-10-06) 몸마다 칸 수가 바뀐다. 길이만 늘리면
        ///   [몸 × 6 + 능력치] 로 적힌 값이 [몸 × 8 + 능력치] 로 읽혀 **다른 몸의 강화가 섞인다.**
        ///   옛 칸 수를 길이에서 알아내 몸별로 옮긴다.
        /// </summary>
        public void NormalizeStats(int statCount)
        {
            ghostStatLevels = Resize(ghostStatLevels, statCount, 0);
            int hosts = hostKeys?.Length ?? 0;
            int len = hostStatLevels?.Length ?? 0;
            if (hosts > 0 && len > 0 && len % hosts == 0 && len / hosts != statCount && len / hosts < statCount)
            {
                int old = len / hosts;
                var moved = new int[hosts * statCount];
                for (int h = 0; h < hosts; h++)
                    for (int s = 0; s < old; s++)
                        moved[h * statCount + s] = hostStatLevels[h * old + s];
                hostStatLevels = moved;
                return;
            }
            hostStatLevels = Resize(hostStatLevels, hosts * statCount, 0);
        }

        private static T[] Resize<T>(T[] src, int n, T fill)
        {
            var dst = new T[n];
            for (int i = 0; i < n; i++) dst[i] = src != null && i < src.Length ? src[i] : fill;
            return dst;
        }

        public static UserData CreateNew() => new UserData();
    }

    /// <summary>
    /// 로비 상점 기록 — 상품별 산 횟수 · 마지막 산 날/주(제한 계산) · 첫 구매 2배 쓴 젬 묶음 · 계약 · 첫 실행 날.
    /// JsonUtility 가 Dictionary 를 못 써서 나란한 배열로 둔다(같은 첨자 = 같은 상품).
    /// </summary>
    [Serializable]
    public sealed class StoreSaveData
    {
        /// <summary>첫 실행 날(하루 경계 기준 yyyyMMdd) — 입문 패키지 7일. 0 이면 상점이 처음 열릴 때 채운다.</summary>
        public int installDay;
        public string[] ids = Array.Empty<string>();
        public int[] counts = Array.Empty<int>();
        public int[] lastDay = Array.Empty<int>();
        public int[] lastWeek = Array.Empty<int>();
        public int[] dayCounts = Array.Empty<int>();
        public int[] weekCounts = Array.Empty<int>();
        public string[] firstDoubleUsed = Array.Empty<string>();
        /// <summary>계약 — 끝나는 날(그날까지 받는다) · 마지막으로 받은 날.</summary>
        public string[] cardKeys = Array.Empty<string>();
        public int[] cardEndDay = Array.Empty<int>();
        public int[] cardLastClaimDay = Array.Empty<int>();
        /// <summary>결제는 됐는데 지급 전에 꺼진 거래(상품 id) — 다음 실행 때 지급한다.</summary>
        public string[] pending = Array.Empty<string>();

        /// <summary>조각 뽑기 — 대박까지 센 수 · 무료를 쓴 날 · 뽑은 수 · 최근 100회(「yyyyMMdd,hostKey,shards,MMddHHmm,pay,jackpot」).</summary>
        public int gachaPity;
        public int gachaFreeDay;
        public int gachaTotal;
        public string[] gachaHist = Array.Empty<string>();

        /// <summary>시즌 패스 — 시즌이 바뀌면 경험치 · 받기 · 프리미엄을 비운다. 받은 단계는 비트(단계 n → 1 &lt;&lt; n).</summary>
        public string passSeason = string.Empty;
        public int passXp;
        public bool passPremium;
        public long passClaimedFree;
        public long passClaimedPremium;

        /// <summary>미션 — 경계가 바뀌면 그 주기 카운터를 비운다. 같은 첨자 = 같은 미션.</summary>
        public int missionDay;
        public int missionWeek;
        public string[] missionIds = Array.Empty<string>();
        public int[] missionProgress = Array.Empty<int>();
        public bool[] missionClaimed = Array.Empty<bool>();
        public string[] hostsThisWeek = Array.Empty<string>();
        public int loginDay;

        /// <summary>프로필 꾸미기 — 가진 테두리 · 칭호 키, 장착(대표 얼굴은 ghost 또는 호스트 키).</summary>
        public string[] profileOwned = Array.Empty<string>();
        public string profileFrame = string.Empty;
        public string profileTitle = string.Empty;
        public string profileFace = string.Empty;
    }

    /// <summary>
    /// 그날의 일일 상점. **짠 진열을 그대로 저장한다** — 날짜로 다시 계산하면 조각을 사서 몸이
    /// 다 자란 순간 진열이 바뀐다(후보가 「덜 자란 몸」이기 때문이다).
    /// </summary>
    [Serializable]
    public sealed class DailyShopData
    {
        /// <summary>진열을 짠 날(현지 날짜 yyyyMMdd). 날이 바뀌면 다시 짠다.</summary>
        public int day;
        /// <summary>그날 젬으로 새로고침한 횟수.</summary>
        public int refreshes;
        /// <summary>산 칸(칸 i → 1 &lt;&lt; i).</summary>
        public int bought;
        public int[] kinds = Array.Empty<int>();
        public string[] keys = Array.Empty<string>();
        public int[] counts = Array.Empty<int>();
        public int[] prices = Array.Empty<int>();
    }

    /// <summary>
    /// 행동력 회복 시계 · 젬 채우기 횟수 · 오늘의 몸(기획서 `AVSR_Content_Store.md` B-12).
    /// 규칙은 `IStaminaService` · `ITodayHostService` 가 갖고, 여기는 값만 둔다.
    /// </summary>
    [Serializable]
    public sealed class RunEntryData
    {
        /// <summary>마지막으로 회복을 계산한 시각(UTC 초). 0 이면 아직 센 적이 없다 — 지금부터 센다.</summary>
        public long staminaTickUtc;
        /// <summary>젬 채우기를 센 날(하루 경계 기준 yyyyMMdd)과 그날 횟수.</summary>
        public int refillDay;
        public int refillCount;
        /// <summary>
        /// 오늘의 몸을 뽑은 날과 그 몸들. **다시 계산하지 않고 저장한다** — 그날 몸을 새로 얻어도
        /// 목록이 바뀌면 안 된다(아침에 본 무료 몸이 낮에 사라지면 속은 느낌이 든다).
        /// </summary>
        public int todayDay;
        public string[] todayHosts = Array.Empty<string>();
    }

    /// <summary>
    /// 망령의 탑 · 망령의 밤 진행(기획서 AVSR_Content_Tower 4-3 · NightMode 4-3). 규칙은 `IModeProgress` 가 갖고 여기는 값만.
    /// 하루 · 주는 `IResetClock` 의 DayKey · WeekKey(일본 05:00 · 월요일).
    /// </summary>
    [Serializable]
    public sealed class ModeProgressData
    {
        // 탑
        public int towerWeekId;          // 이번 주(WeekKey). 바뀌면 주간 초기화
        public int towerWeekCleared;     // 이번 주 깬 층
        public int towerLastWeekBest;    // 지난주 최고(비교 표시)
        public int towerAllTimeBest;
        public int towerDayId;           // 오늘(DayKey). 바뀌면 입장 · 젬 횟수 · 소탕 채움
        public int towerEntries;         // 오늘 남은 입장(무료 + 산 것)
        public int towerBoughtToday;
        public bool towerSweptToday;

        // 밤
        public int nightDayId;
        public int nightEntries;
        public int nightBoughtToday;
        public int[] nightBestKills = new int[7];   // 요일(월 0 … 일 6)별 최고 처치
        public int[] nightBestTime = new int[7];    // 요일별 최고 버틴 초
        public int nightClears;

        /// <summary>시각을 뒤로 돌린 것을 알아보는 마지막 채운 시각(UTC 초).</summary>
        public long lastRefillUtc;

        public void Normalize()
        {
            if (nightBestKills == null || nightBestKills.Length != 7) nightBestKills = Resize(nightBestKills);
            if (nightBestTime == null || nightBestTime.Length != 7) nightBestTime = Resize(nightBestTime);
        }

        private static int[] Resize(int[] src)
        {
            var a = new int[7];
            if (src != null) Array.Copy(src, a, Math.Min(7, src.Length));
            return a;
        }
    }
}
