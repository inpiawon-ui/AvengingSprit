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
                e.FindPropertyRelative("_template").stringValue = d.Layout ?? string.Empty;
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
            // 회복은 이벤트 방만. 나머지는 방 자체가 보상이다.
            e.FindPropertyRelative("_healPct").intValue = d.Kind == "이벤트" ? 15 : 0;
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

            // ⚠ **보스 방(010)에는 출구가 없다.** `RoomEntry.IsChapterEnd` 가
            //   「출구가 없는 방」으로 챕터의 끝을 판단하기 때문이다.
            //   여기서 다음 챕터 001 로 이어 버리면 한 판에 60방을 내리 돌게 되고
            //   챕터가 영영 안 오른다 — 실제로 그렇게 이어 놨었다.
            //   챕터를 넘기는 것은 방 출구가 아니라 **보스를 잡은 결과**다.
            if (defs[index].No == "010") return;

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

            for (int i = 0; i < list.Length; i++)
            {
                var s = list[i];
                arr.InsertArrayElementAtIndex(i);
                var p = arr.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("_spawnId").stringValue = $"{roomId}_S{i + 1:00}";
                p.FindPropertyRelative("_actorId").stringValue = s.Unit;
                p.FindPropertyRelative("_at").vector2Value = new Vector2(s.X, s.Y);
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
        /// 지형지물. 이미 있는 레이아웃 12종에서 읽는다 — 새 지형을 만들지 않는다.
        /// 보스 방은 비운다: 전용 아레나가 제 자리를 갖고 있어 얹으면 패턴이 걸린다.
        /// </summary>
        private static void WriteObjects(SerializedProperty e, RoomDef60.Room d)
        {
            var objs = e.FindPropertyRelative("_objects");
            objs.ClearArray();
            if (!string.IsNullOrEmpty(d.Boss) || string.IsNullOrEmpty(d.Layout)) return;

            var layout = RoomLayoutTable.Get(d.Layout);
            if (layout?.Objects == null) return;

            // ⚠ 규격·차단·해저드는 `RoomImporterV33.WriteObject` 가 이미 정해 둔 규칙이다.
            //   여기서 다시 정하면 두 임포터가 서로 다른 방을 굽게 된다 — 그것을 그대로 부른다.
            for (int i = 0; i < layout.Objects.Length; i++)
                RoomImporterV33.WriteObjectPublic(objs, i, layout.Objects[i]);
        }
    }
}
