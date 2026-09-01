using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Game.Module.InGame;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 정본 런타임 JSON → Unity 런타임 에셋.
    ///
    /// 정본이 정한 흐름은 한 방향이다 (UNITY_EDITOR_IMPORTER_SPEC):
    ///   마스터 XLSX → 검증기 → **런타임 JSON** → 런타임 에셋 → 레지스트리 → 방 생성기
    /// 런타임에서 JSON 을 직접 읽지 않는다. 여기서 한 번 굽고 그 뒤로는 에셋만 본다.
    ///
    /// 스폰 출처는 `layout.enemySpawns` 하나뿐이다(SPAWN_SRC_01). 최상위 `spawns` 를
    /// 찾거나 대체로 쓰면 임포트 오류로 처리한다 — 정본이 명시적으로 금지한 항목이다.
    ///
    /// 오류는 생성을 멈추고, 경고는 기록하고 넘어간다(정본 Error policy).
    /// </summary>
    public static class CanonImporter
    {
        private const string Runtime =
            "Projects/AVSR/Canon/Runtime/CH01_03_RUNTIME_DATA_v1.5.json";
        private const string Contract =
            "Projects/AVSR/Canon/Runtime/DATA_CONTRACT_v1.5.json";
        private const string OutDir = "Assets/BundleResource/TableData";
        private const string OutPath = OutDir + "/RoomTable.asset";
        private const string Address = "TableData/RoomTable";
        private const string Group = "tabledata";

        [MenuItem("Tools/Game/Import Canon Runtime Data")]
        public static void Run()
        {
            var root = Directory.GetParent(Application.dataPath)!.FullName;
            var runtimePath = Path.Combine(root, Runtime);
            if (!File.Exists(runtimePath))
            {
                Debug.LogError($"[Canon] 런타임 JSON 없음: {Runtime}");
                return;
            }

            var json = JObject.Parse(File.ReadAllText(runtimePath));
            var schema = LoadSchemas(Path.Combine(root, Contract));
            var errors = new List<string>();
            var warnings = new List<string>();

            // 정본이 금지한 항목 — 조용히 넘어가면 옛 좌표로 방이 만들어진다
            if (json["spawns"] != null)
                errors.Add("최상위 `spawns` 가 있다. 정본은 이것을 제거했다(SPAWN_SRC_01). "
                           + "layout.enemySpawns 만 스폰 출처다.");

            var rooms = BuildRooms(json, schema, errors, warnings);

            foreach (var w in warnings) Debug.LogWarning($"[Canon] {w}");
            if (errors.Count > 0)
            {
                foreach (var e in errors) Debug.LogError($"[Canon] {e}");
                Debug.LogError($"[Canon] 오류 {errors.Count}건 — 에셋을 만들지 않았다.");
                return;
            }

            Directory.CreateDirectory(OutDir);
            var table = ScriptableObject.CreateInstance<RoomTable>();
            table.name = "RoomTable";   // 비어 있으면 파일명과 다르다는 경고가 뜬다
            SetField(table, "_contractVersion", (string)json["schemaVersion"] ?? "");
            SetField(table, "_rooms", rooms.ToArray());

            var existing = AssetDatabase.LoadAssetAtPath<RoomTable>(OutPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(table, existing);
                EditorUtility.SetDirty(existing);
            }
            else AssetDatabase.CreateAsset(table, OutPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RegisterAddressable(OutPath, Address);

            PatchHosts(json, schema);

            int spawnTotal = rooms.Sum(r => ((SpawnEntry[])GetField(r, "_spawns")).Length);
            int objTotal = rooms.Sum(r => ((ObjectEntry[])GetField(r, "_objects")).Length);
            Debug.Log($"[Canon] 방 {rooms.Count}개 · 스폰 {spawnTotal}개 "
                      + $"· 지형지물 {objTotal}개 → {OutPath} "
                      + $"(주소 {Address}, 경고 {warnings.Count}건)");
        }

        // ─────────────────────────────────────────────────────────
        private static List<RoomEntry> BuildRooms(JObject json,
                                                  Dictionary<string, string[]> schema,
                                                  List<string> errors, List<string> warnings)
        {
            var layout = json["layout"] as JObject ?? new JObject();

            var layouts = Rows(layout["layouts"]);
            var playerSpawns = Rows(layout["playerSpawns"]);
            var enemySpawns = Rows(layout["enemySpawns"]);
            var bossLayouts = Rows(layout["bossLayouts"]);
            var objects = Rows(layout["objects"]);
            var bosses = Rows(json["bosses"]);
            var bossPhases = Rows(json["bossPhases"]);
            var entryExit = Rows(json["entryExit"]);
            var waves = Rows(json["waves"]);

            var eeF = Fields(schema, "entryExit");
            var spF = Fields(schema, "layout.enemySpawns");
            var wvF = Fields(schema, "waves");
            var boF = Fields(schema, "bosses");
            var phF = Fields(schema, "bossPhases");

            // 살아 있는 배우 ID — 스폰이 가리키는 대상이 실재하는지 본다
            var actors = new HashSet<string>();
            foreach (var key in new[] { "enemies", "elites", "bosses" })
                foreach (var r in Rows(json[key]))
                    actors.Add(Str(r[0]));

            var startNodes = new HashSet<string>();
            foreach (var c in json["chapters"] ?? new JArray())
            {
                var s = (string)c["startNodeId"];
                if (!string.IsNullOrEmpty(s)) startNodes.Add(s);
            }

            var roomMeta = new Dictionary<string, JToken>();
            foreach (var o in json["rooms"] ?? new JArray())
            {
                var id = (string)o["roomId"];
                if (!string.IsNullOrEmpty(id)) roomMeta[id] = o;
            }

            var result = new List<RoomEntry>();
            var seen = new HashSet<string>();

            foreach (var lay in layouts)
            {
                string id = Str(lay[0]);
                if (!seen.Add(id)) { errors.Add($"방 ID 중복: {id}"); continue; }

                var room = new RoomEntry();
                roomMeta.TryGetValue(id, out var meta);
                float w = Num(lay[5]), h = Num(lay[6]);

                SetField(room, "_roomId", id);
                SetField(room, "_chapter", (int)Num(lay[1]));
                SetField(room, "_type", Str(lay[2]));
                SetField(room, "_route", meta != null ? (string)meta["route"] ?? "" : "");
                SetField(room, "_intent", meta != null ? (string)meta["intent"] ?? "" : Str(lay[3]));
                SetField(room, "_isChapterStart", startNodes.Contains(id));
                SetField(room, "_width", w);
                SetField(room, "_height", h);
                SetField(room, "_cameraMode", lay.Length > 11 ? Str(lay[11]) : "");

                // 출입구
                var ee = entryExit.FirstOrDefault(r => Str(r[0]) == id);
                if (ee == null) errors.Add($"{id}: 출입구(entryExit) 줄이 없다");
                else
                {
                    SetField(room, "_entry", new Vector2(F(ee, eeF, "EntryX"), F(ee, eeF, "EntryY")));
                    SetField(room, "_exits", Exits(ee, eeF, errors, id));
                    SetField(room, "_unlockRule", S(ee, eeF, "UnlockRule"));
                }

                // 보스는 enemySpawns 에 없다 — 페이즈마다 자리가 바뀌므로 따로 있다.
                // 여기서 안 집으면 보스방이 텅 빈 방으로 들어온다.
                var boss = bossLayouts.FirstOrDefault(r => Str(r[0]) == id);
                if (boss != null)
                {
                    string bossId = Str(boss[1]);
                    SetField(room, "_bossId", bossId);
                    SetField(room, "_bossName", Str(boss[2]));
                    SetField(room, "_bossAt", new Vector2(Num(boss[5]), Num(boss[6])));

                    var def = bosses.FirstOrDefault(r => Str(r[0]) == bossId);
                    if (def == null) errors.Add($"{id}: 보스 {bossId} 가 bosses 에 없다");
                    else
                    {
                        SetField(room, "_bossHp", (int)F(def, boF, "MaxHP"));
                        SetField(room, "_bossAtk", (int)F(def, boF, "AttackDamage"));
                        SetField(room, "_bossMoveSpeed", F(def, boF, "MoveSpeed"));
                    }

                    // 페이즈가 바뀌는 체력 비율. P1 의 시작(1.0)은 문턱이 아니라 시작점이라 뺀다.
                    var gates = bossPhases
                        .Where(r => S(r, phF, "BossID") == bossId)
                        .Select(r => F(r, phF, "HPStart"))
                        .Where(v => v > 0f && v < 1f)
                        .OrderByDescending(v => v)
                        .ToArray();
                    SetField(room, "_bossPhaseGates", gates);

                    var phases = new List<BossPhaseEntry>();
                    foreach (var r in bossPhases.Where(r => S(r, phF, "BossID") == bossId))
                    {
                        var e = new BossPhaseEntry();
                        // Phase 는 "P1" 형태다. 숫자만 뽑는다.
                        SetField(e, "_phase", PhaseNumber(S(r, phF, "Phase")));
                        SetField(e, "_hpStart", F(r, phF, "HPStart"));
                        SetField(e, "_pattern", S(r, phF, "AttackPattern"));
                        SetField(e, "_telegraphSeconds", TelegraphSeconds(S(r, phF, "Telegraph")));
                        SetField(e, "_minionPool", Pool(S(r, phF, "MinionPool")));
                        SetField(e, "_switchWindows", (int)F(r, phF, "SwitchWindowCount"));
                        SetField(e, "_arenaBehavior", S(r, phF, "ArenaBehavior"));
                        phases.Add(e);
                    }
                    SetField(room, "_bossPhases", phases.OrderBy(x => x.Phase).ToArray());
                    if (phases.Count == 0) warnings.Add($"{id}: 보스 {bossId} 의 페이즈가 없다");
                }

                // 플레이어 스폰 — 스키마가 없는 컬렉션이라 위치로 읽는다
                //   [SpawnID, RoomID, X, Y, Facing, ...]
                var ps = playerSpawns.FirstOrDefault(r => Str(r[1]) == id);
                if (ps == null) errors.Add($"{id}: 플레이어 스폰이 없다");
                else SetField(room, "_playerSpawn", new Vector2(Num(ps[2]), Num(ps[3])));

                // 적 스폰
                var list = new List<SpawnEntry>();
                foreach (var r in enemySpawns)
                {
                    if (S(r, spF, "RoomID") != id) continue;
                    string actor = S(r, spF, "ActorID");
                    if (!actors.Contains(actor))
                        errors.Add($"{id}: 스폰이 없는 배우를 가리킨다 — {actor}");

                    float x = F(r, spF, "X"), y = F(r, spF, "Y");
                    if (x < 0f || x > w || y < 0f || y > h)
                        errors.Add($"{id}: 스폰 {S(r, spF, "SpawnID")} 가 방 밖이다 "
                                   + $"({x:0.##}, {y:0.##}) / {w}×{h}");

                    var s = new SpawnEntry();
                    SetField(s, "_spawnId", S(r, spF, "SpawnID"));
                    SetField(s, "_actorId", actor);
                    SetField(s, "_at", new Vector2(x, y));
                    SetField(s, "_facing", S(r, spF, "Facing"));
                    SetField(s, "_delaySeconds", F(r, spF, "DelaySec"));
                    SetField(s, "_trigger", S(r, spF, "Trigger"));
                    SetField(s, "_telegraph", S(r, spF, "Telegraph"));
                    list.Add(s);
                }
                SetField(room, "_spawns", list.ToArray());

                // 지형지물. 계약에 스키마가 없는 컬렉션이라 열 위치로 읽는다.
                //   [RoomID, ObjectID, Kind, X, Y, W, H,
                //    BlocksMove, BlocksShot, BlocksSight, Destructible,
                //    HazardKind, HazardDamage, HazardTick, ?, Note]
                var ol = new List<ObjectEntry>();
                foreach (var r in objects)
                {
                    if (Str(r[0]) != id) continue;
                    var e = new ObjectEntry();
                    SetField(e, "_objectId", Str(r[1]));
                    SetField(e, "_kind", Str(r[2]));
                    SetField(e, "_at", new Vector2(Num(r[3]), Num(r[4])));
                    SetField(e, "_size", new Vector2(Num(r[5]), Num(r[6])));
                    SetField(e, "_blocksMove", Bool(r[7]));
                    SetField(e, "_blocksShot", Bool(r[8]));
                    SetField(e, "_blocksSight", Bool(r[9]));
                    SetField(e, "_destructible", Bool(r[10]));
                    SetField(e, "_hazardKind", Str(r[11]));
                    SetField(e, "_hazardDamage", (int)Num(r[12]));
                    SetField(e, "_hazardTick", Num(r[13]));
                    SetField(e, "_unnamed", r.Length > 14 ? Num(r[14]) : 0f);
                    ol.Add(e);
                }
                SetField(room, "_objects", ol.ToArray());


                result.Add(room);
            }

            // 다음 방이 실재하는가 — 끊기면 런이 그 자리에서 막힌다
            foreach (var r in result)
            {
                var id = (string)GetField(r, "_roomId");
                foreach (var x in (ExitEntry[])GetField(r, "_exits"))
                    if (!seen.Contains(x.NextRoomId))
                        errors.Add($"{id}: 다음 방 {x.NextRoomId} 이 없다");
            }
            return result;
        }

        /// <summary>
        /// 다음 방 값을 푼다. 정본은 세 가지 형태를 한 칸에 담는다.
        ///   `CH1_N02`              한 갈래
        ///   `CH2_N04A|CH2_N04B`    갈림길
        ///   `CHAPTER_CLEAR`        더 갈 곳 없음(끝)
        /// 이걸 그대로 방 ID 로 보면 끝 방과 갈림길이 전부 "없는 방" 으로 잡힌다.
        /// </summary>
        private static readonly string[] Terminals =
            { "CHAPTER_CLEAR", "GAME_SLICE_CLEAR", "" };

        /// <summary>
        /// 출구를 푼다. 정본은 갈림길을 파이프로 한 칸에 담는다.
        ///   ExitPointID `EX_A|EX_B` · ExitX `2.1|6.3` · NextRoomID `N04A|N04B`
        /// 세 칸의 갈래 수가 맞아야 짝이 지어진다 — 어긋나면 어느 문이 어디로
        /// 가는지 알 수 없으므로 오류다.
        /// 끝 방(CHAPTER_CLEAR)은 출구가 없다.
        /// </summary>
        private static ExitEntry[] Exits(JToken[] ee, string[] f,
                                         List<string> errors, string roomId)
        {
            var ids = Split(S(ee, f, "ExitPointID"));
            var xs = Split(S(ee, f, "ExitX"));
            var nexts = Split(S(ee, f, "NextRoomID"));
            float y = F(ee, f, "ExitY");

            var live = nexts.Where(n => !Terminals.Contains(n)).ToArray();
            if (live.Length == 0) return Array.Empty<ExitEntry>();

            if (xs.Length != nexts.Length)
            {
                errors.Add($"{roomId}: 출구 좌표 {xs.Length}개인데 다음 방 {nexts.Length}개다");
                return Array.Empty<ExitEntry>();
            }

            var result = new List<ExitEntry>();
            for (int i = 0; i < nexts.Length; i++)
            {
                if (Terminals.Contains(nexts[i])) continue;
                var e = new ExitEntry();
                SetField(e, "_exitId", i < ids.Length ? ids[i] : $"EX_{roomId}_{i}");
                SetField(e, "_at", new Vector2(
                    float.TryParse(xs[i], NumberStyles.Float, CultureInfo.InvariantCulture,
                                   out var x) ? x : 0f, y));
                SetField(e, "_nextRoomId", nexts[i]);
                result.Add(e);
            }
            return result.ToArray();
        }

        /// <summary>"P2" → 2. 숫자가 없으면 1.</summary>
        private static int PhaseNumber(string raw)
        {
            var digits = new string((raw ?? "").Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var n) && n > 0 ? n : 1;
        }

        /// <summary>
        /// 예고 시간을 문구에서 뽑는다 — "Ground crack line 0.85s" → 0.85.
        /// 정본이 예고를 산문으로 적어 놔서 이 숫자만 건져 쓴다. 예고 길이는
        /// 피할 수 있느냐를 가르는 값이라 눈대중으로 정하면 안 된다.
        /// </summary>
        private static float TelegraphSeconds(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return 0.45f;
            var m = System.Text.RegularExpressions.Regex.Match(raw, @"([0-9]*\.?[0-9]+)\s*s");
            return m.Success && float.TryParse(m.Groups[1].Value, NumberStyles.Float,
                                               CultureInfo.InvariantCulture, out var v)
                ? v : 0.45f;
        }

        /// <summary>"E001/E002" → ["E001","E002"]. NONE 은 빈 목록.</summary>
        private static string[] Pool(string raw)
            => string.IsNullOrEmpty(raw) || raw == "NONE"
               ? Array.Empty<string>()
               : raw.Split('/').Select(v => v.Trim()).Where(v => v.Length > 0).ToArray();

        private static string[] Split(string raw)
            => string.IsNullOrEmpty(raw)
               ? Array.Empty<string>()
               : raw.Split('|').Select(v => v.Trim()).ToArray();

        // ── 스키마 기반 접근 ────────────────────────────────────
        // 런타임 JSON 의 여러 컬렉션은 **이름 없는 위치 기반 배열**이다.
        // 순서는 DATA_CONTRACT 의 collectionSchemas 가 정한다. 위치를 코드에 박으면
        // 정본이 필드를 하나 끼우는 순간 조용히 어긋난다.
        private static Dictionary<string, string[]> LoadSchemas(string path)
        {
            var result = new Dictionary<string, string[]>();
            if (!File.Exists(path)) return result;
            var c = JObject.Parse(File.ReadAllText(path));
            if (c["collectionSchemas"] is not JObject cs) return result;

            foreach (var kv in cs)
            {
                if (kv.Value?["fields"] is not JArray fs) continue;
                result[kv.Key] = fs.Select(f => (string)(f["name"] ?? f) ?? "").ToArray();
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────
        // HostTable 에 정본 실수치를 밀어넣는다.
        //
        // 예전에는 표시 스탯(0~100)에 배율을 곱해 전투 수치를 만들었다. 그 결과 정본이
        // 90 이라 한 갱스터의 체력이 264 가 되는 식으로 전 배우가 2~3배 어긋나 있었다.
        // 이제 정본 값을 **그대로** 쓴다.
        //
        // 손으로 정한 것(이름·그림·액티브 스킬·해금 조건)은 건드리지 않는다. 정본이
        // 권위를 갖는 열만 덮어쓴다.
        // ─────────────────────────────────────────────────────────

        private const string HostPath = OutDir + "/HostTable.asset";

        /// <summary>
        /// 정본에 있지만 우리 테이블에 자리가 없던 배우. 없으면 대역으로 서서
        /// 다른 배우의 수치로 싸운다 — 방패병이 갱스터 체력으로 나오는 식이다.
        ///
        /// 그림이 아직 없어 다른 몸의 그림을 빌린다(SpriteKey). 빌릴 그림조차 없으면
        /// 통째로 안 보여서 방이 끝나지 않으므로 반드시 채운다.
        /// </summary>
        private readonly struct StandIn
        {
            public readonly string ActorId, HostKey, NameKr, SpriteKey;
            /// <summary>정본은 엘리트에게 교전 프로필(attacks)을 주지 않는다. 가장 가까운 배우의 값을 빌린다.</summary>
            public readonly float Range, Interval;
            /// <summary>근접인가. 안 정하면 기본값이 근접이라 사거리 531px 짜리 주먹이 나온다.</summary>
            public readonly bool Melee;

            public StandIn(string actorId, string hostKey, string nameKr, string spriteKey,
                           float range, float interval, bool melee)
            {
                ActorId = actorId; HostKey = hostKey; NameKr = nameKr; SpriteKey = spriteKey;
                Range = range; Interval = interval; Melee = melee;
            }
        }

        private static readonly StandIn[] StandIns =
        {
            // 정본 enemies 에 있고 교전 프로필도 있다 — Range/Interval 은 attacks 에서 온다
            new("E013", "actor_missile_merc", "미사일 용병", "commando_grenade", 0f, 0f, false),
            new("E017", "actor_shield_trooper", "방패병",     "amazon_elite",     0f, 0f, true),
            new("E018", "actor_sensor_drone",  "센서 드론",   "robot",            0f, 0f, false),

            // 엘리트는 정본에 교전 프로필이 없다 — 근접 둘은 파이터(AP_E002),
            // 원거리 하나는 화이트위저드(AP_E010) 값을 빌린다.
            // 제 그림이 들어왔다 — 더는 남의 그림을 빌리지 않는다
            new("EL01", "actor_enforcer",      "집행자",     "",             1.2f, 1.15f, true),
            new("EL02", "actor_shield_captain","방패 대장",  "amazon_elite", 1.2f, 1.15f, true),
            new("EL03", "actor_arc_warden",    "아크 워든",  "white_wizard", 6.2f, 2.0f, false),
        };

        /// <summary>
        /// 정본에 없는 창작 몸. **정본이 아니다** — 다만 그대로 두면 예전 공식이 돌아
        /// 체력이 정본 배우의 2~3배가 되어 같은 방에 세울 수가 없다.
        /// 가장 가까운 정본 형제의 값에서 성격만큼만 비틀어 같은 격에 맞춘 것이다.
        ///
        /// 순서: HP, 공격력, 이동(m/s), 사거리(m), 간격(s), 탄속(m/s),
        ///       내 사거리(m), 내 간격(s), 내 이동(m/s)
        /// </summary>
        private static readonly Dictionary<string, float[]> Derived = new()
        {
            // 갱스터(E001 90/11/1.2/6.5/1.45) 기준 — 더 가볍고 빠르다
            ["hopper"]      = new[] { 85f, 10f, 1.4f, 6.2f, 1.20f, 8.5f, 6.4f, 1.00f, 4.6f },
            // 폭력배(E004 120/9/1.5/5.5/0.38) 기준 — 연사형
            ["hopper_smg"]  = new[] { 95f,  7f, 1.6f, 5.2f, 0.42f, 8.5f, 5.6f, 0.42f, 4.5f },
            // 돌격 갱스터(E007 135/10/1.6/7.0/0.7) 기준 — 중화기라 느리고 두껍다
            ["commando_mg"] = new[] { 145f, 9f, 1.3f, 6.8f, 0.45f, 8.5f, 7.0f, 0.45f, 3.5f },
            // 살라만더(E003 170/16/1.8/3.0/1.8) 기준 — 같은 브레스, 냉기
            ["dragon_blue"] = new[] { 175f, 15f, 1.7f, 3.2f, 1.85f, 8.5f, 3.4f, 1.70f, 3.5f },
            // 닌자(E011 110/19/1.7/5.0/1.2) 기준 — 사슬이라 근접
            ["ninja_chain"] = new[] { 120f, 17f, 1.8f, 1.8f, 0.95f, 0f,   1.9f, 0.85f, 5.0f },
            // 화이트위저드(E010 145/15/1.8/6.2/2.0) 기준 — 제어형
            ["snowwoman"]   = new[] { 130f, 12f, 1.7f, 6.0f, 1.85f, 8.5f, 6.2f, 1.60f, 3.8f },
        };

        /// <summary>
        /// 정본을 일부러 따르지 않는 몸. **여기 있는 것만 예외**이고 나머지는 전부 정본이다.
        ///
        /// 정본은 배우의 교전 방식을 문자열로만 주는데, 그림이 이미 나온 뒤라
        /// 데이터와 그림이 어긋나는 자리가 생긴다. 그때는 그림 쪽을 따른다 —
        /// 수치는 고칠 수 있지만 그려 놓은 동작은 못 바꾼다.
        ///
        /// 값: 교전 방식, 적으로 나올 때의 사거리(m).
        /// 사거리를 같이 안 내리면 근접 판정에 원거리 사거리가 붙어 멀리서 주먹이 닿는다.
        /// </summary>
        private static readonly Dictionary<string, (Game.Character.AttackKind Kind, float RangeM)>
            KindOverrides = new()
        {
            // 정본 E006 Master Fighter 는 PROJECTILE 3.5m 인데 우리 아마존 정예 그림은
            // 도끼를 든 근접이다. 파이터(E002 1.2m)보다 한 뼘 긴 1.5m 로 둔다.
            ["amazon_elite"] = (Game.Character.AttackKind.Melee, 1.5f),
        };

        /// <summary>
        /// 정본의 호스트 프로필(AP_H##)을 이 몸에 써도 되는가.
        ///
        /// 정본은 배우 여럿이 호스트 하나를 나눠 쓰게 짰다 — 폭력배(E004)와 드래군(E016)이
        /// 근접 호스트(H02·H20)에 물려 있다. 그대로 가져오면 산탄과 브레스의 사거리가
        /// 1.45m 가 되어 총을 든 채 붙어야 쏜다. 교전 거리의 격이 다르면 안 쓴다.
        /// 그때는 그 배우 자신의 값(적으로 나올 때의 값)을 내가 탔을 때도 쓴다.
        /// </summary>
        private static bool SameReachClass(Game.Character.HostEntry e, string canonMode)
        {
            var kind = (Game.Character.AttackKind)GetField(e, "_attackKind");
            bool oursMelee = kind is Game.Character.AttackKind.Melee
                                  or Game.Character.AttackKind.Pulse;
            bool canonMelee = canonMode == "MELEE";
            return oursMelee == canonMelee;
        }

        private static void PatchHosts(JObject json, Dictionary<string, string[]> schema)
        {
            var table = AssetDatabase.LoadAssetAtPath<Game.Character.HostTable>(HostPath);
            if (table == null)
            {
                Debug.LogWarning($"[Canon] HostTable 없음 — 정본 수치를 적용하지 못했다: {HostPath}");
                return;
            }

            var enF = Fields(schema, "enemies");
            var elF = Fields(schema, "elites");
            var atF = Fields(schema, "attacks");

            var enemies = new Dictionary<string, JToken[]>();
            foreach (var r in Rows(json["enemies"])) enemies[Str(r[0])] = r;
            var elites = new Dictionary<string, JToken[]>();
            foreach (var r in Rows(json["elites"])) elites[Str(r[0])] = r;

            // 같은 배우라도 적일 때(AP_E###)와 내가 탔을 때(AP_H##)의 교전값이 다르다
            var byOwner = new Dictionary<string, JToken[]>();
            foreach (var r in Rows(json["attacks"]))
                byOwner[$"{S(r, atF, "OwnerType")}:{S(r, atF, "OwnerID")}"] = r;

            // 탄속·탄 수는 projectiles 가 권위다. 주인 ID 로 찾는다.
            var prF = Fields(schema, "projectiles");
            var shots = new Dictionary<string, JToken[]>();
            foreach (var r in Rows(json["projectiles"])) shots[S(r, prF, "OwnerID")] = r;

            // 예고 시간·동시 공격 수는 aiProfiles 에 있다
            var aiF = Fields(schema, "aiProfiles");
            var ai = new Dictionary<string, JToken[]>();
            foreach (var r in Rows(json["aiProfiles"])) ai[S(r, aiF, "EnemyID")] = r;

            var list = new List<Game.Character.HostEntry>(
                (Game.Character.HostEntry[])GetField(table, "_entries"));

            int patched = 0, added = 0, derived = 0;
            var missing = new List<string>();
            var flipped = new List<string>();
            var overrode = new List<string>();

            foreach (var stand in StandIns)
            {
                if (list.Exists(x => (string)GetField(x, "_enemyId") == stand.ActorId)) continue;
                var made = new Game.Character.HostEntry();
                SetField(made, "_hostKey", stand.HostKey);
                SetField(made, "_nameEn", stand.ActorId);
                SetField(made, "_nameKr", stand.NameKr);
                SetField(made, "_enemyId", stand.ActorId);
                SetField(made, "_spriteKey", stand.SpriteKey);
                SetField(made, "_actorOnly", true);
                SetField(made, "_attackKind", stand.Melee ? Game.Character.AttackKind.Melee
                                                          : Game.Character.AttackKind.Single);
                // 표시 스탯은 정본 실수치가 있으면 쓰이지 않는다. 0 이면 UI 가 빈 막대를 그리므로 중간값을 둔다.
                SetField(made, "_hp", 50); SetField(made, "_atk", 50);
                SetField(made, "_spd", 50); SetField(made, "_atkSpeed", 50);
                if (stand.Range > 0f)
                {
                    SetField(made, "_canonRange", stand.Range);
                    SetField(made, "_canonInterval", stand.Interval);
                }
                list.Add(made);
                added++;
            }

            foreach (var e in list)
            {
                var id = (string)GetField(e, "_enemyId");
                if (string.IsNullOrEmpty(id))
                {
                    var hk = (string)GetField(e, "_hostKey");
                    if (Derived.TryGetValue(hk, out var v))
                    {
                        SetField(e, "_canonHp", (int)v[0]);
                        SetField(e, "_canonAtk", (int)v[1]);
                        SetField(e, "_canonMoveSpeed", v[2]);
                        SetField(e, "_canonRange", v[3]);
                        SetField(e, "_canonInterval", v[4]);
                        SetField(e, "_canonShotSpeed", v[5]);
                        SetField(e, "_canonShotCount", 1);
                        SetField(e, "_canonHostRange", v[6]);
                        SetField(e, "_canonHostInterval", v[7]);
                        SetField(e, "_canonHostMoveSpeed", v[8]);
                        SetField(e, "_canonHostShotSpeed", v[5]);
                        derived++;
                    }
                    else missing.Add(hk);
                    continue;
                }

                string possess;
                if (enemies.TryGetValue(id, out var row))
                {
                    SetField(e, "_canonHp", (int)F(row, enF, "MaxHP"));
                    SetField(e, "_canonAtk", (int)F(row, enF, "AttackDamage"));
                    SetField(e, "_canonMoveSpeed", F(row, enF, "MoveSpeed"));
                    SetField(e, "_canonEngageSpeed", F(row, enF, "EngageSpeed"));
                    if (ai.TryGetValue(id, out var ar))
                    {
                        SetField(e, "_canonTelegraph", TelegraphSeconds(S(ar, aiF, "Telegraph")));
                        SetField(e, "_canonMaxConcurrent", (int)F(ar, aiF, "MaxConcurrent"));
                    }
                    // 사거리·간격의 권위는 attacks 다. enemies 에도 같은 열이 있지만
                    // 교전 프로필이 있는 쪽이 조준·탄속까지 함께 정한다.
                    SetField(e, "_canonRange", F(row, enF, "AttackRange"));
                    SetField(e, "_canonInterval", F(row, enF, "AttackInterval"));
                    // 정본 0.16~1.0 을 정수 칸에 담는다. 순서만 지키면 되므로 100 배한다.
                    SetField(e, "_possessPriority",
                             Mathf.RoundToInt(F(row, enF, "PossessPriority") * 100f));
                    possess = S(row, enF, "PossessionType");

                    // 교전 거리의 격(근접/원거리)은 그 배우 자신의 프로필이 정한다.
                    // 우리가 흡혈귀·야구선수를 근접으로 적어 뒀는데 정본은 둘 다 원거리다.
                    // 그대로 두면 근접 판정에 사거리 514px 이 붙어 방 건너편을 주먹으로 때린다.
                    var key = (string)GetField(e, "_hostKey");
                    if (KindOverrides.TryGetValue(key, out var ov))
                    {
                        // 일부러 정본을 안 따르는 자리 — 자동 판정보다 먼저 본다
                        SetField(e, "_attackKind", ov.Kind);
                        SetField(e, "_canonRange", ov.RangeM);
                        overrode.Add(key);
                    }
                    else if (byOwner.TryGetValue($"ENEMY:{id}", out var er2)
                             && !SameReachClass(e, S(er2, atF, "AttackMode")))
                    {
                        bool canonMelee = S(er2, atF, "AttackMode") == "MELEE";
                        SetField(e, "_attackKind",
                                 canonMelee ? Game.Character.AttackKind.Melee
                                            : Game.Character.AttackKind.Single);
                        flipped.Add($"{key}→{(canonMelee ? "근접" : "원거리")}");
                    }

                    if (shots.TryGetValue(id, out var ep))
                    {
                        SetField(e, "_canonShotSpeed", F(ep, prF, "Speed"));
                        SetField(e, "_canonShotCount", Mathf.Max(1, (int)F(ep, prF, "Count")));
                    }

                    // 내가 탔을 때의 값 — 정본이 이 몸에 호스트 프로필을 준 경우만
                    var hostId = S(row, enF, "HostID");
                    if (!string.IsNullOrEmpty(hostId) && hostId != "None"
                        && byOwner.TryGetValue($"HOST:{hostId}", out var hr))
                    {
                        // 이동속도는 근접/원거리와 상관이 없다. 프로필이 안 맞아도 이건 쓴다 —
                        // 안 쓰면 적 걸음(1.0~2.5m/s)으로 조종하게 되어 다른 몸의 절반도 못 간다.
                        SetField(e, "_canonHostMoveSpeed", F(hr, atF, "MoveSpeed"));

                        if (SameReachClass(e, S(hr, atF, "AttackMode")))
                        {
                            SetField(e, "_canonHostRange", F(hr, atF, "Range"));
                            SetField(e, "_canonHostInterval", F(hr, atF, "Interval"));

                            // 탄 수는 내가 탔을 때만 정본을 따른다. 적은 정본이 전부 1 발이고,
                            // 우리 쪽 확산(폭력배 5 발 등)은 그 몸의 정체성이라 지운다면 그림이 죽는다.
                            if (shots.TryGetValue(hostId, out var hp))
                            {
                                SetField(e, "_canonHostShotSpeed", F(hp, prF, "Speed"));
                                int n = (int)F(hp, prF, "Count");
                                if (n > 0) SetField(e, "_shotCount", n);
                            }
                        }
                        else
                        {
                            // 격이 안 맞는 프로필이 예전 실행 때 들어가 있을 수 있다.
                            // 지워야 그 배우 자신의 교전값으로 돌아간다.
                            SetField(e, "_canonHostRange", 0f);
                            SetField(e, "_canonHostInterval", 0f);
                            SetField(e, "_canonHostShotSpeed", 0f);
                        }
                    }
                }
                else if (elites.TryGetValue(id, out var er))
                {
                    SetField(e, "_canonHp", (int)F(er, elF, "MaxHP"));
                    SetField(e, "_canonAtk", (int)F(er, elF, "AttackDamage"));
                    SetField(e, "_canonMoveSpeed", F(er, elF, "MoveSpeed"));
                    possess = S(er, elF, "PossessionType");

                    // 엘리트도 예고가 있다 — elites 표의 Telegraph 칸이다.
                    // 한 방에 한 마리뿐이라 동시 공격 수는 1 로 둔다.
                    SetField(e, "_canonTelegraph", TelegraphSeconds(S(er, elF, "Telegraph")));
                    SetField(e, "_canonMaxConcurrent", 1);

                    // 엘리트는 정본에 교전 프로필이 없어 자동 판정이 안 걸린다.
                    // 여기서 안 정하면 기본값(근접)에 원거리 사거리가 붙는다.
                    var st = Array.Find(StandIns, x => x.ActorId == id);
                    if (st.ActorId != null)
                    {
                        SetField(e, "_attackKind", st.Melee ? Game.Character.AttackKind.Melee
                                                            : Game.Character.AttackKind.Single);
                        SetField(e, "_canonRange", st.Range);
                        SetField(e, "_canonInterval", st.Interval);
                    }
                }
                else { missing.Add(id); continue; }

                // "Not Possessable" 처럼 띄어쓰기가 섞여 온다 — 공백을 지우고 본다
                var p = possess.Replace(" ", "");
                SetField(e, "_possessKind",
                    p == "NotPossessable" ? Game.Character.PossessKind.NotPossessable
                  : p == "Condition"      ? Game.Character.PossessKind.Condition
                                          : Game.Character.PossessKind.Immediate);
                patched++;
            }

            SetField(table, "_entries", list.ToArray());
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Canon] 호스트 {patched}종에 정본 실수치 적용 · 전투 전용 배우 {added}종 추가"
                      + $" · 정본에 없어 형제 값에서 맞춘 창작 몸 {derived}종"
                      + (flipped.Count > 0
                         ? $" · 교전 거리의 격을 정본에 맞춘 몸: {string.Join(", ", flipped)}"
                         : "")
                      + (overrode.Count > 0
                         ? $" · 일부러 정본을 안 따르는 몸: {string.Join(", ", overrode)}"
                         : "")
                      + (missing.Count > 0
                         ? $" · 아무 값도 못 준 몸 {missing.Count}종: {string.Join(", ", missing)}"
                         : ""));
        }

        private static string[] Fields(Dictionary<string, string[]> schema, string name)
        {
            if (schema.TryGetValue(name, out var f)) return f;
            Debug.LogWarning($"[Canon] 계약에 {name} 스키마가 없다 — 그 컬렉션은 비게 된다");
            return Array.Empty<string>();
        }

        private static string S(JToken[] row, string[] fields, string name)
            => Str(Cell(row, fields, name));

        private static float F(JToken[] row, string[] fields, string name)
            => Num(Cell(row, fields, name));

        private static JToken Cell(JToken[] row, string[] fields, string name)
        {
            int i = Array.IndexOf(fields, name);
            return i >= 0 && i < row.Length ? row[i] : null;
        }

        // ── 값 변환 ────────────────────────────────────────────
        // 정본 JSON 은 같은 열에도 숫자와 문자열이 섞여 있다(ExitX 가 "4.2" 인 줄이 있다).
        // 여기서 흡수하지 않으면 임포트가 그 한 줄에서 죽는다.
        private static string Str(JToken t)
            => t == null || t.Type == JTokenType.Null ? string.Empty : t.ToString();

        private static bool Bool(JToken t)
            => t != null && t.Type != JTokenType.Null
               && (t.Type == JTokenType.Boolean ? (bool)t
                   : bool.TryParse(t.ToString(), out var b) && b);

        private static float Num(JToken t)
        {
            if (t == null || t.Type == JTokenType.Null) return 0f;
            if (t.Type is JTokenType.Integer or JTokenType.Float) return (float)t;
            return float.TryParse(t.ToString(), NumberStyles.Float,
                                  CultureInfo.InvariantCulture, out var v) ? v : 0f;
        }

        private static List<JToken[]> Rows(JToken t)
        {
            var result = new List<JToken[]>();
            if (t is not JArray arr) return result;
            foreach (var item in arr)
                if (item is JArray row) result.Add(row.ToArray());
            return result;
        }

        // ── 리플렉션 (테이블 SO 는 전부 [SerializeField] private) ──
        private static void SetField(object target, string field, object value)
        {
            var f = target.GetType().GetField(field,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f == null) { Debug.LogError($"[Canon] 필드 없음: {target.GetType().Name}.{field}"); return; }
            f.SetValue(target, value);
        }

        private static object GetField(object target, string field)
        {
            var f = target.GetType().GetField(field,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f?.GetValue(target);
        }

        private static void RegisterAddressable(string path, string address)
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogWarning("[Canon] Addressable 설정 없음"); return; }
            var group = settings.FindGroup(Group);
            if (group == null) { Debug.LogWarning($"[Canon] 그룹 없음: {Group}"); return; }

            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}
