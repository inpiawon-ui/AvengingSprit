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

        public bool IsHostUnlocked(HostEntry host)
        {
            if (host == null) return false;
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
