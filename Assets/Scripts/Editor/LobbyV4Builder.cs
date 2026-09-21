using System;
using System.IO;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 로비 v4 — 시안(Reference/Mockups/lobby_hub_v3_jp.png)을 **그대로** 세운다 (2026-09-21 지시: 「이 이미지랑 똑같이」).
    ///
    /// 그림은 시안 픽셀이다. `Projects/AVSR/Tools/lobby_v4_build.py` 가 시안에서
    ///   - 바뀌는 글자만 지운 바탕 두 장(위판 0~632 · 아래판 632~1672, 상자 카드는 빈 카드),
    ///   - 카드 속 부품(은 · 금 · 백금 상자 · 시간 판 · 시계) · 「プレイ」 뒤 ▶,
    ///   - 글자 자리 · 크기 · 색(lobby_v4_spec.json)
    /// 을 뽑아 `Assets/BaseResource/LobbyV4/` 에 둔다. 이 빌더는 그것을 프리팹에 꽂는다.
    ///
    /// 해상도: 위판은 화면 위에, 아래판은 화면 아래에 붙는다. 9:16 에서는 두 판이 맞닿아 시안 그대로이고,
    /// 더 긴 화면에서는 둘 사이가 벌어진다(그 틈은 `LobbyV4Gap` 이 메운다). 태블릿 양옆은 `SideLeft/Right`.
    ///
    /// 노드 이름 = `LobbyMainUI` 가 찾는 이름. 예전 로비 노드(v3 포함)는 지운다.
    /// </summary>
    public static class LobbyV4Builder
    {
        private const string Prefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Dir = "Assets/BaseResource/LobbyV4";
        private const string BoldFont = "Assets/BaseResource/Fonts/NotoSansKR-Bold SDF.asset";
        private const float S = 720f / 941f;
        /// <summary>
        /// 글자 칸 위아래 여유(시안 px). 칸은 시안 잉크 높이로 잡는데, 자동 줄이기는 줄 높이로 재서
        /// 여유가 없으면 시안 크기에서도 줄어든다. 세로는 잉크 기준 가운데라 여유를 둬도 자리는 그대로다.
        /// </summary>
        private const float Pad = 20f;

        /// <summary>시안 카드 순서 = 등급. 시안의 1 · 2 · 3번 카드가 은 · 금 · 백금이다.</summary>
        private static readonly string[] Grades = { "silver", "gold", "platinum" };

        [Serializable] private class TextSpec { public string name; public int[] box; public string color; public string align; public int[] area; public int size; }
        [Serializable] private class BoxSpec { public string name; public int[] box; public int card; }
        [Serializable] private class Spec { public int mockupW, mockupH, splitY, extendH, extendOverlap, sideW; public TextSpec[] texts; public BoxSpec[] parts, icons, slots; }
        [Serializable] private class CalibItem { public string name; public float dx, dy, scale = 1f, dilate, aspect = 1f; }
        [Serializable] private class Calib { public CalibItem[] items; }

        /// <summary>예전 로비 그림 노드 — 통째로 지운다.</summary>
        private static readonly string[] OldNodes =
        {
            "LobbyStageArea", "TopHudGroup", "GhostSearchPanel", "ChestBand", "GameModeGroup", "MainActionBar",
            "LobbyV3Backdrop", "LobbyV3Gap", "LobbyV3Top", "LobbyV3Bottom",
            "LobbyV4Gap", "LobbyV4Top", "LobbyV4Bottom",
        };

        /// <summary>고정 글자 → 언어 표 키. 숫자 · 시간 · 상자 칸은 코드가 채운다.</summary>
        private static readonly (string node, string key)[] Keys =
        {
            ("SeasonPassTitleText", "ui.lobby.season.title"), ("SeasonPassSubText", "ui.lobby.season.sub"),
            ("EventTitleText", "ui.lobby.event.title"), ("EventSubText", "ui.lobby.event.sub"),
            ("GhostSearchTitleText", "ui.lobby.search.title"), ("GhostSearchDescText", "ui.lobby.search.desc"),
            ("GameModeLabel", "ui.lobby.game_mode"),
            ("ModeScenarioTitleText", "ui.lobby.mode.scenario.name"), ("ModeScenarioSubText", "ui.lobby.mode.scenario.desc"),
            ("ModePlayButtonText", "ui.lobby.play"),
            ("ModeSurvivalTitleText", "ui.lobby.mode.survival.name"), ("ModeSurvivalSubText", "ui.lobby.mode.survival.desc"),
            ("ModeDefenseTitleText", "ui.lobby.mode.defense.name"), ("ModeDefenseSubText", "ui.lobby.mode.defense.desc"),
            ("HostButtonSubText", "ui.lobby.host_button.sub"), ("ChapterButtonSubText", "ui.lobby.game_mode"),
            ("ShopButtonSubText", "ui.lobby.shop_button.sub"),
        };

        /// <summary>언어와 무관한 글자 · 자리표시 글자(기능이 붙으면 코드가 덮는다).</summary>
        private static readonly (string node, string text)[] Fixed =
        {
            ("GoldText", "1,357"), ("GemText", "1,000,000"),
            ("GhostSearchTimerText", "04:32:18"), ("GhostSearchGoldText", "+12,640 G"),
        };

        /// <summary>
        /// 외곽선 있는 글자 — 시안 확대로 본 것(그림 위 글자 · 하단 PLAY 칸). 두께는 TMP 거리장 단위.
        /// ⚠ `HeavyText` 가 쥔다 — 언어가 바뀌면 재질이 통째로 바뀌어 글자 칸에 올린 외곽선은 사라진다.
        /// </summary>
        private static readonly (string node, float width, string color)[] Outlines =
        {
            ("GameModeLabel", 0.18f, "#0A1638"),
            ("ModeScenarioTitleText", 0.2f, "#0A2458"),
            ("ModeScenarioSubText", 0.16f, "#06203F"),
            ("ModeSurvivalTitleText", 0.2f, "#050C24"), ("ModeSurvivalSubText", 0.18f, "#050C24"),
            ("ModeDefenseTitleText", 0.2f, "#050C24"), ("ModeDefenseSubText", 0.18f, "#050C24"),
            ("ChapterButtonSubText", 0.14f, "#1466C8"),
        };

        /// <summary>눌리는 자리(시안 좌표) — 그림은 바탕에 있고 여기엔 투명한 판만 둔다. 뒤에 적은 것이 위에 온다.</summary>
        private static readonly (string node, int x0, int y0, int x1, int y1)[] Buttons =
        {
            ("GoldPlusButton", 494, 26, 540, 76), ("GemPlusButton", 719, 26, 765, 76),
            ("MailButton", 786, 16, 854, 82), ("SettingsButton", 864, 18, 930, 82),
            ("SeasonPassButton", 490, 107, 698, 196), ("EventButton", 715, 107, 913, 196),
            ("GhostSearchHelpButton", 274, 272, 316, 308),
            ("ModeScenarioCard", 30, 970, 910, 1205),
            ("ModePlayButton", 645, 1114, 887, 1188),
            ("ModeSurvivalCard", 34, 1214, 463, 1409), ("ModeDefenseCard", 476, 1214, 908, 1409),
            ("HostButton", 5, 1474, 301, 1668), ("ChapterButton", 305, 1471, 636, 1670),
            ("ShopButton", 640, 1474, 938, 1668),
        };

        /// <summary>왼쪽 정렬 글자 칸의 오른쪽 끝을 어디까지 넓혀도 되는가(시안 px) — 그 앞까지 다른 그림이 없다.</summary>
        private static readonly (string node, float x1)[] WideRight =
        {
            ("GameModeLabel", 420f),
            // 시나리오 글자 오른쪽엔 병사가 있다(x 400~) — 넘으면 줄인다
            ("ModeScenarioTitleText", 410f), ("ModeScenarioSubText", 400f),
            ("GhostSearchTitleText", 268f), ("GhostSearchTimerText", 318f), ("GhostSearchGoldText", 318f),
            ("GhostSearchDescText", 322f),
        };

        private static Spec s_spec;
        private static Calib s_calib;
        private static TMP_FontAsset s_bold;

        [MenuItem("Tools/Game/로비 v4 — 시안 그대로 세우기")]
        public static void Run()
        {
            var json = File.ReadAllText(Path.Combine(Dir, "lobby_v4_spec.json"));
            s_spec = JsonUtility.FromJson<Spec>(json);
            // 글자 보정표 — lobby_calib.py 가 스샷과 시안의 잉크를 재서 채운다(없으면 보정 없음)
            var calibPath = Path.Combine(Dir, "lobby_v4_calib.json");
            s_calib = File.Exists(calibPath) ? JsonUtility.FromJson<Calib>(File.ReadAllText(calibPath)) : null;
            s_bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFont);
            if (s_spec == null || s_bold == null) { Debug.LogError("[로비 v4] 스펙 · 굵은 폰트가 없다"); return; }
            ImportSprites();

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                foreach (var n in OldNodes)
                {
                    var old = root.transform.Find(n);
                    if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                }
                var version = root.transform.Find("VersionText");
                if (version != null) version.gameObject.SetActive(false);   // 시안에 없다

                int split = s_spec.splitY;
                // 긴 화면에서 두 판 사이에 뜨는 틈 — 위판 바로 아래에 바닥 연장 그림을 붙인다.
                // 아래판이 그 위를 덮으므로 9:16 에서는 안 보이고, 화면이 길어질수록 더 드러난다.
                var gap = Node(root.transform, "LobbyV4Gap");
                gap.anchorMin = gap.anchorMax = gap.pivot = new Vector2(0.5f, 1f);
                gap.anchoredPosition = new Vector2(0f, -(split - s_spec.extendOverlap) * S);   // 위판과 몇 줄 겹쳐 알파로 녹인다
                gap.sizeDelta = new Vector2(s_spec.mockupW * S, Mathf.Max(1, s_spec.extendH) * S);
                gap.gameObject.AddComponent<ScreenFitLock>();
                Img(gap, Spr("base_extend")).enabled = s_spec.extendH > 0;

                var top = Band(root.transform, "LobbyV4Top", Spr("base_top"), s_spec.mockupW, split, true);
                var bottom = Band(root.transform, "LobbyV4Bottom", Spr("base_bottom"), s_spec.mockupW,
                                  s_spec.mockupH - split, false);

                // 태블릿 양옆 — 판 바깥에 붙는 풍경(폰에서는 화면 밖이라 안 보인다)
                if (s_spec.sideW > 0)
                {
                    int sw = s_spec.sideW;
                    Side(top, "SideLeft", "side_left_top", -sw, split);
                    Side(top, "SideRight", "side_right_top", s_spec.mockupW, split);
                    Side(bottom, "SideLeft", "side_left_bottom", -sw, s_spec.mockupH - split);
                    Side(bottom, "SideRight", "side_right_bottom", s_spec.mockupW, s_spec.mockupH - split);
                }

                // 눌리는 자리
                foreach (var (node, x0, y0, x1, y1) in Buttons)
                {
                    bool isTop = y1 <= split;
                    var b = Place(isTop ? top : bottom, node, x0, y0 - (isTop ? 0 : split), x1, y1 - (isTop ? 0 : split));
                    var img = b.gameObject.AddComponent<Image>();
                    img.color = new Color(1f, 1f, 1f, 0f);
                    b.gameObject.AddComponent<Button>().targetGraphic = img;
                }

                // 상자 카드 — 글자보다 먼저(카드 버튼이 글자를 덮지 않게 글자가 뒤에 온다)
                BuildChests(bottom, split);

                // 글자
                foreach (var t in s_spec.texts)
                {
                    if (t.name.StartsWith("_")) continue;
                    bool isTop = t.box[3] <= split;
                    Text(isTop ? top : bottom, t, isTop ? 0 : split);
                }

                // 「プレイ ▶」 — 글자와 ▶ 를 버튼 밑으로. 자리는 LobbyMainUI 가 글자 끝에 맞춰 옮긴다
                var play = Find(root.transform, "ModePlayButton") as RectTransform;
                var playText = Find(root.transform, "ModePlayButtonText");
                playText.SetParent(play, true);
                var pb = Array.Find(Buttons, b => b.node == "ModePlayButton");
                var ab = Icon("playarrow");
                var arrow = Node(play, "ModePlayButtonArrow");
                TopLeft(arrow, new Rect((ab[0] - pb.x0) * S, -(ab[1] - pb.y0) * S, (ab[2] - ab[0]) * S, (ab[3] - ab[1]) * S));
                Img(arrow, Spr("playarrow"));

                BindLobby(root, pb.x0);

                // 판이 늦게 만들어져 맨 뒤로 가면 창(호스트 선택 등)을 덮는다 — 맨 앞으로 보낸다
                top.SetSiblingIndex(0); gap.SetSiblingIndex(1); bottom.SetSiblingIndex(2);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("[로비 v4] 세움 — 시안 그대로");
        }

        // ── 상자 카드 세 칸 ─────────────────────────────────────

        private static void BuildChests(RectTransform bottom, int split)
        {
            var band = Node(bottom, "ChestBand");
            Stretch(band);
            for (int i = 0; i < 3; i++)
            {
                var sb = SlotBox(i);
                var slot = Place(band, $"ChestSlot{i + 1}", sb[0], sb[1] - split, sb[2], sb[3] - split);
                var hit = slot.gameObject.AddComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0f);
                slot.gameObject.AddComponent<Button>().targetGraphic = hit;

                // 상자 — 등급마다 제 자리. 기본 그림은 시안 그 칸의 등급
                var rects = new Rect[Grades.Length];
                for (int g = 0; g < Grades.Length; g++) rects[g] = Moved(PartBox($"chest_{Grades[g]}"), g, i);
                var chest = Node(slot, "ChestArt");
                TopLeft(chest, rects[i]);
                Img(chest, Spr($"chest_{Grades[i]}")).preserveAspect = true;
                slot.gameObject.AddComponent<ChestSlotLayout>().Set(Grades, rects);

                // 시간 판 · 시계 — 1번 카드에서 뗀 것을 이 칸으로 옮긴다
                var plate = Node(slot, "ChestTimePlate");
                TopLeft(plate, Moved(PartBox("timeplate"), 0, i));
                Img(plate, Spr("timeplate"));
                var clock = Node(slot, "ChestTimeIcon");
                TopLeft(clock, Moved(Icon("clock"), 0, i));
                Img(clock, Spr("clock"));

                SlotText(slot, "ChestTimeText", $"_time{i + 1}", sb, "3時間 12分", false);
                SlotText(slot, "ChestTitleText", $"_title{i + 1}", sb, Localize($"chest.{Grades[i]}.name"), true);

                // 빈 칸 글자 — 시안에 없는 상태라 카드 가운데에 조용히 둔다(제목 글자와 같은 색)
                var title = Array.Find(s_spec.texts, x => x.name == $"_title{i + 1}");
                ColorUtility.TryParseHtmlString(title.color, out var ink);
                var empty = TextNode(slot, "ChestEmptyText", Localize("ui.lobby.chest.empty"), title.size * S, ink, true);
                var ert = empty.rectTransform;
                ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0.5f); ert.pivot = new Vector2(0.5f, 0.5f);
                ert.anchoredPosition = Vector2.zero; ert.sizeDelta = new Vector2((sb[2] - sb[0] - 30) * S, 40);
                empty.enableAutoSizing = true; empty.fontSizeMax = empty.fontSize; empty.fontSizeMin = empty.fontSize * 0.6f;
                empty.gameObject.AddComponent<LocalizedText>();
                SetKey(empty.gameObject, "ui.lobby.chest.empty");
            }
        }

        /// <summary>시안 <paramref name="fromCard"/> 번 카드의 부품을 <paramref name="toCard"/> 번 카드로 옮긴 자리(카드 기준 좌표). 폭 차이의 절반만큼 민다.</summary>
        private static Rect Moved(int[] box, int fromCard, int toCard)
        {
            var f = SlotBox(fromCard); var t = SlotBox(toCard);
            float dx = (t[0] - f[0]) + ((t[2] - t[0]) - (f[2] - f[0])) * 0.5f;
            return new Rect((box[0] + dx - t[0]) * S, -(box[1] - t[1]) * S, (box[2] - box[0]) * S, (box[3] - box[1]) * S);
        }

        private static int[] SlotBox(int i) => s_spec.slots[i].box;

        private static TextMeshProUGUI SlotText(RectTransform slot, string name, string specName, int[] src,
                                                string text, bool center)
        {
            var t = Array.Find(s_spec.texts, x => x.name == specName);
            var c = CalibOf(specName);
            var rt = Node(slot, name);
            var tmp = Style(rt, t, text, center, c);
            float x0 = center ? t.area[0] : t.box[0], x1 = t.area[2];
            TopLeft(rt, new Rect((x0 - src[0] + c.dx) * S, -(t.box[1] - src[1] + c.dy - Pad) * S,
                                 (x1 - x0) * S, (t.box[3] - t.box[1] + 2 * Pad) * S));
            FitWidth(tmp, center);
            return tmp;
        }

        // ── 글자 ────────────────────────────────────────────────

        private static void Text(RectTransform band, TextSpec t, int yOff)
        {
            bool center = t.align == "C";
            var c = CalibOf(t.name);
            float x0 = center ? t.area[0] : t.box[0];
            float x1 = t.area[2];
            // 오른쪽이 비어 있는 왼쪽 정렬 글자 — 다른 언어가 길 때 줄이지 않고 빈 곳까지 쓴다
            var wide = Array.Find(WideRight, w => w.node == t.name);
            if (wide.node != null && !center) x1 = Mathf.Max(x1, wide.x1);
            var rt = Place(band, t.name, x0 + c.dx, t.box[1] - yOff + c.dy - Pad, x1 + c.dx, t.box[3] - yOff + c.dy + Pad);
            string text = Array.Find(Fixed, f => f.node == t.name).text;
            var tmp = Style(rt, t, text ?? string.Empty, center, c);
            var key = Array.Find(Keys, k => k.node == t.name).key;
            if (!string.IsNullOrEmpty(key))
            {
                rt.gameObject.AddComponent<LocalizedText>();
                SetKey(rt.gameObject, key);
                tmp.text = Localize(key);
            }
            if (t.name == "GhostSearchDescText") tmp.lineSpacing = -2f;
            var outline = Array.Find(Outlines, o => o.node == t.name);
            if (outline.node != null)
            {
                ColorUtility.TryParseHtmlString(outline.color, out var oc);
                var so = new SerializedObject(rt.GetComponent<HeavyText>());
                so.FindProperty("_outlineWidth").floatValue = outline.width;
                so.FindProperty("_outlineColor").colorValue = oc;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            FitWidth(tmp, center);
        }

        /// <summary>
        /// 한국어(빌더 기본 글자)가 자동 줄이기에 걸리지 않게 칸 폭을 글자 폭 이상으로 넓힌다.
        /// 가운데 정렬 칸은 **보이는 가운데**를 지킨다.
        /// </summary>
        private static void FitWidth(TextMeshProUGUI tmp, bool center)
        {
            var rt = tmp.rectTransform;
            // 가운데 정렬 칸은 기준점을 가운데로 — 가로 비율(localScale.x)을 줄이면 기준점 쪽으로 줄어드는데,
            // 왼쪽 기준이면 줄인 만큼 가운데가 왼쪽으로 쏠려 보정이 매번 헛돌았다(2026-09-21)
            if (center && rt.pivot.x != 0.5f)
            {
                rt.pivot = new Vector2(0.5f, rt.pivot.y);
                rt.anchoredPosition += new Vector2(rt.sizeDelta.x * 0.5f, 0f);
            }
            float need = tmp.GetPreferredValues(tmp.text, 99999f, 99999f).x + 2f;
            // 넓힌다 — 가운데 정렬은 기준점이 가운데라 보이는 가운데가 그대로다
            if (need > rt.sizeDelta.x) rt.sizeDelta = new Vector2(need, rt.sizeDelta.y);
        }

        private static CalibItem CalibOf(string name)
            => Array.Find(s_calib?.items ?? Array.Empty<CalibItem>(), x => x.name == name) ?? new CalibItem();

        private static TextMeshProUGUI Style(RectTransform rt, TextSpec t, string text, bool center, CalibItem calib)
        {
            ColorUtility.TryParseHtmlString(t.color, out var c);
            var tmp = TextNodeOn(rt, text, t.size * S * calib.scale, c, center);
            // 다른 언어 글자가 길어 칸을 넘으면 줄인다
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = tmp.fontSize;
            tmp.fontSizeMin = tmp.fontSize * 0.6f;
            // 시안 글꼴 폭이 조금 다르다 — 가로만 눌러 폭을 맞춘다
            rt.localScale = new Vector3(calib.aspect, 1f, 1f);
            var so = new SerializedObject(rt.GetComponent<HeavyText>());
            so.FindProperty("_dilate").floatValue = calib.dilate;
            so.ApplyModifiedPropertiesWithoutUndo();
            return tmp;
        }

        private static TextMeshProUGUI TextNode(RectTransform parent, string name, string text, float size, Color c, bool center)
            => TextNodeOn(Node(parent, name), text, size, c, center);

        private static TextMeshProUGUI TextNodeOn(RectTransform rt, string text, float size, Color c, bool center)
        {
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = s_bold;
            tmp.fontSize = size;
            tmp.color = c;
            tmp.text = text;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.horizontalAlignment = center ? HorizontalAlignmentOptions.Center : HorizontalAlignmentOptions.Left;
            // 글자 모양 그대로의 위아래 가운데 — 시안 글자 칸(잉크 높이)에 딱 앉는다
            tmp.verticalAlignment = VerticalAlignmentOptions.Geometry;
            tmp.raycastTarget = false;
            rt.gameObject.AddComponent<HeavyText>();
            return tmp;
        }

        /// <summary>
        /// 빌더 기본 글자 = **시안 언어(일본어)**. 칸 폭을 이 글자로 잰다 — 한국어로 재면 더 긴 일본어가
        /// 칸을 넘어 자동 줄이기에 걸려, 보정을 몇 번 돌려도 글자가 커지지 않았다(2026-09-21).
        /// </summary>
        private static string Localize(string key)
        {
            var st = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/BundleResource/TableData/StringTable.asset");
            var e = st != null ? st.Find(key) : null;
            if (e == null) return key;
            return (e.Raw(Language.Japanese) ?? e.Korean).Replace("\\n", "\n");
        }

        private static void SetKey(GameObject go, string key)
        {
            var so = new SerializedObject(go.GetComponent<LocalizedText>());
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── LobbyMainUI 연결 ────────────────────────────────────

        private static void BindLobby(GameObject root, int playButtonX0)
        {
            var so = new SerializedObject(root.GetComponent<LobbyMainUI>());
            var arts = so.FindProperty("_chestArts");
            arts.arraySize = Grades.Length;
            for (int i = 0; i < Grades.Length; i++)
            {
                var e = arts.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = Grades[i];
                e.FindPropertyRelative("Sprite").objectReferenceValue = Spr($"chest_{Grades[i]}");
            }

            // 시계 → 시간 간격, 덩어리가 판 가운데에서 비낀 양 — 시안 1번 카드에서 잰다
            var clock = Icon("clock");
            var time = Array.Find(s_spec.texts, x => x.name == "_time1").box;
            var plate = PartBox("timeplate");
            so.FindProperty("_timeRowGap").floatValue = (time[0] - clock[2]) * S;
            so.FindProperty("_timeRowShift").floatValue = ((clock[0] + time[2]) * 0.5f - (plate[0] + plate[2]) * 0.5f) * S;

            // 「プレイ ▶」 — 글자 끝 → ▶ 간격, 덩어리 가운데(버튼 왼쪽 기준)
            var play = Array.Find(s_spec.texts, x => x.name == "ModePlayButtonText").box;
            var arrow = Icon("playarrow");
            so.FindProperty("_playArrowGap").floatValue = (arrow[0] - play[2]) * S;
            so.FindProperty("_playGroupCenter").floatValue = ((play[0] + arrow[2]) * 0.5f - playButtonX0) * S;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Side(RectTransform band, string name, string sprite, int x0, int h)
        {
            var rt = Place(band, name, x0, 0, x0 + s_spec.sideW, h);
            Img(rt, Spr(sprite));
            rt.SetAsFirstSibling();
        }

        // ── 판 · 노드 ───────────────────────────────────────────

        private static RectTransform Band(Transform root, string name, Sprite sprite, int w, int h, bool top)
        {
            var rt = Node(root, name);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w * S, h * S);
            rt.gameObject.AddComponent<ScreenFitLock>();
            Img(rt, sprite);
            return rt;
        }

        /// <summary>판 안에 시안 좌표로 놓는다(판의 왼쪽 위가 원점).</summary>
        private static RectTransform Place(RectTransform band, string name, float x0, float y0, float x1, float y1)
        {
            var rt = Node(band, name);
            TopLeft(rt, new Rect(x0 * S, -y0 * S, (x1 - x0) * S, (y1 - y0) * S));
            return rt;
        }

        private static void TopLeft(RectTransform rt, Rect r)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = r.position;
            rt.sizeDelta = r.size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image Img(RectTransform rt, Sprite sprite)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = Find(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        private static int[] PartBox(string name) => Array.Find(s_spec.parts, p => p.name == name).box;
        private static int[] Icon(string name) => Array.Find(s_spec.icons, p => p.name == name).box;

        private static Sprite Spr(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/{file}.png");

        private static void ImportSprites()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Dir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                if (ti.textureType == TextureImporterType.Sprite && ti.textureCompression == TextureImporterCompression.Uncompressed
                    && !ti.mipmapEnabled) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.filterMode = FilterMode.Bilinear;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.maxTextureSize = 2048;
                ti.SaveAndReimport();
            }
        }
    }
}
