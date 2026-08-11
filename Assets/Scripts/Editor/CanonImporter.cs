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

            int spawnTotal = rooms.Sum(r => ((SpawnEntry[])GetField(r, "_spawns")).Length);
            int objTotal = rooms.Sum(r => ((ObjectEntry[])GetField(r, "_objects")).Length);
            int waveTotal = rooms.Sum(r => ((WaveEntry[])GetField(r, "_waves")).Length);
            Debug.Log($"[Canon] 방 {rooms.Count}개 · 스폰 {spawnTotal}개 · 웨이브 {waveTotal}개 "
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
                    SetField(s, "_wave", (int)F(r, spF, "WaveIndex"));
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

                // 웨이브
                var wl = new List<WaveEntry>();
                foreach (var r in waves)
                {
                    if (S(r, wvF, "RoomID") != id) continue;
                    var e = new WaveEntry();
                    SetField(e, "_index", (int)F(r, wvF, "WaveIndex"));
                    SetField(e, "_startDelay", F(r, wvF, "StartDelay"));
                    SetField(e, "_composition", S(r, wvF, "Composition"));
                    SetField(e, "_clearRule", S(r, wvF, "ClearRule"));
                    wl.Add(e);
                }
                SetField(room, "_waves", wl.ToArray());

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

                // 방 타입은 22가지나 되고 이름만으로는 전투 방인지 알 수 없다
                // (Recovery·Route Choice·Build Choice 는 원래 적이 없다).
                // 대신 **웨이브가 있는데 스폰이 없는** 경우만 잡는다 — 그건 자기모순이다.
                if (wl.Count > 0 && list.Count == 0)
                    warnings.Add($"{id}({Str(lay[2])}): 웨이브는 있는데 적 스폰이 없다");

                // 웨이브 표와 실제 스폰이 어긋나는 방. 정본 자체의 불일치라 고치지 않고
                // 알리기만 한다 — 런타임은 스폰만 보므로 동작에는 영향이 없다.
                int wTable = wl.Count == 0 ? 1 : wl.Max(w => (int)GetField(w, "_index"));
                int wSpawn = list.Count == 0 ? 1 : list.Max(s => (int)GetField(s, "_wave"));
                if (wTable > wSpawn)
                    warnings.Add($"{id}: 웨이브 표는 {wTable}웨이브인데 스폰은 {wSpawn}웨이브뿐이다 "
                                 + "— 스폰을 따른다");

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
