using System;
using System.Collections.Generic;
using Game.Module.InGame;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 60방 배정을 `RoomTable` 에 굽는다 — 6챕터 × 10방.
    ///
    /// ── 왜 새로 만들었나 ────────────────────────────────────────
    /// 예전 `RoomImporterV33` 은 정본 48방을 **챕터당 10방으로 접어** 3챕터 30방을 만들었다.
    /// 접는 규칙(`SelectTen`)은 정본에 없는 우리 발명이었고, 그래서 방 4·7·9 가
    /// 정본에서는 EVENT·REST·SHOP 인데 우리는 COMBAT 으로 덮는 식의 예외가 쌓였다.
    ///
    /// 새 배정표는 **접지 않는다.** 6챕터 × 10방을 자리마다 적어 두었으므로
    /// 그대로 옮기면 된다. 예외가 없으면 규칙도 없다.
    ///
    /// ── 새로 만든 값이 없다 ────────────────────────────────────
    /// 방 종류 · 바닥 · 레이아웃 · 적 자리 · 대장 · 부하는 전부 배정표에서 읽는다.
    /// 지형지물은 이미 있는 `RoomLayoutTable` 의 레이아웃 12종에서 읽는다.
    /// 보스 체력·공격력은 이미 있는 `BossDefTable` 에서 읽는다 —
    /// 그 표가 정본 도면(`AVSR_Bosses12.html`)과 대조를 마친 유일한 자리다.
    /// </summary>
    public static class RoomImporter60
    {
        private const string TablePath = "Assets/BundleResource/TableData/RoomTable.asset";

        /// <summary>방 규격. 한 화면에 들어와야 방을 보고 판단할 수 있다.</summary>
        private const float RoomWidth = 10f;
        private const float RoomHeight = 13f;

        private const int RoomsPerChapter = 10;

        [MenuItem("Tools/Game/60방 임포트 (6챕터 × 10방)")]
        public static void Import()
        {
            var table = AssetDatabase.LoadAssetAtPath<RoomTable>(TablePath);
            if (table == null) { Debug.LogError("[60방] RoomTable 없음: " + TablePath); return; }

            var defs = RoomDef60.All;
            if (defs == null || defs.Length != 60)
            {
                Debug.LogError($"[60방] 배정표가 60방이 아니다: {defs?.Length ?? 0}");
                return;
            }

            var so = new SerializedObject(table);
            so.FindProperty("_contractVersion").stringValue = "rooms60-1.0";
            var rooms = so.FindProperty("_rooms");
            rooms.ClearArray();

            int spawns = 0, bossRooms = 0, midBossRooms = 0, eliteRooms = 0, eventRooms = 0;

            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                string roomId = $"ROOM_CH{d.Ch}_{d.No}";

                rooms.InsertArrayElementAtIndex(i);
                var e = rooms.GetArrayElementAtIndex(i);

                e.FindPropertyRelative("_roomId").stringValue = roomId;
                e.FindPropertyRelative("_chapter").intValue = d.Ch;
                e.FindPropertyRelative("_type").stringValue = TypeOf(d.Kind);
                e.FindPropertyRelative("_route").stringValue = "MAIN";
                e.FindPropertyRelative("_intent").stringValue = d.LayoutKo ?? string.Empty;
                e.FindPropertyRelative("_isChapterStart").boolValue = d.No == "001";
                e.FindPropertyRelative("_handEdited").boolValue = false;
                e.FindPropertyRelative("_width").floatValue = RoomWidth;
                e.FindPropertyRelative("_height").floatValue = RoomHeight;
                e.FindPropertyRelative("_cameraMode").stringValue = "VERTICAL_FOLLOW";
                e.FindPropertyRelative("_template").stringValue = LayoutOf(d) ?? string.Empty;
                e.FindPropertyRelative("_unlockRule").stringValue = "ROOM_CLEAR";
                // 바닥은 배정표가 정한다. 코드가 챕터로 유추하면 CH4~6 이 어긋난다.
                e.FindPropertyRelative("_floor").stringValue = d.Floor ?? string.Empty;

                // 들어오는 자리는 아래, 나가는 자리는 위. 세로로 올라가는 방이다.
                e.FindPropertyRelative("_entry").vector2Value = new Vector2(RoomWidth * 0.5f, 0.8f);
                e.FindPropertyRelative("_playerSpawn").vector2Value = new Vector2(RoomWidth * 0.5f, 1.7f);

                WriteReward(e, d);
                WriteExit(e, i, defs);

                // ── 보스 방 ───────────────────────────────────────
                var bossProp = e.FindPropertyRelative("_bossId");
                if (!string.IsNullOrEmpty(d.Boss))
                {
                    bossProp.stringValue = BossIdOf(d.Boss);
                    WriteBoss(e, d.Boss);
                    bossRooms++;
                }
                else bossProp.stringValue = string.Empty;

                // ── 중간 보스 방 ──────────────────────────────────
                //
                // ⚠ 대장은 `_bossId` 를 쓰지 않는다. 그것을 켜면 `IsBoss` 가 참이 되어
                //   전용 아레나 바닥으로 빠지는데, 이 방은 배정표가 정한 바닥
                //   (`roomfloor_env_holding`)을 써야 한다.
                e.FindPropertyRelative("_midBossKey").stringValue = d.Captain ?? string.Empty;
                if (!string.IsNullOrEmpty(d.Captain)) midBossRooms++;

                e.FindPropertyRelative("_eventPool").stringValue = d.Pool ?? string.Empty;
                if (!string.IsNullOrEmpty(d.Pool)) eventRooms++;

                spawns += WriteSpawns(e, d, roomId, ref eliteRooms);
                WriteObjects(e, d);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            Debug.Log($"[60방] 방 {defs.Length} · 스폰 {spawns} — "
                      + $"보스 {bossRooms} · 중간보스 {midBossRooms} · 엘리트 {eliteRooms} · 이벤트 {eventRooms}");
        }

        /// <summary>배정표의 한글 종류 → 방 타입 문자열. `KindOfCanon` 이 이 값을 읽는다.</summary>
        private static string TypeOf(string kind) => kind switch
        {
            "보스"     => "BOSS",
            "중간보스" => "MIDBOSS",
            "엘리트"   => "ELITE",
            "이벤트"   => "EVENT",
            "회복"     => "REST",
            "상점"     => "SHOP",
            _          => "COMBAT",
        };

        /// <summary>우리 슬러그 → 정본 보스 ID. `BossSlug` 의 역방향이다.</summary>
        private static string BossIdOf(string slug) => slug switch
        {
            "crusher"      => "B01",
            "guardian"     => "B02",
            "kingpin"      => "B03",
            "python"       => "B04",
            "robot_snakes" => "B05",
            "sludge"       => "B06",
            _              => string.Empty,
        };

        /// <summary>
        /// 보스 체력·공격력·이동속도·페이즈를 `BossDefTable` 에서 옮긴다.
        ///
        /// ⚠ 여기서 숫자를 짓지 않는다. 그 표가 정본 도면과 대조를 마친 유일한 자리이고,
        ///   두 곳에 적으면 한쪽이 반드시 낡는다.
        /// </summary>
        private static void WriteBoss(SerializedProperty e, string slug)
        {
            BossDefTable.Boss def = null;
            foreach (var b in BossDefTable.All)
                if (b.Key == slug) { def = b; break; }
            if (def == null) { Debug.LogWarning($"[60방] BossDefTable 에 없는 보스: {slug}"); return; }

            e.FindPropertyRelative("_bossName").stringValue = def.NameEn;
            e.FindPropertyRelative("_bossHp").intValue = def.Hp;
            e.FindPropertyRelative("_bossAtk").intValue = def.Atk;
            e.FindPropertyRelative("_bossMoveSpeed").floatValue = 2.2f;
            // 방 가운데 위쪽. 플레이어는 아래에서 들어온다.
            e.FindPropertyRelative("_bossAt").vector2Value = new Vector2(RoomWidth * 0.5f, RoomHeight * 0.72f);

            // 페이즈 문턱. 정본 60% / 30% — `BossBrain` 이 1.0 을 걸러 낸다.
            var gates = e.FindPropertyRelative("_bossPhaseGates");
            gates.ClearArray();
            float[] g = { 1f, 0.6f, 0.3f };
            for (int i = 0; i < g.Length; i++)
            {
                gates.InsertArrayElementAtIndex(i);
                gates.GetArrayElementAtIndex(i).floatValue = g[i];
            }

            // 페이즈별 예고 시간. 패턴이 제 값을 들고 있으면 그것이 이기므로
            // 여기 값은 도형이 없는 패턴의 대비책이다.
            var phases = e.FindPropertyRelative("_bossPhases");
            phases.ClearArray();
            for (int i = 0; i < 3; i++)
            {
                phases.InsertArrayElementAtIndex(i);
                var p = phases.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("_phase").intValue = i + 1;
                p.FindPropertyRelative("_hpStart").floatValue = g[i];
                p.FindPropertyRelative("_pattern").stringValue = $"P{i + 1}";
                p.FindPropertyRelative("_telegraphSeconds").floatValue = 1.2f;
            }

            // ⚠ 방에는 공격 목록을 굽지 않는다. `BossTable` 이 보스 키로 들고 있고,
            //   60방 체계에서는 챕터마다 최종 보스가 하나뿐이라 헷갈릴 여지가 없다.
            //   (중간 보스는 제 액티브 스킬을 쓰므로 목록이 필요 없다.)
            e.FindPropertyRelative("_bossMoves").ClearArray();
        }

        /// <summary>
        /// 방 보상. 배정표에 값이 없으므로 **방 종류로 정한다** —
        /// 챕터가 깊어질수록 오른다. 새 수치를 지어내는 대신 규칙 하나로 만든다.
        /// </summary>
        private static void WriteReward(SerializedProperty e, RoomDef60.Room d)
        {
            int gold = d.Kind switch
            {
                "보스"     => 120,
                "중간보스" => 80,
                "엘리트"   => 60,
                "이벤트"   => 0,
                "회복"     => 0,
                "상점"     => 0,
                _          => 24,
            };
            int exp = d.Kind switch
            {
                "보스"     => 200,
                "중간보스" => 130,
                "엘리트"   => 90,
                "이벤트"   => 0,
                _          => 40,
            };
            float chapterMul = 1f + (d.Ch - 1) * 0.35f;

            e.FindPropertyRelative("_gold").intValue = Mathf.RoundToInt(gold * chapterMul);
            e.FindPropertyRelative("_exp").intValue = Mathf.RoundToInt(exp * chapterMul);
            // ⚠ 회복은 **어느 방에도 얹지 않는다.**
            //
            //   예전에는 `이벤트` 에 15 를 줬다. 그때 원본의 `이벤트` 는 004 였고
            //   004 는 실물에서 전투방으로 돌려세워져 있었다 — 그 15 는 「회복 방을
            //   전투방으로 바꾼 대가」였지 이벤트의 몫이 아니었다.
            //
            //   이제 004 가 **진짜 이벤트 방**이 된다. 그런데 `OnRoomCleared` 는
            //   「적 0 · 문 아직 안 열림」이면 도는 자리라 이벤트 방에서도 돈다 —
            //   즉 이벤트 보상 위에 15% 가 한 번 더 얹힌다. 중복이다.
            //
            //   돌려받을 자리는 따로 생겼다. 004 는 고스트 체력이 40% 아래면
            //   회복 제단(REST)으로 갈린다(`BattleDirector.KindOfCanon`).
            e.FindPropertyRelative("_healPct").intValue = 0;
        }

        /// <summary>
        /// 출구. 같은 챕터 안에서는 다음 번호로, 010 은 다음 챕터 001 로 간다.
        /// 마지막 방(CH6 010)은 출구가 없다 — 거기가 끝이다.
        /// </summary>
        private static void WriteExit(SerializedProperty e, int index, RoomDef60.Room[] defs)
        {
            var exits = e.FindPropertyRelative("_exits");
            exits.ClearArray();
            if (index + 1 >= defs.Length) return;

            // ⚠ **보스 방(010)도 다음 챕터 001 로 잇는다**(기획 2026-09-08 —
            //   "챕터 1-10 을 깨면 챕터 2-1 로 연결").
            //
            //   예전에는 여기서 끊었다. `RoomEntry.IsChapterEnd` 가 「출구가 없는 방」
            //   으로 챕터의 끝을 판단하므로, 끊으면 보스 방에서 판이 끝났다.
            //   그러면 한 판 = 한 챕터인데 **정상 플레이로 챕터를 올리는 길이 없어서**
            //   1챕터 열 방만 무한히 반복하게 된다(실측).
            //
            //   이제 끝은 표의 마지막 방(CH6 010) 하나뿐이다 — 위 `index + 1` 검사가
            //   그것을 걸러 낸다. 한 판이 60방을 이어서 간다.

            var next = defs[index + 1];
            exits.InsertArrayElementAtIndex(0);
            var x = exits.GetArrayElementAtIndex(0);
            x.FindPropertyRelative("_exitId").stringValue = "EXIT_MAIN";
            x.FindPropertyRelative("_nextRoomId").stringValue = $"ROOM_CH{next.Ch}_{next.No}";
            x.FindPropertyRelative("_at").vector2Value = new Vector2(RoomWidth * 0.5f, RoomHeight - 0.6f);
        }

        private static int WriteSpawns(SerializedProperty e, RoomDef60.Room d,
                                       string roomId, ref int eliteRooms)
        {
            var arr = e.FindPropertyRelative("_spawns");
            arr.ClearArray();

            // 중간 보스 방은 배정표에 자리가 없다. 부하 셋을 대장 앞에 세운다 —
            // 대장은 `SpawnMidBoss` 가 방 위쪽에 따로 세운다.
            var list = d.Spawns;
            if (list == null || list.Length == 0)
            {
                if (string.IsNullOrEmpty(d.Captain)) return 0;
                return WriteMinions(arr, d, roomId);
            }

            bool elite = d.Kind == "엘리트";
            if (elite) eliteRooms++;
            bool flipRoom = MirrorRoom(roomId);

            for (int i = 0; i < list.Length; i++)
            {
                var s = list[i];
                arr.InsertArrayElementAtIndex(i);
                var p = arr.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("_spawnId").stringValue = $"{roomId}_S{i + 1:00}";
                p.FindPropertyRelative("_actorId").stringValue = s.Unit;
                // 지형지물과 **같은 방향으로** 뒤집는다 (`MirrorRoom` 주석 참조)
                p.FindPropertyRelative("_at").vector2Value =
                    PushOffEntrance(new Vector2(flipRoom ? MirrorX(s.X) : s.X, s.Y));
                p.FindPropertyRelative("_facing").stringValue = "S";
                p.FindPropertyRelative("_delaySeconds").floatValue = 0f;
                // ⚠ 빙의 대상 표시는 **`_trigger` 다.** `RoomEntry.IsPossessionTarget` 이
                //   `_trigger == "POSSESSION_TARGET"` 으로 판단한다 — 따로 불린이 없다.
                p.FindPropertyRelative("_trigger").stringValue =
                    s.IsHost ? "POSSESSION_TARGET" : "ROOM_ENTER";
                p.FindPropertyRelative("_telegraph").stringValue = string.Empty;
                // ⚠ 엘리트는 **빼앗을 수 있다**(배정표 ELITE_RULE). 중간 보스와 다르다.
                //   그 방의 몸 자리를 엘리트로 세운다 — 크고 아픈 몸이 곧 보상이다.
                p.FindPropertyRelative("_elite").boolValue = elite && s.IsHost;
            }
            return list.Length;
        }

        /// <summary>
        /// 중간 보스 부하. 배정표는 `minionFrom` 으로 **후보만** 준다 —
        /// 자리는 대장 앞 반원으로 우리가 정한다.
        /// </summary>
        private static int WriteMinions(SerializedProperty arr, RoomDef60.Room d, string roomId)
        {
            var from = d.MinionFrom;
            int n = Mathf.Max(1, d.Minions);
            if (from == null || from.Length == 0) return 0;

            // 대장이 y=0.78 쯤(방 위쪽)에 서므로 부하는 그 아래 가로로 벌린다.
            float[] xs = { RoomWidth * 0.28f, RoomWidth * 0.5f, RoomWidth * 0.72f };
            float y = RoomHeight * 0.55f;

            for (int i = 0; i < n; i++)
            {
                arr.InsertArrayElementAtIndex(i);
                var p = arr.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("_spawnId").stringValue = $"{roomId}_M{i + 1:00}";
                p.FindPropertyRelative("_actorId").stringValue = from[i % from.Length];
                p.FindPropertyRelative("_at").vector2Value = new Vector2(xs[i % xs.Length], y);
                p.FindPropertyRelative("_facing").stringValue = "S";
                p.FindPropertyRelative("_delaySeconds").floatValue = 0f;
                // 부하는 **전부 빼앗을 수 있다.** 이 방의 전투가 그것으로 성립한다.
                p.FindPropertyRelative("_trigger").stringValue = "POSSESSION_TARGET";
                p.FindPropertyRelative("_telegraph").stringValue = string.Empty;
                p.FindPropertyRelative("_elite").boolValue = false;
            }
            return n;
        }

        /// <summary>
        /// 이 방이 쓸 레이아웃 글자.
        ///
        /// 굽는 원본(`RoomDef60`)은 레이아웃 12종만 알던 시절의 배정을 들고 있다.
        /// 그 파일은 도구가 다시 구우므로 여기서 **덮어쓴다** — 원본을 고치면
        /// 다음 납품에 조용히 사라진다.
        ///
        /// ── 왜 챕터마다 다르게 주는가 ─────────────────────────────
        /// 위험물을 60방에 골고루 흩뿌리면 1챕터부터 가시를 밟는다. 챕터가
        /// **무엇을 새로 배우는 자리**인지가 배치로 보여야 한다.
        ///
        ///   CH1  튜토리얼 — 위험물 0. 기둥·상자·엄폐물만
        ///   CH2  가시(TIMED_SPIKE) 등장
        ///   CH3  십자 포탑이 서기 시작 · **도랑을 처음 만난다**(M 좁은 목)
        ///   CH4  도랑이 길을 가른다(N 두 갈래) · 가시가 늘어난다(H)
        ///   CH5  양쪽 도랑 가운데 길(O) · 섬과 톱니(Q) · 네 귀퉁이 가시(L)
        ///   CH6  지그재그 물길(P) · 톱니(I) · 용광로 회랑(R) · 섬(Q)
        ///
        /// ⚠ 005 는 여섯 챕터 모두 중간보스 방이라 늘 F(정적)다.
        ///   보스급이 주인공인 자리에서 바닥까지 시끄러우면 패턴이 안 읽힌다.
        /// </summary>
        private static string LayoutOf(RoomDef60.Room d)
        {
            if (!string.IsNullOrEmpty(d.Boss)) return string.Empty;   // 보스는 전용 아레나
            int ch = Mathf.Clamp(d.Ch, 1, 6);
            return (ch, d.No) switch
            {
                (1, "001") => "A", (1, "002") => "B", (1, "003") => "E",
                (1, "005") => "F", (1, "006") => "J", (1, "008") => "D", (1, "009") => "G",

                (2, "001") => "B", (2, "002") => "E", (2, "003") => "J",
                (2, "005") => "F", (2, "006") => "D", (2, "008") => "C", (2, "009") => "K",

                (3, "001") => "E", (3, "002") => "J", (3, "003") => "D",
                (3, "005") => "F", (3, "006") => "C", (3, "008") => "K", (3, "009") => "M",

                (4, "001") => "J", (4, "002") => "D", (4, "003") => "C",
                (4, "005") => "F", (4, "006") => "K", (4, "008") => "N", (4, "009") => "H",

                (5, "001") => "D", (5, "002") => "C", (5, "003") => "K",
                (5, "005") => "F", (5, "006") => "O", (5, "008") => "Q", (5, "009") => "L",

                (6, "001") => "C", (6, "002") => "K", (6, "003") => "P",
                (6, "005") => "F", (6, "006") => "I", (6, "008") => "R", (6, "009") => "Q",

                _ => d.Layout,   // 회복(004)·상점(007) 은 원본대로 — 비어 있다
            };
        }

        /// <summary>
        /// 지형지물. 이미 있는 레이아웃 12종에서 읽는다 — 새 지형을 만들지 않는다.
        /// 보스 방은 비운다: 전용 아레나가 제 자리를 갖고 있어 얹으면 패턴이 걸린다.
        /// </summary>
        /// <summary>
        /// 이 방을 좌우로 뒤집어 놓는가.
        ///
        /// 60방이 레이아웃 18종을 돌려쓴다. 같은 글자를 받은 방은 지형이 **한 픽셀도
        /// 다르지 않아서**, 다른 챕터인데 같은 방을 또 걷는 것처럼 보였다 —
        /// CH1 008 과 CH2 006 이 그랬다. 방 이름으로 좌우를 뒤집으면 종류가 두 배가 된다.
        ///
        /// ⚠ **지형지물과 적 자리를 함께 뒤집는다.** 한쪽만 뒤집으면 물건이 적 위에 얹힌다 —
        ///   빈 자리를 찾아 앉혀 둔 여유가 통째로 무너진다.
        /// </summary>
        private static bool MirrorRoom(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return false;
            int h = 0;
            for (int i = 0; i < roomId.Length; i++) h = h * 31 + roomId[i];
            return (h & 1) != 0;
        }

        /// <summary>방 폭 10 m 를 기준으로 뒤집은 x.</summary>
        private static float MirrorX(float x) => RoomWidth - x;

        /// <summary>발자국 반지름(미터). `RoomImporterV33.PropSize` 와 같아야 한다.</summary>
        private static Vector2 FootHalf(string kind) => kind switch
        {
            "PILLAR" => new Vector2(0.5f, 0.5f),
            "CRATE" => new Vector2(1f, 0.5f),
            "LOW_COVER" or "BARRICADE" => new Vector2(1.5f, 0.5f),
            _ => new Vector2(1f, 1f),   // BULK · TIMED_SPIKE · ROTATING_BLADE · CHANNEL
        };

        /// <summary>바닥에 눕는 것은 뒤에 선 적을 안 가린다 — 자리를 다퉈도 된다.</summary>
        private static bool LiesFlat(string kind)
            => kind == "TIMED_SPIKE" || kind == "CHANNEL_H" || kind == "CHANNEL_V";

        /// <summary>
        /// 이 자리에 앉혀도 방 안에 들어오고 **적을 가리지 않는가.**
        ///
        /// 적 자리와 0.8 m 를 띄운다. 이 여유가 없으면 물건이 적 바로 앞에 서서
        /// 적이 화면에서 사라진다 — CH1 008 · CH2 006 · CH2 008 이 그랬다.
        /// 바닥에 눕는 것(도랑·가시)은 뒤를 안 가리므로 겹침만 본다.
        /// </summary>
        private static bool FitsOne(string kind, float x, float y, List<Vector2> taken)
        {
            var half = FootHalf(kind);
            if (x - half.x < 0.4f || x + half.x > RoomWidth - 0.4f) return false;

            float clear = LiesFlat(kind) ? 0.2f : 0.8f;
            for (int k = 0; k < taken.Count; k++)
                if (Mathf.Abs(taken[k].x - x) < half.x + clear
                    && Mathf.Abs(taken[k].y - y) < half.y + clear) return false;
            return true;
        }

        /// <summary>플레이어가 들어서는 자리. `_playerSpawn` 과 같아야 한다.</summary>
        private static readonly Vector2 EntranceAt = new Vector2(RoomWidth * 0.5f, 1.7f);

        /// <summary>입구에서 이만큼(m)은 비워 둔다.</summary>
        private const float EntranceClearMeters = 4.5f;

        /// <summary>
        /// 입구 코앞에 선 적을 위로 밀어낸다.
        ///
        /// 배정표에는 `(5, 1.5)` 처럼 **플레이어가 서는 자리(5, 1.7)** 와 겹치는 자리가 있다.
        /// 방에 들어서자마자 몸이 겹친 채로 맞기 시작하면, 무엇이 있었는지 보기도 전에
        /// 체력이 깎인다 — 대응할 방법이 없는 피해는 난이도가 아니라 고장이다.
        ///
        /// 좌우는 그대로 두고 **위로만** 민다. 좌우로 밀면 배치의 모양(가운데·측면)이
        /// 무너지는데, 위로 미는 것은 「조금 멀리 세운다」로 끝난다.
        /// </summary>
        private static Vector2 PushOffEntrance(Vector2 at)
        {
            float dy = at.y - EntranceAt.y;
            float dx = at.x - EntranceAt.x;
            if (dx * dx + dy * dy >= EntranceClearMeters * EntranceClearMeters) return at;

            // 입구에서 같은 x 로 EntranceClear 만큼 떨어진 y. 좌우로 벌어진 만큼은 덜 민다.
            float need = Mathf.Sqrt(Mathf.Max(0f,
                EntranceClearMeters * EntranceClearMeters - dx * dx));
            float y = Mathf.Min(EntranceAt.y + need, RoomHeight - 2.0f);
            return new Vector2(at.x, Mathf.Max(at.y, y));
        }

        private static void WriteObjects(SerializedProperty e, RoomDef60.Room d)
        {
            var objs = e.FindPropertyRelative("_objects");
            objs.ClearArray();
            if (!string.IsNullOrEmpty(d.Boss) || string.IsNullOrEmpty(d.Layout)) return;

            var layout = RoomLayoutTable.Get(LayoutOf(d));
            if (layout?.Objects == null) return;

            string roomId = $"ROOM_CH{d.Ch}_{d.No}";
            bool flip = MirrorRoom(roomId);

            // ⚠ **표에 이미 적힌 적 자리를 읽는다.** 배정표(`d.Spawns`)를 보면 안 된다 —
            //   중간보스 부하(`WriteMinions`)처럼 임포터가 따로 만들어 넣는 자리가 빠져 있어,
            //   검사를 통과하고도 물건이 적 위에 앉았다. `WriteObjects` 는 `WriteSpawns`
            //   다음에 도므로 이 시점에는 자리가 다 적혀 있다.
            var taken = new List<Vector2>();
            var written = e.FindPropertyRelative("_spawns");
            for (int i = 0; i < written.arraySize; i++)
                taken.Add(written.GetArrayElementAtIndex(i)
                                 .FindPropertyRelative("_at").vector2Value);

            // 같은 글자를 받은 방이 **한 픽셀도 다르지 않게** 나오던 것을 여기서 흩는다.
            // 좌우 뒤집기만으로는 두 배뿐이라, 가로로 미는 폭까지 방 이름으로 고른다.
            int seed = 0;
            for (int i = 0; i < roomId.Length; i++) seed = seed * 31 + roomId[i];
            var shifts = new[] { 0f, 1f, -1f, 2f, -2f, 3f, -3f };

            // ⚠ **물건마다 따로 앉힌다.** 예전에는 「셋이 전부 맞는 밀기」를 찾았는데,
            //   적이 방마다 예닐곱씩 서므로 그런 밀기가 거의 없어 방이 통째로 비었다.
            //   하나씩 자리를 찾고, 끝내 못 앉히는 것만 뺀다 — 적을 가리느니 빼는 게 낫다.
            int put = 0;
            for (int i = 0; i < layout.Objects.Length; i++)
            {
                var o = layout.Objects[i];
                float baseX = flip ? MirrorX(o.At.x) : o.At.x;
                bool placed = false;
                for (int k = 0; k < shifts.Length; k++)
                {
                    float dx = shifts[(Mathf.Abs(seed / 2) + i + k) % shifts.Length];
                    if (!FitsOne(o.Kind, baseX + dx, o.At.y, taken)) continue;
                    o.At = new Vector2(baseX + dx, o.At.y);
                    placed = true;
                    break;
                }
                if (!placed) continue;

                // 앉힌 것도 자리를 차지한다 — 다음 물건이 그 위에 겹치지 않게.
                taken.Add(o.At);
                RoomImporterV33.WriteObjectPublic(objs, put++, o);
            }
        }
    }
}
