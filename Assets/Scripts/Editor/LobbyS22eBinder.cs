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
        private static readonly Color Ink = new(0.16f, 0.08f, 0.03f, 1f);        // 시안 도전하기 글자 — 짙은 고동
        private static readonly Color CostRed = new(0.72f, 0.12f, 0.06f, 1f);    // 시안 「5 + 132」

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
            Border($"{Nav}/top_pass_plate.png", new Vector4(70, 40, 70, 40));
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                var bar = Find(lobby.transform, "FixedTopBar");
                if (bar == null) { Debug.LogError("[S22e] FixedTopBar 없음"); return; }
                var band = bar.GetComponent<Image>();
                var sprite = Spr($"{Nav}/fixed_top_band_s22e.png");
                if (band == null || sprite == null) { Debug.LogError("[S22e] 띠 그림 없음"); return; }
                band.sprite = sprite;
                // 칸 판이 그려진 한 장 그림이라 가로로 늘리면(4:3 에서 960) 판과 글자가 어긋난다 —
                // 720 가운데로 고정하고 남는 양옆은 이음 띠(FixedTopBarEdgeL · R)가 메운다
                var barRect = (RectTransform)bar;
                float barH = barRect.rect.height > 0f ? barRect.rect.height : barRect.sizeDelta.y;
                barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 1f);
                barRect.pivot = new Vector2(0.5f, 1f);
                barRect.sizeDelta = new Vector2(720f, barH);
                barRect.anchoredPosition = new Vector2(0f, barRect.anchoredPosition.y);
                // ScreenFit 이 「화면 폭을 채우는 판」으로 보고 다시 늘리지 않게
                if (bar.GetComponent<Game.Module.Common.UI.ScreenFitLock>() == null)
                    bar.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();

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

                // 시즌 패스 배지 — 시안 오른쪽 아래 x 318~532 · y 80~114. 누르면 패스(값은 LobbyMainUI 가 채운다)
                var pass = Img(bar, "PassBadge", $"{Nav}/top_pass_plate.png");
                pass.raycastTarget = true;
                pass.type = Image.Type.Sliced;
                pass.pixelsPerUnitMultiplier = 2.6f;
                if (pass.GetComponent<Button>() == null) pass.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
                TL(pass.rectTransform, 318, 80, 380, 36);
                var crown = Img(pass.transform, "PassCrown", $"{Nav}/icon_season_crown.png");
                crown.preserveAspect = true;
                crown.raycastTarget = false;
                At(crown.rectTransform, -168, 0, 26, 22);
                // 글자는 배지 밑으로 옮겨 두어 막대에서 다시 못 찾는다 — 다시 돌릴 때마다 겹쳐 쌓이지 않게 먼저 지운다
                for (int i = pass.transform.childCount - 1; i >= 0; i--)
                {
                    var old = pass.transform.GetChild(i);
                    // 예전 이름(PassTitleText · PassLevelText)은 패스 창 글자와 이름이 겹쳐 값이 엉뚱한 칸에 들어갔다
                    if (old.name.StartsWith("PassTitleText") || old.name.StartsWith("PassLevelText")
                        || old.name.StartsWith("PassBadgeTitle") || old.name.StartsWith("PassBadgeLevel"))
                        Object.DestroyImmediate(old.gameObject);
                }
                var ptxt = CloneTop(bar, "GoldText", "PassBadgeTitleText");
                ptxt.transform.SetParent(pass.transform, false);
                ptxt.fontSize = 14;
                ptxt.fontSizeMax = 14;
                ptxt.alignment = TextAlignmentOptions.Left;
                ptxt.color = Color.white;
                ptxt.rectTransform.anchorMin = ptxt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ptxt.rectTransform.pivot = new Vector2(0f, 0.5f);
                ptxt.rectTransform.anchoredPosition = new Vector2(-148, 0);
                ptxt.rectTransform.sizeDelta = new Vector2(240, 24);
                var plv = CloneTop(bar, "GoldText", "PassBadgeLevelText");
                plv.transform.SetParent(pass.transform, false);
                plv.fontSize = 17;
                plv.fontSizeMax = 17;
                plv.alignment = TextAlignmentOptions.Right;
                plv.color = Gold;
                plv.rectTransform.anchorMin = plv.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                plv.rectTransform.pivot = new Vector2(1f, 0.5f);
                plv.rectTransform.anchoredPosition = new Vector2(166, 0);   // 오른쪽 화살 끝 · 「!」 안쪽
                plv.rectTransform.sizeDelta = new Vector2(70, 26);
                var pdot = Img(pass.transform, "PassBadgeDot", $"{Nav}/icon_reddot.png");
                pdot.preserveAspect = true;
                pdot.raycastTarget = false;
                At(pdot.rectTransform, 192, 16, 22, 22);   // 시안처럼 배지 오른쪽 위 모서리에 걸친다
                Bang(pdot.transform, bar.Find("GoldText"));
                pdot.gameObject.SetActive(false);

                // 시즌 배지 글자 · 아래 바 「!」 를 채우는 것 — 로비 전체(위 막대 · 아래 바를 다 찾는다)
                if (lobby.GetComponent<Game.Module.Lobby.LobbyNewsBadges>() == null)
                    lobby.AddComponent<Game.Module.Lobby.LobbyNewsBadges>();
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
        // 시안 S22e 아래 바 — 칸 163 × 80, 간격 9, 왼쪽 18 (시안 941 폭을 0.765 배)
        private const float TabW = 163f, TabH = 80f, TabGap = 9f, TabLeft = 18f;

        /// <summary>
        /// 아래 바를 4칸으로. 예전 탭은 아이콘 · 「HOST」 글자까지 구운 236 폭 통그림이라 4칸(168 폭)에 못 넣는다 —
        /// 새 칸 판(`nav_plate`) 위에 아이콘(옛 통그림에서 오린 것)과 제목 글자를 얹는다.
        /// </summary>
        [MenuItem("Tools/Game/로비 — 아래 바 4칸 꽂기 (S22e)")]
        public static void RunNav()
        {
            Border($"{Nav}/nav_sel_rim.png", 48);
            Border($"{Nav}/nav_plate.png", 60);
            Border($"{Nav}/nav_plate_summon.png", 60);
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
                // 육성 화면의 nav_bg 는 탭 셋이 그려진 그림이라 새 4칸 사이로 옛 탭 · 글자가 비친다 — 무늬 없는 띠(양옆 이음 그림)로
                var bgNode = nav.Find("NavBg");
                var bg = bgNode != null ? bgNode.GetComponent<Image>() : null;
                var band = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BaseResource/Growth/nav_side_l.png");
                if (bg != null && band != null)
                {
                    bg.sprite = band;
                    bg.preserveAspect = false;
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
            tab.anchoredPosition = new Vector2(TabLeft + i * (TabW + TabGap), -14f);
            tab.sizeDelta = new Vector2(TabW, TabH);
            if (tab.GetComponent<Image>() is Image hit) hit.color = new Color(1f, 1f, 1f, 0f);   // 누를 자리만

            bool summon = Tabs[i] == "SummonButton";
            var plate = Img(tab, "NavPlate", $"{Nav}/{(summon ? "nav_plate_summon" : "nav_plate")}.png");
            plate.raycastTarget = false;
            plate.type = Image.Type.Sliced;   // 칸 비율이 그림과 달라 9분할로 늘린다(하네스 R1)
            plate.pixelsPerUnitMultiplier = 2.4f;
            Stretch(plate.rectTransform, 0f);
            plate.transform.SetAsFirstSibling();

            var off = tab.Find("NavOff");
            if (off != null) off.gameObject.SetActive(false);   // 옛 통그림 — 판 · 아이콘 · 글자가 대신한다
            var on = Img(tab, "NavOn", $"{Nav}/nav_sel_rim.png");
            on.type = Image.Type.Sliced;
            on.pixelsPerUnitMultiplier = 2.6f;
            on.raycastTarget = false;
            Stretch(on.rectTransform, 0f);   // 칸 안 — 밖으로 내면 칸 밖이 된다(하네스 C1)

            var icon = Img(tab, "NavIcon", $"{Nav}/{TabIcons[i]}.png");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            At(icon.rectTransform, -50f, 1f, 58f, 56f);

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
                title.fontSize = 25;
                title.fontStyle = FontStyles.Bold;
                title.alignment = TextAlignmentOptions.Center;
                title.enableAutoSizing = true;
                title.fontSizeMin = 13;
                title.fontSizeMax = 25;
                At(title.rectTransform, 30f, 11f, 94f, 30f);
                // 시안 제목은 굵은 글자 + 검은 외곽선(옛 탭 그림의 「HOST」 결)
                if (title.GetComponent<Game.Module.Common.HeavyText>() is Game.Module.Common.HeavyText heavy)
                    heavy.Set(0.18f, 0.22f, new Color32(8, 10, 30, 255));
            }
            if (sub != null)
            {
                var st = sub.GetComponent<TextMeshProUGUI>();
                st.fontSize = 13;
                st.alignment = TextAlignmentOptions.Center;
                st.enableAutoSizing = true;
                st.fontSizeMin = 9;
                st.fontSizeMax = 13;
                st.fontSize = 14;
                st.fontSizeMax = 14;
                At(st.rectTransform, 27f, -16f, 88f, 20f);   // 오른쪽 테두리 안쪽까지(영 「Train Hosts」 · 일 「ホスト育成」 가 테에 닿았다)
            }
            on.transform.SetSiblingIndex(plate.transform.GetSiblingIndex() + 1);   // 판 바로 위 · 아이콘 · 글자 아래(하네스 C6)

            // 새 소식 「!」 — 소환(오늘 무료) · 상점(오늘의 무료 선물). 켜고 끄는 것은 LobbyMainUI
            if (summon || Tabs[i] == "ShopButton")
            {
                var badge = Img(tab, "NavBadge", $"{Nav}/icon_reddot.png");
                badge.preserveAspect = true;
                badge.raycastTarget = false;
                At(badge.rectTransform, TabW * 0.5f - 6f, TabH * 0.5f - 4f, 24f, 24f);   // 시안처럼 칸 모서리에 걸친다(제목을 안 가린다)
                badge.transform.SetAsLastSibling();
                Bang(badge.transform, FindSub(tab));
                badge.gameObject.SetActive(false);
            }

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
            dots.gameObject.SetActive(false);   // 시안 S22e 아래 바에는 점이 없다 — 고른 칸은 금 테가 보인다
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
            Border($"{Ch}/mode_card_sel_plain.png", 44);
            Border($"{Ch}/ribbon_today.png", new Vector4(40, 20, 40, 20));
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
            at = Sec(box, "CHSecModeTick", "ch_s22e_tick", 41, 467, 54, 489, at, sliced: false);
            at = Sec(box, "CHSecModeLine", "ch_s22e_hline", 36, 498, 684, 503, at);
            at = Sec(box, "CHSecHost", "ch_s22e_box", 22, 751, 696, 964, at);
            at = Sec(box, "CHSecHostTick", "ch_s22e_tick", 41, 762, 54, 784, at, sliced: false);
            at = Sec(box, "CHSecHostLine", "ch_s22e_hline", 36, 790, 684, 795, at);
            Sec(box, "CHSecInfoFrame", "ch_s22e_bossframe", 30, 968, 182, 1072, at);
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
            if (box.Find("CHIconSkull") is Transform skull && skull.GetComponent<Image>() is Image skullImg)
                skullImg.preserveAspect = true;   // 칸(33 × 39)이 그림(0.73)보다 넓다
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
            BossMask(box);
            Scale(box, "CHRewardChestIcon", 0.8f);
        }

        // ── 출전 방식 ─────────────────────────────────────────

        private static void ModeSection(Transform box)
        {
            var title = CloneText(box, "CHHostTitle", "CHModeTitle");
            L(box, "CHModeTitle", 66, 480, 300, 34, 27);
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
                At(a.rectTransform, 0, 18, 168, 122);

                // 시안의 고른 카드는 금 테만 — 체크 칸이 없다(체크는 호스트 카드에만). 체크를 뺀 테를 쓴다
                var sel = Img(t, "ModeSel", $"{Ch}/mode_card_sel_plain.png");
                sel.type = Image.Type.Sliced;
                sel.pixelsPerUnitMultiplier = 1.4f;
                sel.raycastTarget = false;
                At(sel.rectTransform, 0, 0, 205, 228);

                var tt = CloneText(t, "CHHostTitle", "ModeTitle", box);
                tt.alignment = TextAlignmentOptions.Center;
                tt.fontSize = 22;
                tt.enableAutoSizing = true;
                tt.fontSizeMin = 12;
                tt.fontSizeMax = 22;
                At(tt.rectTransform, 0, 81, 160, 28);   // 판 위 제목 띠 안

                var si = Img(t, "ModeStaminaIcon", $"{Nav}/icon_stamina.png");
                si.preserveAspect = true;
                si.raycastTarget = false;
                At(si.rectTransform, -16, -50, 19, 28);
                var st = CloneText(t, "CHHostTitle", "ModeStaminaText", box);
                st.alignment = TextAlignmentOptions.Left;
                st.fontSize = 30;
                st.color = Gold;
                st.rectTransform.pivot = new Vector2(0f, 0.5f);
                At(st.rectTransform, -4, -50, 44, 34);

                var note = CloneText(t, "CHInfoDesc", "ModeNote", box);
                note.alignment = TextAlignmentOptions.Center;
                note.fontSize = 15;
                note.color = Color.white;
                note.enableAutoSizing = true;
                note.fontSizeMin = 9;
                note.fontSizeMax = 15;
                At(note.rectTransform, 0, -86, 186, 38);
                // 금 테는 판 테두리를 덮고 글자 · 그림 **뒤** — 맨 위에 두면 제목을 가린다(하네스 C6)
                sel.transform.SetAsFirstSibling();
            }
        }

        // ── 호스트 줄 ─────────────────────────────────────────

        private static void HostSection(Transform box)
        {
            L(box, "CHHostTitle", 66, 773, 300, 32, 24);
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
            Fit(card, "CardFrameSel", 0, 0, 100, 168, sliced: true, sprite: $"{Ch}/ch_card_sel_overlay.png");
            // 고른 테는 판 바로 위 · 글자 아래 — 위에 그리면 아래 전투력 글자 끝을 테가 덮는다
            if (card.Find("CardFrame") is Transform frame && card.Find("CardFrameSel") is Transform selFrame)
                selFrame.SetSiblingIndex(frame.GetSiblingIndex() + 1);
            Fit(card, "CardThumb", 0, 30, 90, 84);
            Fit(card, "CardKindGem", 33, 68, 22, 22);
            Fit(card, "CardMatchArrow", 17, 68, 14, 14);
            for (int s = 0; s < 5; s++) Fit(card, $"CardStar{s}", -30 + s * 15, -18, 14, 14);
            Fit(card, "CardName", 0, -36, 90, 18, font: 14);
            if (card.Find("CardName")?.GetComponent<TMP_Text>() is TMP_Text cn)
            {
                cn.enableAutoSizing = true;
                cn.fontSizeMin = 9;
                cn.fontSizeMax = 14;
            }
            Fit(card, "CardPowerIcon", -28, -53, 14, 14);
            if (card.Find("CardPowerText") is RectTransform pt)
            {
                pt.pivot = new Vector2(0f, 0.5f);
                pt.anchoredPosition = new Vector2(-18, -53);
                pt.sizeDelta = new Vector2(56, 18);   // 오른쪽 금 테(약 6) 안쪽까지
                var ptText = pt.GetComponent<TMP_Text>();
                ptText.fontSize = 15;
                ptText.enableAutoSizing = true;
                ptText.fontSizeMin = 10;
                ptText.fontSizeMax = 15;
            }
            // 체크는 고른 금 테 그림에 들어 있다 — 노드는 두고(코드가 켜고 끈다) 그림만 끈다
            if (card.Find("CardCheck")?.GetComponent<Image>() is Image check) check.enabled = false;

            var grade = Img(card, "CardGrade", $"{Ch}/ch_grade_b.png");
            grade.preserveAspect = true;
            grade.raycastTarget = false;
            At(grade.rectTransform, -34, 68, 24, 25);

            var today = Img(card, "CardToday", $"{Ch}/ch_today_band.png");
            today.raycastTarget = false;
            At(today.rectTransform, 0, -73, 94, 18);
            var tt = CloneText(today.transform, "CHInfoDesc", "CardTodayText", box);
            tt.alignment = TextAlignmentOptions.Center;
            tt.fontSize = 12;
            tt.color = Color.white;
            tt.enableAutoSizing = true;
            tt.fontSizeMin = 8;
            tt.fontSizeMax = 12;
            At(tt.rectTransform, 0, 0, 84, 15);
            today.gameObject.SetActive(false);

            // 미보유 카드(시안 「???」) — 자물쇠
            var lockImg = Img(card, "CardLock", $"{Ch}/ch_lock.png");
            lockImg.preserveAspect = true;
            lockImg.raycastTarget = false;
            At(lockImg.rectTransform, 0, 30, 30, 38);
            lockImg.gameObject.SetActive(false);

            // 고른 금 테는 판 바로 위 · 얼굴 · 글자 아래(이름 · 전투력을 덮지 않게), 「오늘의 몸」 띠는 맨 위
            if (card.Find("CardFrame") is Transform cf && card.Find("CardFrameSel") is Transform cs)
                cs.SetSiblingIndex(cf.GetSiblingIndex() + 1);
            card.Find("CardToday")?.SetAsLastSibling();
        }

        // ── 설명 줄 ───────────────────────────────────────────

        private static void InfoRow(Transform box)
        {
            C(box, "CHInfoPortrait", 106, 1020, 150, 100);
            C(box, "CHInfoRandomFx", 106, 1020, 150, 150);
            Scale(box, "CHInfoRandomFx", 0.65f);
            L(box, "CHInfoName", 197, 980, 104, 30, 24);
            Shrink(box, "CHInfoName", 12);
            C(box, "CHInfoKindIcon", 206, 1006, 20, 20);
            L(box, "CHInfoKindText", 219, 1006, 80, 22, 16);
            L(box, "CHInfoDesc", 197, 1040, 104, 52, 13);
            Shrink(box, "CHInfoDesc", 8);
            SkillBox(box, "CHInfoSkillA", 396, 1012);
            SkillBox(box, "CHInfoSkillP", 591, 1012);
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
            C(box, "CHStartButton", 365, 1094, 200, 70);
            b.SetAsLastSibling();

            var icon = Img(b, "CHStartStaminaIcon", $"{Nav}/icon_stamina.png");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            At(icon.rectTransform, -40, 15, 15, 22);
            var st = CloneText(b, "CHStartCostText", "CHStartStaminaText", box);
            var plus = CloneText(b, "CHStartCostText", "CHStartPlus", box);
            plus.text = "+";
            foreach (var t in new[] { st, plus, b.Find("CHStartCostText")?.GetComponent<TextMeshProUGUI>() })
            {
                if (t == null) continue;
                t.fontSize = 21;
                t.color = CostRed;
                // 시안은 굵은 빨강 — 외곽선 없이 굵기만 올린다
                if (t.GetComponent<Game.Module.Common.HeavyText>() is Game.Module.Common.HeavyText th)
                    th.Set(0.3f, 0f, new Color32(0, 0, 0, 0));
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
                lr.anchoredPosition = new Vector2(0, -11);   // 아래 금 테에 안 걸리게
                lr.sizeDelta = new Vector2(180, 30);
                var lt = lr.GetComponent<TMP_Text>();
                lt.fontSize = 27;
                lt.fontStyle = FontStyles.Bold;
                lt.alignment = TextAlignmentOptions.Center;
                lt.color = Ink;
                lt.enableAutoSizing = true;
                lt.fontSizeMin = 14;
                lt.fontSizeMax = 26;
                Plain(lt);
                // 시안의 「도전하기 ▶」 는 두꺼운 먹색 — Plain 뒤에 굵기만 다시 올린다
                if (lt.GetComponent<Game.Module.Common.HeavyText>() is Game.Module.Common.HeavyText lh)
                    lh.Set(0.35f, 0f, new Color32(0, 0, 0, 0));
            }

            var rib = Img(box, "CHStartRibbon", $"{Ch}/ribbon_today.png");
            rib.raycastTarget = false;
            // 띠 그림(3.8)보다 칸이 길다 — 9분할로 늘린다. 왼쪽 설명 글자와 안 겹치게 조금 오른쪽
            rib.type = Image.Type.Sliced;
            rib.pixelsPerUnitMultiplier = 3f;
            C(box, "CHStartRibbon", 372, 1047, 128, 26);   // 왼쪽 설명 · 아래 스킬 설명 글자를 덮지 않게 폭을 줄인다
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
            so.FindProperty("_starGold").objectReferenceValue = Spr($"{Ch}/ch_star_gold.png");
            so.FindProperty("_shardIcon").objectReferenceValue = Spr("Assets/BaseResource/Growth/shard_icon.png");
            so.FindProperty("_starSilver").objectReferenceValue = Spr($"{Ch}/ch_star_silver.png");
            so.FindProperty("_ribbonToday").objectReferenceValue = Spr($"{Ch}/ribbon_today.png");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>빨간 점 위 흰 「!」 — 시안의 새 소식 표시. 점 그림에는 글자가 없다(그림에 글자를 넣지 않는다).</summary>
        private static void Bang(Transform dot, Transform template)
        {
            if (template == null || dot.Find("BangText") != null) return;
            var go = Object.Instantiate(template.gameObject, dot, false);
            go.name = "BangText";
            var loc = go.GetComponent("LocalizedText");
            if (loc != null) Object.DestroyImmediate(loc);
            for (int i = go.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = "!";
            t.enableAutoSizing = false;
            t.fontSize = 17;
            t.fontStyle = FontStyles.Bold;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
            At(t.rectTransform, 0, 1, 20, 22);
        }

        /// <summary>보스 그림은 256 칸 안에 여백이 있다 — 크게 그리고 칸 안쪽(마스크)으로 자른다(시안은 칸에 꽉 찬다).</summary>
        private static void BossMask(Transform box)
        {
            var portrait = box.Find("CHBossPortrait") ?? Find(box, "CHBossPortrait");
            if (portrait == null) return;
            var mask = box.Find("CHBossMask");
            if (mask == null)
            {
                var go = new GameObject("CHBossMask", typeof(RectTransform), typeof(RectMask2D));
                go.layer = box.gameObject.layer;
                mask = go.transform;
                mask.SetParent(box, false);
            }
            mask.SetSiblingIndex(box.Find("CHSecBossFrame").GetSiblingIndex() + 1);
            C(box, "CHBossMask", 103.5f, 390, 128, 79);
            portrait.SetParent(mask, false);
            if (portrait is RectTransform pr) At(pr, 0, -6, 150, 116);
        }

        /// <summary>스킬 칸 — 시안은 큰 아이콘 + 이름 + 두 줄 설명(183 × 80). 알약 그림을 9분할로 늘린다.</summary>
        private static void SkillBox(Transform box, string node, float cx, float cy)
        {
            if (box.Find(node) is not RectTransform r) return;
            r.localScale = Vector3.one;
            C(box, node, cx, cy, 186, 78);
            if (r.GetComponent<Image>() is Image img)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 2f;
            }
            string k = node.EndsWith("A") ? "A" : "P";
            if (r.Find($"CHInfoSkill{k}Icon") is RectTransform icon) At(icon, -60, 0, 52, 52);
            if (r.Find($"CHInfoSkill{k}Name")?.GetComponent<TMP_Text>() is TMP_Text n)
            {
                n.rectTransform.pivot = new Vector2(0f, 0.5f);
                At(n.rectTransform, -30, 20, 116, 22);
                n.rectTransform.anchoredPosition = new Vector2(-30, 20);
                n.fontSize = 15;
                n.alignment = TextAlignmentOptions.Left;
                n.enableAutoSizing = true;
                n.fontSizeMin = 9;
                n.fontSizeMax = 15;
            }
            if (r.Find($"CHInfoSkill{k}Desc")?.GetComponent<TMP_Text>() is TMP_Text d)
            {
                d.rectTransform.pivot = new Vector2(0f, 0.5f);
                At(d.rectTransform, -30, -10, 116, 40);
                d.rectTransform.anchoredPosition = new Vector2(-30, -10);
                d.fontSize = 12;
                d.alignment = TextAlignmentOptions.TopLeft;
                d.enableAutoSizing = true;
                d.fontSizeMin = 8;
                d.fontSizeMax = 12;
            }
        }

        /// <summary>외곽선 · 그림자 없는 맨 글자 — 노란 버튼 위 짙은 글자(외곽선이 있으면 회색으로 뭉갠다).</summary>
        private static void Plain(TMP_Text t)
        {
            if (t.GetComponent<Game.Module.Common.HeavyText>() is Game.Module.Common.HeavyText h) h.Set(0.1f, 0f, new Color32(0, 0, 0, 0));
            if (t.font != null && t.font.material != null) t.fontSharedMaterial = t.font.material;
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
