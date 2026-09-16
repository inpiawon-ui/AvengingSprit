using System;
using Cysharp.Threading.Tasks;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
using UnityEngine;

namespace Game.Module.Common.Chest
{
    /// <summary>
    /// 보물상자 (2026-09-16 기획 — 크래시 로얄 참고).
    ///
    /// 판을 끝내면 상자가 나오고, 로비 세 칸에 담겨 **실시간으로** 열린다.
    /// 자동 전투라 손을 떼는 시간이 긴 게임이므로, 돌아올 이유를 만드는 장치다.
    ///
    /// 자세한 규칙과 그 이유는 `Assets/Scripts/Module/CLAUDE.md` 6절.
    /// </summary>
    [Module(Layer = ModuleLayer.Game)]
    public sealed class ChestModule : IModule, ITickable
    {
        private ChestService _service;

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            _service = new ChestService();
            CoreModule.Register<IChestService>(_service);
            IsInitialized = true;
        }

        public void Initialize()
        {
            _service.Bind(CoreModule.Get<IEventBus>());
            _service.LoadAsync().Forget();   // fire-and-forget: 표가 오기 전엔 빈 칸으로 그린다
        }

        public void Tick(float deltaTime) => _service?.Tick(deltaTime);

        public void Dispose()
        {
            _service?.Shutdown();
            CoreModule.Unregister<IChestService>();
            _service = null;
            IsInitialized = false;
        }
    }

    internal sealed class ChestService : IChestService
    {
        private const string TableAddress = "TableData/ChestTable";

        /// <summary>완료 여부를 다시 보는 주기. 초 단위 표시라 1초면 충분하다.</summary>
        private const float PollSeconds = 1f;

        private ChestTable _table;
        private IEventBus _bus;
        private IPlayerDataService _player;
        private float _pollTimer;
        private bool _wasReady0, _wasReady1, _wasReady2;

        public int SlotCount => 3;

        public void Bind(IEventBus bus) => _bus = bus;

        public async UniTask LoadAsync()
        {
            try
            {
                _table = await CoreModule.Get<IResourceManager>().LoadAsync<ChestTable>(TableAddress);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Chest] 표를 못 읽었다: {e.Message}");
            }
            _bus?.Publish(new ChestChangedEvent());
        }

        public void Shutdown()
        {
            _table = null;
            _bus = null;
            _player = null;
        }

        // ── 칸 읽기 ──────────────────────────────────────────────

        private IPlayerDataService Player
        {
            get
            {
                if (_player == null) CoreModule.TryGet<IPlayerDataService>(out _player);
                return _player;
            }
        }

        private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public ChestSlotState Get(int slot)
        {
            var p = Player;
            if (p == null || !p.IsReady || slot < 0 || slot >= SlotCount)
                return new ChestSlotState(null, 0, 0, 0);

            string key = p.GetChestKey(slot);
            if (string.IsNullOrEmpty(key)) return new ChestSlotState(null, 0, 0, 0);

            int total = Mathf.Max(1, p.GetChestSeconds(slot));
            // ⚠ 기기 시각을 뒤로 돌리면 남은 시간이 통째로 늘어난다 — 총 소요로 잘라 막는다.
            long ms = p.GetChestUnlockAt(slot) - NowMs();
            int remain = Mathf.Clamp(Mathf.CeilToInt(ms / 1000f), 0, total);
            return new ChestSlotState(key, remain, total, GemCost(key, remain));
        }

        /// <summary>즉시 열기 젬값 = 남은 «분» × 등급별 분당 젬값. 1분 미만도 한 푼은 받는다.</summary>
        private int GemCost(string chestKey, int remainSeconds)
        {
            if (remainSeconds <= 0) return 0;
            var e = _table?.Find(chestKey);
            float perMinute = e?.GemPerMinute ?? 3f;
            return Mathf.Max(1, Mathf.CeilToInt(remainSeconds / 60f * perMinute));
        }

        public bool Has(string chestKey) => _table?.Find(chestKey) != null;

        public string SpriteOf(string chestKey) => _table?.Find(chestKey)?.Sprite ?? string.Empty;

        // ── 담기 · 열기 ──────────────────────────────────────────

        public bool TryGrant(string chestKey, out int slot)
        {
            slot = -1;
            var p = Player;
            var entry = _table?.Find(chestKey);
            if (p == null || !p.IsReady || entry == null) return false;

            for (int i = 0; i < SlotCount; i++)
            {
                if (!string.IsNullOrEmpty(p.GetChestKey(i))) continue;
                slot = i;
                break;
            }
            if (slot < 0) return false;   // 칸이 다 찼다 — 조용히 버리지 않고 알린다

            p.SetChestSlot(slot, chestKey,
                           NowMs() + (long)entry.UnlockSeconds * 1000L, entry.UnlockSeconds);
            Save();
            _bus?.Publish(new ChestChangedEvent());
            return true;
        }

        public bool TryOpenNow(int slot)
        {
            var p = Player;
            if (p == null || !p.IsReady) return false;
            var s = Get(slot);
            if (s.IsEmpty || s.IsReady) return false;
            if (p.Gem < s.GemCost) return false;

            p.AddCurrency(0, -s.GemCost);
            p.SetChestSlot(slot, s.ChestKey, NowMs(), s.TotalSeconds);
            Save();
            _bus?.Publish(new ChestChangedEvent());
            return true;
        }

        public bool TryClaim(int slot, out ChestReward reward)
        {
            reward = default;
            var p = Player;
            if (p == null || !p.IsReady) return false;
            var s = Get(slot);
            if (!s.IsReady) return false;

            var e = _table.Find(s.ChestKey);
            if (e == null) return false;

            // ⚠ 보상은 **열 때** 굴린다. 받을 때 굴려 저장하면 저장 파일을 보고
            //   마음에 안 들 때 다시 받는 식이 가능해진다.
            reward = Roll(e, p);
            p.AddGrowthCurrency(reward.Gold, reward.Gem, reward.SpiritCore, reward.HostMemory);
            if (reward.Shards > 0 && !string.IsNullOrEmpty(reward.ShardHostKey))
                p.AddShards(reward.ShardHostKey, reward.Shards);

            p.SetChestSlot(slot, string.Empty, 0, 0);
            Save();

            _bus?.Publish(new ChestOpenedEvent
            {
                OpenedChestKey = e.Key,
                RewardGold = reward.Gold,
                RewardSpiritCore = reward.SpiritCore,
                RewardHostMemory = reward.HostMemory,
                RewardGem = reward.Gem,
                RewardShards = reward.Shards,
                RewardShardHostKey = reward.ShardHostKey,
            });
            _bus?.Publish(new ChestChangedEvent());
            return true;
        }

        private static ChestReward Roll(ChestEntry e, IPlayerDataService p)
        {
            int gold = UnityEngine.Random.Range(e.GoldMin, e.GoldMax + 1);
            int core = UnityEngine.Random.Range(e.CoreMin, e.CoreMax + 1);
            int memory = UnityEngine.Random.Range(e.MemoryMin, e.MemoryMax + 1);
            int gem = UnityEngine.Random.Range(e.GemMin, e.GemMax + 1);
            int shards = UnityEngine.Random.Range(e.ShardMin, e.ShardMax + 1);

            // 파편은 **고를 수 있는 몸 중에서** 하나를 뽑아 준다. 전투 전용 배우에게 주면
            // 로비에서 쓸 곳이 없어 받은 티가 안 난다.
            string host = string.Empty;
            var list = p.PlayableHosts;
            if (shards > 0 && list != null && list.Count > 0)
            {
                var pick = list[UnityEngine.Random.Range(0, list.Count)];
                host = pick != null ? pick.HostKey : string.Empty;
            }
            if (string.IsNullOrEmpty(host)) shards = 0;
            return new ChestReward(gold, core, memory, gem, shards, host);
        }

        private void Save()
        {
            var p = Player;
            if (p != null) p.SaveAsync().Forget();   // fire-and-forget: 저장 실패는 다음 저장이 덮는다
        }

        // ── 완료 감시 ────────────────────────────────────────────
        //
        // 화면이 매 프레임 물어보면 되지만, 로비를 안 보고 있어도 완료를 알려야
        // 하단 바 배지를 켤 수 있다. 1초에 한 번만 본다.

        public void Tick(float deltaTime)
        {
            _pollTimer += deltaTime;
            if (_pollTimer < PollSeconds) return;
            _pollTimer = 0f;

            bool r0 = Get(0).IsReady, r1 = Get(1).IsReady, r2 = Get(2).IsReady;
            if (r0 == _wasReady0 && r1 == _wasReady1 && r2 == _wasReady2) return;
            _wasReady0 = r0; _wasReady1 = r1; _wasReady2 = r2;
            _bus?.Publish(new ChestChangedEvent());
        }
    }
}
