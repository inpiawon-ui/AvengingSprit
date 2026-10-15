using System;
using Cysharp.Threading.Tasks;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;

namespace Game.Module.Common.Store
{
    /// <summary>
    /// 행동력 — 궁수의 전설과 같은 규칙(기획서 `AVSR_Content_Store.md` B-12).
    /// 최대 20 · 판 입장 5 · 12분에 1 회복 · 깨면 5 돌려줌 · 젬으로 가득 채우기(하루 횟수 제한).
    /// </summary>
    public interface IStaminaService
    {
        int Current { get; }
        int Max { get; }

        /// <summary>다음 1 회복까지. 가득 찼거나 넘쳤으면 Zero.</summary>
        TimeSpan UntilNext { get; }

        /// <summary>판 하나 들어가는 값.</summary>
        int PerRun { get; }

        bool CanEnter { get; }

        /// <summary>판 시작 — 모자라면 false(부족 창은 화면이 띄운다).</summary>
        bool TryPayEntry();

        int RefillGemCost { get; }
        int RefillLeftToday { get; }

        /// <summary>젬을 내고 최대만큼 채운다. 실패하면 이유를 문자열 키로(`ui.stamina.fail.*`).</summary>
        bool TryRefillWithGem(out string failKey);

        /// <summary>회복 시계를 지금에 맞춘다. 로비가 열릴 때 · 상단 바 1초 타이머에서 부른다.</summary>
        void Sync();
    }

    [Module(Layer = ModuleLayer.Game)]
    public sealed class StaminaModule : IModule
    {
        private StaminaService _service;
        private IDisposable _token;

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            _service = new StaminaService();
            CoreModule.Register<IStaminaService>(_service);
            IsInitialized = true;
        }

        public void Initialize()
            => _token = CoreModule.Get<IEventBus>().Subscribe<StageFinishedEvent>(_service.OnStageFinished);

        public void Dispose()
        {
            _token?.Dispose();
            _token = null;
            CoreModule.Unregister<IStaminaService>();
            _service = null;
            IsInitialized = false;
        }
    }

    internal sealed class StaminaService : IStaminaService
    {
        private static IPlayerDataService Player
            => CoreModule.TryGet<IPlayerDataService>(out var p) && p.IsReady ? p : null;

        private static Game.Character.GameConfig Config => Player?.Config;

        private static long NowUtc => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public int Current { get { Sync(); return Player?.Stamina ?? 0; } }

        public int Max => Config != null ? Config.StaminaMax : 20;

        public int PerRun => Config != null ? Config.StaminaPerRun : 5;

        public bool CanEnter => Current >= PerRun;

        public TimeSpan UntilNext
        {
            get
            {
                Sync();
                var p = Player;
                if (p == null || Config == null || p.Stamina >= Max) return TimeSpan.Zero;
                long next = p.RunEntry.staminaTickUtc + Config.StaminaRegenSeconds;
                return TimeSpan.FromSeconds(Math.Max(0, next - NowUtc));
            }
        }

        public void Sync()
        {
            var p = Player;
            var cfg = Config;
            if (p == null || cfg == null) return;
            var d = p.RunEntry;
            long now = NowUtc;
            // 가득 찼거나 넘쳤으면 시계를 지금에 붙여 둔다 — 쓰는 순간부터 12분을 센다
            if (p.Stamina >= cfg.StaminaMax || d.staminaTickUtc <= 0 || d.staminaTickUtc > now)
            {
                // ⚠ 기기 시각을 뒤로 돌렸으면(`> now`) 그 사이는 없던 것으로 친다 — 회복을 거저 주지 않는다
                d.staminaTickUtc = now;
                return;
            }
            long gained = (now - d.staminaTickUtc) / cfg.StaminaRegenSeconds;
            if (gained <= 0) return;
            int room = cfg.StaminaMax - p.Stamina;
            if (gained >= room)
            {
                p.SetStamina(cfg.StaminaMax);
                d.staminaTickUtc = now;
            }
            else
            {
                p.SetStamina(p.Stamina + (int)gained);
                d.staminaTickUtc += gained * cfg.StaminaRegenSeconds;   // 남은 몫(초)은 다음 회복으로 이어진다
            }
        }

        public bool TryPayEntry()
        {
            Sync();
            var p = Player;
            if (p == null) return false;
            int cost = PerRun;
            if (cost <= 0) return true;
            bool wasFull = p.Stamina >= Max;
            if (!p.TrySpendStamina(cost)) return false;
            // 가득 찬 채 쓰면 그때부터 센다(가득 찬 동안은 시계가 멈춰 있다)
            if (wasFull && p.Stamina < Max) p.RunEntry.staminaTickUtc = NowUtc;
            return true;
        }

        public int RefillGemCost => Config != null ? Config.StaminaRefillGem : 100;

        public int RefillLeftToday
        {
            get
            {
                var p = Player;
                if (p == null || Config == null) return 0;
                int used = p.RunEntry.refillDay == Today ? p.RunEntry.refillCount : 0;
                return Math.Max(0, Config.StaminaRefillPerDay - used);
            }
        }

        public bool TryRefillWithGem(out string failKey)
        {
            failKey = null;
            Sync();
            var p = Player;
            if (p == null) { failKey = "ui.stamina.fail.notready"; return false; }
            if (RefillLeftToday <= 0) { failKey = "ui.stamina.fail.limit"; return false; }
            if (p.Gem < RefillGemCost) { failKey = "ui.stamina.fail.gem"; return false; }
            if (!p.TrySpendGem(RefillGemCost)) { failKey = "ui.stamina.fail.gem"; return false; }
            var d = p.RunEntry;
            if (d.refillDay != Today) { d.refillDay = Today; d.refillCount = 0; }
            d.refillCount++;
            // 「최대만큼 더한다」 — 남은 값 위에 얹어 넘칠 수 있다(궁수의 전설과 같다)
            p.SetStamina(p.Stamina + Max);
            p.SaveAsync().Forget();   // fire-and-forget: 젬을 냈다 — 화면을 기다리게 할 이유가 없다
            return true;
        }

        internal void OnStageFinished(StageFinishedEvent e)
        {
            if (!e.IsCleared || Config == null) return;
            var p = Player;
            if (p == null) return;
            Sync();
            p.SetStamina(p.Stamina + Config.StaminaOnClear);
            // 저장은 클리어 지급(`GrantChapterClearAsync`)이 곧 한다
        }

        private static int Today => CoreModule.TryGet<IResetClock>(out var c) ? c.DayKey : 0;
    }
}
