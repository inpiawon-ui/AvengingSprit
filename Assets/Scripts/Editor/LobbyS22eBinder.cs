using System.IO;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 로비 챕터 · 호스트 창을 **시안 S22e**(PD 2026-10-15 확정 — 출전 방식 셋 · 카드 키트 · ⚡ 도전하기)로 꽂는다.
    ///
    /// 순서: 「챕터·호스트 선택 창 세우기」 → 「위 · 아래 고정 꽂기」 → 「「?」 카드 · 설명 칸 꽂기」 → **이것**.
    /// 앞 단계가 세운 노드를 S22e 자리로 옮기고, 출전 방식 · 카드 키트 · 버튼 줄 노드를 더한다.
    ///
    /// ⚠ 칸 그림은 판에 굽지 않는다. 코덱스가 칸 좌표를 시안대로 옮기지 못해(챕터 · 보스 칸이 옛 자리),
    ///   칸 없는 배경(`ch_screen_s22e`) 위에 늘려 쓰는 칸(`ch_s22e_box`)을 시안 좌표대로 놓는다.
    ///   좌표는 S22e(941 폭)를 재서 0.765 배한 화면 좌표(720 × 1280, 왼쪽 위 기준)다.
    /// </summary>
    public static class LobbyS22eBinder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Ch = "Assets/BaseResource/ChapterHost";
        private const string Nav = "Assets/BaseResource/LobbyMainUI/Nav";

        private static readonly Color Dark = new(0.08f, 0.1f, 0.24f, 1f);
        private static readonly Color Gold = new(1f, 0.85f, 0.32f, 1f);
        private static readonly Color Soft = new(0.78f, 0.84f, 0.95f, 1f);

        // 출전 카드 셋 — 가운데 x(화면), 가운데 y 620, 205 × 228
        private static readonly float[] ModeX = { 144.5f, 359.5f, 574.5f };
        private static readonly string[] ModeNodes = { "CHModeHost", "CHModeRandom", "CHModeGhost" };

        [MenuItem("Tools/Game/로비 — 출전 방식 · 카드 키트 · 도전하기 꽂기 (S22e)")]
        public static void Run()
        {
            if (!File.Exists($"{Ch}/ch_screen_s22e.png"))
            {
                Debug.LogError("[S22e] 칸 없는 배경 ch_screen_s22e.png 가 없다 — 옛 판 위에 칸을 겹치면 두 겹이 된다. 멈춘다");
                return;
            }
            Borders();

            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                var box = Find(lobby.transform, "CHContent");
                var panel = lobby.GetComponentInChildren<ChapterHostPanel>(true);
                if (box == null || panel == null) { Debug.LogError("[S22e] CHContent · ChapterHostPanel 없음"); return; }

                // 옛 자리 → 새 자리 옮기기(MoveY)는 **처음 한 번만** — 다시 돌리면 두 번 밀린다
                bool first = box.Find("CHSecChapter") == null;
                Backdrop(box);
                Sections(box);
                ChapterGroup(box, first);
                BossGroup(box, first);
                ModeSection(box);
                HostSection(box);
                InfoRow(box);
                StartButton(box);
                BindPanel(panel);
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[S22e] 로비 챕터 · 호스트 창을 S22e 로 꽂았다");
        }

        // ── 위 막대 — 행동력 · 금화 · 젬 세 칸(S22e) ─────────────────────────
        //
        // 띠 그림(fixed_top_band_s22e, 1080 × 180)을 재서 720 으로 옮긴 자리. 칸 y 23~67.
        //   행동력 x 187~323(번개 195~215 · + 283~315) · 금화 330~470 · 젬 477~613 · 편지 621~673 · 톱니 680~720
        // 유령 프로필 판은 0~187 로 줄인다(시안 S22e 의 프로필 폭).

        [MenuItem("Tools/Game/로비 — 위 막대 행동력 칸 꽂기 (S22e)")]
        public static void RunTop()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                var bar = Find(lobby.transform, "FixedTopBar");
                if (bar == null) { Debug.LogError("[S22e] FixedTopBar 없음"); return; }
                var band = bar.GetComponent<Image>();
                var sprite = Spr($"{Nav}/fixed_top_band_s22e.png");
                if (band == null || sprite == null) { Debug.LogError("[S22e] 띠 그림 없음"); return; }
                band.sprite = sprite;

                if (bar.Find("LobbyProfile") is Transform profile) profile.localScale = new Vector3(0.75f, 0.75f, 1f);

                var st = CloneTop(bar, "GoldText", "StaminaText");
                st.fontSize = 20;
                st.alignment = TextAlignmentOptions.Center;
                TL(st.rectTransform, 218, 22, 64, 24);
                var timer = CloneTop(bar, "GoldText", "StaminaTimerText");
                timer.fontSize = 13;
                timer.alignment = TextAlignmentOptions.Center;
                timer.color = Soft;
                TL(timer.rectTransform, 218, 45, 64, 16);
                var plusSrc = bar.Find("GoldPlusButton");
                if (bar.Find("StaminaPlusButton") == null && plusSrc != null)
                    Object.Instantiate(plusSrc.gameObject, bar, false).name = "StaminaPlusButton";
                TLn(bar, "StaminaPlusButton", 283, 24, 34, 34);

                TLn(bar, "GoldText", 366, 20, 60, 42);
                foreach (var n in new[] { "GoldText", "GemText" })
                    if (bar.Find(n)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI money)
                    {
                        money.enableAutoSizing = true;
                        money.fontSizeMin = 10;
                        money.fontSizeMax = 20;
                    }
                TLn(bar, "GoldPlusButton", 430, 24, 34, 34);
                TLn(bar, "GemText", 520, 20, 54, 42);
                TLn(bar, "GemPlusButton", 575, 24, 34, 34);
                TLn(bar, "MailButton", 621, 20, 52, 48);
                TLn(bar, "SettingsButton", 680, 22, 40, 44);
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[S22e] 위 막대 행동력 칸");
        }

        private static TextMeshProUGUI CloneTop(Transform bar, string template, string name)
        {
            var t = bar.Find(name);
            if (t == null)
            {
                var src = bar.Find(template);
                t = Object.Instantiate(src.gameObject, bar, false).transform;
                t.name = name;
            }
            var tmp = t.GetComponent<TextMeshProUGUI>();
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10;
            return tmp;
        }

        // 위 막대 안 자리 — 왼쪽 위 기준(x, y 는 칸의 왼쪽 위)
        private static void TL(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
        }

        private static void TLn(Transform bar, string name, float x, float y, float w, float h)
        {
            if (bar.Find(name) is RectTransform r) TL(r, x, y, w, h);
            else Debug.LogWarning($"[S22e] 노드 없음: {name}");
        }

        // ── 아래 바 4칸 — HOST · PLAY · SUMMON · SHOP (PD 2026-10-15 A안) ─────────────

        private static readonly string[] Tabs = { "HostButton", "ChapterButton", "SummonButton", "ShopButton" };
        private static readonly string[] TabTitles = { "HOST", "PLAY", "SUMMON", "SHOP" };
        private static readonly string[] TabIcons = { "nav_icon_host", "nav_icon_play", "nav_icon_summon", "nav_icon_shop" };
        private const float TabW = 168f, TabH = 84f, TabGap = 6f, TabLeft = 15f;

        /// <summary>
        /// 아래 바를 4칸으로. 예전 탭은 아이콘 · 「HOST」 글자까지 구운 236 폭 통그림이라 4칸(168 폭)에 못 넣는다 —
        /// 새 칸 판(`nav_plate`) 위에 아이콘(옛 통그림에서 오린 것)과 제목 글자를 얹는다.
        /// </summary>
        [MenuItem("Tools/Game/로비 — 아래 바 4칸 꽂기 (S22e)")]
        public static void RunNav()
        {
            Border($"{Nav}/nav_sel_rim.png", 48);
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                var nav = Find(lobby.transform, "BottomNav");
                if (nav == null) { Debug.LogError("[S22e] BottomNav 없음"); return; }
                var shop = nav.Find("ShopButton");
                if (nav.Find("SummonButton") == null && shop != null)
                {
                    var summon = Object.Instantiate(shop.gameObject, nav, false);
                    summon.name = "SummonButton";
                    summon.transform.SetSiblingIndex(shop.GetSiblingIndex());
                    var sub = summon.transform.Find("ShopButtonSubText");
                    if (sub != null)
                    {
                        sub.name = "SummonButtonSubText";
                        var loc = sub.GetComponent("LocalizedText");
                        if (loc != null)
                        {
                            var so = new SerializedObject(loc);
                            so.FindProperty("_key").stringValue = "ui.lobby.summon_button.sub";
                            so.ApplyModifiedPropertiesWithoutUndo();
                        }
                        sub.GetComponent<TMP_Text>().text = "소환";
                    }
                }
                for (int i = 0; i < Tabs.Length; i++) Tab(nav, i);
                Dots(nav);
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[S22e] 아래 바 4칸");
        }

        private static void Tab(Transform nav, int i)
        {
            if (nav.Find(Tabs[i]) is not RectTransform tab) { Debug.LogWarning($"[S22e] 칸 없음: {Tabs[i]}"); return; }
            tab.anchorMin = tab.anchorMax = tab.pivot = new Vector2(0f, 1f);
            tab.anchoredPosition = new Vector2(TabLeft + i * (TabW + TabGap), -13f);
            tab.sizeDelta = new Vector2(TabW, TabH);
            if (tab.GetComponent<Image>() is Image hit) hit.color = new Color(1f, 1f, 1f, 0f);   // 누를 자리만

            bool summon = Tabs[i] == "SummonButton";
            var plate = Img(tab, "NavPlate", $"{Nav}/{(summon ? "nav_plate_summon" : "nav_plate")}.png");
            plate.raycastTarget = false;
            Stretch(plate.rectTransform, 0f);
            plate.transform.SetAsFirstSibling();

            var off = tab.Find("NavOff");
            if (off != null) off.gameObject.SetActive(false);   // 옛 통그림 — 판 · 아이콘 · 글자가 대신한다
            var on = Img(tab, "NavOn", $"{Nav}/nav_sel_rim.png");
            on.type = Image.Type.Sliced;
            on.pixelsPerUnitMultiplier = 2.6f;
            on.raycastTarget = false;
            Stretch(on.rectTransform, -5f);

            var icon = Img(tab, "NavIcon", $"{Nav}/{TabIcons[i]}.png");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            At(icon.rectTransform, -56f, 1f, 44f, 44f);

            var sub = FindSub(tab);
            var title = tab.Find("NavTitle")?.GetComponent<TextMeshProUGUI>();
            if (title == null && sub != null)
            {
                var go = Object.Instantiate(sub.gameObject, tab, false);
                go.name = "NavTitle";
                var loc = go.GetComponent("LocalizedText");
                if (loc != null) Object.DestroyImmediate(loc);   // 「HOST」 등은 언어와 상관없이 같은 글자다
                title = go.GetComponent<TextMeshProUGUI>();
            }
            if (title != null)
            {
                title.text = TabTitles[i];
                title.fontSize = 20;
                title.alignment = TextAlignmentOptions.Center;
                title.enableAutoSizing = true;
                title.fontSizeMin = 12;
                title.fontSizeMax = 20;
                At(title.rectTransform, 22f, 11f, 104f, 26f);
            }
            if (sub != null)
            {
                var st = sub.GetComponent<TextMeshProUGUI>();
                st.fontSize = 13;
                st.alignment = TextAlignmentOptions.Center;
                st.enableAutoSizing = true;
                st.fontSizeMin = 9;
                st.fontSizeMax = 13;
                At(st.rectTransform, 22f, -14f, 104f, 20f);
            }
            on.transform.SetAsLastSibling();

            var view = tab.GetComponent<Game.Module.Common.UI.NavTabView>();
            if (view == null) view = tab.gameObject.AddComponent<Game.Module.Common.UI.NavTabView>();
            var so = new SerializedObject(view);
            so.FindProperty("_on").objectReferenceValue = on.gameObject;
            so.FindProperty("_off").objectReferenceValue = null;
            if (sub != null) so.FindProperty("_sub").objectReferenceValue = sub.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_titleOn").colorValue = new Color(1f, 0.92f, 0.2f, 1f);
            so.FindProperty("_titleOff").colorValue = summon ? new Color(0.93f, 0.62f, 1f, 1f) : Color.white;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindSub(Transform tab)
        {
            foreach (Transform c in tab)
                if (c.name.EndsWith("SubText")) return c;
            return null;
        }

        private static void Dots(Transform nav)
        {
            var dots = nav.Find("NavDots");
            if (dots == null) return;
            if (dots.Find("NavDot3") == null && dots.Find("NavDot2") is Transform d2)
                Object.Instantiate(d2.gameObject, dots, false).name = "NavDot3";
            for (int i = 0; i < 4; i++)
                if (dots.Find($"NavDot{i}") is RectTransform r) r.anchoredPosition = new Vector2(-45f + i * 30f, 0f);
        }

        private static void Stretch(RectTransform r, float pad)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = new Vector2(pad, pad);
            r.offsetMax = new Vector2(-pad, -pad);
        }

        // ── 그림 준비 — 늘려 쓰는 칸은 9칸 테두리 ─────────────────

        private static void Borders()
        {
            Border($"{Ch}/ch_s22e_box.png", 34);
            Border($"{Ch}/ch_s22e_bossframe.png", 16);
            Border($"{Ch}/ch_s22e_hline.png", new Vector4(8, 0, 8, 0));
            Border($"{Ch}/ch_card_frame_b.png", 30);
            Border($"{Ch}/ch_card_frame_a.png", 30);
            Border($"{Ch}/ch_card_frame_s.png", 30);
            Border($"{Ch}/ch_card_sel_overlay.png", 34);
            Border($"{Ch}/mode_card_sel.png", 44);
            Border($"{Ch}/row_dim.png", 30);
            Border($"{Ch}/row_notice_band.png", new Vector4(40, 0, 40, 0));
            Border($"{Ch}/ch_start_button.png", new Vector4(110, 60, 110, 60));
            Border($"{Ch}/start_btn_off.png", new Vector4(110, 55, 110, 55));
        }

        private static void Border(string path, float all) => Border(path, new Vector4(all, all, all, all));

        private static void Border(string path, Vector4 b)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) { Debug.LogWarning($"[S22e] 그림 없음: {path}"); return; }
            if (ti.spriteBorder == b) return;
            ti.spriteBorder = b;
            ti.SaveAndReimport();
        }

        // ── 배경 · 칸 ─────────────────────────────────────────

        private static void Backdrop(Transform box)
        {
            var screen = box.Find("CHScreen")?.GetComponent<Image>();
            if (screen == null) return;
            screen.sprite = Spr($"{Ch}/ch_screen_s22e.png");
            float k = 720f / 941f;
            C(box, "CHScreen", 360, 645, screen.sprite.rect.width * k, screen.sprite.rect.height * k);
        }

        private static void Sections(Transform box)
        {
            int at = box.Find("CHScreen").GetSiblingIndex() + 1;
            at = Sec(box, "CHSecChapter", "ch_s22e_box", 22, 122, 696, 327, at);
            at = Sec(box, "CHSecBoss", "ch_s22e_box", 22, 335, 315, 445, at);
            at = Sec(box, "CHSecBossFrame", "ch_s22e_bossframe", 37, 348, 170, 432, at);
            at = Sec(box, "CHSecReward", "ch_s22e_box", 321, 335, 696, 445, at);
            at = Sec(box, "CHSecMode", "ch_s22e_box", 22, 456, 696, 744, at);
            at = Sec(box, "CHSecModeTick", "ch_s22e_tick", 41, 469, 53, 491, at, sliced: false);
            at = Sec(box, "CHSecModeLine", "ch_s22e_hline", 36, 498, 684, 503, at);
            at = Sec(box, "CHSecHost", "ch_s22e_box", 22, 751, 696, 964, at);
            at = Sec(box, "CHSecHostTick", "ch_s22e_tick", 41, 762, 53, 784, at, sliced: false);
            at = Sec(box, "CHSecHostLine", "ch_s22e_hline", 36, 790, 684, 795, at);
            Sec(box, "CHSecInfoFrame", "ch_s22e_bossframe", 37, 968, 165, 1072, at);
        }

        private static int Sec(Transform box, string name, string sprite, float x0, float y0, float x1, float y1, int at,
                               bool sliced = true)
        {
            var img = Img(box, name, $"{Ch}/{sprite}.png");
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.pixelsPerUnitMultiplier = 2f;   // 판 그림은 화면의 약 1.3 배로 그려져 있다 — 테두리를 화면 굵기로
            img.raycastTarget = false;
            img.transform.SetSiblingIndex(at);
            C(box, name, (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, x1 - x0, y1 - y0);
            return at + 1;
        }

        // ── 챕터 칸 — 옛 칸(y 132~411) 안의 자리를 새 칸(y 122~327)으로 줄여 옮긴다 ─────

        private static float MapChapter(float y) => 122f + (y - 132f) * (205f / 279f);
        private static float MapBoss(float y) => 335f + (y - 423f) * (110f / 145f);

        private static void ChapterGroup(Transform box, bool first)
        {
            C(box, "CHChapterArtBox", 359, 224.5f, 662, 193);
            foreach (var n in new[] { "CHChapterArtBox/CHChapterDim", "CHChapterArtBox/CHChapterArt" })
                if (box.Find(n) is RectTransform r) r.sizeDelta = new Vector2(662, 193);
            if (first) MoveY(box, MapChapter, "CHPrevArrow", "CHNextArrow", "CHPrevButton", "CHNextButton", "CHChapterLabel",
                  "CHChapterNoText", "CHChapterNameText", "CHChapterDescText", "CHLockIcon", "CHRecPowerText",
                  "CHKindGem0", "CHKindCount0", "CHKindGem1", "CHKindCount1", "CHKindGem2", "CHKindCount2");
            // 설명은 두 줄 칸(적 구성 줄 위까지) — 넘치면 줄인다
            if (box.Find("CHChapterDescText") is RectTransform desc)
                desc.sizeDelta = new Vector2(desc.sizeDelta.x, 46);
            Shrink(box, "CHChapterDescText", 11);
            Shrink(box, "CHChapterNameText", 18);
        }

        /// <summary>칸이 낮아져 긴 언어(영어)가 넘친다 — 칸 안에서 자동으로 줄인다(06_ui 글자 넘침 금지).</summary>
        private static void Shrink(Transform box, string name, float min)
        {
            if (box.Find(name)?.GetComponent<TMP_Text>() is not TMP_Text t) return;
            if (!t.enableAutoSizing) t.fontSizeMax = t.fontSize;
            t.enableAutoSizing = true;
            t.fontSizeMin = min;
        }

        private static void BossGroup(Transform box, bool first)
        {
            if (first) MoveY(box, MapBoss, "CHIconSkull", "CHIconCoin", "CHBossPortrait", "CHBossLabel", "CHBossDash", "CHRewardLabel",
                  "CHBossNameText", "CHRewardGoldText", "CHRewardChestIcon", "CHRewardChestText", "CHBossKindGem");
            C(box, "CHBossPortrait", 103.5f, 390, 120, 76);
            Scale(box, "CHRewardChestIcon", 0.8f);
        }

        // ── 출전 방식 ─────────────────────────────────────────

        private static void ModeSection(Transform box)
        {
            var title = CloneText(box, "CHHostTitle", "CHModeTitle");
            L(box, "CHModeTitle", 64, 480, 300, 30, title.fontSize);
            title.text = "출전 방식";

            var art = new[] { (Sprite)null, Spr($"{Ch}/mode_random_art.png"), null };
            for (int i = 0; i < ModeNodes.Length; i++)
            {
                var card = Img(box, ModeNodes[i], $"{Ch}/mode_card_plate.png");
                card.raycastTarget = true;
                if (card.GetComponent<Button>() == null) card.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
                card.transform.SetSiblingIndex(box.Find("CHSecHostLine").GetSiblingIndex() + 1);
                C(box, ModeNodes[i], ModeX[i], 620, 205, 228);
                var t = card.transform;

                var a = Img(t, "ModeArt", null);
                a.sprite = art[i];
                a.preserveAspect = true;
                a.raycastTarget = false;
                At(a.rectTransform, 0, 22, 136, 100);

                var sel = Img(t, "ModeSel", $"{Ch}/mode_card_sel.png");
                sel.type = Image.Type.Sliced;
                sel.pixelsPerUnitMultiplier = 1.5f;
                sel.raycastTarget = false;
                At(sel.rectTransform, 0, 0, 215, 238);

                var tt = CloneText(t, "CHHostTitle", "ModeTitle", box);
                tt.alignment = TextAlignmentOptions.Center;
                tt.fontSize = 19;
                tt.enableAutoSizing = true;
                tt.fontSizeMin = 12;
                tt.fontSizeMax = 19;
                At(tt.rectTransform, -12, 80, 136, 26);   // 판 위 제목 띠 안 — 오른쪽은 고른 금 테의 체크 칸

                var si = Img(t, "ModeStaminaIcon", $"{Nav}/icon_stamina.png");
                si.preserveAspect = true;
                si.raycastTarget = false;
                At(si.rectTransform, -14, -40, 16, 24);
                var st = CloneText(t, "CHHostTitle", "ModeStaminaText", box);
                st.alignment = TextAlignmentOptions.Left;
                st.fontSize = 24;
                st.color = Gold;
                st.rectTransform.pivot = new Vector2(0f, 0.5f);
                At(st.rectTransform, -4, -40, 40, 28);

                var note = CloneText(t, "CHInfoDesc", "ModeNote", box);
                note.alignment = TextAlignmentOptions.Center;
                note.fontSize = 13;
                note.color = Soft;
                note.enableAutoSizing = true;
                note.fontSizeMin = 9;
                note.fontSizeMax = 13;
                At(note.rectTransform, 0, -73, 176, 34);
                sel.transform.SetAsLastSibling();   // 금 테가 맨 위
            }
        }

        // ── 호스트 줄 ─────────────────────────────────────────

        private static void HostSection(Transform box)
        {
            L(box, "CHHostTitle", 64, 773, 300, 30, Find(box, "CHHostTitle").GetComponent<TMP_Text>().fontSize);
            var note = CloneText(box, "CHInfoDesc", "CHTodayNote");
            note.alignment = TextAlignmentOptions.Right;
            note.fontSize = 14;
            note.color = Soft;
            note.enableAutoSizing = true;
            note.fontSizeMin = 10;
            note.fontSizeMax = 14;
            if (box.Find("CHTodayNote") is RectTransform tn)
            {
                tn.anchorMin = tn.anchorMax = new Vector2(0.5f, 0.5f);
                tn.pivot = new Vector2(1f, 0.5f);
                tn.anchoredPosition = new Vector2(684 - 360f, 640f - 773);
                tn.sizeDelta = new Vector2(420, 24);
            }

            C(box, "CHHostViewport", 359, 878, ChapterHostLayout.ModeViewWidth, 172);
            C(box, "CHHostPrev", 29, 878, 16, 80);
            C(box, "CHHostNext", 690, 878, 16, 80);
            var content = box.Find("CHHostViewport/CHHostContent") as RectTransform;
            if (content != null)
            {
                content.sizeDelta = new Vector2(content.sizeDelta.x, 172);
                for (int i = 0; i < content.childCount; i++) Card(content.GetChild(i), box);
            }

            var dim = Img(box, "CHHostDim", $"{Ch}/row_dim.png");
            dim.type = Image.Type.Sliced;
            dim.color = new Color(1f, 1f, 1f, 0.6f);
            dim.raycastTarget = true;   // 덮인 동안 카드가 눌리지 않게
            C(box, "CHHostDim", 359, 878, 656, 176);
            dim.transform.SetSiblingIndex(box.Find("CHHostNext").GetSiblingIndex() + 1);
            var band = Img(dim.transform, "CHHostDimBand", $"{Ch}/row_notice_band.png");
            band.type = Image.Type.Sliced;
            band.raycastTarget = false;
            At(band.rectTransform, 0, 0, 560, 44);
            var bt = CloneText(band.transform, "CHInfoDesc", "CHHostDimText", box);
            bt.alignment = TextAlignmentOptions.Center;
            bt.fontSize = 16;
            bt.color = Color.white;
            bt.enableAutoSizing = true;
            bt.fontSizeMin = 10;
            bt.fontSizeMax = 16;
            At(bt.rectTransform, 0, 0, 530, 36);
        }

        /// <summary>카드 한 장 — 100 × 168. 왼쪽 위 등급 · 오른쪽 위 속성 · 얼굴 · 별 · 이름 · 전투력 · 오늘의 몸 띠.</summary>
        private static void Card(Transform card, Transform box)
        {
            if (card is RectTransform cr) cr.sizeDelta = new Vector2(ChapterHostLayout.ModeCardWidth, ChapterHostLayout.ModeCardHeight);
            Fit(card, "CardFrame", 0, 0, 100, 168, sliced: true);
            Fit(card, "CardFrameSel", 0, 0, 108, 176, sliced: true, sprite: $"{Ch}/ch_card_sel_overlay.png");
            Fit(card, "CardThumb", 0, 30, 90, 84);
            Fit(card, "CardKindGem", 37, 70, 22, 22);
            Fit(card, "CardMatchArrow", 21, 70, 14, 14);
            for (int s = 0; s < 5; s++) Fit(card, $"CardStar{s}", -30 + s * 15, -18, 14, 14);
            Fit(card, "CardName", 0, -35, 96, 18, font: 14);
            Fit(card, "CardPowerIcon", -28, -53, 14, 14);
            if (card.Find("CardPowerText") is RectTransform pt)
            {
                pt.pivot = new Vector2(0f, 0.5f);
                pt.anchoredPosition = new Vector2(-18, -53);
                pt.sizeDelta = new Vector2(66, 18);
                pt.GetComponent<TMP_Text>().fontSize = 15;
            }
            // 체크는 고른 금 테 그림에 들어 있다 — 노드는 두고(코드가 켜고 끈다) 그림만 끈다
            if (card.Find("CardCheck")?.GetComponent<Image>() is Image check) check.enabled = false;

            var grade = Img(card, "CardGrade", $"{Ch}/ch_grade_b.png");
            grade.preserveAspect = true;
            grade.raycastTarget = false;
            At(grade.rectTransform, -36, 70, 24, 25);

            var today = Img(card, "CardToday", $"{Ch}/ch_today_band.png");
            today.raycastTarget = false;
            At(today.rectTransform, 0, -76, 98, 19);
            var tt = CloneText(today.transform, "CHInfoDesc", "CardTodayText", box);
            tt.alignment = TextAlignmentOptions.Center;
            tt.fontSize = 12;
            tt.color = Color.white;
            tt.enableAutoSizing = true;
            tt.fontSizeMin = 8;
            tt.fontSizeMax = 12;
            At(tt.rectTransform, 0, 1, 92, 17);
            today.gameObject.SetActive(false);

            // 금 테는 맨 위, 띠는 그 아래
            card.Find("CardFrameSel")?.SetAsLastSibling();
        }

        // ── 설명 줄 ───────────────────────────────────────────

        private static void InfoRow(Transform box)
        {
            C(box, "CHInfoPortrait", 101, 1020, 116, 94);
            C(box, "CHInfoRandomFx", 101, 1020, 150, 150);
            Scale(box, "CHInfoRandomFx", 0.65f);
            L(box, "CHInfoName", 176, 984, 130, 26, 20);
            C(box, "CHInfoKindIcon", 184, 1007, 18, 18);
            L(box, "CHInfoKindText", 196, 1007, 80, 20, 14);
            L(box, "CHInfoDesc", 176, 1034, 106, 46, 12);
            Shrink(box, "CHInfoDesc", 8);
            C(box, "CHInfoSkillA", 404, 1004, 230, 64);
            C(box, "CHInfoSkillP", 594, 1004, 230, 64);
            Scale(box, "CHInfoSkillA", 0.82f);
            Scale(box, "CHInfoSkillP", 0.82f);
            L(box, "CHRandomTitle", 176, 982, 320, 26, 20);
            L(box, "CHRandomDesc1", 176, 1005, 480, 20, 14);
            L(box, "CHRandomDesc2", 176, 1025, 480, 20, 14);
            C(box, "CHRandomPill", 630, 985, 116, 28);
            foreach (var n in new[] { "CHRandomCard", "CHIconDice", "CHRandomStrip", "CHInfoRandomDice" })
                if (box.Find(n) is Transform t) t.gameObject.SetActive(false);
        }

        // ── 도전하기 ─────────────────────────────────────────

        private static void StartButton(Transform box)
        {
            var b = box.Find("CHStartButton");
            if (b == null) return;
            var img = b.GetComponent<Image>() ?? b.gameObject.AddComponent<Image>();
            img.sprite = Spr($"{Ch}/ch_start_button.png");
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 3.2f;
            img.color = Color.white;
            img.raycastTarget = true;
            C(box, "CHStartButton", 360, 1094, 236, 80);
            b.SetAsLastSibling();

            var icon = Img(b, "CHStartStaminaIcon", $"{Nav}/icon_stamina.png");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            At(icon.rectTransform, -40, 16, 13, 19);
            var st = CloneText(b, "CHStartCostText", "CHStartStaminaText", box);
            var plus = CloneText(b, "CHStartCostText", "CHStartPlus", box);
            plus.text = "+";
            foreach (var t in new[] { st, plus, b.Find("CHStartCostText")?.GetComponent<TextMeshProUGUI>() })
            {
                if (t == null) continue;
                t.fontSize = 18;
                t.color = Dark;
                t.rectTransform.pivot = new Vector2(0f, 0.5f);
                t.rectTransform.anchoredPosition = new Vector2(t.rectTransform.anchoredPosition.x, 16);
                t.rectTransform.sizeDelta = new Vector2(40, 24);
            }
            if (b.Find("CHStartCostIcon") is RectTransform coin)
            {
                coin.sizeDelta = new Vector2(18, 18);
                coin.anchoredPosition = new Vector2(coin.anchoredPosition.x, 16);
            }

            var label = box.Find("CHStartLabel") ?? b.Find("CHStartLabel");
            if (label is RectTransform lr)
            {
                lr.SetParent(b, false);
                lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0.5f, 0.5f);
                lr.anchoredPosition = new Vector2(0, -13);
                lr.sizeDelta = new Vector2(200, 28);
                var lt = lr.GetComponent<TMP_Text>();
                lt.fontSize = 22;
                lt.alignment = TextAlignmentOptions.Center;
                lt.color = Dark;
            }

            var rib = Img(box, "CHStartRibbon", $"{Ch}/ribbon_today.png");
            rib.raycastTarget = false;
            C(box, "CHStartRibbon", 360, 1047, 146, 26);
            rib.transform.SetAsLastSibling();
            var rt = CloneText(rib.transform, "CHInfoDesc", "CHStartRibbonText", box);
            rt.alignment = TextAlignmentOptions.Center;
            rt.fontSize = 13;
            rt.color = Color.white;
            rt.enableAutoSizing = true;
            rt.fontSizeMin = 9;
            rt.fontSizeMax = 13;
            At(rt.rectTransform, 0, 1, 128, 24);
            rib.gameObject.SetActive(false);
        }

        private static void BindPanel(ChapterHostPanel panel)
        {
            var so = new SerializedObject(panel);
            Arr(so, "_gradeFrames", $"{Ch}/ch_card_frame_b.png", $"{Ch}/ch_card_frame_a.png", $"{Ch}/ch_card_frame_s.png");
            Arr(so, "_gradeBadges", $"{Ch}/ch_grade_b.png", $"{Ch}/ch_grade_a.png", $"{Ch}/ch_grade_s.png");
            so.FindProperty("_starHalf").objectReferenceValue = Spr($"{Ch}/ch_star_half.png");
            so.FindProperty("_ribbonToday").objectReferenceValue = Spr($"{Ch}/ribbon_today.png");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 도우미 ───────────────────────────────────────────

        private static void Arr(SerializedObject so, string field, params string[] paths)
        {
            var p = so.FindProperty(field);
            p.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = Spr(paths[i]);
        }

        private static Sprite Spr(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

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

        /// <summary>있으면 그대로, 없으면 만든다(다시 돌려도 겹치지 않게).</summary>
        private static Image Img(Transform parent, string name, string sprite)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.layer = parent.gameObject.layer;
                t = go.transform;
                t.SetParent(parent, false);
            }
            var img = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>();
            if (sprite != null) img.sprite = Spr(sprite);
            return img;
        }

        /// <summary>같은 글자 결(폰트 · 재질 · 외곽선)을 쓰려고 있는 글자 칸을 복제한다.</summary>
        private static TextMeshProUGUI CloneText(Transform parent, string template, string name, Transform searchRoot = null)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<TextMeshProUGUI>();
            var src = Find(searchRoot ?? parent, template);
            var go = Object.Instantiate(src.gameObject, parent, false);
            go.name = name;
            for (int i = go.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = string.Empty;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            return t;
        }

        /// <summary>부모 가운데 기준 자리.</summary>
        private static void At(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            if (r.pivot != new Vector2(0f, 0.5f)) r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        private static void Fit(Transform card, string name, float x, float y, float w, float h,
                                bool sliced = false, string sprite = null, float font = 0f)
        {
            if (card.Find(name) is not RectTransform r) return;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
            if (r.TryGetComponent<Image>(out var img))
            {
                if (sprite != null) img.sprite = Spr(sprite);
                if (sliced)
                {
                    img.type = Image.Type.Sliced;
                    img.pixelsPerUnitMultiplier = 2.4f;
                }
            }
            if (font > 0f && r.TryGetComponent<TMP_Text>(out var t)) t.fontSize = font;
        }

        private static void MoveY(Transform box, System.Func<float, float> map, params string[] names)
        {
            foreach (var n in names)
            {
                if (box.Find(n) is not RectTransform r) continue;
                float y = 640f - r.anchoredPosition.y;   // 화면 좌표(위 기준)
                r.anchoredPosition = new Vector2(r.anchoredPosition.x, 640f - map(y));
            }
        }

        private static void Scale(Transform box, string name, float s)
        {
            if (box.Find(name) is Transform t) t.localScale = new Vector3(s, s, 1f);
        }

        // 화면 좌표(720 × 1280, 왼쪽 위 기준) — 가운데 x, y
        private static void C(Transform box, string n, float x, float y, float w, float h)
        {
            if (box.Find(n) is not RectTransform r) { Debug.LogWarning($"[S22e] 노드 없음: {n}"); return; }
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x - 360f + (r.pivot.x - 0.5f) * w, 640f - y);
            r.sizeDelta = new Vector2(w, h);
        }

        // 왼쪽 끝 x, 가운데 y
        private static void L(Transform box, string n, float x, float y, float w, float h, float size)
        {
            if (box.Find(n) is not RectTransform r) { Debug.LogWarning($"[S22e] 노드 없음: {n}"); return; }
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(x - 360f, 640f - y);
            r.sizeDelta = new Vector2(w, h);
            if (r.TryGetComponent<TMP_Text>(out var tmp)) tmp.fontSize = size;
        }
    }
}
