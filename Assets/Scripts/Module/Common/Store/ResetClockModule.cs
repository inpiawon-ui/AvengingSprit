using System;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Common;

namespace Game.Module.Common.Store
{
    /// <summary>
    /// 하루 · 주 경계를 한 곳에서 잰다(기획서 `AVSR_Content_Store.md` B-1). 지금은 일본 05:00 · 월요일(PD 2026-10-15) —
    /// `GameConfig.DailyResetUtcHour` · `WeekResetDay` 만 바꾸면 오늘의 몸 · 젬 채우기 · 미션 · 탑 · 밤이 같이 옮겨 간다.
    /// ⚠ 하루 경계를 따로 재는 곳을 만들지 않는다 — 화면마다 날이 바뀌는 시각이 달라진다.
    /// </summary>
    public interface IResetClock
    {
        /// <summary>하루 경계 기준 날짜(yyyyMMdd).</summary>
        int DayKey { get; }

        /// <summary>다음 경계까지 남은 시간.</summary>
        TimeSpan UntilNextDay { get; }

        /// <summary>이번 주가 시작된 날의 <see cref="DayKey"/>(주 시작 요일 · 하루 경계 기준).</summary>
        int WeekKey { get; }

        /// <summary>다음 주 경계까지 남은 시간.</summary>
        TimeSpan UntilNextWeek { get; }
    }

    [Module(Layer = ModuleLayer.Game)]
    public sealed class ResetClockModule : IModule
    {
        private ResetClock _clock;

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            _clock = new ResetClock();
            CoreModule.Register<IResetClock>(_clock);
            IsInitialized = true;
        }

        public void Initialize() { }

        public void Dispose()
        {
            CoreModule.Unregister<IResetClock>();
            _clock = null;
            IsInitialized = false;
        }
    }

    internal sealed class ResetClock : IResetClock
    {
        // 표가 아직 안 읽혔을 때(부팅 직후)의 값 — 일본 05:00
        private const int FallbackUtcHour = 20;

        public int DayKey
        {
            get
            {
                var d = Shifted.Date;
                return d.Year * 10000 + d.Month * 100 + d.Day;
            }
        }

        public TimeSpan UntilNextDay
        {
            get
            {
                var s = Shifted;
                return s.Date.AddDays(1) - s;
            }
        }

        public int WeekKey
        {
            get
            {
                var d = WeekStart(Shifted.Date);
                return d.Year * 10000 + d.Month * 100 + d.Day;
            }
        }

        public TimeSpan UntilNextWeek
        {
            get
            {
                var s = Shifted;
                return WeekStart(s.Date).AddDays(7) - s;
            }
        }

        private static DateTime WeekStart(DateTime day)
        {
            var start = DayOfWeek.Monday;
            if (CoreModule.TryGet<IPlayerDataService>(out var p) && p.Config != null) start = p.Config.WeekResetDay;
            int back = ((int)day.DayOfWeek - (int)start + 7) % 7;
            return day.AddDays(-back);
        }

        // 경계 시각을 자정으로 옮긴 「게임 시계」. 경계가 UTC 20시(일본 05:00)면 UTC+4 = 일본 시각 − 5시간이다.
        // 요일도 이 날짜로 센다 — 일본 월요일 05:00(UTC 일요일 20:00)이 여기서 월요일 0시가 된다.
        private static DateTime Shifted
        {
            get
            {
                int h = FallbackUtcHour;
                if (CoreModule.TryGet<IPlayerDataService>(out var p) && p.Config != null) h = p.Config.DailyResetUtcHour;
                return DateTime.UtcNow.AddHours(h == 0 ? 0 : 24 - h);
            }
        }
    }
}
