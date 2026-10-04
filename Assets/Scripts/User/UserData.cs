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
        public const int CurrentSaveVersion = 6;

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
        public int stamina = 30;
        public int staminaMax = 30;
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

        /// <summary>능력치 강화 배열 길이를 맞춘다 — 옛 저장(v3)은 비어 있고, 호스트가 늘면 짧다.</summary>
        public void NormalizeStats(int statCount)
        {
            ghostStatLevels = Resize(ghostStatLevels, statCount, 0);
            hostStatLevels = Resize(hostStatLevels, (hostKeys?.Length ?? 0) * statCount, 0);
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
}
