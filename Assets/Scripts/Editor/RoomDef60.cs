// ⚠ 이 파일은 손으로 고치지 않는다. 아래 도구가 굽는다.
//   원본  Projects/AVSR/_exchange/out/42_jobs/AVSR_Rooms60.js
//   생성  scratchpad/gen_rooms60.py
//
// 고칠 것이 있으면 **원본을 고치고 다시 굽는다.** 여기를 고치면 다음 납품에
// 조용히 덮어써진다 — 어느 쪽이 정본인지 알 수 없게 되는 것이 가장 나쁘다.

namespace Game.EditorTools
{
    /// <summary>
    /// 6챕터 × 10방 = 60방 배정 (정본 v1.0 · 2026-09-01).
    ///
    /// 정본이 새로 만든 것은 **배정뿐**이다 — 레이아웃 12종 · 자리 10개 ·
    /// 잡몹 7종 · 호스트 23명은 이미 있던 것을 그대로 쓴다.
    /// 새 잡몹 0종 · 새 레이아웃 0종.
    ///
    /// 정본이 코드로 검증했다고 적은 규칙 넷:
    ///   1. 모든 전투방에 빼앗을 몸이 최소 하나 있다
    ///   2. 모든 전투방에 원거리와 근접이 둘 다 있다
    ///   3. 같은 방에 같은 몸이 둘 있지 않다
    ///   4. 데뷔 챕터보다 먼저 나오는 몸이 없다
    /// </summary>
    public static class RoomDef60
    {
        /// <summary>spawn 한 줄. 좌표는 발자국 중심(미터).</summary>
        public sealed class Spawn
        {
            public float X, Y;
            public string Role;      // FRONT · RANGED · FLANK · BACK
            public string Unit;      // 우리 액터 키
            public bool IsHost;      // H = 빼앗을 수 있다 · T = 잡몹
        }

        public sealed class Room
        {
            public int Ch;
            public string No;        // "001" ~ "010"
            public string Kind;      // 전투 · 이벤트 · 중간보스 · 엘리트 · 보스
            public string Floor;
            public string Layout;    // 레이아웃 글자. 보스방은 빈 값
            public string LayoutKo;
            public string Comp;
            public string Pool;      // 이벤트 방일 때만 — BODY · STAKE
            public string Boss;      // 보스 방일 때만 — crusher …
            public string Captain;   // 중간보스 방일 때만 — 대장 호스트 키
            public int Minions;      // 중간보스 방 부하 수
            public string[] MinionFrom = System.Array.Empty<string>();
            public bool Elite;
            public Spawn[] Spawns = System.Array.Empty<Spawn>();
        }

        /// <summary>엘리트 규칙. 중간 보스(1.8배·빙의 불가)와 **다르다** — 엘리트는 빼앗을 수 있다.</summary>
        public const float EliteScale = 1.3f;
        public const int EliteHpMul = 2;
        public const float EliteAtkMul = 1.2f;
        public const bool ElitePossessable = true;
        // 출처 — 중간 보스(1.8배·빙의 불가)와 다르다. 엘리트는 빼앗을 수 있다 — 크고 아픈 몸이 곧 보상이다. 009 는 보스 직전이라 여기서 얻은 몸으로 010 에 들어간다

        public static readonly Room[] All =
        {
            new() { Ch = 1, No = "001", Kind = "전투",
                    Floor = "roomfloor_env_junkyard", Layout = "A", LayoutKo = "지그재그 관문",
                    Comp = "FLANK_REINFORCEMENT_PLUS_ZONER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 7.5f, Y = 3.5f, Role = "FLANK", Unit = "gangster", IsHost = true },
                        new() { X = 7f, Y = 7.5f, Role = "RANGED", Unit = "scrapgunner", IsHost = false },
                        new() { X = 1.5f, Y = 6.5f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                        new() { X = 8.5f, Y = 6f, Role = "RANGED", Unit = "scrapgunner", IsHost = false },
                    },
                    },
            new() { Ch = 1, No = "002", Kind = "전투",
                    Floor = "roomfloor_env_junkyard", Layout = "B", LayoutKo = "쌍기둥 통로",
                    Comp = "BACKLINE_PRESSURE_PLUS_HUNTER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "amazon", IsHost = true },
                        new() { X = 2f, Y = 7.5f, Role = "FLANK", Unit = "bat", IsHost = false },
                        new() { X = 8f, Y = 7.5f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                        new() { X = 8f, Y = 10f, Role = "FLANK", Unit = "bat", IsHost = false },
                        new() { X = 5f, Y = 6f, Role = "RANGED", Unit = "scrapgunner", IsHost = false },
                    },
                    },
            new() { Ch = 1, No = "003", Kind = "전투",
                    Floor = "roomfloor_env_junkyard", Layout = "E", LayoutKo = "계단",
                    Comp = "FRONTLINE_PLUS_RANGED", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 3f, Y = 6.5f, Role = "FRONT", Unit = "commando_grenade", IsHost = true },
                        new() { X = 3.5f, Y = 8.5f, Role = "RANGED", Unit = "scrapgunner", IsHost = false },
                        new() { X = 2f, Y = 4.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                        new() { X = 8.5f, Y = 8f, Role = "RANGED", Unit = "scrapgunner", IsHost = false },
                        new() { X = 5f, Y = 3.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                    },
                    },
            new() { Ch = 1, No = "004", Kind = "이벤트",
                    Floor = "roomfloor_env_junkyard", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 1, No = "005", Kind = "중간보스",
                    Floor = "roomfloor_env_holding", Layout = "F", LayoutKo = "모서리 요새",
                    Comp = "", Pool = "", Boss = "", Captain = "salamander",
                    Minions = 3, Elite = false,
                    MinionFrom = new[] { "gangster", "amazon", "commando_grenade" },
                    },
            new() { Ch = 1, No = "006", Kind = "전투",
                    Floor = "roomfloor_env_junkyard", Layout = "J", LayoutKo = "엇갈린 문",
                    Comp = "FLANK_REINFORCEMENT_PLUS_ZONER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 8f, Y = 5.5f, Role = "FLANK", Unit = "salamander", IsHost = true },
                        new() { X = 3f, Y = 7.5f, Role = "RANGED", Unit = "scrapgunner", IsHost = false },
                        new() { X = 6.5f, Y = 10f, Role = "FLANK", Unit = "bat", IsHost = false },
                        new() { X = 7f, Y = 7.5f, Role = "RANGED", Unit = "scrapgunner", IsHost = false },
                        new() { X = 8f, Y = 9.5f, Role = "BACK", Unit = "scrapgunner", IsHost = false },
                        new() { X = 5f, Y = 9.5f, Role = "BACK", Unit = "scrapgunner", IsHost = false },
                    },
                    },
            new() { Ch = 1, No = "007", Kind = "상점",
                    Floor = "roomfloor_env_junkyard", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 1, No = "008", Kind = "전투",
                    Floor = "roomfloor_env_junkyard", Layout = "D", LayoutKo = "십자 분단",
                    Comp = "BACKLINE_PRESSURE_PLUS_HUNTER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 2.5f, Y = 9.5f, Role = "BACK", Unit = "gangster", IsHost = true },
                        new() { X = 2.5f, Y = 5.5f, Role = "FLANK", Unit = "scrapgunner", IsHost = false },
                        new() { X = 7.5f, Y = 9.5f, Role = "BACK", Unit = "scrapgunner", IsHost = false },
                        new() { X = 7.5f, Y = 5.5f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "scrapgunner", IsHost = false },
                        new() { X = 5f, Y = 2.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                    },
                    },
            new() { Ch = 1, No = "009", Kind = "엘리트",
                    Floor = "roomfloor_env_junkyard", Layout = "C", LayoutKo = "중앙 요새",
                    Comp = "FRONTLINE_PLUS_RANGED", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = true,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 3f, Role = "FRONT", Unit = "hopper", IsHost = true },
                        new() { X = 2.5f, Y = 7.5f, Role = "RANGED", Unit = "scrapgunner", IsHost = false },
                        new() { X = 2.5f, Y = 2.5f, Role = "FRONT", Unit = "bat", IsHost = false },
                        new() { X = 7.5f, Y = 7.5f, Role = "RANGED", Unit = "scrapgunner", IsHost = false },
                        new() { X = 5f, Y = 1.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                        new() { X = 7.5f, Y = 10f, Role = "RANGED", Unit = "scrapgunner", IsHost = false },
                    },
                    },
            new() { Ch = 1, No = "010", Kind = "보스",
                    Floor = "roomfloor_robot_snakes", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "robot_snakes", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 2, No = "001", Kind = "전투",
                    Floor = "roomfloor_env_missile", Layout = "B", LayoutKo = "쌍기둥 통로",
                    Comp = "BACKLINE_PRESSURE_PLUS_HUNTER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "amazon", IsHost = true },
                        new() { X = 2f, Y = 7.5f, Role = "FLANK", Unit = "hopper_smg", IsHost = true },
                        new() { X = 8f, Y = 7.5f, Role = "FLANK", Unit = "bat", IsHost = false },
                        new() { X = 8f, Y = 10f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 5f, Y = 6f, Role = "RANGED", Unit = "roadwarden", IsHost = false },
                    },
                    },
            new() { Ch = 2, No = "002", Kind = "전투",
                    Floor = "roomfloor_env_missile", Layout = "E", LayoutKo = "계단",
                    Comp = "FRONTLINE_PLUS_RANGED", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 3f, Y = 6.5f, Role = "FRONT", Unit = "thug", IsHost = true },
                        new() { X = 3.5f, Y = 8.5f, Role = "RANGED", Unit = "roadwarden", IsHost = false },
                        new() { X = 2f, Y = 4.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 8.5f, Y = 8f, Role = "RANGED", Unit = "roadwarden", IsHost = false },
                        new() { X = 5f, Y = 3.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 2, No = "003", Kind = "전투",
                    Floor = "roomfloor_env_missile", Layout = "J", LayoutKo = "엇갈린 문",
                    Comp = "FLANK_REINFORCEMENT_PLUS_ZONER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 8f, Y = 5.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 3f, Y = 7.5f, Role = "RANGED", Unit = "commando_mg", IsHost = true },
                        new() { X = 6.5f, Y = 10f, Role = "FLANK", Unit = "bat", IsHost = false },
                        new() { X = 7f, Y = 7.5f, Role = "RANGED", Unit = "roadwarden", IsHost = false },
                        new() { X = 8f, Y = 9.5f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                        new() { X = 5f, Y = 9.5f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                    },
                    },
            new() { Ch = 2, No = "004", Kind = "이벤트",
                    Floor = "roomfloor_env_missile", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 2, No = "005", Kind = "중간보스",
                    Floor = "roomfloor_env_holding", Layout = "F", LayoutKo = "모서리 요새",
                    Comp = "", Pool = "", Boss = "", Captain = "robot",
                    Minions = 3, Elite = false,
                    MinionFrom = new[] { "amazon", "thug", "commando_mg" },
                    },
            new() { Ch = 2, No = "006", Kind = "전투",
                    Floor = "roomfloor_env_missile", Layout = "D", LayoutKo = "십자 분단",
                    Comp = "BACKLINE_PRESSURE_PLUS_HUNTER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 2.5f, Y = 9.5f, Role = "BACK", Unit = "robot", IsHost = true },
                        new() { X = 2.5f, Y = 5.5f, Role = "FLANK", Unit = "roadwarden", IsHost = false },
                        new() { X = 7.5f, Y = 9.5f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                        new() { X = 7.5f, Y = 5.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                        new() { X = 5f, Y = 2.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 2, No = "007", Kind = "상점",
                    Floor = "roomfloor_env_missile", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 2, No = "008", Kind = "전투",
                    Floor = "roomfloor_env_missile", Layout = "C", LayoutKo = "중앙 요새",
                    Comp = "FRONTLINE_PLUS_RANGED", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 3f, Role = "FRONT", Unit = "guru", IsHost = true },
                        new() { X = 2.5f, Y = 7.5f, Role = "RANGED", Unit = "roadwarden", IsHost = false },
                        new() { X = 2.5f, Y = 2.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 7.5f, Y = 7.5f, Role = "RANGED", Unit = "roadwarden", IsHost = false },
                        new() { X = 5f, Y = 1.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 7.5f, Y = 10f, Role = "RANGED", Unit = "roadwarden", IsHost = false },
                        new() { X = 5f, Y = 9.5f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                    },
                    },
            new() { Ch = 2, No = "009", Kind = "엘리트",
                    Floor = "roomfloor_env_missile", Layout = "K", LayoutKo = "사선 분단",
                    Comp = "FLANK_REINFORCEMENT_PLUS_ZONER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = true,
                    Spawns = new Spawn[]
                    {
                        new() { X = 2f, Y = 7f, Role = "FLANK", Unit = "white_wizard", IsHost = true },
                        new() { X = 8f, Y = 7f, Role = "RANGED", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 6.5f, Y = 2f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 5f, Y = 3f, Role = "RANGED", Unit = "roadwarden", IsHost = false },
                        new() { X = 8.5f, Y = 1.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 6.5f, Y = 10f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                        new() { X = 5.5f, Y = 7.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 2, No = "010", Kind = "보스",
                    Floor = "roomfloor_crusher", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "crusher", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 3, No = "001", Kind = "전투",
                    Floor = "roomfloor_env_street", Layout = "E", LayoutKo = "계단",
                    Comp = "FRONTLINE_PLUS_RANGED", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 3f, Y = 6.5f, Role = "FRONT", Unit = "snowwoman", IsHost = true },
                        new() { X = 3.5f, Y = 8.5f, Role = "RANGED", Unit = "turret_cross", IsHost = false },
                        new() { X = 2f, Y = 4.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 8.5f, Y = 8f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 5f, Y = 3.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 3, No = "002", Kind = "전투",
                    Floor = "roomfloor_env_street", Layout = "J", LayoutKo = "엇갈린 문",
                    Comp = "FLANK_REINFORCEMENT_PLUS_ZONER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 8f, Y = 5.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 3f, Y = 7.5f, Role = "RANGED", Unit = "amazon", IsHost = true },
                        new() { X = 6.5f, Y = 10f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                        new() { X = 7f, Y = 7.5f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 8f, Y = 9.5f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                        new() { X = 5f, Y = 9.5f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                    },
                    },
            new() { Ch = 3, No = "003", Kind = "전투",
                    Floor = "roomfloor_env_street", Layout = "D", LayoutKo = "십자 분단",
                    Comp = "BACKLINE_PRESSURE_PLUS_HUNTER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 2.5f, Y = 9.5f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                        new() { X = 2.5f, Y = 5.5f, Role = "FLANK", Unit = "snowwoman", IsHost = true },
                        new() { X = 7.5f, Y = 9.5f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 7.5f, Y = 5.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 5f, Y = 2.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 3, No = "004", Kind = "이벤트",
                    Floor = "roomfloor_env_street", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 3, No = "005", Kind = "중간보스",
                    Floor = "roomfloor_env_holding", Layout = "F", LayoutKo = "모서리 요새",
                    Comp = "", Pool = "", Boss = "", Captain = "vampire",
                    Minions = 3, Elite = false,
                    MinionFrom = new[] { "snowwoman", "ninja", "amazon" },
                    },
            new() { Ch = 3, No = "006", Kind = "전투",
                    Floor = "roomfloor_env_street", Layout = "C", LayoutKo = "중앙 요새",
                    Comp = "FRONTLINE_PLUS_RANGED", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 3f, Role = "FRONT", Unit = "vampire", IsHost = true },
                        new() { X = 2.5f, Y = 7.5f, Role = "RANGED", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 2.5f, Y = 2.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 7.5f, Y = 7.5f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 5f, Y = 1.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 7.5f, Y = 10f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 5f, Y = 9.5f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                    },
                    },
            new() { Ch = 3, No = "007", Kind = "상점",
                    Floor = "roomfloor_env_street", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 3, No = "008", Kind = "전투",
                    Floor = "roomfloor_env_street", Layout = "K", LayoutKo = "사선 분단",
                    Comp = "FLANK_REINFORCEMENT_PLUS_ZONER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 2f, Y = 7f, Role = "FLANK", Unit = "ninja", IsHost = true },
                        new() { X = 8f, Y = 7f, Role = "RANGED", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 6.5f, Y = 2f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 5f, Y = 3f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 8.5f, Y = 1.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 6.5f, Y = 10f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 5.5f, Y = 7.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 3, No = "009", Kind = "엘리트",
                    Floor = "roomfloor_env_street", Layout = "G", LayoutKo = "좁은 문",
                    Comp = "BACKLINE_PRESSURE_PLUS_HUNTER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = true,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 8.5f, Role = "BACK", Unit = "ninja", IsHost = true },
                        new() { X = 1.5f, Y = 5.5f, Role = "FLANK", Unit = "turret_cross", IsHost = false },
                        new() { X = 2.5f, Y = 9.5f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                        new() { X = 7.5f, Y = 9.5f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                        new() { X = 3f, Y = 5.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 7f, Y = 5.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                    },
                    },
            new() { Ch = 3, No = "010", Kind = "보스",
                    Floor = "roomfloor_python", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "python", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 4, No = "001", Kind = "전투",
                    Floor = "roomfloor_env_rooftop", Layout = "J", LayoutKo = "엇갈린 문",
                    Comp = "FLANK_REINFORCEMENT_PLUS_ZONER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 8f, Y = 5.5f, Role = "FLANK", Unit = "baseball", IsHost = true },
                        new() { X = 3f, Y = 7.5f, Role = "RANGED", Unit = "hopper_smg", IsHost = true },
                        new() { X = 6.5f, Y = 10f, Role = "FLANK", Unit = "bat", IsHost = false },
                        new() { X = 7f, Y = 7.5f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 8f, Y = 9.5f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                        new() { X = 5f, Y = 9.5f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                    },
                    },
            new() { Ch = 4, No = "002", Kind = "전투",
                    Floor = "roomfloor_env_rooftop", Layout = "D", LayoutKo = "십자 분단",
                    Comp = "BACKLINE_PRESSURE_PLUS_HUNTER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 2.5f, Y = 9.5f, Role = "BACK", Unit = "snowwoman", IsHost = true },
                        new() { X = 2.5f, Y = 5.5f, Role = "FLANK", Unit = "medium", IsHost = true },
                        new() { X = 7.5f, Y = 9.5f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 7.5f, Y = 5.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                        new() { X = 5f, Y = 2.5f, Role = "FRONT", Unit = "bat", IsHost = false },
                    },
                    },
            new() { Ch = 4, No = "003", Kind = "전투",
                    Floor = "roomfloor_env_rooftop", Layout = "C", LayoutKo = "중앙 요새",
                    Comp = "FRONTLINE_PLUS_RANGED", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 3f, Role = "FRONT", Unit = "white_wizard", IsHost = true },
                        new() { X = 2.5f, Y = 7.5f, Role = "RANGED", Unit = "amazon", IsHost = true },
                        new() { X = 2.5f, Y = 2.5f, Role = "FRONT", Unit = "bat", IsHost = false },
                        new() { X = 7.5f, Y = 7.5f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 5f, Y = 1.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 7.5f, Y = 10f, Role = "RANGED", Unit = "roadwarden", IsHost = false },
                        new() { X = 5f, Y = 9.5f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                    },
                    },
            new() { Ch = 4, No = "004", Kind = "이벤트",
                    Floor = "roomfloor_env_rooftop", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 4, No = "005", Kind = "중간보스",
                    Floor = "roomfloor_env_holding", Layout = "F", LayoutKo = "모서리 요새",
                    Comp = "", Pool = "", Boss = "", Captain = "dragon_blue",
                    Minions = 3, Elite = false,
                    MinionFrom = new[] { "baseball", "snowwoman", "white_wizard" },
                    },
            new() { Ch = 4, No = "006", Kind = "전투",
                    Floor = "roomfloor_env_rooftop", Layout = "K", LayoutKo = "사선 분단",
                    Comp = "FLANK_REINFORCEMENT_PLUS_ZONER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 2f, Y = 7f, Role = "FLANK", Unit = "dragon_blue", IsHost = true },
                        new() { X = 8f, Y = 7f, Role = "RANGED", Unit = "thug", IsHost = true },
                        new() { X = 6.5f, Y = 2f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 5f, Y = 3f, Role = "RANGED", Unit = "roadwarden", IsHost = false },
                        new() { X = 8.5f, Y = 1.5f, Role = "FLANK", Unit = "bat", IsHost = false },
                        new() { X = 6.5f, Y = 10f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 5.5f, Y = 7.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 4, No = "007", Kind = "상점",
                    Floor = "roomfloor_env_rooftop", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 4, No = "008", Kind = "전투",
                    Floor = "roomfloor_env_rooftop", Layout = "G", LayoutKo = "좁은 문",
                    Comp = "BACKLINE_PRESSURE_PLUS_HUNTER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 8.5f, Role = "BACK", Unit = "ninja", IsHost = true },
                        new() { X = 1.5f, Y = 5.5f, Role = "FLANK", Unit = "commando_laser", IsHost = true },
                        new() { X = 2.5f, Y = 9.5f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                        new() { X = 7.5f, Y = 9.5f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                        new() { X = 3f, Y = 5.5f, Role = "FRONT", Unit = "bat", IsHost = false },
                        new() { X = 7f, Y = 5.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 4, No = "009", Kind = "엘리트",
                    Floor = "roomfloor_env_rooftop", Layout = "H", LayoutKo = "가시밭",
                    Comp = "FRONTLINE_PLUS_RANGED", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = true,
                    Spawns = new Spawn[]
                    {
                        new() { X = 3f, Y = 3f, Role = "FRONT", Unit = "ninja_chain", IsHost = true },
                        new() { X = 5f, Y = 6f, Role = "RANGED", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 7f, Y = 3f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 5f, Y = 8.5f, Role = "RANGED", Unit = "roadwarden", IsHost = false },
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 1.5f, Y = 8.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 8.5f, Y = 8.5f, Role = "FLANK", Unit = "bat", IsHost = false },
                        new() { X = 2.5f, Y = 6.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 4, No = "010", Kind = "보스",
                    Floor = "roomfloor_sludge", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "sludge", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 5, No = "001", Kind = "전투",
                    Floor = "roomfloor_env_lab", Layout = "D", LayoutKo = "십자 분단",
                    Comp = "BACKLINE_PRESSURE_PLUS_HUNTER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 2.5f, Y = 9.5f, Role = "BACK", Unit = "hopper_smg", IsHost = true },
                        new() { X = 2.5f, Y = 5.5f, Role = "FLANK", Unit = "snowwoman", IsHost = true },
                        new() { X = 7.5f, Y = 9.5f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 7.5f, Y = 5.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                        new() { X = 5f, Y = 2.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                    },
                    },
            new() { Ch = 5, No = "002", Kind = "전투",
                    Floor = "roomfloor_env_lab", Layout = "C", LayoutKo = "중앙 요새",
                    Comp = "FRONTLINE_PLUS_RANGED", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 3f, Role = "FRONT", Unit = "commando_laser", IsHost = true },
                        new() { X = 2.5f, Y = 7.5f, Role = "RANGED", Unit = "guru", IsHost = true },
                        new() { X = 2.5f, Y = 2.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 7.5f, Y = 7.5f, Role = "RANGED", Unit = "turret_cross", IsHost = false },
                        new() { X = 5f, Y = 1.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                        new() { X = 7.5f, Y = 10f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 5f, Y = 9.5f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                    },
                    },
            new() { Ch = 5, No = "003", Kind = "전투",
                    Floor = "roomfloor_env_lab", Layout = "K", LayoutKo = "사선 분단",
                    Comp = "FLANK_REINFORCEMENT_PLUS_ZONER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 2f, Y = 7f, Role = "FLANK", Unit = "white_wizard", IsHost = true },
                        new() { X = 8f, Y = 7f, Role = "RANGED", Unit = "commando_grenade", IsHost = true },
                        new() { X = 6.5f, Y = 2f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                        new() { X = 5f, Y = 3f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 8.5f, Y = 1.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 6.5f, Y = 10f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                        new() { X = 5.5f, Y = 7.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                    },
                    },
            new() { Ch = 5, No = "004", Kind = "이벤트",
                    Floor = "roomfloor_env_lab", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 5, No = "005", Kind = "중간보스",
                    Floor = "roomfloor_env_holding", Layout = "F", LayoutKo = "모서리 요새",
                    Comp = "", Pool = "", Boss = "", Captain = "amazon_elite",
                    Minions = 3, Elite = false,
                    MinionFrom = new[] { "hopper_smg", "commando_laser", "white_wizard" },
                    },
            new() { Ch = 5, No = "006", Kind = "전투",
                    Floor = "roomfloor_env_lab", Layout = "G", LayoutKo = "좁은 문",
                    Comp = "BACKLINE_PRESSURE_PLUS_HUNTER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 8.5f, Role = "BACK", Unit = "amazon_elite", IsHost = true },
                        new() { X = 1.5f, Y = 5.5f, Role = "FLANK", Unit = "ninja_chain", IsHost = true },
                        new() { X = 2.5f, Y = 9.5f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                        new() { X = 7.5f, Y = 9.5f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                        new() { X = 3f, Y = 5.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                        new() { X = 7f, Y = 5.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 5, No = "007", Kind = "상점",
                    Floor = "roomfloor_env_lab", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 5, No = "008", Kind = "전투",
                    Floor = "roomfloor_env_lab", Layout = "H", LayoutKo = "가시밭",
                    Comp = "FRONTLINE_PLUS_RANGED", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 3f, Y = 3f, Role = "FRONT", Unit = "ninja", IsHost = true },
                        new() { X = 5f, Y = 6f, Role = "RANGED", Unit = "dragon_blue", IsHost = true },
                        new() { X = 7f, Y = 3f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                        new() { X = 5f, Y = 8.5f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                        new() { X = 1.5f, Y = 8.5f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                        new() { X = 8.5f, Y = 8.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 2.5f, Y = 6.5f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                    },
                    },
            new() { Ch = 5, No = "009", Kind = "엘리트",
                    Floor = "roomfloor_env_lab", Layout = "L", LayoutKo = "네 귀퉁이 가시",
                    Comp = "FLANK_REINFORCEMENT_PLUS_ZONER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = true,
                    Spawns = new Spawn[]
                    {
                        new() { X = 2f, Y = 6.5f, Role = "FLANK", Unit = "commando_missile", IsHost = true },
                        new() { X = 5f, Y = 8f, Role = "RANGED", Unit = "robot", IsHost = true },
                        new() { X = 8f, Y = 6.5f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                        new() { X = 6.5f, Y = 7.5f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 3.5f, Y = 10f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                        new() { X = 6.5f, Y = 10f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 3.5f, Y = 2.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 6.5f, Y = 2.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                    },
                    },
            new() { Ch = 5, No = "010", Kind = "보스",
                    Floor = "roomfloor_guardian", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "guardian", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 6, No = "001", Kind = "전투",
                    Floor = "roomfloor_env_refinery", Layout = "C", LayoutKo = "중앙 요새",
                    Comp = "FRONTLINE_PLUS_RANGED", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 3f, Role = "FRONT", Unit = "commando_grenade", IsHost = true },
                        new() { X = 2.5f, Y = 7.5f, Role = "RANGED", Unit = "commando_mg", IsHost = true },
                        new() { X = 2.5f, Y = 2.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 7.5f, Y = 7.5f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 5f, Y = 1.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                        new() { X = 7.5f, Y = 10f, Role = "RANGED", Unit = "turret_cross", IsHost = false },
                        new() { X = 5f, Y = 9.5f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                    },
                    },
            new() { Ch = 6, No = "002", Kind = "전투",
                    Floor = "roomfloor_env_refinery", Layout = "K", LayoutKo = "사선 분단",
                    Comp = "FLANK_REINFORCEMENT_PLUS_ZONER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 2f, Y = 7f, Role = "FLANK", Unit = "snowwoman", IsHost = true },
                        new() { X = 8f, Y = 7f, Role = "RANGED", Unit = "guru", IsHost = true },
                        new() { X = 6.5f, Y = 2f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                        new() { X = 5f, Y = 3f, Role = "RANGED", Unit = "turret_cross", IsHost = false },
                        new() { X = 8.5f, Y = 1.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 6.5f, Y = 10f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 5.5f, Y = 7.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                    },
                    },
            new() { Ch = 6, No = "003", Kind = "전투",
                    Floor = "roomfloor_env_refinery", Layout = "G", LayoutKo = "좁은 문",
                    Comp = "BACKLINE_PRESSURE_PLUS_HUNTER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 8.5f, Role = "BACK", Unit = "thug", IsHost = true },
                        new() { X = 1.5f, Y = 5.5f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                        new() { X = 2.5f, Y = 9.5f, Role = "BACK", Unit = "turret_cross", IsHost = false },
                        new() { X = 7.5f, Y = 9.5f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 3f, Y = 5.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                        new() { X = 7f, Y = 5.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 5f, Y = 6.5f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                    },
                    },
            new() { Ch = 6, No = "004", Kind = "이벤트",
                    Floor = "roomfloor_env_refinery", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 6, No = "005", Kind = "중간보스",
                    Floor = "roomfloor_env_holding", Layout = "F", LayoutKo = "모서리 요새",
                    Comp = "", Pool = "", Boss = "", Captain = "medium",
                    Minions = 3, Elite = false,
                    MinionFrom = new[] { "commando_grenade", "snowwoman", "thug" },
                    },
            new() { Ch = 6, No = "006", Kind = "전투",
                    Floor = "roomfloor_env_refinery", Layout = "H", LayoutKo = "가시밭",
                    Comp = "FRONTLINE_PLUS_RANGED", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 3f, Y = 3f, Role = "FRONT", Unit = "medium", IsHost = true },
                        new() { X = 5f, Y = 6f, Role = "RANGED", Unit = "skeleton", IsHost = false },
                        new() { X = 7f, Y = 3f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                        new() { X = 5f, Y = 8.5f, Role = "RANGED", Unit = "turret_cross", IsHost = false },
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                        new() { X = 1.5f, Y = 8.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 8.5f, Y = 8.5f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                        new() { X = 2.5f, Y = 6.5f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 6, No = "007", Kind = "상점",
                    Floor = "roomfloor_env_refinery", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    },
            new() { Ch = 6, No = "008", Kind = "전투",
                    Floor = "roomfloor_env_refinery", Layout = "L", LayoutKo = "네 귀퉁이 가시",
                    Comp = "FLANK_REINFORCEMENT_PLUS_ZONER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = false,
                    Spawns = new Spawn[]
                    {
                        new() { X = 2f, Y = 6.5f, Role = "FLANK", Unit = "amazon_elite", IsHost = true },
                        new() { X = 5f, Y = 8f, Role = "RANGED", Unit = "skeleton", IsHost = false },
                        new() { X = 8f, Y = 6.5f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                        new() { X = 6.5f, Y = 7.5f, Role = "RANGED", Unit = "turret_cross", IsHost = false },
                        new() { X = 3.5f, Y = 10f, Role = "BACK", Unit = "roadwarden", IsHost = false },
                        new() { X = 6.5f, Y = 10f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 3.5f, Y = 2.5f, Role = "FRONT", Unit = "skeleton", IsHost = false },
                        new() { X = 6.5f, Y = 2.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 6, No = "009", Kind = "엘리트",
                    Floor = "roomfloor_env_refinery", Layout = "I", LayoutKo = "회전 관문",
                    Comp = "BACKLINE_PRESSURE_PLUS_HUNTER", Pool = "", Boss = "", Captain = "",
                    Minions = 0, Elite = true,
                    Spawns = new Spawn[]
                    {
                        new() { X = 5f, Y = 10f, Role = "BACK", Unit = "robot", IsHost = true },
                        new() { X = 2f, Y = 7.5f, Role = "FLANK", Unit = "coilwalker", IsHost = false },
                        new() { X = 3.5f, Y = 8f, Role = "BACK", Unit = "coilwalker", IsHost = false },
                        new() { X = 8f, Y = 7.5f, Role = "FLANK", Unit = "skeleton", IsHost = false },
                        new() { X = 6.5f, Y = 8f, Role = "FLANK", Unit = "actor_enforcer", IsHost = false },
                        new() { X = 2f, Y = 5f, Role = "RANGED", Unit = "coilwalker", IsHost = false },
                        new() { X = 8f, Y = 5f, Role = "RANGED", Unit = "turret_cross", IsHost = false },
                        new() { X = 3.5f, Y = 3.5f, Role = "FRONT", Unit = "actor_enforcer", IsHost = false },
                    },
                    },
            new() { Ch = 6, No = "010", Kind = "보스",
                    Floor = "roomfloor_kingpin", Layout = "", LayoutKo = "",
                    Comp = "", Pool = "", Boss = "kingpin", Captain = "",
                    Minions = 0, Elite = false,
                    },
        };

        /// <summary>챕터별 잡몹 회전. CH4~6 은 기존 7종 재조합이다 — 새 잡몹 0종.</summary>
        public static readonly string[][] TrashPool =
        {
            System.Array.Empty<string>(),   // 0 번은 안 쓴다 — 챕터는 1부터다
            new[] { "skeleton", "bat", "scrapgunner" },
            new[] { "actor_enforcer", "bat", "roadwarden" },
            new[] { "actor_enforcer", "skeleton", "coilwalker", "turret_cross" },
            new[] { "actor_enforcer", "roadwarden", "bat", "coilwalker" },
            new[] { "actor_enforcer", "turret_cross", "skeleton", "coilwalker" },
            new[] { "actor_enforcer", "coilwalker", "skeleton", "turret_cross", "roadwarden" },
        };

        /// <summary>호스트 데뷔 챕터. 각 챕터 중간 보스 대장이 그 챕터에 처음 나오는 몸이다.</summary>
        public static readonly string[][] Debut =
        {
            System.Array.Empty<string>(),
            new[] { "amazon", "baseball", "commando_mg", "commando_grenade" },
            new[] { "hopper", "commando_missile", "medium", "dragon_blue" },
            new[] { "ninja_chain", "ninja", "snowwoman", "white_wizard" },
            new[] { "gangster", "thug", "guru", "dragoon" },
            new[] { "robot", "commando_laser", "amazon_elite", "hopper_smg" },
            new[] { "salamander", "vampire", "death" },
        };

        /// <summary>방 번호별 빼앗을 몸 개수. 적혀 있지 않은 방은 0 이다.</summary>
        public static int HostsInRoom(string no) => no switch
        {
            "001" => 1,
            "002" => 1,
            "003" => 1,
            "006" => 2,
            "008" => 2,
            "009" => 2,
            _ => 0,
        };
    }
}
