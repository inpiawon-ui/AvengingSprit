using System.Collections.Generic;

namespace Game.EditorTools
{
    /// <summary>
    /// 보스 6종과 패턴 24개.
    ///
    /// 단일 출처는 `_exchange/out/42_jobs/AVSR_Bosses.js` 이고, 이 파일은 그것을
    /// 옮겨 적은 것이다 — ⚠ **손으로 고치지 않는다.** 기획이 바뀌면 .js 를 받아 다시 뽑는다.
    ///
    /// ── 왜 방 번호를 들고 있나 ──────────────────────────────────
    /// `ForChapter(chapter)` 로는 방 6개를 못 가른다. 챕터마다 보스가 **둘**이다
    /// (MID · FINAL). 그래서 예전에는 CH1 006 과 012 에 같은 보스가 섰다.
    ///   CH1 006/012 · CH2 008/016 · CH3 010/020
    ///
    /// ── 공간 기본형 6종 ─────────────────────────────────────────
    /// 24개 패턴은 전부 여섯(Arc·Line·Lane·Zone·Dash·Mark)의 매개변수 조합이다.
    /// 여섯만 만들면 24개가 데이터가 된다 — 그리고 **예고를 그리는 도형과
    /// 판정하는 도형이 같은 것**이 되어 "피했는데 맞았다" 가 구조적으로 불가능해진다.
    /// </summary>
    public static class BossDefTable
    {
        public sealed class Move
        {
            public int Phase;              // 이 페이즈부터 쓴다 (1·2·3)
            public string NameKr, NameEn;
            public float Cooldown, Telegraph, DamageMul;
            public string Shape;           // Arc · Line · Lane · Zone · Dash · Mark
            public string Draw;            // 원본 draw.t — 같은 도형의 변주를 가른다
            public string Dodge;           // BACK · SIDE · GAP · PERP · ZONE · CLOSE · SWAP · HOLD
            public float Degrees, Radius, Width, Length, InnerRadius, GapDegrees;
            public int Lanes;
            public float SafeX, SafeY;     // 안전지대(m). 0 이면 없다
        }

        public sealed class Boss
        {
            public string Key, NameKr, NameEn, Sprite;
            public int Chapter, RoomNo;    // CH1 006 → Chapter 1 · RoomNo 6
            public string Gate;            // MID · FINAL
            public int Hp, Atk;
            public string State;           // GUARD · TWIN · SPLIT · 빈 문자열
            public float BreakSeconds;     // 취약 창 길이
            public string BreakCause;
            public Move[] Moves;
        }

        public static readonly Boss[] All =
        {
            new() { Key = "crusher", NameKr = "크러셔", NameEn = "Crusher", Sprite = "unit_crusher",
                    Chapter = 1, RoomNo = 5, Gate = "MID", Hp = 1650, Atk = 18,
                    State = "", BreakSeconds = 2.5f, BreakCause = "돌진을 옆으로 피했다",
                    Moves = new Move[]
                    {
                        new() { Phase = 1, NameKr = "진각", NameEn = "Quake Fist",
                                Cooldown = 8f, Telegraph = 1.25f, DamageMul = 0.94f,
                                Shape = "Arc", Draw = "arc", Dodge = "BACK",
                                Degrees = 180f, Radius = 2.5f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f },
                        new() { Phase = 1, NameKr = "벽 돌진", NameEn = "Wall Charge",
                                Cooldown = 11f, Telegraph = 1.5f, DamageMul = 1.33f,
                                Shape = "Dash", Draw = "dash", Dodge = "SIDE",
                                Degrees = 0f, Radius = 0f, Width = 3.6f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f },
                        new() { Phase = 2, NameKr = "파편 부채꼴", NameEn = "Debris Fan",
                                Cooldown = 10f, Telegraph = 1.1f, DamageMul = 0.72f,
                                Shape = "Arc", Draw = "fan", Dodge = "GAP",
                                Degrees = 100f, Radius = 6f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 7,
                                SafeX = 5f, SafeY = 3.4f },
                        new() { Phase = 3, NameKr = "이중 진각", NameEn = "Double Quake",
                                Cooldown = 14f, Telegraph = 1f, DamageMul = 1.06f,
                                Shape = "Arc", Draw = "halves", Dodge = "SIDE",
                                Degrees = 180f, Radius = 2.5f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f },
                    } },
            new() { Key = "guardian", NameKr = "가디언", NameEn = "Guardian", Sprite = "unit_guardian",
                    Chapter = 1, RoomNo = 10, Gate = "FINAL", Hp = 2400, Atk = 22,
                    State = "GUARD", BreakSeconds = 2f, BreakCause = "방패 행진을 벽으로 유인했다",
                    Moves = new Move[]
                    {
                        new() { Phase = 1, NameKr = "이지스 스윕", NameEn = "Aegis Sweep",
                                Cooldown = 7f, Telegraph = 1f, DamageMul = 0.82f,
                                Shape = "Arc", Draw = "arc", Dodge = "BACK",
                                Degrees = 120f, Radius = 3f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f },
                        new() { Phase = 1, NameKr = "방패 행진", NameEn = "Shield March",
                                Cooldown = 12f, Telegraph = 1.4f, DamageMul = 0.73f,
                                Shape = "Line", Draw = "line", Dodge = "SIDE",
                                Degrees = 0f, Radius = 0f, Width = 3.6f, Length = 7f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f },
                        new() { Phase = 2, NameKr = "반사선", NameEn = "Reflective Line",
                                Cooldown = 10f, Telegraph = 1.3f, DamageMul = 0.91f,
                                Shape = "Line", Draw = "arc", Dodge = "HOLD",
                                Degrees = 120f, Radius = 8f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f },
                        new() { Phase = 3, NameKr = "깨진 이지스 돌진", NameEn = "Broken Aegis Rush",
                                Cooldown = 9f, Telegraph = 0.9f, DamageMul = 1.14f,
                                Shape = "Dash", Draw = "dash", Dodge = "BACK",
                                Degrees = 0f, Radius = 0f, Width = 3f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f },
                    } },
            new() { Key = "kingpin", NameKr = "킹핀", NameEn = "Kingpin", Sprite = "unit_kingpin",
                    Chapter = 2, RoomNo = 5, Gate = "MID", Hp = 3150, Atk = 25,
                    State = "", BreakSeconds = 1.8f, BreakCause = "3점사를 버텼다",
                    Moves = new Move[]
                    {
                        new() { Phase = 1, NameKr = "교차사격 지휘", NameEn = "Crossfire Command",
                                Cooldown = 9f, Telegraph = 1.35f, DamageMul = 0.88f,
                                Shape = "Line", Draw = "crossline", Dodge = "PERP",
                                Degrees = 0f, Radius = 0f, Width = 1.2f, Length = 10f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f },
                        new() { Phase = 1, NameKr = "처형 표식", NameEn = "Execution Mark",
                                Cooldown = 13f, Telegraph = 1.8f, DamageMul = 1.24f,
                                Shape = "Mark", Draw = "mark", Dodge = "SWAP",
                                Degrees = 0f, Radius = 2f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 5f, SafeY = 3f },
                        new() { Phase = 2, NameKr = "엄폐 이동 사격", NameEn = "Cover-to-Cover Burst",
                                Cooldown = 8f, Telegraph = 1f, DamageMul = 0.72f,
                                Shape = "Line", Draw = "burst", Dodge = "CLOSE",
                                Degrees = 0f, Radius = 0f, Width = 1.2f, Length = 10f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 3,
                                SafeX = 2.5f, SafeY = 7.5f },
                        new() { Phase = 3, NameKr = "전탄 개방", NameEn = "All Guns Open",
                                Cooldown = 15f, Telegraph = 1.2f, DamageMul = 1.12f,
                                Shape = "Zone", Draw = "cover", Dodge = "ZONE",
                                Degrees = 0f, Radius = 5f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 4,
                                SafeX = 7.5f, SafeY = 8.5f },
                    } },
            new() { Key = "python", NameKr = "파이썬", NameEn = "Python", Sprite = "unit_python",
                    Chapter = 2, RoomNo = 10, Gate = "FINAL", Hp = 4300, Atk = 29,
                    State = "", BreakSeconds = 2.2f, BreakCause = "조임 나선의 틈으로 빠져나왔다",
                    Moves = new Move[]
                    {
                        new() { Phase = 1, NameKr = "조임 나선", NameEn = "Constrict Spiral",
                                Cooldown = 12f, Telegraph = 1.6f, DamageMul = 0.83f,
                                Shape = "Zone", Draw = "ring", Dodge = "GAP",
                                Degrees = 0f, Radius = 4.5f, Width = 0f, Length = 0f,
                                InnerRadius = 1.5f, GapDegrees = 50f, Lanes = 0,
                                SafeX = 7.4f, SafeY = 4.8f },
                        new() { Phase = 1, NameKr = "독 자국", NameEn = "Venom Spit Trail",
                                Cooldown = 9f, Telegraph = 1.1f, DamageMul = 0.59f,
                                Shape = "Zone", Draw = "trail", Dodge = "PERP",
                                Degrees = 0f, Radius = 1.2f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 4,
                                SafeX = 0f, SafeY = 0f },
                        new() { Phase = 2, NameKr = "꼬리 관문", NameEn = "Tail Gate",
                                Cooldown = 10f, Telegraph = 1.25f, DamageMul = 0.93f,
                                Shape = "Line", Draw = "sweep", Dodge = "SIDE",
                                Degrees = 0f, Radius = 0f, Width = 2f, Length = 10f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f },
                        new() { Phase = 3, NameKr = "탈피 광란", NameEn = "Shed Skin Frenzy",
                                Cooldown = 16f, Telegraph = 1f, DamageMul = 1.17f,
                                Shape = "Dash", Draw = "shed", Dodge = "ZONE",
                                Degrees = 0f, Radius = 0f, Width = 3f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 3f, SafeY = 8.5f },
                    } },
            new() { Key = "robot_snakes", NameKr = "로봇 스네이크", NameEn = "Robot Snakes", Sprite = "unit_robot_snakes",
                    Chapter = 3, RoomNo = 5, Gate = "MID", Hp = 5200, Atk = 33,
                    State = "TWIN", BreakSeconds = 3f, BreakCause = "두 머리 사이에 서서 로켓을 유도했다",
                    Moves = new Move[]
                    {
                        new() { Phase = 1, NameKr = "교대 레일", NameEn = "Alternating Rail",
                                Cooldown = 8f, Telegraph = 1.15f, DamageMul = 0.85f,
                                Shape = "Lane", Draw = "lane", Dodge = "SIDE",
                                Degrees = 0f, Radius = 0f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 3,
                                SafeX = 0f, SafeY = 0f },
                        new() { Phase = 1, NameKr = "케이블 쓸기", NameEn = "Cable Sweep",
                                Cooldown = 11f, Telegraph = 1.35f, DamageMul = 0.73f,
                                Shape = "Line", Draw = "cable", Dodge = "ZONE",
                                Degrees = 0f, Radius = 0f, Width = 1.2f, Length = 10f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 2.6f, SafeY = 8.5f },
                        new() { Phase = 2, NameKr = "자기 유도 로켓", NameEn = "Magnetized Rockets",
                                Cooldown = 13f, Telegraph = 1.5f, DamageMul = 0.94f,
                                Shape = "Line", Draw = "homing", Dodge = "ZONE",
                                Degrees = 0f, Radius = 0f, Width = 1f, Length = 10f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 5f, SafeY = 6.2f },
                        new() { Phase = 3, NameKr = "트윈 오버로드", NameEn = "Twin Overload",
                                Cooldown = 18f, Telegraph = 1.2f, DamageMul = 1.15f,
                                Shape = "Zone", Draw = "overload", Dodge = "ZONE",
                                Degrees = 0f, Radius = 1.5f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 7.4f, SafeY = 8.5f },
                    } },
            new() { Key = "sludge", NameKr = "슬러지", NameEn = "Sludge", Sprite = "unit_sludge",
                    Chapter = 3, RoomNo = 10, Gate = "FINAL", Hp = 6900, Atk = 37,
                    State = "SPLIT", BreakSeconds = 4f, BreakCause = "분열 코어 3개를 다 부쉈다",
                    Moves = new Move[]
                    {
                        new() { Phase = 1, NameKr = "독성 붕괴", NameEn = "Toxic Collapse",
                                Cooldown = 10f, Telegraph = 1.4f, DamageMul = 0.78f,
                                Shape = "Zone", Draw = "quad", Dodge = "ZONE",
                                Degrees = 0f, Radius = 5f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 7f, SafeY = 4f },
                        new() { Phase = 1, NameKr = "슬러지 손", NameEn = "Sludge Hand",
                                Cooldown = 8f, Telegraph = 1.05f, DamageMul = 0.68f,
                                Shape = "Line", Draw = "line", Dodge = "PERP",
                                Degrees = 0f, Radius = 0f, Width = 1.2f, Length = 6f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 0f, SafeY = 0f },
                        new() { Phase = 2, NameKr = "오염 분열", NameEn = "Contaminated Split",
                                Cooldown = 14f, Telegraph = 1.7f, DamageMul = 0.89f,
                                Shape = "Zone", Draw = "split", Dodge = "CLOSE",
                                Degrees = 0f, Radius = 5f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 3,
                                SafeX = 5f, SafeY = 5.5f },
                        new() { Phase = 3, NameKr = "최종 용해", NameEn = "Final Dissolution",
                                Cooldown = 20f, Telegraph = 1.25f, DamageMul = 1.14f,
                                Shape = "Zone", Draw = "island", Dodge = "ZONE",
                                Degrees = 0f, Radius = 1.8f, Width = 0f, Length = 0f,
                                InnerRadius = 0f, GapDegrees = 0f, Lanes = 0,
                                SafeX = 4f, SafeY = 6f },
                    } },
        };

        private static Dictionary<string, Boss> s_index;

        public static Boss Get(string key)
        {
            if (s_index == null)
            {
                s_index = new Dictionary<string, Boss>(All.Length);
                for (int i = 0; i < All.Length; i++) s_index[All[i].Key] = All[i];
            }
            return key != null && s_index.TryGetValue(key, out var e) ? e : null;
        }

        /// <summary>그 방에 서는 보스. 챕터만으로는 못 가른다 — 챕터마다 둘이다.</summary>
        public static Boss At(int chapter, int roomNo)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Chapter == chapter && All[i].RoomNo == roomNo) return All[i];
            return null;
        }
    }
}
