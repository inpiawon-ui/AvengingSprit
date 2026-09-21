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
    /// 육성 화면 + 공통 하단 바 — 시안(Reference/Mockups/growth/*.png, 720×1280)을 **그대로** 세운다 (2026-09-21).
    ///
    /// 그림은 시안 픽셀이다. `Projects/AVSR/Tools/growth_build.py` 가 바탕 셋(유령 · 호스트 · 스킬)과
    /// 부품(탭 · 줄 · 버튼 · 별 · 막대 · 카드 틀 · 성장 경로 · 하단 바 칸)과 글자 자리(growth_spec.json)를
    /// `Assets/BaseResource/Growth/` 에 둔다. 이 빌더는 그것을 `LobbyMainUI` 프리팹에 꽂는다.
    ///
    /// ⚠ **로비 v4 빌더 다음에** 돌린다. 로비 빌더는 옛 하단 바를 지우고 이 빌더가 공통 하단 바를 세운다.
    /// 시안 = 캔버스(720×1280)라 배율이 없다. 좌표는 전부 「왼쪽 위 원점, 아래로 +」 시안 px 다.
    /// </summary>
    public static class GrowthBuilder
    {
        private const string Prefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Dir = "Assets/BaseResource/Growth";
        private const string UnitDir = "Assets/BaseResource/Unit";
        private const string SkillIconDir = "Assets/BaseResource/HostSelectPanel";
        private const string BoldFont = "Assets/BaseResource/Fonts/NotoSansKR-Bold SDF.asset";
        private const float Pad = 16f;
        private const float PageW = 720f;

        [Serializable] private class GText { public string name; public int[] box; public int[] area; public string align; public string color; public int size; public int lines; }
        [Serializable] private class GRect { public string name; public float[] box; }
        [Serializable] private class Spec { public int navY; public int skillListDy; public float[] nodeX; public float markX; public float pathY; public GText[] texts; public GRect[] rects; }

        [Serializable] private class CalibItem { public string name; public float dx, dy, scale = 1f, dilate, aspect = 1f; }
        [Serializable] private class Calib { public CalibItem[] items; }

        private static Spec s_spec;
        private static Calib s_calib;
        private static TMP_FontAsset s_bold;

        /// <summary>글자 보정(growth_calib.py 가 스샷과 시안 잉크를 재서 채운다). 스펙 이름 단위다.</summary>
        private static CalibItem CalibOf(string specName)
            => Array.Find(s_calib?.items ?? Array.Empty<CalibItem>(), x => x.name == specName) ?? new CalibItem();

        [MenuItem("Tools/Game/육성 화면 · 하단 바 — 시안 그대로 세우기")]
        public static void Run()
        {
            s_spec = JsonUtility.FromJson<Spec>(File.ReadAllText(Path.Combine(Dir, "growth_spec.json")));
            s_bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFont);
            if (s_spec == null || s_bold == null) { Debug.LogError("[육성] 스펙 · 굵은 폰트가 없다"); return; }
            var calibPath = Path.Combine(Dir, "growth_calib.json");
            s_calib = File.Exists(calibPath) ? JsonUtility.FromJson<Calib>(File.ReadAllText(calibPath)) : null;
            ImportSprites();

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                foreach (var n in new[] { "GrowthPanel", "BottomNav" })
                {
                    var old = root.transform.Find(n);
                    if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                }
                var growth = BuildGrowth(root.transform);
                var nav = BuildNav(root.transform);
                // 로비 판 셋(틈 · 위 · 가운데) 바로 뒤 — 그 뒤의 창(챕터 · 호스트 선택 · 보상)은 하단 바를 덮는다
                growth.SetSiblingIndex(3);
                nav.SetSiblingIndex(4);
                var so = new SerializedObject(root.GetComponent<LobbyMainUI>());
                so.FindProperty("_growthPanel").objectReferenceValue = growth.GetComponent<GrowthPanel>();
                so.ApplyModifiedPropertiesWithoutUndo();
                growth.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("[육성] 세움 — 시안 그대로");
        }

        // ── 육성 화면 ───────────────────────────────────────────

        private static RectTransform BuildGrowth(Transform root)
        {
            var panel = Node(root, "GrowthPanel");
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            var gp = panel.gameObject.AddComponent<GrowthPanel>();

            // 페이지 — 화면 위에 붙는 720 × navY
            var page = Node(panel, "Page");
            page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 1f);
            page.anchoredPosition = Vector2.zero;
            page.sizeDelta = new Vector2(PageW, s_spec.navY);
            page.gameObject.AddComponent<ScreenFitLock>();

            // 태블릿 양옆 · 긴 화면 아래 — 가장자리 줄을 늘인다
            var sideL = Place(page, "GrowthSideL", -400, 0, 0, s_spec.navY); Img(sideL, Spr("page_side_l"));
            var sideR = Place(page, "GrowthSideR", PageW, 0, PageW + 400, s_spec.navY); Img(sideR, Spr("page_side_r"));
            var filler = Place(page, "GrowthFiller", -400, s_spec.navY - 2, PageW + 400, s_spec.navY + 700);
            Img(filler, Spr("page_filler"));

            foreach (var (node, file) in new[] { ("BgGhost", "bg_ghost"), ("BgHost", "bg_host"), ("BgSkill", "bg_skill") })
                Img(Place(page, node, 0, 0, PageW, s_spec.navY), Spr(file));

            // 머리 — 골드 · 버튼(그림은 바탕에 있다)
            HitButton(page, "GrowthGoldPlusButton", 505, 14, 548, 58);
            HitButton(page, "GrowthMailButton", 562, 8, 630, 64);
            HitButton(page, "GrowthSettingsButton", 644, 8, 712, 64);
            Text(page, "GrowthGoldText", T("GoldText"), "125,680", null);

            // 탭 둘
            BuildTab(page, "TabGhostButton", R("@TAB_GHOST"), "tab_ghost_on", "tab_ghost_off", "TabGhostOn", "TabGhostOff",
                     "TabGhostText", R("@TAB_GHOST_TEXT"), "ui.growth.tab.ghost", T("TabOnText"), T("TabOffText"));
            BuildTab(page, "TabHostButton", R("@TAB_HOST"), "tab_host_on", "tab_host_off", "TabHostOn", "TabHostOff",
                     "TabHostText", R("@TAB_HOST_TEXT"), "ui.growth.tab.host", T("TabOnText"), T("TabOffText"));

            BuildCard(page);
            BuildHostTabs(page);
            BuildStats(page);
            BuildSkills(page);
            BuildHostList(page);
            BuildPath(page);
            BindGrowth(gp);
            return panel;
        }

        private static void BuildTab(RectTransform page, string name, float[] r, string onFile, string offFile, string onNode,
                                     string offNode, string textNode, float[] textArea, string key, GText on, GText off)
        {
            var b = HitButton(page, name, r[0], r[1], r[2], r[3]);
            Img(Place(b, onNode, 0, 0, r[2] - r[0], r[3] - r[1]), Spr(onFile));
            Img(Place(b, offNode, 0, 0, r[2] - r[0], r[3] - r[1]), Spr(offFile));
            var t = on;
            // 글자는 탭 오른쪽 끝 바로 앞까지 쓸 수 있다 — 일본어가 한국어 시안보다 길다
            var box = new[] { (int)textArea[0], t.box[1], (int)(r[2] - 12), t.box[3] };
            var tmp = TextAtBox(b, textNode, box, t, Localize(key), r[0], r[1]);
            Key(tmp.gameObject, key);
            tmp.gameObject.AddComponent<TabTextColors>().Set(Col(on.color), Col(off.color));
        }

        private static void BuildCard(RectTransform page)
        {
            Text(page, "CardNameEnText", T("CardNameEn"), "GHOST", null);
            Text(page, "CardNameText", T("CardName"), Localize("ui.growth.ghost.name"), null);
            var desc = Text(page, "CardDescText", T("CardDesc"), Localize("ui.growth.ghost.desc"), null);
            desc.textWrappingMode = TextWrappingModes.NoWrap;
            desc.lineSpacing = 2f;   // 시안 줄 간격(세 줄 64px) — 좁히면 글자가 작게 재져 보정이 키워 버린다
            desc.verticalAlignment = VerticalAlignmentOptions.Top;
            var a = Text(page, "CardLvLabel", T("CardLvLabel"), "Lv.", null);
            var b = Text(page, "CardLvNum", T("CardLvNum"), "28", null);
            var c = Text(page, "CardLvMax", T("CardLvMax"), "/ 50", null);
            Run(page, "CardLvRun", new[] { a, b, c }, new[] { "CardLvLabel", "CardLvNum", "CardLvMax" });
            for (int i = 0; i < 5; i++)
            {
                var r = R($"@STAR{i}");
                Img(Place(page, $"CardStar{i}", r[0], r[1], r[2], r[3]), Spr(i < 3 ? "star_big_on" : "star_big_off")).preserveAspect = true;
            }

            var exp = Node(page, "ExpGroup"); Stretch(exp);
            Text(exp, "ExpLabelText", T("ExpLabel"), "EXP", null);
            Bar(exp, "ExpBar", "bar_g");
            Text(exp, "ExpValueText", T("ExpValue"), "1,820 / 2,400", null);

            var shard = Node(page, "ShardGroup"); Stretch(shard);
            var si = R("shard_icon");
            Img(Place(shard, "ShardIcon", si[0], si[1], si[2], si[3]), Spr("shard_icon"));
            Bar(shard, "ShardBar", "bar_h");
            Text(shard, "ShardValueText", T("ShardValue"), "27 / 50", null);
            var bf = R("bar_h_frame");
            HitButton(shard, "ShardBarButton", si[0], bf[1] - 6, bf[2] + 90, bf[3] + 6);

            var pa = R("@HOST_PORTRAIT_AREA");
            // 유닛 그림(96px)은 위 · 옆에 투명 여백이 있다 — 칸 그대로면 시안보다 작다. 발을 카드 바닥에 두고 위로 키운다
            var portrait = Img(Place(page, "HostPortrait", pa[0], pa[3] - 262, pa[2], pa[3]), null);
            portrait.preserveAspect = true;
        }

        /// <summary>막대 — 틀 + 채움(가로 채우기). 채움 그림은 시안 채움을 가로로 늘인 것이다.</summary>
        private static void Bar(RectTransform parent, string name, string file)
        {
            var fr = R(file + "_frame");
            Img(Place(parent, name + "Frame", fr[0], fr[1], fr[2], fr[3]), Spr(file + "_frame"));
            var fl = R(file + "_fill");
            // 채움 칸은 틀 속 전체 폭 — 시안 채움 왼쪽에서 틀 속 오른쪽 끝(틀 오른쪽 − 채움 왼쪽 여백)까지
            float inset = fl[0] - fr[0];
            var fill = Img(Place(parent, name + "Fill", fl[0], fl[1], fr[2] - inset, fl[3]), Spr(file + "_fill"));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0.7f;
        }

        private static void BuildHostTabs(RectTransform page)
        {
            var g = Node(page, "HostTabs"); Stretch(g);
            BuildTab(g, "StatTabButton", R("@STAT_TAB"), "stattab_on", "stattab_off", "StatTabOn", "StatTabOff",
                     "StatTabText", R("@STAT_TAB"), "ui.growth.tab.stats", T("StatTabOn"), T("StatTabOff"));
            BuildTab(g, "SkillTabButton", R("@SKILL_TAB"), "skilltab_on", "skilltab_off", "SkillTabOn", "SkillTabOff",
                     "SkillTabText", R("@SKILL_TAB"), "ui.growth.tab.skills", T("StatTabOn"), T("StatTabOff"));
            // 탭 글자는 가운데 — 빌더 기본은 왼쪽이라 가운데로 바꾼다
            foreach (var n in new[] { "StatTabText", "SkillTabText" })
            {
                var t = Find(g, n).GetComponent<TextMeshProUGUI>();
                t.horizontalAlignment = HorizontalAlignmentOptions.Center;
            }
        }

        private static void BuildStats(RectTransform page)
        {
            var sec = Node(page, "StatSection"); Stretch(sec);
            var h = Text(sec, "StatHeaderText", T("StatHeader"), Localize("ui.growth.stat_header"), "ui.growth.stat_header");
            var help = R("@HELP_ICON");
            var hb = HitButton(sec, "StatHelpButton", help[0], help[1], help[2], help[3]);
            Img(Place(hb, "Icon", 0, 0, help[2] - help[0], help[3] - help[1]), Spr("help"));

            // 스크롤 창 — 첫 줄 위 4px 부터 판 아랫변까지
            var row0 = R("@ROW0");
            var panel = R("@STAT_PANEL_HOST");
            var vp = Place(sec, "StatViewport", panel[0] + 4, row0[1] - 4, panel[2] - 4, panel[3]);
            vp.gameObject.AddComponent<RectMask2D>();
            var hit = vp.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0);
            var content = Node(vp, "StatContent");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0, 320);
            var scroll = vp.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic; scroll.viewport = vp;

            float ox = panel[0] + 4, oy = row0[1] - 4;   // 창 왼쪽 위
            var row = Place(content, "StatRow", row0[0] - ox, row0[1] - oy, row0[2] - ox, row0[3] - oy);
            Img(row, Spr("stat_row"));
            float rx = row0[0], ry = row0[1];
            var ic = R("@ROW_ICON");
            Img(Place(row, "RowIcon", ic[0] - rx, ic[1] - ry, ic[2] - rx, ic[3] - ry), Spr("icon_atk")).preserveAspect = true;
            TextIn(row, "RowNameText", T("RowName"), "공격력", rx, ry);
            var a = TextIn(row, "RowLvLabel", T("RowLvLabel"), "Lv.", rx, ry);
            var b = TextIn(row, "RowLvNum", T("RowLvNum"), "15", rx, ry);
            var c = TextIn(row, "RowLvMax", T("RowLvMax"), "/ 50", rx, ry);
            Run(row, "RowLvRun", new[] { a, b, c }, new[] { "RowLvLabel", "RowLvNum", "RowLvMax" });
            TextIn(row, "RowPctText", T("RowPct"), "+30.0%", rx, ry);
            var br = R("gold_button");
            var btn = HitButton(row, "RowButton", br[0] - rx, br[1] - ry, br[2] - rx, br[3] - ry);
            Img(Place(btn, "Art", 0, 0, br[2] - br[0], br[3] - br[1]), Spr("gold_button"));
            TextIn(btn, "RowCostText", T("RowCost"), "2,800", br[0], br[1]);
        }

        private static void BuildSkills(RectTransform page)
        {
            var sec = Node(page, "SkillSection"); Stretch(sec);
            var ia = R("@SKILL_ICON_A");
            Img(Place(sec, "SkillActiveIcon", ia[0], ia[1], ia[2], ia[3]), null).preserveAspect = true;
            Text(sec, "SkillActiveName", T("SkillName"), "跳躍強襲", null);
            var d1 = Text(sec, "SkillActiveDesc", T("SkillDesc"), "最も遠い敵に飛び込み、\n2秒間無敵になる。", null);
            Wrap(d1, 470f);
            var ip = R("@SKILL_ICON_P");
            Img(Place(sec, "SkillPassiveIcon", ip[0], ip[1], ip[2], ip[3]), Spr("passive_icon"));
            var np = R("@SKILL_NAME_P");
            var nameSpec = T("SkillName");
            TextAtBox(sec, "SkillPassiveName", new[] { (int)np[0] + 4, (int)np[1] + 4, (int)np[2], (int)np[3] - 2 }, nameSpec, "衝撃反動");
            var dp = R("@SKILL_DESC_P");
            var d2 = TextAtBox(sec, "SkillPassiveDesc", new[] { (int)dp[0] + 4, (int)dp[1] + 4, (int)dp[2], (int)dp[3] }, T("SkillDesc"),
                               "10%の確率で当たった敵を押し返す。");
            Wrap(d2, 470f);
        }

        private static void BuildHostList(RectTransform page)
        {
            var sec = Node(page, "HostList"); Stretch(sec);
            Text(sec, "ListHeaderText", T("ListHeader"), Localize("ui.growth.host_list"), "ui.growth.host_list");
            Text(sec, "SortText", T("SortText"), Localize("ui.growth.sort.default"), "ui.growth.sort.default");
            HitButton(sec, "SortButton", 547, 793, 700, 830);

            var grid = R("@GRID");
            var vp = Place(sec, "HostViewport", grid[0], grid[1], grid[2], s_spec.navY);
            vp.gameObject.AddComponent<RectMask2D>();
            var hit = vp.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0);
            var content = Node(vp, "HostContent");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0, 1000);
            var scroll = vp.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.horizontal = false; scroll.vertical = true; scroll.viewport = vp;

            var c0 = R("@CARD0");
            float ox = grid[0], oy = grid[1];
            var card = Place(content, "HostCard", c0[0] - ox, c0[1] - oy, c0[2] - ox, c0[3] - oy);
            var hitImg = card.gameObject.AddComponent<Image>(); hitImg.color = new Color(1, 1, 1, 0);
            card.gameObject.AddComponent<Button>().targetGraphic = hitImg;
            float cw = c0[2] - c0[0], ch = c0[3] - c0[1];
            var sel = R("hostcard_sel"); var nor = R("hostcard");
            // 틀 부품은 칸보다 2px 넓게 뗐다 — 칸 기준으로 맞춘다
            Img(Place(card, "CardFrame", nor[0] - (c0[0] + 174.3f), nor[1] - c0[1], nor[2] - (c0[0] + 174.3f), nor[3] - c0[1]), Spr("hostcard"));
            Img(Place(card, "CardFrameSel", sel[0] - c0[0], sel[1] - c0[1], sel[2] - c0[0], sel[3] - c0[1]), Spr("hostcard_sel"));
            // 썸네일도 같은 이유로 칸보다 크게(투명 여백만큼) — 발은 이름 줄 바로 위
            Img(Place(card, "CardThumb", 20, -20, cw - 20, 112), null).preserveAspect = true;
            var name = T("CardListName");
            TextAtBox(card, "CardName", new[] { (int)(c0[0] + 8), name.box[1], (int)(c0[2] - 8), name.box[3] }, name, "Commando",
                      c0[0], c0[1], center: true);
            var st = R("@STAR_SMALL_ON");
            float sw = st[2] - st[0];
            for (int i = 0; i < 5; i++)
            {
                float x = st[0] - c0[0] + 23.4f * i;
                Img(Place(card, $"CardStar{i}", x, st[1] - c0[1], x + sw, st[3] - c0[1]), Spr(i < 3 ? "slot_star_on" : "slot_star_off"))
                    .preserveAspect = true;
            }
        }

        private static void BuildPath(RectTransform page)
        {
            var sec = Node(page, "GhostPath"); Stretch(sec);
            Text(sec, "PathTitleText", T("PathTitle"), Localize("ui.growth.path_header"), "ui.growth.path_header");
            var ph = R("@PATH_HELP");
            var hb = HitButton(sec, "PathHelpButton", ph[0], ph[1], ph[2], ph[3]);
            Img(Place(hb, "Icon", 0, 0, ph[2] - ph[0], ph[3] - ph[1]), Spr("help"));

            float y = s_spec.pathY;
            foreach (var (node, file, part) in new[] { ("PathLineDone", "line_cyan", "line_cyan"),
                                                       ("PathLineNow", "line_gold", "line_gold"),
                                                       ("PathLineLock", "line_grey", "line_grey") })
            {
                var lr = R(part);
                var rt = Place(sec, node, 100, lr[1], 200, lr[3]);
                rt.pivot = new Vector2(0.5f, 1f);
                Img(rt, Spr(file));
            }
            var done = R("node_done");
            float nw = done[2] - done[0], nh = done[3] - done[1];
            int[] labelIndex = { 0, 1, 3, 4, 5 };
            var labelSpecs = new[] { T("PathLabelDone"), T("PathLabelDone"), T("PathLabelNext"), T("PathLabelLock"), T("PathLabelLock") };
            for (int i = 0; i < 5; i++)
            {
                float x = s_spec.nodeX[i];
                var n = Place(sec, $"PathNode{i}", x - nw / 2, y - nh / 2, x + nw / 2, y + nh / 2);
                n.pivot = new Vector2(0.5f, 0.5f);
                n.anchoredPosition = new Vector2(x, -y);
                var img = Img(n, Spr(i < 2 ? "node_done" : i == 2 ? "node_next" : "node_lock"));
                img.raycastTarget = true;
                n.gameObject.AddComponent<Button>().targetGraphic = img;
                var lr = R($"@LABEL{labelIndex[i]}");
                var l = TextAtBox(sec, $"PathLabel{i}", new[] { (int)lr[0] - 10, labelSpecs[i].box[1], (int)lr[2] + 10, labelSpecs[i].box[3] },
                                  labelSpecs[i], $"Lv. {(i + 1) * 10}", 0, 0, center: true);
                CenterPivot(l.rectTransform, x);
            }
            var mk = R("marker");
            float mw = mk[2] - mk[0], mh = mk[3] - mk[1];
            var marker = Place(sec, "PathMarker", 0, 0, mw, mh);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.anchoredPosition = new Vector2(s_spec.markX, -((mk[1] + mk[3]) * 0.5f));
            Img(marker, Spr("marker"));
            var ml = R("@LABEL2");
            var now = T("PathLabelNow");
            var mlt = TextAtBox(sec, "PathMarkerLabel", new[] { (int)ml[0] - 10, now.box[1], (int)ml[2] + 10, now.box[3] }, now, "Lv. 28",
                                0, 0, center: true);
            CenterPivot(mlt.rectTransform, s_spec.markX);

            // 아래 상자
            var bx = R("path_box");
            Img(Place(sec, "PathBox", bx[0], bx[1], bx[2], bx[3]), Spr("path_box"));
            HitButton(sec, "PathBoxButton", bx[0], bx[1], bx[2], bx[3]);
            var rewards = Node(sec, "PathRewards"); Stretch(rewards);
            string[] icons = { "reward_coin", "reward_gem", "reward_flame" };
            string[] amounts = { "PathRewardGold", "PathRewardGem", "PathRewardCore" };
            var val = T("BoxValue");
            for (int i = 0; i < 3; i++)
            {
                var s = R($"@SLOT{i}");
                Img(Place(rewards, $"PathRewardIcon{i}", s[0] + 14, s[1] + 8, s[2] - 14, s[3] - 18), Spr(icons[i])).preserveAspect = true;
                TextAtBox(rewards, amounts[i], new[] { (int)s[0] + 2, (int)s[3] - 20, (int)s[2] - 2, (int)s[3] - 4 }, val, "×5,000",
                          0, 0, center: true);
            }
            var a = Text(sec, "BoxLvLabel", T("BoxLvLabel"), "Lv.", null);
            var b = Text(sec, "BoxLvNum", T("BoxLvNum"), "28", null);
            var c = Text(sec, "BoxLvMax", T("BoxLvMax"), "/ 50", null);
            Run(sec, "BoxLvRun", new[] { a, b, c }, new[] { "BoxLvLabel", "BoxLvNum", "BoxLvMax" });
            Bar(sec, "BoxBar", "bar_box");
            Text(sec, "BoxValueText", T("BoxValue"), "1,820 / 2,400", null);
        }

        private static void BindGrowth(GrowthPanel gp)
        {
            var so = new SerializedObject(gp);
            var arts = so.FindProperty("_hostArts");
            var keys = Directory.GetDirectories(UnitDir);
            arts.arraySize = 0;
            foreach (var d in keys)
            {
                string key = Path.GetFileName(d);
                var portrait = AssetDatabase.LoadAssetAtPath<Sprite>($"{UnitDir}/{key}/unit_{key}_e.png");
                var thumb = AssetDatabase.LoadAssetAtPath<Sprite>($"{UnitDir}/{key}/unit_{key}_se.png");
                if (portrait == null && thumb == null) continue;
                int i = arts.arraySize++;
                var e = arts.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = key;
                e.FindPropertyRelative("Portrait").objectReferenceValue = portrait;
                e.FindPropertyRelative("Thumb").objectReferenceValue = thumb;
                e.FindPropertyRelative("Skill").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{SkillIconDir}/ultimateicon_{key}.png");
            }
            // HostStat 순서 — Hp · Atk · Crit · AtkSpeed · Range · MoveSpeed
            var icons = so.FindProperty("_statIcons");
            string[] iconFiles = { "icon_hp", "icon_atk", "icon_crit", "icon_atkspeed", "icon_range", "icon_movespeed" };
            icons.arraySize = iconFiles.Length;
            for (int i = 0; i < iconFiles.Length; i++) icons.GetArrayElementAtIndex(i).objectReferenceValue = Spr(iconFiles[i]);
            so.FindProperty("_starBigOn").objectReferenceValue = Spr("star_big_on");
            so.FindProperty("_starBigOff").objectReferenceValue = Spr("star_big_off");
            so.FindProperty("_starSmallOn").objectReferenceValue = Spr("slot_star_on");
            so.FindProperty("_starSmallOff").objectReferenceValue = Spr("slot_star_off");
            so.FindProperty("_nodeDone").objectReferenceValue = Spr("node_done");
            so.FindProperty("_nodeNext").objectReferenceValue = Spr("node_next");
            so.FindProperty("_nodeLock").objectReferenceValue = Spr("node_lock");
            so.FindProperty("_labelDone").colorValue = Col(T("PathLabelDone").color);
            so.FindProperty("_labelNow").colorValue = Col(T("PathLabelNow").color);
            so.FindProperty("_labelNext").colorValue = Col(T("PathLabelNext").color);
            so.FindProperty("_labelLock").colorValue = Col(T("PathLabelLock").color);
            so.FindProperty("_skillListDrop").floatValue = s_spec.skillListDy;
            so.FindProperty("_ghostStatLift").floatValue = 50f;
            so.FindProperty("_rowPitch").floatValue = 50.6f;
            so.FindProperty("_cardPitch").vector2Value = new Vector2(174.3f, 162f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 공통 하단 바 ────────────────────────────────────────

        private static RectTransform BuildNav(Transform root)
        {
            int navY = s_spec.navY;
            float h = 1280 - navY;
            var nav = Node(root, "BottomNav");
            nav.anchorMin = nav.anchorMax = nav.pivot = new Vector2(0.5f, 0f);
            nav.anchoredPosition = Vector2.zero;
            nav.sizeDelta = new Vector2(PageW, h);
            nav.gameObject.AddComponent<ScreenFitLock>();
            Img(Place(nav, "NavSideL", -400, 0, 0, h), Spr("nav_side_l"));
            Img(Place(nav, "NavSideR", PageW, 0, PageW + 400, h), Spr("nav_side_r"));
            Img(Place(nav, "NavBg", 0, 0, PageW, h), Spr("nav_bg"));
            foreach (var (btn, node, subNode, key) in new[] {
                         ("host", "HostButton", "HostButtonSubText", "ui.lobby.host_button.sub"),
                         ("play", "ChapterButton", "ChapterButtonSubText", "ui.lobby.game_mode"),
                         ("shop", "ShopButton", "ShopButtonSubText", "ui.lobby.shop_button.sub") })
            {
                var r = R($"nav_{btn}");
                var b = HitButton(nav, node, r[0], r[1] - navY, r[2], r[3] - navY);
                var on = Img(Place(b, "NavOn", 0, 0, r[2] - r[0], r[3] - r[1]), Spr($"nav_{btn}_on"));
                var off = Img(Place(b, "NavOff", 0, 0, r[2] - r[0], r[3] - r[1]), Spr($"nav_{btn}_off"));
                var sub = R($"@NAV_SUB_{btn}");
                var onSpec = T($"Nav_{btn}_on"); var offSpec = T($"Nav_{btn}_off");
                var tmp = TextAtBox(b, subNode, new[] { (int)sub[0], offSpec.box[1], (int)sub[2], offSpec.box[3] }, offSpec,
                                    Localize(key), r[0], r[1], center: true);
                Key(tmp.gameObject, key);
                var view = b.gameObject.AddComponent<NavTabView>();
                view.Set(on.gameObject, off.gameObject, tmp, Col(onSpec.color), Col(offSpec.color));
                view.SetSelected(btn == "play");
            }
            return nav;
        }

        // ── 글자 ────────────────────────────────────────────────

        /// <summary>페이지 좌표 글자 — 스펙 칸 그대로.</summary>
        private static TextMeshProUGUI Text(RectTransform parent, string name, GText t, string text, string key)
        {
            var tmp = TextAt(parent, name, t.align == "C" ? t.area : t.box, t, text, t.align == "C", 0, 0, t.align == "R");
            if (key != null) Key(tmp.gameObject, key);
            return tmp;
        }

        /// <summary>줄(부모) 안 글자 — 부모 왼쪽 위(ox, oy) 기준.</summary>
        private static TextMeshProUGUI TextIn(RectTransform parent, string name, GText t, string text, float ox, float oy)
            => TextAt(parent, name, t.align == "C" ? t.area : t.box, t, text, t.align == "C", ox, oy, t.align == "R");

        /// <summary>
        /// 정한 칸에 글자 — 가로 · **세로 모두** <paramref name="box"/> 를 따른다(스펙은 글자 크기 · 색만).
        /// ⚠ 세로를 스펙에서 읽으면 패시브 글자가 액티브 줄에 찍힌다(2026-09-21).
        /// </summary>
        private static TextMeshProUGUI TextAtBox(RectTransform parent, string name, int[] box, GText t, string text,
                                                 float ox = 0, float oy = 0, bool center = false)
            => TextAt(parent, name, box, t, text, center, ox, oy, false, box);

        private static TextMeshProUGUI TextAt(RectTransform parent, string name, int[] box, GText t, string text,
                                              bool center, float ox, float oy, bool right = false, int[] yBox = null)
        {
            // 오른쪽 정렬은 잉크 오른쪽 끝을 지킨다 — 칸을 왼쪽으로 넉넉히 넓힌다
            float x0 = right ? t.area[0] : box[0];
            // 왼쪽 정렬은 오른쪽으로 여유 30 — 칸이 잉크에 딱 붙으면 보정으로 키운 만큼 자동 줄이기가 도로 줄인다(「Lv.」)
            float x1 = right ? t.box[2] : Mathf.Max(box[2], t.area != null ? t.area[2] : box[2]) + 30f;
            // 칸을 정해 준 글자는 그 칸만 쓴다 — 스펙 칸(다른 자리에서 잰 것)으로 넓히면 옆 칸까지 먹는다(탭 글자 폭 484, 2026-09-21)
            if (center || yBox != null) { x0 = box[0]; x1 = box[2]; }
            var yb = yBox ?? t.box;
            var c = CalibOf(t.name);
            var rt = Place(parent, name, x0 - ox + c.dx, yb[1] - oy - Pad + c.dy, x1 - ox + c.dx, yb[3] - oy + Pad + c.dy);
            // 가로 비율(localScale.x)은 기준점 쪽으로 줄어든다 — 가운데 · 오른쪽 정렬은 기준점을 그쪽에 둬야 제자리다
            if (center || right)
            {
                float px = center ? 0.5f : 1f;
                rt.pivot = new Vector2(px, 1f);
                rt.anchoredPosition += new Vector2(rt.sizeDelta.x * px, 0f);
            }
            rt.localScale = new Vector3(c.aspect, 1f, 1f);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = s_bold;
            tmp.fontSize = t.size * c.scale;
            tmp.color = Col(t.color);
            tmp.text = text;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.horizontalAlignment = center ? HorizontalAlignmentOptions.Center
                                   : right ? HorizontalAlignmentOptions.Right : HorizontalAlignmentOptions.Left;
            tmp.verticalAlignment = t.lines > 1 ? VerticalAlignmentOptions.Top : VerticalAlignmentOptions.Geometry;
            if (t.lines > 1) TopAlign(rt);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = tmp.fontSize;
            tmp.fontSizeMin = tmp.fontSize * 0.6f;
            tmp.raycastTarget = false;
            var heavy = new SerializedObject(rt.gameObject.AddComponent<HeavyText>());
            heavy.FindProperty("_dilate").floatValue = c.dilate;
            heavy.ApplyModifiedPropertiesWithoutUndo();
            return tmp;
        }

        private static void Wrap(TextMeshProUGUI t, float width)
        {
            t.textWrappingMode = TextWrappingModes.Normal;
            if (t.verticalAlignment != VerticalAlignmentOptions.Top) TopAlign(t.rectTransform);
            t.verticalAlignment = VerticalAlignmentOptions.Top;
            t.enableAutoSizing = false;
            var rt = t.rectTransform;
            rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        }

        /// <summary>
        /// 여러 줄 · 줄바꿈 글자 — 칸 윗변을 첫 줄 잉크 윗선에 둔다(위아래 여유 Pad 를 걷는다).
        /// ⚠ 반대로 올리면 한 칸 위로 떠서 이름 줄과 겹친다(2026-09-21).
        /// </summary>
        private static void TopAlign(RectTransform rt)
        {
            var p = rt.anchoredPosition;
            p.y -= Pad;
            rt.anchoredPosition = p;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, rt.sizeDelta.y + 40);
        }

        /// <summary>글자 칸을 가운데 기준으로 — 코드가 x 만 옮기면 되게.</summary>
        private static void CenterPivot(RectTransform rt, float x)
        {
            rt.pivot = new Vector2(0.5f, 1f);
            var p = rt.anchoredPosition;
            p.x = x;
            rt.anchoredPosition = p;
        }

        private static void Run(RectTransform parent, string name, TextMeshProUGUI[] parts, string[] names)
        {
            var gaps = new float[parts.Length - 1];
            for (int i = 0; i < gaps.Length; i++) gaps[i] = T(names[i + 1]).box[0] - T(names[i]).box[2];
            var holder = Node(parent, name);
            holder.gameObject.AddComponent<InkRun>().Set(parts, gaps);
        }

        private static void Key(GameObject go, string key)
        {
            var lt = go.GetComponent<LocalizedText>();
            if (lt == null) lt = go.AddComponent<LocalizedText>();
            var so = new SerializedObject(lt);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>빌더 기본 글자 — 시안 언어(한국어 시안이지만 게임 기본은 일본어라 일본어로 칸을 잰다).</summary>
        private static string Localize(string key)
        {
            var st = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/BundleResource/TableData/StringTable.asset");
            var e = st != null ? st.Find(key) : null;
            if (e == null) return key;
            return (e.Raw(Language.Japanese) ?? e.Korean).Replace("\\n", "\n");
        }

        // ── 노드 ────────────────────────────────────────────────

        private static RectTransform HitButton(RectTransform parent, string name, float x0, float y0, float x1, float y1)
        {
            var b = Place(parent, name, x0, y0, x1, y1);
            var img = b.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            b.gameObject.AddComponent<Button>().targetGraphic = img;
            return b;
        }

        private static RectTransform Place(RectTransform parent, string name, float x0, float y0, float x1, float y1)
        {
            var rt = Node(parent, name);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x0, -y0);
            rt.sizeDelta = new Vector2(x1 - x0, y1 - y0);
            return rt;
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
            var img = rt.gameObject.GetComponent<Image>();
            if (img == null) img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = false;
            if (sprite == null) img.enabled = false;
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

        private static GText T(string name)
        {
            var t = Array.Find(s_spec.texts, x => x.name == name);
            if (t == null) throw new Exception($"[육성] 글자 스펙 없음: {name}");
            return t;
        }

        private static float[] R(string name)
        {
            var r = Array.Find(s_spec.rects, x => x.name == name);
            if (r == null) throw new Exception($"[육성] 자리 스펙 없음: {name}");
            return r.box;
        }

        private static Color Col(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;

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
