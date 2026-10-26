// RE:BORN UI 리뉴얼(2026-10-23) — 로비(전투 탭 = 출전 준비 한 화면) 세우기. 시안 Screens/lobby.png(1080 × 2340).
// 좌표는 시안 픽셀 상자 그대로 적는다(재서 옮김 — 스킬 ui-mockup-to-game 1단계). 이름 = 화면 스크립트(RbLobbyUI) 키.
using Game.Module.Lobby;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.ReBorn.RbBuild;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbLobbyBuilder
    {
        public const string PrefabPath = "Assets/BundleResource/Prefabs/UI/Lobby/RbLobbyUI.prefab";

        [MenuItem("Tools/Game/RE:BORN 화면/로비 세우기")]
        public static void Build()
        {
            var root = Root("RbLobbyUI");
            var rt = (RectTransform)root.transform;
            // 쪽(page) — 전투 탭(출전 준비) · 호스트 도감 · 호스트 성장. 위 막대 · 아래 탭은 맨 뒤 형제(모든 쪽 위)
            var battle = Stretch(Node(rt, "PageBattle"));

            // ── 무대 배경 — 가로를 꽉 채우고 위에 붙인다(세로는 그림 비율, 아래로 넘치는 곳은 그림 자체가 어둡게 끝난다)
            var bg = Node(battle, "StageBg");
            bg.anchorMin = new Vector2(0f, 1f); bg.anchorMax = new Vector2(1f, 1f); bg.pivot = new Vector2(0.5f, 1f);
            bg.sizeDelta = new Vector2(0f, 2520f * S); bg.anchoredPosition = new Vector2(0f, MockTop * S);
            var bgImg = Img(bg, "Art/rb_art_bg_stage_ch01");
            bgImg.preserveAspect = false;
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fit.aspectRatio = 1080f / 2520f;

            // 챕터 카드 아래를 시안처럼 짙은 남색으로 가라앉힌다 — 16:9 는 가운데가 줄어 무대 그림의 물 반사가 카드 아래까지 내려온다
            var fade = Node(battle, "StageFade");
            fade.anchorMin = new Vector2(0f, 0f); fade.anchorMax = new Vector2(1f, 0f); fade.pivot = new Vector2(0.5f, 0f);
            // 그림의 위 절반은 옅은 띠라 길게 깐다 — 짙은 곳이 출전 줄 위(시안 1400)부터 시작하게
            // (16:9 플레이 캡처: 1213 로는 카드 뒤 물 반사가 비쳤다 — 더 위에서 시작)
            fade.sizeDelta = new Vector2(0f, (2340f - 300f) * S); fade.anchoredPosition = new Vector2(0f, -110f * S);
            Img(fade, "Components/rb_fade_bottom").preserveAspect = false;

            BuildRails(battle);
            BuildStage(battle);
            BuildChapterCard(battle);
            BuildSortieRow(battle);
            BuildSortieButton(battle);

            RbHostBuilder.BuildCollection(rt);
            RbHostBuilder.BuildGrowth(rt);
            var host = rt.Find("PageHost");
            var growth = rt.Find("PageGrowth");
            RbHostBuilder.WireCollection(host.gameObject.AddComponent<RbHostPage>());
            RbHostBuilder.WireGrowth(growth.gameObject.AddComponent<RbGrowthPage>());
            host.gameObject.SetActive(false);
            growth.gameObject.SetActive(false);
            RbHostLockBuilder.Build(rt);
            RbChapterBuilder.Build(rt);    // 챕터 선택 쪽   // 미보유 호스트 · 새 몸/각성 보여주기 쪽
            RbSummonBuilder.Build(rt);
            RbShopBuilder.Build(rt);
            RbChallengeBuilder.Build(rt);
            RbRailBuilder.BuildPages(rt);

            BuildHeader(rt);
            BuildTabBar(rt);
            RbRailBuilder.BuildOverlay(rt);   // 우편 · 오늘 무료 · 설정 · 언어 — 위 막대 · 아래 탭보다 위
            RbGrowthPopups.Build(rt);
            RbRandomBuilder.Build(rt);        // 랜덤 출전 공개(한 화면 — 위 막대 · 아래 탭 위)
            RbNoticeBuilder.Build(rt);        // 보상 · 모자라요 · 구매 확인 — 맨 앞
            RbExtraScreensBuilder.BuildChestOpen(rt);   // 상자 열기(상점) — 알림 창 위
            RbExtraScreensBuilder.BuildGuide(rt);       // 안내 — 가장 위
            // 상자 열기는 시안처럼 위 막대 밑 · 아래 탭 위 — 아래 탭(TabBar · Tab*)을 위 막대 첫 노드(Chip* · Header*) 자리로
            // 당기고 그 바로 뒤에 상자 열기를 둔다. 뒤에 있던 노드를 앞으로 당기면 그 자리에 정확히 놓인다
            var chestOpen = rt.Find("RbChestOpen");
            var tabs = new System.Collections.Generic.List<Transform>();
            int firstHead = -1;
            for (int i = 0; i < rt.childCount; i++)
            {
                var ch = rt.GetChild(i);
                if (firstHead < 0 && (ch.name.StartsWith("Chip") || ch.name.StartsWith("Header"))) firstHead = i;
                if (ch.name.StartsWith("Tab")) tabs.Add(ch);
            }
            for (int k = 0; k < tabs.Count; k++) tabs[k].SetSiblingIndex(firstHead + k);
            chestOpen.SetSiblingIndex(firstHead + tabs.Count);
            chestOpen.gameObject.SetActive(false);
            // 성급 강화 성공(전체 화면, 시안 star-success-detail)도 같은 층 — 상자 열기 바로 뒤
            if (rt.Find("GrwStarDone") is Transform starDone) starDone.SetSiblingIndex(chestOpen.GetSiblingIndex() + 1);
            rt.Find("RbGuide").gameObject.SetActive(false);         // 성급 강화 · 성급 올랐어요 · 스킬 정보 — 딤이 위 막대 · 아래 탭까지 덮게 맨 뒤

            Wire(root.AddComponent<RbLobbyUI>());
            RbFxBuilder.Attach(root);   // 아웃게임 연출(새 몸 · 각성 · 상자 · 보상 · 강화 · 별 · 버튼 빛)
            Save(root, PrefabPath);
            Register(PrefabPath, "UI/Lobby/RbLobbyUI", "label_lobby");
            Debug.Log($"[RE:BORN 화면] 로비 → {PrefabPath}");
        }

        // 11~20챕터 무대 배경 — 아직 없어 같은 무대(chapters.tsv 의 stage)의 앞 챕터 그림을 쓴다(배너 11~20 발주와 같은 짝)
        private static readonly int[] StageBgOf = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2 };

        private static void Wire(RbLobbyUI ui)
        {
            var so = new SerializedObject(ui);
            void One(string field, string path) => so.FindProperty(field).objectReferenceValue = KitSprite(path);
            void Many(string field, params string[] paths)
            {
                var p = so.FindProperty(field);
                p.arraySize = paths.Length;
                for (int i = 0; i < paths.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = KitSprite(paths[i]);
            }
            Many("_gradeFrames", "Components/rb_host_frame_r", "Components/rb_host_frame_sr", "Components/rb_host_frame_ssr");
            One("_frameRandom", "Components/rb_host_frame_random");
            One("_frameGhost", "Components/rb_host_frame_ghost");
            WireFrameBodies(so, "Components/rb_host_frame_r", "Components/rb_host_frame_sr", "Components/rb_host_frame_ssr",
                            "Components/rb_host_frame_random", "Components/rb_host_frame_ghost");
            One("_badgeBest", "Components/rb_badge_best");
            One("_badgeToday", "Components/rb_badge_today");
            Many("_rarityTags", "Components/rb_rarity_r", "Components/rb_rarity_sr", "Components/rb_rarity_ssr");
            // 속성 순서 = Affinity 값 순서(무기 · 파워 · 마법) — 0(None)은 빼고 1부터
            Many("_affIcons", "Icons/rb_icon_aff_blade_128", "Icons/rb_icon_aff_force_128", "Icons/rb_icon_aff_magic_128");
            One("_iconRandom", "Icons/rb_icon_mode_random_144");
            One("_iconGhost", "Icons/rb_icon_mode_ghost_144");
            One("_iconNext", "Icons/rb_icon_sys_next_96");
            One("_iconLock", "Icons/rb_icon_sys_lock_96");
            var bgs = new string[StageBgOf.Length];
            for (int i = 0; i < bgs.Length; i++) bgs[i] = $"Art/rb_art_bg_stage_ch{StageBgOf[i]:00}";
            Many("_stageBgs", bgs);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 위 막대: 프로필 · 행동력 · 골드 · 젬 · + ─────────────────
        private static void BuildHeader(RectTransform rt)
        {
            var prof = Place(Node(rt, "HeaderProfile"), Edge.Top, 35, 145, 160, 262);
            HitArea(prof);
            // 원 테 안에 유령 얼굴 · 오른쪽 아래 레벨 배지(배지 그림이 짙은 남색이라 글자는 흰색)
            Img(Local(Node(prof, "HeaderProfileRing"), 35, 145, 160, 262, 30, 140, 140, 250), "Components/rb_profile_frame");
            Img(Local(Node(prof, "HeaderProfileFace"), 35, 145, 160, 262, 46, 156, 124, 234), "Icons/rb_icon_mode_ghost_96");
            var lv = Local(Node(prof, "HeaderLevelBadge"), 35, 145, 160, 262, 88, 210, 160, 260);
            Img(lv, "Components/rb_level_badge");
            Text(Stretch(Node(lv, "HeaderLevelText")), null, "12", 34, White, TA.Center, number: true);

            Chip(rt, "ChipStamina", "StaminaText", "Icons/rb_icon_currency_energy_48", 135, 360, "20/20");
            Chip(rt, "ChipGold", "GoldText", "Icons/rb_icon_currency_gold_48", 375, 640, "4,676");
            Chip(rt, "ChipGem", "GemText", "Icons/rb_icon_currency_gem_48", 648, 905, "14,550");

            var plus = Place(Node(rt, "HeaderPlus"), Edge.Top, 938, 143, 1042, 245);
            Btn(plus, "Components/rb_btn_primary");
            Text(Stretch(Node(plus, "HeaderPlusText")), null, "+", 64, Ink, TA.Center, heavy: true);
            // 레벨 배지가 행동력 칩 왼쪽 끝에 걸친다 — 칩보다 뒤에 그리면 숫자가 칩에 가렸다(하네스 C6)
            prof.SetSiblingIndex(plus.GetSiblingIndex());
        }

        /// <summary>재화 칩 — 칸 폭 고정(행동력 230 · 골드 · 젬 260, 묶음 규칙), 아이콘은 칩 안 왼쪽, 숫자는 오른쪽 정렬.</summary>
        private static void Chip(RectTransform rt, string name, string textName, string icon, float x0, float x1, string sample)
        {
            const float y0 = 145, y1 = 238;
            var chip = Place(Node(rt, name), Edge.Top, x0, y0, x1, y1);
            Img(chip, "Components/rb_info_chip");
            Img(Local(Node(chip, name + "Icon"), x0, y0, x1, y1, x0 + 20, 168, x0 + 66, 214), icon);
            Text(Local(Node(chip, textName), x0, y0, x1, y1, x0 + 76, 160, x1 - 18, 224), null, sample, 46, White, TA.Right, number: true);
        }

        // ── 양옆 아이콘(레일) ─────────────────────────────────
        private static void BuildRails(RectTransform rt)
        {
            Rail(rt, "RailMail", "rail_mail", "ui.rb.rail.mail", "우편", 95, 365, true);
            Rail(rt, "RailMission", "rail_mission", "ui.rb.rail.mission", "미션", 95, 565, true);
            Rail(rt, "RailSettings", "rail_settings", "ui.rb.rail.settings", "설정", 95, 765, false);
            Rail(rt, "RailPass", "rail_pass", null, "패스 3", 985, 365, true);
            Rail(rt, "RailEvent", "rail_event", "ui.rb.rail.event", "이벤트", 985, 565, false);
            Rail(rt, "RailDaily", "rail_daily_free", "ui.rb.rail.daily", "오늘 무료", 985, 765, true);
        }

        /// <summary>아이콘 80(시안) + 아래 이름. 누름 칸 = 아이콘 + 이름 전체(터치 144 이상).</summary>
        private static void Rail(RectTransform rt, string name, string icon, string key, string korean, float cx, float iy, bool dot)
        {
            float x0 = cx - 80, x1 = cx + 80, y0 = iy - 8, y1 = iy + 150;
            var r = Place(Node(rt, name), Edge.Top, x0, y0, x1, y1);
            HitArea(r);
            Img(Local(Node(r, name + "Icon"), x0, y0, x1, y1, cx - 42, iy, cx + 42, iy + 84), $"Icons/rb_icon_{icon}_96");
            Text(Local(Node(r, name + "Label"), x0, y0, x1, y1, x0 - 10, iy + 108, x1 + 10, iy + 146), key, korean, 34, White, TA.Center, heavy: true);
            if (!dot) return;
            Img(Local(Node(r, name + "Dot"), x0, y0, x1, y1, cx + 36, iy - 12, cx + 60, iy + 12), "Components/rb_dot_red");
        }

        // ── 무대: 랜덤 구슬 · 유령 · 고른 몸 · 안내 ───────────────────
        private static void BuildStage(RectTransform rt)
        {
            var orb = Place(Node(rt, "StageOrb"), Edge.Top, 395, 410, 720, 825);
            Img(orb, "Art/rb_art_lobby_random_stage");
            // 고른 몸(직접 · 오늘의 몸) — 원본 정지컷 + 받침(묶음 지시 4장 2번). 몸 그림은 화면 스크립트가 꽂는다
            var host = Place(Node(rt, "StageHost"), Edge.Top, 380, 430, 740, 830);
            Img(Local(Node(host, "StagePedestal"), 380, 430, 740, 830, 400, 740, 720, 830), "Art/rb_art_host_pedestal");
            Img(Local(Node(host, "StageHostArt"), 380, 430, 740, 830, 420, 440, 700, 790), "Art/rb_art_mascot_ghost_happy");
            host.gameObject.SetActive(false);
            Img(Place(Node(rt, "StageGhost"), Edge.Top, 195, 595, 350, 715), "Art/rb_art_mascot_ghost_happy");

            var prompt = Place(Node(rt, "StagePrompt"), Edge.Bottom, 380, 930, 700, 990);
            Img(prompt, "Components/rb_info_chip");
            Text(Stretch(Node(prompt, "StagePromptText")), "ui.rb.lobby.prompt", "어떤 몸과 만날까요?", 34, Dim, TA.Center);
        }

        // ── 챕터 카드(히어로 판) — 16칸 방 진행 칸은 없앴고(PD 2026-10-23) 세 구간 게이지로 다시 넣었다(PD 2026-10-26) ─────────
        public const int GaugeSections = 3;
        private const float GaugeLeft = 51f, GaugeStep = 235f, GaugeBarW = 172f, GaugeTop = 1124f, GaugeH = 21f;

        private static void BuildChapterCard(RectTransform rt)
        {
            const float x0 = 30, y0 = 1030, x1 = 1050, y1 = 1250;
            var card = Place(Node(rt, "ChapterCard"), Edge.Bottom, x0, y0, x1, y1);
            Img(card, "Components/rb_panel_hero");
            HitArea(Local(Node(card, "ChapterOpen"), x0, y0, x1, y1, 40, 1040, 755, 1240));   // 챕터 카드 왼쪽 → 챕터 선택(앞뒤 버튼은 그 위)
            Text(Local(Node(card, "ChapterNoText"), x0, y0, x1, y1, 50, 1060, 190, 1115), null, "CH 1", 56, White, TA.Left, number: true);
            Text(Local(Node(card, "ChapterNameText"), x0, y0, x1, y1, 200, 1060, 750, 1115), null, "쓰레기 집적장", 50, White, TA.Left, heavy: true);
            // 방 게이지 — 세 구간(1~10 · 11~20 · 21~30), 끝마다 중간보스 · 보스 아이콘(PD 2026-10-26 A안, 시안 stage_gauge_options.png)
            for (int s = 0; s < GaugeSections; s++)
            {
                float bx0 = GaugeLeft + s * GaugeStep, bx1 = bx0 + GaugeBarW;
                var track = Local(Node(card, $"ChapterSeg{s}"), x0, y0, x1, y1, bx0, GaugeTop, bx1, GaugeTop + GaugeH);
                Img(track, "Components/rb_bar_track");
                // 채움은 왼쪽에 붙여 폭만 바꾼다(화면 스크립트가 깬 방 수만큼)
                var fill = Node(track, $"ChapterSeg{s}Fill");
                fill.anchorMin = new Vector2(0f, 0f); fill.anchorMax = new Vector2(0f, 1f); fill.pivot = new Vector2(0f, 0.5f);
                fill.sizeDelta = new Vector2(GaugeBarW * S, 0f); fill.anchoredPosition = Vector2.zero;
                Img(fill, "Components/rb_bar_fill_cyan");
                float ic = GaugeTop + GaugeH * 0.5f;
                Img(Local(Node(card, $"ChapterSeg{s}Icon"), x0, y0, x1, y1, bx1 + 4, ic - 28, bx1 + 60, ic + 28),
                    s == GaugeSections - 1 ? "Icons/rb_icon_room_boss_48" : "Icons/rb_icon_room_midboss_48");
            }
            // 안내 줄 「최고 7/30방 · 권장 1,200 · 」 + 유리한 속성 아이콘 + 「유리 ▲」 — 아이콘은 글자 폭을 재서 바로 뒤에 붙인다.
            // 속성은 글자 대신 아이콘(PD 2026-10-26 「아이콘으로 해야 속성 공부가 되고 적응이 된다」). 폭을 재야 해서 자동 축소는 끈다
            var info = Text(Local(Node(card, "ChapterInfoText"), x0, y0, x1, y1, 50, 1160, 750, 1212), null, "최고 7/30방 · 권장 1,200 · ", 36, Dim, TA.Left);
            info.enableAutoSizing = false; info.fontSize = 36 * S;
            Img(Local(Node(card, "ChapterAdvIcon"), x0, y0, x1, y1, 480, 1164, 524, 1208), "Icons/rb_icon_aff_magic_128");
            var adv = Text(Local(Node(card, "ChapterAdvText"), x0, y0, x1, y1, 530, 1160, 750, 1212), null, "유리 ▲", 36, White, TA.Left, heavy: true);
            adv.enableAutoSizing = false; adv.fontSize = 36 * S;
            var prev = Local(Node(card, "ChapterPrev"), x0, y0, x1, y1, 765, 1085, 890, 1210);
            Btn(prev, "Components/rb_btn_icon_round");
            Img(Local(Node(prev, "ChapterPrevIcon"), 765, 1085, 890, 1210, 795, 1112, 860, 1177), "Icons/rb_icon_sys_arrow_left_96");
            var next = Local(Node(card, "ChapterNext"), x0, y0, x1, y1, 908, 1085, 1035, 1210);
            Btn(next, "Components/rb_btn_icon_round");
            Img(Local(Node(next, "ChapterNextIcon"), 908, 1085, 1035, 1210, 939, 1112, 1004, 1177), "Icons/rb_icon_sys_next_96");
        }

        // ── 출전 줄: 「출전 호스트」 · 「전체 N ›」 · 카드 줄 · 보너스 칩 · 안내 ─────────
        public const float CardW = 207f, CardH = 276f, CardStep = 220f;   // 시안 1080 기준
        private const float CardDrop = 10f;   // 카드를 보기 칸 가운데보다 이만큼 아래로(윗변 위 BEST · 오늘 판 자리)

        /// <summary>빌더에서 본보기 틀을 몸통 기준으로 맞춘다(화면 스크립트 RbFrameFit 과 같은 계산).</summary>
        public static void RbFrameFitEditor(RectTransform frame, string path)
        {
            var card = (RectTransform)frame.parent;
            var s = KitSprite(path);
            RbFrameFit.Apply(frame, card.sizeDelta, s, new[] { s }, new[] { FrameBody(path) });
        }

        private static void BuildSortieRow(RectTransform rt)
        {
            Text(Place(Node(rt, "SortieTitle"), Edge.Bottom, 30, 1280, 400, 1330), "ui.rb.lobby.sortie_title", "출전 호스트", 44, White, TA.Left, heavy: true);
            var all = Place(Node(rt, "SortieAll"), Edge.Bottom, 775, 1262, 1050, 1395);
            Btn(all, "Components/rb_btn_chip");
            Text(Local(Node(all, "SortieAllText"), 775, 1262, 1050, 1395, 790, 1290, 1035, 1370), null, "전체 23 ›", 44, White, TA.Center, heavy: true);

            // 카드 줄 — 가로로 넘겨 보는 줄(오른쪽이 잘려 보여 넘김을 알린다). 보기 칸은 RectMask2D 만(규칙 05)
            // 위로 조금 넓혔다 — BEST · 오늘 판이 카드 윗변 위로 올라와 잘리지 않게(카드도 10 내렸다)
            var view = Place(Node(rt, "SortieViewport"), Edge.Bottom, 0, 1396, 1080, 1705);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0);   // 끌기를 받을 칸(보이지 않음)
            var content = Node(view, "SortieContent");
            content.anchorMin = new Vector2(0f, 0f); content.anchorMax = new Vector2(0f, 1f); content.pivot = new Vector2(0f, 0.5f);
            content.sizeDelta = new Vector2(1080f * S, 0f); content.anchoredPosition = Vector2.zero;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = view; scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic; scroll.inertia = true;

            // 카드 본보기 — 화면 스크립트가 몸 수만큼 복제한다(첫 카드 자리 = 시안 x 30~237)
            var card = Node(content, "SortieCardTemplate");
            card.anchorMin = card.anchorMax = new Vector2(0f, 0.5f); card.pivot = new Vector2(0f, 0.5f);
            card.sizeDelta = new Vector2(CardW * S, CardH * S); card.anchoredPosition = new Vector2(30f * S, -CardDrop * S);
            HitArea(card);
            float cx0 = 30, cy0 = 1412, cx1 = 237, cy1 = 1688;
            // 틀은 몸통(테 선)이 카드 칸에 딱 맞게 놓는다 — 화면 스크립트가 등급마다 다시 맞춘다(RbFrameFit)
            var frame = Node(card, "CardFrame");
            Img(frame, "Components/rb_host_frame_r").preserveAspect = false;
            RbFrameFitEditor(frame, "Components/rb_host_frame_r");
            var sel = Img(Local(Node(card, "CardSel"), cx0, cy0, cx1, cy1, 24, 1405, 243, 1695), "Components/rb_host_select_ring");
            sel.gameObject.SetActive(false);
            // 카드 안 위 왼쪽 = 내 속성(네모 칸 안) · 위 오른쪽 = 등급 태그 · 틀 윗변 위 = BEST · 오늘 판 (PD 2026-10-26)
            var affBox = Local(Node(card, "CardAffBox"), cx0, cy0, cx1, cy1, 40, 1422, 86, 1468);
            var affImg = Img(affBox, "Components/rb_level_badge");
            affImg.pixelsPerUnitMultiplier = 2f;   // 배지 테(25 · 13 px)가 작은 네모에 맞게 — 테 두께가 위 막대 레벨 배지와 비슷해진다
            Img(Local(Node(affBox, "CardAff"), 40, 1422, 86, 1468, 44, 1426, 82, 1464), "Icons/rb_icon_aff_force_128");
            Img(Local(Node(card, "CardRarity"), cx0, cy0, cx1, cy1, 168, 1424, 228, 1465), "Components/rb_rarity_sr");
            Img(Local(Node(card, "CardIcon"), cx0, cy0, cx1, cy1, 78, 1476, 190, 1566), "Icons/rb_icon_mode_random_144");
            // BEST · 오늘 판 — 틀 윗변 가운데에 얹는다(그림 비율 그대로 80:41 · 9분할로 늘리지 않는다)
            var badge = Local(Node(card, "CardBadge"), cx0, cy0, cx1, cy1, 83, 1388, 183, 1439);
            var badgeImg = Img(badge, "Components/rb_badge_today");
            badgeImg.type = Image.Type.Simple;
            badgeImg.preserveAspect = true;
            Text(Local(Node(badge, "CardBadgeText"), 83, 1388, 183, 1439, 91, 1392, 175, 1435), null, "오늘", 32, White, TA.Center, heavy: true);
            // 긴 이름은 RbCardName 이 괄호 앞에서 두 줄로 접는다(「코만도 / (수류탄)」) — 두 줄이 들어가게 칸을 위아래로 조금 늘렸다.
            // 시안 규칙 「글자 32 미만 0」(PD 2026-10-24 「시안을 따라줘」) — 하한은 그대로
            // 폭은 틀 안쪽 끝까지(34~233) — 「サラマンダー」가 최소 크기에서 몇 px 넘쳐 글자 중간에서 접혔다(2026-10-26 캡처)
            var cardName = Text(Local(Node(card, "CardName"), cx0, cy0, cx1, cy1, 34, 1554, 233, 1626), null, "? 랜덤", 36, White, TA.Center, heavy: true);
            cardName.enableWordWrapping = true;
            cardName.lineSpacing = -28f;
            Img(Local(Node(card, "CardPriceIcon"), cx0, cy0, cx1, cy1, 60, 1628, 104, 1668), "Icons/rb_icon_currency_gold_48");
            Text(Local(Node(card, "CardPrice"), cx0, cy0, cx1, cy1, 40, 1622, 227, 1674), null, "무료", 40, Green, TA.Center, heavy: true);

            // 보너스 칩 3 · 안내 한 줄 — 출전 방식마다 글이 바뀐다
            float[][] chips = { new float[] { 222, 463 }, new float[] { 480, 632 }, new float[] { 650, 858 } };
            for (int i = 0; i < chips.Length; i++)
            {
                var c = Place(Node(rt, $"BonusChip{i}"), Edge.Bottom, chips[i][0], 1718, chips[i][1], 1778);
                Img(c, "Components/rb_info_chip");
                Text(Stretch(Node(c, $"BonusChip{i}Text")), null, "히든 등장 가능", 36, Dim, TA.Center);
            }
            Text(Place(Node(rt, "BonusNote"), Edge.Bottom, 120, 1800, 960, 1846), null, "클리어 보너스 · 히든은 이번 판 체험, 조각 미지급", 36, Dim, TA.Center);
        }

        // ── 출전 버튼(주요 · 화면당 하나) ─────────────────────────
        private static void BuildSortieButton(RectTransform rt)
        {
            const float x0 = 28, y0 = 1865, x1 = 1050, y1 = 2032;
            var b = Place(Node(rt, "SortieButton"), Edge.Bottom, x0, y0, x1, y1);
            Btn(b, "Components/rb_btn_primary");
            // 「출전 | ⚡5」 — 비용 줄은 화면 스크립트가 글자 폭을 재서 가운데에 세운다
            Text(Local(Node(b, "SortieLabel"), x0, y0, x1, y1, 300, 1895, 530, 1985), null, "출전", 56, Ink, TA.Right, heavy: true);
            Img(Local(Node(b, "SortieStaminaIcon"), x0, y0, x1, y1, 562, 1918, 610, 1966), "Icons/rb_icon_currency_energy_48");
            Text(Local(Node(b, "SortieStaminaText"), x0, y0, x1, y1, 620, 1895, 700, 1985), null, "5", 60, Ink, TA.Left, number: true);
            Img(Local(Node(b, "SortieGoldIcon"), x0, y0, x1, y1, 712, 1918, 760, 1966), "Icons/rb_icon_currency_gold_48").gameObject.SetActive(false);
            var gold = Text(Local(Node(b, "SortieGoldText"), x0, y0, x1, y1, 768, 1895, 920, 1985), null, "200", 60, Ink, TA.Left, number: true);
            gold.gameObject.SetActive(false);
        }

        // ── 하단 탭 5 — 상점 · 호스트 · 전투 · 소환 · 도전(순서 · 이름 바꾸지 않는다) ───────
        public static readonly string[] Tabs = { "Shop", "Host", "Battle", "Summon", "Challenge" };
        private static readonly string[] TabIcons = { "tab_shop", "tab_host", "tab_battle", "tab_summon", "tab_challenge" };
        private static readonly string[] TabKorean = { "상점", "호스트", "전투", "소환", "도전" };

        private static void BuildTabBar(RectTransform rt)
        {
            var bar = Node(rt, "TabBar");
            bar.anchorMin = new Vector2(0f, 0f); bar.anchorMax = new Vector2(1f, 0f); bar.pivot = new Vector2(0.5f, 0f);
            bar.sizeDelta = new Vector2(0f, (MockBottom - 2040f) * S); bar.anchoredPosition = Vector2.zero;
            Img(bar, "Components/rb_tab_bar_bg");
            for (int i = 0; i < Tabs.Length; i++)
            {
                float cx = 108f + i * 215.5f;
                var tab = Place(Node(rt, $"Tab{Tabs[i]}"), Edge.Bottom, cx - 105, 2045, cx + 105, 2228);
                HitArea(tab);
                // 전투 탭 둘레 원(rb_tab_center_ring)은 뺐다 — PD 2026-10-26 「왜 있는 거야, 그냥 제거」
                Img(Local(Node(tab, $"Tab{Tabs[i]}Icon"), cx - 105, 2045, cx + 105, 2228, cx - 52, 2062, cx + 52, 2166), $"Icons/rb_icon_{TabIcons[i]}_96");
                Text(Local(Node(tab, $"Tab{Tabs[i]}Label"), cx - 105, 2045, cx + 105, 2228, cx - 100, 2180, cx + 100, 2224),
                     $"ui.rb.tab.{Tabs[i].ToLowerInvariant()}", TabKorean[i], 36, Dim, TA.Center, heavy: true);
            }
        }
    }
}
