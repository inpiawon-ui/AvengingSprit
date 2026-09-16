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
        public const int CurrentSaveVersion = 3;

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

        [Header("선택")]
        public string selectedHostId = string.Empty;

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

        private static T[] Resize<T>(T[] src, int n, T fill)
        {
            var dst = new T[n];
            for (int i = 0; i < n; i++) dst[i] = src != null && i < src.Length ? src[i] : fill;
            return dst;
        }

        public static UserData CreateNew() => new UserData();
    }
}
