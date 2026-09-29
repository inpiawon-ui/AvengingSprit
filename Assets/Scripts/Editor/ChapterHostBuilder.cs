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
    /// ⚠ **자리값은 전부 시안 픽셀 그대로다.** 시안은 1024 × 1536 이고 게임 판은 720 × 1280 이라
    ///   가로를 기준으로 `720 / 1024 = 0.703125` 를 **모든 것에** 곱한다 — 크기 · 자리 · 글자 크기까지.
    ///   그래야 비율이 한 군데도 안 틀어진다. 세로는 1536 × 0.703 = 1080 이 되어 위아래로 100 씩 남는다.
    ///   눈대중으로 옮기면 「비슷한데 다른 화면」이 된다(2026-09-29 반려).
    ///
    /// 좌표를 적을 때는 <see cref="T"/>(화면 기준) · <see cref="TR"/>(부모 기준) 을 쓴다.
    /// 둘 다 **시안 픽셀**을 받는다. 게임 좌표로 환산하지 말고 시안에서 잰 값을 그대로 적는다.
    ///
    /// 노드 이름 = 바인딩 키이며 <see cref="ChapterHostPanel"/> 이 이름으로만 찾는다.
    /// </summary>
    public static class ChapterHostBuilder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string PartsDir = "Assets/BaseResource/ChapterHost";
        private const string Lobby = "Assets/BaseResource/LobbyMainUI/";
        private const string HostSel = "Assets/BaseResource/HostSelectPanel/";
        private const string Growth = "Assets/BaseResource/Growth/";
        private const string UnitDir = "Assets/BaseResource/Unit";
        private const string BossTablePath = "Assets/BundleResource/TableData/BossTable.asset";

        /// <summary>시안(1024 폭) → 게임 판(720 폭) 배율. **이 하나로 전부 줄인다.**</summary>
        private const float S = 720f / 1024f;

        private const float DesignW = 1024f, DesignH = 1536f;

        /// <summary>발주한 부품 → 없을 때 대신 쓸 그림.</summary>
        private static readonly (string part, string fallback)[] Parts =
        {
            ("ch_bg", HostSel + "hostselectbackground.png"),
            ("ch_panel", Lobby + "panelframe.png"),
            ("ch_plate", Lobby + "hudpill_solid.png"),
            ("ch_pill", Lobby + "hudpill.png"),
            ("ch_card_frame", Growth + "hostcard.png"),
            ("ch_card_frame_sel", Growth + "hostcard_sel.png"),
            ("ch_check", Lobby + "dailylogincheck.png"),
            ("ch_start_button", Lobby + "buttongold.png"),
            ("ch_arrow_left", Lobby + "modearrow_left.png"),
            ("ch_arrow_right", Lobby + "modearrow_right.png"),
            ("ch_lock", Lobby + "modelockicon.png"),
            ("ch_random_icon", Lobby + "chest_magic.png"),
            ("ch_power_icon", Growth + "icon_atk.png"),
            ("ch_back", Lobby + "modearrow_left.png"),
            ("ch_thumbframe", null),
            ("ch_skull", null),
            ("ch_dot", null),
        };

        private static TMP_FontAsset s_font;
        private static Material s_outline, s_plain;

        [MenuItem("Tools/Game/챕터·호스트 선택 창 세우기")]
        public static void Run()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                PickFont(lobby);
                var panel = Build(lobby.transform);
                Bind(panel);

                var so = new SerializedObject(lobby.GetComponent<LobbyMainUI>());
                var field = so.FindProperty("_chapterHostPanel");
                if (field != null) field.objectReferenceValue = panel;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }

            int have = 0;
            foreach (var (part, _) in Parts) if (File.Exists($"{PartsDir}/{part}.png")) have++;
            Debug.Log($"[챕터·호스트] 세움 — 시안 배율 {S:0.000} · 부품 {have}/{Parts.Length}");
        }

        // ── 판 ───────────────────────────────────────────────

        private static ChapterHostPanel Build(Transform root)
        {
            var panel = Fresh(root, "ChapterHostPanel");

            var dim = Img(Node(panel, "CHDim"), null);
            dim.color = new Color(0f, 0f, 0f, 0.92f);
            dim.raycastTarget = true;          // 뒤 로비가 눌리지 않게 막는다
            Full(dim.rectTransform);

            var box = Content(panel, "CHContent");
            Full(Img(Node(box, "CHBg"), P("ch_bg")).rectTransform);

            TopBar(box);
            ChapterBox(box);
            HostBox(box);
            RandomCard(box);
            StartButton(box);

            return panel.gameObject.AddComponent<ChapterHostPanel>();
        }

        // 아래 숫자는 전부 **시안 1024×1536 에서 잰 픽셀**이다.

        private static void TopBar(Transform box)
        {
            Btn(T(Img(Node(box, "CHBackButton"), P("ch_back")).rectTransform, 56, 52, 52, 52));
            T(Img(Node(box, "CHBackFrame"), P("ch_thumbframe")).rectTransform, 56, 52, 88, 80);

            // 골드 칸 — 시안은 240~725 인데 젬 칸을 끼우느라 왼쪽만 305 로 당겼다.
            // 오른쪽 끝(725)·높이·글자 크기는 시안 그대로다.
            var gold = T(Img(Node(box, "CHGoldPill"), P("ch_pill")).rectTransform, 515, 53, 420, 58);
            TR(Img(Node(gold, "CHGoldIcon"), L("goldicon")).rectTransform, 515, 53, 330, 53, 46, 46);
            var goldText = Txt(Node(gold, "CHGoldText"), "0", F(44), TextAlignmentOptions.Midline);
            TR(goldText.rectTransform, 515, 53, 545, 53, 330, 52);

            // 젬 칸 — 시안에 없던 것이다(2026-09-29 지시). 골드 칸 왼쪽 빈자리에 끼운다
            var gem = T(Img(Node(box, "CHGemPill"), P("ch_pill")).rectTransform, 190, 53, 190, 58);
            TR(Img(Node(gem, "CHGemIcon"), L("gemicon")).rectTransform, 190, 53, 120, 53, 38, 34);
            var gemText = Txt(Node(gem, "CHGemText"), "0", F(28), TextAlignmentOptions.Midline);
            TR(gemText.rectTransform, 190, 53, 210, 53, 140, 44);

            Btn(T(Img(Node(box, "CHMailButton"), L("mailbutton")).rectTransform, 805, 52, 74, 68));
            Btn(T(Img(Node(box, "CHSettingsButton"), L("settingsbutton")).rectTransform, 945, 54, 82, 74));
        }

        private static void ChapterBox(Transform box)
        {
            // 실측(2026-09-29): 판 가로 x 28~992 · 챕터판 y 134~564
            const float cx = 512f, cy = 349f;
            var frame = T(Img(Node(box, "CHChapterBox"), P("ch_panel")).rectTransform, cx, cy, 964, 430);

            // 그림은 제 창(마스크) 안에서만 그린다 — 안 그러면 액자 밖으로 삐져나온다
            var artBox = TR(Node(frame, "CHChapterArtBox"), cx, cy, 743, 265, 463, 250);
            artBox.gameObject.AddComponent<RectMask2D>();
            var art = Img(Node(artBox, "CHChapterArt"), L("modeart_scenario"));
            art.preserveAspect = true;
            Center(art.rectTransform, 0, 0, 463 * S, 250 * S);
            TR(Img(Node(frame, "CHLockIcon"), P("ch_lock")).rectTransform, cx, cy, 743, 265, 110, 140);

            Btn(TR(Img(Node(frame, "CHPrevButton"), P("ch_arrow_left")).rectTransform, cx, cy, 75, 187, 34, 44));
            // 시안은 「CHAPTER」보다 번호가 크다 — 한 줄 안에서 번호만 키운다
            var no = Txt(Node(frame, "CHChapterNoText"), "CHAPTER 01", F(38), TextAlignmentOptions.Center);
            no.richText = true;
            TR(no.rectTransform, cx, cy, 262, 187, 340, 56);
            Btn(TR(Img(Node(frame, "CHNextButton"), P("ch_arrow_right")).rectTransform, cx, cy, 438, 187, 34, 44));

            var name = Txt(Node(frame, "CHChapterNameText"), "", F(64), TextAlignmentOptions.Left);
            TL(name.rectTransform, cx, cy, 60, 258, 430, 80);
            name.enableAutoSizing = true;      // 「밤의 공중기지 옥상」이 그림 위로 넘어가지 않게
            name.fontSizeMin = F(40);
            name.fontSizeMax = F(64);

            // ⚠ 글 상자의 **위쪽**이 314 에 오게 둔다(가운데가 아니라) — 가운데로 두면 이름과 겹친다.
            //   한글은 시안 글꼴보다 폭이 넓어 24 로 두면 줄이 넘어간다. 22 로 한 칸 줄였다.
            var desc = Txt(Node(frame, "CHChapterDescText"), "", F(22), TextAlignmentOptions.TopLeft);
            TL(desc.rectTransform, cx, cy, 60, 362, 452, 96);
            desc.textWrappingMode = TextWrappingModes.Normal;
            desc.color = new Color(0.78f, 0.84f, 0.92f);

            BossPlate(frame, cx, cy);
            RewardPlate(frame, cx, cy);
        }

        private static void BossPlate(Transform frame, float cx, float cy)
        {
            // 실측: 보스판 x 32~414 · y 411~550
            const float bx = 223f, by = 480.5f;
            var plate = TR(Img(Node(frame, "CHBossPlate"), P("ch_plate")).rectTransform, cx, cy, bx, by, 382, 139);

            // 유닛 그림은 투명 여백이 넓다 — 액자를 가득 채우려면 액자보다 크게 잡아야 한다
            var portrait = Img(Node(plate, "CHBossPortrait"), L("bossportrait"));
            portrait.preserveAspect = true;
            TR(portrait.rectTransform, bx, by, 125, 480, 96, 100);
            TR(Img(Node(plate, "CHBossThumbFrame"), P("ch_thumbframe")).rectTransform, bx, by, 125, 480, 104, 112);

            TR(Img(Node(plate, "CHBossSkull"), P("ch_skull")).rectTransform, bx, by, 263, 462, 30, 30);
            TL(Txt(Node(plate, "CHBossLabel"), "BOSS", F(30), TextAlignmentOptions.Left).rectTransform,
               bx, by, 285, 462, 160, 40);

            var name = Txt(Node(plate, "CHBossNameText"), "", F(30), TextAlignmentOptions.Left);
            TL(name.rectTransform, bx, by, 250, 506, 190, 40);
            name.enableAutoSizing = true;      // ROBOT SNAKES 처럼 긴 이름이 판을 넘지 않게
            name.fontSizeMin = F(20);
            name.fontSizeMax = F(30);
        }

        private static void RewardPlate(Transform frame, float cx, float cy)
        {
            // 실측: 보상판 x 432~989 · y 411~550
            const float rx = 710.5f, ry = 480.5f;
            var plate = TR(Img(Node(frame, "CHRewardPlate"), P("ch_plate")).rectTransform, cx, cy, rx, ry, 557, 139);

            TL(Txt(Node(plate, "CHRewardLabel"), "", F(28), TextAlignmentOptions.Left).rectTransform,
               rx, ry, 452, 438, 260, 40);

            TR(Img(Node(plate, "CHRewardGoldIcon"), L("goldicon")).rectTransform, rx, ry, 472, 495, 42, 42);
            var gold = Txt(Node(plate, "CHRewardGoldText"), "", F(32), TextAlignmentOptions.Left);
            TL(gold.rectTransform, rx, ry, 500, 495, 250, 44);
            gold.color = new Color(1f, 0.85f, 0.32f);

            var chest = Img(Node(plate, "CHRewardChestIcon"), L("chest_gold"));
            chest.preserveAspect = true;
            TR(chest.rectTransform, rx, ry, 760, 492, 62, 56);
            TL(Txt(Node(plate, "CHRewardChestText"), "", F(26), TextAlignmentOptions.Left).rectTransform,
               rx, ry, 800, 495, 180, 38);
        }

        private static void HostBox(Transform box)
        {
            // 실측: 호스트판 y 601~950
            const float hx = 512f, hy = 775.5f;
            var frame = T(Img(Node(box, "CHHostBox"), P("ch_panel")).rectTransform, hx, hy, 964, 349);

            // 제목 앞 금색 막대 — 시안에 있는 표시다
            var bar = Img(Node(frame, "CHHostTitleBar"), null);
            bar.color = new Color(1f, 0.82f, 0.25f);
            TR(bar.rectTransform, hx, hy, 47, 655, 10, 36);

            var title = Txt(Node(frame, "CHHostTitle"), "", F(34), TextAlignmentOptions.Left);
            TL(title.rectTransform, hx, hy, 65, 655, 320, 48);
            title.color = new Color(1f, 0.85f, 0.32f);

            TR(Img(Node(frame, "CHHostHintDot"), P("ch_dot")).rectTransform, hx, hy, 695, 650, 16, 16);
            var hint = Txt(Node(frame, "CHHostHintText"), "", F(22), TextAlignmentOptions.Right);
            TR(hint.rectTransform, hx, hy, 725, 650, 520, 34);
            hint.color = new Color(0.72f, 0.82f, 0.94f);

            // 가로 목록 — 뷰포트는 RectMask2D 만 쓴다(05_prefabs 규약: Image 금지)
            // 실측: 카드 줄 x 61~962 · y 691~949
            var view = TR(Node(frame, "CHHostViewport"), hx, hy, 511.5f, 820, 901, 258);
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

            Btn(TR(Img(Node(frame, "CHHostPrev"), P("ch_arrow_left")).rectTransform, hx, hy, 30, 820, 40, 62));
            Btn(TR(Img(Node(frame, "CHHostNext"), P("ch_arrow_right")).rectTransform, hx, hy, 988, 820, 40, 62));
        }

        /// <summary>카드 한 장. 시안 카드(140 × 258) 안에서 잰 자리다.</summary>
        private static void Card(Transform content, int index)
        {
            var card = Node(content, $"CHHostCard{index}");
            card.sizeDelta = new Vector2(ChapterHostLayout.CardWidth, ChapterHostLayout.CardHeight);
            Btn(card);   // 카드 전체가 누르는 자리다

            Center(Img(Node(card, "CardFrame"), P("ch_card_frame")).rectTransform, 0, 0,
                   ChapterHostLayout.CardWidth, ChapterHostLayout.CardHeight);

            var thumb = Img(Node(card, "CardThumb"), null);
            thumb.preserveAspect = true;
            Center(thumb.rectTransform, 0, 52 * S, 104 * S, 104 * S);

            var name = Txt(Node(card, "CardName"), "", F(24), TextAlignmentOptions.Center);
            Center(name.rectTransform, 0, -44 * S, 132 * S, 34 * S);
            // 「코만도(수류탄)」처럼 긴 이름이 액자를 넘지 않게. 넘으면 칸 구분이 안 보인다
            name.enableAutoSizing = true;
            name.fontSizeMin = F(11);
            name.fontSizeMax = F(24);

            for (int s = 0; s < 5; s++)
            {
                var star = Img(Node(card, $"CardStar{s}"), Spr(Growth + "star_big_off.png"));
                star.preserveAspect = true;
                Center(star.rectTransform, (-40 + s * 20) * S, -72 * S, 19 * S, 19 * S);
            }

            var powerIcon = Img(Node(card, "CardPowerIcon"), P("ch_power_icon"));
            powerIcon.preserveAspect = true;
            Center(powerIcon.rectTransform, -38 * S, -100 * S, 26 * S, 26 * S);
            var power = Txt(Node(card, "CardPowerText"), "", F(26), TextAlignmentOptions.Left);
            Left(power.rectTransform, -21 * S, -100 * S, 110 * S, 36 * S);

            var check = Img(Node(card, "CardCheck"), P("ch_check"));
            check.preserveAspect = true;
            Center(check.rectTransform, 45 * S, 105 * S, 40 * S, 40 * S);
        }

        private static void RandomCard(Transform box)
        {
            // 실측: 랜덤판 y 972~1221
            const float rx = 512f, ry = 1096.5f;
            var card = T(Node(box, "CHRandomCard"), rx, ry, 964, 249);
            Btn(card);
            // ⚠ 액자는 **카드의 자식**이다. 여기서 T(화면 기준)를 쓰면 두 번 밀려 딴 데 가 붙는다.
            Center(Img(Node(card, "RandomFrame"), P("ch_panel")).rectTransform, 0, 0, 964 * S, 249 * S);

            TR(Img(Node(card, "RandomIconFrame"), P("ch_thumbframe")).rectTransform, rx, ry, 152, 1090, 185, 210);
            var icon = Img(Node(card, "RandomIcon"), P("ch_random_icon"));
            icon.preserveAspect = true;
            TR(icon.rectTransform, rx, ry, 152, 1090, 150, 150);

            var divider = Img(Node(card, "RandomDivider"), null);
            divider.color = new Color(0.45f, 0.62f, 0.85f, 0.7f);
            TR(divider.rectTransform, rx, ry, 272, 1090, 3, 180);

            var t = Txt(Node(card, "RandomTitle"), "", F(38), TextAlignmentOptions.Left);
            TL(t.rectTransform, rx, ry, 305, 1035, 520, 52);
            t.color = new Color(1f, 0.92f, 0.62f);

            var d1 = Txt(Node(card, "RandomDesc1"), "", F(30), TextAlignmentOptions.Left);
            TL(d1.rectTransform, rx, ry, 305, 1085, 660, 44);
            var d2 = Txt(Node(card, "RandomDesc2"), "", F(30), TextAlignmentOptions.Left);
            TL(d2.rectTransform, rx, ry, 305, 1132, 660, 44);
            d2.color = new Color(0.86f, 0.72f, 1f);

            var check = Img(Node(card, "RandomCheck"), P("ch_check"));
            check.preserveAspect = true;
            TR(check.rectTransform, rx, ry, 940, 1010, 48, 48);
        }

        private static void StartButton(Transform box)
        {
            // 실측: 버튼 x 235~790 · y 1255~1385
            const float sx = 512f, sy = 1320f;
            var start = T(Img(Node(box, "CHStartButton"), P("ch_start_button")).rectTransform, sx, sy, 555, 130);
            Btn(start);

            TR(Img(Node(start, "CHStartCostIcon"), L("goldicon")).rectTransform, sx, sy, 295, 1320, 48, 48);
            var cost = Txt(Node(start, "CHStartCostText"), "0", F(44), TextAlignmentOptions.Left, plain: true);
            TL(cost.rectTransform, sx, sy, 330, 1320, 160, 56);
            cost.color = new Color(0.22f, 0.15f, 0.04f);

            var divider = Img(Node(start, "CHStartDivider"), null);
            divider.color = new Color(0.35f, 0.26f, 0.08f, 0.85f);
            TR(divider.rectTransform, sx, sy, 480, 1320, 3, 56);

            var text = Txt(Node(start, "CHStartText"), "", F(46), TextAlignmentOptions.Center, plain: true);
            TR(text.rectTransform, sx, sy, 627, 1320, 250, 60);
            text.color = new Color(0.18f, 0.12f, 0.03f);
        }

        // ── 값 꽂기 ──────────────────────────────────────────

        private static void Bind(ChapterHostPanel panel)
        {
            var so = new SerializedObject(panel);
            so.FindProperty("_cardFrame").objectReferenceValue = P("ch_card_frame");
            so.FindProperty("_cardFrameSelected").objectReferenceValue = P("ch_card_frame_sel");
            so.FindProperty("_starOn").objectReferenceValue = Spr(Growth + "star_big_on.png");
            so.FindProperty("_starOff").objectReferenceValue = Spr(Growth + "star_big_off.png");

            // 몸 그림 — 초상(발주본)이 있으면 그것, 없으면 유닛 그림. 육성 화면과 같은 규칙이다
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

            // 챕터마다 붙는 것 — 보스 이름·얼굴과 상자. 보스는 표에서 읽는다
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
                e.FindPropertyRelative("BossPortrait").objectReferenceValue =
                    Spr($"{UnitDir}/{key}/unit_{key}_e.png") ?? Spr(Lobby + "bossportrait.png");
                e.FindPropertyRelative("Chest").objectReferenceValue = ChestOfChapter(ch);
                e.FindPropertyRelative("ChestLabel").stringValue = ChestLabelOfChapter(ch);
                chapterArt.GetArrayElementAtIndex(ch - 1).objectReferenceValue =
                    Spr($"{PartsDir}/ch_art_{ch}.png") ?? Spr(Lobby + "modeart_scenario.png");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>그 챕터의 보스 키와 영문 이름. 표가 없으면 빈 값이다.</summary>
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

        /// <summary>챕터별 클리어 상자 — 기존 규칙 그대로(1~2 실버 · 3~4 골드 · 5~6 플래티넘).</summary>
        private static Sprite ChestOfChapter(int chapter)
            => chapter <= 2 ? L("chest_silver") : chapter <= 4 ? L("chest_gold") : L("chest_magic");

        private static string ChestLabelOfChapter(int chapter)
            => chapter <= 2 ? "실버 상자" : chapter <= 4 ? "골드 상자" : "플래티넘 상자";

        // ── 시안 좌표 → 게임 좌표 ────────────────────────────

        /// <summary>시안 글자 크기 → 게임 글자 크기.</summary>
        private static float F(float designSize) => designSize * S;

        /// <summary>화면 한가운데 기준. 시안 픽셀(왼쪽 위가 0,0)을 받는다.</summary>
        private static RectTransform T(RectTransform r, float tx, float ty, float tw, float th)
        {
            Center(r, (tx - DesignW / 2f) * S, (DesignH / 2f - ty) * S, tw * S, th * S);
            return r;
        }

        /// <summary>부모 기준. 부모의 시안 중심(px, py)과 제 시안 자리를 함께 받는다.</summary>
        private static RectTransform TR(RectTransform r, float px, float py, float tx, float ty, float tw, float th)
        {
            Center(r, (tx - px) * S, (py - ty) * S, tw * S, th * S);
            return r;
        }

        /// <summary>부모 기준, 왼쪽 끝을 맞춘다(왼쪽 정렬 글자).</summary>
        private static RectTransform TL(RectTransform r, float px, float py, float tx, float ty, float tw, float th)
        {
            Left(r, (tx - px) * S, (py - ty) * S, tw * S, th * S);
            return r;
        }

        // ── 노드 도우미 ──────────────────────────────────────

        private static Sprite P(string part)
        {
            var got = AssetDatabase.LoadAssetAtPath<Sprite>($"{PartsDir}/{part}.png");
            if (got != null) return got;
            foreach (var (p, fallback) in Parts)
                if (p == part && fallback != null) return AssetDatabase.LoadAssetAtPath<Sprite>(fallback);
            return null;
        }

        private static Sprite L(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Lobby}{file}.png");
        private static Sprite Spr(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

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
            t.fontStyle = FontStyles.Bold;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        private static void Btn(Transform node)
        {
            var g = node.GetComponent<Graphic>();
            if (g == null)
            {
                // ⚠ 누를 자리를 만들려고 붙이는 빈 이미지다. **투명하게 둔다** —
                //   그냥 두면 흰 판이 깔려서, 반투명한 액자 뒤로 허옇게 비친다.
                var img = node.gameObject.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);
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
