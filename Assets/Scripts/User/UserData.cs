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
        public const int CurrentSaveVersion = 1;

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

        [Header("선택")]
        public string selectedHostId = string.Empty;

        public static UserData CreateNew() => new UserData();
    }
}
