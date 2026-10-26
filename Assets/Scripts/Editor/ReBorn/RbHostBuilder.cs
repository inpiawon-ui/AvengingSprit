// RE:BORN UI 리뉴얼(2026-10-23) — 호스트 탭: 도감(Screens/collection.png) · 호스트 성장 한 화면(Screens/growth.png).
// 로비 프리팹(RbLobbyUI) 안의 쪽(page)으로 세운다 — 위 막대 · 아래 탭은 로비 것을 같이 쓴다. 좌표는 시안 1080 픽셀 상자.
using System.IO;
using Game.Module.Lobby;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.ReBorn.RbBuild;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbHostBuilder
    {
        private const string UnitDir = "Assets/BaseResource/Unit";
        private const string SkillIconDir = "Assets/BaseResource/HostSelectPanel";
        private const string PassiveFallback = "Assets/BaseResource/Growth/passive_icon.png";

        // ── 공용: 쪽 바탕 · 쪽 머리(뒤로 · 제목 · 도움말) ───────────────────

        public static RectTransform Page(RectTransform rt, string name)
        {
            var page = Stretch(Node(rt, name));
            var bg = Stretch(Node(page, name + "Bg"));
            var img = Img(bg, "Art/rb_art_bg_outgame_blur");
            img.preserveAspect = false;
            // 화면을 덮는다(검은 여백 없이) — 그림 비율 그대로 넘치는 쪽을 자른다
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = img.sprite != null ? img.sprite.rect.width / img.sprite.rect.height : 1080f / 2340f;
            page.gameObject.AddComponent<RectMask2D>();
            return page;
        }

        /// <summary>뒤로(왼쪽 원) · 제목 · 도움말(오른쪽 원). 이름 = `{p}Back` · `{p}Title` · `{p}Help`.</summary>
        public static void Head(RectTransform page, string p, string titleKey, string korean,
                                float bx0, float by0, float bx1, float by1, float tx1, float hx0)
        {
            var back = Place(Node(page, p + "Back"), Edge.Top, bx0, by0, bx1, by1);
            Btn(back, "Components/rb_btn_icon_round");
            float c = (bx0 + bx1) * 0.5f, m = (by0 + by1) * 0.5f;
            Img(Local(Node(back, p + "BackIcon"), bx0, by0, bx1, by1, c - 32, m - 32, c + 32, m + 32), "Icons/rb_icon_sys_back_96");
            Text(Place(Node(page, p + "Title"), Edge.Top, bx1 + 32, m - 42, tx1, m + 42), titleKey, korean, 72, White, TA.Left, heavy: true);
            float hx1 = hx0 + (bx1 - bx0);
            var help = Place(Node(page, p + "Help"), Edge.Top, hx0, by0, hx1, by1);
            Btn(help, "Components/rb_btn_icon_round");
            float hc = (hx0 + hx1) * 0.5f;
            Img(Local(Node(help, p + "HelpIcon"), hx0, by0, hx1, by1, hc - 32, m - 32, hc + 32, m + 32), "Icons/rb_icon_sys_help_96");
        }

        // ═════════ 도감 ═════════

        public const float CardTop = 862f, CardH = 386f, CardStepY = 405f;
        public static readonly float[] CardCols = { 30f, 375f, 722f };   // 카드 왼쪽 x(폭 325 · 330 · 328 — 시안 그대로 둘째는 330)
        public const float CardW = 328f;

        public static void BuildCollection(RectTransform rt)
        {
            var page = Page(rt, "PageHost");
            Head(page, "Col", "ui.rb.host.title", "호스트", 35, 280, 165, 410, 600, 915);
            Text(Place(Node(page, "ColOwned"), Edge.Top, 600, 322, 895, 372), null, "보유 <color=#FFFFFF>23</color> /25", 40, Dim, TA.Right, number: false);

            // 유령 카드 — 모든 호스트에 붙는 성장
            const float gx0 = 30, gy0 = 450, gx1 = 1050, gy1 = 690;
            var ghost = Place(Node(page, "ColGhostCard"), Edge.Top, gx0, gy0, gx1, gy1);
            Img(ghost, "Components/rb_panel_base");
            Img(Local(Node(ghost, "ColGhostArt"), gx0, gy0, gx1, gy1, 58, 488, 218, 640), "Art/rb_art_mascot_ghost_happy");
            Text(Local(Node(ghost, "ColGhostLv"), gx0, gy0, gx1, gy1, 252, 488, 800, 552), null, "유령 Lv 12", 56, White, TA.Left, heavy: true);
            Text(Local(Node(ghost, "ColGhostDesc"), gx0, gy0, gx1, gy1, 252, 572, 820, 616), "ui.rb.host.ghost_desc", "모든 호스트에 붙는 성장", 36, Dim, TA.Left);
            Bar(Local(Node(ghost, "ColGhostExp"), gx0, gy0, gx1, gy1, 252, 638, 815, 658), "ColGhostExpFill", "Components/rb_bar_fill_cyan");
            var grow = Local(Node(ghost, "ColGhostGrow"), gx0, gy0, gx1, gy1, 847, 500, 1025, 640);
            Btn(grow, "Components/rb_btn_secondary");
            Text(Stretch(Node(grow, "ColGhostGrowText")), "ui.rb.host.grow", "성장 ›", 44, White, TA.Center, heavy: true);

            // 거르기 칩 — 전체 · 파워 · 무기 · 마법 · 정렬
            float[][] chips = { new[] { 30f, 212f }, new[] { 228f, 410f }, new[] { 427f, 610f }, new[] { 626f, 808f }, new[] { 824f, 1050f } };
            string[] keys = { "ui.rb.host.filter_all", "ui.affinity.force", "ui.affinity.blade", "ui.affinity.magic", null };
            string[] ko = { "전체", "파워", "무기", "마법", "전투력 순" };
            string[] affChip = { "Icons/rb_icon_aff_force_128", "Icons/rb_icon_aff_blade_128", "Icons/rb_icon_aff_magic_128" };
            for (int i = 0; i < chips.Length; i++)
            {
                var c = Place(Node(page, $"ColFilter{i}"), Edge.Top, chips[i][0], 720, chips[i][1], 835);
                // 시안은 알약(btn_chip)이 아니라 모서리만 둥근 직사각형 버튼(btn_secondary) — 묶음 final.css `button.btn`(PD 2026-10-26)
                Btn(c, "Components/rb_btn_secondary");
                // 속성 칩은 글자 앞에 카드와 같은 속성 아이콘 — 유저가 아이콘과 속성 이름을 짝지어 익히게(PD 2026-10-26 B안)
                // 아이콘 60 · 글자 35, 묶음 가운데 맞춤 · 언어별 줄임은 화면 스크립트가 한다
                Text(Local(Node(c, $"ColFilter{i}Text"), chips[i][0], 720, chips[i][1], 835, chips[i][0] + 12, 745, chips[i][1] - 12, 805),
                     keys[i], ko[i], i is >= 1 and <= 3 ? 35 : 38, Dim, TA.Center, heavy: true);
                if (i is >= 1 and <= 3)
                    Img(Local(Node(c, $"ColFilter{i}Icon"), chips[i][0], 720, chips[i][1], 835, chips[i][0] + 12, 748, chips[i][0] + 72, 808),
                        affChip[i - 1]);
            }
            // 정렬 칩 — 글꼴에 ⇅ 가 없어 키트 아이콘을 글자 오른쪽에 둔다(화면 스크립트가 글자 폭을 재서 붙인다)
            Img(Local(Node(page.Find("ColFilter4") as RectTransform, "ColFilter4Icon"), 824, 720, 1050, 835, 990, 756, 1026, 792), "Icons/rb_icon_sys_sort_48");

            // 카드 칸 — 세로로 넘긴다(아래 탭 위까지)
            var content = VScroll(page, "ColGrid", 850, 2040, 1200);
            var card = Node(content, "ColCardTemplate");
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f); card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(CardW * S, CardH * S);
            card.anchoredPosition = new Vector2((CardCols[0] + CardW * 0.5f - 540f) * S, -(CardTop + CardH * 0.5f - 850f) * S);
            HitArea(card);
            float x0 = 30, y0 = CardTop, x1 = 30 + CardW, y1 = CardTop + CardH;
            // 카드 바탕 — 시안(묶음 v6.css rarity-inner6)은 짙은 남색 위에 「카드 아래 75% 지점을 가운데로 폭 90% · 높이 70% 반지름」
            // 등급색이 둥글게 퍼진다. 둥근 빛 한 장(rb_card_glow)을 그 자리 · 크기로 깔고 카드 안으로 잘라 낸다(PD 2026-10-26
            // 「시안은 블러를 전체적으로 · 동그랗게 퍼진다, 지금은 사각 블러」 — 예전엔 아래 절반에 네모 판을 깔았다)
            var inner = Stretch(Node(card, "ColCardInner"));
            inner.sizeDelta = new Vector2(-6f, -6f);
            inner.gameObject.AddComponent<RectMask2D>();
            Img(inner, "Components/rb_panel_base");
            var glowRt = Node(inner, "ColCardGlow");
            glowRt.anchorMin = glowRt.anchorMax = new Vector2(0.5f, 0.25f);
            glowRt.pivot = new Vector2(0.5f, 0.5f);
            glowRt.sizeDelta = new Vector2(CardW * S * 1.8f, CardH * S * 1.4f);
            glowRt.anchoredPosition = Vector2.zero;
            var glow = Img(glowRt, "Components/rb_card_glow");
            glow.type = Image.Type.Simple;
            glow.preserveAspect = false;
            // 틀은 몸통(테 선)이 카드 칸에 딱 맞게 — 화면 스크립트가 등급마다 다시 맞춘다(RbFrameFit)
            var frame = Node(card, "ColCardFrame");
            Img(frame, "Components/rb_host_frame_r").preserveAspect = false;
            RbLobbyBuilder.RbFrameFitEditor(frame, "Components/rb_host_frame_r");
            // 위 왼쪽 = 내 속성(네모 칸 안) · 위 오른쪽 = 등급 태그 — 로비 출전 카드와 같은 자리(PD 2026-10-26)
            // 「Lv」 줄(능력치 강화 단계 합)은 뺐다 — 호스트 레벨은 없다, 별 단계가 강화 상한(한 단계 5 · 10성 50)을 정한다(PD 2026-10-26).
            // 빈 자리만큼 위 두 표시를 키웠다(「등급이 잘 안 보인다 · 공간이 여유롭다」)
            var affBox = Local(Node(card, "ColCardAffBox"), x0, y0, x1, y1, 40, 872, 122, 954);
            Img(affBox, "Components/rb_level_badge").pixelsPerUnitMultiplier = 1.2f;
            Img(Local(Node(affBox, "ColCardAff"), 40, 872, 122, 954, 47, 879, 115, 947), "Icons/rb_icon_aff_force_128");
            Img(Local(Node(card, "ColCardRarity"), x0, y0, x1, y1, 236, 876, 348, 952), "Components/rb_rarity_r");
            // 빨간 점 — 등급 태그 오른쪽 위 모서리에 얹는다(성급을 올릴 수 있다)
            Img(Local(Node(card, "ColCardDot"), x0, y0, x1, y1, 326, 864, 354, 892), "Components/rb_dot_red");
            Img(Local(Node(card, "ColCardFace"), x0, y0, x1, y1, 138, 946, 250, 1058), "Icons/rb_icon_mode_ghost_144");
            Img(Local(Node(card, "ColCardLock"), x0, y0, x1, y1, 165, 973, 223, 1031), "Icons/rb_icon_sys_lock_96");
            // 긴 이름은 RbCardName 이 괄호 앞에서 두 줄로 접는다
            var colName = Text(Local(Node(card, "ColCardName"), x0, y0, x1, y1, 42, 1062, 346, 1120), null, "아마존", 36, White, TA.Center, heavy: true);
            colName.lineSpacing = -28f;
            var stars = Local(Node(card, "ColCardStars"), x0, y0, x1, y1, 66, 1122, 310, 1172);
            for (int i = 0; i < 5; i++)
                Img(Local(Node(stars, $"ColCardStar{i}"), 66, 1122, 310, 1172, 68 + i * 48, 1124, 112 + i * 48, 1170), "Icons/rb_icon_star_off_48");
            // 전투력(가진 몸) · 조각 진행(안 가진 몸)
            Img(Local(Node(card, "ColCardPowerIcon"), x0, y0, x1, y1, 100, 1186, 140, 1222), "Icons/rb_icon_stat_power_48");
            // 칸은 카드 폭 — 화면 스크립트(CenterRun)가 글자 폭을 재서 아이콘과 같이 가운데에 세운다(「Shards 88/88」 도 들어가게)
            Text(Local(Node(card, "ColCardPower"), x0, y0, x1, y1, 42, 1180, 346, 1228), null, "3,230", 44, White, TA.Left, number: true);
        }

        // ═════════ 호스트 성장 ═════════

        /// <summary>성장 쪽 스크롤 내용의 위 기준 줄(시안 y) — 쪽 머리 바로 아래.</summary>
        public const float GrowTop = 430f;
        public const float StatRow0 = 1215f, StatStepY = 162.6f, StatRowH = 140f;
        public static readonly float[][] StatCols = { new[] { 25f, 530f }, new[] { 550f, 1052f } };

        public static void BuildGrowth(RectTransform rt)
        {
            var page = Page(rt, "PageGrowth");
            Head(page, "Grw", "ui.rb.growth.title", "호스트 성장", 25, 290, 155, 410, 800, 922);
            var body = VScroll(page, "GrwBody", GrowTop, 2040, 2030 - GrowTop);
            const float T = GrowTop;

            // 히어로 판 — 왼쪽에 챕터 무대 그림(판 안에 잘라 넣는다) · 받침 · 몸
            const float hx0 = 30, hy0 = 440, hx1 = 1050, hy1 = 835;
            var hero = PlaceIn(Node(body, "GrwHero"), T, hx0, hy0, hx1, hy1);
            Img(hero, "Components/rb_panel_hero");
            var clip = Local(Node(hero, "GrwHeroClip"), hx0, hy0, hx1, hy1, 42, 452, 640, 823);
            clip.gameObject.AddComponent<RectMask2D>();
            var stage = Node(clip, "GrwHeroStage");
            stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
            stage.sizeDelta = new Vector2(606f * S * 1.1f, 606f * S * 1.1f * 2520f / 1080f);
            stage.anchoredPosition = new Vector2(0f, -40f * S);   // 그림 가운데(물 반사 위 거리)가 판 높이에 오게
            Img(stage, "Art/rb_art_bg_stage_ch01").preserveAspect = false;
            var shade = Local(Node(hero, "GrwHeroShade"), hx0, hy0, hx1, hy1, 42, 452, 640, 823);
            Img(shade, "Components/rb_fade_bottom").preserveAspect = false;
            // 옆으로 눕힌 그늘 — 그림 위 변(투명)이 왼쪽, 아래 변(짙음)이 오른쪽에 오게 90도
            var edge = Node(hero, "GrwHeroEdge");
            edge.anchorMin = edge.anchorMax = new Vector2(0.5f, 0.5f);
            edge.sizeDelta = new Vector2((823f - 452f) * S, 260f * S);
            edge.anchoredPosition = new Vector2((640f - 130f - (hx0 + hx1) * 0.5f) * S, -((452f + 823f) * 0.5f - (hy0 + hy1) * 0.5f) * S);
            edge.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Img(edge, "Components/rb_fade_bottom").preserveAspect = false;
            Img(Local(Node(hero, "GrwPedestal"), hx0, hy0, hx1, hy1, 70, 640, 450, 710), "Art/rb_art_host_pedestal");
            Img(Local(Node(hero, "GrwHostArt"), hx0, hy0, hx1, hy1, 110, 450, 410, 700), "Art/rb_art_mascot_ghost_happy");
            Text(Local(Node(hero, "GrwName"), hx0, hy0, hx1, hy1, 420, 455, 800, 532), null, "갱스터", 66, White, TA.Center, heavy: true);
            for (int i = 0; i < 3; i++)
            {
                var tag = Local(Node(hero, $"GrwTag{i}"), hx0, hy0, hx1, hy1, 520 + i * 100, 553, 600 + i * 100, 607);
                Img(tag, "Components/rb_info_chip");
                Text(Stretch(Node(tag, $"GrwTag{i}Text")), null, "R", 36, Dim, TA.Center, heavy: true);
            }
            Text(Local(Node(hero, "GrwPowerLabel"), hx0, hy0, hx1, hy1, 420, 655, 600, 708), "ui.rb.growth.power", "전투력", 38, White, TA.Right, heavy: true);
            Text(Local(Node(hero, "GrwPowerValue"), hx0, hy0, hx1, hy1, 618, 630, 1000, 712), null, "2,159", 84, White, TA.Left, number: true);
            var stars = Local(Node(hero, "GrwStars"), hx0, hy0, hx1, hy1, 515, 740, 835, 795);
            for (int i = 0; i < 5; i++)
                Img(Local(Node(stars, $"GrwStar{i}"), 515, 740, 835, 795, 520 + i * 63, 742, 572 + i * 63, 794), "Icons/rb_icon_star_off_48");
            // 강화 성공 「전투력 +12」(시안 growth-success) — 판 왼쪽 위 무대 그림 위. 화면 스크립트가 잠깐 띄운다
            var powerUp = Local(Node(hero, "GrwPowerUp"), hx0, hy0, hx1, hy1, 70, 458, 430, 532);
            Img(powerUp, "Components/rb_info_chip").preserveAspect = false;
            Text(Stretch(Node(powerUp, "GrwPowerUpText")), null, "전투력 +12", 46, Green, TA.Center, number: true);
            powerUp.gameObject.SetActive(false);

            // 스킬 3칸 — 필살기 · 액티브 · 패시브(누르면 자세히)
            float[][] tiles = { new[] { 25f, 355f }, new[] { 372f, 705f }, new[] { 720f, 1050f } };
            string[] tn = { "Ult", "Act", "Pas" };
            string[] tk = { "ui.rb.growth.ult", "ui.rb.growth.act", "ui.rb.growth.pas" };
            string[] tko = { "필살기", "액티브", "패시브" };
            for (int i = 0; i < 3; i++)
            {
                float x0 = tiles[i][0], x1 = tiles[i][1], cx = (x0 + x1) * 0.5f;
                var t = PlaceIn(Node(body, $"GrwSkill{tn[i]}"), T, x0, 860, x1, 1140);
                Img(t, "Components/rb_panel_base");
                t.gameObject.AddComponent<Button>().targetGraphic = t.GetComponent<Image>();
                t.GetComponent<Image>().raycastTarget = true;
                Img(Local(Node(t, $"GrwSkill{tn[i]}Icon"), x0, 860, x1, 1140, cx - 58, 892, cx + 58, 1008), "Icons/rb_icon_mode_ghost_144");
                Img(Local(Node(t, $"GrwSkill{tn[i]}Ring"), x0, 860, x1, 1140, cx - 86, 864, cx + 86, 1036), "Components/rb_profile_frame");
                Text(Local(Node(t, $"GrwSkill{tn[i]}Kind"), x0, 860, x1, 1140, x0 + 10, 1038, x1 - 10, 1078), tk[i], tko[i], 36, Dim, TA.Center, heavy: true);
                Text(Local(Node(t, $"GrwSkill{tn[i]}Name"), x0, 860, x1, 1140, x0 + 10, 1082, x1 - 10, 1134), null, "청부 처형", 46, White, TA.Center, heavy: true);
            }

            // 능력치 머리 · 길게 누르기 안내
            Text(PlaceIn(Node(body, "GrwStatHeader"), T, 30, 1152, 520, 1204), "ui.rb.growth.stat_header", "◆ 능력치", 52, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "GrwStatHint"), T, 560, 1158, 1050, 1200), "ui.rb.growth.hold_hint", "길게 누르면 연속 강화", 36, Cyan, TA.Right);

            // 능력치 칸 본보기(8칸으로 복제 — 화면 스크립트) — 판 · 아이콘 · 이름 · 값 · 강화 띠(▲ 강화 · 동전 · 값)
            float sx0 = StatCols[0][0], sx1 = StatCols[0][1], sy0 = StatRow0, sy1 = StatRow0 + StatRowH;
            var tile = PlaceIn(Node(body, "GrwStatTemplate"), T, sx0, sy0, sx1, sy1);
            Img(tile, "Components/rb_stat_tile");
            // 「Lv 0/50」 줄은 뺐다(PD 2026-10-26) — 아이콘을 칸 세로 가운데로
            Img(Local(Node(tile, "GrwStatIcon"), sx0, sy0, sx1, sy1, 50, sy0 + 29, 126, sy0 + 105), "Icons/rb_icon_stat_attack_96");
            // 일본어 「クリティカルダメージ」 가 가장 길다 — 하한을 낮춰 칸 안에서 줄인다(규칙 06 넘침 금지)
            Text(Local(Node(tile, "GrwStatName"), sx0, sy0, sx1, sy1, 148, sy0 + 16, 372, sy0 + 62), null, "공격력", 36, Dim, TA.Left).fontSizeMin = 22f * S;
            Text(Local(Node(tile, "GrwStatValue"), sx0, sy0, sx1, sy1, 148, sy0 + 68, 372, sy0 + 124), null, "+6% › +8%", 40, White, TA.Left, number: true);
            // 강화 버튼 — 초록 띠 대신 차분한 남색 버튼에 「금화 + 값」 한 줄(PD 2026-10-26 「녹색 버튼 없애고 차분하게」).
            // 「▲ 강화」 글자는 뺐다. 값은 금색 · 골드가 모자라면 빨강(화면 스크립트)
            const float bx0 = 392, bx1 = 518;
            float by0 = sy0 + 30, by1 = sy0 + 104;
            var band = Local(Node(tile, "GrwStatBuy"), sx0, sy0, sx1, sy1, bx0, by0, bx1, by1);
            Btn(band, "Components/rb_btn_secondary");
            band.gameObject.AddComponent<Game.Module.Common.UI.RbHoldRepeat>();
            Img(Local(Node(band, "GrwStatBuyCoin"), bx0, by0, bx1, by1, 402, sy0 + 47, 442, sy0 + 87), "Icons/rb_icon_currency_gold_48");
            Text(Local(Node(band, "GrwStatBuyCost"), bx0, by0, bx1, by1, 446, sy0 + 44, 512, sy0 + 90), null, "60", 38, Yellow, TA.Center, number: true);

            // 성급 판 — 조각 아이콘 · 「성급 2 › 3」 · 조각 획득처 · 막대 · 「12/20 · 체력·공격 +7%」
            const float px0 = 25, py0 = 1880, px1 = 640, py1 = 2020;
            var star = PlaceIn(Node(body, "GrwStarPanel"), T, px0, py0, px1, py1);
            Img(star, "Components/rb_panel_base");
            Img(Local(Node(star, "GrwStarShardIcon"), px0, py0, px1, py1, 36, 1918, 104, 1986), "Icons/rb_icon_shard_96");
            Text(Local(Node(star, "GrwStarRank"), px0, py0, px1, py1, 120, 1884, 420, 1946), null, "성급 2 › 3", 48, White, TA.Left, heavy: true);
            var where = Local(Node(star, "GrwStarWhere"), px0, py0, px1, py1, 428, 1884, 634, 1950);
            Btn(where, "Components/rb_btn_chip");
            Text(Stretch(Node(where, "GrwStarWhereText")), "ui.rb.growth.shard_where", "조각 획득처 ›", 34, White, TA.Center, heavy: true);
            Bar(Local(Node(star, "GrwStarBar"), px0, py0, px1, py1, 124, 1958, 426, 1972), "GrwStarBarFill", "Components/rb_bar_fill_cyan");
            Text(Local(Node(star, "GrwStarInfo"), px0, py0, px1, py1, 120, 1976, 632, 2018), null, "12/20 · 체력·공격 +7%", 38, White, TA.Left).fontSizeMin = 26f * S;

            var up = PlaceIn(Node(body, "GrwStarUp"), T, 660, 1880, 1055, 2020);
            Btn(up, "Components/rb_btn_secondary");
            Text(Local(Node(up, "GrwStarUpLabel"), 660, 1880, 1055, 2020, 680, 1890, 1035, 1958), "ui.rb.growth.starup", "성급 올리기", 50, White, TA.Center, heavy: true);
            Text(Local(Node(up, "GrwStarUpSub"), 660, 1880, 1055, 2020, 680, 1958, 1035, 2008), null, "조각 8개 부족", 36, Dim, TA.Center);
        }

        /// <summary>몸마다 스킬 아이콘(필살기 · 액티브 · 패시브) — 몸 그림 폴더(Unit/{key})가 있는 것만.</summary>
        public static void WireGrowth(RbGrowthPage page)
        {
            var so = new SerializedObject(page);
            var arts = so.FindProperty("_arts");
            arts.arraySize = 0;
            foreach (var d in Directory.GetDirectories(UnitDir))
            {
                string key = Path.GetFileName(d);
                var ult = AssetDatabase.LoadAssetAtPath<Sprite>($"{SkillIconDir}/ulticon_{key}.png");
                var act = AssetDatabase.LoadAssetAtPath<Sprite>($"{SkillIconDir}/ultimateicon_{key}.png");
                var pas = AssetDatabase.LoadAssetAtPath<Sprite>($"{SkillIconDir}/passiveicon_{key}.png");
                if (ult == null && act == null) continue;
                int i = arts.arraySize++;
                var e = arts.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = key;
                e.FindPropertyRelative("Ult").objectReferenceValue = ult != null ? ult : act;
                e.FindPropertyRelative("Active").objectReferenceValue = act;
                e.FindPropertyRelative("Passive").objectReferenceValue = pas != null ? pas : AssetDatabase.LoadAssetAtPath<Sprite>(PassiveFallback);
            }
            // HostStat 순서 — Hp · Atk · Crit · AtkSpeed · Range · MoveSpeed · CritDamage · Defense
            string[] stat = { "hp", "attack", "crit", "attackspeed", "range", "movespeed", "critdamage", "defense" };
            var icons = so.FindProperty("_statIcons");
            icons.arraySize = stat.Length;
            for (int i = 0; i < stat.Length; i++) icons.GetArrayElementAtIndex(i).objectReferenceValue = KitSprite($"Icons/rb_icon_stat_{stat[i]}_96");
            Stars(so);
            so.FindProperty("_tileNormal").objectReferenceValue = KitSprite("Components/rb_stat_tile");
            so.FindProperty("_tileMax").objectReferenceValue = KitSprite("Components/rb_stat_tile_max");
            so.FindProperty("_tilePoor").objectReferenceValue = KitSprite("Components/rb_stat_tile_poor");
            so.FindProperty("_btnReady").objectReferenceValue = KitSprite("Components/rb_btn_primary");
            so.FindProperty("_btnShort").objectReferenceValue = KitSprite("Components/rb_btn_secondary");
            var bgs = so.FindProperty("_stageBgs");
            bgs.arraySize = 10;
            for (int i = 0; i < 10; i++) bgs.GetArrayElementAtIndex(i).objectReferenceValue = KitSprite($"Art/rb_art_bg_stage_ch{i + 1:00}");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void WireCollection(RbHostPage page)
        {
            var so = new SerializedObject(page);
            Stars(so);
            void Many(string field, params string[] paths)
            {
                var p = so.FindProperty(field);
                p.arraySize = paths.Length;
                for (int i = 0; i < paths.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = KitSprite(paths[i]);
            }
            Many("_gradeFrames", "Components/rb_host_frame_r", "Components/rb_host_frame_sr", "Components/rb_host_frame_ghost");   // SSR = 은빛 틀 × 금색(왕관 뺌, RbFrameFit.SsrTint)
            Many("_rarityTags", "Components/rb_rarity_r", "Components/rb_rarity_sr", "Components/rb_rarity_ssr");
            Many("_affIcons", "Icons/rb_icon_aff_blade_128", "Icons/rb_icon_aff_force_128", "Icons/rb_icon_aff_magic_128");
            WireFrameBodies(so, "Components/rb_host_frame_r", "Components/rb_host_frame_sr", "Components/rb_host_frame_ghost");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Stars(SerializedObject so)
        {
            // 성급 별 — 5성마다 큰 금별 · 그 사이는 작은 은별(PD 2026-10-16 StarRank, 2026-10-26 되돌림)
            so.FindProperty("_rankGold").objectReferenceValue = KitSprite("Icons/rb_icon_rank_gold_64");
            so.FindProperty("_rankSilver").objectReferenceValue = KitSprite("Icons/rb_icon_rank_silver_64");
        }
    }
}
