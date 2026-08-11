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

        /// <summary>현재 선택된 호스트 키. 없으면 시작 보유 호스트로 대체된다.</summary>
        string SelectedHostId { get; }

        UniTask LoadAsync();
        UniTask SaveAsync();

        /// <summary>진행도로 해금 여부를 평가한다. 저장하지 않고 매번 계산한다.</summary>
        bool IsHostUnlocked(HostEntry host);

        IReadOnlyList<HostEntry> AllHosts { get; }
        HostEntry GetHost(string hostKey);
        UltimateEntry GetUltimate(string ultimateKey);

        /// <summary>보유 호스트 수 / 전체. 하단 `보유 HOST n/12` 표시용.</summary>
        int OwnedHostCount { get; }

        /// <summary>선택 호스트 변경. 잠금 호스트도 선택은 가능하다(목표 확인용).</summary>
        void SelectHost(string hostKey);

        /// <summary>스테이지 진입 비용 차감. 부족하면 false 를 돌려주고 아무것도 바꾸지 않는다.</summary>
        bool TrySpendStamina(int amount);

        void AddCurrency(int gold, int gem);
        void SetProgress(int chapter, int stage);

        /// <summary>
        /// 스테이지 종료 보상. 골드·고스트 EXP 지급 후 저장까지 한 번에 처리한다.
        /// `cleared` 일 때만 스테이지를 전진시킨다 — 실패는 진행도를 건드리지 않는다.
        /// </summary>
        UniTask GrantStageRewardAsync(int gold, int ghostExp, bool cleared);

        /// <summary>정본 REWARD_DB 를 반영하는 확장형. 영구 재화는 실패해도 남는다.</summary>
        UniTask GrantStageRewardAsync(int gold, int ghostExp, bool cleared,
                                      int spiritCore, int hostMemory, int gem);
    }
}
