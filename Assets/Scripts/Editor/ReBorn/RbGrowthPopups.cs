// RE:BORN UI 리뉴얼(2026-10-24) — 호스트 성장 위에 뜨는 창 3종. 시안 Screens/star-confirm · star-success · skill.png(1080 × 2340).
// 좌표는 720 × 1280 판 안 게임 px: x = 시안 x × 2/3, y = (시안 y − 1185) × 2/3 + 640(시안 내용 가운데 = 판 가운데).
// 시안 창들이 판 가운데에서 조금씩 비껴 있어(시안 그림의 흔들림) 가로는 가운데로 맞췄다.
using UnityEngine;
using static Game.Editor.ReBorn.RbBuild;
using static Game.Editor.ReBorn.RbRailBuilder;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbGrowthPopups
    {
        public static void Build(RectTransform growth)
        {
            BuildStarAsk(growth);
            BuildStarDone(growth);
            BuildSkill(growth);
            BuildSources(growth);
            BuildEssence(growth);
            foreach (var n in new[] { "GrwStarAsk", "GrwStarDone", "GrwSkillInfo", "GrwSources", "GrwEssence" }) growth.Find(n)?.gameObject.SetActive(false);
        }

        // 조각 획득처 줄 — 이름 · 설명 · 꼬리표 · 버튼(시안 fragments-exchange.png). 버튼 이름 = GrwSrc{i}Go
        private static readonly (string icon, string title, string titleKo, string sub, string subKo, string tag, string tagKo, string btn, string btnKo)[] Sources =
        {
            ("Icons/rb_icon_tab_summon_96", "ui.rb.src.summon", "소환", "ui.rb.src.summon_sub", "이 몸 포함 · 중복 획득 시 조각 전환", "ui.rb.src.summon_tag", "중복 조각", "ui.rb.src.go", "이동 ›"),
            ("Icons/rb_icon_tab_challenge_96", "ui.rb.src.tower", "망령의 탑", "ui.rb.src.tower_sub", "해당 호스트의 보상 풀 확인", "ui.rb.src.reward_tag", "보상 확인", "ui.rb.src.reward", "보상 확인"),
            ("Icons/rb_icon_rail_event_96", "ui.rb.src.night", "망령의 밤", "ui.rb.src.night_sub", "오늘의 조각 보상 확인", "ui.rb.src.night_tag", "요일 보상", "ui.rb.src.reward", "보상 확인"),
            ("Icons/rb_icon_mode_random_96", "ui.rb.src.random", "랜덤 출전", null, "클리어 보너스 조각 +3", "ui.rb.src.random_tag", "클리어 보너스", "ui.rb.src.go", "이동 ›"),
            ("Icons/rb_icon_essence_96", "ui.rb.src.essence", "영혼 정수 → 조각", null, "정수 30개로 조각 1개", "ui.rb.src.essence_tag", "정수 교환", "ui.rb.src.exchange", "교환 ›"),
        };

        /// <summary>
        /// 조각 획득처 — 머리(조각 아이콘 · 「갱스터 조각」 · 보유 12 /20) + 다섯 줄. 시안 상자 165~1033 × 610~1765(가로는 가운데로).
        /// 줄 가운데(시안 y) 901 · 1084 · 1268 · 1452 · 1636 → 판 451 · 573 · 695 · 818 · 941.
        /// </summary>
        private static void BuildSources(RectTransform root)
        {
            Modal(root, "GrwSources", "ui.rb.src.title", "조각 획득처", 70f, 257f, 650f, 1037f, out var c);
            Img(Corner(Node(c, "GrwSourcesIcon"), 100f, 296f, 153f, 350f), "Icons/rb_icon_shard_96");
            Text(Corner(Node(c, "GrwSourcesName"), 177f, 302f, 620f, 338f), null, "갱스터 조각", 58, White, TA.Left, heavy: true);
            Text(Corner(Node(c, "GrwSourcesOwned"), 177f, 340f, 620f, 370f), null, "보유 12 /20", 46, Dim, TA.Left, heavy: true);
            float[] centers = { 461f, 583f, 705f, 828f, 951f };
            for (int i = 0; i < Sources.Length; i++)
            {
                var s = Sources[i];
                float y = centers[i];
                Img(Corner(Node(c, $"GrwSrc{i}Icon"), 100f, y - 30f, 153f, y + 30f), s.icon);
                Text(Corner(Node(c, $"GrwSrc{i}Title"), 163f, y - 58f, 490f, y - 22f), s.title, s.titleKo, 54, White, TA.Left, heavy: true);
                Text(Corner(Node(c, $"GrwSrc{i}Sub"), 163f, y - 10f, 490f, y + 18f), s.sub, s.subKo, 40, Dim, TA.Left, heavy: true);
                var tag = Corner(Node(c, $"GrwSrc{i}Tag"), 163f, y + 23f, 310f, y + 51f);
                Img(tag, "Components/rb_info_chip").preserveAspect = false;
                Text(Stretch(Node(tag, $"GrwSrc{i}TagText")), s.tag, s.tagKo, 38, Cyan, TA.Center, heavy: true);
                var go = Corner(Node(c, $"GrwSrc{i}Go"), 499f, y - 42f, 622f, y + 41f);
                Btn(go, "Components/rb_btn_secondary");
                Text(Stretch(Node(go, $"GrwSrc{i}GoText")), s.btn, s.btnKo, 44, White, TA.Center, heavy: true).margin = new Vector4(10f, 0f, 10f, 0f);   // 둥근 모서리를 비켜
            }
        }

        /// <summary>
        /// 영혼 정수 → 조각 교환 — 정수 아이콘 → 조각(+얼굴) · 비율 줄 · − 수 + · 필요/보유 · 교환(모자라면 「정수 n개 부족」).
        /// 시안 essence-exchange · essence-poor.png, 상자 330~870 × 600~1705(가로는 가운데로).
        /// </summary>
        private static void BuildEssence(RectTransform root)
        {
            Modal(root, "GrwEssence", "ui.rb.ess.title", "영혼 정수", 180f, 250f, 540f, 987f, out var c);
            Img(Corner(Node(c, "GrwEssIcon"), 212f, 414f, 284f, 486f), "Icons/rb_icon_essence_96");
            Text(Corner(Node(c, "GrwEssLabel"), 172f, 512f, 324f, 542f), "ui.rb.ess.title", "영혼 정수", 38, White, TA.Center, heavy: true);
            Img(Corner(Node(c, "GrwEssArrow"), 338f, 460f, 366f, 488f), "Icons/rb_icon_sys_next_48");
            Img(Corner(Node(c, "GrwEssShard"), 416f, 359f, 500f, 443f), "Icons/rb_icon_shard_96");
            Img(Corner(Node(c, "GrwEssFace"), 440f, 470f, 490f, 520f), "Icons/rb_icon_mode_ghost_96");
            var shardName = Text(Corner(Node(c, "GrwEssShardName"), 372f, 526f, 540f, 574f), null, "갱스터 조각", 38, White, TA.Top, heavy: true);
            shardName.enableWordWrapping = true;   // 일본어 「ギャングスターのかけら」는 두 줄
            Text(Corner(Node(c, "GrwEssRate"), 203f, 622f, 520f, 660f), null, "정수 30개로 조각 1개", 46, White, TA.Left, heavy: true);
            var minus = Corner(Node(c, "GrwEssMinus"), 232f, 689f, 317f, 773f);
            Btn(minus, "Components/rb_btn_icon_round");
            Img(Inside(Node(minus, "GrwEssMinusIcon"), 42.5f, 42f, 36f, 36f), "Icons/rb_icon_sys_minus_48");
            Text(Corner(Node(c, "GrwEssCount"), 320f, 695f, 400f, 767f), null, "1", 96, White, TA.Center, number: true);
            var plus = Corner(Node(c, "GrwEssPlus"), 403f, 689f, 488f, 773f);
            Btn(plus, "Components/rb_btn_icon_round");
            Img(Inside(Node(plus, "GrwEssPlusIcon"), 42.5f, 42f, 36f, 36f), "Icons/rb_icon_sys_plus_48");
            Text(Corner(Node(c, "GrwEssNeed"), 203f, 806f, 520f, 840f), null, "필요 정수 30 · 보유 120", 44, Dim, TA.Left, heavy: true);
            var go = Corner(Node(c, "GrwEssGo"), 207f, 866f, 513f, 962f);
            Btn(go, "Components/rb_btn_primary");
            Text(Stretch(Node(go, "GrwEssGoText")), "ui.rb.ess.go", "교환", 60, Ink, TA.Center, heavy: true);
        }

        /// <summary>성급 강화 확인 — 얼굴 · 「갱스터 성급 2 → 3」 / 조각 칸 · 골드 칸 / 효과 두 줄 / 취소 · 성급 올리기.</summary>
        private static void BuildStarAsk(RectTransform root)
        {
            Modal(root, "GrwStarAsk", "ui.rb.growth.starup_title", "성급 강화", 180f, 317f, 620f, 943f, out var c);
            Img(Corner(Node(c, "GrwStarAskFace"), 222f, 410f, 282f, 462f), "Icons/rb_icon_mode_ghost_96");
            Text(Corner(Node(c, "GrwStarAskLine"), 296f, 410f, 600f, 462f), null, "갱스터 성급 2 → 3", 56, White, TA.Left, heavy: true);

            var shard = Corner(Node(c, "GrwStarAskShard"), 205f, 505f, 395f, 670f);
            Img(shard, "Components/rb_slot_common");
            Img(Corner(Node(c, "GrwStarAskShardIcon"), 262f, 518f, 322f, 578f), "Icons/rb_icon_shard_96");
            Img(Corner(Node(c, "GrwStarAskShardFace"), 324f, 568f, 378f, 622f), "Icons/rb_icon_mode_ghost_96");
            Text(Corner(Node(c, "GrwStarAskShardCount"), 220f, 588f, 324f, 622f), null, "20", 56, White, TA.Right, number: true);
            Text(Corner(Node(c, "GrwStarAskShardName"), 215f, 626f, 385f, 656f), null, "갱스터 조각", 38, White, TA.Center, heavy: true);

            var gold = Corner(Node(c, "GrwStarAskGold"), 405f, 505f, 595f, 670f);
            Img(gold, "Components/rb_slot_ssr");
            Img(Corner(Node(c, "GrwStarAskGoldIcon"), 468f, 518f, 532f, 582f), "Icons/rb_icon_currency_gold_96");
            Text(Corner(Node(c, "GrwStarAskGoldCount"), 415f, 588f, 585f, 622f), null, "1,200", 56, White, TA.Center, number: true);
            Text(Corner(Node(c, "GrwStarAskGoldName"), 415f, 626f, 585f, 656f), "ui.rb.result.gold", "골드", 38, White, TA.Center, heavy: true);

            Text(Corner(Node(c, "GrwStarAskGain"), 207f, 688f, 595f, 720f), null, "체력·공격 +7%", 50, Green, TA.Left, heavy: true);
            Text(Corner(Node(c, "GrwStarAskCap"), 207f, 738f, 595f, 772f), "ui.rb.growth.cap_up", "능력치 강화 상한 상승", 46, White, TA.Left, heavy: true);

            var no = Corner(Node(c, "GrwStarAskNo"), 207f, 808f, 385f, 904f);
            Btn(no, "Components/rb_btn_secondary");
            Text(Stretch(Node(no, "GrwStarAskNoText")), "ui.common.cancel", "취소", 52, White, TA.Center, heavy: true);
            var yes = Corner(Node(c, "GrwStarAskYes"), 397f, 804f, 595f, 908f);
            Btn(yes, "Components/rb_btn_primary");
            Text(Stretch(Node(yes, "GrwStarAskYesText")), "ui.rb.growth.starup", "성급 올리기", 50, Ink, TA.Center, heavy: true);
        }

        /// <summary>성급이 올랐어요 — 얼굴 · 별 다섯 · 「성급 3」 · 효과 · 전투력 증가 · 확인.</summary>
        private static void BuildStarDone(RectTransform root)
        {
            Modal(root, "GrwStarDone", "ui.rb.growth.starup_done", "성급이 올랐어요", 170f, 293f, 550f, 950f, out var c);
            Img(Corner(Node(c, "GrwStarDoneFace"), 330f, 418f, 390f, 470f), "Icons/rb_icon_mode_ghost_96");
            var stars = Corner(Node(c, "GrwStarDoneStars"), 193f, 525f, 527f, 585f);
            for (int i = 0; i < 5; i++)
                Img(Corner(Node(stars, $"GrwStarDoneStar{i}"), 4f + i * 67f, 0f, 4f + i * 67f + 58f, 58f), "Icons/rb_icon_star_on_96");
            Text(Corner(Node(c, "GrwStarDoneRank"), 200f, 620f, 520f, 670f), null, "성급 3", 72, White, TA.Center, heavy: true);
            Text(Corner(Node(c, "GrwStarDoneGain"), 200f, 698f, 520f, 730f), null, "체력·공격 +7%", 48, Green, TA.Center, heavy: true);
            Text(Corner(Node(c, "GrwStarDonePower"), 200f, 751f, 520f, 785f), "ui.rb.growth.power_up", "전투력 증가", 46, Green, TA.Center, heavy: true);
            var ok = Corner(Node(c, "GrwStarDoneOk"), 197f, 827f, 523f, 925f);
            Btn(ok, "Components/rb_btn_primary");
            Text(Stretch(Node(ok, "GrwStarDoneOkText")), "ui.common.ok", "확인", 64, Ink, TA.Center, heavy: true);
        }

        /// <summary>스킬 정보 — 아이콘 · 종류 표 · 이름 / 설명. 시안의 「각성 옵션 열림」 줄은 게임에 각성 옵션 자료가 없어 뺐다.</summary>
        private static void BuildSkill(RectTransform root)
        {
            Modal(root, "GrwSkillInfo", "ui.rb.growth.skill_title", "스킬 정보", 48f, 430f, 672f, 740f, out var c);
            Img(Corner(Node(c, "GrwSkillInfoIcon"), 76f, 484f, 182f, 590f), "Icons/rb_icon_skill_base_96");
            Text(Corner(Node(c, "GrwSkillInfoKind"), 205f, 496f, 640f, 530f), null, "필살기", 38, Yellow, TA.Left, heavy: true);
            Text(Corner(Node(c, "GrwSkillInfoName"), 205f, 536f, 640f, 586f), null, "청부 처형", 58, White, TA.Left, heavy: true);
            var desc = Text(Corner(Node(c, "GrwSkillInfoDesc"), 76f, 612f, 644f, 716f), null,
                            "가장 체력이 높은 적(보스 포함)을 1.5초 조준한 뒤 초대형 한 발. 보스는 최대 체력의 % 피해.", 42, White, TA.TopLeft, heavy: true);
            desc.enableWordWrapping = true;   // 설명은 두세 줄
        }
    }
}
