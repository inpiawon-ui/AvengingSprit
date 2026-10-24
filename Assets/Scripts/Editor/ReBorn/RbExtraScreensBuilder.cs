// RE:BORN UI 리뉴얼(2026-10-24) — 남은 화면: 안내(guide) · 보스 경고(boss) · 다음 방(exit) · 밤 몸 고르기(night-pick) · 상자 열기(chest-opening · chest-open).
// 판은 720 × 1280 게임 px(RbRailBuilder.Corner · Inside). 안내는 로비 · 인게임이 같이 쓴다.
using Game.Module.Common.UI;
using Game.Module.InGame;
using Game.Module.Lobby;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.ReBorn.RbBuild;
using static Game.Editor.ReBorn.RbRailBuilder;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbExtraScreensBuilder
    {
        public static readonly string[] GuideTopics =
        {
            "sortie", "growth", "summon", "tower", "night", "pass", "possess", "stand_fire", "ghost_hp",
            "lock", "release", "rooms", "cards", "merchant", "boss",
        };

        /// <summary>판(딤 + 가운데 720 × 1280) — 꺼진 채 저장하지 않는다(글자 크기가 풀린다). 다 세운 뒤 부르는 쪽이 끈다.</summary>
        private static RectTransform Sheet(RectTransform root, string name, float dimAlpha, out RectTransform c)
        {
            var old = root.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var page = Stretch(Node(root, name));
            var dim = page.gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.06f, dimAlpha);
            dim.raycastTarget = dimAlpha > 0f;
            c = Node(page, name + "Content");
            c.anchorMin = c.anchorMax = c.pivot = new Vector2(0.5f, 0.5f);
            c.sizeDelta = new Vector2(720f, 1280f);
            c.gameObject.AddComponent<ScreenFitLock>();   // 인게임 화면 맞춤(ScreenFit)에서 뺀다 — 로비에는 영향 없음
            return page;
        }

        private static void Plate(RectTransform c, string name, string key, string ko, float x0, float y0, float x1)
        {
            float cx = (x0 + x1) * 0.5f;
            Img(Corner(Node(c, name + "TitlePlate"), cx - 130f, y0 - 25f, cx + 130f, y0 + 25f), "Components/rb_popup_title_plate");
            Text(Corner(Node(c, name + "Title"), cx - 112f, y0 - 21f, cx + 112f, y0 + 21f), key, ko, 44, White, TA.Center, heavy: true);
            var x = Corner(Node(c, name + "Close"), x1 - 54f, y0 - 22f, x1 + 2f, y0 + 34f);
            Btn(x, "Components/rb_btn_icon_round");
            Img(Inside(Node(x, name + "CloseIcon"), 28f, 28f, 30f, 30f), "Icons/rb_icon_sys_close_48");
        }

        // ── 안내 ──────────────────────────────────────────────

        /// <summary>안내 — 시안 guide.png: 제목판 · 그림(720 × 480) · 유령 + 두세 줄 · 쪽 점 · 다음.</summary>
        public static RectTransform BuildGuide(RectTransform root, float dimAlpha = 0.86f)
        {
            var page = Sheet(root, "RbGuide", dimAlpha, out var c);
            Img(Corner(Node(c, "RbGuideBox"), 50f, 440f, 670f, 1070f), "Components/rb_modal");
            Plate(c, "RbGuide", null, "아주 가끔 히든 호스트!", 50f, 440f, 670f);
            Img(Corner(Node(c, "RbGuideArt"), 90f, 488f, 630f, 820f), "Art/rb_art_guide_sortie");
            Img(Corner(Node(c, "RbGuideMascot"), 86f, 846f, 156f, 916f), "Art/rb_art_mascot_ghost_point");
            var body = Text(Corner(Node(c, "RbGuideBody"), 168f, 840f, 640f, 950f), null,
                            "랜덤 출전에서 특별한 몸을 만날 수 있어요.\n이번 판 한정 체험이며 조각은 주지 않아요.", 40, White, TA.TopLeft, heavy: true);
            body.enableWordWrapping = true;
            Text(Corner(Node(c, "RbGuideDots"), 200f, 952f, 520f, 982f), null, "● ○ ○", 30, Dim, TA.Center);
            var next = Corner(Node(c, "RbGuideNext"), 80f, 990f, 640f, 1052f);
            Btn(next, "Components/rb_btn_primary");
            Text(Stretch(Node(next, "RbGuideNextText")), null, "다음", 52, Ink, TA.Center, heavy: true);

            var view = page.gameObject.AddComponent<RbGuide>();
            var so = new SerializedObject(view);
            var arts = so.FindProperty("_arts");
            arts.arraySize = GuideTopics.Length;
            for (int i = 0; i < GuideTopics.Length; i++) arts.GetArrayElementAtIndex(i).objectReferenceValue = KitSprite($"Art/rb_art_guide_{GuideTopics[i]}");
            so.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        // ── 인게임: 보스 경고 · 다음 방 · 밤 몸 고르기 ──────────────

        // 인게임 캔버스는 카메라 화면 + 선형 색 공간이라 반투명 딤이 로비보다 훨씬 옅게 보인다
        // (0.86 은 거의 안 어두워지고 0.95 도 옅다 — 2026-10-24 실측). 시안 어둡기는 0.99 · 판을 가리는 쪽은 1
        public const float InGameDim = 0.99f;

        public static void BuildInGame(RectTransform root)
        {
            BuildBossWarn(root);
            BuildNextRoom(root);
            BuildNightPick(root);
            BuildGuide(root, InGameDim);
        }

        /// <summary>보스 경고 — 시안 boss.png: 위 막대 밑 띠(WARNING · 보스 얼굴 · 약점 · 「보스는 빙의할 수 없어요」). 판을 멈추지 않는다.</summary>
        private static void BuildBossWarn(RectTransform root)
        {
            var page = Sheet(root, "RbBossWarn", 0f, out var c);
            var band = Corner(Node(c, "RbBossWarnBand"), 14f, 260f, 706f, 440f);
            Img(band, "Components/rb_panel_base").preserveAspect = false;
            Img(Corner(Node(c, "RbBossWarnWord"), 230f, 268f, 490f, 316f), "WordArt/rb_word_word_warning");
            var face = Corner(Node(c, "RbBossWarnFaceBox"), 30f, 322f, 190f, 428f);
            Img(face, "Components/rb_slot_common").preserveAspect = false;
            face.gameObject.AddComponent<RectMask2D>();
            Img(Inside(Node(face, "RbBossWarnFace"), 80f, 53f, 150f, 150f), "Icons/rb_icon_room_boss_96");
            var weak = Text(Corner(Node(c, "RbBossWarnWeak"), 205f, 326f, 696f, 384f), null, "약점: 나온 머리가 들어가기 전에 때려라", 38, White, TA.Left, heavy: true);
            weak.enableWordWrapping = true;
            Text(Corner(Node(c, "RbBossWarnNote"), 205f, 390f, 696f, 424f), "ui.rb.boss.no_possess", "보스는 빙의할 수 없어요", 32, Dim, TA.Left, heavy: true);
            page.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;   // 띠 뒤로 조작이 그대로 간다
        }

        /// <summary>다음 방 — 시안 exit.png: 「1-08 /16」 · 방 아이콘 · 이름 · 설명 · 다음 방으로.</summary>
        private static void BuildNextRoom(RectTransform root)
        {
            Sheet(root, "RbNextRoom", InGameDim, out var c);
            Img(Corner(Node(c, "RbNextRoomBox"), 150f, 430f, 570f, 820f), "Components/rb_modal");
            Plate(c, "RbNextRoom", "ui.rb.next.title", "다음 방", 150f, 430f, 570f);
            Text(Corner(Node(c, "RbNextRoomStage"), 180f, 478f, 540f, 530f), null, "1-08 /16", 60, White, TA.Left, number: true);
            Img(Corner(Node(c, "RbNextRoomIcon"), 180f, 545f, 240f, 605f), "Icons/rb_icon_room_merchant_96");
            Text(Corner(Node(c, "RbNextRoomName"), 180f, 612f, 540f, 650f), null, "떠돌이 상인", 44, White, TA.Left, heavy: true);
            var desc = Text(Corner(Node(c, "RbNextRoomDesc"), 180f, 656f, 548f, 712f), null, "다음 방에서 상품을 확인할 수 있어요.", 36, Dim, TA.TopLeft, heavy: true);
            desc.enableWordWrapping = true;
            var go = Corner(Node(c, "RbNextRoomGo"), 180f, 728f, 540f, 796f);
            Btn(go, "Components/rb_btn_primary");
            Text(Stretch(Node(go, "RbNextRoomGoText")), "ui.rb.next.go", "다음 방으로", 52, Ink, TA.Center, heavy: true);
        }

        /// <summary>밤 몸 고르기 — 시안 night-pick.png: 제목 · 부제 · 카드 셋(몸 · 이름 · 상성) · 「몸을 선택하세요」 → 고르면 노란 「빙의」.</summary>
        private static void BuildNightPick(RectTransform root)
        {
            Sheet(root, "RbNightPick", 1f, out var c);   // 시안은 판이 안 보이는 한 화면
            Text(Corner(Node(c, "RbNightPickTitle"), 40f, 300f, 680f, 352f), "ui.rb.nightpick.title", "몸 하나를 골라 빙의하라", 60, White, TA.Center, heavy: true);
            Text(Corner(Node(c, "RbNightPickSub"), 40f, 358f, 680f, 392f), "ui.rb.nightpick.sub", "나머지 둘은 사라진다", 36, Dim, TA.Center, heavy: true);
            for (int i = 0; i < 3; i++)
            {
                float x0 = 30f + i * 224f;
                var card = Corner(Node(c, $"RbNightPickCard{i}"), x0, 430f, x0 + 212f, 800f);
                Btn(card, "Components/rb_card_common").GetComponent<Image>().preserveAspect = false;
                Img(Stretch(Node(card, $"RbNightPickCard{i}Sel")), "Components/rb_card_common_selected").preserveAspect = false;
                Img(Inside(Node(card, $"RbNightPickCard{i}Art"), 106f, 140f, 180f, 180f), "Art/rb_art_mascot_ghost_happy");
                Text(Inside(Node(card, $"RbNightPickCard{i}Name"), 106f, 262f, 196f, 40f), null, "아마존", 42, White, TA.Center, heavy: true);
                var chip = Inside(Node(card, $"RbNightPickCard{i}Chip"), 106f, 310f, 120f, 36f);
                Img(chip, "Components/rb_info_chip").preserveAspect = false;
                Text(Stretch(Node(chip, $"RbNightPickCard{i}ChipText")), null, "파워", 32, Cyan, TA.Center, heavy: true);
            }
            var go = Corner(Node(c, "RbNightPickGo"), 40f, 1010f, 680f, 1086f);
            Btn(go, "Components/rb_btn_primary");
            Text(Stretch(Node(go, "RbNightPickGoText")), null, "몸을 선택하세요", 50, Ink, TA.Center, heavy: true);
        }

        // ── 로비: 상자 열기 ────────────────────────────────────

        /// <summary>
        /// 상자 열기 — 시안 chest-opening(「금 상자 열기!」 · 빛 · 화면을 눌러 계속) → chest-open(「획득!」 · 보상 칸 · 확인).
        /// 상점에서 상자를 사면 보상 창 대신 이 쪽(RbNotice.ShowPurchase). 시안처럼 위 막대는 보이고 아래 탭은 덮는다
        /// (그리기 순서는 로비 빌더가 「아래 탭 → 상자 열기 → 위 막대」로 옮긴다). 자리는 시안 px(Place · Local).
        /// </summary>
        public static void BuildChestOpen(RectTransform root)
        {
            var page = Sheet(root, "RbChestOpen", 0f, out var c);
            Object.DestroyImmediate(c.gameObject);   // 가운데 720 × 1280 판은 안 쓴다 — 로비 화면처럼 위 · 아래 가장자리에 붙인다
            var bg = Stretch(Node(page, "RbChestOpenBg"));
            var bgImg = Img(bg, "Art/rb_art_bg_outgame_blur");
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = true;
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 1080f / 2340f;
            bg.SetAsFirstSibling();
            var tap = Stretch(Node(page, "RbChestOpenTap"));
            HitArea(tap);   // 「화면을 눌러 계속」 — 판 전체
            tap.SetSiblingIndex(1);
            // 박자 1 — chest-opening: 가운데 빛(연출 단계가 얹는다) 자리에 닫힌 상자 · 아래 큰 제목
            Img(Place(Node(page, "RbChestOpenChest"), Edge.Top, 340f, 620f, 740f, 1020f), "Icons/rb_icon_chest_gold_144");
            Text(Place(Node(page, "RbChestOpenTitle"), Edge.Top, 90f, 1245f, 990f, 1340f), null, "금 상자 열기!", 66, White, TA.Center, number: true);
            // 박자 2 — chest-open: 머리줄(뒤로 · 제목 · ?) · 「획득!」 · 보상 칸(첫 칸 77 ~ 367, 간격 318)
            var head = Stretch(Node(page, "RbChestOpenHead"));
            var back = Place(Node(head, "RbChestOpenBack"), Edge.Top, 37f, 280f, 163f, 411f);
            Btn(back, "Components/rb_btn_icon_round");
            Img(Local(Node(back, "RbChestOpenBackIcon"), 37f, 280f, 163f, 411f, 64f, 309f, 136f, 381f), "Icons/rb_icon_sys_back_48");
            Text(Place(Node(head, "RbChestOpenHeadTitle"), Edge.Top, 196f, 310f, 880f, 380f), null, "금 상자 열기!", 48, White, TA.Left, number: true);
            var help = Place(Node(head, "RbChestOpenHelp"), Edge.Top, 916f, 280f, 1043f, 411f);
            Btn(help, "Components/rb_btn_icon_round");
            Img(Local(Node(help, "RbChestOpenHelpIcon"), 916f, 280f, 1043f, 411f, 943f, 309f, 1015f, 381f), "Icons/rb_icon_sys_help_48");
            Text(Place(Node(page, "RbChestOpenGot"), Edge.Top, 140f, 620f, 940f, 720f), "ui.rb.chest.got", "획득!", 84, Yellow, TA.Center, number: true);
            const float cx0 = 77f, cy0 = 811f, cx1 = 367f, cy1 = 1253f;
            for (int i = 0; i < 4; i++)
            {
                var cell = Place(Node(page, $"RbChestOpenCell{i}"), Edge.Top, cx0 + i * 318f, cy0, cx1 + i * 318f, cy1);
                // 시안 칸 = 얇은 테 둥근 판 — 목록 줄 판을 9분할로(테두리를 반으로 줄여야 193 px 폭에 들어간다)
                var plate = Img(cell, "Components/rb_list_row");
                plate.pixelsPerUnitMultiplier = 2f;
                Img(Local(Node(cell, $"RbChestOpenCell{i}Icon"), cx0, cy0, cx1, cy1, 147f, 890f, 297f, 1040f), "Icons/rb_icon_currency_gold_96");
                Img(Local(Node(cell, $"RbChestOpenCell{i}Face"), cx0, cy0, cx1, cy1, 280f, 915f, 355f, 990f), "Art/rb_art_mascot_ghost_happy");
                Text(Local(Node(cell, $"RbChestOpenCell{i}Count"), cx0, cy0, cx1, cy1, 87f, 1045f, 357f, 1105f), null, "500", 48, White, TA.Center, number: true);
                var label = Text(Local(Node(cell, $"RbChestOpenCell{i}Label"), cx0, cy0, cx1, cy1, 87f, 1105f, 357f, 1175f), null, "골드 +500", 38, White, TA.Center, heavy: true);
                label.enableWordWrapping = true;
            }
            var go = Place(Node(page, "RbChestOpenGo"), Edge.Bottom, 30f, 2040f, 1050f, 2160f);
            Btn(go, "Components/rb_btn_secondary");
            Text(Stretch(Node(go, "RbChestOpenGoText")), "ui.rb.chest.tap", "화면을 눌러 계속", 44, White, TA.Center, heavy: true);
            page.gameObject.AddComponent<RbChestOpen>();
        }
    }
}
