using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Events;
using GameFramework.Core.Module.EventBus;
using UnityEngine;

namespace Game.User
{
    /// <summary>
    /// 유저 데이터의 단일 소유자. 다른 코드는 `UserData` 를 직접 들고 있지 않는다.
    /// 모든 변경이 여기를 지나므로 이벤트 발행과 저장 시점을 한 곳에서 통제할 수 있다.
    /// </summary>
    public sealed class PlayerDataService : IPlayerDataService
    {
        private readonly IUserDataRepository _repo;
        private readonly IEventBus _bus;
        private readonly HostTable _hosts;
        private readonly UltimateTable _ultimates;

        private UserData _data;

        public PlayerDataService(IUserDataRepository repo, IEventBus bus,
                                 HostTable hosts, UltimateTable ultimates)
        {
            _repo = repo;
            _bus = bus;
            _hosts = hosts;
            _ultimates = ultimates;
        }

        public bool IsReady => _data != null;

        public int Stamina    => _data?.stamina    ?? 0;
        public int StaminaMax => _data?.staminaMax ?? 0;
        public int Gold       => _data?.gold       ?? 0;
        public int Gem        => _data?.gem        ?? 0;
        public int SpiritCore => _data?.spiritCore ?? 0;
        public int HostMemory => _data?.hostMemory ?? 0;

        public int GhostLevel  => _data?.ghostLevel  ?? 1;
        public int GhostExp    => _data?.ghostExp    ?? 0;
        public int GhostExpMax => _data?.ghostExpMax ?? 100;

        public int CurrentChapter => _data?.currentChapter ?? 1;
        public int ReachedStage   => _data?.reachedStage   ?? 1;
        public int ClearedChapter => _data?.clearedChapter ?? 0;

        public IReadOnlyList<HostEntry> AllHosts
            => _hosts != null ? _hosts.Entries : System.Array.Empty<HostEntry>();

        public string SelectedHostId
        {
            get
            {
                if (_data != null && !string.IsNullOrEmpty(_data.selectedHostId))
                {
                    // 저장된 선택이 아직 잠겨 있으면 시작 보유 호스트로 되돌린다
                    var e = GetHost(_data.selectedHostId);
                    if (e != null && IsHostUnlocked(e)) return _data.selectedHostId;
                }
                return _hosts != null ? _hosts.FirstOwned()?.HostKey ?? string.Empty : string.Empty;
            }
        }

        public async UniTask LoadAsync()
        {
            _data = await _repo.LoadAsync();
            _bus?.Publish(new UserDataReadyEvent { LoadedGhostLevel = _data.ghostLevel });
            PublishCurrency();
        }

        public UniTask SaveAsync()
            => _data == null ? UniTask.CompletedTask : _repo.SaveAsync(_data).AsUniTask();

        /// <summary>
        /// 해금 조건을 무시하고 전부 열어 둔다 — **테스트용**이다.
        /// 21종을 다 만져 봐야 밸런스를 판단할 수 있는데, 정상 진행으로는
        /// 챕터를 깨야 열려서 확인에 며칠이 걸린다.
        ///
        /// ⚠ 출시 전에 반드시 false 로 되돌린다. 켜 두면 해금이라는 성장 축이 통째로 사라진다.
        /// </summary>
        public const bool UnlockAllForTest = true;

        public bool IsHostUnlocked(HostEntry host)
        {
            if (host == null) return false;
            if (UnlockAllForTest) return true;
            return host.UnlockType switch
            {
                HostUnlockType.Owned => true,
                HostUnlockType.StageReach =>
                    ClearedChapter >= host.UnlockChapter ||
                    (CurrentChapter == host.UnlockChapter && ReachedStage >= host.UnlockStage) ||
                    CurrentChapter > host.UnlockChapter,
                HostUnlockType.ChapterBossClear => ClearedChapter >= host.UnlockChapter,
                _ => false,
            };
        }

        public HostEntry GetHost(string hostKey) => _hosts != null ? _hosts.Get(hostKey) : null;

        public UltimateEntry GetUltimate(string ultimateKey)
            => _ultimates != null ? _ultimates.Get(ultimateKey) : null;

        public int OwnedHostCount
        {
            get
            {
                if (_hosts == null) return 0;
                int n = 0;
                var list = _hosts.Entries;
                for (int i = 0; i < list.Count; i++)
                    if (IsHostUnlocked(list[i])) n++;
                return n;
            }
        }

        public void SelectHost(string hostKey)
        {
            if (_data == null || string.IsNullOrEmpty(hostKey)) return;
            var e = GetHost(hostKey);
            if (e == null) return;

            bool unlocked = IsHostUnlocked(e);
            // 잠금 호스트도 선택은 허용한다(다음 목표 확인). 단 저장은 해금된 것만.
            if (unlocked) _data.selectedHostId = hostKey;

            _bus?.Publish(new HostSelectedEvent { SelectedKey = hostKey, IsUnlockedHost = unlocked });
        }

        public bool TrySpendStamina(int amount)
        {
            if (_data == null || amount <= 0 || _data.stamina < amount) return false;
            _data.stamina -= amount;
            PublishCurrency();
            return true;
        }

        public void AddCurrency(int gold, int gem)
        {
            if (_data == null) return;
            _data.gold = Mathf.Max(0, _data.gold + gold);
            _data.gem  = Mathf.Max(0, _data.gem + gem);
            PublishCurrency();
        }

        public void SetProgress(int chapter, int stage)
        {
            if (_data == null) return;
            _data.currentChapter = Mathf.Max(1, chapter);
            _data.reachedStage   = Mathf.Max(1, stage);
            _bus?.Publish(new ProgressChangedEvent
            {
                NewChapter        = _data.currentChapter,
                NewStage          = _data.reachedStage,
                NewClearedChapter = _data.clearedChapter,
            });
        }

        public UniTask GrantStageRewardAsync(int gold, int ghostExp, bool cleared)
            => GrantStageRewardAsync(gold, ghostExp, cleared, 0, 0, 0);

        /// <summary>
        /// 런의 결과를 반영한다. 정본 REWARD_DB 는 방·정예·챕터마다 다른 재화를 준다 —
        /// 골드만 주면 보스를 잡을 이유가 "다음 방으로 간다" 뿐이게 된다.
        /// </summary>
        public async UniTask GrantStageRewardAsync(int gold, int ghostExp, bool cleared,
                                                   int spiritCore, int hostMemory, int gem)
        {
            if (_data == null) return;

            _data.gold = Mathf.Max(0, _data.gold + Mathf.Max(0, gold));
            _data.gem = Mathf.Max(0, _data.gem + Mathf.Max(0, gem));
            // 영구 재화는 실패한 런에서도 남긴다. 정본이 "Run 은 끝나지만 Ghost 의 성장은
            // 계속된다" 를 성장 시스템의 한 줄 요지로 세웠다.
            _data.spiritCore = Mathf.Max(0, _data.spiritCore + Mathf.Max(0, spiritCore));
            _data.hostMemory = Mathf.Max(0, _data.hostMemory + Mathf.Max(0, hostMemory));
            PublishCurrency();

            // 고스트 EXP — 넘치면 레벨업하고 남은 양을 이월한다
            _data.ghostExp += Mathf.Max(0, ghostExp);
            while (_data.ghostExpMax > 0 && _data.ghostExp >= _data.ghostExpMax)
            {
                _data.ghostExp -= _data.ghostExpMax;
                _data.ghostLevel++;
                _data.ghostExpMax += 20;
            }
            _bus?.Publish(new GhostProgressChangedEvent
            {
                NewLevel = _data.ghostLevel,
                NewExp = _data.ghostExp,
                NewExpMax = _data.ghostExpMax,
            });

            // 클리어했을 때만 스테이지를 전진시킨다. 실패는 진행도를 건드리지 않는다.
            if (cleared) SetProgress(_data.currentChapter, _data.reachedStage + 1);

            await SaveAsync();
        }

        private void PublishCurrency()
        {
            if (_data == null) return;
            _bus?.Publish(new CurrencyChangedEvent
            {
                NewStamina = _data.stamina,
                NewGold    = _data.gold,
                NewGem     = _data.gem,
            });
        }
    }
}
