// RE:BORN UI 리뉴얼(2026-10-24) — 로비 공용 알림 창 4종(맨 앞). 시안 Screens/reward · energy-short · energy-poor · gem-short · gold-short · purchase-confirm.png.
// 좌표는 720 × 1280 판 안 게임 px: x = (시안 x − 비낌) × 2/3, y = (시안 y − 1185) × 2/3 + 640. 시안 창들이 가운데에서 30~60 비껴 있어 가운데로 맞췄다.
using Game.Module.Lobby;
using UnityEditor;
using UnityEngine;
using static Game.Editor.ReBorn.RbBuild;
using static Game.Editor.ReBorn.RbRailBuilder;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbNoticeBuilder
    {
        public const int RewardCells = 4, GoldSources = 3;

        private static readonly string[] Icons =
        {
            "rb_icon_currency_gold_96", "rb_icon_currency_gem_96", "rb_icon_currency_energy_96", "rb_icon_summon_ticket_96",
            "rb_icon_essence_96", "rb_icon_chest_silver_96", "rb_icon_chest_gold_96", "rb_icon_chest_platinum_96",
            "rb_icon_pass_xp_96", "rb_icon_shard_96", "rb_icon_reward_chest_96", "rb_icon_ghost_xp_96",
        };

        public static void Build(RectTransform rt)
        {
            var root = Stretch(Node(rt, "RbNotice"));
            BuildReward(root);
            BuildShort(root);
            BuildGold(root);
            BuildConfirm(root);
            foreach (var n in new[] { "RbNotReward", "RbNotShort", "RbNotGold", "RbNotConfirm" }) root.Find(n)?.gameObject.SetActive(false);

            var view = root.gameObject.AddComponent<RbNotice>();
            var so = new SerializedObject(view);
            var p = so.FindProperty("_icons");
            p.arraySize = Icons.Length;
            for (int i = 0; i < Icons.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = KitSprite($"Icons/{Icons[i]}");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>REWARD — 유령(환호) · 「보상을 받았어요」 · 칸 4(받은 수만큼 켜고 가운데로 모은다) · 확인.</summary>
        private static void BuildReward(RectTransform root)
        {
            Modal(root, "RbNotReward", "ui.rb.notice.reward_title", "REWARD", 80f, 330f, 640f, 980f, out var c);
            Img(Corner(Node(c, "NotRewardArt"), 300f, 380f, 420f, 492f), "Art/rb_art_mascot_ghost_cheer");
            Text(Corner(Node(c, "NotRewardLine"), 110f, 520f, 610f, 556f), "ui.rb.notice.reward_line", "보상을 받았어요", 46, White, TA.Center, heavy: true);
            for (int i = 0; i < RewardCells; i++)
            {
                float x0 = 102f + i * 132f;
                var cell = Corner(Node(c, $"NotRewardCell{i}"), x0, 578f, x0 + 120f, 778f);
                Img(cell, "Components/rb_slot_common");
                Img(Corner(Node(cell, $"NotRewardCell{i}Icon"), 26f, 14f, 94f, 82f), "Icons/rb_icon_currency_gold_96");
                Text(Corner(Node(cell, $"NotRewardCell{i}Count"), 4f, 88f, 116f, 124f), null, "432", 48, White, TA.Center, number: true);
                Text(Corner(Node(cell, $"NotRewardCell{i}Label"), 6f, 128f, 114f, 188f), null, "골드", 34, White, TA.Center, heavy: true).enableWordWrapping = true;   // 「Summon Tickets」 는 두 줄
            }
            var ok = Corner(Node(c, "NotRewardOk"), 160f, 830f, 560f, 945f);
            Btn(ok, "Components/rb_btn_primary");
            Text(Stretch(Node(ok, "NotRewardOkText")), "ui.common.ok", "확인", 60, Ink, TA.Center, heavy: true);
        }

        /// <summary>모자라요(행동력 · 젬) — 아이콘 · 두 줄 · 큰 버튼(못 쓰면 잿빛) · 밑줄. 시안 energy-short 상자 220~920 × 688~1580.</summary>
        private static void BuildShort(RectTransform root)
        {
            Modal(root, "RbNotShort", null, "행동력이 부족해요", 127f, 309f, 593f, 903f, out var c);
            Img(Corner(Node(c, "NotShortIcon"), 320f, 440f, 400f, 520f), "Icons/rb_icon_currency_energy_96");
            Text(Corner(Node(c, "NotShortLine1"), 150f, 552f, 570f, 593f), null, "필요 5 · 보유 2", 52, White, TA.Center, heavy: true);
            Text(Corner(Node(c, "NotShortLine2"), 150f, 616f, 570f, 652f), null, "다음 회복 03:24", 46, White, TA.Center, heavy: true);
            var go = Corner(Node(c, "NotShortGo"), 165f, 703f, 555f, 815f);
            Btn(go, "Components/rb_btn_primary");
            Text(Stretch(Node(go, "NotShortGoText")), null, "젬 100 채우기", 56, Ink, TA.Center, heavy: true).margin = new Vector4(14f, 0f, 14f, 0f);
            Text(Corner(Node(c, "NotShortFoot"), 150f, 843f, 570f, 880f), null, "젬 100 으로 20 채우기 · 오늘 1/3", 38, Dim, TA.Center, heavy: true);
        }

        /// <summary>골드가 모자라요 — 아이콘 · 필요/보유 · 메모 · 대안 버튼 · 「골드 얻는 곳」 세 줄. 시안 gold-short 상자 200~1000 × 700~1680.</summary>
        private static void BuildGold(RectTransform root)
        {
            Modal(root, "RbNotGold", "ui.rb.notice.gold_title", "골드가 모자라요", 93f, 317f, 627f, 975f, out var c);
            Img(Corner(Node(c, "NotGoldIcon"), 120f, 352f, 176f, 408f), "Icons/rb_icon_currency_gold_96");
            Text(Corner(Node(c, "NotGoldLine"), 192f, 348f, 600f, 384f), null, "필요 200 / 보유 40", 48, White, TA.Left, heavy: true);
            Text(Corner(Node(c, "NotGoldNote"), 192f, 392f, 600f, 424f), null, "선택한 호스트는 그대로 유지됩니다.", 34, Dim, TA.Left, heavy: true);
            var go = Corner(Node(c, "NotGoldGo"), 115f, 452f, 605f, 560f);
            Btn(go, "Components/rb_btn_primary");
            Text(Stretch(Node(go, "NotGoldGoText")), "ui.rb.lobby.gold_short_random", "랜덤으로 출전 · 골드 없음", 52, Ink, TA.Center, heavy: true).margin = new Vector4(16f, 0f, 16f, 0f);
            Text(Corner(Node(c, "NotGoldWhere"), 112f, 594f, 600f, 628f), "ui.rb.notice.gold_where", "골드 얻는 곳", 40, White, TA.Left, heavy: true);
            string[] names = { "ui.rb.notice.src_story", "ui.rb.notice.src_shop", "ui.rb.notice.src_mail" };
            string[] ko = { "챕터 클리어", "오늘의 상점", "우편" };
            string[] icons = { "Icons/rb_icon_tab_battle_96", "Icons/rb_icon_tab_shop_96", "Icons/rb_icon_rail_mail_96" };
            float[] centers = { 690f, 796f, 902f };
            for (int i = 0; i < GoldSources; i++)
            {
                float y = centers[i];
                var row = Corner(Node(c, $"NotGoldRow{i}"), 108f, y - 48f, 612f, y + 48f);
                Img(row, "Components/rb_list_row").preserveAspect = false;
                Img(Corner(Node(c, $"NotGoldRow{i}Icon"), 124f, y - 26f, 176f, y + 26f), icons[i]);
                Text(Corner(Node(c, $"NotGoldRow{i}Name"), 192f, y - 22f, 470f, y + 22f), names[i], ko[i], 44, White, TA.Left, heavy: true);
                var b = Corner(Node(c, $"NotGoldRow{i}Go"), 482f, y - 34f, 598f, y + 34f);
                Btn(b, "Components/rb_btn_secondary");
                Text(Stretch(Node(b, $"NotGoldRow{i}GoText")), "ui.rb.src.go", "이동 ›", 40, White, TA.Center, heavy: true).margin = new Vector4(8f, 0f, 8f, 0f);
            }
        }

        /// <summary>구매 확인 — 아이콘 · 상품 이름 · 값(노랑) · 메모 · 닫기 / 구매. 시안 purchase-confirm 상자 280~920 × 740~1560.</summary>
        private static void BuildConfirm(RectTransform root)
        {
            Modal(root, "RbNotConfirm", "ui.rb.notice.buy_title", "구매 확인", 147f, 343f, 573f, 890f, out var c);
            Img(Corner(Node(c, "NotConfirmIcon"), 320f, 440f, 400f, 520f), "Icons/rb_icon_currency_gem_96");
            Text(Corner(Node(c, "NotConfirmName"), 170f, 554f, 550f, 592f), null, "입문 패키지 I 구매", 46, White, TA.Center, heavy: true);
            Text(Corner(Node(c, "NotConfirmPrice"), 170f, 614f, 550f, 680f), null, "₩5,900", 84, Yellow, TA.Center, number: true);
            var note = Text(Corner(Node(c, "NotConfirmNote"), 170f, 694f, 550f, 740f), null, "결제는 스토어 계정으로 진행됩니다", 36, Dim, TA.Center, heavy: true);
            note.enableWordWrapping = true;
            var no = Corner(Node(c, "NotConfirmNo"), 168f, 762f, 345f, 866f);
            Btn(no, "Components/rb_btn_secondary");
            Text(Stretch(Node(no, "NotConfirmNoText")), "ui.rb.notice.close", "닫기", 52, White, TA.Center, heavy: true);
            var yes = Corner(Node(c, "NotConfirmYes"), 360f, 758f, 552f, 870f);
            Btn(yes, "Components/rb_btn_primary");
            Text(Stretch(Node(yes, "NotConfirmYesText")), "ui.rb.shop.buy", "구매", 56, Ink, TA.Center, heavy: true);
        }
    }
}
