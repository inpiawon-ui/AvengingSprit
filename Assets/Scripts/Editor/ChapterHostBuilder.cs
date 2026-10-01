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
        /// 배경 그림의 바깥 여백 (시안 픽셀). **그림의 실제 크기와 반드시 같아야 한다** —
        /// 다르면 그림이 늘어나 화면이 통째로 확대돼 보인다
        /// (2026-10-01 : 1080x1920 그림을 1440x2400 자리에 그려 1.33 배로 커졌다).
        ///
        /// 가로 여백은 **필요 없다** — `ScreenFitStretchX` 가 가로를 늘려 채운다.
        /// 세로 여백만 둔다(20:9 처럼 더 긴 화면용). 그 여백은 가장자리를 늘린 것이 아니라
        /// **새로 그려 받은 그림**이다 — 늘린 것은 뿌옇게 번진다.
        /// </summary>
        private const float PadX = 0f, PadY = 240f;

        private static readonly string[] KeepAspectNames =
        {
            "CardThumb", "CardStar", "CardPowerIcon", "CardCheck", "RandomCheck",
            "CHBossPortrait", "CHRewardChestIcon", "CHStartCostIcon",
            "CHPrevArrow", "CHNextArrow", "CHLockIcon",
        };

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

            // 4:3 에서 가로로 늘릴 때 **캐릭터 그림·아이콘은 찌그러뜨리지 않는다**(글자는 자동)
            foreach (var img in panel.GetComponentsInChildren<Image>(true))
                foreach (var prefix in KeepAspectNames)
                    if (img.name.StartsWith(prefix))
                    {
                        img.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitKeepAspect>();
                        break;
                    }

            return panel.gameObject.AddComponent<ChapterHostPanel>();
        }

        // ── 상단 — 칸과 아이콘은 시안에 이미 있다. 값과 누를 자리만 얹는다 ──

        private static void TopBar(Transform box)
        {
            // 실측(v6) — 칸 y 60~144 (가운데 101)
            //   뒤로가기 x 61~144 · 다이아 칸 234~488 · 금화 칸 505~800 · 메일 827~937 · 설정 937~1020
            Hit(box, "CHBackButton", 103, 101, 88, 82);
            Hit(box, "CHMailButton", 882, 101, 112, 82);
            Hit(box, "CHSettingsButton", 978, 101, 86, 82);

            // 칸과 아이콘은 **시안에 이미 그려져 있다**. 숫자만 얹는다.
            Num(box, "CHGemText", 318, 101, 164);
            Num(box, "CHGoldText", 606, 101, 186);
        }

        /// <summary>재화 숫자 한 칸. 칸·아이콘은 시안 것이고 숫자만 왼쪽 정렬로 얹는다.</summary>
        /// <summary>
        /// 배경에서 빼낸 **붙박이 글자** 한 칸. 값이 아니라 화면 문구다.
        ///
        /// 일본 납품이라 그림에 박힌 글자는 번역할 수 없다 — 전부 여기로 뺀다(2026-10-01).
        /// 실제 문구는 `ChapterHostPanel` 이 `Localize.FromTable` 로 채운다.
        /// </summary>
        private static TextMeshProUGUI Label(Transform box, string name, string sample,
                                             float x, float y, float w, float h, float size,
                                             TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var t = Txt(Node(box, name), sample, F(size), align);
            if (align == TextAlignmentOptions.Right)
                Right(t.rectTransform, (x - DesignW / 2f) * S, (DesignH / 2f - y) * S, w * S, h * S);
            else
                TL2(t.rectTransform, x, y, w, h);
            t.enableAutoSizing = true;
            t.fontSizeMin = F(size * 0.55f);
            t.fontSizeMax = F(size);
            t.richText = true;
            return t;
        }

        /// <summary>오른쪽 끝을 맞춘다(오른쪽 정렬 글자).</summary>
        private static void Right(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(1f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

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
            // ── 챕터 큰 판 — 시안의 **스테이지에는 따로 박스가 없다** ──
            // 스테이지 그림이 큰 판 전체(x 36~1044 · y 172~856 의 테두리 안쪽)에 깔리고,
            // 그 위에 딤(왼쪽 + 아래)이 덮이고, 아래에 보스 칸 · 보상 칸 두 네모만 얹힌다.
            // ⚠ 두 번 틀렸다(2026-10-01) — 처음엔 그림을 오른쪽 네모에만 넣었고,
            //   다음엔 챕터에 안쪽 박스를 남긴 채 그 안에만 깔았다. 박스는 보스·보상 둘뿐이다.
            var artBox = T(Node(box, "CHChapterArtBox"), 540, 514, 1000, 676);
            artBox.gameObject.AddComponent<RectMask2D>();
            var art = Img(Node(artBox, "CHChapterArt"), null);
            Center(art.rectTransform, 0, 0, 1000 * S, 676 * S);

            // 딤 — 왼쪽(글자 자리)과 아래(두 네모 자리)를 어둡게. 깎인 모서리도 이 그림이 가린다
            var dim = Img(Node(artBox, "CHChapterDim"), P("ch_dim2"));
            Center(dim.rectTransform, 0, 0, 1000 * S, 676 * S);

            // 보스 칸 · 보상 칸 — 배경에서 떼어 낸 빈 칸을 그림 위에 다시 올린다
            // (실측 보스 x 52~442 · 보상 x 459~1026 · y 617~833, 잘라낸 판은 둘레 4 씩 더 크다)
            T(Img(Node(box, "CHBossBox"), P("ch_bossbox")).rectTransform, 247, 725.5f, 398, 225);
            T(Img(Node(box, "CHRewardBox"), P("ch_rewardbox")).rectTransform, 742.5f, 725.5f, 575, 225);
            // 보상 동전은 칸 그림에 들어 있다(가운데 508,752)

            // 화살표는 그림 위에 다시 올린다 (실측 ◀ 가운데 97,266 · ▶ 453,266)
            Img(Node(box, "CHPrevArrow"), P("ch_arrow_left")).preserveAspect = true;
            T((RectTransform)box.Find("CHPrevArrow"), 97, 266, 36, 60);
            Img(Node(box, "CHNextArrow"), P("ch_arrow_right")).preserveAspect = true;
            T((RectTransform)box.Find("CHNextArrow"), 453, 266, 36, 60);
            Hit(box, "CHPrevButton", 97, 266, 72, 72);
            Hit(box, "CHNextButton", 453, 266, 72, 72);

            // 「CHAPTER」·번호 (실측 글자 x 153~313 · 번호 x 333~404 · y 240~286)
            Label(box, "CHChapterLabel", "CHAPTER", 152, 263, 280, 62, 42)
                .color = new Color(0.78f, 0.86f, 0.96f);
            var no = Txt(Node(box, "CHChapterNoText"), "01", F(50), TextAlignmentOptions.Left);
            TL2(no.rectTransform, 331, 263, 92, 58);
            no.color = new Color(0.36f, 0.78f, 1f);

            // 챕터 이름 — 실측 y 325~410
            var name = Txt(Node(box, "CHChapterNameText"), "", F(80), TextAlignmentOptions.Left);
            TL2(name.rectTransform, 85, 368, 350, 100);
            name.enableAutoSizing = true;
            name.fontSizeMin = F(44);
            name.fontSizeMax = F(80);

            // 설명 — TopLeft 는 칸 위쪽부터 그려진다. 글자 위가 458 이 되게 칸 가운데를 506 에 둔다
            var desc = Txt(Node(box, "CHChapterDescText"), "", F(27), TextAlignmentOptions.TopLeft);
            TL2(desc.rectTransform, 85, 506, 440, 96);
            desc.textWrappingMode = TextWrappingModes.Normal;
            desc.lineSpacing = 12f;
            desc.color = new Color(0.86f, 0.90f, 0.96f);

            T(Img(Node(box, "CHLockIcon"), L("modelockicon")).rectTransform, 747, 385, 150, 180);

            // ── 보스 칸 · 클리어 보상 칸 (같은 큰 액자 안의 작은 네모 둘) ──
            // 보스 얼굴은 둥근 틀(x 80~257 · y 654~805) **안쪽**에만 들어간다
            var portrait = Img(Node(box, "CHBossPortrait"), null);
            portrait.preserveAspect = true;
            T(portrait.rectTransform, 169, 730, 158, 132);
            Label(box, "CHBossLabel", "BOSS", 324, 683, 92, 50, 38);
            Label(box, "CHRewardLabel", "클리어 보상", 485, 675, 170, 54, 36);

            // 보스 이름 (y 740~775)
            var bossName = Txt(Node(box, "CHBossNameText"), "", F(34), TextAlignmentOptions.Left);
            TL2(bossName.rectTransform, 278, 756, 180, 48);
            bossName.enableAutoSizing = true;
            bossName.fontSizeMin = F(18);
            bossName.fontSizeMax = F(34);

            // 동전(가운데 508,752)은 시안 것. 값·상자·상자 이름만 낸다
            var gold = Txt(Node(box, "CHRewardGoldText"), "", F(38), TextAlignmentOptions.Left);
            TL2(gold.rectTransform, 552, 757, 228, 52);
            gold.color = new Color(1f, 0.85f, 0.32f);
            gold.enableAutoSizing = true;
            gold.fontSizeMin = F(22);
            gold.fontSizeMax = F(38);

            var chest = Img(Node(box, "CHRewardChestIcon"), L("chest_gold"));
            chest.preserveAspect = true;
            T(chest.rectTransform, 846, 763, 128, 122);
            var chestText = Txt(Node(box, "CHRewardChestText"), "", F(30), TextAlignmentOptions.Left);
            TL2(chestText.rectTransform, 914, 760, 100, 44);
            chestText.enableAutoSizing = true;
            chestText.fontSizeMin = F(12);
            chestText.fontSizeMax = F(30);
        }

        // ── 호스트 줄 ──────────────────────────────────────

        private static void HostBox(Transform box)
        {
            // 실측 — 첫 칸 x 85 · 폭 144 · 사이 10 · 여섯 장 914 · 카드 가운데 y 1099
            // 머리말도 코드 글자다 — 노란 막대(x 68~80)와 청록 점(x 835~855)만 시안 것
            Label(box, "CHHostTitle", "호스트 선택", 110, 922, 330, 54, 38);
            Label(box, "CHHostNote", "보유한 호스트만 선택 가능합니다.", 1004, 922, 252, 44, 24,
                  TextAlignmentOptions.Right).color = new Color(0.78f, 0.86f, 0.96f);

            // 실측(v6) — 첫 칸 x 82 · 폭 142 · 사이 12 · 카드 가운데 y 1145
            var view = T(Node(box, "CHHostViewport"), 540, 1145, 914, 360);
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

            Hit(box, "CHHostPrev", 65, 1145, 58, 120);
            Hit(box, "CHHostNext", 1015, 1145, 58, 120);
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
            Center(thumb.rectTransform, 0, 53 * S, 138 * S, 182 * S);

            // 실측(카드 가운데 1145 기준) — 이름 +61 · 별 +99 · 전투력 +132
            var name = Txt(Node(card, "CardName"), "", F(30), TextAlignmentOptions.Center);
            Center(name.rectTransform, 0, -61 * S, 140 * S, 38 * S);
            name.enableAutoSizing = true;
            name.fontSizeMin = F(13);
            name.fontSizeMax = F(30);

            for (int s = 0; s < 5; s++)
            {
                // 실측 — 별 다섯이 카드 가운데 기준 -50 에서 25 간격, 한 개 22
                var star = Img(Node(card, $"CardStar{s}"), P("ch_star_off"));
                star.preserveAspect = true;
                Center(star.rectTransform, (-50f + s * 25f) * S, -101 * S, 24 * S, 24 * S);
            }

            var powerIcon = Img(Node(card, "CardPowerIcon"), P("ch_power_icon"));
            powerIcon.preserveAspect = true;
            Center(powerIcon.rectTransform, -53 * S, -132 * S, 30 * S, 30 * S);
            var power = Txt(Node(card, "CardPowerText"), "", F(32), TextAlignmentOptions.Left);
            Left(power.rectTransform, -27 * S, -132 * S, 116 * S, 42 * S);

            // 고른 표시 — 금색 액자와 체크. 둘 다 시안에서 잘라 온 것이다
            var sel = Img(Node(card, "CardFrameSel"), P("ch_card_frame_sel"));
            Center(sel.rectTransform, 0, 0, ChapterHostLayout.CardWidth + 10f, ChapterHostLayout.CardHeight + 10f);
            var check = Img(Node(card, "CardCheck"), P("ch_check"));
            check.preserveAspect = true;
            Center(check.rectTransform, 41 * S, 155 * S, 44 * S, 44 * S);
        }

        // ── 아래 — 랜덤 칸과 도전 버튼 ──────────────────────

        private static void BottomRow(Transform box)
        {
            // 실측(v6) — 랜덤 판 y 1347~1608 / 버튼 노란판 x 272~800 · y 1730~1855
            var random = T(Node(box, "CHRandomCard"), 540, 1478, 960, 252);
            Btn(random);
            // 랜덤 칸 문구 셋도 코드 글자다 (시안 x 238~ · y 1415 / 1475 / 1521)
            Label(box, "CHRandomTitle", "랜덤 선택", 341, 1425, 300, 50, 38);
            Label(box, "CHRandomDesc1", "모든 호스트 중 하나가 랜덤으로 선택됩니다.", 341, 1482, 590, 44, 32);
            Label(box, "CHRandomDesc2", "낮은 확률로 전설 호스트 등장!", 341, 1528, 590, 44, 32);

            var check = Img(Node(random, "RandomCheck"), P("ch_check"));
            check.preserveAspect = true;
            TR(check.rectTransform, 540, 1478, 975, 1382, 48, 48);

            var start = T(Node(box, "CHStartButton"), 536, 1792, 528, 126);
            Btn(start);
            // 실측 — 동전 x 305~365(가운데 335) · 값 글자 x 378~481 · 구분선 506
            TR(Img(Node(start, "CHStartCostIcon"), L("goldicon")).rectTransform, 536, 1792, 335, 1792, 62, 62);
            // 「도전하기 ▶」 도 코드 글자다 (시안 x 534~760 · y 1765~1825)
            Label(box, "CHStartLabel", "도전하기 ▶", 530, 1792, 272, 60, 46)
                .color = new Color(0.20f, 0.13f, 0.03f);

            var cost = Txt(Node(start, "CHStartCostText"), "0", F(48), TextAlignmentOptions.Left);
            TLR(cost.rectTransform, 536, 1792, 376, 1792, 122, 58);
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
            // ⚠ `ScreenFit` 은 부모를 꽉 채우는 상자를 **가로 스트레치로 바꿔 버린다**.
            //   그러면 상자만 960 이 되고 안의 판·글자는 절대 좌표라 가운데 720 에 그대로 몰린다.
            //   게다가 아래 배율까지 겹쳐 두 번 늘어난다(2026-10-01). 그래서 먼저 막는다.
            box.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();
            // 4:3 에서는 보이는 폭이 960 이라 그대로 두면 판이 가운데 720 에만 모인다.
            // **가로만** 배율로 늘려 채운다 — 세로는 배경 그림의 위아래 여백이 메운다.
            box.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitStretchX>();
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
