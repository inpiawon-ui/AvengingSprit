using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Game.Character;
using Game.Module.InGame;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 정본 v3.3 버티컬 슬라이스의 방 48개를 `RoomTable` 에 굽는다.
    ///
    /// ── 좌표 변환 ────────────────────────────────────────────
    /// 정본 방은 폭 24~32 m 인데 우리는 **전 방 8 m 고정**이다 (2026-08-19 결정).
    /// 세로 9:16 화면에 24 m 는 안 들어가고, 방마다 폭이 다르면 픽셀/미터가 달라져
    /// 캐릭터 크기가 방마다 변한다. 폭만 1/4 로 줄이고 높이는 정본 그대로 둔다.
    ///
    /// 정본 스폰 좌표는 **방 중심 기준 상대값**이다 (X ±8.5 · Y ±3.6).
    /// 폭을 1/4 로 줄였으니 X 도 1/4 로 줄여야 방 안에 들어온다. Y 는 그대로다.
    ///
    ///     x(m) = 4 + X/4          왼쪽 0, 오른쪽 8
    ///     y(m) = 높이/2 + Y       아래 0, 위 높이
    /// </summary>
    public static class RoomImporterV33
    {
        private const string Dir = "Projects/AVSR/Canon/v23";
        private const string TablePath = "Assets/BundleResource/TableData/RoomTable.asset";

        /// <summary>
        /// 우리 방 폭(미터). 8 → 15 → **10**. `BattleDirector.RoomMeterWidth` 와 같아야 한다.
        ///
        /// 15 는 방이 화면보다 넓어져 가로 스크롤이 생겼다. 오른쪽 절반을 찾아다녀야 해서
        /// 한눈에 안 들어왔다. 10 은 **방 폭 = 화면 폭**이라 가로 스크롤이 사라진다.
        /// </summary>
        private const float RoomWidth = 10f;

        /// <summary>
        /// 정본 X 를 우리 방에 옮길 때의 축척. 정본 |X| 최대가 8.5 이고 우리 반폭이 5.0,
        /// 가장자리 여유 0.9 를 빼면 4.1 이라 4.1/8.5 ≈ 0.48 이다.
        /// 폭을 화면에 맞추면 정본 X 를 **절반으로 눌러야 한다** — 이건 가로 스크롤을
        /// 없애는 대가다. 눌린 결과는 방 지형 편집기의 검사에 그대로 걸리므로,
        /// 겹치거나 막히는 자리는 손으로 고칠 수 있다.
        /// </summary>
        private const float SpawnXScale = 0.48f;

        /// <summary>
        /// 방 세로(미터). **폭처럼 전 방 고정**이다.
        ///
        /// 정본은 14~26 m 를 주고 거기에 증원 칸까지 붙여 28 m 짜리 방도 있었다.
        /// 화면에 보이는 높이가 약 11 m(800px ÷ 72)라 방 하나가 두세 화면이었고,
        /// 그러면 **방이 한눈에 안 들어온다** — 어디에 뭐가 있는지 모르는 채로 올라가게 된다.
        ///
        /// 한 화면보다 조금만 크게 잡는다. 조금 올라가면 방 전체가 보이는 크기다.
        /// </summary>
        private const float RoomHeight = 13f;

        /// <summary>
        /// 방 가장자리 여백(미터). 지형·적을 여기에 놓지 않는다.
        ///
        /// 경기장 테두리는 **배경 리소스**가 그린다. 코드가 블록을 쌓아 만들지 않는다 —
        /// 블록은 발자국 위로 솟는 그림이라 세로로 쌓으면 벽이 아니라 줄무늬 띠가 된다.
        /// 여기서는 그 배경이 들어올 자리만 비워 둔다.
        /// </summary>
        private const float EdgeMeters = 1f;

        /// <summary>위쪽 문이 서는 구역(미터). 여기에도 지형·적을 놓지 않는다.</summary>
        private const float GateBandMeters = 2.5f;

        /// <summary>적 EN_xx → 우리 슬러그. 정본이 적과 호스트를 1:1 로 묶었다.</summary>
        private static readonly string[] EnemySlug =
        {
            "",                  // 0 자리맞춤
            "amazon", "amazon_elite", "commando_grenade", "commando_laser", "commando_mg",
            "commando_missile", "dragon_blue", "salamander", "dragoon", "gangster",
            "thug", "guru", "hopper", "hopper_smg", "medium",
            "white_wizard", "ninja_chain", "ninja", "robot", "baseball",
            "snowwoman", "vampire", "death",
        };

        [MenuItem("Tools/Game/Canon v3.3 임포트 (방 48개)")]
        public static void Import()
        {
            var master = Load("ROOM_MASTER.json");
            if (master == null) return;

            var geometry = Index(Load("ROOM_GEOMETRY.json"), "GeometryID");
            var route    = Index(Load("ROOM_ROUTE.json"), "RoomID");
            var boss     = Index(Load("BOSS_RUNTIME.json"), "BossID");
            var attacks  = Group(Load("BOSS_ATTACK_RUNTIME.json"), "BossID");
            var reward   = Index(Load("ROOM_REWARD.json"), "RoomID");
            var spawnBy  = Group(Load("ROOM_SPAWN.json"), "RoomID");

            var table = AssetDatabase.LoadAssetAtPath<RoomTable>(TablePath);
            if (table == null) { Debug.LogError("[RoomV33] RoomTable 없음"); return; }

            var so = new SerializedObject(table);
            so.FindProperty("_contractVersion").stringValue = "3.3.0";
            var rooms = so.FindProperty("_rooms");
            rooms.ClearArray();     // 34방 체계와 ID 가 안 겹친다 — 통째로 간다

            // 챕터별 전투방 평균 보상. 돌려세운 방을 채울 때 쓴다.
            var combatGold = AverageReward(master, reward, "Gold");
            var combatExp  = AverageReward(master, reward, "EXP");

            // ⚠ **정본 48방을 챕터당 10방으로 접는다.** 아래 `SelectTen` 참조.
            var picked = SelectTen(master);
            var newIdOf = new Dictionary<string, string>(picked.Count);
            for (int i = 0; i < picked.Count; i++)
                newIdOf[S(picked[i], "RoomID")] = NewRoomId(picked[i], i, picked);

            int spawnCount = 0;
            for (int i = 0; i < picked.Count; i++)
            {
                var m = picked[i];
                string canonId = S(m, "RoomID");
                string roomId = newIdOf[canonId];
                // 다음 방은 **고른 목록의 다음 칸**이다. 정본 연결을 그대로 쓰면
                // 버린 방을 가리켜 문이 아무 데도 안 간다.
                string nextId = NextOf(picked, newIdOf, i);
                var geo = geometry.TryGetValue(S(m, "GeometryID"), out var g) ? g : null;
                float baseH = geo != null ? F(geo, "Height") : 14f;

                // ⚠ 증원(웨이브 2)은 **소환하지 않는다.** 방을 다 비운 순간 여섯 기가
                //   허공에서 튀어나오면 대응할 방법이 없다(2026-08-20 지적).
                //   대신 방을 세로로 한 칸 더 만들고 **처음부터 거기 세워 둔다.**
                //   위로 올라가다 만나는 것이라, 보고 판단하고 준비할 수 있다.
                // 증원 칸을 더 붙이지 않는다. 방은 전부 같은 크기다 —
                // 한 화면에 들어와야 방을 보고 판단할 수 있다.
                float height = RoomHeight;

                // 손으로 짠 레이아웃이 이 방의 형태와 적 자리를 둘 다 갖는다. 보스방만 null 이다.
                // ⚠ 보상 계산이 `roomType` 을 보므로 **여기서 먼저** 정한다.
                int chapterNo = ChapterNo(S(m, "ChapterID"));
                bool isBossRoom = !string.IsNullOrEmpty(S(m, "BossID"));
                var plan = RoomLayoutTable.PlanFor(chapterNo, RoomSeqOf(roomId));
                // ⚠ **배정표가 정본보다 우선한다** (v1.1). 방 종류·적 수·조합을 거기서 읽는다.
                //   방 4·7·9 는 정본이 EVENT·REST·SHOP 이지만 COMBAT 으로 덮는다.
                string roomType = plan != null ? plan.Type : S(m, "RoomType");
                var layout = RoomLayoutTable.For(chapterNo, RoomSeqOf(roomId), isBossRoom);

                rooms.InsertArrayElementAtIndex(i);
                var e = rooms.GetArrayElementAtIndex(i);

                e.FindPropertyRelative("_roomId").stringValue = roomId;
                e.FindPropertyRelative("_chapter").intValue = chapterNo;
                e.FindPropertyRelative("_type").stringValue = roomType;
                e.FindPropertyRelative("_route").stringValue = S(m, "RouteGroup");
                e.FindPropertyRelative("_intent").stringValue = S(m, "Notes");
                e.FindPropertyRelative("_isChapterStart").boolValue = I(m, "SequenceDepth") == 1;
                e.FindPropertyRelative("_width").floatValue = RoomWidth;
                e.FindPropertyRelative("_height").floatValue = height;
                e.FindPropertyRelative("_cameraMode").stringValue = "VERTICAL_FOLLOW";
                e.FindPropertyRelative("_template").stringValue = geo != null ? S(geo, "Template") : string.Empty;
                e.FindPropertyRelative("_unlockRule").stringValue = "ROOM_CLEAR";

                // 정본 ROOM_REWARD — 방마다 붙는 골드·경험치·회복
                //
                // ⚠ 옛 REST·SHOP 방은 정본에서 **골드·경험치가 0** 이다. 싸울 일이 없는
                //   자리였으니 당연했는데, 이제 전투방으로 돌려세웠으므로 그대로 두면
                //   적 일곱을 잡고 아무것도 못 받는 방이 된다.
                //   그 챕터 전투방 평균으로 채운다 — 새 값을 지어내지 않는다.
                var rw = reward.TryGetValue(roomId, out var r) ? r : null;
                int gold = rw != null ? I(rw, "Gold") : 0;
                int exp  = rw != null ? I(rw, "EXP") : 0;
                if (S(m, "RoomType") != roomType)   // 배정표가 돌려세운 방(구 EVENT·REST·SHOP)
                {
                    if (gold <= 0) gold = combatGold.TryGetValue(chapterNo, out var cg) ? cg : 20;
                    if (exp  <= 0) exp  = combatExp.TryGetValue(chapterNo, out var cx) ? cx : 42;
                }
                e.FindPropertyRelative("_gold").intValue = gold;
                e.FindPropertyRelative("_exp").intValue = exp;
                e.FindPropertyRelative("_healPct").intValue = rw != null ? I(rw, "HealPct") : 0;

                // 들어오는 자리는 아래, 나가는 자리는 위. 세로로 올라가는 방이다.
                e.FindPropertyRelative("_entry").vector2Value = new Vector2(RoomWidth * 0.5f, 0.8f);
                e.FindPropertyRelative("_playerSpawn").vector2Value = new Vector2(RoomWidth * 0.5f, 1.7f);

                WriteExits(e, roomId, nextId, canonId, height, route);
                WriteBoss(e, m, boss, height);
                WriteBossMoves(e, S(m, "BossID"), attacks);
                // ⚠ 보스방은 정본 좌표를 그대로 쓴다. 전용 아레나가 이미 자리를 갖고 있어
                //   레이아웃을 얹으면 보스 패턴이 지형에 걸린다.
                spawnCount += isBossRoom
                    ? WriteCanonSpawns(e, spawnBy, roomId, baseH, geo)
                    : WriteSpawns(e, layout, plan, roomId);
                // 손으로 배치한 방은 지형지물을 건드리지 않는다 — 맵툴 작업이 날아간다.
                if (!e.FindPropertyRelative("_handEdited").boolValue)
                    WriteObjects(e, roomId, layout, isBossRoom);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            Debug.Log($"[RoomV33] 방 {master.Count}개 · 스폰 {spawnCount}개 — 폭 {RoomWidth}m 고정");
        }

        // ── 출구 ──────────────────────────────────────────────────
        /// <summary>
        /// 정본 BOSS_ATTACK_RUNTIME 4줄을 그 방 보스의 공격 목록으로 굽는다.
        ///
        /// 정본은 이름과 대응법(`Counterplay`)만 준다 — 우리가 가진 8가지 거동 중
        /// 무엇으로 옮길지는 **대응법을 보고** 정한다. "옆으로 피해라"는 돌진이고,
        /// "차선을 바꿔라"는 번갈아 솟는 레이저다. 이름만 보면 다 그럴듯해서
        /// 실제로 피하는 방법이 달라지지 않는다.
        /// </summary>
        private static void WriteBossMoves(SerializedProperty e, string bossId,
                                           Dictionary<string, List<Dictionary<string, string>>> attacks)
        {
            var moves = e.FindPropertyRelative("_bossMoves");
            moves.ClearArray();
            if (string.IsNullOrEmpty(bossId) || !attacks.TryGetValue(bossId, out var rows)) return;

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                moves.InsertArrayElementAtIndex(i);
                var m = moves.GetArrayElementAtIndex(i);

                m.FindPropertyRelative("_pattern").enumValueIndex = (int)PatternOf(S(r, "AttackID"));
                m.FindPropertyRelative("_fromPhase").intValue = PhaseOf(S(r, "Phase"));
                m.FindPropertyRelative("_cooldown").floatValue = F(r, "CooldownSec");
                m.FindPropertyRelative("_shotCount").intValue = ShotsOf(S(r, "AttackID"));
                m.FindPropertyRelative("_spreadDegrees").floatValue = SpreadOf(S(r, "AttackID"));
                // 피해는 방의 보스 공격력 대비 배율로 넣는다. 정본 절대값을 그대로 쓰면
                // 방마다 보스 공격력이 따로 노는데, 그 값도 정본이 이미 정해 두었다.
                float atk = Mathf.Max(1f, F(Row(rows, 0), "Damage"));
                m.FindPropertyRelative("_damageMul").floatValue =
                    Mathf.Clamp(F(r, "Damage") / atk, 0.4f, 2.5f);
            }
        }

        private static Dictionary<string, string> Row(List<Dictionary<string, string>> rows, int i)
            => i < rows.Count ? rows[i] : new Dictionary<string, string>();

        private static int PhaseOf(string phase) => phase switch
        {
            "P2+" => 2,
            "P3" => 3,
            _ => 1,
        };

        /// <summary>정본 공격 ID → 우리 거동. 대응법을 보고 옮겼다.</summary>
        private static BossPattern PatternOf(string attackId) => attackId switch
        {
            // B01 크러셔 — 부수는 기계
            "B01_ATK_1" => BossPattern.Ring,        // Quake Fist · 발밑 충격파, 뒤로 돌아 피한다
            "B01_ATK_2" => BossPattern.Charge,      // Wall Charge · 옆으로 피한다
            "B01_ATK_3" => BossPattern.Volley,      // Debris Fan · 틈으로 지나간다
            "B01_ATK_4" => BossPattern.PopupLaser,  // Double Quake · 줄을 번갈아 바꾼다

            // B02 가디언 — 방패를 든 것
            "B02_ATK_1" => BossPattern.Volley,      // Aegis Sweep · 뒤로 돌아 피한다
            "B02_ATK_2" => BossPattern.ShieldCycle, // Shield March · 벽으로 유인한다
            "B02_ATK_3" => BossPattern.AimedBurst,  // Reflective Line · 번쩍일 때 사격을 멈춘다
            "B02_ATK_4" => BossPattern.Charge,      // Broken Aegis Rush · 뒤 틈으로 빠진다

            // B03 킹핀 — 부하를 부리는 우두머리
            "B03_ATK_1" => BossPattern.AimedBurst,  // Crossfire Command · 표시된 선을 끊는다
            "B03_ATK_2" => BossPattern.Summon,      // Execution Mark · 몸을 갈아타거나 벗어난다
            "B03_ATK_3" => BossPattern.Volley,      // Cover-to-Cover Burst · 재장전 틈을 노린다
            "B03_ATK_4" => BossPattern.Ring,        // All Guns Open · 엄폐물을 돌며 피한다

            // B04 파이썬 — 조이고 뱉는 뱀
            "B04_ATK_1" => BossPattern.Ring,        // Constrict Spiral · 틈으로 대시한다
            "B04_ATK_2" => BossPattern.VenomCloud,  // Venom Spit Trail · 웅덩이가 생기기 전에 건넌다
            "B04_ATK_3" => BossPattern.Charge,      // Tail Gate · 쓸어오는 방향을 읽는다
            "B04_ATK_4" => BossPattern.Summon,      // Shed Skin Frenzy · 벗은 허물을 엄폐로 쓴다

            // B05 로봇 스네이크 — 머리가 둘인 기계
            "B05_ATK_1" => BossPattern.PopupLaser,  // Alternating Rail · 차선을 바꾼다
            "B05_ATK_2" => BossPattern.Charge,      // Cable Sweep · 관절에서 건넌다
            "B05_ATK_3" => BossPattern.AimedBurst,  // Magnetized Rockets · 반대 머리로 유인한다
            "B05_ATK_4" => BossPattern.Ring,        // Twin Overload · 한쪽 머리를 먼저 죽인다

            // B06 슬러지 — 녹아 흐르는 마지막 것
            "B06_ATK_1" => BossPattern.VenomCloud,  // Toxic Collapse · 안전지대가 돈다
            "B06_ATK_2" => BossPattern.Charge,      // Sludge Hand · 직각으로 피한다
            "B06_ATK_3" => BossPattern.Summon,      // Contaminated Split · 갈라진 핵을 죽인다
            "B06_ATK_4" => BossPattern.Ring,        // Final Dissolution · 깨끗한 섬을 따라 돈다

            _ => BossPattern.Volley,
        };

        /// <summary>탄 수. 사방 탄막은 많고, 조준탄은 적다.</summary>
        private static int ShotsOf(string attackId) => PatternOf(attackId) switch
        {
            BossPattern.Ring => 12,
            BossPattern.Volley => 6,
            BossPattern.AimedBurst => 3,
            _ => 1,
        };

        private static float SpreadOf(string attackId) => PatternOf(attackId) switch
        {
            BossPattern.Ring => 360f,
            BossPattern.Volley => 150f,
            BossPattern.AimedBurst => 12f,
            _ => 0f,
        };

        /// <summary>
        /// 출구 하나. **갈림길은 없다 — 문은 언제나 하나다** (2026-08-28 결정).
        ///
        /// 다음 방은 부르는 쪽이 정해서 넘긴다. 정본 `NextNodeIDs` 를 그대로 쓰면
        /// **버린 방을 가리켜** 문이 아무 데도 가지 않는다 —
        /// 48방에서 30방으로 접었기 때문이다.
        /// </summary>
        private static void WriteExits(SerializedProperty e, string roomId, string nextId,
                                       string canonId, float height,
                                       Dictionary<string, Dictionary<string, string>> route)
        {
            var exits = e.FindPropertyRelative("_exits");
            exits.ClearArray();

            // 다음 방이 없으면(CH3 마지막) 문을 세우지 않는다 — 그건 런의 끝이고,
            // 아무 데도 가지 않는 문을 세우면 걸어 들어가 정본 밖으로 떨어진다.
            if (string.IsNullOrEmpty(nextId)) return;
            {
                exits.InsertArrayElementAtIndex(0);
                var x = exits.GetArrayElementAtIndex(0);
                x.FindPropertyRelative("_exitId").stringValue = $"EX_{roomId}_1";
                x.FindPropertyRelative("_at").vector2Value =
                    new Vector2(RoomWidth * 0.5f, height - 0.6f);
                x.FindPropertyRelative("_nextRoomId").stringValue = nextId;

                // 문 너머가 어떤 길인지 — 맵툴에서 보려고 남긴다. 팻말로는 안 쓴다.
                // ⚠ 이 값은 **정본 ID** 로 찾는다. 우리가 다시 매긴 번호는 정본에 없다.
                if (!route.TryGetValue(canonId, out var t)) return;
                x.FindPropertyRelative("_archetype").stringValue = S(t, "RouteArchetype");
                x.FindPropertyRelative("_risk").intValue = I(t, "RiskScore");
                x.FindPropertyRelative("_reward").intValue = I(t, "RewardScore");
                x.FindPropertyRelative("_recovery").intValue = I(t, "RecoveryScore");
                x.FindPropertyRelative("_build").intValue = I(t, "BuildScore");
            }
        }

        // ── 보스 ──────────────────────────────────────────────────
        private static void WriteBoss(SerializedProperty e, Dictionary<string, string> m,
                                      Dictionary<string, Dictionary<string, string>> boss, float height)
        {
            string bossId = S(m, "BossID");
            if (string.IsNullOrEmpty(bossId) || !boss.TryGetValue(bossId, out var b))
            {
                e.FindPropertyRelative("_bossId").stringValue = string.Empty;
                return;
            }
            e.FindPropertyRelative("_bossId").stringValue = bossId;
            e.FindPropertyRelative("_bossName").stringValue = S(b, "Name");
            e.FindPropertyRelative("_bossHp").intValue = I(b, "HP");
            e.FindPropertyRelative("_bossAtk").intValue = I(b, "Damage");
            e.FindPropertyRelative("_bossMoveSpeed").floatValue = 70f;
            // 보스는 방 위쪽. 들어오자마자 붙지 않게 띄운다.
            e.FindPropertyRelative("_bossAt").vector2Value = new Vector2(RoomWidth * 0.5f, height * 0.72f);

            var gates = e.FindPropertyRelative("_bossPhaseGates");
            gates.ClearArray();
            float[] g = { 1f, 0.6f, 0.3f };
            for (int i = 0; i < g.Length; i++)
            {
                gates.InsertArrayElementAtIndex(i);
                gates.GetArrayElementAtIndex(i).floatValue = g[i];
            }
        }

        // ── 스폰 ──────────────────────────────────────────────────
        // ── 이벤트·상점·회복 방은 없앴다 ────────────────────────
        //
        // 판을 도는 중에 상점 창이 뜨고 이벤트 2택이 끼어드는데, 그 셋은 **나중에 새로
        // 기획한다.** 그때까지 방으로 남겨 두면 반쯤 만들어진 화면이 계속 끼어들어
        // 전투가 어떤지 판단할 수가 없다 — 48방 중 15방이 그랬다.
        //
        // 지우지 않고 **일반 전투방으로 돌렸다.** 배정표(`RoomLayoutTable`)가 방 종류를
        // 갖고 있고 거기서 셋 다 COMBAT 이다 — 되돌릴 때 그 표만 고치면 된다.

        /// <summary>
        /// 챕터별 <c>COMBAT</c> 방의 평균 보상. 돌려세운 방(옛 REST·SHOP)을 채우는 데 쓴다.
        /// 정예·보스는 뺀다 — 그쪽은 값이 서너 배라 평균을 끌어올린다.
        /// </summary>
        private static Dictionary<int, int> AverageReward(
            List<Dictionary<string, string>> master,
            Dictionary<string, Dictionary<string, string>> reward, string field)
        {
            var sum = new Dictionary<int, int>();
            var count = new Dictionary<int, int>();
            for (int i = 0; i < master.Count; i++)
            {
                if (S(master[i], "RoomType") != "COMBAT") continue;
                if (!reward.TryGetValue(S(master[i], "RoomID"), out var r)) continue;
                int ch = ChapterNo(S(master[i], "ChapterID"));
                sum[ch] = (sum.TryGetValue(ch, out var a) ? a : 0) + I(r, field);
                count[ch] = (count.TryGetValue(ch, out var b) ? b : 0) + 1;
            }

            var avg = new Dictionary<int, int>();
            foreach (var kv in count)
                if (kv.Value > 0) avg[kv.Key] = Mathf.RoundToInt((float)sum[kv.Key] / kv.Value);
            return avg;
        }

        /// <summary>
        /// 적 자리 — **레이아웃과 배정표가 갖는다.**
        ///
        /// 정본 <c>ROOM_SPAWN</c> 의 X·Y 는 28×17 m 방 기준이라 10×13 에 접으면
        /// 의도가 깨진다 — 가로를 절반으로 눌러 적끼리 어깨가 닿거나,
        /// 세로를 접어 앞줄과 뒤줄이 같은 줄에 선다. 좌표는 버리고 자리표를 쓴다.
        ///
        /// ⚠ **첫 무리만 굽는다.** 이 게임에 증원 소환은 없다 — 방을 다 비운 순간
        ///   허공에서 적이 나오면 빙의할 몸이 방금 다 죽은 상태라 유령으로 새 무리를
        ///   맞게 된다. 배정표의 두 번째 숫자(`waves:[8,10]` 의 10)는 쓰지 않는다.
        /// </summary>
        private static int WriteSpawns(SerializedProperty e, RoomLayoutTable.Layout layout,
                                       RoomLayoutTable.RoomPlan plan, string roomId)
        {
            var spawns = e.FindPropertyRelative("_spawns");
            spawns.ClearArray();
            if (layout == null || layout.Slots == null || plan == null) return 0;

            int count = Mathf.Clamp(plan.Count, 0, layout.Slots.Length);
            if (count <= 0) return 0;

            // ⚠ **조합이 자리를 고른다.** 선두 태그 둘을 번갈아 가져가므로 앞줄부터
            //   채운 방과 측면부터 채운 방은 적이 서는 곳이 통째로 다르다.
            //   48방을 레이아웃 12종으로 채우는 힘이 여기서 나온다.
            var order = RoomLayoutTable.OrderFor(layout, plan.Comp);

            // 고른 결과에 빼앗을 몸이 하나도 없으면 꼬리 하나를 호스트 자리로 바꾼다.
            RoomLayoutTable.EnsureHostInPick(layout, order, count);

            // 엘리트는 뒤에 선 놈부터. 뚫고 들어가는 값이 거기서 생긴다.
            var elites = RoomLayoutTable.PickElites(layout, order, count, plan.Elite);

            for (int i = 0; i < count; i++)
            {
                var slot = layout.Slots[order[i]];
                spawns.InsertArrayElementAtIndex(i);
                var x = spawns.GetArrayElementAtIndex(i);
                x.FindPropertyRelative("_spawnId").stringValue = $"{roomId}_{i + 1:00}";
                x.FindPropertyRelative("_actorId").stringValue = slot.Unit;
                x.FindPropertyRelative("_at").vector2Value = slot.At;
                x.FindPropertyRelative("_facing").stringValue = "DOWN";
                x.FindPropertyRelative("_elite").boolValue = elites.Contains(i);
                // 빼앗을 수 있는 몸은 레이아웃이 찍는다. 런타임이 이 표시를 보고
                // 숫자를 `MaxHostsPerWave` 로 다시 한번 조인다.
                x.FindPropertyRelative("_trigger").stringValue =
                    RoomLayoutTable.IsHostUnit(slot.Unit) ? "POSSESSION_TARGET" : "STANDARD";
            }
            return count;
        }

        /// <summary>
        /// 정본 좌표 그대로 쓰는 예전 경로. 이젠 **보스방에만** 쓴다 —
        /// 보스 아레나는 레이아웃 8종과 무관하게 자기 자리를 갖고 있다.
        /// </summary>
        private static int WriteCanonSpawns(SerializedProperty e,
                                       Dictionary<string, List<Dictionary<string, string>>> by,
                                       string roomId, float baseH,
                                       Dictionary<string, string> geo)
        {
            var spawns = e.FindPropertyRelative("_spawns");
            spawns.ClearArray();
            if (!by.TryGetValue(roomId, out var list)) return 0;

            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                spawns.InsertArrayElementAtIndex(i);
                var x = spawns.GetArrayElementAtIndex(i);
                // 전부 방에 들어설 때 함께 세운다. 웨이브 번호는 **어느 칸에 두느냐**로만 쓴다.
                x.FindPropertyRelative("_spawnId").stringValue = S(s, "SpawnID");
                x.FindPropertyRelative("_actorId").stringValue = SlugOf(S(s, "EnemyID"));
                // ⚠ 가로도 세로와 **같은 축척(1 m = 1 m)** 으로 옮긴다.
                //   예전에는 4 로 나누거나 정본 폭(24~32) 대비 비율로 줄였는데, 그러면
                //   가로 간격만 절반이 되어 적끼리 어깨가 닿는다.
                //   정본 |X| 최대가 8.5 이고 우리 반폭이 7.5 라, 0.88 만 곱하면
                //   가장자리까지 잘리지 않고 들어온다.
                float px = RoomWidth * 0.5f + F(s, "X") * SpawnXScale;

                // ── 세로 접기 ────────────────────────────────────
                //
                // 정본은 방을 14~26 m 로 잡고 Y 를 그 안에 흩어 놓는다. 우리 방은 13 m 고정이라
                // 그대로 쓰면 절반이 방 밖으로 나간다. **정본 방 높이 대비 비율**로 접어 넣는다.
                //   웨이브 1  아레나 아래 절반
                //   웨이브 2  아레나 위 절반   ← 칸을 더 붙이는 대신 위쪽에 세운다
                //
                // 걸어 다니는 자리는 테두리 안쪽 + 문 구역 아래다.
                float lo = EdgeMeters + 0.8f;
                float hi = RoomHeight - GateBandMeters - 0.8f;
                int wave = Mathf.Max(1, I(s, "Wave"));

                // 정본 Y 는 방 중심 기준 ±3.6 안팎이다. 그 폭을 아레나 절반에 맞춰 줄인다.
                float half = (hi - lo) * 0.5f;
                float mid = wave >= 2 ? lo + half * 1.5f : lo + half * 0.5f;
                float ny = mid + F(s, "Y") / Mathf.Max(1f, baseH) * half;

                x.FindPropertyRelative("_at").vector2Value = new Vector2(
                    Mathf.Clamp(px, EdgeMeters + 0.7f, RoomWidth - EdgeMeters - 0.7f),
                    Mathf.Clamp(ny, lo, hi));
                x.FindPropertyRelative("_facing").stringValue = "DOWN";
                x.FindPropertyRelative("_trigger").stringValue = S(s, "Role");
            }
            return list.Count;
        }

        // ── 지형지물 ──────────────────────────────────────────────
        //
        // 정본은 엄폐물 **개수**(1~2)와 해저드 종류만 준다. 좌표는 안 준다 —
        // 템플릿 이름이 배치의 전부다. 그래서 템플릿마다 자리를 여기서 정한다.
        private static void WriteObjects(SerializedProperty e, string roomId,
                                         RoomLayoutTable.Layout layout, bool isBossRoom)
        {
            var objs = e.FindPropertyRelative("_objects");
            objs.ClearArray();

            // 보스방은 손대지 않는다. 전용 아레나(`BossArenaKinds`)가 자리를 갖는다 —
            // 보스와 그 패턴이 주인공인 자리라 바닥이 시끄러우면 안 된다.
            if (isBossRoom)
            {
                var spawnPts = new List<Vector2>();
                var sp = e.FindPropertyRelative("_spawns");
                for (int i = 0; i < sp.arraySize; i++)
                    spawnPts.Add(sp.GetArrayElementAtIndex(i).FindPropertyRelative("_at").vector2Value);
                WriteProps(objs, 0, roomId, RoomHeight, spawnPts, true);
                return;
            }

            if (layout == null) return;
            var list = layout.Objects;
            for (int i = 0; i < list.Length; i++) WriteObject(objs, i, list[i]);
        }

        /// <summary>
        /// 레이아웃의 물건 하나를 그대로 적는다.
        ///
        /// ⚠ **스냅·클램프·겹침 검사를 하지 않는다.** 좌표가 이미 검증된 값이라
        ///   여기서 한 번 더 옮기면 손으로 맞춰 둔 형태가 반 칸씩 밀린다.
        ///   절차 생성 시절에는 좌표를 코드가 만들었으니 검사가 필요했지만,
        ///   지금은 사람이 정한 값이 들어온다.
        /// </summary>
        /// <summary>
        /// 60방 임포터가 같은 규칙으로 지형지물을 굽도록 열어 둔다.
        /// 규격·차단·해저드를 두 곳에 적으면 두 임포터가 서로 다른 방을 굽는다.
        /// </summary>
        internal static void WriteObjectPublic(SerializedProperty objs, int n, RoomLayoutTable.Obj src)
            => WriteObject(objs, n, src);

        private static void WriteObject(SerializedProperty objs, int n, RoomLayoutTable.Obj src)
        {
            objs.InsertArrayElementAtIndex(n);
            var o = objs.GetArrayElementAtIndex(n);

            string kind = src.Kind;
            o.FindPropertyRelative("_objectId").stringValue = $"{kind}_{n + 1}";
            o.FindPropertyRelative("_kind").stringValue = kind;
            o.FindPropertyRelative("_at").vector2Value = src.At;
            o.FindPropertyRelative("_size").vector2Value = PropSize(kind);

            bool solid = PropSolid(kind);
            bool channel = IsChannel(kind);
            o.FindPropertyRelative("_blocksMove").boolValue = solid || channel;
            o.FindPropertyRelative("_blocksShot").boolValue = solid;   // 도랑 위로는 탄이 지나간다
            // 적 탄은 지형을 통과한다 — 엄폐 뒤에 붙어 서는 것이 정답이 되면
            // 지형이 전술이 아니라 은신처가 된다. 막히는 것은 내 탄뿐이다.
            o.FindPropertyRelative("_blocksEnemyShot").boolValue = false;

            // ⚠ 앞 원소 복사에 딸려 온 해저드 값이 남으면 안 된다.
            //   가시는 밟으면 아픈 물건이라 여기서 늘 지우면 장식이 된다.
            bool spike = kind == "TIMED_SPIKE";
            o.FindPropertyRelative("_hazardKind").stringValue = spike ? "SPIKE" : "NONE";
            o.FindPropertyRelative("_hazardDamage").intValue = spike ? SpikeDamage : 0;
            o.FindPropertyRelative("_hazardTick").floatValue = spike ? SpikeTickSeconds : 0f;
        }

        /// <summary>
        /// 그 엄폐물이 무엇으로 보여야 하는가 — **생김새로 정한다.**
        ///
        /// 정본(ROOM_GEOMETRY)은 `Template` 과 `CoverCount` 만 준다. 종류는 없다.
        /// 그래서 전부 `PILLAR` 로 찍었는데, 기둥 그림(86×200 세로로 긴 것)이
        /// 2.6 m 정사각 덩어리에 늘어붙어 **픽셀이 뭉개진 건물**처럼 보였다.
        /// 이미 있는 그림 넷은 비율이 서로 다르다. 자리 크기에 비율이 맞는 것을 고른다.
        ///
        ///   가로:세로  1:2 이상 깊게 긺  → DIVIDER   (60×344)
        ///              깊이가 더 큼       → PILLAR    (86×200)
        ///              작은 정사각        → PILLAR    — 아래 주의 참고
        ///              가로가 크게 긺     → LOW_COVER (120×77)
        ///              그 밖(큰 정사각·조금 넓음) → BARRICADE (171×106)
        ///
        /// ⚠ 발자국 비율만 보면 안 된다. 엄폐물은 그림이 발자국보다 위로 솟는다
        ///   (`ObstacleRise` — 기둥은 114 px). 그래서 1.2 m 정사각 발자국도 화면에서는
        ///   108 × 222 인 **세로로 긴 기둥**이 된다. 작은 정사각은 기둥으로 보내야
        ///   PILLAR_CROSS 방의 기둥 열이 납작한 상자가 되지 않는다.
        /// </summary>
        // ── 장치물 (움직이거나 부술 수 있는 것) ────────────────────
        //
        // 엄폐물(기둥·칸막이·낮은벽)은 **서 있기만 한다.** 그것만으로는 방이
        // 전부 같은 모양이 된다 — 숨을 곳이 어디냐만 다를 뿐이다.
        // 장치물은 **시간을 만든다.** 지금 지나갈 수 있는가, 한 박자 기다려야 하는가.
        //
        // 챕터가 깊어질수록 종류가 하나씩 늘어난다. 처음부터 넷을 다 보여 주면
        // 무엇을 조심해야 하는지 배울 자리가 없다.
        //
        //   CH1  상자(부술 수 있다) · 가시(주기적으로 솟는다)
        //   CH2  + 회전 칼날
        //   CH3  + 왕복 해머
        //
        // 크기는 **이미 그림이 나온 것에 맞춘다** (72 px/m 기준).
        //   obj_crate       108×168 → 발자국 1.5×1.5
        //   obj_timed_spike 144×144 → 2.0×2.0
        //   obj_blade       126×126 → 1.75×1.75
        //   obj_hammer      126×126 → 1.75×1.75

        // ── 큰 물건 (블록 말고 낱개로 놓이는 것) ──────────────────
        //
        // 격자 블록만으로는 방이 전부 같은 얼굴이 된다. 키가 큰 기둥, 낮게 깔린 잔해,
        // 부술 수 있는 상자, 주기적으로 솟는 가시 — 이런 것이 섞여야 방마다 다른
        // 판단이 생긴다.
        //
        // 크기는 **이미 나온 그림에 맞춘다**(72 px/m).
        //   obj_pillar      72×186  → 발자국 1×1     · 솟음 114
        //   obj_low_cover  144×106  → 발자국 2×1     · 솟음 34
        //   obj_barricade  144×127  → 발자국 2×1     · 솟음 55
        //   obj_crate      108×168  → 발자국 1.5×1.5 · 솟음 60
        //   obj_timed_spike 144×144 → 발자국 2×2     · 솟음 0

        /// <summary>낱개로 놓는 물건 5종. **CH1 은 이 순서대로 돌려 가며** 전부 보여 준다.</summary>
        /// <summary>
        /// 무대마다 **쓰는 도형 조합이 다르다.**
        ///
        /// 같은 도형 여섯 개를 색만 바꾸면 아무리 다른 물건을 그려도 같은 맵으로 보인다.
        /// 눈이 먼저 읽는 것은 재질이 아니라 **실루엣**이다. 그래서 맵마다
        /// 어떤 크기·비율의 덩어리가 서 있는지를 바꾼다.
        ///
        /// 효과와 칸 크기는 종류마다 고정이므로(아래 `PropSize`) 게임 규칙은 그대로다.
        /// 바뀌는 것은 **그 맵에 어떤 도형이 나오는가** 뿐이다.
        /// </summary>
        /// ⚠ **배관(LOW_COVER·BARRICADE)은 아무 데나 넣지 않는다.**
        ///   배관은 연구소와 정유소의 물건이다. 밤거리 한복판이나 옥상에 파이프 다발이
        ///   놓여 있으면 그 방만 다른 게임처럼 보인다. 종류를 억지로 채우느니
        ///   **가짓수가 적은 편이 그 맵답다.**
        /// <summary>
        /// 보스 아레나 전용 조합. 배관도 톱니도 없다 —
        /// 보스와 그 패턴이 주인공인 자리라 바닥이 시끄러우면 안 된다.
        /// </summary>
        private static readonly string[] BossArenaKinds =
        {
            "PILLAR", "BULK", "RAIL", "CRATE", "TIMED_SPIKE",
        };

        /// ── 지금 놓는 것은 두 가지뿐이다 ─────────────────────────
        ///   ① 길을 막는 것 — block · pillar · rail · crate · bulk · low_cover · barricade
        ///   ② 밟으면 아픈 것 — timed_spike
        ///
        /// **움직이는 함정(톱니 `ROTATING_BLADE` · 해머 `SWING_HAMMER`)은 아직 안 놓는다.**
        /// 종류·그림·회전 로직은 다 살아 있다. 스테이지별로 어디에 몇 개 둘지는
        /// 기본 배치가 자리를 잡은 **뒤에** 정한다 — 지금 섞으면 무엇 때문에
        /// 방이 어려운지 구분이 안 된다.
        /// 톱니(`ROTATING_BLADE`)는 되살렸다 — 레이아웃 I(회전 관문)가 쓴다.
        /// 해머(`SWING_HAMMER`)는 아직 안 놓는다.
        private static string[] PropKindsFor(int stage) => stage switch
        {
            // 연구소 — 기둥과 배관, 그리고 큰 기계 덩어리.
            // ⚠ `BULK` 는 손으로 짠 레이아웃(B·D·E)이 CH1 에서 쓴다. 2×2 짜리가
            //   목록에 없으면 발자국이 같은 대체품이 없어 형태를 못 지킨다 —
            //   그래서 목록이 레이아웃을 따라간다. 반대가 아니다.
            1 => new[] { "PILLAR", "LOW_COVER", "BARRICADE", "CRATE", "BULK", "TIMED_SPIKE" },
            // 쓰레기장 — 큰 덩어리와 상자가 쌓인 곳. 기둥도 배관도 없다
            2 => new[] { "BULK", "CRATE", "RAIL", "TIMED_SPIKE" },
            // 미사일 기지 — 방호벽과 탄약. 네 가지뿐이라 오히려 정돈되어 보인다
            3 => new[] { "PILLAR", "BULK", "CRATE", "TIMED_SPIKE" },
            // 밤거리 — 길쭉한 것(가로등·표지)과 상자. 톱니가 처음 도는 무대다
            4 => new[] { "RAIL", "PILLAR", "CRATE", "TIMED_SPIKE", "ROTATING_BLADE" },
            // 옥상 — 낮은 설비와 세로 안테나. 기둥이 없어 시야가 트인다
            5 => new[] { "RAIL", "BULK", "CRATE", "TIMED_SPIKE", "ROTATING_BLADE" },
            // 정유소 — 탱크와 배관. 배관이 여기서는 주인공이다
            _ => new[] { "BULK", "PILLAR", "BARRICADE", "TIMED_SPIKE", "ROTATING_BLADE" },
        };

        /// <summary>
        /// 발자국은 **반드시 정수 칸**이다. 반 칸(1.5)이 하나라도 섞이면
        /// 그 물건만 격자에서 빠져나와 바닥 줄눈을 가로지른다 —
        /// 옆에 블록을 붙였을 때 한 놈만 어긋나 보이는 원인이 이것이었다.
        /// </summary>
        /// 칸 수는 **시안(`_exchange/out/30_tile/tile_concept.png`)을 재서** 정했다.
        /// 시안 바닥 타일이 32.6 px 이고 석조 블록 하나가 그 2칸이다 → 블록 = 우리 1칸.
        /// 나머지는 그 자로 잰 값이다: 파이프 다발·난간 6칸(=3), 상자 4×2칸(=2×1), 배수구 4×4칸(=2×2).
        private static Vector2 PropSize(string kind) => kind switch
        {
            "PILLAR"      => new Vector2(1f, 1f),   // 부서진 석조 기둥
            "LOW_COVER"   => new Vector2(3f, 1f),   // 누운 파이프 다발
            "BARRICADE"   => new Vector2(3f, 1f),   // 철망 달린 파이프 난간
            "CRATE"       => new Vector2(2f, 1f),   // 철제 상자
            // 톱니는 축을 중심으로 반경 2.2 m 를 돈다. 발자국은 날 한 장 크기지만
            // **비워 둬야 하는 자리는 그보다 훨씬 넓다** — 그래서 놓을 때 여유를 더 본다.
            "ROTATING_BLADE" => new Vector2(2f, 2f),
            "BULK"        => new Vector2(2f, 2f),   // 큰 덩어리 — 시야를 크게 가린다
            "RAIL"        => new Vector2(1f, 2f),   // 세로로 긴 것
            // 도랑 — 못 건너는 자리. 조각을 이어 붙여 긴 길을 만든다.
            "CHANNEL_H"   => new Vector2(2f, 1f),   // 가로 토막
            "CHANNEL_V"   => new Vector2(1f, 2f),   // 세로 토막
            _             => new Vector2(2f, 2f),   // TIMED_SPIKE — 바닥 배수구
        };

        /// <summary>
        /// 물건을 타일 격자에 앉힌다 — **발자국의 네 변이 정수 미터(=72 px)에 떨어지게** 한다.
        ///
        /// 가운데를 반올림하면 안 된다. 폭이 짝수(2 칸)인 물건은 가운데가 정수여야 하고
        /// 홀수(1 칸)인 물건은 가운데가 .5 여야 한다. 기준은 언제나 **변**이다.
        /// </summary>
        private static KeyValuePair<Vector2, Vector2> SnapToTiles(
            KeyValuePair<Vector2, Vector2> spot)
        {
            var s = spot.Value;
            float x = Mathf.Round(spot.Key.x - s.x * 0.5f) + s.x * 0.5f;
            float y = Mathf.Round(spot.Key.y - s.y * 0.5f) + s.y * 0.5f;
            return new KeyValuePair<Vector2, Vector2>(new Vector2(x, y), s);
        }

        /// <summary>
        /// 길을 막는가. 가시판만 밟고 지나간다 — 막아 버리면 피하는 물건이 아니라 벽이 된다.
        /// </summary>
        /// <summary>
        /// 방 ID → 구간 번호(1~6). `BattleDirector.FloorKeyOf` 와 **같은 규칙**이어야 한다 —
        /// 어긋나면 배경은 정유소인데 물건은 밤거리 것이 선다.
        /// </summary>
        /// <summary>`ROOM_CH2_007` → 7. 무대 물건을 방마다 돌려 쓰는 데 쓴다.</summary>
        // ═══════════════════════════════════════════════════════════
        //  48방 → 30방 (챕터당 10)
        // ═══════════════════════════════════════════════════════════
        //
        // 정본은 CH1 12 · CH2 16 · CH3 20 방이다. 챕터마다 길이가 달라
        // 3챕터가 1챕터의 두 배 가까이 길다 — 뒤로 갈수록 한 판이 늘어진다.
        // **챕터당 10방으로 통일한다.**
        //
        //   001~004  전투 4
        //   005      중간 보스        (정본 MID)
        //   006~009  전투 4 (마지막이 엘리트)
        //   010      최종 보스        (정본 FINAL)
        //
        // ⚠ **보스 6은 하나도 안 버린다.** 버리는 것은 전투방 18개뿐이고,
        //   챕터마다 앞에서부터 8개를 남긴다 — 정본이 난이도를 앞에서 뒤로
        //   올려 두었으므로 앞쪽을 남기는 것이 곡선을 덜 깨뜨린다.

        private const int RoomsPerChapter = 10;

        private static List<Dictionary<string, string>> SelectTen(
            List<Dictionary<string, string>> master)
        {
            var picked = new List<Dictionary<string, string>>(RoomsPerChapter * 3);
            for (int ch = 1; ch <= 3; ch++)
            {
                var combat = new List<Dictionary<string, string>>();
                var bosses = new List<Dictionary<string, string>>();
                for (int i = 0; i < master.Count; i++)
                {
                    var m = master[i];
                    if (ChapterNo(S(m, "ChapterID")) != ch) continue;
                    if (!string.IsNullOrEmpty(S(m, "BossID"))) bosses.Add(m);
                    else combat.Add(m);
                }
                // 정본이 이 모양이 아니면 접는 규칙이 성립하지 않는다. 조용히 넘기지 않는다.
                if (bosses.Count < 2 || combat.Count < 8)
                {
                    Debug.LogError($"[RoomV33] CH{ch} 정본이 예상과 다르다 — "
                                 + $"보스 {bosses.Count} · 전투 {combat.Count}");
                    continue;
                }

                for (int i = 0; i < 4; i++) picked.Add(combat[i]);
                picked.Add(bosses[0]);                       // 005 중간 보스
                for (int i = 4; i < 8; i++) picked.Add(combat[i]);
                picked.Add(bosses[1]);                       // 010 최종 보스
            }
            return picked;
        }

        /// <summary>고른 순서대로 `ROOM_CH{장}_{01..10}` 으로 다시 매긴다.</summary>
        private static string NewRoomId(Dictionary<string, string> m, int index,
                                        List<Dictionary<string, string>> picked)
        {
            int ch = ChapterNo(S(m, "ChapterID"));
            int no = index % RoomsPerChapter + 1;
            return $"ROOM_CH{ch}_{no:000}";
        }

        /// <summary>
        /// 다음 방. 챕터 끝이면 **다음 챕터 첫 방**으로 잇는다 —
        /// 한 런은 1→2→3 을 이어서 간다. CH3 마지막만 진짜 끝이다.
        /// </summary>
        private static string NextOf(List<Dictionary<string, string>> picked,
                                     Dictionary<string, string> newIdOf, int index)
            => index + 1 < picked.Count ? newIdOf[S(picked[index + 1], "RoomID")] : null;

        private static int RoomSeqOf(string roomId)
            => roomId != null && roomId.Length >= 3
               && int.TryParse(roomId.Substring(roomId.Length - 3), out int no) ? no : 0;

        private static int StageOf(string roomId)
        {
            if (string.IsNullOrEmpty(roomId) || roomId.Length < 8) return 1;
            int ch = roomId[7] - '0';
            if (ch < 1 || ch > 3) ch = 1;
            int no = 0;
            int.TryParse(roomId.Substring(roomId.Length - 3), out no);

            // ⚠ 테스트 순회가 켜져 있으면 **여기도 같이 돌아야 한다.**
            //   배경은 런타임에 정해지는데 물건 종류는 이 임포터가 미리 구워 둔다.
            //   한쪽만 돌면 옥상 배경에 연구소 배관이 서는 화면이 나온다.
            //   (`BattleDirector.FloorKeyOf` 의 순회 규칙과 같은 식이어야 한다.)
            if (ch == 1 && BattleDirector.CycleThemesInChapter1)
                return (no - 1 - (no > 6 ? 1 : 0)) % 6 + 1;

            int total = ch == 1 ? 12 : ch == 2 ? 16 : 20;
            return (ch - 1) * 2 + (no > total / 2 ? 2 : 1);
        }

        private static bool PropSolid(string kind)
            => kind != "TIMED_SPIKE" && kind != "ROTATING_BLADE" && !IsChannel(kind);

        /// <summary>
        /// 바닥에 파인 도랑인가.
        ///
        /// 엄폐물과 반대다 — **몸은 못 건너는데 탄은 위로 지나간다.**
        /// 그래서 막힘 하나로 둘을 같이 정하는 `PropSolid` 에 넣을 수 없다.
        /// 도랑이 탄까지 막으면 그냥 벽이고, 벽은 이미 `BULK` 가 한다.
        /// </summary>
        private static bool IsChannel(string kind)
            => kind == "CHANNEL_H" || kind == "CHANNEL_V";

        /// <summary>가시판을 밟고 있을 때의 피해와 간격.</summary>
        private const int SpikeDamage = 6;
        private const float SpikeTickSeconds = 0.8f;

        /// <summary>톱니에 스치면. 밟고 서 있는 가시보다 아프고 간격도 짧다.</summary>
        private const int BladeDamage = 10;
        private const float BladeTickSeconds = 0.5f;

        /// <summary>
        /// 낱개 물건을 놓는다. 방마다 2~3개.
        ///
        /// CH1 은 **확인용**이라 방 순서대로 종류를 돌려 5종을 전부 보여 준다.
        /// 자리는 방 ID 해시로 정해 매번 같게 만든다 — 맵툴에서 보고 손댈 수 있어야 한다.
        /// </summary>
        private static int WriteProps(SerializedProperty objs, int n, string roomId,
                                      float height, List<Vector2> spawns, bool isBoss)
        {
            int h = Mathf.Abs((roomId ?? string.Empty).GetHashCode());
            // 방 번호(ROOM_CH1_007 → 7)로 종류를 돌린다. 해시로 고르면 어떤 종류는
            // 챕터 내내 한 번도 안 나올 수 있다 — 확인용으로는 그러면 안 된다.
            int idx = 0;
            if (roomId != null && roomId.Length >= 3
                && int.TryParse(roomId.Substring(roomId.Length - 3), out int no)) idx = no;

            float lo = EdgeMeters + 2f;
            float hi = height - GateBandMeters - 2f;
            // 톱니는 반경 2.2 m 를 돌기 때문에 방 한가운데 쪽에 놓아야 한다.
            // 가장자리에 두면 날이 벽 그림 속으로 절반쯤 들어간다.

            int count = 3;
            for (int k = 0; k < count; k++)
            {
                var pool = isBoss ? BossArenaKinds : PropKindsFor(StageOf(roomId));
                string kind = pool[(idx + k) % pool.Length];
                var size = PropSize(kind);

                // 가로 세 자리(3/10 · 5/10 · 7/10)를 돌아가며 쓴다.
                float x = RoomWidth * (0.3f + 0.2f * ((idx + k) % 3));
                // 세로는 **한 줄 걸러** 놓는다. 블록 패턴은 6줄 중 홀수 줄이 비어 있으므로
                // 빈 줄을 바로 짚으면 `LiftOffBlockedRow` 가 헤맬 일이 없다.
                //
                // ⚠ 예전에는 `Lerp(lo, hi, ...)` 로 나눴다. 그 자리가 죄다 블록 줄이라
                //   셋 다 비켜 다니다 **맨 아래 한 줄에 나란히 쌓였다.**
                float y = lo + k * 2f + size.y * 0.5f;

                var spot = SnapToTiles(NudgeOffSpawns(
                    new KeyValuePair<Vector2, Vector2>(new Vector2(x, y), size), spawns));
                // ⚠ 큰 물건(폭 2 m)이 블록 줄과 같은 높이에 놓이면 그 줄이 통째로 막힌다.
                //   막히면 한 칸씩 올려 가며 지나갈 틈이 남는 줄을 찾는다.
                spot = LiftOffBlockedRow(objs, n, spot, lo, hi);
                if (float.IsNaN(spot.Key.x)) continue;   // 빈자리가 없다 — 이 물건은 거른다
                // ⚠ 마지막에 반드시 안전구역으로 조인다. 스폰 회피(`NudgeOffSpawns`)와
                //   줄 비키기(`LiftOffBlockedRow`)가 물건을 밀다 보면 가장자리를 넘긴다 —
                //   넘기면 배경 벽에 파묻히거나 문 구역을 침범한다.
                spot = ClampToSafeZone(spot, height);
                // 조이면서 다시 겹칠 수 있다. 그때도 거른다.
                if (!Fits(objs, n, spot)) continue;

                objs.InsertArrayElementAtIndex(n);
                var o = objs.GetArrayElementAtIndex(n);
                o.FindPropertyRelative("_objectId").stringValue = $"PROP_{n + 1}";
                o.FindPropertyRelative("_kind").stringValue = kind;
                o.FindPropertyRelative("_at").vector2Value = spot.Key;
                o.FindPropertyRelative("_size").vector2Value = spot.Value;
                bool solid = PropSolid(kind);
                o.FindPropertyRelative("_blocksMove").boolValue = solid;
                o.FindPropertyRelative("_blocksShot").boolValue = solid;
                // 적 탄은 지형을 통과한다 — 엄폐 뒤에 붙어 서는 것이 정답이 되면 안 된다.
                o.FindPropertyRelative("_blocksEnemyShot").boolValue = false;
                // ⚠ InsertArrayElementAtIndex 는 앞 원소를 복사한다. 값을 반드시 다시 쓴다.
                //
                // 가시판은 **밟으면 아픈 물건**이다. 여기를 늘 "NONE" 으로 지우고 있어서
                // 가시가 오르내리기만 하고 데미지가 0 이었다 — 피할 이유가 없는 장식이었다.
                bool spike = kind == "TIMED_SPIKE";
                bool blade = kind == "ROTATING_BLADE";
                o.FindPropertyRelative("_hazardKind").stringValue =
                    spike ? "SPIKE" : blade ? "BLADE" : "NONE";
                o.FindPropertyRelative("_hazardDamage").intValue =
                    spike ? SpikeDamage : blade ? BladeDamage : 0;
                o.FindPropertyRelative("_hazardTick").floatValue =
                    spike ? SpikeTickSeconds : blade ? BladeTickSeconds : 0f;
                n++;
            }
            return n;
        }

        /// <summary>
        /// 물건을 안전구역 안으로 조인다. 가장자리 여백과 위쪽 문 구역은 배경이 쓴다.
        /// </summary>
        private static KeyValuePair<Vector2, Vector2> ClampToSafeZone(
            KeyValuePair<Vector2, Vector2> spot, float height)
        {
            var size = spot.Value;
            float hx = size.x * 0.5f, hy = size.y * 0.5f;
            // ⚠ 안전구역의 경계부터 칸에 맞춘다. 경계가 2.5 같은 반 칸이면
            //   거기에 조인 물건이 반 칸 어긋난 채로 굳는다.
            float minX = Mathf.Ceil(EdgeMeters) + hx;
            float maxX = Mathf.Floor(RoomWidth - EdgeMeters) - hx;
            float minY = Mathf.Ceil(EdgeMeters) + hy;
            float maxY = Mathf.Floor(height - GateBandMeters) - hy;
            float x = Mathf.Clamp(spot.Key.x, minX, maxX);
            float y = Mathf.Clamp(spot.Key.y, minY, maxY);
            return SnapToTiles(new KeyValuePair<Vector2, Vector2>(new Vector2(x, y), size));
        }

        /// <summary>
        /// 이 자리에 놓으면 그 가로줄이 막히는가. 막히면 한 칸씩 올려 빈 줄을 찾는다.
        /// 몸이 지나가려면 **연속 빈칸 1칸**이 남아야 한다 (한 칸 = 72 px, 몸 = 50.4 px).
        /// </summary>
        private static KeyValuePair<Vector2, Vector2> LiftOffBlockedRow(
            SerializedProperty objs, int count, KeyValuePair<Vector2, Vector2> spot,
            float lo, float hi)
        {
            var first = spot;
            // ⚠ 되돌아갈 바닥 줄을 **칸에 맞춰 올려** 잡는다.
            //   그냥 `lo`(3 m)로 되돌리면 발자국 반 칸(0.5)을 빼고 반올림하는 순간
            //   2.5 로 **내려앉는다.** 그 줄은 블록 밴드 아래라 언제나 비어 있어서,
            //   비켜 다니던 물건 셋이 죄다 거기 나란히 쌓였다(들어서는 자리 정면).
            float floorY = Mathf.Ceil(lo - spot.Value.y * 0.5f) + spot.Value.y * 0.5f;
            // 세로로 한 칸씩 올리며 본다. 한 바퀴 돌아도 못 찾으면 가로로 한 칸 옮겨 다시.
            for (int lane = 0; lane < 6; lane++)
            {
                for (int tries = 0; tries < 8; tries++)
                {
                    if (Fits(objs, count, spot)) return spot;
                    // ⚠ 옮기는 폭은 **정수 칸**이어야 한다. 1.5 칸씩 밀면 비켜 준 물건만
                    //   격자에서 빠져나와 바닥 줄눈을 가로지른다.
                    float y = spot.Key.y + 1f;
                    if (y > hi) y = floorY;      // 위쪽 끝에 닿으면 아래부터 다시 본다
                    spot = SnapToTiles(
                        new KeyValuePair<Vector2, Vector2>(new Vector2(spot.Key.x, y), spot.Value));
                }
                // 한 칸씩 옮긴다. `SnapToTiles` 가 위상을 다시 잡으므로 홀수·짝수 폭 모두 안전하다.
                float nx = spot.Key.x + 1f;
                if (nx > RoomWidth - EdgeMeters - spot.Value.x * 0.5f)
                    nx = EdgeMeters + spot.Value.x * 0.5f;
                spot = SnapToTiles(
                    new KeyValuePair<Vector2, Vector2>(new Vector2(nx, floorY), spot.Value));
            }
            // ⚠ 못 찾으면 **놓지 않는다.** 억지로 우겨넣으면 길이 막히거나 물건이 겹친다.
            //   물건 하나가 빠지는 것보다 방이 막히는 쪽이 훨씬 나쁘다.
            return new KeyValuePair<Vector2, Vector2>(new Vector2(float.NaN, float.NaN), first.Value);
        }

        /// <summary>빈자리인가 — 다른 물건과 안 겹치고, 그 줄이 막히지도 않는가.</summary>
        private static bool Fits(SerializedProperty objs, int count,
                                 KeyValuePair<Vector2, Vector2> spot)
            => !Overlaps(objs, count, spot) && RowPasses(objs, count, spot)
               && Walkable(objs, count, spot);

        // ── 통행 검사 ─────────────────────────────────────────────
        //
        // 줄 단위 검사(`RowPasses`)만으로는 모자란다. 줄마다 빈칸이 있어도
        // 그 빈칸들이 **서로 이어지지 않으면** 방이 막힌다.
        // 빈칸 2칸을 요구하던 시절에는 여유가 커서 우연히 안 걸렸는데,
        // 1칸으로 풀자마자 48방 중 10방이 출구까지 못 가게 됐다.
        //
        // 그래서 여기서는 **런타임과 같은 판정으로 실제로 걸어 본다.**
        //   · 몸 = 발판 상자 50.4 × 30.9 px (그림의 절반 폭)
        //   · 블록 판정 = 세로 0.7 칸 (`BattleDirector.ObstacleFootScale`)
        //   · 좌표는 런타임과 같은 **그림 중심** 기준
        // 이 셋 중 하나라도 런타임과 어긋나면 검사가 거짓말을 한다.

        private const float BodyW = 96f * 1.05f;      // 숙주 그림 폭 × UnitScale
        private const float BodyH = 92f * 1.05f;
        private const float FootHalfX = BodyW * 0.25f / 72f;          // m
        private const float FootHalfY = BodyH * 0.16f / 72f;          // m
        private const float FootDropM = (BodyH * 0.5f - BodyH * 0.16f) / 72f;
        // 블록 축소 배율. `BattleDirector.ObstacleViewScale` 과 **같아야 한다** —
        // 어긋나면 이 검사가 통과시킨 방이 실제로는 막히거나, 그 반대가 된다.
        // (블록은 그림·판정이 같은 값으로 줄어든다. 보이는 것이 곧 막는 것.)
        private const float BlockHitX = 0.7f;
        private const float BlockHitY = 0.7f;

        /// <summary>이 자리에 놓아도 입구에서 출구까지 걸어갈 수 있는가.</summary>
        private static bool Walkable(SerializedProperty objs, int count,
                                     KeyValuePair<Vector2, Vector2> spot)
        {
            const float Step = 0.25f;
            int W = Mathf.RoundToInt(RoomWidth / Step);
            int H = Mathf.RoundToInt(RoomHeight / Step);

            // 막는 상자들을 미터 단위로 모은다 (후보 포함)
            var bx = new List<Vector4>();   // (cx, cy, halfW, halfH)
            // 모든 장애물이 같은 배율로 줄어든다 (`BattleDirector.ObstacleViewScale`).
            void Add(Vector2 at, Vector2 sz, bool isBlock)
                => bx.Add(new Vector4(at.x, at.y,
                                      sz.x * BlockHitX * 0.5f,
                                      sz.y * BlockHitY * 0.5f));
            for (int i = 0; i < count; i++)
            {
                var e = objs.GetArrayElementAtIndex(i);
                if (!e.FindPropertyRelative("_blocksMove").boolValue) continue;
                Add(e.FindPropertyRelative("_at").vector2Value,
                    e.FindPropertyRelative("_size").vector2Value,
                    e.FindPropertyRelative("_kind").stringValue == "BLOCK");
            }
            Add(spot.Key, spot.Value, false);

            float halfX = BodyW * 0.5f / 72f, halfY = BodyH * 0.5f / 72f;
            bool Free(int gx, int gy)
            {
                float px = gx * Step, py = gy * Step;                 // 그림 중심
                if (px < halfX || px > RoomWidth - halfX) return false;
                if (py < halfY || py > RoomHeight - halfY) return false;
                float fy = py - FootDropM;                            // 발판 중심
                for (int i = 0; i < bx.Count; i++)
                {
                    var b = bx[i];
                    if (Mathf.Abs(px - b.x) < b.z + FootHalfX &&
                        Mathf.Abs(fy - b.y) < b.w + FootHalfY) return false;
                }
                return true;
            }

            // 플레이어 스폰에서 시작한다. 막혀 있으면 위로 한 칸씩 비켜 본다.
            int sx = Mathf.RoundToInt(RoomWidth * 0.5f / Step);
            int sy = Mathf.RoundToInt(1.7f / Step);
            while (sy <= H && !Free(sx, sy)) sy++;
            if (sy > H) return false;

            var seen = new bool[W + 1, H + 1];
            var q = new Queue<Vector2Int>();
            q.Enqueue(new Vector2Int(sx, sy)); seen[sx, sy] = true;
            float topY = 0f;
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                topY = Mathf.Max(topY, p.y * Step);
                for (int d = 0; d < 4; d++)
                {
                    int nx = p.x + (d == 0 ? 1 : d == 1 ? -1 : 0);
                    int ny = p.y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                    if (nx < 0 || ny < 0 || nx > W || ny > H) continue;
                    if (seen[nx, ny] || !Free(nx, ny)) continue;
                    seen[nx, ny] = true; q.Enqueue(new Vector2Int(nx, ny));
                }
            }
            // 문 구역(위 2.5 m) 바로 아래까지 닿아야 나갈 수 있다.
            return topY >= RoomHeight - GateBandMeters - 0.5f;
        }

        /// <summary>
        /// 이미 놓인 물건과 **겹치는가.** 칸을 나눠 가지면 겹침이고, 변끼리 맞닿는 것은 아니다.
        ///
        /// ⚠ 예전에는 0.1 m 여유를 둬서 **딱 붙는 것까지 겹침으로 셌다.**
        ///   모든 물건이 칸에 맞춰 앉는 지금은 이게 치명적이다 —
        ///   3칸짜리(파이프 다발·난간)는 옆에 블록만 있으면 어디에도 못 앉아
        ///   48방에 여섯 개밖에 안 들어갔다. 붙여 놓는 것이 오히려 정답이다.
        /// </summary>
        private static bool Overlaps(SerializedProperty objs, int count,
                                     KeyValuePair<Vector2, Vector2> spot)
        {
            const float Pad = -0.01f;   // 맞닿음 허용, 한 칸이라도 겹치면 차단
            for (int i = 0; i < count; i++)
            {
                var e = objs.GetArrayElementAtIndex(i);
                var at = e.FindPropertyRelative("_at").vector2Value;
                var sz = e.FindPropertyRelative("_size").vector2Value;
                if (Mathf.Abs(at.x - spot.Key.x) < (sz.x + spot.Value.x) * 0.5f + Pad
                 && Mathf.Abs(at.y - spot.Key.y) < (sz.y + spot.Value.y) * 0.5f + Pad) return true;
            }
            return false;
        }

        /// <summary>
        /// 이 물건을 놓아도 **그 물건이 걸치는 모든 가로줄**에 길이 남는가.
        /// 몸이 지나가려면 줄마다 **연속 빈칸 1칸**이 있어야 한다.
        ///
        /// ⚠ 예전에는 2칸을 요구했다. 그때는 판정 상자가 몸통 폭(1.1 m)이었기 때문이다.
        ///   지금은 발판이 그림의 **절반**(50.4 px = 0.7 칸)이라 한 칸(72 px)을 걸어서 지나간다 —
        ///   좌우로 10.8 px 씩 남는다. 2칸을 계속 요구하면 만들 수 있는 지형이 헛되이 좁아진다.
        ///
        /// ⚠ 예전에는 줄 번호를 `RoundToInt(at.y)` 로 잡았다. 물건 가운데가 늘 X.5 라
        ///   은행가 반올림 때문에 3.5 와 4.5 가 **둘 다 4** 로 뭉쳤다 —
        ///   서로 다른 두 줄이 한 줄로 합쳐져, 위아래 줄에 블록이 있으면
        ///   비어 있는 줄까지 막힌 것으로 읽혔다. 3칸짜리는 그래서 방마다 거의 못 들어갔다.
        ///   이제 **칸 경계**(발자국의 아래·위 변)로 줄을 세므로 뭉치지 않는다.
        /// </summary>
        private static bool RowPasses(SerializedProperty objs, int count,
                                      KeyValuePair<Vector2, Vector2> spot)
        {
            int lo = Mathf.RoundToInt(spot.Key.y - spot.Value.y * 0.5f);
            int hi = Mathf.RoundToInt(spot.Key.y + spot.Value.y * 0.5f);   // 반열림 [lo, hi)
            var occupied = new bool[12];

            void Mark(float cx, float w)
            {
                int a = Mathf.RoundToInt(cx - w * 0.5f), b = Mathf.RoundToInt(cx + w * 0.5f);
                for (int c = Mathf.Max(0, a); c < Mathf.Min(12, b); c++) occupied[c] = true;
            }

            for (int row = lo; row < hi; row++)
            {
                System.Array.Clear(occupied, 0, occupied.Length);
                for (int i = 0; i < count; i++)
                {
                    var e = objs.GetArrayElementAtIndex(i);
                    if (!e.FindPropertyRelative("_blocksMove").boolValue) continue;
                    var at = e.FindPropertyRelative("_at").vector2Value;
                    var sz = e.FindPropertyRelative("_size").vector2Value;
                    int a0 = Mathf.RoundToInt(at.y - sz.y * 0.5f);
                    int a1 = Mathf.RoundToInt(at.y + sz.y * 0.5f);
                    if (row < a0 || row >= a1) continue;
                    Mark(at.x, sz.x);
                }
                Mark(spot.Key.x, spot.Value.x);

                int best = 0, run = 0;
                for (int c = 1; c <= 9; c++) { if (!occupied[c]) { run++; best = Mathf.Max(best, run); } else run = 0; }
                if (best < 1) return false;
            }
            return true;
        }

        /// <summary>
        /// 엄폐물을 스폰 자리에서 비켜 놓는다.
        ///
        /// 정본은 적을 방 한가운데 근처에 세우고, 우리 템플릿(RING·PILLAR_CROSS)도
        /// 한가운데에 엄폐물을 놓는다. 그대로 두면 적이 기둥 속에 갇혀 **탄이 막혀 안 죽고
        /// 방이 영영 안 끝난다.** 가로로 조금씩 밀어 겹치지 않는 자리를 찾는다.
        /// 못 찾으면 원래 자리에 둔다 — 런타임의 `ClearOfCover` 가 마지막 안전망이다.
        /// </summary>
        private static KeyValuePair<Vector2, Vector2> NudgeOffSpawns(
            KeyValuePair<Vector2, Vector2> spot, List<Vector2> spawns)
        {
            var at = spot.Key; var size = spot.Value;
            bool Hits(Vector2 c)
            {
                for (int i = 0; i < spawns.Count; i++)
                {
                    // 스폰 몸 반폭(0.4 m)만큼 여유를 둔다. 모서리에 걸치기만 해도 탄이 먹힌다.
                    if (Mathf.Abs(spawns[i].x - c.x) <= size.x * 0.5f + 0.4f &&
                        Mathf.Abs(spawns[i].y - c.y) <= size.y * 0.5f + 0.4f) return true;
                }
                return false;
            }
            if (!Hits(at)) return spot;

            // ① 가로로 먼저 민다. 템플릿이 정한 높이(위/아래 갈래)를 지키는 쪽이 낫다.
            float halfX = size.x * 0.5f;
            for (float step = 0.5f; step <= RoomWidth; step += 0.5f)
                foreach (float dir in new[] { 1f, -1f })
                {
                    float x = Mathf.Clamp(at.x + dir * step, halfX, RoomWidth - halfX);
                    var c = new Vector2(x, at.y);
                    if (!Hits(c)) return new KeyValuePair<Vector2, Vector2>(c, size);
                }

            // ② 가로로 못 피하면 세로로 민다. 6.4 m 벽처럼 가로로 긴 것은 방 안 어디로
            //    옮겨도 같은 높이의 스폰을 비껴갈 수 없다 — SPLIT_LEVEL 이 그 경우다.
            float halfY = size.y * 0.5f;
            for (float step = 0.5f; step <= 6f; step += 0.5f)
                foreach (float dir in new[] { 1f, -1f })
                {
                    var c = new Vector2(at.x, at.y + dir * step);
                    if (c.y < halfY || c.y > 40f) continue;   // 방 높이는 14~17 이라 넉넉히 자른다
                    if (!Hits(c)) return new KeyValuePair<Vector2, Vector2>(c, size);
                }
            return spot;
        }

        /// <summary>
        /// 몸이 지나가는 데 필요한 최소 틈(m). 유닛 144 px 의 발밑 반폭이 39.6 px 이고
        /// 72 px/m 이므로 몸 하나가 1.1 m 다. 방향키로 편하게 통과하려면 그 1.5 배는 있어야 한다.
        /// </summary>
        private const float MinGapMeters = 1.6f;

        // ── 지형 블록 ────────────────────────────────────────────
        //
        // 예전에는 큰 사각형 한두 개를 통째로 놓았다. 그래서 방마다 "덩어리가 하나
        // 놓여 있을 뿐" 이고 형태가 안 읽혔다. 궁수의 전설은 **같은 크기 블록을
        // 격자에 늘어놓아 모양을 만든다** — 긴 벽, 짧은 토막, 흩어진 기둥.
        // 블록 하나하나는 의미가 없고 **배열이 의미를 만든다.**
        //
        // 아래 패턴은 한 글자가 블록 한 칸(1 m)이다. 폭 10 m = 10 칸.
        //   '#' 블록  ·  '.' 빈칸
        //
        // ⚠ 모든 행에 **연속된 빈칸 1칸 이상**이 있어야 한다.
        //   한 칸은 72 px 이고 몸 판정은 50.4 px 이라 지나간다. 통행 검사가 이걸 잡는다.

        /// <summary>블록 한 칸의 크기(m). 방 폭 10 m 가 정확히 10 칸이 된다.</summary>
        private const float TileMeters = 1f;

        // ── 절차 생성은 버렸다 ──────────────────────────────────
        //
        // 예전에는 4행짜리 패턴 6종을 방 ID 해시로 고르고, 밴드마다 찍고, 좌우를 뒤집었다.
        // 결과는 방마다 물건 15~25개에 절반 이상이 1×1 블록이었고 **형태가 안 읽혔다.**
        // 적이 하나도 없는 EVENT 방에 물건이 제일 많은(25개) 것이 그 증상이었다.
        //
        // 이제 형태는 `RoomLayoutTable` 의 손으로 짠 레이아웃 8종이 갖는다.
        // (AVSR_Decisions.md §10-3 — "절차적 생성은 버린다")



        // ── 잡일 ──────────────────────────────────────────────────
        private static string SlugOf(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId) || !enemyId.StartsWith("EN_")) return string.Empty;
            return int.TryParse(enemyId.Substring(3), out int n) && n > 0 && n < EnemySlug.Length
                ? EnemySlug[n] : string.Empty;
        }

        private static int ChapterNo(string chapterId)
            => chapterId != null && chapterId.Length >= 3
               && int.TryParse(chapterId.Substring(2), out int n) ? n : 1;

        private static List<Dictionary<string, string>> Load(string name)
        {
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, Dir, name);
            if (!File.Exists(path)) { Debug.LogError($"[RoomV33] {name} 없음 — {path}"); return null; }
            return FlatJson.Rows(File.ReadAllText(path));
        }

        private static Dictionary<string, Dictionary<string, string>> Index(
            List<Dictionary<string, string>> rows, string key)
        {
            var map = new Dictionary<string, Dictionary<string, string>>();
            if (rows == null) return map;
            foreach (var r in rows) map[S(r, key)] = r;
            return map;
        }

        private static Dictionary<string, List<Dictionary<string, string>>> Group(
            List<Dictionary<string, string>> rows, string key)
        {
            var map = new Dictionary<string, List<Dictionary<string, string>>>();
            if (rows == null) return map;
            foreach (var r in rows)
            {
                string k = S(r, key);
                if (!map.TryGetValue(k, out var l)) map[k] = l = new List<Dictionary<string, string>>();
                l.Add(r);
            }
            return map;
        }

        private static string S(Dictionary<string, string> r, string k)
            => r != null && r.TryGetValue(k, out var v) && v != null ? v : string.Empty;

        private static float F(Dictionary<string, string> r, string k)
            => float.TryParse(S(r, k), NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0f;

        private static int I(Dictionary<string, string> r, string k) => Mathf.RoundToInt(F(r, k));
    }
}
