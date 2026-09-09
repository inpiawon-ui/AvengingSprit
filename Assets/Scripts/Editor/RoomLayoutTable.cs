using System.Collections.Generic;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 손으로 짠 방 레이아웃 12종과 60방 배정표. 지형 좌표·적 자리·방 종류를 함께 갖는다.
    ///
    /// 그 전에는 <c>RoomImporterV33</c> 이 지형을 **절차적으로 생성**했다 — 패턴 6종을
    /// 방 ID 해시로 고르고, 밴드마다 찍고, 좌우를 뒤집었다. 결과는 방마다 물건 15~25개에
    /// 절반 이상이 1×1 블록이었고, **어느 방도 형태가 읽히지 않았다.**
    ///
    /// 정본은 오브젝트 좌표를 주지 않는다(<c>ROOM_GEOMETRY</c> 는 템플릿 이름과 개수뿐).
    /// 그래서 이 표가 그 자리를 대신한다 — 물건 2~3개, 대칭·기하학.
    ///
    /// ⚠ **지형지물(Objects)만은 2026-09-09 에 여기서 다시 깎았다.** .js 원본보다 이쪽이 최신이다.
    ///   방마다 5~8개가 서 있었고 12종을 60방이 돌려써서 **어느 방이나 똑같아 보였다**
    ///   (CH1 008 과 CH2 006 은 배치가 한 픽셀도 다르지 않았다).
    ///   게다가 `BULK`(2×2 발자국에 그림은 238px 로 솟는다)를 방 한가운데 세로로 쌓아 두고
    ///   그 뒤에 적을 세워 **적이 아예 안 보였다.**
    ///   2~3개로 줄이고, 솟는 것은 가운데 통로(x 3~7)에서 빼고,
    ///   적 자리 열 곳 전부와 0.8 m 여유를 검사해 빈 자리에만 앉힌다.
    ///   적 자리(Slots)는 .js 원본 그대로다.
    ///
    /// ⚠ 나머지(적 자리·방 배정)는 손으로 고치지 않는다. `_exchange/out/42_jobs` 의 .js 세 개가
    ///   원본이고, 그것을 옮겨 적은 것이다. 기획이 바뀌면 .js 를 받아 다시 옮긴다.
    ///
    /// ⚠ **좌표는 이미 검증된 값이다.** 격자 정렬·범위·겹침을 전부 통과한 상태로 넘어온다.
    ///   홀수 폭은 x.5, 짝수 폭은 정수에 앉는다 — 반 칸이 섞이면 그 물건만 바닥 줄눈을
    ///   가로질러 혼자 어긋나 보인다. 임포터가 다시 스냅·클램프하지 않는 이유다.
    ///
    /// 발자국은 <c>RoomImporterV33.PropSize</c> 와 같아야 한다.
    ///   PILLAR 1×1 · CRATE 2×1 · LOW_COVER|BARRICADE 3×1
    ///   BULK 2×2(막힘) · TIMED_SPIKE 2×2(바닥) · ROTATING_BLADE 2×2(도는 톱니)
    ///
    /// ⚠ 종류 이름은 **발자국 이름**이지 그림 이름이 아니다. 실제 그림은 무대가 고른다
    ///   (<c>BattleDirector.EnvSprite</c>). 그 무대에 그 발자국 그림이 없으면
    ///   다른 무대 것을 빌려 쓴다 — 색이 안 맞는 것이 빈 상자보다 낫다.
    /// </summary>
    public static class RoomLayoutTable
    {
        // ── 적 자리의 역할 ──────────────────────────────────────
        //
        //   FRONT   앞줄. 길을 막는 벽
        //   RANGED  원거리. 붙기 전에 아프게 한다
        //   FLANK   측면. 따라붙는다
        //   BACK    후방. **빼앗을 수 있는 몸이 여기 선다**
        //
        // ⚠ 호스트는 BACK 이다. 앞줄에 있으면 그냥 가까운 것을 뺏으면 되고,
        //   그건 선택이 아니다. 잡몹을 뚫고 들어가야 손에 넣는 것이 된다.
        //   (예외: CH1 001 튜토리얼 — 빙의를 가르치는 방이라 앞줄에도 세운다)

        public struct Obj
        {
            public string Kind;
            public Vector2 At;      // 발자국 중심(미터)
            public Obj(string kind, float x, float y) { Kind = kind; At = new Vector2(x, y); }
        }

        public struct Slot
        {
            public Vector2 At;
            public string Role;     // FRONT · RANGED · FLANK · BACK
            public string Unit;     // 우리 액터 키
            public Slot(float x, float y, string role, string unit)
            { At = new Vector2(x, y); Role = role; Unit = unit; }
        }

        public sealed class Layout
        {
            public string Id;
            public string NameKr;
            public Obj[] Objects;
            public Slot[] Slots;
        }

        // 잡몹 키는 `BattleDirector` 의 `CreateTrash` 세 종과 같아야 한다.
        private const string Skeleton = "skeleton";
        private const string Bat      = "bat";
        private const string Gunner   = "scrapgunner";

        // 호스트 키는 `HostTable` 의 `_hostKey` 와 같아야 한다.
        private const string Amazon = "amazon";
        private const string Cmg    = "commando_mg";
        private const string Cgren  = "commando_grenade";

        /// <summary>이 자리가 **빼앗을 수 있는 몸**인가. 임포터가 `POSSESSION_TARGET` 으로 찍는다.</summary>
        public static bool IsHostUnit(string unit)
            => unit == Amazon || unit == Cmg || unit == Cgren;

        private static readonly Dictionary<string, Layout> Table = new()
        {
            ["A"] = new Layout
            {
                Id = "A", NameKr = "지그재그 관문",
                Objects = new[]
                {
                    new Obj("BARRICADE", 7.5f, 9.5f), new Obj("BARRICADE", 4.5f, 2.5f)
                
                },
                Slots = new[]
                {
                    new Slot(2.5f, 9.5f, "BACK", Amazon),
                    new Slot(7f, 7.5f, "RANGED", Gunner),
                    new Slot(4.5f, 6.5f, "FRONT", Skeleton),
                    new Slot(2f, 3.5f, "FRONT", Amazon),
                    new Slot(5f, 9.5f, "BACK", Gunner),
                    new Slot(7.5f, 3.5f, "FLANK", Bat),
                    new Slot(1.5f, 6.5f, "FLANK", Bat),
                    new Slot(5f, 4.5f, "FRONT", Skeleton),
                    new Slot(8.5f, 6f, "RANGED", Gunner),
                    new Slot(1.5f, 2f, "FLANK", Bat),
                },
            },

            ["B"] = new Layout
            {
                Id = "B", NameKr = "쌍기둥 통로",
                Objects = new[]
                {
                    new Obj("PILLAR", 3.5f, 6.5f), new Obj("PILLAR", 8.5f, 5.5f),
                    new Obj("CRATE", 2f, 9.5f)
                
                },
                Slots = new[]
                {
                    new Slot(5f, 10f, "BACK", Cmg),
                    new Slot(2f, 7.5f, "FLANK", Bat),
                    new Slot(8f, 7.5f, "FLANK", Bat),
                    new Slot(5f, 6f, "RANGED", Gunner),
                    new Slot(2f, 3.5f, "FRONT", Skeleton),
                    new Slot(8f, 3.5f, "FRONT", Amazon),
                    new Slot(5f, 4f, "FRONT", Skeleton),
                    new Slot(5f, 8.5f, "FRONT", Skeleton),
                    new Slot(5f, 1.5f, "RANGED", Gunner),
                    new Slot(8f, 10f, "FLANK", Bat),
                },
            },

            ["C"] = new Layout
            {
                Id = "C", NameKr = "중앙 요새",
                Objects = new[]
                {
                    new Obj("CRATE", 5f, 6.5f), new Obj("PILLAR", 8.5f, 2.5f),
                    new Obj("PILLAR", 6.5f, 4.5f)
                
                },
                Slots = new[]
                {
                    new Slot(5f, 9.5f, "BACK", Cgren),
                    new Slot(2.5f, 7.5f, "RANGED", Gunner),
                    new Slot(7.5f, 7.5f, "RANGED", Gunner),
                    new Slot(5f, 3f, "FRONT", Skeleton),
                    new Slot(8f, 5f, "FLANK", Bat),
                    new Slot(2f, 5f, "FLANK", Bat),
                    new Slot(2.5f, 2.5f, "FRONT", Skeleton),
                    new Slot(2.5f, 10f, "BACK", Cmg),
                    new Slot(7.5f, 10f, "RANGED", Gunner),
                    new Slot(5f, 1.5f, "FRONT", Skeleton),
                },
            },

            ["D"] = new Layout
            {
                Id = "D", NameKr = "십자 분단",
                Objects = new[]
                {
                    new Obj("PILLAR", 4.5f, 6.5f), new Obj("PILLAR", 1.5f, 2.5f)
                
                },
                Slots = new[]
                {
                    new Slot(2.5f, 9.5f, "BACK", Cmg),
                    new Slot(7.5f, 9.5f, "BACK", Gunner),
                    new Slot(2.5f, 5.5f, "FLANK", Skeleton),
                    new Slot(7.5f, 5.5f, "FLANK", Bat),
                    new Slot(5f, 2.5f, "FRONT", Skeleton),
                    new Slot(3.5f, 2.5f, "FRONT", Bat),
                    new Slot(6.5f, 7.5f, "RANGED", Gunner),
                    new Slot(5f, 10f, "BACK", Cgren),
                    new Slot(2.5f, 7.5f, "RANGED", Gunner),
                    new Slot(8f, 2.5f, "FRONT", Skeleton),
                },
            },

            ["E"] = new Layout
            {
                Id = "E", NameKr = "계단",
                Objects = new[]
                {
                    new Obj("CRATE", 8f, 6.5f), new Obj("CRATE", 2f, 2.5f),
                    new Obj("CRATE", 7f, 2.5f)
                
                },
                Slots = new[]
                {
                    new Slot(7.5f, 9.5f, "BACK", Cmg),
                    new Slot(3.5f, 8.5f, "RANGED", Gunner),
                    new Slot(3f, 6.5f, "FRONT", Skeleton),
                    new Slot(7f, 4f, "FLANK", Bat),
                    new Slot(2f, 4.5f, "FRONT", Skeleton),
                    new Slot(5.5f, 9.5f, "BACK", Gunner),
                    new Slot(5f, 3.5f, "FRONT", Bat),
                    new Slot(2f, 10f, "BACK", Cgren),
                    new Slot(8.5f, 8f, "RANGED", Gunner),
                    new Slot(5f, 2f, "FRONT", Bat),
                },
            },

            ["F"] = new Layout
            {
                Id = "F", NameKr = "모서리 요새",
                Objects = new[]
                {
                    new Obj("CRATE", 6f, 6.5f), new Obj("CRATE", 2f, 8.5f)
                
                },
                Slots = new[]
                {
                    new Slot(4f, 9.5f, "BACK", Cmg),
                    new Slot(6.5f, 9.5f, "BACK", Gunner),
                    new Slot(5f, 8f, "RANGED", Gunner),
                    new Slot(3.5f, 3.5f, "FRONT", Skeleton),
                    new Slot(6.5f, 3.5f, "FRONT", Bat),
                    new Slot(5f, 5f, "FRONT", Skeleton),
                    new Slot(5f, 2f, "FRONT", Bat),
                    new Slot(2f, 7f, "FLANK", Bat),
                    new Slot(8f, 7f, "RANGED", Gunner),
                    new Slot(2f, 10f, "BACK", Cgren),
                },
            },

            ["G"] = new Layout
            {
                Id = "G", NameKr = "좁은 문",
                Objects = new[]
                {
                    new Obj("BARRICADE", 2.5f, 7.5f), new Obj("BARRICADE", 7.5f, 2.5f)
                
                },
                Slots = new[]
                {
                    new Slot(5f, 8.5f, "BACK", Cgren),
                    new Slot(2.5f, 9.5f, "BACK", Gunner),
                    new Slot(7.5f, 9.5f, "BACK", Gunner),
                    new Slot(3f, 5.5f, "FRONT", Skeleton),
                    new Slot(7f, 5.5f, "FRONT", Bat),
                    new Slot(1.5f, 5.5f, "FLANK", Bat),
                    new Slot(5f, 6.5f, "RANGED", Skeleton),
                    new Slot(5f, 10f, "BACK", Cmg),
                    new Slot(8.5f, 6.5f, "RANGED", Gunner),
                    new Slot(5f, 3.5f, "FRONT", Skeleton),
                },
            },

            ["H"] = new Layout
            {
                Id = "H", NameKr = "가시밭",
                Objects = new[]
                {
                    new Obj("TIMED_SPIKE", 7f, 5f), new Obj("TIMED_SPIKE", 3f, 8f),
                    new Obj("CRATE", 2f, 4.5f)
                
                },
                Slots = new[]
                {
                    new Slot(5f, 10f, "BACK", Cmg),
                    new Slot(1.5f, 8.5f, "FLANK", Bat),
                    new Slot(8.5f, 8.5f, "FLANK", Skeleton),
                    new Slot(5f, 6f, "RANGED", Gunner),
                    new Slot(3f, 3f, "FRONT", Skeleton),
                    new Slot(7f, 3f, "FRONT", Bat),
                    new Slot(5f, 8.5f, "RANGED", Gunner),
                    new Slot(2.5f, 6.5f, "FLANK", Skeleton),
                    new Slot(7.5f, 6.5f, "FLANK", Bat),
                    new Slot(2f, 10f, "BACK", Cgren),
                },
            },

            ["I"] = new Layout
            {
                Id = "I", NameKr = "회전 관문",
                Objects = new[]
                {
                    new Obj("ROTATING_BLADE", 6f, 6f), new Obj("PILLAR", 1.5f, 9.5f),
                    new Obj("PILLAR", 1.5f, 2.5f)
                
                },
                Slots = new[]
                {
                    new Slot(5f, 10f, "BACK", Cmg),
                    new Slot(2f, 7.5f, "FLANK", Bat),
                    new Slot(8f, 7.5f, "FLANK", Bat),
                    new Slot(2f, 5f, "RANGED", Gunner),
                    new Slot(8f, 5f, "RANGED", Gunner),
                    new Slot(3.5f, 3.5f, "FRONT", Skeleton),
                    new Slot(6.5f, 3.5f, "FRONT", Bat),
                    new Slot(3.5f, 8f, "BACK", Cgren),
                    new Slot(6.5f, 8f, "FLANK", Skeleton),
                    new Slot(5f, 1.5f, "FRONT", Bat),
                },
            },

            ["J"] = new Layout
            {
                Id = "J", NameKr = "엇갈린 문",
                Objects = new[]
                {
                    new Obj("BARRICADE", 5.5f, 5.5f), new Obj("BARRICADE", 7.5f, 2.5f)
                
                },
                Slots = new[]
                {
                    new Slot(8f, 9.5f, "BACK", Cmg),
                    new Slot(5f, 9.5f, "BACK", Gunner),
                    new Slot(3f, 7.5f, "RANGED", Gunner),
                    new Slot(7f, 7.5f, "RANGED", Gunner),
                    new Slot(2f, 5.5f, "FRONT", Skeleton),
                    new Slot(8f, 5.5f, "FLANK", Bat),
                    new Slot(2f, 3.5f, "FRONT", Bat),
                    new Slot(5f, 3.5f, "FRONT", Skeleton),
                    new Slot(6.5f, 10f, "FLANK", Bat),
                    new Slot(3.5f, 10f, "BACK", Cgren),
                },
            },

            ["K"] = new Layout
            {
                Id = "K", NameKr = "사선 분단",
                Objects = new[]
                {
                    new Obj("BULK", 2f, 3f), new Obj("CRATE", 8f, 8.5f),
                    new Obj("PILLAR", 1.5f, 9.5f)
                
                },
                Slots = new[]
                {
                    new Slot(6.5f, 10f, "BACK", Cmg),
                    new Slot(2f, 7f, "FLANK", Bat),
                    new Slot(8f, 7f, "RANGED", Gunner),
                    new Slot(5.5f, 7.5f, "FRONT", Skeleton),
                    new Slot(2f, 5f, "FRONT", Skeleton),
                    new Slot(8f, 5.5f, "FRONT", Bat),
                    new Slot(5f, 3f, "RANGED", Gunner),
                    new Slot(3.5f, 10f, "BACK", Cgren),
                    new Slot(6.5f, 2f, "FLANK", Bat),
                    new Slot(8.5f, 1.5f, "FLANK", Skeleton),
                },
            },

            ["L"] = new Layout
            {
                Id = "L", NameKr = "네 귀퉁이 가시",
                Objects = new[]
                {
                    new Obj("TIMED_SPIKE", 5f, 6f), new Obj("TIMED_SPIKE", 8f, 10f),
                    new Obj("CRATE", 8f, 4.5f)
                
                },
                Slots = new[]
                {
                    new Slot(3.5f, 10f, "BACK", Cmg),
                    new Slot(6.5f, 10f, "BACK", Gunner),
                    new Slot(2f, 6.5f, "FLANK", Bat),
                    new Slot(8f, 6.5f, "FLANK", Bat),
                    new Slot(5f, 8f, "RANGED", Gunner),
                    new Slot(3.5f, 2.5f, "FRONT", Skeleton),
                    new Slot(6.5f, 2.5f, "FRONT", Bat),
                    new Slot(5f, 4f, "FRONT", Skeleton),
                    new Slot(6.5f, 7.5f, "RANGED", Gunner),
                    new Slot(3.5f, 7.5f, "BACK", Cgren),
                },
            },

            // == 도랑 레이아웃 6종 (2026-09-08) =======================
            //
            // 도랑(`CHANNEL_H`·`CHANNEL_V`)은 **몸은 못 건너고 탄은 지나가는** 자리다.
            //
            // 주의: 낱개로 흩뿌리면 그냥 걸리적거리는 웅덩이가 된다. 아래 여섯은 전부
            //   **길을 만든다** - 어디로 갈 수 있는지가 도랑의 배치로 정해지고,
            //   그 길목에 가시·톱니·적을 놓아 「지나갈 것인가」를 묻는다.
            //
            //   조각 규격: 가로 2x1 m · 세로 1x2 m. 이어 붙여 긴 도랑을 만든다.
            //   짝수 폭은 중심이 정수, 홀수 폭은 .5 에 앉는다(위 규약).

            ["M"] = new Layout
            {
                // 가로 도랑이 방을 끊고 **가운데 2 m 만** 열려 있다.
                // 아래 기둥 둘이 그 목으로 깔때기처럼 몰아넣는다.
                Id = "M", NameKr = "좁은 목",
                Objects = new[]
                {
                    new Obj("CHANNEL_H", 1f, 7.5f), new Obj("CHANNEL_H", 3f, 7.5f),
                    new Obj("CHANNEL_H", 7f, 7.5f), new Obj("CHANNEL_H", 9f, 7.5f),
                    new Obj("PILLAR", 3.5f, 4.5f), new Obj("PILLAR", 6.5f, 4.5f),
                    new Obj("CRATE", 2f, 10.5f), new Obj("CRATE", 8f, 10.5f),
                },
                Slots = new[]
                {
                    new Slot(5f, 9.5f, "FRONT", Skeleton),
                    new Slot(2f, 9f, "RANGED", Gunner),
                    new Slot(8f, 9f, "RANGED", Gunner),
                    new Slot(5f, 11.5f, "BACK", Cmg),
                    new Slot(3f, 3f, "FLANK", Bat),
                    new Slot(7f, 3f, "FLANK", Bat),
                },
            },

            ["N"] = new Layout
            {
                // 세로 도랑이 방 한가운데를 갈라 **왼쪽 길·오른쪽 길** 둘이 된다.
                // 어느 쪽으로 가느냐가 곧 어떤 적을 먼저 만나느냐다.
                Id = "N", NameKr = "두 갈래",
                Objects = new[]
                {
                    new Obj("CHANNEL_V", 4.5f, 5f), new Obj("CHANNEL_V", 4.5f, 7f),
                    new Obj("CHANNEL_V", 4.5f, 9f),
                    new Obj("BULK", 2f, 8f), new Obj("BULK", 8f, 8f),
                    new Obj("CRATE", 2f, 3.5f), new Obj("CRATE", 8f, 3.5f),
                },
                Slots = new[]
                {
                    new Slot(2f, 10.5f, "RANGED", Gunner),
                    new Slot(8f, 10.5f, "RANGED", Gunner),
                    new Slot(2f, 6f, "FRONT", Skeleton),
                    new Slot(8f, 6f, "FRONT", Bat),
                    new Slot(5f, 11.5f, "BACK", Cgren),
                    new Slot(5f, 3f, "FLANK", Skeleton),
                },
            },

            ["O"] = new Layout
            {
                // **양쪽이 도랑, 가운데가 길.** 좁은 길 한복판에 가시를 놓아
                // 「빨리 지날 것인가 기다릴 것인가」를 만든다.
                // 원거리 적은 도랑 건너편에 세운다 - 탄은 도랑을 넘어오지만 몸은 못 온다.
                Id = "O", NameKr = "가운데 길",
                Objects = new[]
                {
                    new Obj("CHANNEL_V", 2.5f, 5f), new Obj("CHANNEL_V", 2.5f, 7f),
                    new Obj("CHANNEL_V", 2.5f, 9f),
                    new Obj("CHANNEL_V", 7.5f, 5f), new Obj("CHANNEL_V", 7.5f, 7f),
                    new Obj("CHANNEL_V", 7.5f, 9f),
                    new Obj("TIMED_SPIKE", 5f, 7f), new Obj("CRATE", 5f, 10.5f),
                },
                Slots = new[]
                {
                    new Slot(5f, 9.5f, "FRONT", Skeleton),
                    new Slot(1.5f, 7f, "RANGED", Gunner),
                    new Slot(8.5f, 7f, "RANGED", Gunner),
                    new Slot(5f, 11.5f, "BACK", Cmg),
                    new Slot(3.5f, 3f, "FLANK", Bat),
                    new Slot(6.5f, 3f, "FLANK", Bat),
                },
            },

            ["P"] = new Layout
            {
                // 가로 도랑 두 줄이 **열린 쪽을 서로 반대로** 두어 S 자로 지나가게 한다.
                // 오른쪽으로 돌아 올라갔다가 다시 왼쪽으로 꺾어야 출구에 닿는다.
                Id = "P", NameKr = "지그재그 물길",
                Objects = new[]
                {
                    new Obj("CHANNEL_H", 1f, 5.5f), new Obj("CHANNEL_H", 3f, 5.5f),
                    new Obj("CHANNEL_H", 5f, 5.5f),
                    new Obj("CHANNEL_H", 5f, 9.5f), new Obj("CHANNEL_H", 7f, 9.5f),
                    new Obj("CHANNEL_H", 9f, 9.5f),
                    new Obj("PILLAR", 8.5f, 7.5f), new Obj("CRATE", 2f, 7.5f),
                },
                Slots = new[]
                {
                    new Slot(8f, 7.5f, "FRONT", Skeleton),
                    new Slot(2f, 11f, "BACK", Cgren),
                    new Slot(7f, 11f, "RANGED", Gunner),
                    new Slot(3f, 7.5f, "RANGED", Gunner),
                    new Slot(5f, 3f, "FLANK", Bat),
                    new Slot(8f, 3.5f, "FLANK", Skeleton),
                },
            },

            ["Q"] = new Layout
            {
                // ㄷ 자 도랑이 **가운데에 섬**을 만들고 아래로만 열려 있다.
                // 섬 안에 톱니가 돈다 - 들어가면 빠져나올 길이 하나뿐이다.
                Id = "Q", NameKr = "섬",
                Objects = new[]
                {
                    new Obj("CHANNEL_H", 3f, 9.5f), new Obj("CHANNEL_H", 5f, 9.5f),
                    new Obj("CHANNEL_H", 7f, 9.5f),
                    new Obj("CHANNEL_V", 2.5f, 6f), new Obj("CHANNEL_V", 2.5f, 8f),
                    new Obj("CHANNEL_V", 7.5f, 6f), new Obj("CHANNEL_V", 7.5f, 8f),
                    new Obj("ROTATING_BLADE", 5f, 7f),
                },
                Slots = new[]
                {
                    new Slot(5f, 11f, "BACK", Cmg),
                    new Slot(1.5f, 10.5f, "RANGED", Gunner),
                    new Slot(8.5f, 10.5f, "RANGED", Gunner),
                    new Slot(4f, 5f, "FRONT", Skeleton),
                    new Slot(6f, 5f, "FRONT", Bat),
                    new Slot(5f, 3f, "FLANK", Skeleton),
                },
            },

            ["R"] = new Layout
            {
                // 마지막 챕터. **좌우가 통째로 용암**이고 가운데 넓은 회랑만 남는다.
                // 회랑 안에 가시 둘과 톱니 하나 - 넓지만 안전한 자리는 계속 옮겨 간다.
                Id = "R", NameKr = "용광로 회랑",
                Objects = new[]
                {
                    new Obj("CHANNEL_V", 1.5f, 5f), new Obj("CHANNEL_V", 1.5f, 7f),
                    new Obj("CHANNEL_V", 1.5f, 9f),
                    new Obj("CHANNEL_V", 8.5f, 5f), new Obj("CHANNEL_V", 8.5f, 7f),
                    new Obj("CHANNEL_V", 8.5f, 9f),
                    new Obj("TIMED_SPIKE", 3f, 8f), new Obj("TIMED_SPIKE", 7f, 8f),
                    new Obj("ROTATING_BLADE", 5f, 5f),
                },
                Slots = new[]
                {
                    new Slot(5f, 11f, "BACK", Cgren),
                    new Slot(3f, 10.5f, "RANGED", Gunner),
                    new Slot(7f, 10.5f, "RANGED", Gunner),
                    new Slot(5f, 7.5f, "FRONT", Skeleton),
                    new Slot(3.5f, 3f, "FLANK", Bat),
                    new Slot(6.5f, 3f, "FLANK", Bat),
                },
            },

        };

        public static Layout Get(string id)
            => id != null && Table.TryGetValue(id, out var l) ? l : null;

        // ── 조합 이름 ────────────────────────────────────────────
        //
        // 배정표가 쓰는 값이자 `LeadTags`·`RestTags` 가 `StartsWith` 로 가르는 접두사다.
        private const string Frontline = "FRONTLINE";
        private const string Flank     = "FLANK";
        private const string Backline  = "BACKLINE";

        // ── 60방 배정표 (v4.0 · 6챕터 × 10방) ────────────────────
        //
        // ⚠ **이 표가 정본보다 우선한다.** 방 종류·적 수·엘리트 수·조합을 여기서 읽는다.
        //   정본(CH1 12 · CH2 16 · CH3 20 = 48방)에는 챕터가 셋뿐이라
        //   4~6챕터는 정본에 아예 없다. **이 표가 유일한 출처다.**
        //
        // ── 왜 6챕터인가 ─────────────────────────────────────────
        // 원작 어벤징 스피릿이 정확히 6스테이지다
        // (근거: `Reference/Original/Miscellaneous - Stage End Cutscenes.png` 컷신 6장).
        // 우리 무대 6종이 그 6스테이지고, **챕터 하나 = 무대 하나**다.
        //
        //   CH1 쓰레기장 · CH2 미사일기지 · CH3 밤거리
        //   CH4 옥상     · CH5 연구소     · CH6 정유소
        //
        // 3챕터일 때는 무대가 6이고 챕터가 3이라 **챕터를 반으로 갈라** 여섯을
        // 만들어야 했다. 방 번호가 하나 밀리면 배경도 같이 밀리는 계산이었다.
        // 챕터=무대가 되면서 그 계산이 통째로 사라졌다.
        //
        // ── 방 구조는 챕터마다 같다 ───────────────────────────────
        //
        //   001 002 003  전투
        //   004          이벤트 — 「몸」   (중간 보스 직전이라 상태를 고친다)
        //   005          중간 보스 — 호스트 대장 1 + 부하 3
        //   006          전투
        //   007          이벤트 — 「판돈」 (최종까지 3방, 지금 안 걸면 늦는다)
        //   008          전투
        //   009          엘리트
        //   010          최종 보스 — 원작 보스
        //
        // ⚠ **전투방은 여섯이고 레이아웃 창도 여섯이다.** 딱 맞는다.
        //   005(중간 보스)는 창에서 빼고 **F 모서리 요새 고정**이다 —
        //   네 모서리에만 덩어리가 있고 가운데가 트여 있어 대장 1 + 부하 3 이 서기에 넓고,
        //   **챕터마다 같아야 「아 중간 보스방이구나」가 읽힌다.**
        //   그래서 F 는 난이도 사다리에서 빠졌다.
        //
        // 난이도 사다리: A → B → E → J → D → C → K → G → H → L → I
        //   챕터마다 여섯씩 창을 옮겨 간다. A 는 CH1 에만, I 는 CH6 에만 나온다.

        /// <summary>이벤트 방이 뽑는 풀. 자리마다 성격이 다르다.</summary>
        public const string PoolBody  = "BODY";    // 004 — 몸 상태를 고친다
        public const string PoolStake = "STAKE";   // 007 — 최종 보스에 걸 판돈

        /// <summary>중간 보스 방은 챕터마다 같은 모양이다.</summary>
        public const string MidBossLayout = "F";
        public const string MidBossFloor  = "roomfloor_env_holding";

        public sealed class RoomPlan
        {
            public string LayoutId;    // null 이면 지형을 얹지 않는다 (최종 보스 아레나)
            public string Type;        // TUTORIAL · COMBAT · EVENT · MIDBOSS · ELITE · BOSS
            public int Count;          // 이 방에 서는 적 수 (엘리트·부하 포함)
            public int Elite;          // 그중 엘리트 수
            public string Comp;        // 조합
            public string Pool;        // EVENT 일 때만 — 뽑을 이벤트 풀
        }

        private static readonly Dictionary<int, RoomPlan[]> Chapters = new()
        {
            [1] = new RoomPlan[]
            {
                new() { LayoutId = "A", Type = "TUTORIAL", Count = 4, Elite = 0, Comp = Backline },   // 01
                new() { LayoutId = "B", Type = "COMBAT", Count = 5, Elite = 0, Comp = Frontline },   // 02
                new() { LayoutId = "E", Type = "COMBAT", Count = 5, Elite = 0, Comp = Flank },   // 03
                new() { Type = "EVENT", Pool = PoolBody },    // 04 이벤트 — 몸
                new() { LayoutId = MidBossLayout, Type = "MIDBOSS", Count = 4, Elite = 0, Comp = Flank },   // 05 중간 보스 — 대장 1 + 부하 3
                new() { LayoutId = "J", Type = "COMBAT", Count = 6, Elite = 0, Comp = Flank },   // 06
                new() { Type = "EVENT", Pool = PoolStake },   // 07 이벤트 — 판돈
                new() { LayoutId = "D", Type = "COMBAT", Count = 6, Elite = 0, Comp = Frontline },   // 08
                new() { LayoutId = "C", Type = "ELITE", Count = 6, Elite = 1, Comp = Flank },   // 09
                new() { Type = "BOSS" },                              // 10 최종 보스
            },
            [2] = new RoomPlan[]
            {
                new() { LayoutId = "B", Type = "COMBAT", Count = 5, Elite = 0, Comp = Frontline },   // 01
                new() { LayoutId = "E", Type = "COMBAT", Count = 5, Elite = 0, Comp = Flank },   // 02
                new() { LayoutId = "J", Type = "COMBAT", Count = 6, Elite = 0, Comp = Backline },   // 03
                new() { Type = "EVENT", Pool = PoolBody },    // 04 이벤트 — 몸
                new() { LayoutId = MidBossLayout, Type = "MIDBOSS", Count = 4, Elite = 0, Comp = Backline },   // 05 중간 보스 — 대장 1 + 부하 3
                new() { LayoutId = "D", Type = "COMBAT", Count = 6, Elite = 0, Comp = Backline },   // 06
                new() { Type = "EVENT", Pool = PoolStake },   // 07 이벤트 — 판돈
                new() { LayoutId = "C", Type = "COMBAT", Count = 7, Elite = 0, Comp = Flank },   // 08
                new() { LayoutId = "K", Type = "ELITE", Count = 7, Elite = 1, Comp = Backline },   // 09
                new() { Type = "BOSS" },                              // 10 최종 보스
            },
            [3] = new RoomPlan[]
            {
                new() { LayoutId = "E", Type = "COMBAT", Count = 5, Elite = 0, Comp = Flank },   // 01
                new() { LayoutId = "J", Type = "COMBAT", Count = 6, Elite = 0, Comp = Backline },   // 02
                new() { LayoutId = "D", Type = "COMBAT", Count = 6, Elite = 0, Comp = Frontline },   // 03
                new() { Type = "EVENT", Pool = PoolBody },    // 04 이벤트 — 몸
                new() { LayoutId = MidBossLayout, Type = "MIDBOSS", Count = 4, Elite = 0, Comp = Frontline },   // 05 중간 보스 — 대장 1 + 부하 3
                new() { LayoutId = "C", Type = "COMBAT", Count = 7, Elite = 0, Comp = Frontline },   // 06
                new() { Type = "EVENT", Pool = PoolStake },   // 07 이벤트 — 판돈
                new() { LayoutId = "K", Type = "COMBAT", Count = 7, Elite = 0, Comp = Backline },   // 08
                new() { LayoutId = "G", Type = "ELITE", Count = 7, Elite = 1, Comp = Frontline },   // 09
                new() { Type = "BOSS" },                              // 10 최종 보스
            },
            [4] = new RoomPlan[]
            {
                new() { LayoutId = "J", Type = "COMBAT", Count = 6, Elite = 0, Comp = Backline },   // 01
                new() { LayoutId = "D", Type = "COMBAT", Count = 6, Elite = 0, Comp = Frontline },   // 02
                new() { LayoutId = "C", Type = "COMBAT", Count = 7, Elite = 0, Comp = Flank },   // 03
                new() { Type = "EVENT", Pool = PoolBody },    // 04 이벤트 — 몸
                new() { LayoutId = MidBossLayout, Type = "MIDBOSS", Count = 4, Elite = 0, Comp = Flank },   // 05 중간 보스 — 대장 1 + 부하 3
                new() { LayoutId = "K", Type = "COMBAT", Count = 7, Elite = 0, Comp = Flank },   // 06
                new() { Type = "EVENT", Pool = PoolStake },   // 07 이벤트 — 판돈
                new() { LayoutId = "G", Type = "COMBAT", Count = 7, Elite = 0, Comp = Frontline },   // 08
                new() { LayoutId = "H", Type = "ELITE", Count = 8, Elite = 1, Comp = Flank },   // 09
                new() { Type = "BOSS" },                              // 10 최종 보스
            },
            [5] = new RoomPlan[]
            {
                new() { LayoutId = "D", Type = "COMBAT", Count = 6, Elite = 0, Comp = Frontline },   // 01
                new() { LayoutId = "C", Type = "COMBAT", Count = 7, Elite = 0, Comp = Flank },   // 02
                new() { LayoutId = "K", Type = "COMBAT", Count = 7, Elite = 0, Comp = Backline },   // 03
                new() { Type = "EVENT", Pool = PoolBody },    // 04 이벤트 — 몸
                new() { LayoutId = MidBossLayout, Type = "MIDBOSS", Count = 4, Elite = 0, Comp = Backline },   // 05 중간 보스 — 대장 1 + 부하 3
                new() { LayoutId = "G", Type = "COMBAT", Count = 7, Elite = 0, Comp = Backline },   // 06
                new() { Type = "EVENT", Pool = PoolStake },   // 07 이벤트 — 판돈
                new() { LayoutId = "H", Type = "COMBAT", Count = 8, Elite = 0, Comp = Flank },   // 08
                new() { LayoutId = "L", Type = "ELITE", Count = 8, Elite = 1, Comp = Backline },   // 09
                new() { Type = "BOSS" },                              // 10 최종 보스
            },
            [6] = new RoomPlan[]
            {
                new() { LayoutId = "C", Type = "COMBAT", Count = 7, Elite = 0, Comp = Flank },   // 01
                new() { LayoutId = "K", Type = "COMBAT", Count = 7, Elite = 0, Comp = Backline },   // 02
                new() { LayoutId = "G", Type = "COMBAT", Count = 8, Elite = 0, Comp = Frontline },   // 03
                new() { Type = "EVENT", Pool = PoolBody },    // 04 이벤트 — 몸
                new() { LayoutId = MidBossLayout, Type = "MIDBOSS", Count = 4, Elite = 0, Comp = Frontline },   // 05 중간 보스 — 대장 1 + 부하 3
                new() { LayoutId = "H", Type = "COMBAT", Count = 8, Elite = 0, Comp = Frontline },   // 06
                new() { Type = "EVENT", Pool = PoolStake },   // 07 이벤트 — 판돈
                new() { LayoutId = "L", Type = "COMBAT", Count = 8, Elite = 0, Comp = Backline },   // 08
                new() { LayoutId = "I", Type = "ELITE", Count = 8, Elite = 1, Comp = Frontline },   // 09
                new() { Type = "BOSS" },                              // 10 최종 보스
            },
        };

        /// <summary>이 방의 배정. 없으면 <c>null</c>.</summary>
        public static RoomPlan PlanFor(int chapter, int roomNo)
        {
            if (!Chapters.TryGetValue(chapter, out var list)) return null;
            return roomNo >= 1 && roomNo <= list.Length ? list[roomNo - 1] : null;
        }

        /// <summary>이 방이 쓸 레이아웃. 보스방은 <c>null</c> 이다.</summary>
        public static Layout For(int chapter, int roomNo, bool isBoss)
        {
            if (isBoss) return null;
            var plan = PlanFor(chapter, roomNo);
            return plan != null ? Get(plan.LayoutId) : null;
        }

        // ── 조합 → 자리 우선순위 ─────────────────────────────────
        //
        // 같은 레이아웃에 조합만 바꿔 끼우면 **다른 방이 된다.** 앞줄부터 채운 방과
        // 측면부터 채운 방은 적이 서는 자리가 통째로 달라서, 지형이 같아도
        // 걸어 들어갔을 때 읽히는 것이 다르다. 60방을 12종으로 채우는 힘이 여기서 나온다.

        /// <summary>이 조합의 **선두 태그 둘**. 이 둘을 번갈아 가져간다.</summary>
        private static string[] LeadTags(string composition)
        {
            // 정본 조합 ID 는 뒤에 난이도가 붙는다(`..._CH1_D1`). 앞부분만 본다.
            if (composition == null) return null;
            if (composition.StartsWith(Frontline)) return new[] { "FRONT", "RANGED" };
            if (composition.StartsWith(Flank))     return new[] { "FLANK", "RANGED" };
            if (composition.StartsWith(Backline))  return new[] { "BACK",  "FLANK"  };
            return null;
        }

        /// <summary>선두가 다 떨어진 뒤 채우는 순서.</summary>
        private static string[] RestTags(string composition)
        {
            if (composition == null) return null;
            if (composition.StartsWith(Frontline)) return new[] { "BACK",   "FLANK" };
            if (composition.StartsWith(Flank))     return new[] { "FRONT",  "BACK"  };
            if (composition.StartsWith(Backline))  return new[] { "RANGED", "FRONT" };
            return null;
        }

        /// <summary>그 태그의 자리 번호를 **표에 적힌 순서 그대로** 모은다.</summary>
        private static List<int> IndicesOf(Layout layout, string role)
        {
            var list = new List<int>();
            for (int i = 0; i < layout.Slots.Length; i++)
                if (layout.Slots[i].Role == role) list.Add(i);
            return list;
        }

        /// <summary>
        /// 이 조합으로 자리를 고른 순서. 앞에서부터 적 수만큼 쓴다.
        ///
        /// ⚠ **선두 태그 둘을 번갈아** 가져간다. 하나를 소진하고 다음으로 넘어가면
        ///   CH1 003 웨이브1(3기)에 원거리가 한 명도 안 선다 — 앞줄만 셋이 되고
        ///   조합 이름이 거짓말이 된다.
        ///   한쪽이 떨어지면 남은 쪽으로 계속 채우고, 둘 다 떨어지면 나머지 순서로 넘어간다.
        ///
        /// 같은 태그 안에서는 표의 배열 순서를 지킨다 — 그 순서가 기획자가 정한
        /// 채우는 차례이고, 호스트가 앞쪽에 있는 이유다.
        /// </summary>
        public static int[] OrderFor(Layout layout, string composition)
        {
            int n = layout != null && layout.Slots != null ? layout.Slots.Length : 0;
            var lead = LeadTags(composition);
            var rest = RestTags(composition);

            // 모르는 조합이면 표 순서 그대로. 자리가 뒤죽박죽되지 않는다.
            if (n == 0 || lead == null || rest == null)
            {
                var plain = new int[n];
                for (int i = 0; i < n; i++) plain[i] = i;
                return plain;
            }

            var order = new List<int>(n);

            // ① 선두 둘을 번갈아
            var qa = IndicesOf(layout, lead[0]);
            var qb = IndicesOf(layout, lead[1]);
            int a = 0, b = 0;
            bool turnA = true;
            while (a < qa.Count || b < qb.Count)
            {
                if (turnA) order.Add(a < qa.Count ? qa[a++] : qb[b++]);
                else       order.Add(b < qb.Count ? qb[b++] : qa[a++]);
                turnA = !turnA;
            }

            // ② 나머지는 정해진 순서대로 통째로
            for (int t = 0; t < rest.Length; t++)
            {
                var q = IndicesOf(layout, rest[t]);
                for (int i = 0; i < q.Count; i++) order.Add(q[i]);
            }

            // ③ 어느 태그에도 안 걸린 자리(오타 등)를 마지막에 붙인다 — 빠뜨리지 않는다.
            for (int i = 0; i < n; i++)
                if (!order.Contains(i)) order.Add(i);

            return order.ToArray();
        }

        /// <summary>
        /// 고른 자리에 **빼앗을 몸이 하나도 없으면** 마지막 자리를 첫 호스트 자리로 바꾼다.
        ///
        /// ⚠ 이 규칙이 없으면 CH1 004·008, CH2 004·006 이 뺏을 것 없는 방이 된다.
        ///   방에 들어섰는데 뺏을 게 없으면 그 방은 이 게임의 방이 아니다 —
        ///   몸을 잃은 채 들어가면 손쓸 방법이 아예 없다.
        ///
        /// 마지막 자리를 바꾸는 이유: 앞쪽은 조합이 고른 핵심 자리라 건드리면
        /// 그 방의 성격이 바뀐다. 꼬리 하나만 양보한다.
        /// </summary>
        public static void EnsureHostInPick(Layout layout, int[] order, int count)
        {
            if (layout == null || order == null || count <= 0) return;
            if (count > order.Length) count = order.Length;

            for (int i = 0; i < count; i++)
                if (IsHostUnit(layout.Slots[order[i]].Unit)) return;   // 이미 있다

            // 배열 순서상 첫 호스트 자리를 찾는다.
            int host = -1;
            for (int i = 0; i < layout.Slots.Length; i++)
                if (IsHostUnit(layout.Slots[i].Unit)) { host = i; break; }
            if (host < 0) return;   // 이 레이아웃엔 호스트가 없다(있을 수 없지만)

            // 그 자리가 뒤쪽에 있으면 마지막으로 고른 자리와 맞바꾼다.
            for (int i = count; i < order.Length; i++)
                if (order[i] == host) { order[i] = order[count - 1]; order[count - 1] = host; return; }
        }

        /// <summary>
        /// 엘리트로 세울 자리. 고른 것 중 <c>BACK → FLANK → RANGED → FRONT</c> 순으로 찍는다.
        /// 뒤에 선 놈이 엘리트여야 뚫고 들어가는 값이 생긴다.
        /// </summary>
        public static HashSet<int> PickElites(Layout layout, int[] order, int count, int elite)
        {
            var set = new HashSet<int>();
            if (layout == null || elite <= 0) return set;
            if (count > order.Length) count = order.Length;

            foreach (var role in new[] { "BACK", "FLANK", "RANGED", "FRONT" })
            {
                for (int i = 0; i < count && set.Count < elite; i++)
                    if (layout.Slots[order[i]].Role == role) set.Add(i);
                if (set.Count >= elite) break;
            }
            return set;
        }
    }
}
