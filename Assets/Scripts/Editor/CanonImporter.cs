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
            Debug.Log($"[Canon] 방 {rooms.Count}개 · 스폰 {spawnTotal}개 → {OutPath} "
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
            var entryExit = Rows(json["entryExit"]);
            var waves = Rows(json["waves"]);

            var eeF = Fields(schema, "entryExit");
            var spF = Fields(schema, "layout.enemySpawns");
            var wvF = Fields(schema, "waves");

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
                    SetField(room, "_exit", new Vector2(F(ee, eeF, "ExitX"), F(ee, eeF, "ExitY")));
                    SetField(room, "_nextRoomIds", NextRooms(S(ee, eeF, "NextRoomID")));
                    SetField(room, "_unlockRule", S(ee, eeF, "UnlockRule"));
                }

                // 보스는 enemySpawns 에 없다 — 페이즈마다 자리가 바뀌므로 따로 있다.
                // 여기서 안 집으면 보스방이 텅 빈 방으로 들어온다.
                var boss = bossLayouts.FirstOrDefault(r => Str(r[0]) == id);
                if (boss != null)
                {
                    SetField(room, "_bossId", Str(boss[1]));
                    SetField(room, "_bossAt", new Vector2(Num(boss[5]), Num(boss[6])));
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

                // 방 타입은 22가지나 되고 이름만으로는 전투 방인지 알 수 없다
                // (Recovery·Route Choice·Build Choice 는 원래 적이 없다).
                // 대신 **웨이브가 있는데 스폰이 없는** 경우만 잡는다 — 그건 자기모순이다.
                if (wl.Count > 0 && list.Count == 0)
                    warnings.Add($"{id}({Str(lay[2])}): 웨이브는 있는데 적 스폰이 없다");

                result.Add(room);
            }

            // 다음 방이 실재하는가 — 끊기면 런이 그 자리에서 막힌다
            foreach (var r in result)
            {
                var id = (string)GetField(r, "_roomId");
                foreach (var next in (string[])GetField(r, "_nextRoomIds"))
                    if (!seen.Contains(next))
                        errors.Add($"{id}: 다음 방 {next} 이 없다");
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

        private static string[] NextRooms(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
            return raw.Split('|')
                      .Select(s => s.Trim())
                      .Where(s => !Terminals.Contains(s))
                      .ToArray();
        }

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
