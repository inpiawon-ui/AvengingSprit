using System.IO;
using Game.Character;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 챕터 + 호스트를 한 창에서 고르는 판을 세운다 (기획 2026-09-29 · 사용자 제공 시안).
    ///
    /// ⚠ **화면 그림은 시안 그 자체다.** 액자·버튼·라벨·배경을 새로 그리지 않는다 —
    ///   새로 그리면 아무리 맞춰도 다른 물건이 된다(2026-09-29 네 번 반려).
    ///   `ch_screen.png` 는 시안에서 **값이 바뀌는 글자·그림만 지운 것**이고,
    ///   게임은 그 위에 제 값만 얹는다. 그래서 틀·글자 두께·버튼 모양이 시안과 같다.
    ///   (만드는 스크립트: `scratchpad/gen/makescreenbg.py`)
    ///
    /// 자리값은 전부 **시안 픽셀(1024 × 1536)** 이고, `720 / 1024 = 0.703125` 를 곱해 쓴다.
    /// 좌표를 적을 때는 <see cref="T"/>(화면 기준) · <see cref="TR"/>/<see cref="TL"/>(부모 기준)를 쓴다.
    ///
    /// 노드 이름 = 바인딩 키이며 <see cref="ChapterHostPanel"/> 이 이름으로만 찾는다.
    /// </summary>
    public static class ChapterHostBuilder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string PartsDir = "Assets/BaseResource/ChapterHost";
        private const string Lobby = "Assets/BaseResource/LobbyMainUI/";
        private const string Growth = "Assets/BaseResource/Growth/";
        private const string UnitDir = "Assets/BaseResource/Unit";
        private const string BossTablePath = "Assets/BundleResource/TableData/BossTable.asset";
        private const string FontPath = "Assets/BaseResource/Fonts/NotoSansKR-Bold SDF.asset";

        /// <summary>시안(1080 폭) → 게임 판(720 폭) 배율.</summary>
        ///
        /// 시안이 **9:16(1080x1920)** 으로 다시 그려졌다(2026-10-01).
        /// 예전 2:3 시안은 9:16 에 넣으려고 벽 띠를 거울로 되풀이해 늘렸는데,
        /// 같은 배관 줄이 대여섯 번 반복돼 가짜 티가 났다 — 이제 **늘리지 않는다.**
        /// 1080 x 0.6667 = 720 · 1920 x 0.6667 = 1280 으로 화면에 딱 맞는다.
        private const float S = 720f / 1080f;
        private const float DesignW = 1080f, DesignH = 1920f;

        /// <summary>
        /// 배경 그림의 바깥 여백 — 좌우 180 · 위아래 240 (시안 픽셀).
        /// 기기 비율이 달라 더 넓거나 더 긴 화면에서만 드러나는 자리다.
        /// **가장자리를 늘려 때우지 않고 새로 그려 받았다**(v5) — 늘린 것은 뿌옇게 번진다.
        ///   16:9 1080x1920 · 4:3 1440x1920 · 20:9 1080x2400 을 모두 덮는다.
        /// </summary>
        private const float PadX = 180f, PadY = 240f;

        private static TMP_FontAsset s_font;

        [MenuItem("Tools/Game/챕터·호스트 선택 창 세우기")]
        public static void Run()
        {
            s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                var panel = Build(lobby.transform);
                Bind(panel);

                var so = new SerializedObject(lobby.GetComponent<LobbyMainUI>());
                var field = so.FindProperty("_chapterHostPanel");
                if (field != null) field.objectReferenceValue = panel;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }

            bool screen = File.Exists($"{PartsDir}/ch_screen.png");
            Debug.Log($"[챕터·호스트] 세움 — 시안 배율 {S:0.000} · 화면 그림 {(screen ? "있음" : "**없음**")}");
        }

        private static ChapterHostPanel Build(Transform root)
        {
            var panel = Fresh(root, "ChapterHostPanel");

            var dim = Img(Node(panel, "CHDim"), null);
            dim.color = new Color(0f, 0f, 0f, 1f);
            dim.raycastTarget = true;          // 뒤 로비가 눌리지 않게 막는다
            Full(dim.rectTransform);

            var box = Content(panel, "CHContent");

            // 화면 = 시안 그림 한 장. 9:16 으로 그려져 있어 그대로 깔면 꽉 찬다.
            var screen = Img(Node(box, "CHScreen"), P("ch_screen"));
            Center(screen.rectTransform, 0f, 0f, (DesignW + 2f * PadX) * S, (DesignH + 2f * PadY) * S);

            TopBar(box);
            ChapterBox(box);
            HostBox(box);
            BottomRow(box);

            return panel.gameObject.AddComponent<ChapterHostPanel>();
        }

        // ── 상단 — 칸과 아이콘은 시안에 이미 있다. 값과 누를 자리만 얹는다 ──

        private static void TopBar(Transform box)
        {
            // 실측(v4) — 칸 y 62~140 (가운데 101) · 다이아 칸 x 172~460 · 금화 칸 x 470~762
            Hit(box, "CHBackButton", 108, 101, 96, 82);
            Hit(box, "CHMailButton", 821, 101, 100, 82);
            Hit(box, "CHSettingsButton", 966, 101, 96, 82);

            // 칸과 아이콘은 **시안에 이미 그려져 있다**(다이아 칸도 포함). 숫자만 얹는다.
            Num(box, "CHGemText", 262, 101, 190);
            Num(box, "CHGoldText", 570, 101, 200);
        }

        /// <summary>재화 숫자 한 칸. 칸·아이콘은 시안 것이고 숫자만 왼쪽 정렬로 얹는다.</summary>
        private static void Num(Transform box, string name, float x, float y, float w)
        {
            var t = Txt(Node(box, name), "0", F(34), TextAlignmentOptions.Left);
            TL2(t.rectTransform, x, y, w, 50);
            t.enableAutoSizing = true;
            t.fontSizeMin = F(20);
            t.fontSizeMax = F(34);
        }

        // ── 챕터 칸 ────────────────────────────────────────

        private static void ChapterBox(Transform box)
        {
            // 실측(v4) — 판 y 334~652 · 바깥 x 57~1022
            Hit(box, "CHPrevButton", 110, 383, 64, 64);
            Hit(box, "CHNextButton", 462, 383, 64, 64);

            // 「CHAPTER」 글자는 시안에 있다(x 148~330). 번호만 쓴다(x 343~420 · y 362~402)
            var no = Txt(Node(box, "CHChapterNoText"), "01", F(50), TextAlignmentOptions.Left);
            TL2(no.rectTransform, 341, 382, 118, 62);
            no.color = new Color(0.36f, 0.78f, 1f);

            // 챕터 이름 — 실측 y 418~500
            var name = Txt(Node(box, "CHChapterNameText"), "", F(74), TextAlignmentOptions.Left);
            TL2(name.rectTransform, 85, 459, 380, 96);
            name.enableAutoSizing = true;
            name.fontSizeMin = F(42);
            name.fontSizeMax = F(74);

            // 설명 — TopLeft 는 칸 위쪽부터 그려진다. 글자 위가 518 이 되게 칸 가운데를 556 에 둔다
            var desc = Txt(Node(box, "CHChapterDescText"), "", F(25), TextAlignmentOptions.TopLeft);
            TL2(desc.rectTransform, 85, 556, 390, 76);
            desc.textWrappingMode = TextWrappingModes.Normal;
            desc.lineSpacing = 12f;
            desc.color = new Color(0.86f, 0.90f, 0.96f);

            // 챕터 그림 — 실측 x 473~1024 · y 334~652
            var artBox = T(Node(box, "CHChapterArtBox"), 748, 493, 550, 316);
            artBox.gameObject.AddComponent<RectMask2D>();
            var art = Img(Node(artBox, "CHChapterArt"), null);
            art.preserveAspect = true;
            Center(art.rectTransform, 0, 0, 550 * S, 316 * S);
            T(Img(Node(box, "CHLockIcon"), L("modelockicon")).rectTransform, 748, 493, 120, 150);

            // 보스 얼굴 — 시안의 빈 틀(x 84~226 · y 654~788) 안
            var portrait = Img(Node(box, "CHBossPortrait"), null);
            portrait.preserveAspect = true;
            T(portrait.rectTransform, 154, 721, 132, 124);
            // 「BOSS」 는 시안 글자(y 681~714). 보스 이름만 그 아래(y 735~757)에 쓴다
            var bossName = Txt(Node(box, "CHBossNameText"), "", F(32), TextAlignmentOptions.Left);
            TL2(bossName.rectTransform, 282, 746, 180, 44);
            bossName.enableAutoSizing = true;
            bossName.fontSizeMin = F(18);
            bossName.fontSizeMax = F(32);

            // 클리어 보상 — 「클리어 보상」 글자와 동전(x 484~534)은 시안 것
            var gold = Txt(Node(box, "CHRewardGoldText"), "", F(36), TextAlignmentOptions.Left);
            TL2(gold.rectTransform, 552, 736, 196, 48);
            gold.color = new Color(1f, 0.85f, 0.32f);
            gold.enableAutoSizing = true;
            gold.fontSizeMin = F(22);
            gold.fontSizeMax = F(36);

            var chest = Img(Node(box, "CHRewardChestIcon"), L("chest_gold"));
            chest.preserveAspect = true;
            T(chest.rectTransform, 819, 727, 118, 128);
            var chestText = Txt(Node(box, "CHRewardChestText"), "", F(28), TextAlignmentOptions.Left);
            TL2(chestText.rectTransform, 862, 746, 160, 44);
            chestText.enableAutoSizing = true;
            chestText.fontSizeMin = F(18);
            chestText.fontSizeMax = F(28);
        }

        // ── 호스트 줄 ──────────────────────────────────────

        private static void HostBox(Transform box)
        {
            // 실측 — 첫 칸 x 85 · 폭 144 · 사이 10 · 여섯 장 914 · 카드 가운데 y 1099
            var view = T(Node(box, "CHHostViewport"), 542, 1063, 912, 266);
            view.gameObject.AddComponent<RectMask2D>();

            var content = Node(view, "CHHostContent");
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(ChapterHostLayout.ViewWidth, ChapterHostLayout.CardHeight);

            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            for (int i = 0; i < ChapterHostLayout.CardSlots; i++) Card(content, i);

            Hit(box, "CHHostPrev", 66, 1063, 58, 110);
            Hit(box, "CHHostNext", 1016, 1063, 58, 110);
        }

        /// <summary>
        /// 카드 한 장. **빈 칸은 시안에 이미 그려져 있다** — 여기서는 안에 들어갈 것만 만든다.
        /// 고른 칸만 금색 액자를 덮어 표시한다.
        /// </summary>
        private static void Card(Transform content, int index)
        {
            var card = Node(content, $"CHHostCard{index}");
            card.sizeDelta = new Vector2(ChapterHostLayout.CardWidth, ChapterHostLayout.CardHeight);
            Btn(card);

            // 빈 액자 — 시안에서 떼어 온 것이다. **카드에 붙어 같이 흐른다**.
            // 잘라 온 그림(148×266)에는 바깥 빛이 들어 있어, 칸 크기(140×258)에 맞춰 그린다 —
            // 칸보다 크게 그리면 보이는 창 가장자리에서 잘린다.
            Center(Img(Node(card, "CardFrame"), P("ch_card_frame")).rectTransform, 0, 0,
                   ChapterHostLayout.CardWidth, ChapterHostLayout.CardHeight);

            var thumb = Img(Node(card, "CardThumb"), null);
            thumb.preserveAspect = true;
            Center(thumb.rectTransform, 0, 51 * S, 134 * S, 160 * S);

            // 실측(카드 가운데 1063 기준) — 이름 +48 · 별 +77 · 전투력 +109
            var name = Txt(Node(card, "CardName"), "", F(26), TextAlignmentOptions.Center);
            Center(name.rectTransform, 0, -48 * S, 138 * S, 34 * S);
            name.enableAutoSizing = true;
            name.fontSizeMin = F(12);
            name.fontSizeMax = F(26);

            for (int s = 0; s < 5; s++)
            {
                // 실측 — 별 다섯이 카드 가운데 기준 -50 에서 24.6 간격, 한 개 22
                var star = Img(Node(card, $"CardStar{s}"), P("ch_star_off"));
                star.preserveAspect = true;
                Center(star.rectTransform, (-50f + s * 24.6f) * S, -77 * S, 24 * S, 24 * S);
            }

            var powerIcon = Img(Node(card, "CardPowerIcon"), P("ch_power_icon"));
            powerIcon.preserveAspect = true;
            Center(powerIcon.rectTransform, -50 * S, -109 * S, 26 * S, 26 * S);
            var power = Txt(Node(card, "CardPowerText"), "", F(28), TextAlignmentOptions.Left);
            Left(power.rectTransform, -20 * S, -109 * S, 112 * S, 38 * S);

            // 고른 표시 — 금색 액자와 체크. 둘 다 시안에서 잘라 온 것이다
            var sel = Img(Node(card, "CardFrameSel"), P("ch_card_frame_sel"));
            Center(sel.rectTransform, 0, 0, ChapterHostLayout.CardWidth + 8f, ChapterHostLayout.CardHeight + 8f);
            var check = Img(Node(card, "CardCheck"), P("ch_check"));
            check.preserveAspect = true;
            Center(check.rectTransform, 47 * S, 112 * S, 46 * S, 42 * S);
        }

        // ── 아래 — 랜덤 칸과 도전 버튼 ──────────────────────

        private static void BottomRow(Transform box)
        {
            // 실측(v4) — 랜덤 판 y 1217~1461 / 버튼 노란판 x 270~806 · y 1729~1855
            var random = T(Node(box, "CHRandomCard"), 540, 1339, 960, 240);
            Btn(random);
            var check = Img(Node(random, "RandomCheck"), P("ch_check"));
            check.preserveAspect = true;
            TR(check.rectTransform, 540, 1339, 975, 1252, 48, 44);

            var start = T(Node(box, "CHStartButton"), 538, 1792, 536, 126);
            Btn(start);
            // 실측 — 동전 x 300~360 · 값 글자 x 378~508 · 구분선 520
            TR(Img(Node(start, "CHStartCostIcon"), L("goldicon")).rectTransform, 538, 1792, 330, 1792, 60, 60);
            var cost = Txt(Node(start, "CHStartCostText"), "0", F(46), TextAlignmentOptions.Left);
            TLR(cost.rectTransform, 538, 1792, 374, 1792, 140, 56);
            cost.color = new Color(0.20f, 0.13f, 0.03f);
        }

        // ── 값 꽂기 ──────────────────────────────────────────

        private static void Bind(ChapterHostPanel panel)
        {
            var so = new SerializedObject(panel);
            so.FindProperty("_cardFrame").objectReferenceValue = P("ch_card_frame");
            so.FindProperty("_cardFrameSelected").objectReferenceValue = P("ch_card_frame_sel");
            // 별도 시안에서 떼어 온 것이다 — 다른 별을 쓰면 색과 두께가 티 난다
            so.FindProperty("_starOn").objectReferenceValue = P("ch_star_on");
            so.FindProperty("_starOff").objectReferenceValue = P("ch_star_off");

            var arts = so.FindProperty("_hostArts");
            arts.arraySize = 0;
            foreach (var dir in Directory.GetDirectories(UnitDir))
            {
                string key = Path.GetFileName(dir);
                var drawn = Spr($"{Growth}Portraits/portrait_{key}.png");
                var thumb = drawn != null ? drawn : Spr($"{UnitDir}/{key}/unit_{key}_se.png");
                if (thumb == null) thumb = Spr($"{UnitDir}/{key}/unit_{key}_e.png");
                if (thumb == null) continue;
                int i = arts.arraySize++;
                var e = arts.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = key;
                e.FindPropertyRelative("Thumb").objectReferenceValue = thumb;
                e.FindPropertyRelative("IsPortrait").boolValue = drawn != null;
            }
            {
                int i = arts.arraySize++;   // 유령 — 표에 없는 칸이라 따로 넣는다
                var e = arts.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = HostEntry.GhostKey;
                e.FindPropertyRelative("Thumb").objectReferenceValue = Spr(Lobby + "ghostavatar.png");
                e.FindPropertyRelative("IsPortrait").boolValue = true;
            }

            var bossTable = AssetDatabase.LoadAssetAtPath<ScriptableObject>(BossTablePath);
            var bossSo = bossTable != null ? new SerializedObject(bossTable) : null;
            var chapterArts = so.FindProperty("_chapterArts");
            chapterArts.arraySize = 6;
            var chapterArt = so.FindProperty("_chapterArt");
            chapterArt.arraySize = 6;
            for (int ch = 1; ch <= 6; ch++)
            {
                var e = chapterArts.GetArrayElementAtIndex(ch - 1);
                var (key, nameEn) = BossOfChapter(bossSo, ch);
                e.FindPropertyRelative("BossName").stringValue = nameEn;
                e.FindPropertyRelative("BossPortrait").objectReferenceValue = BossFace(key);
                e.FindPropertyRelative("Chest").objectReferenceValue = ChestOfChapter(ch);
                e.FindPropertyRelative("ChestLabel").stringValue = ChestLabelOfChapter(ch);
                chapterArt.GetArrayElementAtIndex(ch - 1).objectReferenceValue =
                    Spr($"{PartsDir}/ch_art_{ch}.png");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 보스 얼굴. 동쪽(`_e`) 그림이 없는 보스도 있다 — 파이썬은 남쪽(`_s`)뿐이다.
        /// 없으면 남쪽 · 그것도 없으면 그 보스 폴더의 아무 그림이나 쓴다(빈 칸보다 낫다).
        /// </summary>
        private static Sprite BossFace(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var dir = $"{UnitDir}/{key}";
            var face = Spr($"{dir}/unit_{key}_e.png") ?? Spr($"{dir}/unit_{key}_s.png");
            if (face != null || !Directory.Exists(dir)) return face;

            foreach (var f in Directory.GetFiles(dir, "*.png"))
            {
                var s = Spr(f.Replace(Path.DirectorySeparatorChar, '/'));
                if (s != null) return s;
            }
            return null;
        }

        private static (string key, string nameEn) BossOfChapter(SerializedObject bossSo, int chapter)
        {
            if (bossSo == null) return (string.Empty, string.Empty);
            var list = bossSo.FindProperty("_entries");
            if (list == null || !list.isArray) return (string.Empty, string.Empty);
            for (int i = 0; i < list.arraySize; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                var chProp = e.FindPropertyRelative("_chapter");
                if (chProp == null || chProp.intValue != chapter) continue;
                string key = e.FindPropertyRelative("_bossKey")?.stringValue ?? string.Empty;
                string nameEn = e.FindPropertyRelative("_nameEn")?.stringValue ?? string.Empty;
                return (key, string.IsNullOrEmpty(nameEn) ? key.ToUpperInvariant() : nameEn.ToUpperInvariant());
            }
            return (string.Empty, string.Empty);
        }

        private static Sprite ChestOfChapter(int chapter)
            => chapter <= 2 ? L("chest_silver") : chapter <= 4 ? L("chest_gold") : L("chest_magic");

        private static string ChestLabelOfChapter(int chapter)
            => chapter <= 2 ? "실버 상자" : chapter <= 4 ? "골드 상자" : "플래티넘 상자";

        // ── 시안 좌표 → 게임 좌표 ────────────────────────────

        private static float F(float designSize) => designSize * S;

        private static RectTransform T(RectTransform r, float tx, float ty, float tw, float th)
        {
            Center(r, (tx - DesignW / 2f) * S, (DesignH / 2f - ty) * S, tw * S, th * S);
            return r;
        }

        /// <summary>화면 기준, 왼쪽 끝을 맞춘다(왼쪽 정렬 글자).</summary>
        private static RectTransform TL2(RectTransform r, float tx, float ty, float tw, float th)
        {
            Left(r, (tx - DesignW / 2f) * S, (DesignH / 2f - ty) * S, tw * S, th * S);
            return r;
        }

        private static RectTransform TR(RectTransform r, float px, float py, float tx, float ty, float tw, float th)
        {
            Center(r, (tx - px) * S, (py - ty) * S, tw * S, th * S);
            return r;
        }

        private static RectTransform TLR(RectTransform r, float px, float py, float tx, float ty, float tw, float th)
        {
            Left(r, (tx - px) * S, (py - ty) * S, tw * S, th * S);
            return r;
        }

        /// <summary>누를 자리만 만든다 — 그림은 시안에 이미 있다.</summary>
        private static void Hit(Transform box, string name, float tx, float ty, float tw, float th)
        {
            var n = T(Node(box, name), tx, ty, tw, th);
            Btn(n);
        }

        // ── 노드 도우미 ──────────────────────────────────────

        private static Sprite P(string part)
            => AssetDatabase.LoadAssetAtPath<Sprite>($"{PartsDir}/{part}.png");

        private static Sprite L(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Lobby}{file}.png");
        private static Sprite Spr(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static RectTransform Fresh(Transform root, string name)
        {
            var old = root.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var node = Node(root, name);
            Full(node);
            return node;
        }

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
            return img;
        }

        /// <summary>
        /// 글자. **시안 글꼴에 맞춰 두꺼운 고딕을 쓰고 외곽선을 넣지 않는다** —
        /// 외곽선 재질을 쓰면 같은 크기라도 획이 굵어 보여 시안과 달라진다.
        /// </summary>
        private static TextMeshProUGUI Txt(Transform node, string text, float size, TextAlignmentOptions align)
        {
            var t = node.gameObject.AddComponent<TextMeshProUGUI>();
            if (s_font != null)
            {
                t.font = s_font;
                t.fontSharedMaterial = s_font.material;
            }
            t.fontSize = size;
            t.text = text;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        private static void Btn(Transform node)
        {
            var g = node.GetComponent<Graphic>();
            if (g == null)
            {
                var img = node.gameObject.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);   // 누를 자리만. 투명해도 터치는 받는다
                g = img;
            }
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

        private static void Center(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        private static void Left(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }
    }
}
