using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Common;

namespace Game.Module.Common.Store
{
    /// <summary>
    /// 오늘의 몸 — 하루 경계마다 가진 몸 중 3명을 뽑아 그날 골드 출전비를 0 으로(기획서 B-12).
    /// 판 수 제한은 행동력이 한다.
    /// </summary>
    public interface ITodayHostService
    {
        /// <summary>오늘의 몸 키. 날이 바뀌었으면 먼저 다시 뽑는다.</summary>
        IReadOnlyList<string> Today { get; }

        bool IsToday(string hostKey);

        TimeSpan UntilReset { get; }
    }

    [Module(Layer = ModuleLayer.Game)]
    public sealed class TodayHostModule : IModule
    {
        private TodayHostService _service;

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            _service = new TodayHostService();
            CoreModule.Register<ITodayHostService>(_service);
            IsInitialized = true;
        }

        public void Initialize() { }

        public void Dispose()
        {
            CoreModule.Unregister<ITodayHostService>();
            _service = null;
            IsInitialized = false;
        }
    }

    internal sealed class TodayHostService : ITodayHostService
    {
        private readonly List<string> _today = new();
        private readonly List<string> _pool = new();
        private int _shownDay = -1;

        public IReadOnlyList<string> Today
        {
            get { Ensure(); return _today; }
        }

        public bool IsToday(string hostKey)
        {
            if (string.IsNullOrEmpty(hostKey)) return false;
            Ensure();
            return _today.Contains(hostKey);
        }

        public TimeSpan UntilReset
            => CoreModule.TryGet<IResetClock>(out var c) ? c.UntilNextDay : TimeSpan.Zero;

        private void Ensure()
        {
            if (!CoreModule.TryGet<IPlayerDataService>(out var p) || !p.IsReady) return;
            if (!CoreModule.TryGet<IResetClock>(out var clock)) return;
            int day = clock.DayKey;
            var d = p.RunEntry;
            if (d.todayDay != day) Roll(p, d, day);
            if (_shownDay == day && _today.Count == (d.todayHosts?.Length ?? 0)) return;
            _shownDay = day;
            _today.Clear();
            if (d.todayHosts != null) _today.AddRange(d.todayHosts);
        }

        private void Roll(IPlayerDataService p, RunEntryData d, int day)
        {
            _pool.Clear();
            var hosts = p.PlayableHosts;
            for (int i = 0; i < hosts.Count; i++)
            {
                var e = hosts[i];
                // 유령 · 숨은 몸(사신)은 빼고, **가진 몸만** — 공짜로 쓰는 날이지 빌려주는 날이 아니다
                if (e == null || e.IsGhost || PlayerDataService.IsHiddenHost(e.HostKey)) continue;
                if (p.IsHostOwned(e)) _pool.Add(e.HostKey);
            }
            int n = Math.Min(p.Config != null ? p.Config.TodayFreeHosts : 3, _pool.Count);
            var picked = new string[n];
            for (int i = 0; i < n; i++)
            {
                int at = UnityEngine.Random.Range(i, _pool.Count);
                (_pool[i], _pool[at]) = (_pool[at], _pool[i]);
                picked[i] = _pool[i];
            }
            d.todayDay = day;
            d.todayHosts = picked;
            p.SaveAsync().Forget();   // fire-and-forget: 뽑은 목록이 남아야 다시 열어도 같다
        }
    }
}
