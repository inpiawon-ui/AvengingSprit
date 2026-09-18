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
    /// 로비 v3 — 시안(lobby_hub_v2.png)을 **그대로** 세운다 (2026-09-18 지시: 「100% 똑같이, 토씨 하나 틀리지 말고」).
    ///
    /// 그림은 시안 픽셀이다. `Projects/AVSR/Tools/lobby_v3_build.py` 가 시안에서
    ///   - 바뀌는 글자만 지운 바탕 두 장(위판 0~620 · 아래판 620~1672),
    ///   - 상자 칸 속 부품(상자 · 시간 판 · 젬 버튼 · 금색 버튼 · 완료 띠 · 시계 · 젬),
    ///   - 글자 자리 · 크기 · 색(lobby_v3_spec.json)
    /// 을 뽑아 `Assets/BaseResource/LobbyV3/` 에 둔다. 이 빌더는 그것을 프리팹에 꽂는다.
    ///
    /// 해상도: 위판은 화면 위에, 아래판은 화면 아래에 붙는다. 9:16 에서는 두 판이 맞닿아 시안 그대로이고,
    /// 더 긴 화면에서는 둘 사이가 벌어진다(그 틈은 `LobbyV3Gap` 이 메운다).
    ///
    /// 노드 이름 = `LobbyMainUI` 가 찾는 이름. 예전 로비 노드는 지운다.
    /// </summary>
    public static class LobbyV3Builder
    {
        private const string Prefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Dir = "Assets/BaseResource/LobbyV3";
        private const string BoldFont = "Assets/BaseResource/Fonts/NotoSansKR-Bold SDF.asset";
        private const float S = 720f / 941f;
        /// <summary>
        /// 글자 칸 위아래 여유(시안 px). 칸은 시안 잉크 높이로 잡는데, 자동 줄이기는 줄 높이로 재서
        /// 여유가 없으면 시안 크기에서도 줄어든다. 세로는 잉크 기준 가운데라 여유를 둬도 자리는 그대로다.
        /// </summary>
        private const float Pad = 12f;

        [Serializable] private class TextSpec { public string name; public int[] box; public string color; public string align; public int[] area; public int size; }
        [Serializable] private class BoxSpec { public string name; public int[] box; }
        [Serializable] private class Spec { public int mockupW, mockupH, splitY, extendH, extendOverlap, sideW; public TextSpec[] texts; public BoxSpec[] parts, icons, slots; }
        [Serializable] private class CalibItem { public string name; public float dx, dy, scale = 1f, dilate, aspect = 1f; }
        [Serializable] private class Calib { public CalibItem[] items; }

        /// <summary>예전 로비 그림 노드 — 통째로 지운다.</summary>
        private static readonly string[] OldNodes =
        {
            "LobbyStageArea", "TopHudGroup", "GhostSearchPanel", "ChestBand", "GameModeGroup", "MainActionBar",
            "LobbyV3Backdrop", "LobbyV3Gap", "LobbyV3Top", "LobbyV3Bottom",
        };

        /// <summary>고정 글자 → 언어 표 키. 숫자 · 시간 · 모드 칸은 코드가 채운다.</summary>
        private static readonly (string node, string key)[] Keys =
        {
            ("SeasonPassTitleText", "ui.lobby.season.title"), ("SeasonPassSubText", "ui.lobby.season.sub"),
            ("EventTitleText", "ui.lobby.event.title"), ("EventSubText", "ui.lobby.event.sub"),
            ("GhostSearchTitleText", "ui.lobby.search.title"), ("GhostSearchDescText", "ui.lobby.search.desc"),
            ("GhostSearchClaimText", "ui.lobby.search.claim"), ("GameModeLabel", "ui.lobby.game_mode"),
            ("HostButtonSubText", "ui.lobby.host_button.sub"), ("ShopButtonSubText", "ui.lobby.shop_button.sub"),
            ("ChapterButtonSubText", "ui.lobby.game_mode"),
        };

        /// <summary>언어와 무관한 글자 · 자리표시 글자(기능이 붙으면 코드가 덮는다).</summary>
        private static readonly (string node, string text)[] Fixed =
        {
            ("GoldText", "125,680"), ("GemText", "2,340"),
            ("GhostSearchTimerText", "04:32:18"), ("GhostSearchGoldText", "+ 12,640 G"),
            ("HostButtonTitleText", "HOST"), ("ChapterButtonTitleText", "PLAY"), ("ShopButtonTitleText", "SHOP"),
            ("ModeSideLeftTitleText", "서바이벌 모드"), ("ModeSideLeftSubText", "끝까지 살아남아라"),
            ("ModeCenterTitleText", "시나리오 모드"), ("ModeCenterSubText", "영혼이 깃든 새로운 이야기"),
            ("ModePlayButtonText", "플레이하기"),
            ("ModeSideRightTitleText", "디펜스 모드"), ("ModeSideRightSubText", "몰려오는 적을 막아라"),
        };

        /// <summary>눌리는 자리(시안 좌표) — 그림은 바탕에 있고 여기엔 투명한 판만 둔다.</summary>
        private static readonly (string node, int x0, int y0, int x1, int y1)[] Buttons =
        {
            ("MailButton", 845, 12, 932, 90),
            ("SeasonPassButton", 500, 110, 702, 202), ("EventButton", 720, 110, 920, 202),
            ("GhostSearchHelpButton", 268, 252, 312, 294), ("GhostSearchClaimButton", 642, 506, 912, 592),
            ("ModeCardLeft", 40, 968, 266, 1382), ("ModeCardCenter", 282, 948, 660, 1388),
            ("ModeCardRight", 678, 968, 902, 1382),
            ("ModePlayButton", 305, 1296, 636, 1368),
            ("ModeArrowLeft", 0, 1126, 50, 1194), ("ModeArrowRight", 891, 1126, 941, 1194),
            ("HostButton", 34, 1490, 306, 1616), ("ChapterButton", 316, 1484, 628, 1620),
            ("ShopButton", 640, 1490, 908, 1616),
        };

        /// <summary>글자 노드를 어느 버튼 밑에 둘지 — 코드가 버튼 안에서 글자를 찾는다.</summary>
        private static readonly (string text, string parent, string rename)[] TextParents =
        {
            ("ModeSideLeftTitleText", "ModeCardLeft", "ModeTitleText"), ("ModeSideLeftSubText", "ModeCardLeft", "ModeSubText"),
            ("ModeSideRightTitleText", "ModeCardRight", "ModeTitleText"), ("ModeSideRightSubText", "ModeCardRight", "ModeSubText"),
            ("ModePlayButtonText", "ModePlayButton", null),
        };

        /// <summary>왼쪽 정렬 글자 칸의 오른쪽 끝을 어디까지 넓혀도 되는가(시안 px) — 그 앞까지 다른 그림이 없다.</summary>
        private static readonly (string node, float x1)[] WideRight =
        {
            ("GameModeLabel", 276f),   // 그 오른쪽은 MAIN 카드 윗모서리
            ("HostButtonTitleText", 296f), ("HostButtonSubText", 296f),
            ("ChapterButtonTitleText", 612f), ("ChapterButtonSubText", 612f),
            ("ShopButtonTitleText", 898f), ("ShopButtonSubText", 898f),
        };

        private static Spec s_spec;
        private static Calib s_calib;
        private static TMP_FontAsset s_bold;

        [MenuItem("Tools/Game/로비 v3 — 시안 그대로 세우기")]
        public static void Run()
        {
            var json = File.ReadAllText(Path.Combine(Dir, "lobby_v3_spec.json"));
            s_spec = JsonUtility.FromJson<Spec>(json);
            // 글자 보정표 — lobby_calib.py 가 스샷과 시안의 잉크를 재서 채운다(없으면 보정 없음)
            var calibPath = Path.Combine(Dir, "lobby_v3_calib.json");
            s_calib = File.Exists(calibPath) ? JsonUtility.FromJson<Calib>(File.ReadAllText(calibPath)) : null;
            s_bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFont);
            if (s_spec == null || s_bold == null) { Debug.LogError("[로비 v3] 스펙 · 굵은 폰트가 없다"); return; }
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
                // 긴 화면에서 두 판 사이에 뜨는 틈 — 위판 바로 아래에 골목 바닥 연장 그림을 붙인다.
                // 아래판이 그 위를 덮으므로 9:16 에서는 안 보이고, 화면이 길어질수록 더 드러난다.
                var gap = Node(root.transform, "LobbyV3Gap");
                gap.anchorMin = gap.anchorMax = gap.pivot = new Vector2(0.5f, 1f);
                gap.anchoredPosition = new Vector2(0f, -(split - s_spec.extendOverlap) * S);   // 위판과 몇 줄 겹쳐 알파로 녹인다
                gap.sizeDelta = new Vector2(s_spec.mockupW * S, Mathf.Max(1, s_spec.extendH) * S);
                gap.gameObject.AddComponent<ScreenFitLock>();
                Img(gap, Spr("base_extend")).enabled = s_spec.extendH > 0;

                var top = Band(root.transform, "LobbyV3Top", Spr("base_top"), s_spec.mockupW, split, true);
                var bottom = Band(root.transform, "LobbyV3Bottom", Spr("base_bottom"), s_spec.mockupW,
                                  s_spec.mockupH - split, false);

                // 태블릿 양옆 — 판 바깥에 붙는 골목 풍경(폰에서는 화면 밖이라 안 보인다)
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

                // 글자
                foreach (var t in s_spec.texts)
                {
                    if (t.name.StartsWith("_")) continue;
                    bool isTop = t.box[3] <= split;
                    Text(isTop ? top : bottom, t, isTop ? 0 : split);
                }
                foreach (var (text, parent, rename) in TextParents)
                {
                    var tt = Find(root.transform, text); var p = Find(root.transform, parent);
                    if (tt == null || p == null) continue;
                    tt.SetParent(p, true);
                    if (rename != null) tt.name = rename;
                }

                // 「플레이하기」 뒤 ▶ — 시안에서 뗀 그림. 자리는 LobbyMainUI 가 글자 끝에 맞춰 옮긴다
                var play = Find(root.transform, "ModePlayButton") as RectTransform;
                var ab = Icon("playarrow");
                var arrow = Node(play, "ModePlayButtonArrow");
                TopLeft(arrow, new Rect((ab[0] - 305) * S, -(ab[1] - 1296) * S, (ab[2] - ab[0]) * S, (ab[3] - ab[1]) * S));
                Img(arrow, Spr("playarrow"));

                BuildChests(bottom, split);
                BindLobby(root);

                // 판이 늦게 만들어져 맨 뒤로 가면 창(호스트 선택 등)을 덮는다 — 맨 앞으로 보낸다
                top.SetSiblingIndex(0); gap.SetSiblingIndex(1); bottom.SetSiblingIndex(2);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("[로비 v3] 세움 — 시안 그대로");
        }

        // ── 상자 세 칸 ──────────────────────────────────────────

        /// <summary>
        /// 칸마다 요소별 미세 자리(시안 px). 시안의 2번 칸은 1번 칸을 그대로 옮긴 것이 아니다 —
        /// 상자는 +5.5 · 시간 판 −3.3 · 젬 버튼 −4.7 처럼 요소마다 조금씩 다르다(위상 상관으로 잰 값, 2026-09-18).
        /// 3번 칸의 「세는 중」 과 1 · 2번 칸의 「완료」 는 시안에 없어 옮긴 값 그대로다.
        /// </summary>
        private static readonly (float chestX, float chestY, float plateX, float buttonX, float gemX, float costX, float labelX)[] Tweaks =
        {
            (0f, -0.9f, -0.1f, -0.8f, 0f, 0f, 0f),
            (5.5f, -1.0f, -3.3f, -4.7f, 2.5f, 3.1f, -5.4f),
            (0f, -0.9f, -0.1f, -0.8f, 0f, 0f, 0f),
        };
        // 완료 칸(3번 칸 시안) — 상자 · 금색 버튼 · 월계관 띠
        private const float ReadyChestY = -0.5f, ReadyButtonX = -0.4f, BannerX = 0.4f, BannerY = -0.4f;

        private static Rect Nudge(Rect r, float dx, float dy) => new Rect(r.x + dx * S, r.y - dy * S, r.width, r.height);

        private static void Nudge(Component c, float dx, float dy)
        {
            var rt = (RectTransform)c.transform;
            rt.anchoredPosition += new Vector2(dx * S, -dy * S);
        }

        private static void BuildChests(RectTransform bottom, int split)
        {
            var band = Node(bottom, "ChestBand");
            Stretch(band);
            var s1 = SlotBox(0); var s3 = SlotBox(2);
            for (int i = 0; i < 3; i++)
            {
                var sb = SlotBox(i);
                var slot = Place(band, $"ChestSlot{i + 1}", sb[0], sb[1] - split, sb[2], sb[3] - split);
                float dxC = ((sb[2] - sb[0]) - (s1[2] - s1[0])) * 0.5f;   // 1번 칸에서 뗀 부품 — 폭 차이의 절반만 민다
                float dxR = ((sb[2] - sb[0]) - (s3[2] - s3[0])) * 0.5f;   // 3번 칸에서 뗀 부품
                var tw = Tweaks[i];

                var chest = SlotImg(slot, "ChestArt", Part("chest_blue"), s1, dxC, Spr("chest_blue"));
                chest.preserveAspect = true;
                var plate = SlotImg(slot, "ChestTimePlate", Part("timeplate"), s1, dxC, Spr("timeplate"));
                // 시안에서 상자 발이 시간 판 윗변을 덮는다 — 상자를 판 뒤에 그린다
                chest.transform.SetSiblingIndex(plate.transform.GetSiblingIndex());
                Nudge(plate, tw.plateX, 0f);
                SlotImg(slot, "ChestTimeIcon", Icon("clock"), s1, dxC, Spr("clock"));
                SlotText(slot, "ChestTimeText", "_time1", s1, dxC, "3시간 12분", false);
                var button = SlotImg(slot, "ChestActionButton", Part("gembutton"), s1, dxC, Spr("gembutton"));
                button.raycastTarget = true;
                button.gameObject.AddComponent<Button>().targetGraphic = button;
                Nudge(SlotImg(slot, "ChestActionGemIcon", Icon("gem"), s1, dxC, Spr("gem")), tw.gemX, 0f);
                // 젬 값은 제 칸 가운데 — 시안 「1,000」(165~231) · 「800」 의 가운데가 199.75 로 같다. 칸 = 165 ~ 234.5
                var cost = SlotText(slot, "ChestActionCostText", "_cost1", s1, dxC, "1,000", true);
                var cc = CalibOf("_cost1");
                TopLeft(cost.rectTransform, new Rect((165f - s1[0] + dxC + cc.dx) * S, cost.rectTransform.anchoredPosition.y,
                                                     (234.5f - 165f) * S, cost.rectTransform.sizeDelta.y));
                FitWidth(cost, true);
                Nudge(cost, tw.costX, 0f);
                Nudge(SlotText(slot, "ChestActionLabelText", "_label1", s1, dxC, "즉시 열기", true), tw.labelX, 0f);
                Nudge(SlotImg(slot, "ChestReadyBanner", Part("readybanner"), s3, dxR, Spr("readybanner")), BannerX, BannerY);
                var ready = SlotText(slot, "ChestReadyText", "_ready", s3, dxR, "완료!", true);
                ready.gameObject.AddComponent<LocalizedText>();
                SetKey(ready.gameObject, "ui.lobby.chest.ready");
                SlotText(slot, "ChestReadyLabelText", "_claim", s3, dxR, "보상 획득하기", true);

                // 빈 칸 글자 — 시안에 없는 상태라 칸 가운데에 조용히 둔다
                var empty = TextNode(slot, "ChestEmptyText", "빈 칸", 26, new Color32(0x8F, 0xA8, 0xC8, 255), true);
                var ert = empty.rectTransform;
                ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0.5f); ert.pivot = new Vector2(0.5f, 0.5f);
                ert.anchoredPosition = Vector2.zero; ert.sizeDelta = new Vector2(200, 40);
                empty.gameObject.AddComponent<LocalizedText>();
                SetKey(empty.gameObject, "ui.lobby.chest.empty");

                // 상태별 자리 — 세는 중(1번 칸 모양) · 완료(3번 칸 모양)
                var layout = slot.gameObject.AddComponent<ChestSlotLayout>();
                layout.Set(Nudge(SlotRect(Part("chest_blue"), s1, dxC), tw.chestX, tw.chestY),
                           Nudge(SlotRect(Part("chest_black"), s3, dxR), 0f, ReadyChestY),
                           Nudge(SlotRect(Part("gembutton"), s1, dxC), tw.buttonX, 0f),
                           Nudge(SlotRect(Part("goldbutton"), s3, dxR), ReadyButtonX, 0f));
            }
        }

        private static int[] SlotBox(int i) => s_spec.slots[i].box;

        private static Rect SlotRect(int[] box, int[] slot, float dx)
            => new Rect((box[0] - slot[0] + dx) * S, -(box[1] - slot[1]) * S, (box[2] - box[0]) * S, (box[3] - box[1]) * S);

        private static Image SlotImg(RectTransform slot, string name, int[] box, int[] src, float dx, Sprite sprite)
        {
            var rt = Node(slot, name);
            TopLeft(rt, SlotRect(box, src, dx));
            return Img(rt, sprite);
        }

        private static TextMeshProUGUI SlotText(RectTransform slot, string name, string specName, int[] src,
                                                float dx, string text, bool center)
        {
            var t = Array.Find(s_spec.texts, x => x.name == specName);
            var c = CalibOf(specName);
            var rt = Node(slot, name);
            var tmp = Style(rt, t, text, center, c);
            float x0 = center ? t.area[0] : t.box[0], x1 = t.area[2];
            TopLeft(rt, new Rect((x0 - src[0] + dx + c.dx) * S, -(t.box[1] - src[1] + c.dy - Pad) * S,
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
            if (t.name == "GhostSearchDescText")
            {
                tmp.lineSpacing = -2f;
            }
            FitWidth(tmp, center);
        }

        /// <summary>
        /// 한국어(시안 글자)가 자동 줄이기에 걸리지 않게 칸 폭을 한국어 글자 폭 이상으로 넓힌다.
        /// 가로 비율(localScale.x)을 줄인 칸은 줄이기 전 폭으로 재므로 시안 칸보다 넓게 잡힌다(2026-09-18).
        /// 가운데 정렬 칸은 **보이는 가운데**를 지킨다.
        /// </summary>
        private static void FitWidth(TextMeshProUGUI tmp, bool center)
        {
            var rt = tmp.rectTransform;
            float need = tmp.GetPreferredValues(tmp.text, 99999f, 99999f).x + 2f;
            float w = rt.sizeDelta.x;
            if (need <= w) return;
            float sx = rt.localScale.x;
            var p = rt.anchoredPosition;
            if (center) p.x += (w - need) * sx * 0.5f;
            rt.anchoredPosition = p;
            rt.sizeDelta = new Vector2(need, rt.sizeDelta.y);
        }

        private static CalibItem CalibOf(string name)
            => Array.Find(s_calib?.items ?? Array.Empty<CalibItem>(), x => x.name == name) ?? new CalibItem();

        private static TextMeshProUGUI Style(RectTransform rt, TextSpec t, string text, bool center, CalibItem calib)
        {
            ColorUtility.TryParseHtmlString(t.color, out var c);
            var tmp = TextNodeOn(rt, text, t.size * S * calib.scale, c, center);
            // 다른 언어 글자가 길어 칸을 넘으면 줄인다 — 한국어(시안 글자)는 칸 안이라 제 크기 그대로다
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = tmp.fontSize;
            tmp.fontSizeMin = tmp.fontSize * 0.6f;
            // 시안 글꼴이 조금 좁다 — 가로만 눌러 폭을 맞춘다
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

        private static string Localize(string key)
        {
            var st = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/BundleResource/TableData/StringTable.asset");
            var e = st != null ? st.Find(key) : null;
            return e != null ? e.Korean.Replace("\\n", "\n") : key;
        }

        private static void SetKey(GameObject go, string key)
        {
            var so = new SerializedObject(go.GetComponent<LocalizedText>());
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── LobbyMainUI 연결 ────────────────────────────────────

        private static void BindLobby(GameObject root)
        {
            var so = new SerializedObject(root.GetComponent<LobbyMainUI>());
            so.FindProperty("_chestButtonBlue").objectReferenceValue = Spr("gembutton");
            so.FindProperty("_chestButtonGold").objectReferenceValue = Spr("goldbutton");
            var arts = so.FindProperty("_chestArts");
            (string key, Sprite s)[] map =
            {
                ("silver", Spr("chest_blue")), ("gold", Spr("chest_black")),
                ("platinum", Spr("chest_purple")),   // 다른 화면의 백금 상자(chest_magic)도 보라색이다
            };
            arts.arraySize = map.Length;
            for (int i = 0; i < map.Length; i++)
            {
                var e = arts.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = map[i].key;
                e.FindPropertyRelative("Sprite").objectReferenceValue = map[i].s;
            }
            // 금 상자는 세는 중에 빛살 없는 그림을 쓴다(있을 때만)
            var calm = Spr("chest_black_calm");
            for (int i = 0; i < map.Length; i++)
                arts.GetArrayElementAtIndex(i).FindPropertyRelative("Counting").objectReferenceValue =
                    map[i].key == "gold" ? calm : null;
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

        private static int[] Part(string name) => Array.Find(s_spec.parts, p => p.name == name).box;
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















