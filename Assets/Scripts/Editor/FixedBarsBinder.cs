using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 로비 · 육성 · 챕터 선택의 **위 막대 · 아래 바 고정** (PD 확정 2026-10-06, 시안 `_exchange/ref/lobby_profile/fixed_*.png`).
    ///
    ///   위 막대 `FixedTopBar` — 유령 프로필 · 금화 · 다이아 · 우편 · 설정. 세 화면이 **같은 노드**를 본다(로비 루트에 하나).
    ///   아래 바 `BottomNav` — HOST · PLAY · SHOP 과 쪽 표시 점 셋. 고른 칸의 점에 불이 들어온다.
    ///   육성 — 시안은 지금 판을 58px 내리고 세로로 2.5% 줄인 것이다(가로는 그대로, 실측). 판을 통째로 옮긴다.
    ///   챕터 선택 — 시안이 칸마다 따로 움직였다. 빈 판을 새로 받아 깔고 칸 자리를 시안에서 잰 값으로 옮긴다.
    ///
    /// 좌표는 전부 **시안 화면 720 x 1280** 기준.
    /// ⚠ 챕터 창의 상성 아이콘(CHKindGem* · CardKindGem 등)은 빌더에 없는 노드라, 챕터 창을 빌더로 다시 세우면 사라진다 —
    ///   그래서 이 도구는 다시 세우지 않고 **있는 노드를 옮긴다.**
    /// </summary>
    public static class FixedBarsBinder
    {
        private const string Prefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Lobby = "Assets/BaseResource/LobbyV4";
        private const string Ch = "Assets/BaseResource/ChapterHost";

        /// <summary>시안 실측 — 위 막대 높이 · 아래 바 위 끝.</summary>
        public const float TopBarH = 120f;
        public const float NavTop = 1170f;

        [MenuItem("Tools/Game/로비 · 육성 · 챕터 — 위 · 아래 고정 꽂기")]
        public static void Run()
        {
            foreach (var p in new[] { $"{Lobby}/fixed_top_band.png", $"{Lobby}/profile_plate_fx.png", $"{Lobby}/band_edge_l.png", $"{Lobby}/band_edge_r.png",
                                      $"{Lobby}/nav_dot_on.png", $"{Lobby}/nav_dot_off.png",
                                      $"{Ch}/ch_screen_fixed.png", $"{Ch}/ch_card_frame_fx.png", $"{Ch}/ch_card_frame_sel_fx.png",
                                      $"{Ch}/ch_check_fx.png", $"{Ch}/ch_icon_dice_fx.png" })
                EnsureSprite(p);
            Border($"{Ch}/ch_card_frame_fx.png", 28);
            Border($"{Ch}/ch_card_frame_sel_fx.png", 36);

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                TopBar(root.transform);
                Nav(root.transform);
                Growth(root.transform);
                Chapter(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("[FixedBars] 꽂기 끝");
        }

        // ── 위 막대 ───────────────────────────────────────────────

        private static void TopBar(Transform root)
        {
            var bar = root.Find("FixedTopBar") as RectTransform;
            if (bar == null)
            {
                var go = new GameObject("FixedTopBar", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                bar = (RectTransform)go.transform;
            }
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(720f, TopBarH);
            var img = bar.GetComponent<Image>();
            img.sprite = Spr($"{Lobby}/fixed_top_band.png");
            img.raycastTarget = true;   // 막대 아래 화면이 눌리지 않게

            // 4:3 처럼 넓은 화면 — 막대 가장자리 한 줄을 옆으로 늘려 잇는다(밤하늘 띠라 늘려도 티가 안 난다)
            Edge(bar, "FixedTopBarEdgeL", $"{Lobby}/band_edge_l.png", left: true);
            Edge(bar, "FixedTopBarEdgeR", $"{Lobby}/band_edge_r.png", left: false);

            // 로비 판에 있던 값 · 단추를 막대로 옮긴다 — 같은 이름이라 로비 코드가 그대로 채운다
            var top = root.Find("LobbyV4Top");
            Move(top, bar, "LobbyProfile", 0, 0, 250, 120);
            Move(top, bar, "GoldText", 293, 20, 96, 42);
            Move(top, bar, "GemText", 490, 20, 70, 42);
            Move(top, bar, "GoldPlusButton", 389, 24, 36, 34);
            Move(top, bar, "GemPlusButton", 560, 24, 36, 34);
            Move(top, bar, "MailButton", 612, 8, 50, 56);
            Move(top, bar, "SettingsButton", 668, 18, 44, 46);
            Profile(bar);
            bar.SetAsLastSibling();
            // 창들(상자 보상)은 막대보다 위
            var chest = root.Find("ChestRewardPopup");
            if (chest != null) chest.SetAsLastSibling();
        }

        /// <summary>경험치 채움의 가득 찬 폭(시안 홈 안 x 108~225) — `LobbyMainUI.ProfileFillWidth` 와 같아야 한다.</summary>
        public const float ProfileFillWidth = 117f;

        /// <summary>
        /// 유령 프로필 — 시안 왼쪽 위 250 x 120 을 그대로 판으로 받았다(`profile_plate_fx.png`, 1.5배).
        /// 안쪽 칸은 시안에서 잰 값. 글자는 금화 수치와 같은 굵은 고딕(시안이 그렇다 — 픽셀 글꼴 아님).
        /// </summary>
        private static void Profile(RectTransform bar)
        {
            var plate = bar.Find("LobbyProfile");
            if (plate == null) return;
            plate.GetComponent<Image>().sprite = Spr($"{Lobby}/profile_plate_fx.png");
            void At(string n, float x0, float y0, float x1, float y1)
            {
                if (plate.Find(n) is not RectTransform r) { Debug.LogWarning($"[FixedBars] 노드 없음: {n}"); return; }
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
                r.anchoredPosition = new Vector2(x0, -y0);
                r.sizeDelta = new Vector2(x1 - x0, y1 - y0);
            }
            At("ProfileGhostFace", 16, 22, 96, 100);
            At("ProfileExpFill", 108, 64, 108 + ProfileFillWidth, 74);
            // ⚠ 화면 맞춤(ScreenFit)이 pivot 을 가운데로 바꾸면 채움이 가운데서 줄어든다 — 왼쪽 끝에 묶어 둔다
            if (plate.Find("ProfileExpFill") is Transform fill && fill.GetComponent<Game.Module.Common.UI.ScreenFitLock>() == null)
                fill.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();
            At("ProfileLevelText", 149, 28, 223, 52);
            At("ProfileExpText", 104, 82, 230, 108);

            var goldNode = bar.Find("GoldText");
            var gold = goldNode != null ? goldNode.GetComponent<TMP_Text>() : null;
            foreach (var (n, size) in new[] { ("ProfileLevelText", 21f), ("ProfileExpText", 23f) })
            {
                var node = plate.Find(n);
                if (node == null || !node.TryGetComponent<TMP_Text>(out var t)) continue;
                if (gold != null) { t.font = gold.font; t.fontSharedMaterial = gold.fontSharedMaterial; }
                t.characterSpacing = 0f;
                t.fontStyle = FontStyles.Bold;
                Size(t, size);
            }
        }

        private static void Edge(RectTransform bar, string name, string sprite, bool left)
        {
            var t = bar.Find(name) as RectTransform;
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(bar, false);
                t = (RectTransform)go.transform;
            }
            t.SetAsFirstSibling();
            t.anchorMin = t.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            t.pivot = new Vector2(left ? 1f : 0f, 1f);
            t.anchoredPosition = Vector2.zero;
            t.sizeDelta = new Vector2(400f, TopBarH);
            var i = t.GetComponent<Image>();
            i.sprite = Spr(sprite);
            i.raycastTarget = false;
        }

        private static void Move(Transform from, RectTransform to, string name, float x, float y, float w, float h)
        {
            // 로비 빌더를 다시 돌리면 로비 판에 새로 생긴다 — 막대에 있던 옛것은 버리고 새것을 옮긴다
            var fresh = from != null ? from.Find(name) : null;
            var placed = to.Find(name);
            if (fresh != null && placed != null) Object.DestroyImmediate(placed.gameObject);
            var t = fresh != null ? fresh : placed;
            if (t is not RectTransform rt) { Debug.LogWarning($"[FixedBars] 노드 없음: {name}"); return; }
            rt.SetParent(to, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            rt.localScale = Vector3.one;
        }

        // ── 아래 바 · 점 ──────────────────────────────────────────

        private static void Nav(Transform root)
        {
            var nav = root.Find("BottomNav") as RectTransform;
            if (nav == null) return;
            // 켜짐 · 꺼짐 그림이 같은 틀이 됐다 — 같은 자리 · 같은 크기, 비율 지키기
            foreach (var b in new[] { "HostButton", "ChapterButton", "ShopButton" })
            {
                var btn = nav.Find(b);
                if (btn == null) continue;
                foreach (var img in btn.GetComponentsInChildren<Image>(true))
                    if (img.transform != btn && img.sprite != null && img.sprite.name.StartsWith("nav_"))
                    {
                        var r = img.rectTransform;
                        r.anchorMin = Vector2.zero;
                        r.anchorMax = Vector2.one;
                        r.offsetMin = r.offsetMax = Vector2.zero;
                        img.preserveAspect = true;
                    }
            }

            var dots = nav.Find("NavDots") as RectTransform;
            if (dots == null)
            {
                var go = new GameObject("NavDots", typeof(RectTransform));
                go.transform.SetParent(nav, false);
                dots = (RectTransform)go.transform;
            }
            // 시안(로비) : 가운데 360 · 30 간격 · 지름 14. 로비 시안은 y 1118 이지만 챕터 화면에선 「도전하기」와 겹친다 —
            //   세 화면이 같은 자리를 써야 하므로 아래 바 바로 위로 둔다
            dots.anchorMin = dots.anchorMax = new Vector2(0.5f, 1f);
            dots.pivot = new Vector2(0.5f, 0f);
            dots.anchoredPosition = Vector2.zero;   // 점 가운데 y 1163 — 세 화면 모두 비는 줄(챕터는 1150 까지 「도전하기」)
            dots.sizeDelta = new Vector2(90f, 14f);   // 점 줄 높이(보이는 지름)
            for (int i = 0; i < 3; i++)
            {
                var t = dots.Find($"NavDot{i}") as RectTransform;
                if (t == null)
                {
                    var go = new GameObject($"NavDot{i}", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(dots, false);
                    t = (RectTransform)go.transform;
                }
                t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0.5f);
                t.anchoredPosition = new Vector2((i - 1) * 30f, 0f);
                t.sizeDelta = new Vector2(21.4f, 21.4f);   // 그림 26 칸 중 점은 17 — 보이는 점이 시안 지름 14 가 되게
                var img = t.GetComponent<Image>();
                img.sprite = Spr(i == 1 ? $"{Lobby}/nav_dot_on.png" : $"{Lobby}/nav_dot_off.png");
                img.raycastTarget = false;
            }
            var ui = root.GetComponent<Game.Module.Lobby.LobbyMainUI>();
            if (ui != null)
            {
                var so = new SerializedObject(ui);
                so.FindProperty("_navDotOn").objectReferenceValue = Spr($"{Lobby}/nav_dot_on.png");
                so.FindProperty("_navDotOff").objectReferenceValue = Spr($"{Lobby}/nav_dot_off.png");
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── 육성 ─────────────────────────────────────────────────

        /// <summary>시안 실측 — 지금 판의 y 234 → 286 · 979 → 1012 (가로 테두리 줄). y' = 58 + 0.9745 y.</summary>
        public const float GrowthShift = 58f;
        public const float GrowthScaleY = 0.9745f;

        private static void Growth(Transform root)
        {
            var page = root.Find("GrowthPanel/Page") as RectTransform;
            if (page == null) return;
            page.anchoredPosition = new Vector2(page.anchoredPosition.x, -GrowthShift);
            page.localScale = new Vector3(1f, GrowthScaleY, 1f);
            // 육성 판의 위 띠(로고 · 금화 · 우편 · 설정)는 공통 위 막대가 맡는다
            foreach (var n in new[] { "GrowthGoldPlusButton", "GrowthMailButton", "GrowthSettingsButton", "GrowthGoldText" })
                if (Find(page, n) is Transform t) t.gameObject.SetActive(false);
            // 목록 카드 이름 — 「호퍼(기관단총)」 처럼 긴 이름이 카드 밖으로 나갔다. 칸 안에서 줄인다
            foreach (var tmp in page.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.name != "H5CardName") continue;
                if (!tmp.enableAutoSizing) tmp.fontSizeMax = tmp.fontSize;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = Mathf.Min(tmp.fontSizeMax, 9f);
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        // ── 챕터 선택 ─────────────────────────────────────────────

        private static void Chapter(Transform root)
        {
            var panel = root.Find("ChapterHostPanel");
            var box = panel != null ? panel.Find("CHContent") : null;
            if (box == null) return;
            // 창은 아래 바 · 위 막대 **밑에** — 둘 다 보여야 한다
            // ⚠ 이미 아래 바 밑이면 건드리지 않는다 — 바로 앞에 있을 때 nav 자리로 옮기면 nav **뒤로** 넘어간다(코덱스 검토)
            var nav = root.Find("BottomNav");
            if (nav != null && panel.GetSiblingIndex() > nav.GetSiblingIndex()) panel.SetSiblingIndex(nav.GetSiblingIndex());

            foreach (var n in new[] { "CHIconBack", "CHIconGem", "CHIconGold", "CHIconMail", "CHIconGear", "CHBackButton",
                                      "CHMailButton", "CHSettingsButton", "CHGemText", "CHGoldText",
                                      "CHBossBox", "CHRewardBox", "CHIconDot", "CHHostNote" })
                if (box.Find(n) is Transform t) t.gameObject.SetActive(false);

            // 판 — 위 막대 · 아래 바 사이(시안 y 120 ~ 1170)를 그린 것. 키 큰 폰용으로 위아래가 늘어난 판이면 그만큼 크다.
            if (box.Find("CHScreen") is not Transform screenNode) { Debug.LogWarning("[FixedBars] 노드 없음: CHScreen"); return; }
            var screen = screenNode.GetComponent<Image>();
            screen.sprite = Spr($"{Ch}/ch_screen_fixed.png");
            float aspect = screen.sprite != null ? screen.sprite.rect.height / screen.sprite.rect.width : 1050f / 720f;
            C(box, "CHScreen", 360, 645, 720, 720f * aspect);

            // 챕터 큰 판 — 그림은 판 안쪽(x 28~692 · y 136~409)
            C(box, "CHChapterArtBox", 360, 272.5f, 664, 273);
            if (box.Find("CHChapterArtBox/CHChapterDim") is RectTransform dim)
            {
                dim.sizeDelta = new Vector2(664, 273);
                dim.GetComponent<Image>().sprite = Spr($"{Ch}/ch_dim.png");   // 왼쪽만 어둡게 — 시안에는 아래 두 칸이 판 밖이다
            }
            if (box.Find("CHChapterArtBox/CHChapterArt") is RectTransform art) art.sizeDelta = new Vector2(664, 273);
            C(box, "CHPrevArrow", 66.5f, 181, 21, 36);
            C(box, "CHNextArrow", 269, 181, 22, 36);
            C(box, "CHPrevButton", 66.5f, 181, 48, 48);
            C(box, "CHNextButton", 269, 181, 48, 48);
            L(box, "CHChapterLabel", 91, 184.5f, 190, 50, 24f);   // 번호는 같은 칸 <size=128%> — 판 코드
            if (box.Find("CHChapterLabel") is Transform cl && cl.TryGetComponent<TMP_Text>(out var clt))
                clt.enableAutoSizing = false;   // 번호가 커서 칸 높이를 넘으면 자동 크기가 「CHAPTER」까지 줄인다
            L(box, "CHChapterNameText", 57, 247.5f, 340, 58, 47.5f);
            L(box, "CHChapterDescText", 56, 327.5f, 330, 58, 21.5f);
            C(box, "CHLockIcon", 500, 272, 100, 120);
            C(box, "CHKindGem0", 68, 387, 28, 28);
            C(box, "CHKindGem1", 152, 387, 28, 28);
            C(box, "CHKindGem2", 229, 387, 28, 28);
            L(box, "CHKindCount0", 87, 387, 44, 28, 24f);
            L(box, "CHKindCount1", 169, 387, 44, 28, 24f);
            L(box, "CHKindCount2", 248, 387, 44, 28, 24f);

            // 보스 · 보상 줄
            C(box, "CHBossPortrait", 101, 498, 100, 88);
            C(box, "CHBossKindGem", 151, 461, 25, 25);
            C(box, "CHIconSkull", 196, 482.5f, 33, 39);
            L(box, "CHBossLabel", 222, 473, 80, 28, 23.5f);
            L(box, "CHBossNameText", 180, 524, 128, 22, 16f);
            BossDash(box);
            L(box, "CHRewardLabel", 337, 461, 150, 32, 25f);
            C(box, "CHIconCoin", 355, 514, 44, 44);
            L(box, "CHRewardGoldText", 385, 516, 130, 30, 24f);
            C(box, "CHRewardChestIcon", 560, 516, 84, 74);
            L(box, "CHRewardChestText", 610, 516, 80, 26, 18.4f);

            // 호스트 줄
            L(box, "CHHostTitle", 70, 614, 200, 34, 25.8f);
            C(box, "CHHostViewport", 362, 757, 630, 225);
            if (box.Find("CHHostViewport/CHHostContent") is RectTransform content)
                content.sizeDelta = new Vector2(content.sizeDelta.x, 225);
            C(box, "CHHostPrev", 22, 757, 30, 100);
            C(box, "CHHostNext", 698, 757, 30, 100);
            var cards = box.Find("CHHostViewport/CHHostContent");
            for (int i = 0; cards != null && i < cards.childCount; i++) Card(cards.GetChild(i));

            // 랜덤 · 도전
            C(box, "CHRandomCard", 358, 972, 667, 155);
            C(box, "CHIconDice", 121, 971, 122, 122);
            if (box.Find("CHIconDice") is Transform dice) dice.GetComponent<Image>().sprite = Spr($"{Ch}/ch_icon_dice_fx.png");
            L(box, "CHRandomTitle", 227, 936, 220, 32, 25f);
            L(box, "CHRandomDesc1", 227, 974.5f, 450, 28, 21f);
            L(box, "CHRandomDesc2", 227, 1006, 450, 28, 21f);
            if (box.Find("CHRandomCard/RandomCheck") is RectTransform rc)
            {
                rc.anchorMin = rc.anchorMax = rc.pivot = new Vector2(0.5f, 0.5f);
                rc.anchoredPosition = new Vector2(648 - 358, 972 - 908);
                rc.sizeDelta = new Vector2(35, 28);
                rc.GetComponent<Image>().sprite = Spr($"{Ch}/ch_check_fx.png");
            }
            C(box, "CHStartButton", 359, 1105, 346, 89);
            if (box.Find("CHStartButton/CHStartCostIcon") is RectTransform ci)
            {
                ci.anchorMin = ci.anchorMax = ci.pivot = new Vector2(0.5f, 0.5f);
                ci.anchoredPosition = new Vector2(-128, 0);
                ci.sizeDelta = new Vector2(36, 36);
            }
            if (box.Find("CHStartButton/CHStartCostText") is RectTransform ct)
            {
                ct.anchorMin = ct.anchorMax = new Vector2(0.5f, 0.5f);
                ct.pivot = new Vector2(0f, 0.5f);
                ct.anchoredPosition = new Vector2(-106, 0);
                ct.sizeDelta = new Vector2(70, 36);
            }
            L(box, "CHStartLabel", 297, 1104.5f, 170, 42, 32f);

            var panelComp = panel.GetComponent<Game.Module.Lobby.ChapterHostPanel>();
            if (panelComp != null)
            {
                var so = new SerializedObject(panelComp);
                so.FindProperty("_cardFrame").objectReferenceValue = Spr($"{Ch}/ch_card_frame_fx.png");
                so.FindProperty("_cardFrameSelected").objectReferenceValue = Spr($"{Ch}/ch_card_frame_sel_fx.png");
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>카드 한 장 — 카드 가운데 기준, 시안 실측(갱스터 카드 가운데 230,757 · 113 x 225).</summary>
        private static void Card(Transform card)
        {
            if (card is RectTransform cr) cr.sizeDelta = new Vector2(113, 225);
            void P(string n, float x, float y, float w, float h, float px = 0.5f)
            {
                if (card.Find(n) is not RectTransform r) return;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(px, 0.5f);
                r.anchoredPosition = new Vector2(x, y);
                r.sizeDelta = new Vector2(w, h);
            }
            P("CardFrame", 0, 0, 113, 225);
            P("CardFrameSel", 0, 0, 113, 225);
            // 새 틀은 속이 채워져 있다 — 고른 틀이 위에 있으면 얼굴 · 이름을 덮는다. 보통 틀 바로 위로 내린다
            if (card.Find("CardFrameSel") is Transform under) under.SetSiblingIndex(1);
            P("CardThumb", 0, 29, 100, 100);
            P("CardName", 0, -42, 109, 26);
            for (int s = 0; s < 5; s++) P($"CardStar{s}", -40 + s * 20, -68, 19, 19);
            P("CardPowerIcon", -40, -92, 20, 20);
            P("CardPowerText", -20, -92, 76, 26, 0f);
            P("CardCheck", 38, 96, 35, 28);
            P("CardKindGem", -41, 97, 25, 25);
            P("CardMatchArrow", -14, 96, 20, 20);
            // 틀은 카드보다 덜 길쭉하다(203 x 350 vs 113 x 225) — 늘리면 모서리가 찌그러져 9 조각으로 편다
            if (card.Find("CardFrame") is Transform f) Sliced(f.GetComponent<Image>(), $"{Ch}/ch_card_frame_fx.png", 203f / 113f);
            if (card.Find("CardFrameSel") is Transform fs) Sliced(fs.GetComponent<Image>(), $"{Ch}/ch_card_frame_sel_fx.png", 220f / 113f);
            if (card.Find("CardCheck") is Transform ck) ck.GetComponent<Image>().sprite = Spr($"{Ch}/ch_check_fx.png");
            foreach (var (n, size) in new[] { ("CardName", 21f), ("CardPowerText", 21f) })
                if (card.Find(n) is Transform t && t.TryGetComponent<TMP_Text>(out var tmp)) Size(tmp, size);
        }

        /// <summary>
        /// 시안의 「BOSS」 밑 짧은 점선(x 222~270 · y 497, 9px 넷). 그림이 아니라 글자(하이픈)로 둔다 —
        /// 「BOSS」 와 같은 픽셀 폰트라 같은 결로 보인다. 고정 글자라 번역하지 않는다.
        /// </summary>
        private static void BossDash(Transform box)
        {
            var label = box.Find("CHBossLabel");
            if (label == null) return;
            var dash = box.Find("CHBossDash");
            if (dash == null)
            {
                dash = Object.Instantiate(label.gameObject, box).transform;
                dash.name = "CHBossDash";
                foreach (var c in dash.GetComponents<MonoBehaviour>())
                    if (c is not TMP_Text) Object.DestroyImmediate(c);
            }
            dash.SetSiblingIndex(label.GetSiblingIndex() + 1);
            var tmp = dash.GetComponent<TMP_Text>();
            tmp.text = "- - - -";
            tmp.color = new Color32(179, 197, 225, 255);
            tmp.enableAutoSizing = false;
            L(box, "CHBossDash", 222, 497.5f, 60, 16, 16f);
        }

        private static void Sliced(Image img, string path, float multiplier)
        {
            img.sprite = Spr(path);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = multiplier;
            img.preserveAspect = false;
        }

        private static void Border(string path, float px)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
            var b = new Vector4(px, px, px, px);
            if (ti.spriteBorder == b) return;
            ti.spriteBorder = b;
            ti.SaveAndReimport();
        }

        /// <summary>가운데 기준 자리(시안 화면 좌표).</summary>
        private static void C(Transform box, string n, float x, float y, float w, float h)
        {
            if (box.Find(n) is not RectTransform r) { Debug.LogWarning($"[FixedBars] 노드 없음: {n}"); return; }
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x - 360f + (r.pivot.x - 0.5f) * w, 640f - y);
            r.sizeDelta = new Vector2(w, h);
        }

        /// <summary>왼쪽 끝 기준 글자 칸(시안 화면 좌표) — 왼쪽 끝 x, 가운데 y.</summary>
        private static void L(Transform box, string n, float x, float y, float w, float h, float size)
        {
            if (box.Find(n) is not RectTransform r) { Debug.LogWarning($"[FixedBars] 노드 없음: {n}"); return; }
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(x - 360f, 640f - y);
            r.sizeDelta = new Vector2(w, h);
            if (r.TryGetComponent<TMP_Text>(out var tmp)) Size(tmp, size);
        }

        private static void Size(TMP_Text tmp, float size)
        {
            tmp.fontSize = size;
            if (tmp.enableAutoSizing) { tmp.fontSizeMax = size; tmp.fontSizeMin = Mathf.Min(tmp.fontSizeMin, size * 0.6f); }
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

        private static void EnsureSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
            if (ti.textureType == TextureImporterType.Sprite && !ti.mipmapEnabled
                && ti.textureCompression == TextureImporterCompression.Uncompressed && ti.maxTextureSize >= 2048) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
        }
    }
}
