using System.Collections.Generic;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.LobbyShopBuilder;

namespace Game.Editor
{
    /// <summary>
    /// 시즌 패스 창(`SeasonPassPanel`) — 시안 S16(보상) · S17(미션) × 0.765. 위 막대 시즌 배지로 연다.
    /// 단계 30줄 · 미션 줄은 스크롤 안. 그림은 `Assets/BaseResource/Pass` 부품.
    /// </summary>
    public static class SeasonPassBuilder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Pa = "Assets/BaseResource/Pass";
        private const string St = "Assets/BaseResource/Store";
        private const string Pr = "Assets/BaseResource/Store/Products";
        private const string Ch = "Assets/BaseResource/ChapterHost";
        private const string Nav = "Assets/BaseResource/LobbyMainUI/Nav";
        private const string Lobby = "Assets/BaseResource/LobbyMainUI";
        private const string Su = "Assets/BaseResource/Summon";
        private const string Pf = "Assets/BaseResource/Profile";

        private static readonly Color Ink = new(0.16f, 0.08f, 0.03f, 1f);
        private static readonly Color Pink = new(0.98f, 0.55f, 0.95f, 1f);
        private static readonly Color Cyan = new(0.45f, 0.9f, 1f, 1f);
        private static readonly Color Gold = new(1f, 0.85f, 0.32f, 1f);

        private const int Rows = 30;
        private const float RowStep = 67f, RowH = 57f, ListW = 675f;

        [MenuItem("Tools/Game/로비 — 시즌 패스 창 세우기 (S16 · S17)")]
        public static void Run()
        {
            B($"{Pa}/tier_row_free.png", 40); B($"{Pa}/tier_row_premium.png", 40); B($"{Pa}/tier_num_plate.png", 30);
            B($"{Pa}/mission_row.png", 40); B($"{Pa}/btn_claim.png", 34); B($"{Pa}/mission_bar_bg.png", new Vector4(10, 4, 10, 4));
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[패스 창] 글자 본이 없다"); return; }
                var old = lobby.transform.Find("SeasonPassPanel");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var panel = new GameObject("SeasonPassPanel", typeof(RectTransform)).GetComponent<RectTransform>();
                panel.gameObject.layer = lobby.layer;
                panel.SetParent(lobby.transform, false);
                Stretch(panel);
                var nav = lobby.transform.Find("BottomNav");
                if (nav != null) panel.SetSiblingIndex(nav.GetSiblingIndex());
                var box = new GameObject("PassContent", typeof(RectTransform)).GetComponent<RectTransform>();
                box.gameObject.layer = lobby.layer;
                box.SetParent(panel, false);
                box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
                box.sizeDelta = new Vector2(720, 1280);
                box.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();

                var bg = Pic(box, "PassBackdrop", $"{Ch}/ch_screen_s22e.png", 0, 0, 0, 0);
                bg.preserveAspect = false;
                bg.raycastTarget = true;
                float k = 720f / 941f;
                C(bg.rectTransform, 360, 645, bg.sprite.rect.width * k, bg.sprite.rect.height * k);

                Banner(box);
                Tabs(box);
                Sliced(box, "PassListFrame", $"{Ch}/ch_s22e_box.png", 15, 457, 704, 1104, 2f);
                Rewards(Page(box, "PassRewardsPage"));
                Missions(Page(box, "PassMissionsPage"));

                var comp = panel.gameObject.AddComponent<SeasonPassPanel>();
                Bind(comp);
                var main = new SerializedObject(lobby.GetComponent<LobbyMainUI>());
                main.FindProperty("_passPanel").objectReferenceValue = comp;
                main.ApplyModifiedPropertiesWithoutUndo();
                panel.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[패스 창] 세움");
        }

        private static RectTransform Page(Transform box, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = box.gameObject.layer;
            var r = go.GetComponent<RectTransform>();
            r.SetParent(box, false);
            Stretch(r);
            return r;
        }

        private static void Banner(Transform box)
        {
            Sliced(box, "PassBannerFrame", $"{Ch}/ch_s22e_box.png", 15, 111, 704, 385, 2f);
            // 시안은 그림이 칸을 꽉 채운다 — 비율 맞춰 덮게 키우고 칸(마스크)이 잘라 낸다
            var artMask = Node(box, "PassBannerMask", 22, 117, 697, 379);
            artMask.gameObject.AddComponent<RectMask2D>();
            var art = Pic(artMask, "PassBannerArt", $"{Pa}/pass_banner_art.png", 0, 0, 0, 0);
            art.preserveAspect = false;
            if (art.sprite != null)
            {
                // 그림(3.34)이 칸(2.58)보다 길다 — 높이를 칸에 맞추고 왼쪽(유령)에 붙여 오른쪽 하늘만 잘린다
                float h = 379 - 117, w = h * art.sprite.rect.width / art.sprite.rect.height;
                art.rectTransform.anchorMin = art.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                art.rectTransform.pivot = new Vector2(0f, 0.5f);
                art.rectTransform.anchoredPosition = new Vector2(-(697 - 22) * 0.5f, 0f);
                art.rectTransform.sizeDelta = new Vector2(w, h);
            }
            Title(box, "PassSeasonText", 283, 142, 200, 40, 30, Pink).alignment = TextAlignmentOptions.Left;
            Title(box, "PassNameText", 262, 196, 400, 64, 52, Color.white).alignment = TextAlignmentOptions.Center;
            Body(box, "PassSloganText", 300, 246, 320, 26, 17, Color.white).alignment = TextAlignmentOptions.Center;
            Body(box, "PassTimeText", 520, 139, 160, 26, 18, Color.white).alignment = TextAlignmentOptions.Right;
            Title(box, "PassLevelText", 278, 283, 80, 40, 30, Cyan).alignment = TextAlignmentOptions.Left;
            var bar = Sliced(box, "PassXpBar", $"{Su}/pity_bar_bg.png", 356, 272, 681, 295, 3f);
            var fill = Pic(bar.transform, "PassXpFill", $"{Su}/pity_bar_fill.png", 0, 0, 0, 0);
            StretchIn(fill.rectTransform);
            fill.preserveAspect = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            var xt = Body(bar.transform, "PassXpText", 0, 0, 0, 0, 17, Color.white);
            Local(xt.rectTransform, 90, 0, 140, 22);
            xt.alignment = TextAlignmentOptions.Right;

            var a = Sliced(box, "PassBuyPremium", $"{Su}/pull_plate_10.png", 226, 310, 444, 373, 2.6f);
            a.raycastTarget = true;
            Button(a.gameObject);
            var at = Title(a.transform, "PassBuyPremiumText", 0, 0, 0, 0, 21, Color.white);
            Local(at.rectTransform, 0, 0, 200, 56);
            at.textWrappingMode = TextWrappingModes.Normal;
            var b = Sliced(box, "PassBuyBundle", $"{Nav}/nav_plate_summon.png", 451, 310, 685, 373, 2.6f);
            b.raycastTarget = true;
            Button(b.gameObject);
            var bt = Title(b.transform, "PassBuyBundleText", 0, 0, 0, 0, 21, Color.white);
            Local(bt.rectTransform, 0, 0, 216, 56);
            bt.textWrappingMode = TextWrappingModes.Normal;
        }

        private static void Tabs(Transform box)
        {
            Tab(box, "PassTabRewards", "PassTabRewardsText", $"{Nav}/icon_season_crown.png", 17, 398, 233, 448);
            Tab(box, "PassTabMissions", "PassTabMissionsText", $"{Pa}/micon_login.png", 241, 398, 448, 448);
            var dot = Pic(box, "PassMissionDot", $"{Nav}/icon_reddot.png", 430, 390, 452, 412);
            dot.gameObject.SetActive(false);
            var all = Sliced(box, "PassClaimAll", $"{St}/btn_small.png", 509, 398, 700, 448, 3f);
            all.raycastTarget = true;
            Button(all.gameObject);
            var t = Title(all.transform, "PassClaimAllText", 0, 0, 0, 0, 22, Cyan);
            Local(t.rectTransform, 0, 0, 170, 34);
        }

        private static void Tab(Transform box, string name, string label, string icon, float x0, float y0, float x1, float y1)
        {
            var tab = Sliced(box, name, $"{St}/shop_tab.png", x0, y0, x1, y1, 2.2f);
            tab.raycastTarget = true;
            Button(tab.gameObject);
            var ic = Pic(tab.transform, "TabIcon", icon, 0, 0, 0, 0);
            Local(ic.rectTransform, -(x1 - x0) * 0.5f + 44, 0, 30, 28);
            var lb = Title(tab.transform, label, 0, 0, 0, 0, 23, Color.white);
            Local(lb.rectTransform, 16, 0, 120, 32);
        }

        // ── 보상 30단계 ──────────────────────────────────────

        private static void Rewards(Transform page)
        {
            Body(page, "PassHeadTier", 32, 478, 80, 28, 20, Color.white).alignment = TextAlignmentOptions.Center;
            Body(page, "PassHeadFree", 140, 478, 250, 28, 20, Color.white).alignment = TextAlignmentOptions.Center;
            Pic(page, "PassHeadCrown", $"{Nav}/icon_season_crown.png", 455, 466, 485, 491);
            Body(page, "PassHeadPremium", 490, 478, 200, 28, 20, Gold).alignment = TextAlignmentOptions.Left;
            var content = Scroll(page, "PassScroll", 22, 500, 697, 1096, Rows * RowStep);
            for (int i = 0; i < Rows; i++) Row(content, i);
        }

        private static void Row(RectTransform content, int i)
        {
            var row = new GameObject($"PassRow{i}", typeof(RectTransform)).GetComponent<RectTransform>();
            row.gameObject.layer = content.gameObject.layer;
            row.SetParent(content, false);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = new Vector2(0, -(i * RowStep + RowStep * 0.5f));
            row.sizeDelta = new Vector2(ListW, RowH);
            var num = Sliced(row, "TierPlate", $"{Pa}/tier_num_plate.png", 0, 0, 0, 0, 2.6f);
            Local(num.rectTransform, -ListW * 0.5f + 40, 0, 78, RowH);
            var now = Sliced(row, "TierNow", $"{St}/shop_tab_sel.png", 0, 0, 0, 0, 2.6f);
            Local(now.rectTransform, -ListW * 0.5f + 40, 0, 82, RowH + 4);
            now.gameObject.SetActive(false);
            var t = Title(row, "TierText", 0, 0, 0, 0, 32, Cyan);
            Local(t.rectTransform, -ListW * 0.5f + 40, 0, 70, 44);
            Side(row, "Free", $"{Pa}/tier_row_free.png", -ListW * 0.5f + 95, 272, i);
            Side(row, "Prem", $"{Pa}/tier_row_premium.png", -ListW * 0.5f + 372, 296, i);
        }

        private static void Side(RectTransform row, string side, string plate, float left, float w, int i)
        {
            float cx = left + w * 0.5f;
            var p = Sliced(row, $"{side}Plate", plate, 0, 0, 0, 0, 2.6f);
            Local(p.rectTransform, cx, 0, w, RowH);
            var glow = Sliced(row, $"{side}Glow", $"{St}/shop_tab_sel.png", 0, 0, 0, 0, 2.6f);
            Local(glow.rectTransform, cx, 0, w, RowH);
            glow.gameObject.SetActive(false);
            var ic = Pic(row, $"{side}Icon", null, 0, 0, 0, 0);
            Local(ic.rectTransform, left + 36, 0, 48, 40);
            var n = Body(row, $"{side}Name", 0, 0, 0, 0, 16, Color.white);
            Pivot(n.rectTransform, 0f);
            Local(n.rectTransform, left + 72, 12, w - 150, 22);
            n.alignment = TextAlignmentOptions.Left;
            var a = Title(row, $"{side}Amount", 0, 0, 0, 0, 19, Color.white);
            Pivot(a.rectTransform, 0f);
            Local(a.rectTransform, left + 72, -12, w - 150, 24);
            a.alignment = TextAlignmentOptions.Left;
            var claim = Sliced(row, $"PassRow{i}{side}Claim", $"{Pa}/btn_claim.png", 0, 0, 0, 0, 3f);
            Local(claim.rectTransform, left + w - 52, 0, 90, 34);
            claim.raycastTarget = true;
            Button(claim.gameObject);
            var ct = Title(claim.transform, $"{side}ClaimText", 0, 0, 0, 0, 18, Ink);
            Local(ct.rectTransform, 0, 0, 80, 26);
            Plain(ct);
            var chk = Pic(row, $"{side}Check", $"{Pa}/mark_claimed.png", 0, 0, 0, 0);
            Local(chk.rectTransform, left + w - 40, 0, 34, 34);
            var lk = Node(row, $"{side}Lock", 0, 0, 0, 0);
            Local(lk, left + w - 34, 0, 50, RowH - 6);
            var li = Pic(lk, "LockIcon", $"{Ch}/ch_lock.png", 0, 0, 0, 0);
            Local(li.rectTransform, 0, 8, 22, 26);
            var lt = Body(lk, $"{side}LockText", 0, 0, 0, 0, 14, Color.white);
            Local(lt.rectTransform, 0, -14, 50, 18);
            claim.gameObject.SetActive(false);
            chk.gameObject.SetActive(false);
            lk.gameObject.SetActive(false);
        }

        // ── 미션 ─────────────────────────────────────────────

        private const float MStep = 47f, MRowH = 42f;

        private static void Missions(Transform page)
        {
            float h = 2 * 46 + 16 * MStep;
            var content = Scroll(page, "MissionScroll", 22, 466, 697, 1096, h);
            float y = 0;
            for (int k = 0; k < 2; k++)
            {
                var head = Body(content, k == 0 ? "MissionDailyHead" : "MissionWeeklyHead", 0, 0, 0, 0, 24, Color.white);
                head.alignment = TextAlignmentOptions.Left;
                var hr = head.rectTransform;
                hr.anchorMin = hr.anchorMax = new Vector2(0f, 1f);
                hr.pivot = new Vector2(0f, 0.5f);
                hr.anchoredPosition = new Vector2(54, -(y + 23));
                hr.sizeDelta = new Vector2(560, 34);
                var hi = Pic(content, $"MissionHeadIcon{k}", k == 0 ? $"{Lobby}/chesttimeicon.png" : $"{Pa}/micon_login.png", 0, 0, 0, 0);
                var hir = hi.rectTransform;
                hir.anchorMin = hir.anchorMax = new Vector2(0f, 1f);
                hir.anchoredPosition = new Vector2(30, -(y + 23));
                hir.sizeDelta = new Vector2(30, 30);
                y += 46;
                for (int i = 0; i < 8; i++)
                {
                    MRow(content, k, i, y);
                    y += MStep;
                }
            }
        }

        private static void MRow(RectTransform content, int k, int i, float y)
        {
            var row = new GameObject($"Mission{k}_{i}", typeof(RectTransform)).GetComponent<RectTransform>();
            row.gameObject.layer = content.gameObject.layer;
            row.SetParent(content, false);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0, -(y + MStep * 0.5f));
            row.sizeDelta = new Vector2(ListW - 10, MRowH);
            var p = Sliced(row, "MPlate", $"{Pa}/mission_row.png", 0, 0, 0, 0, 3f);
            StretchIn(p.rectTransform);
            float L = -(ListW - 10) * 0.5f;
            var ic = Pic(row, "MIcon", null, 0, 0, 0, 0);
            Local(ic.rectTransform, L + 28, 0, 34, 32);
            var n = Body(row, "MName", 0, 0, 0, 0, 17, Color.white);
            Pivot(n.rectTransform, 0f);
            Local(n.rectTransform, L + 54, 0, 150, 26);
            n.alignment = TextAlignmentOptions.Left;
            var bar = Sliced(row, "MBar", $"{Pa}/mission_bar_bg.png", 0, 0, 0, 0, 2f);
            Local(bar.rectTransform, L + 290, -9, 150, 8);
            var fill = Pic(bar.transform, "MFill", $"{Pa}/mission_bar_fill.png", 0, 0, 0, 0);
            StretchIn(fill.rectTransform);
            fill.preserveAspect = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            var ct = Body(row, "MCount", 0, 0, 0, 0, 14, Color.white);
            Pivot(ct.rectTransform, 1f);
            Local(ct.rectTransform, L + 365, 9, 90, 18);
            ct.alignment = TextAlignmentOptions.Right;
            var ri = Pic(row, "MRewardIcon", $"{Pa}/micon_possess.png", 0, 0, 0, 0);
            Local(ri.rectTransform, L + 395, 0, 26, 26);
            var rw = Body(row, "MReward", 0, 0, 0, 0, 14, Color.white);
            Pivot(rw.rectTransform, 0f);
            Local(rw.rectTransform, L + 412, 0, 150, 22);
            rw.alignment = TextAlignmentOptions.Left;
            float bx = (ListW - 10) * 0.5f - 52;
            var claim = Sliced(row, $"Mission{k}_{i}Claim", $"{Pa}/btn_claim.png", 0, 0, 0, 0, 3f);
            Local(claim.rectTransform, bx, 0, 92, 34);
            claim.raycastTarget = true;
            Button(claim.gameObject);
            var cl = Title(claim.transform, "MClaimText", 0, 0, 0, 0, 17, Ink);
            Local(cl.rectTransform, 0, 0, 84, 26);
            Plain(cl);
            Pill(row, "MDoing", "MDoingText", bx, Color.white);
            Pill(row, "MDone", "MDoneText", bx, new Color(0.6f, 0.95f, 0.6f));
            Pill(row, "MLock", "MLockText", bx, Color.white);
            claim.gameObject.SetActive(false);
        }

        private static void Pill(RectTransform row, string name, string textName, float x, Color color)
        {
            var b = Sliced(row, name, $"{St}/btn_small.png", 0, 0, 0, 0, 3.4f);
            Local(b.rectTransform, x, 0, 92, 34);
            var t = Body(b.transform, textName, 0, 0, 0, 0, 15, color);
            Local(t.rectTransform, 0, 0, 84, 24);
            b.gameObject.SetActive(false);
        }

        private static RectTransform Scroll(Transform page, string name, float x0, float y0, float x1, float y1, float height)
        {
            var view = Node(page, name, x0, y0, x1, y1);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            var content = new GameObject(name + "Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.gameObject.layer = view.gameObject.layer;
            content.SetParent(view, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, height);
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }

        private static void Bind(SeasonPassPanel p)
        {
            var so = new SerializedObject(p);
            so.FindProperty("_gem").objectReferenceValue = Spr($"{Pr}/prod_gem_1.png");
            so.FindProperty("_gold").objectReferenceValue = Spr($"{Pr}/prod_gold_1.png");
            so.FindProperty("_essence").objectReferenceValue = Spr($"{Pa}/micon_possess.png");
            so.FindProperty("_ticket").objectReferenceValue = Spr($"{Pr}/icon_ticket.png");
            so.FindProperty("_stamina").objectReferenceValue = Spr($"{Pr}/prod_stamina.png");
            so.FindProperty("_chestSilver").objectReferenceValue = Spr($"{Lobby}/chest_silver.png");
            so.FindProperty("_chestGold").objectReferenceValue = Spr($"{Lobby}/chest_gold.png");
            so.FindProperty("_chestPlatinum").objectReferenceValue = Spr($"{Lobby}/chest_magic.png");
            so.FindProperty("_frame").objectReferenceValue = Spr($"{Pf}/profile_frame_s1.png");
            so.FindProperty("_title").objectReferenceValue = Spr($"{Nav}/icon_season_crown.png");
            so.FindProperty("_tab").objectReferenceValue = Spr($"{St}/shop_tab.png");
            so.FindProperty("_tabSelected").objectReferenceValue = Spr($"{St}/shop_tab_sel.png");
            var icons = new List<(string, string)>
            {
                ("login", "micon_login"), ("run_start", "micon_run"), ("run_clear", "micon_run"), ("possess", "micon_possess"),
                ("distinct_hosts_possessed", "micon_hosts"), ("kill_advantage", "micon_advantage"), ("kill_boss", "micon_boss"),
                ("daily_shop_buy", "micon_shop"), ("stat_upgrade", "micon_upgrade"), ("chest_open", "micon_chest"),
                ("daily_all_done", "micon_chest"), ("weekly_all_done", "micon_chest"),
            };
            var prop = so.FindProperty("_missionIcons");
            prop.arraySize = icons.Count;
            for (int i = 0; i < icons.Count; i++)
            {
                var e = prop.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = icons[i].Item1;
                e.FindPropertyRelative("Sprite").objectReferenceValue = Spr($"{Pa}/{icons[i].Item2}.png");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
