using System.IO;
using Game.Module.InGame;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 챕터 결과창 · 챕터 선택 · 상자 보상창을 **시안 자리대로** 세운다 (기획 2026-09-18).
    ///
    /// 시안: `Projects/AVSR/_exchange/in/ui_new_chapter_result_v1.png` · `ui_new_chapter_select_v1.png` ·
    /// `ui_new_chest_reward_v1.png` (720×1280). 아래 자리표는 시안에서 잰 값이다.
    ///
    /// 부품(글자 없는 그림 34장, 2026-09-18 납품)은 `_exchange/in/` 에서 가져와
    /// `Assets/BaseResource/ChapterScreens/` 에 두고 그것을 꽂는다. 없는 부품은 게임에 있던
    /// 비슷한 그림을 자리표시로 쓴다 — 부품을 다시 받으면 이 메뉴를 다시 돌리면 끝이다.
    ///
    /// 노드 이름 = 바인딩 키다. 코드(`ChapterSelectPanel` · `ChestRewardPopup` · `ChapterResultPopup`)는
    /// 이름으로만 찾으므로 여기서 이름을 바꾸면 거기도 바꾼다.
    /// </summary>
    public static class ChapterScreensBuilder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string InGamePrefab = "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab";
        private const string Incoming = "Projects/AVSR/_exchange/in";
        private const string PartsDir = "Assets/BaseResource/ChapterScreens";
        private const string Lobby = "Assets/BaseResource/LobbyMainUI/";
        private const string HostSel = "Assets/BaseResource/HostSelectPanel/";

        /// <summary>발주한 부품 → 오기 전까지 대신 쓸 그림. 앞이 부품 파일 이름이다.</summary>
        private static readonly (string part, string fallback)[] Parts =
        {
            ("result_frame", Lobby + "panelframe.png"),
            ("result_subplate", Lobby + "hudpill.png"),
            ("result_rowpanel", Lobby + "chestslotframe.png"),
            ("result_warnbar", Lobby + "hudpill.png"),
            ("reward_goldpile", Lobby + "goldicon.png"),
            ("result_title_ch1", null), ("result_title_ch2", null), ("result_title_ch3", null),
            ("result_title_ch4", null), ("result_title_ch5", null), ("result_title_ch6", null),
            ("button_yellow_wide", Lobby + "buttongold.png"),

            ("chapterselect_bg", HostSel + "hostselectbackground.png"),
            ("chapterselect_title", null),
            ("chaptercard_frame", HostSel + "hostslotframe.png"),
            ("chaptercard_frame_selected", HostSel + "hostslotframe_selected.png"),
            ("chaptercard_art_1", Lobby + "modeart_scenario.png"), ("chaptercard_art_2", Lobby + "modeart_scenario.png"),
            ("chaptercard_art_3", Lobby + "modeart_scenario.png"), ("chaptercard_art_4", Lobby + "modeart_scenario.png"),
            ("chaptercard_art_5", Lobby + "modeart_scenario.png"), ("chaptercard_art_6", Lobby + "modeart_scenario.png"),
            ("chaptercard_lock", Lobby + "modelockicon.png"),
            ("chapterselect_startbutton", Lobby + "buttongold.png"),
            ("button_back", Lobby + "modearrow_left.png"),

            ("chest_platinum", Lobby + "chest_magic.png"),
            ("chestreward_frame", Lobby + "panelframe.png"),
            ("chestreward_open_silver", Lobby + "chest_silver.png"),
            ("chestreward_open_gold", Lobby + "chest_gold.png"),
            ("chestreward_open_platinum", Lobby + "chest_magic.png"),
            ("rewardcard_gold", HostSel + "hostslotframe_selected.png"),
            ("rewardcard_b", HostSel + "hostslotframe.png"),
            ("rewardcard_a", HostSel + "hostslotframe.png"),
            ("rewardcard_s", HostSel + "hostslotframe_selected.png"),
        };

        private static TMP_FontAsset s_font;
        private static Material s_outline;
        private static Material s_plain;

        [MenuItem("Tools/Game/챕터 결과 · 선택 · 상자 보상 화면 세우기")]
        public static void Run()
        {
            int pulled = ImportIncoming();

            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                PickFont(lobby);
                var select = BuildChapterSelect(lobby.transform);
                var reward = BuildChestReward(lobby.transform);

                var so = new SerializedObject(lobby.GetComponent<LobbyMainUI>());
                so.FindProperty("_chapterSelectPanel").objectReferenceValue = select;
                so.FindProperty("_chestRewardPopup").objectReferenceValue = reward;
                // 로비 상자 칸의 백금 그림도 같이 갈아 끼운다
                var arts = so.FindProperty("_chestArts");
                for (int i = 0; i < arts.arraySize; i++)
                {
                    var e = arts.GetArrayElementAtIndex(i);
                    if (e.FindPropertyRelative("Key").stringValue == "platinum")
                        e.FindPropertyRelative("Sprite").objectReferenceValue = P("chest_platinum");
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }

            var ingame = PrefabUtility.LoadPrefabContents(InGamePrefab);
            try
            {
                BuildChapterResult(ingame.transform);
                PrefabUtility.SaveAsPrefabAsset(ingame, InGamePrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(ingame); }

            int have = 0;
            foreach (var (part, _) in Parts) if (File.Exists($"{PartsDir}/{part}.png")) have++;
            Debug.Log($"[챕터 화면] 세움 — 부품 {have}/{Parts.Length} (이번에 가져온 것 {pulled})");
        }

        // ── 챕터 선택 (시안 ui_new_chapter_select_v1) ─────────────
        //
        // 배경만 화면을 채우고, 나머지는 시안 한 장 판(`Content`) 안에 시안 좌표 그대로 담는다.
        // 판이 화면 가운데에 뜨므로 긴 폰에서는 위아래로, 태블릿에서는 좌우로 배경이 더 보인다.

        private static ChapterSelectPanel BuildChapterSelect(Transform root)
        {
            var panel = Fresh(root, "ChapterSelectPanel");
            Full(Img(Node(panel, "ChapterSelectBg"), P("chapterselect_bg")).rectTransform);
            var box = Content(panel, "ChapterSelectContent");

            Top(Img(Node(box, "ChapterSelectBackButton"), P("button_back")).rectTransform, -298, -52, 84, 60);
            Btn(box.Find("ChapterSelectBackButton"));

            var gold = Img(Node(box, "ChapterGoldPill"), L("hudpill")).rectTransform;
            Top(gold, -30, -38, 230, 46);
            Center(Img(Node(gold, "ChapterGoldIcon"), L("goldicon")).rectTransform, -88, 0, 42, 42);
            Center(Txt(Node(gold, "ChapterGoldText"), "0", 26, TextAlignmentOptions.MidlineRight).rectTransform,
                   20, 0, 150, 40);
            var gem = Img(Node(box, "ChapterGemPill"), L("hudpill")).rectTransform;
            Top(gem, 190, -38, 170, 46);
            Center(Img(Node(gem, "ChapterGemIcon"), L("gemicon")).rectTransform, -58, 0, 40, 36);
            Center(Txt(Node(gem, "ChapterGemText"), "0", 26, TextAlignmentOptions.MidlineRight).rectTransform,
                   20, 0, 100, 40);

            var title = Img(Node(box, "ChapterSelectTitle"), P("chapterselect_title"));
            Top(title.rectTransform, 0, -148, 600, 110);
            title.enabled = title.sprite != null;
            var titleText = Txt(Node(box, "ChapterSelectTitleText"), "CHAPTER SELECT", 50, TextAlignmentOptions.Center);
            Top(titleText.rectTransform, 0, -148, 600, 110);

            // 카드 여섯 장 — 2열 × 3행. 시안 중심 (185, 337) (533, 337) … 행 간격 283
            for (int i = 0; i < 6; i++)
            {
                int chapter = i + 1;
                float x = i % 2 == 0 ? -175f : 173f;
                float y = 303f - (i / 2) * 283f;
                var card = Node(box, $"ChapterCard{chapter}");
                Center(card, x, y, 330, 270);
                Btn(card);

                var art = Img(Node(card, "ChapterCardArt"), P($"chaptercard_art_{chapter}"));
                art.preserveAspect = false;
                Center(art.rectTransform, 0, -8, 300, 230);
                Center(Img(Node(card, "ChapterCardFrame"), P("chaptercard_frame")).rectTransform, 0, 0, 330, 270);
                Center(Txt(Node(card, "ChapterCardNoText"), $"CHAPTER {chapter}", 24, TextAlignmentOptions.Center)
                           .rectTransform, 0, 96, 240, 34);
                Center(Txt(Node(card, "ChapterCardNameText"), "", 21, TextAlignmentOptions.Center)
                           .rectTransform, 0, 61, 280, 32);
                Center(Img(Node(card, "ChapterCardLockIcon"), P("chaptercard_lock")).rectTransform, 0, -28, 64, 80);
                var chest = Img(Node(card, "ChapterCardChestIcon"), ChestOfChapter(chapter));
                chest.preserveAspect = true;
                Center(chest.rectTransform, 105, -88, 82, 56);
                foreach (var g in card.GetComponentsInChildren<Graphic>()) g.raycastTarget = g.transform == card;
            }

            var start = Img(Node(box, "ChapterStartButton"), P("chapterselect_startbutton")).rectTransform;
            start.anchorMin = start.anchorMax = new Vector2(0.5f, 0f);
            start.pivot = new Vector2(0.5f, 0.5f);
            start.anchoredPosition = new Vector2(0, 110);
            start.sizeDelta = new Vector2(380, 100);
            Btn(start);
            var startText = Txt(Node(start, "ChapterStartText"), "START", 42, TextAlignmentOptions.Center, plain: true);
            startText.color = new Color(0.12f, 0.1f, 0.05f);
            Center(startText.rectTransform, 20, 0, 170, 70);

            var comp = panel.gameObject.AddComponent<ChapterSelectPanel>();
            var so = new SerializedObject(comp);
            so.FindProperty("_frameNormal").objectReferenceValue = P("chaptercard_frame");
            so.FindProperty("_frameSelected").objectReferenceValue = P("chaptercard_frame_selected");
            var chests = so.FindProperty("_chapterChest");
            chests.arraySize = 6;
            for (int i = 0; i < 6; i++) chests.GetArrayElementAtIndex(i).objectReferenceValue = ChestOfChapter(i + 1);
            so.ApplyModifiedPropertiesWithoutUndo();
            return comp;
        }

        private static Sprite ChestOfChapter(int chapter)
            => chapter <= 2 ? L("chest_silver") : chapter <= 4 ? L("chest_gold") : P("chest_platinum");

        // ── 상자 보상창 (시안 ui_new_chest_reward_v1) ─────────────
        //
        // 화면 가운데 기준. 시안(1280)의 가운데 640 을 0 으로 잰 값이다.

        private static ChestRewardPopup BuildChestReward(Transform root)
        {
            var popup = Fresh(root, "ChestRewardPopup");
            var dim = Img(Node(popup, "RewardDim"), null);
            dim.color = new Color(0f, 0f, 0f, 0.95f);
            Full(dim.rectTransform);
            var box = Content(popup, "RewardContent");

            Center(Img(Node(box, "RewardFrame"), P("chestreward_frame")).rectTransform, 0, -135, 640, 880);
            var chest = Img(Node(box, "RewardChestArt"), P("chestreward_open_gold"));
            chest.preserveAspect = true;
            Center(chest.rectTransform, 0, 340, 420, 340);
            var header = Txt(Node(box, "RewardHeaderText"), "獲得:", 42, TextAlignmentOptions.Center);
            header.color = new Color(1f, 0.85f, 0.35f);
            Center(header.rectTransform, 0, 100, 300, 56);

            // 카드 칸 — 자리는 코드가 장 수에 맞춰 다시 잡는다(`ChestRewardPopup.Fill`)
            var grid = Node(box, "RewardGrid");
            Center(grid, 0, 0, 10, 10);
            for (int i = 0; i < 6; i++)
            {
                var card = Node(grid, $"RewardCard{i}");
                Center(card, i % 2 == 0 ? -112 : 112, -60 - (i / 2) * 217, 190, 190);
                var icon = Img(Node(card, "RewardCardIcon"), i == 0 ? P("reward_goldpile") : null);
                icon.preserveAspect = true;
                Center(icon.rectTransform, 0, 8, 160, 150);
                Center(Img(Node(card, "RewardCardFrame"), i == 0 ? P("rewardcard_gold") : P("rewardcard_b"))
                           .rectTransform, 0, 0, 190, 190);
                Center(Txt(Node(card, "RewardCardCountText"), "×0", 40, TextAlignmentOptions.Center).rectTransform,
                       0, -70, 170, 44);
            }

            var ok = Img(Node(box, "RewardOkButton"), P("button_yellow_wide")).rectTransform;
            Center(ok, 0, -478, 290, 80);
            Btn(ok);
            var okText = Txt(Node(ok, "RewardOkText"), "OK", 40, TextAlignmentOptions.Center, plain: true);
            okText.color = new Color(0.12f, 0.1f, 0.05f);
            Full(okText.rectTransform);

            var comp = popup.gameObject.AddComponent<ChestRewardPopup>();
            var so = new SerializedObject(comp);
            var arts = so.FindProperty("_openArts");
            string[] keys = { "silver", "gold", "platinum" };
            arts.arraySize = keys.Length;
            for (int i = 0; i < keys.Length; i++)
            {
                var e = arts.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = keys[i];
                e.FindPropertyRelative("Sprite").objectReferenceValue = P($"chestreward_open_{keys[i]}");
            }
            so.FindProperty("_cardGold").objectReferenceValue = P("rewardcard_gold");
            so.FindProperty("_cardB").objectReferenceValue = P("rewardcard_b");
            so.FindProperty("_cardA").objectReferenceValue = P("rewardcard_a");
            so.FindProperty("_cardS").objectReferenceValue = P("rewardcard_s");
            so.FindProperty("_goldPile").objectReferenceValue = P("reward_goldpile");
            so.ApplyModifiedPropertiesWithoutUndo();
            return comp;
        }

        // ── 챕터 클리어 결과창 (시안 ui_new_chapter_result_v1) ────
        //
        // 화면 가운데 기준. 시안(1280)의 가운데 640 을 0 으로 잰 값이다.

        private static void BuildChapterResult(Transform root)
        {
            PickFont(root.gameObject);
            var popup = Fresh(root, "ChapterResultPopup");
            var dim = Img(Node(popup, "ResultDim"), null);
            dim.color = new Color(0f, 0f, 0f, 0.88f);
            Full(dim.rectTransform);
            var box = Content(popup, "ResultContent");

            Center(Img(Node(box, "ResultFrame"), P("result_frame")).rectTransform, 0, 20, 660, 800);

            // ⚠ 납품 액자(660×800)에는 **제목 판 · 챕터 이름 판 · 큰 안쪽 판 · OK 자리가 그려져 있다.**
            //   시안 자리가 아니라 **액자 그림 속 칸**에 맞춘다 — 시안대로 두면 줄 판이 안쪽 판
            //   테두리와 겹쳐 선이 두 겹으로 보였다(2026-09-18). 액자 안 좌표(위에서 잰 값):
            //     제목 판 118~178 · 이름 판 195~255 · 안쪽 판 280~660 · OK 자리 670~760
            //   액자는 가운데 +20 에 놓이므로 액자 위 = 가운데 +420. 가운데 기준 = 420 − 액자 안 y.
            var title = Img(Node(box, "ResultTitleImage"), P("result_title_ch1"));
            Center(title.rectTransform, 0, 272, 680, 121);   // 납품 제목은 글자 둘레가 비어 있어 키워 판을 채운다
            var titleText = Txt(Node(box, "ResultTitleText"), "CHAPTER 1 CLEAR", 44, TextAlignmentOptions.Center);
            titleText.color = new Color(1f, 0.82f, 0.25f);
            Center(titleText.rectTransform, 0, 272, 480, 60);

            // 이름 판은 액자에 그려져 있다 — 글자만 얹는다(따로 발주한 판은 겹치므로 안 쓴다)
            Center(Txt(Node(box, "ResultSubText"), "", 30, TextAlignmentOptions.Center).rectTransform,
                   0, 195, 360, 56);

            var goldRow = Img(Node(box, "ResultGoldRow"), P("result_rowpanel")).rectTransform;
            Center(goldRow, 0, 62, 460, 140);
            var pile = Img(Node(goldRow, "ResultGoldIcon"), P("reward_goldpile"));
            pile.preserveAspect = true;
            Center(pile.rectTransform, -115, -3, 185, 130);
            Left(Txt(Node(goldRow, "ResultGoldLabelText"), "", 30, TextAlignmentOptions.MidlineLeft).rectTransform,
                 -10, 28, 230, 44);
            var value = Txt(Node(goldRow, "ResultGoldValueText"), "+0", 52, TextAlignmentOptions.MidlineLeft);
            value.color = new Color(1f, 0.85f, 0.3f);
            Left(value.rectTransform, -10, -20, 230, 64);

            var chestRow = Img(Node(box, "ResultChestRow"), P("result_rowpanel")).rectTransform;
            Center(chestRow, 0, -86, 460, 140);
            var chest = Img(Node(chestRow, "ResultChestArt"), L("chest_silver"));
            chest.preserveAspect = true;
            Center(chest.rectTransform, -105, 0, 170, 117);
            Left(Txt(Node(chestRow, "ResultChestNameText"), "", 32, TextAlignmentOptions.MidlineLeft).rectTransform,
                 10, 0, 210, 50);

            var warn = Img(Node(box, "ResultWarnBar"), P("result_warnbar"));
            if (!File.Exists($"{PartsDir}/result_warnbar.png")) warn.color = new Color(0.8f, 0.15f, 0.15f, 0.85f);
            Center(warn.rectTransform, 0, -192, 440, 46);
            // 띠 왼쪽 끝에 경고 삼각형이 그려져 있다 — 글자는 그 오른쪽 칸에만 쓴다
            var warnText = Txt(Node(warn.transform, "ResultWarnText"), "", 20, TextAlignmentOptions.Center);
            warnText.color = new Color(1f, 0.45f, 0.4f);
            Center(warnText.rectTransform, 30, 0, 340, 40);
            warnText.enableAutoSizing = true;
            warnText.fontSizeMin = 14;
            warnText.fontSizeMax = 20;

            var ok = Img(Node(box, "ResultOkButton"), P("button_yellow_wide")).rectTransform;
            Center(ok, 0, -295, 272, 76);
            Btn(ok);
            var okText = Txt(Node(ok, "ResultOkText"), "OK", 40, TextAlignmentOptions.Center, plain: true);
            okText.color = new Color(0.12f, 0.1f, 0.05f);
            Full(okText.rectTransform);

            var comp = popup.gameObject.AddComponent<ChapterResultPopup>();
            var so = new SerializedObject(comp);
            var titles = so.FindProperty("_titles");
            titles.arraySize = 6;
            for (int i = 0; i < 6; i++) titles.GetArrayElementAtIndex(i).objectReferenceValue = P($"result_title_ch{i + 1}");
            var arts = so.FindProperty("_chestArts");
            (string key, Sprite sprite)[] chests =
            {
                ("silver", L("chest_silver")), ("gold", L("chest_gold")), ("platinum", P("chest_platinum")),
            };
            arts.arraySize = chests.Length;
            for (int i = 0; i < chests.Length; i++)
            {
                var e = arts.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = chests[i].key;
                e.FindPropertyRelative("Sprite").objectReferenceValue = chests[i].sprite;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            popup.gameObject.SetActive(false);
        }

        // ── 부품 가져오기 ────────────────────────────────────────

        /// <summary>납품 폴더에 새로 온 부품을 가져온다. 이미 같은 것이면 건너뛴다.</summary>
        private static int ImportIncoming()
        {
            Directory.CreateDirectory(PartsDir);
            int n = 0;
            foreach (var (part, _) in Parts)
            {
                string src = $"{Incoming}/{part}.png", dst = $"{PartsDir}/{part}.png";
                if (!File.Exists(src)) continue;
                if (File.Exists(dst) && new FileInfo(src).Length == new FileInfo(dst).Length &&
                    System.Linq.Enumerable.SequenceEqual(File.ReadAllBytes(src), File.ReadAllBytes(dst))) continue;
                File.Copy(src, dst, true);
                EnsureSprite(dst);
                n++;
            }
            AssetDatabase.Refresh();
            return n;
        }

        private static void EnsureSprite(string path)
        {
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }

        /// <summary>부품이 왔으면 부품, 아니면 자리표시 그림.</summary>
        private static Sprite P(string part)
        {
            var got = AssetDatabase.LoadAssetAtPath<Sprite>($"{PartsDir}/{part}.png");
            if (got != null) return got;
            foreach (var (p, fallback) in Parts)
                if (p == part && fallback != null) return AssetDatabase.LoadAssetAtPath<Sprite>(fallback);
            return null;
        }

        private static Sprite L(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Lobby}{file}.png");

        // ── 노드 도우미 ──────────────────────────────────────────

        private static void PickFont(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.font == null || !t.fontSharedMaterial.name.Contains("Outline")) continue;
                s_font = t.font;
                s_outline = t.fontSharedMaterial;
                s_plain = t.font.material;
                return;
            }
        }

        /// <summary>있던 것을 지우고 새로 세운다 — 자리표를 고치고 다시 돌려도 찌꺼기가 안 남게.</summary>
        private static RectTransform Fresh(Transform root, string name)
        {
            var old = root.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var node = Node(root, name);
            Full(node);
            return node;
        }

        /// <summary>
        /// 시안 한 장(720×1280) 크기의 판을 **화면 가운데**에 둔다 — 배경 · 어둡게 막 말고는 전부 이 안에 담는다.
        ///
        /// 폰(720×1600) · 태블릿(960×1280) 어디서든 시안 그대로 가운데에 뜬다(2026-09-18 지시
        /// 「모든 UI 는 가운데」). `ScreenFitLock` 을 붙여 `ScreenFit` 이 이 판과 그 안을 가장자리로
        /// 끌어 붙이지 않게 한다 — 안 붙이면 카드는 위로, START 는 아래로 갈라졌다.
        /// </summary>
        private static RectTransform Content(Transform parent, string name)
        {
            var box = Node(parent, name);
            Center(box, 0, 0, 720, 1280);
            box.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();
            return box;
        }

        private static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image Img(Transform node, Sprite sprite)
        {
            var img = node.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        private static TextMeshProUGUI Txt(Transform node, string text, float size, TextAlignmentOptions align,
                                           bool plain = false)
        {
            var t = node.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = s_font;
            t.fontSharedMaterial = plain ? s_plain : s_outline;
            t.fontSize = size;
            t.text = text;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            t.fontStyle = FontStyles.Bold;   // 시안 글자는 전부 굵다
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        private static void Btn(Transform node)
        {
            var g = node.GetComponent<Graphic>();
            if (g == null) g = node.gameObject.AddComponent<Image>();
            g.raycastTarget = true;
            var b = node.gameObject.AddComponent<Button>();
            b.targetGraphic = g;
        }

        private static void Full(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        /// <summary>부모 가운데 기준 자리.</summary>
        private static void Center(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        /// <summary>부모 가운데 기준, 왼쪽 끝을 x 에 맞춘다(왼쪽 정렬 글자).</summary>
        private static void Left(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        /// <summary>화면 위 가운데 기준 — y 는 위에서 잰 중심(음수).</summary>
        private static void Top(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }
    }
}
