namespace Game.Module.Common.Modes
{
    /// <summary>한 판이 어느 모드인가. 챕터(본편) · 망령의 탑 · 망령의 밤.</summary>
    public enum RunMode
    {
        Chapter,
        Tower,
        Night,
    }

    /// <summary>
    /// 로비 → 인게임으로 넘기는 「이번 판은 어떤 모드인가」. 저장하지 않는다.
    ///
    /// 판 시작은 챕터 번호도 요청에 싣지 않고 `IPlayerDataService.SelectedChapter` 를 읽는다 —
    /// 모드도 같은 방식으로 씬 밖 값으로 넘긴다. 판이 끝나면 <see cref="Clear"/> 로 본편으로 되돌린다
    /// (안 되돌리면 다음 챕터 판이 모드로 시작한다).
    /// </summary>
    public static class RunModeContext
    {
        public static RunMode Current { get; private set; } = RunMode.Chapter;

        /// <summary>탑 — 이번 판에서 도전할 층.</summary>
        public static int TowerFloor { get; private set; }

        /// <summary>밤 — 입장할 때의 요일(0 = 월 … 6 = 일, 하루 경계 기준).</summary>
        public static int NightDay { get; private set; }

        /// <summary>판을 시작한 주(`IResetClock.WeekKey`) — 주가 넘어가며 끝난 판의 층을 새 주에 넣지 않게.</summary>
        public static int StartWeekKey { get; private set; }

        public static bool IsTower => Current == RunMode.Tower;
        public static bool IsNight => Current == RunMode.Night;
        public static bool IsMode => Current != RunMode.Chapter;

        public static void BeginTower(int floor, int weekKey)
        {
            Current = RunMode.Tower;
            TowerFloor = floor;
            StartWeekKey = weekKey;
        }

        public static void BeginNight(int day, int weekKey)
        {
            Current = RunMode.Night;
            NightDay = day;
            StartWeekKey = weekKey;
        }

        /// <summary>탑 결과 창의 「다음 층 도전」 — 같은 판 흐름에서 층만 올린다.</summary>
        public static void SetTowerFloor(int floor) => TowerFloor = floor;

        public static void Clear()
        {
            Current = RunMode.Chapter;
            TowerFloor = 0;
            NightDay = 0;
            StartWeekKey = 0;
        }
    }
}
