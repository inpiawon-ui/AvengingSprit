using System.Collections.Generic;
using Game.Character;
using Game.Module.InGame;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 방 지형 편집기 — 48방의 엄폐물·해저드를 눈으로 보고 옮긴다.
    ///
    /// 왜 만들었나: 정본(ROOM_GEOMETRY)은 `Template` 과 `CoverCount` 만 준다.
    /// 엄폐물이 **어디에 얼마만 한 크기로** 서는지는 정본에 없어서 임포터가 지어냈고,
    /// 그 좌표가 코드 안에 숨어 있어 실제로 플레이해 보기 전에는 아무도 확인할 수 없었다.
    /// 그래서 적이 기둥 속에 갇히거나(스폰 121곳), 벽이 방을 막아 진행이 끊기는(틈 2.7px)
    /// 사고가 플레이 중에야 발견됐다.
    ///
    /// 이 창은 배치를 **데이터로 꺼내 놓고** 검사까지 함께 보여 준다.
    /// 손으로 고친 방은 `HandEdited` 가 켜져 임포터가 덮지 않는다.
    /// </summary>
    public sealed class RoomMapWindow : EditorWindow
    {
        private const string TablePath = "Assets/BundleResource/TableData/RoomTable.asset";

        /// <summary>
        /// 발밑 판정 반폭. `BattleDirector.FootHalf` 와 같은 식이라야 검사가 맞다.
        ///
        /// ⚠ 숫자를 박아 두지 않는다. 유닛 크기는 `GameConfig.UnitScale` 에서 오고
        ///   그 값이 바뀔 때마다 여기를 같이 고치는 것을 잊으면, 검사가 실제와 어긋나
        ///   통과 못 할 방을 통과로 본다(1.5 → 1.05 로 줄일 때 실제로 그럴 뻔했다).
        /// </summary>
        private static float FootHalfPx
        {
            get
            {
                var cfg = AssetDatabase.LoadAssetAtPath<Game.Character.GameConfig>(
                    "Assets/BundleResource/TableData/GameConfig.asset");
                float scale = cfg != null ? cfg.UnitScale : 1f;
                // `BattleDirector.FootHalf` 와 같은 식 — 그림 폭의 절반, 하한 21 px.
                return Mathf.Max(96f * scale * 0.5f * 0.5f, 21f);
            }
        }

        private RoomTable _table;
        private HostTable _hosts;
        private SerializedObject _so;
        private int _roomIndex;
        private int _selected = -1;
        private Vector2 _listScroll, _sideScroll;
        private bool _showRanges = true;
        private bool _showSpawns = true;
        private bool _dragging;

        [MenuItem("Tools/Game/방 지형 편집기")]
        public static void Open()
        {
            var w = GetWindow<RoomMapWindow>("방 지형");
            w.minSize = new Vector2(1040f, 680f);
        }

        private void OnEnable() => Reload();

        private void Reload()
        {
            _table = AssetDatabase.LoadAssetAtPath<RoomTable>(TablePath);
            _hosts = AssetDatabase.LoadAssetAtPath<HostTable>(
                "Assets/BundleResource/TableData/HostTable.asset");
            _so = _table != null ? new SerializedObject(_table) : null;
        }

        // ── 화면 ──────────────────────────────────────────────────

        private void OnGUI()
        {
            if (_table == null || _so == null)
            {
                EditorGUILayout.HelpBox("RoomTable 을 찾지 못했습니다.", MessageType.Error);
                if (GUILayout.Button("다시 읽기")) Reload();
                return;
            }

            _so.Update();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRoomList();
                DrawCanvas();
                DrawSidePanel();
            }
            _so.ApplyModifiedProperties();
        }

        private SerializedProperty Rooms => _so.FindProperty("_rooms");
        private SerializedProperty Room => Rooms.GetArrayElementAtIndex(_roomIndex);
        private SerializedProperty Objects => Room.FindPropertyRelative("_objects");

        private void DrawRoomList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(210f)))
            {
                EditorGUILayout.LabelField($"방 {Rooms.arraySize}개", EditorStyles.boldLabel);
                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                for (int i = 0; i < Rooms.arraySize; i++)
                {
                    var r = _table.Rooms[i];
                    if (r == null) continue;

                    // 검사에 걸리는 방은 목록에서 바로 보이게 한다 — 48방을 하나씩
                    // 눌러 봐야 알 수 있으면 없는 것과 같다.
                    string flag = Problems(r).Count > 0 ? "⚠ " : "   ";
                    string hand = r.HandEdited ? " ✎" : string.Empty;
                    var style = i == _roomIndex ? EditorStyles.boldLabel : EditorStyles.label;
                    if (GUILayout.Button($"{flag}{r.RoomId.Replace("ROOM_", "")}  {r.Type}{hand}",
                                         style))
                    {
                        _roomIndex = i;
                        _selected = -1;
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>방 그림이 놓일 자리와 미터→화면 배율.</summary>
        private Rect _canvas;
        private float _scale;

        private void DrawCanvas()
        {
            var r = _table.Rooms[_roomIndex];
            using (new EditorGUILayout.VerticalScope())
            {
                var area = GUILayoutUtility.GetRect(10f, 10f, GUILayout.ExpandWidth(true),
                                                    GUILayout.ExpandHeight(true));
                // 방을 통째로 담을 배율. 여백을 조금 두어 가장자리 물체가 잘리지 않게 한다.
                _scale = Mathf.Min((area.width - 24f) / r.Width, (area.height - 24f) / r.Height);
                float w = r.Width * _scale, h = r.Height * _scale;
                _canvas = new Rect(area.x + (area.width - w) * 0.5f,
                                   area.y + (area.height - h) * 0.5f, w, h);

                EditorGUI.DrawRect(area, new Color(0.13f, 0.14f, 0.17f));
                EditorGUI.DrawRect(_canvas, new Color(0.09f, 0.11f, 0.15f));
                DrawGrid(r);
                if (_showRanges) DrawRanges(r);
                DrawObjects(r);
                if (_showSpawns) DrawSpawns(r);
                DrawEntryAndExits(r);
                HandleMouse(r);
            }
        }

        private Vector2 ToScreen(RoomEntry r, Vector2 meters)
            => new(_canvas.x + meters.x * _scale,
                   // 방 좌표는 아래가 0 이다. 화면은 위가 0 이라 뒤집는다.
                   _canvas.yMax - meters.y * _scale);

        private Vector2 ToMeters(RoomEntry r, Vector2 screen)
            => new((screen.x - _canvas.x) / _scale, (_canvas.yMax - screen.y) / _scale);

        private void DrawGrid(RoomEntry r)
        {
            Handles.color = new Color(1f, 1f, 1f, 0.06f);
            for (int x = 1; x < Mathf.CeilToInt(r.Width); x++)
            {
                float sx = _canvas.x + x * _scale;
                Handles.DrawLine(new Vector3(sx, _canvas.y), new Vector3(sx, _canvas.yMax));
            }
            for (int y = 1; y < Mathf.CeilToInt(r.Height); y++)
            {
                float sy = _canvas.yMax - y * _scale;
                Handles.DrawLine(new Vector3(_canvas.x, sy), new Vector3(_canvas.xMax, sy));
            }
        }

        /// <summary>
        /// 적 사거리를 원으로 깐다. **밸런스는 이 그림에서 읽힌다** —
        /// 원이 방을 다 덮으면 어디로 피해도 사거리 안이라는 뜻이다.
        /// </summary>
        private void DrawRanges(RoomEntry r)
        {
            if (_hosts == null) return;
            for (int i = 0; i < r.Spawns.Count; i++)
            {
                var s = r.Spawns[i];
                var h = FindHost(s.ActorId);
                if (h == null || h.CanonRange <= 0f) continue;
                var c = ToScreen(r, s.At);
                Handles.color = new Color(0.95f, 0.35f, 0.3f, 0.10f);
                Handles.DrawSolidDisc(c, Vector3.forward, h.CanonRange * _scale);
            }
        }

        private HostEntry FindHost(string key)
        {
            if (_hosts == null || string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < _hosts.Entries.Count; i++)
                if (_hosts.Entries[i] != null && _hosts.Entries[i].HostKey == key)
                    return _hosts.Entries[i];
            return null;
        }

        private static Color ColorOf(string kind) => kind switch
        {
            "PILLAR"    => new Color(0.55f, 0.50f, 0.68f, 0.85f),
            "BARRICADE" => new Color(0.62f, 0.52f, 0.42f, 0.85f),
            "LOW_COVER" => new Color(0.46f, 0.53f, 0.62f, 0.85f),
            "DIVIDER"   => new Color(0.45f, 0.44f, 0.58f, 0.85f),
            "HAZARD"    => new Color(0.88f, 0.36f, 0.20f, 0.45f),
            _           => new Color(0.5f, 0.5f, 0.55f, 0.85f),
        };

        private void DrawObjects(RoomEntry r)
        {
            for (int i = 0; i < r.Objects.Count; i++)
            {
                var o = r.Objects[i];
                var rect = RectOf(r, o);
                EditorGUI.DrawRect(rect, ColorOf(o.Kind));

                if (i == _selected)
                {
                    Handles.color = new Color(1f, 0.85f, 0.25f);
                    Handles.DrawSolidRectangleWithOutline(rect, Color.clear, Handles.color);
                }
                var label = new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
                string tall = o.BlocksShot && !o.BlocksEnemyShot ? "낮음" : o.BlocksShot ? "높음" : "";
                GUI.Label(rect, $"{o.Kind}\n{o.Size.x:0.0}×{o.Size.y:0.0} {tall}", label);
            }
        }

        /// <summary>
        /// 장애물 축소 배율. `BattleDirector.ObstacleViewScale` 과 **같아야 한다** —
        /// 어긋나면 이 창에 보이는 크기와 실제 게임 화면이 달라진다.
        /// </summary>
        private const float ObstacleScale = 0.7f;

        private Rect RectOf(RoomEntry r, ObjectEntry o)
        {
            var half = o.Size * ObstacleScale * 0.5f * _scale;
            var c = ToScreen(r, o.At);
            return new Rect(c.x - half.x, c.y - half.y, half.x * 2f, half.y * 2f);
        }

        private void DrawSpawns(RoomEntry r)
        {
            for (int i = 0; i < r.Spawns.Count; i++)
            {
                var s = r.Spawns[i];
                var c = ToScreen(r, s.At);
                // 웨이브 1 과 증원은 색을 나눈다. 증원 자리를 미리 알아야 배치를 판단할 수 있다.
                Handles.color = new Color(0.95f, 0.45f, 0.4f, 0.9f);
                Handles.DrawSolidDisc(c, Vector3.forward, 5f);
                if (s.IsPossessionTarget)
                {
                    Handles.color = new Color(0.4f, 0.85f, 1f);
                    Handles.DrawWireDisc(c, Vector3.forward, 9f);
                }
            }
        }

        private void DrawEntryAndExits(RoomEntry r)
        {
            var e = ToScreen(r, r.PlayerSpawn);
            Handles.color = new Color(0.4f, 0.9f, 0.5f);
            Handles.DrawSolidDisc(e, Vector3.forward, 7f);

            for (int i = 0; i < r.Exits.Count; i++)
            {
                var g = ToScreen(r, r.Exits[i].At);
                var rect = new Rect(g.x - 16f, g.y - 8f, 32f, 16f);
                EditorGUI.DrawRect(rect, new Color(0.35f, 0.7f, 0.95f, 0.9f));
            }
        }

        // ── 끌어 옮기기 ────────────────────────────────────────────

        private void HandleMouse(RoomEntry r)
        {
            var ev = Event.current;
            if (ev.type == EventType.MouseDown && _canvas.Contains(ev.mousePosition))
            {
                _selected = -1;
                for (int i = r.Objects.Count - 1; i >= 0; i--)
                    if (RectOf(r, r.Objects[i]).Contains(ev.mousePosition)) { _selected = i; break; }
                _dragging = _selected >= 0;
                Repaint();
            }
            else if (ev.type == EventType.MouseDrag && _dragging && _selected >= 0)
            {
                var m = ToMeters(r, ev.mousePosition);
                var o = Objects.GetArrayElementAtIndex(_selected);
                var size = o.FindPropertyRelative("_size").vector2Value;
                // 방 밖으로 나가지 않게 자른다. 반쯤 걸친 엄폐물은 벽으로 보인다.
                o.FindPropertyRelative("_at").vector2Value = new Vector2(
                    Mathf.Clamp(m.x, size.x * 0.5f, r.Width - size.x * 0.5f),
                    Mathf.Clamp(m.y, size.y * 0.5f, r.Height - size.y * 0.5f));
                MarkHandEdited();
                ev.Use();
                Repaint();
            }
            else if (ev.type == EventType.MouseUp) _dragging = false;
        }

        private void MarkHandEdited() => Room.FindPropertyRelative("_handEdited").boolValue = true;

        // ── 오른쪽 패널 ────────────────────────────────────────────

        private void DrawSidePanel()
        {
            var r = _table.Rooms[_roomIndex];
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(300f)))
            {
                _sideScroll = EditorGUILayout.BeginScrollView(_sideScroll);

                EditorGUILayout.LabelField(r.RoomId, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{r.Type} · {r.Template} · {r.Width:0.#}×{r.Height:0.#}m");
                EditorGUILayout.LabelField($"적 {r.Spawns.Count}기 · 엄폐물 {r.Objects.Count}개");
                if (r.HandEdited)
                    EditorGUILayout.HelpBox("손으로 배치한 방. 정본 임포트가 지형을 덮지 않습니다.",
                                            MessageType.Info);

                EditorGUILayout.Space(4f);
                _showRanges = EditorGUILayout.ToggleLeft("적 사거리 보기", _showRanges);
                _showSpawns = EditorGUILayout.ToggleLeft("스폰 자리 보기", _showSpawns);

                DrawProblems(r);
                DrawSelected(r);
                DrawButtons(r);

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawProblems(RoomEntry r)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("검사", EditorStyles.boldLabel);
            var problems = Problems(r);
            if (problems.Count == 0)
                EditorGUILayout.HelpBox("걸리는 것 없음", MessageType.None);
            else
                foreach (var p in problems) EditorGUILayout.HelpBox(p, MessageType.Warning);
        }

        /// <summary>
        /// 플레이해 보기 전에 잡아야 하는 것들. 여기 걸린 것이 실제로 사고가 났던 항목이다.
        /// </summary>
        private List<string> Problems(RoomEntry r)
        {
            var list = new List<string>();
            if (r == null) return list;

            float pxPerM = 720f / Mathf.Max(0.01f, r.Width);
            float needPx = FootHalfPx * 2f;

            for (int i = 0; i < r.Objects.Count; i++)
            {
                var o = r.Objects[i];
                if (!o.BlocksMove) continue;

                // ① 몸이 지나갈 틈이 있는가 (ROOM_CH1_004 가 여기서 막혔다)
                float halfPx = o.Size.x * ObstacleScale * 0.5f * pxPerM, cx = o.At.x * pxPerM;
                float gap = Mathf.Max(cx - halfPx, 720f - (cx + halfPx));
                if (gap < needPx)
                    list.Add($"{o.Kind}: 좌우 최대 틈 {gap:0}px — 몸이 지나가려면 {needPx:0}px 필요");

                // ② 스폰이 엄폐물 안에 있는가 (탄이 먹혀 방이 안 끝난다)
                for (int j = 0; j < r.Spawns.Count; j++)
                {
                    var s = r.Spawns[j];
                    if (Mathf.Abs(s.At.x - o.At.x) <= o.Size.x * 0.5f &&
                        Mathf.Abs(s.At.y - o.At.y) <= o.Size.y * 0.5f)
                        list.Add($"적 {s.ActorId} 이 {o.Kind} 안에 있다 — 탄이 막혀 안 죽는다");
                }

                // ③ 입구가 엄폐물 안인가
                if (Mathf.Abs(r.PlayerSpawn.x - o.At.x) <= o.Size.x * 0.5f &&
                    Mathf.Abs(r.PlayerSpawn.y - o.At.y) <= o.Size.y * 0.5f)
                    list.Add($"입구가 {o.Kind} 안에 있다");
            }

            // ④ 적 사거리가 방을 다 덮는가 — 피할 자리가 없다는 뜻이다
            if (_hosts != null && r.Spawns.Count > 0)
            {
                float maxRange = 0f;
                for (int i = 0; i < r.Spawns.Count; i++)
                {
                    var h = FindHost(r.Spawns[i].ActorId);
                    if (h != null) maxRange = Mathf.Max(maxRange, h.CanonRange);
                }
                if (maxRange >= r.Width)
                    list.Add($"적 사거리 {maxRange:0.0}m 가 방 폭 {r.Width:0.#}m 이상 — 좌우로는 피할 곳이 없다");
            }

            // ⑤ 입구에서 출구까지 **실제로 걸어갈 수 있는가**
            //
            // ⚠ 손으로 고친 방은 `HandEdited` 가 켜져 임포터가 건드리지 않는다 —
            //   임포터에 있는 통행 검사(`RoomImporterV33.Walkable`)도 함께 건너뛴다.
            //   그래서 여기서 한 번 더 본다. 줄 단위로 빈칸이 있어도 그 빈칸들이
            //   서로 안 이어지면 방이 막힌다(자동 배치에서 실제로 10방이 그랬다).
            if (!Reachable(r))
                list.Add("입구에서 출구까지 못 간다 — 지형이 길을 막았다");

            return list;
        }

        /// <summary>
        /// 런타임과 같은 판정으로 방을 넓이우선 탐색한다.
        /// 몸은 **그림 중심** 좌표로 움직이고, 발판은 그림 밑변에 붙어 있다.
        /// </summary>
        private static bool Reachable(RoomEntry r)
        {
            const float Step = 0.25f;
            var cfg = AssetDatabase.LoadAssetAtPath<Game.Character.GameConfig>(
                "Assets/BundleResource/TableData/GameConfig.asset");
            float us = cfg != null ? cfg.UnitScale : 1f;
            float bw = 96f * us, bh = 92f * us;                       // 숙주 기준
            float hx = Mathf.Max(bw * 0.25f, 21f) / 72f;              // 발판 반크기(m)
            float hy = Mathf.Max(bh * 0.16f, 14f) / 72f;
            float drop = (bh * 0.5f) / 72f - hy;                      // 그림중심 → 발판중심
            float halfX = bw * 0.5f / 72f, halfY = bh * 0.5f / 72f;

            var bx = new List<Vector4>();
            for (int i = 0; i < r.Objects.Count; i++)
            {
                var o = r.Objects[i];
                if (!o.BlocksMove) continue;
                bx.Add(new Vector4(o.At.x, o.At.y,
                                   o.Size.x * ObstacleScale * 0.5f,
                                   o.Size.y * ObstacleScale * 0.5f));
            }

            int W = Mathf.RoundToInt(r.Width / Step), H = Mathf.RoundToInt(r.Height / Step);
            bool Free(int gx, int gy)
            {
                float px = gx * Step, py = gy * Step;
                if (px < halfX || px > r.Width - halfX) return false;
                if (py < halfY || py > r.Height - halfY) return false;
                float fy = py - drop;
                for (int i = 0; i < bx.Count; i++)
                {
                    var b = bx[i];
                    if (Mathf.Abs(px - b.x) < b.z + hx && Mathf.Abs(fy - b.y) < b.w + hy) return false;
                }
                return true;
            }

            int sx = Mathf.RoundToInt(r.PlayerSpawn.x / Step);
            int sy = Mathf.RoundToInt(r.PlayerSpawn.y / Step);
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
            return topY >= r.Height - 3.0f;   // 문 구역(위 2.5 m) 바로 아래까지
        }

        private void DrawSelected(RoomEntry r)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("선택한 엄폐물", EditorStyles.boldLabel);
            if (_selected < 0 || _selected >= Objects.arraySize)
            {
                EditorGUILayout.LabelField("(그림에서 눌러 고르세요)");
                return;
            }

            var o = Objects.GetArrayElementAtIndex(_selected);
            EditorGUI.BeginChangeCheck();
            var kind = o.FindPropertyRelative("_kind");
            int k = Mathf.Max(0, System.Array.IndexOf(Kinds, kind.stringValue));
            k = EditorGUILayout.Popup("종류", k, Kinds);
            var at = EditorGUILayout.Vector2Field("자리(m)", o.FindPropertyRelative("_at").vector2Value);
            var size = EditorGUILayout.Vector2Field("크기(m)", o.FindPropertyRelative("_size").vector2Value);
            bool move = EditorGUILayout.Toggle("이동 막음", o.FindPropertyRelative("_blocksMove").boolValue);
            bool shot = EditorGUILayout.Toggle("탄 막음", o.FindPropertyRelative("_blocksShot").boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                kind.stringValue = Kinds[k];
                o.FindPropertyRelative("_at").vector2Value = at;
                o.FindPropertyRelative("_size").vector2Value = size;
                o.FindPropertyRelative("_blocksMove").boolValue = move;
                o.FindPropertyRelative("_blocksShot").boolValue = shot;
                MarkHandEdited();
            }

            if (GUILayout.Button("이 엄폐물 지우기"))
            {
                Objects.DeleteArrayElementAtIndex(_selected);
                _selected = -1;
                MarkHandEdited();
            }
        }

        private static readonly string[] Kinds =
            { "PILLAR", "BARRICADE", "LOW_COVER", "DIVIDER", "HAZARD" };

        private void DrawButtons(RoomEntry r)
        {
            EditorGUILayout.Space(10f);
            if (GUILayout.Button("엄폐물 추가"))
            {
                int n = Objects.arraySize;
                Objects.InsertArrayElementAtIndex(n);
                var o = Objects.GetArrayElementAtIndex(n);
                o.FindPropertyRelative("_objectId").stringValue = $"COVER_{n + 1}";
                o.FindPropertyRelative("_kind").stringValue = "PILLAR";
                o.FindPropertyRelative("_at").vector2Value = new Vector2(r.Width * 0.5f, r.Height * 0.5f);
                o.FindPropertyRelative("_size").vector2Value = new Vector2(1.2f, 1.2f);
                o.FindPropertyRelative("_blocksMove").boolValue = true;
                o.FindPropertyRelative("_blocksShot").boolValue = true;
                o.FindPropertyRelative("_hazardKind").stringValue = "NONE";
                o.FindPropertyRelative("_hazardDamage").intValue = 0;
                o.FindPropertyRelative("_hazardTick").floatValue = 0f;
                _selected = n;
                MarkHandEdited();
            }

            if (GUILayout.Button("이 방을 정본 자동배치로 되돌리기"))
            {
                Room.FindPropertyRelative("_handEdited").boolValue = false;
                _so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_table);
                AssetDatabase.SaveAssets();
                RoomImporterV33.Import();
                Reload();
                _selected = -1;
            }

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("저장"))
            {
                _so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_table);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("전체 검사", EditorStyles.boldLabel);
            int bad = 0;
            for (int i = 0; i < _table.Rooms.Count; i++)
                if (Problems(_table.Rooms[i]).Count > 0) bad++;
            EditorGUILayout.LabelField(bad == 0 ? "48방 모두 이상 없음" : $"걸리는 방 {bad}개");
        }
    }
}
