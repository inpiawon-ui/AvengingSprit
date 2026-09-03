using System.Collections.Generic;

namespace Game.EditorTools
{
    /// <summary>
    /// 최종 보스 6종과 패턴 24개.
    ///
    /// 단일 출처는 `_exchange/out/42_jobs/AVSR_Bosses.js` (v2.0) 이고, 이 파일은 그것을
    /// 옮겨 적은 것이다 — ⚠ **손으로 고치지 않는다.** 기획이 바뀌면 .js 를 받아 다시 뽑는다.
    ///
    /// ── v2.0 에서 무엇이 달라졌나 ───────────────────────────────
    /// v1.0 은 정본 `BOSS_ATTACK_RUNTIME` **텍스트만** 보고 짰다가 원작 그림과 어긋났다 —
    /// 크러셔에 없는 주먹을, 가디언에 없는 방패를 시켰다(가디언은 지네다).
    /// v2.0 은 `Reference/Original/Bosses - {이름}.png` 를 앵커로 24개를 전부 다시 짰다.
    ///   정본에서 유지: 쿨 · 예고 시간 · 피해 배율 · 페이즈 (수치는 그림과 무관하다)
    ///   원작에서 가져옴: 무엇을 하는가 · 어떤 모양인가
    ///
    /// ── HP·공격력은 무대 순서를 따른다 ─────────────────────────
    /// 보스 고유값이 아니다. 1650/2400/3150/4300/5200/6900 · 18/22/25/29/33/37 을
    /// 스테이지 1~6 에 그대로 붙인다. 보스는 주제로 배정하고 **세기는 자리가 정한다** —
    /// 안 그러면 3스테이지가 4스테이지보다 세진다.
    /// (정본은 킹핀 3150 · 파이썬 4300 이었는데 둘의 무대를 맞바꿨다)
    ///
    /// ── 중간 보스는 여기 없다 ───────────────────────────────────
    /// 방 005 의 중간 보스는 **호스트 대장**이라 제 패턴이 없다 — 자기 액티브 스킬을 쓴다.
    /// 그래서 이 표에 넣지 않는다. 대장이 누구인지는 방(`RoomEntry.MidBossKey`)이 갖는다.
    ///
    /// ── 수치의 출처 ─────────────────────────────────────────────
    /// .js 는 반경·각도를 `spec` 한국어 문장 안에 둔다. 아래 각 패턴의 `// 출처` 주석이
    /// 그 문장이다 — 값이 맞는지 따질 때 여기를 본다.
    /// </summary>
    public static class BossDefTable
    {
        public sealed class Move
        {
            public int Phase;              // 이 페이즈부터 쓴다 (1·2·3)
            public string NameKr, NameEn;
            public float Cooldown, Telegraph, DamageMul;
            public string Shape;           // Arc · Line · Lane · Zone · Dash · Mark
            public string Draw;            // 패턴 하나에 값 하나. `BossDraw` 와 이름이 같다
            public string Dodge;           // BACK · SIDE · GAP · PERP · ZONE · CLOSE · SWAP · HOLD
            public float Degrees, Radius, Width, Length, InnerRadius, GapDegrees;
            public int Lanes;              // 줄 수 · 또는 **도형 개수**(미사일 5 · 그림자 3 …)
            public float SafeX, SafeY;     // 안전지대(m). 0 이면 없다
            public string Range;           // "" · NEAR · FAR — 거리 조건
            public float RangeMeters;      // 가깝다/멀다 문턱(m). 0 이면 기본 4 m
            public int Group;              // 같은 번호끼리 한 시계로 번갈아 나간다
        }

        public sealed class Boss
        {
            public string Key, NameKr, NameEn, Sprite;
            public int Chapter, RoomNo;    // 최종 보스는 챕터마다 방 010 하나뿐이다
            public string Gate;            // FINAL 뿐이다 (중간 보스는 이 표에 없다)
            public int Hp, Atk;
            public string State;           // Guard · Twin · Split · 빈 문자열
            public float BreakSeconds;     // 취약 창 길이. 0 이면 시간제가 아니다(가디언)
            public string BreakCause;
            public Move[] Moves;
        }

        public static readonly Boss[] All =
        {

            // ── 크러셔 · 쓰레기장 — 쓰레기를 씹는 기계 ─────────────────────────
            // ⚠ **크러셔만 정본 도면과 다르다.** 2026-09-02 에 사용자가 직접 기획했다.
            //   정본 4패턴(압착·쇠사슬 파괴구·컨베이어·방패 전개) 대신 셋을 쓴다 —
            //   미사일(5초) · 부채꼴(현행) · 당기기(20초). 세 개가 다 P1 이라
            //   첫 순간부터 셋이 같이 돈다("같이 써봐").
            //
            //   폐기한 둘은 코드가 남아 있다(쇠사슬 파괴구 궤도·방패판). 되돌리려면
            //   아래 Move 두 개를 다시 넣으면 그대로 산다 — 지우지 않았다.
            new() { Key = "crusher", NameKr = "크러셔", NameEn = "Crusher", Sprite = "unit_crusher",
                    Chapter = 1, RoomNo = 10, Gate = "FINAL", Hp = 1650, Atk = 18,
                    State = "", BreakSeconds = 2.5f, BreakCause = "파괴구가 헛돌아 벽을 때렸다",
                    Moves = new Move[]
                    {
                        // ⚠ 기획 2026-09-02(4차) — 거리로 갈린다.
                        //     붙어 있으면(4 m 안)  파괴구만 돈다              쿨 3s
                        //     떨어져 있으면(4 m 밖) 미사일 ↔ 압착 번갈아       쿨 3s (묶음 1)
                        //                          돌진                     쿨 5s
                        //   예고는 넷 다 1초.

                        // 미사일 한 발이 내 자리에 떨어진다. 터지는 자리는 하나다.
                        new() { Phase = 1, NameKr = "미사일", NameEn = "Missile",
                                Cooldown = 3f, Telegraph = 1.0f, DamageMul = 1.0f,
                                Shape = "Zone", Draw = "MissileSalvo", Dodge = "SIDE",
                                Degrees = 0f, Radius = 1.67f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 1,
                                SafeX = 0f, SafeY = 0f,
                                Range = "FAR", RangeMeters = 4f, Group = 1 },

                        // 쇠사슬 파괴구가 제 둘레를 돈다. **안쪽이 안전하다** — 파고들어야 산다.
                        // 붙어 있을 때만 돈다. 8 m 밖에서 반경 3.5 m 를 돌려 봐야 아무 일도 안 난다.
                        new() { Phase = 1, NameKr = "쇠사슬 파괴구", NameEn = "WreckingBall",
                                Cooldown = 3f, Telegraph = 1.0f, DamageMul = 0.94f,
                                Shape = "Zone", Draw = "WreckingBall", Dodge = "CLOSE",
                                Degrees = 0f, Radius = 3.5f, Width = 0f, Length = 0f,
                                InnerRadius = 1.6f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "NEAR", RangeMeters = 4f, Group = 0 },

                        // 부채꼴로 탄을 쏜다. **부채꼴은 겨냥 표시일 뿐이고** 탄은
                        // 그 방향으로 방 끝까지 날아간다(기획 2026-09-02 5차).
                        //
                        // ⚠ 그래서 반경을 다시 2.5 m 로 줄였다. 이 도형은 피해 범위가
                        //   아니라 "저쪽으로 쏜다" 는 표시라 클 이유가 없다.
                        //   때리는 것은 탄뿐이다(`ShapeHurts` 참조).
                        new() { Phase = 1, NameKr = "부채꼴 사격", NameEn = "FanShot",
                                Cooldown = 3f, Telegraph = 1.0f, DamageMul = 0.94f,
                                Shape = "Arc", Draw = "Crush", Dodge = "SIDE",
                                Degrees = 120f, Radius = 2.5f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "FAR", RangeMeters = 4f, Group = 1 },

                        // 나에게 붉은 줄을 긋고 그 줄을 타고 밀고 들어온다. 그리고 한 발 더.
                        // 떨어져 있을 때만 — 붙어 있는데 돌진하면 지나쳐 버린다.
                        new() { Phase = 1, NameKr = "돌진", NameEn = "RamCharge",
                                Cooldown = 5f, Telegraph = 1.0f, DamageMul = 1.0f,
                                Shape = "Line", Draw = "RamCharge", Dodge = "PERP",
                                Degrees = 0f, Radius = 0f, Width = 2.0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "FAR", RangeMeters = 4f, Group = 0 },
                    } },

            // ── 가디언 · 미사일기지 — 마디를 하나씩 끊어라 ───────────────────────
            new() { Key = "guardian", NameKr = "가디언", NameEn = "Guardian", Sprite = "unit_guardian",
                    Chapter = 2, RoomNo = 10, Gate = "FINAL", Hp = 2400, Atk = 22,
                    State = "Segments", BreakSeconds = 0.0f, BreakCause = "마디를 3개 이하로 끊었다 — 머리 무적이 영구히 풀린다",
                    Moves = new Move[]
                    {
                        // 출처 — 몸을 늘려 직선으로 찌른다 · 폭 1.4 m · 길이 = 남은 마디 수 × 0.9 m (8마디 = 7.2 m)
                        new() { Phase = 1, NameKr = "마디 돌진", NameEn = "SegmentThrust",
                                Cooldown = 7f, Telegraph = 1f, DamageMul = 0.82f,
                                Shape = "Line", Draw = "SegmentThrust", Dodge = "PERP",
                                Degrees = 0f, Radius = 0f, Width = 1.4f, Length = 7.2f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 1 },
                        // 출처 — 마디 2개를 떼어 굴린다 · 각 1.4 m · 3초간 방 안을 튕겨 다닌다
                        new() { Phase = 1, NameKr = "마디 사출", NameEn = "SegmentLaunch",
                                Cooldown = 12f, Telegraph = 1f, DamageMul = 0.73f,
                                Shape = "Zone", Draw = "SegmentLaunch", Dodge = "SIDE",
                                Degrees = 0f, Radius = 1.4f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 2,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 0 },
                        // 출처 — 몸을 말아 반경 3.5 m 원형 벽을 만든다 · 3초 · 마디 사이 틈으로 들어가면 머리를 때린다
                        new() { Phase = 2, NameKr = "똬리", NameEn = "CoilWall",
                                Cooldown = 10f, Telegraph = 1f, DamageMul = 0.91f,
                                Shape = "Zone", Draw = "CoilWall", Dodge = "GAP",
                                Degrees = 0f, Radius = 3.5f, Width = 0f, Length = 0f,
                                InnerRadius = 2.6f, GapDegrees = 60f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "NEAR", RangeMeters = 4f, Group = 0 },
                        // 출처 — 머리만 몸에서 길게 뻗어 문다 · 최대 6 m · 마디가 적을수록 빠르다
                        new() { Phase = 3, NameKr = "머리 물기", NameEn = "HeadBite",
                                Cooldown = 9f, Telegraph = 1f, DamageMul = 1.14f,
                                Shape = "Dash", Draw = "HeadBite", Dodge = "SIDE",
                                Degrees = 0f, Radius = 0f, Width = 1.6f, Length = 6f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 1 },
                    } },

            // ── 파이썬 · 밤거리 — 벽에서 나온다 ─────────────────────────────
            new() { Key = "python", NameKr = "파이썬", NameEn = "Python", Sprite = "unit_python",
                    Chapter = 3, RoomNo = 10, Gate = "FINAL", Hp = 3150, Atk = 25,
                    State = "Walls", BreakSeconds = 3.0f, BreakCause = "머리가 나온 직후 1.2초 안에 때렸다",
                    Moves = new Move[]
                    {
                        // 출처 — 벽 한 곳에 금이 간 뒤 머리가 튀어나와 직선 6 m 를 훑는다 · 폭 1.8 m
                        new() { Phase = 1, NameKr = "벽 돌파", NameEn = "WallBurst",
                                Cooldown = 12f, Telegraph = 1f, DamageMul = 0.83f,
                                Shape = "Line", Draw = "WallBurst", Dodge = "PERP",
                                Degrees = 0f, Radius = 0f, Width = 1.8f, Length = 6f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 1 },
                        // 출처 — 머리가 독을 뿜는다 · 반경 2.2 m 로 시작해 6초 동안 3.2 m 까지 퍼진다
                        new() { Phase = 1, NameKr = "독구름", NameEn = "VenomCloud",
                                Cooldown = 9f, Telegraph = 1f, DamageMul = 0.59f,
                                Shape = "Zone", Draw = "VenomCloud", Dodge = "ZONE",
                                Degrees = 0f, Radius = 3.2f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 0 },
                        // 출처 — 벽에서 벽으로 몸통이 방을 가로지른다 · 폭 2.4 m · 가로 또는 세로 한 줄 · 2.5초
                        new() { Phase = 2, NameKr = "몸통 가로지르기", NameEn = "BodyCross",
                                Cooldown = 10f, Telegraph = 1f, DamageMul = 0.93f,
                                Shape = "Lane", Draw = "BodyCross", Dodge = "SIDE",
                                Degrees = 0f, Radius = 0f, Width = 2.4f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 4,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 0 },
                        // 출처 — 벽 세 곳에서 동시에 나온다 · 세 직선이 교차하고 안 겹치는 자리가 하나뿐
                        new() { Phase = 3, NameKr = "세 갈래 돌파", NameEn = "TripleBurst",
                                Cooldown = 16f, Telegraph = 1f, DamageMul = 1.17f,
                                Shape = "Line", Draw = "TripleBurst", Dodge = "GAP",
                                Degrees = 0f, Radius = 0f, Width = 1.8f, Length = 6f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 3,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 1 },
                    } },

            // ── 킹핀 · 옥상 — 하늘에 떠 있다 ──────────────────────────────
            new() { Key = "kingpin", NameKr = "킹핀", NameEn = "Kingpin", Sprite = "unit_kingpin",
                    Chapter = 4, RoomNo = 10, Gate = "FINAL", Hp = 4300, Atk = 29,
                    State = "", BreakSeconds = 2.0f, BreakCause = "저공 활강을 옥상 구조물 쪽으로 유인했다",
                    Moves = new Move[]
                    {
                        // 출처 — 미사일 5발을 부채꼴로 뿌린다 · 착탄 반경 1.2 m · 바닥에 착탄 원이 먼저 뜬다
                        new() { Phase = 1, NameKr = "미사일 일제", NameEn = "MissileSalvo",
                                Cooldown = 9f, Telegraph = 1f, DamageMul = 0.88f,
                                Shape = "Zone", Draw = "MissileSalvo", Dodge = "SIDE",
                                Degrees = 0f, Radius = 1.2f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 5,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 1 },
                        // 출처 — 지금 입고 있는 몸에 조준 표식 · 3초 뒤 그 자리 반경 2.0 m 에 집중 사격
                        new() { Phase = 1, NameKr = "처형 조준", NameEn = "ExecutionLock",
                                Cooldown = 13f, Telegraph = 1.8f, DamageMul = 1.24f,
                                Shape = "Mark", Draw = "ExecutionLock", Dodge = "SWAP",
                                Degrees = 0f, Radius = 2f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 0 },
                        // 출처 — 탈것으로 방을 가로질러 민다 · 폭 3.0 m · 이때만 근접이 닿는다
                        new() { Phase = 2, NameKr = "저공 활강", NameEn = "StrafingRun",
                                Cooldown = 8f, Telegraph = 1f, DamageMul = 0.72f,
                                Shape = "Dash", Draw = "StrafingRun", Dodge = "SIDE",
                                Degrees = 0f, Radius = 0f, Width = 3f, Length = 10f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "FAR", RangeMeters = 4f, Group = 1 },
                        // 출처 — 화면 밖으로 상승했다가 그림자 예고 후 내려찍는다 · 반경 3.0 m · 착지 충격파
                        new() { Phase = 3, NameKr = "부스터 강하", NameEn = "BoosterDrop",
                                Cooldown = 15f, Telegraph = 1f, DamageMul = 1.12f,
                                Shape = "Zone", Draw = "BoosterDrop", Dodge = "ZONE",
                                Degrees = 0f, Radius = 3f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 0 },
                    } },

            // ── 로봇 스네이크 · 연구소 — 구멍에서 나온다 ────────────────────────
            new() { Key = "robot_snakes", NameKr = "로봇 스네이크", NameEn = "Robot Snakes", Sprite = "unit_robot_snakes",
                    Chapter = 5, RoomNo = 10, Gate = "FINAL", Hp = 5200, Atk = 33,
                    State = "Holes", BreakSeconds = 4.0f, BreakCause = "나온 머리를 되들어가기 전에 때렸다",
                    Moves = new Move[]
                    {
                        // 출처 — 구멍 2개의 덮개가 열린다 — 이것이 예고다. 1.15초 뒤 그 구멍에서 머리가 솟는다 · 반경 1.5 m
                        new() { Phase = 1, NameKr = "구멍 개방", NameEn = "HatchOpen",
                                Cooldown = 8f, Telegraph = 1f, DamageMul = 0.85f,
                                Shape = "Lane", Draw = "HatchOpen", Dodge = "ZONE",
                                Degrees = 0f, Radius = 1.5f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 2,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 1 },
                        // 출처 — 나온 머리가 초록 빔을 쏜다 · 폭 0.8 m · 방 끝까지 · 조준선이 먼저 그려진다
                        new() { Phase = 1, NameKr = "레이저", NameEn = "RailLaser",
                                Cooldown = 11f, Telegraph = 1f, DamageMul = 0.73f,
                                Shape = "Line", Draw = "RailLaser", Dodge = "PERP",
                                Degrees = 0f, Radius = 0f, Width = 0.8f, Length = 13f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 0 },
                        // 출처 — 천장에서 파편이 떨어진다 · 그림자 5개 · 각 반경 1.2 m · 1.5초 뒤 낙하
                        new() { Phase = 2, NameKr = "천장 파편", NameEn = "DebrisFall",
                                Cooldown = 13f, Telegraph = 1f, DamageMul = 0.94f,
                                Shape = "Zone", Draw = "DebrisFall", Dodge = "ZONE",
                                Degrees = 0f, Radius = 1.2f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 5,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 0 },
                        // 출처 — 구멍 6개가 전부 열리고 다섯이 솟는다. 안 나오는 구멍이 하나뿐이고 그 위가 안전지대
                        new() { Phase = 3, NameKr = "일제 출현", NameEn = "FullEmergence",
                                Cooldown = 18f, Telegraph = 1f, DamageMul = 1.15f,
                                Shape = "Zone", Draw = "FullEmergence", Dodge = "ZONE",
                                Degrees = 0f, Radius = 1.5f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 6,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 1 },
                    } },

            // ── 슬러지 · 정유소 — 위에서 떨어진다 ────────────────────────────
            new() { Key = "sludge", NameKr = "슬러지", NameEn = "Sludge", Sprite = "unit_sludge",
                    Chapter = 6, RoomNo = 10, Gate = "FINAL", Hp = 6900, Atk = 37,
                    State = "Ceiling", BreakSeconds = 3.5f, BreakCause = "천장에 붙은 동안 아래에서 때려 떨어뜨렸다",
                    Moves = new Move[]
                    {
                        // 출처 — 바닥으로 가라앉았다가 다른 자리에서 솟는다 · 솟는 자리 반경 2.0 m · 바닥이 부풀어 예고
                        new() { Phase = 1, NameKr = "솟아오름", NameEn = "Emerge",
                                Cooldown = 10f, Telegraph = 1f, DamageMul = 0.78f,
                                Shape = "Zone", Draw = "Emerge", Dodge = "ZONE",
                                Degrees = 0f, Radius = 2f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "NEAR", RangeMeters = 4f, Group = 0 },
                        // 출처 — 끈적한 덩어리를 뱉는다 · 착탄 반경 1.5 m · 웅덩이 4초 · 밟으면 이동 속도 절반
                        new() { Phase = 1, NameKr = "뱉기", NameEn = "Spit",
                                Cooldown = 8f, Telegraph = 1f, DamageMul = 0.68f,
                                Shape = "Line", Draw = "Spit", Dodge = "PERP",
                                Degrees = 0f, Radius = 1.5f, Width = 1.5f, Length = 8f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 0 },
                        // 출처 — 점프해 사라진다. 그림자 3개가 방을 돌아다니다 멈추고 1초 뒤 방울이 떨어진다 · 각 반경 1.8 m
                        new() { Phase = 2, NameKr = "천장 붙기", NameEn = "CeilingCling",
                                Cooldown = 14f, Telegraph = 1f, DamageMul = 0.89f,
                                Shape = "Zone", Draw = "CeilingCling", Dodge = "ZONE",
                                Degrees = 0f, Radius = 1.8f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 3,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 1 },
                        // 출처 — 천장 전체로 퍼진다 · 방 전역에 방울 비 · 깨끗한 자리 하나만 남고 2초마다 옮겨 간다 · 8초
                        new() { Phase = 3, NameKr = "천장 확산", NameEn = "CeilingSpread",
                                Cooldown = 20f, Telegraph = 1f, DamageMul = 1.14f,
                                Shape = "Zone", Draw = "CeilingSpread", Dodge = "ZONE",
                                Degrees = 0f, Radius = 1.6f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f,
                                Range = "", RangeMeters = 4f, Group = 1 },
                    } },

        };
    }
}
