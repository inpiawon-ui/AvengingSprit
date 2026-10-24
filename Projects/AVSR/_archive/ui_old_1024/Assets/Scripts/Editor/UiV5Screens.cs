using System.Collections.Generic;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.LobbyShopBuilder;
using static Game.Editor.LobbyHomeV5Builder;

namespace Game.Editor
{
    /// <summary>
    /// UI v5(혼합 컨셉 「픽셀 연대기 × 쇼케이스」, PD 2026-10-17 · 18) — 홈 다음 화면들을 시안대로 다시 꽂는다.
    /// 기능 노드 · 이름(= 런타임 바인딩 키)은 그대로 두고 **자리 · 크기 · 판 그림 · 글자**만 바꾼다.
    /// 시안은 `Projects/AVSR/_exchange/in/mock_uiv3~5_*.png`. 시안(720 폭 환산)의 세로는 위 막대(시안 70 → 게임 120) ·
    /// 아래 바(시안 1190 → 게임 1170) 사이를 0.9375 배로 옮긴다(<see cref="Gy"/>).
    /// 부품: 키트 `Assets/BaseResource/UiKit/` · 그림 `Assets/BaseResource/HomeV5Art/`.
    /// 순서: 다른 로비 빌더 · 꽂기 다음, 「홈 v5」 · 「아래 바 v5」 다음에 돌린다.
    /// </summary>
    internal static class UiV5Ext
    {
        internal static void Let<T>(this T v, System.Action<T> f) => f(v);
    }

    public static class UiV5Screens
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Kit = "Assets/BaseResource/UiKit";
        private const string Art = "Assets/BaseResource/HomeV5Art";
        private const string Ch = "Assets/BaseResource/ChapterHost";
        private const string Gr = "Assets/BaseResource/Growth";

        internal static readonly Color Ink = new(0.27f, 0.17f, 0.09f, 1f);
        internal static readonly Color InkSoft = new(0.42f, 0.31f, 0.2f, 1f);
        internal static readonly Color Gold = new(1f, 0.85f, 0.32f, 1f);
        internal static readonly Color ChapterRed = new(0.66f, 0.14f, 0.08f, 1f);

        /// <summary>시안(720 폭으로 줄인 값) 세로 → 게임 세로.</summary>
        internal static float Gy(float y) => 120f + (y - 70f) * 0.9375f;

        // ── 위 막대 ──────────────────────────────────────────

        /// <summary>경험치 채움의 가득 찬 폭 — `LobbyMainUI.ProfileFillWidth` 와 같아야 한다.</summary>
        public const float ProfileFillWidth = 134f;

        [MenuItem("Tools/Game/UI v5 — 위 막대 (mock_uiv5_home)")]
        public static void RunTopBar()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[UI v5] 글자 본이 없다"); return; }
                var bar = Find(lobby.transform, "FixedTopBar") as RectTransform;
                if (bar == null) { Debug.LogError("[UI v5] FixedTopBar 없음"); return; }
                TopBar(bar);
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[UI v5] 위 막대");
        }

        /// <summary>
        /// 위 막대(시안 홈 · 출전 위 막대) — 그림 한 장에 칸이 박혀 있던 것을 키트 판 + 아이콘으로 다시 짠다.
        /// 프로필(유령 · 「유령 Lv N」 · 경험치 막대 · 340 / 640) · 행동력 · 골드 · 젬 · 우편 · 설정. 판 높이 92, 그 아래 28 은 비운다(바탕).
        /// </summary>
        private static void TopBar(RectTransform bar)
        {
            var band = bar.GetComponent<Image>();
            band.sprite = Spr($"{Art}/top_band.png");
            band.type = Image.Type.Simple;
            band.preserveAspect = false;
            foreach (var n in new[] { "FixedTopBarEdgeL", "FixedTopBarEdgeR" })
                if (bar.Find(n)?.GetComponent<Image>() is Image edge)
                {
                    edge.sprite = Spr($"{Art}/top_band_side.png");
                    edge.preserveAspect = false;
                }
            // 금테 띠 — 화면 양끝 너머까지 늘려 옆 테가 안 보이게(4:3 에서 막대가 커져도)
            foreach (var old in new[] { "TopV5Plate" })
                if (bar.Find(old) is Transform t) Object.DestroyImmediate(t.gameObject);
            var plate = Sliced(bar, "TopV5Plate", $"{Kit}/panel_navy_big.png", 0, 0, 0, 0, 2.2f);
            TL(plate.rectTransform, -420f, -6f, 1560f, 98f);
            plate.transform.SetSiblingIndex(2);   // 띠 · 옆 이음 다음, 칸들 아래

            // 프로필
            var prof = bar.Find("LobbyProfile") as RectTransform;
            if (prof != null)
            {
                var pimg = prof.GetComponent<Image>();
                if (pimg != null) { pimg.sprite = Spr($"{Kit}/panel_navy_mid.png"); pimg.type = Image.Type.Sliced; pimg.pixelsPerUnitMultiplier = 2.6f; pimg.color = Color.white; }
                TL(prof, 8f, 8f, 232f, 78f);
                if (prof.Find("ProfileGhostFace") is RectTransform face) TL(face, 6f, 6f, 66f, 66f);
                if (prof.Find("ProfileLevelText")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI lv)
                {
                    TL(lv.rectTransform, 78f, 6f, 148f, 30f);
                    lv.fontSize = lv.fontSizeMax = 22f;
                    lv.alignment = TextAlignmentOptions.Left;
                    lv.color = Color.white;
                }
                var expBg = prof.Find("TopV5ExpBg") as RectTransform;
                if (expBg == null) expBg = Sliced(prof, "TopV5ExpBg", $"{Kit}/bar_bg.png", 0, 0, 0, 0, 3f).rectTransform;
                TL(expBg, 78f, 40f, ProfileFillWidth + 6f, 14f);
                expBg.SetSiblingIndex(1);
                if (prof.Find("ProfileExpFill")?.GetComponent<Image>() is Image fill)
                {
                    fill.sprite = Spr($"{Kit}/bar_fill.png");
                    fill.type = Image.Type.Sliced;
                    fill.pixelsPerUnitMultiplier = 3f;
                    fill.preserveAspect = false;
                    TL(fill.rectTransform, 81f, 42f, ProfileFillWidth, 10f);
                    fill.transform.SetSiblingIndex(expBg.GetSiblingIndex() + 1);
                }
                if (prof.Find("ProfileExpText")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI ex)
                {
                    TL(ex.rectTransform, 78f, 54f, 146f, 20f);
                    ex.fontSize = ex.fontSizeMax = 15f;
                    ex.alignment = TextAlignmentOptions.Right;
                }
            }

            // 재화 칸 — 판 · 아이콘 · 글자 · 「+」
            Pill(bar, "TopV5Stamina", 248f, 108f, $"{Kit}/icon_stamina.png", "StaminaText", "StaminaPlusButton");
            Pill(bar, "TopV5Gold", 362f, 126f, $"{Ch}/ch_icon_gold.png", "GoldText", "GoldPlusButton");
            Pill(bar, "TopV5Gem", 494f, 116f, $"{Ch}/ch_icon_gem.png", "GemText", "GemPlusButton");
            if (bar.Find("StaminaTimerText")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI timer)
            {
                TL(timer.rectTransform, 278f, 60f, 50f, 16f);
                timer.fontSize = timer.fontSizeMax = 13f;
            }

            Icon(bar, "TopV5Mail", $"{Ch}/ch_icon_mail.png", 616f, 22f, 44f, 40f);
            if (bar.Find("MailButton") is RectTransform mail) TL(mail, 612f, 14f, 52f, 56f);
            Icon(bar, "TopV5Gear", $"{Ch}/ch_icon_gear.png", 668f, 20f, 42f, 44f);
            if (bar.Find("SettingsButton") is RectTransform gear) TL(gear, 664f, 14f, 52f, 56f);
        }

        /// <summary>재화 한 칸 — x 부터 폭 w. 판(남색) · 왼쪽 아이콘 · 숫자 · 오른쪽 「+」 단추.</summary>
        private static void Pill(RectTransform bar, string name, float x, float w, string icon, string textNode, string plusNode)
        {
            if (bar.Find(name) is Transform old) Object.DestroyImmediate(old.gameObject);
            var p = Sliced(bar, name, $"{Kit}/panel_navy_mid.png", 0, 0, 0, 0, 3f);
            TL(p.rectTransform, x, 22f, w, 44f);
            p.transform.SetSiblingIndex(3);
            var ic = Pic(p.transform, name + "Icon", icon, 0, 0, 0, 0);
            TL(ic.rectTransform, 4f, 6f, 30f, 32f);
            if (bar.Find(textNode)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t)
            {
                TL(t.rectTransform, x + 34f, 22f, w - 66f, 44f);
                t.fontSize = t.fontSizeMax = 20f;
                t.fontSizeMin = 11f;
                t.enableAutoSizing = true;
                t.alignment = TextAlignmentOptions.Center;
                t.color = Color.white;
            }
            if (bar.Find(plusNode) is RectTransform plus)
            {
                TL(plus, x + w - 32f, 27f, 30f, 34f);
                var img = plus.GetComponent<Image>();
                if (img != null) { img.sprite = Spr($"{Kit}/btn_plus.png"); img.color = Color.white; img.preserveAspect = true; }
            }
        }

        private static void Icon(RectTransform bar, string name, string sprite, float x, float y, float w, float h)
        {
            if (bar.Find(name) is Transform old) Object.DestroyImmediate(old.gameObject);
            var ic = Pic(bar, name, sprite, 0, 0, 0, 0);
            TL(ic.rectTransform, x, y, w, h);
            ic.transform.SetSiblingIndex(3);
        }

        /// <summary>왼쪽 위 기준 자리(부모 왼쪽 위에서 x, y 아래).</summary>
        internal static void TL(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
        }

        // ── 태블릿 4:3 가로 채우기 ─────────────────────────────

        private static readonly string[] WideRoots =
        {
            "ChapterHostPanel/CHContent", "GrowthPanel/HostPage", "SummonPanel/SummonContent", "SeasonPassPanel/PassContent",
            "TowerEntryPanel/TowerContent", "NightEntryPanel/NightContent", "LobbyShopPanel/ShopContent",
            "GrowthPanel/Page",
        };

        /// <summary>
        /// PD 2026-10-18 「4:3 도 다른 화면들 꽉 차게」 — 화면 판을 **가로만** 부모 폭으로 늘리고, 안의 칸을 720 기준 **비율 자리**로 바꾼다.
        /// 5 %~45 % 에 있던 판은 4:3(960 폭)에서도 5 %~45 % — 판 · 카드 · 단추가 고르게 넓어지고, 그림(캐릭터 · 아이콘)은
        /// 제 비율(preserveAspect)을 지켜 찌그러지지 않는다. 세로는 그대로라 20:9 은 지금 배치 그대로다. 9:16 · 20:9 은 폭이 720 이라 같다.
        /// (ScreenFit 의 「90 % 넘는 판만 늘리기」는 판만 늘고 글자 · 카드는 가운데에 남아 반쯤만 찼다.)
        /// 다른 UI v5 메뉴 **뒤**에 돌린다 — 빌더는 시작할 때 판을 720 으로 되돌린다(Narrow).
        /// </summary>
        [MenuItem("Tools/Game/UI v5 — 4:3 가로 채우기")]
        public static void RunWide()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                foreach (var path in WideRoots)
                {
                    if (lobby.transform.Find(path) is not RectTransform root) { Debug.LogWarning($"[UI v5] 없음: {path}"); continue; }
                    if (root.GetComponent<Game.Module.Common.UI.ScreenFit>() is Component sf) Object.DestroyImmediate(sf);
                    if (root.GetComponent<Game.Module.Common.UI.ScreenFitLock>() == null) root.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();
                    Narrow(root);
                    s_rootTop = DesignY(root).top;
                    s_rootBottom = DesignY(root).bottom;
                    Spread(root);
                    KeepAspect(root);
                    // 능력치 2열 카드 — 런타임이 왼쪽 위부터 깐다. 묶음(684 폭)을 넓어진 창 가운데에
                    if (Find(root, "H6StatContent") is RectTransform sc)
                    {
                        float ch = sc.sizeDelta.y;
                        sc.anchorMin = new Vector2(0.5f, 1f); sc.anchorMax = new Vector2(0.5f, 1f); sc.pivot = new Vector2(0.5f, 1f);
                        sc.sizeDelta = new Vector2(684f, ch);
                        sc.anchoredPosition = new Vector2(0f, sc.anchoredPosition.y);
                    }
                    if (Find(root, "StatContent") is RectTransform gc && gc.parent != null && gc.parent.name == "StatViewport")
                    {
                        float gh = gc.sizeDelta.y;
                        gc.anchorMin = new Vector2(0.5f, 1f); gc.anchorMax = new Vector2(0.5f, 1f); gc.pivot = new Vector2(0.5f, 1f);
                        gc.sizeDelta = new Vector2(684f, gh);
                        gc.anchoredPosition = new Vector2(0f, gc.anchoredPosition.y);
                    }
                    // 판을 부모(화면)에 가득 — 가로 · 세로 둘 다. 그린 위 · 아래 변 밖(Page 의 58 · 52)은 그만큼 띄운다
                    var (top, bottom, _) = DesignY(root);
                    root.anchorMin = Vector2.zero;
                    root.anchorMax = Vector2.one;
                    root.pivot = new Vector2(0.5f, 0.5f);
                    root.offsetMin = new Vector2(0f, 1280f - bottom);
                    root.offsetMax = new Vector2(0f, -top);
                }
                // 고정 진열 칸(유령 · 패스 · 밤)에 큰 몸 그림 — 빌더가 칸을 새로 만들면 빠지므로 늘 마지막인 여기서 다시 꽂는다
                UnitLargeImport.AttachFixed(lobby);
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[UI v5] 4:3 가로 채우기");
        }

        /// <summary>
        /// 넓어진 칸 안에서 얼굴 · 아이콘 그림이 늘어나지 않게 — 늘려 쓰는 판(9분할) · 채우기(Filled) · 덮기 그림(Envelope)은 그대로.
        /// </summary>
        private static void KeepAspect(RectTransform root)
        {
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (img.type != Image.Type.Simple || img.sprite == null) continue;
                if (img.GetComponent<AspectRatioFitter>() != null) continue;
                string n = img.name;
                if (n.Contains("Dim") || n.Contains("Mask") || n.Contains("Hit") || n.Contains("Backdrop") || n == "H6Bg" || n == "CHScreen"
                    || n == "CHChapterArt" || n.StartsWith("PathLine")) continue;   // 챕터 그림은 칸을 덮어야 한다(런타임 FillBox)
                img.preserveAspect = true;
            }
        }

        private static bool IsFullY(RectTransform c)
            => c.anchorMin.y == 0f && c.anchorMax.y == 1f && Mathf.Abs(c.offsetMin.y) < 1f && Mathf.Abs(c.offsetMax.y) < 1f;

        /// <summary>판 크기와 같은(꽉 채운) 묶음 — 그 안의 칸도 화면 좌표라 같이 비율로 바꾼다.</summary>
        private static bool IsFullOf(RectTransform c, RectTransform p)
            => c.anchorMin == Vector2.zero && c.anchorMax == Vector2.one
               && Mathf.Abs(c.offsetMin.x) < 1f && Mathf.Abs(c.offsetMax.x) < 1f;

        /// <summary>화면 바탕 — 비율 자리로 바꾸면 옆으로 늘어난다. 덮기(Envelope)로 바꾼다.</summary>
        private static readonly string[] Backdrops = { "CHScreen", "H6Bg", "SummonBackdrop", "PassBackdrop", "ShopBackdrop", "BgGhost" };
        /// <summary>런타임이 다른 글자 끝을 따라 옮기는 칸 — 비율 자리면 옮긴 값이 어긋난다(가운데 기준 고정).</summary>
        private static readonly string[] RuntimeFollowers = { "TowerSweepTitleText", "TowerSweepSubText" };

        // ── 세로(20:9) ── 위 막대(화면 0~120) · 아래 바(1170~1280) 사이 1050 을 화면 높이에 맞춰 늘린다(PD 2026-10-19 「20:9 도 꽉 차게」).
        // 그 사이에 있는 칸은 위에서 잰 비율 자리 + 고정 몫으로 둔다 — 9:16(1280)에서는 그린 자리 그대로다.
        private const float TopBand = 120f, NavBand = 1170f, DesignH = 1280f;
        private static float s_rootTop, s_rootBottom = DesignH;

        /// <summary>그린 화면 세로 D(위에서) → (판 높이 비율 a, 고정 몫 b) — 판 안 위에서 = a·판높이 + b.</summary>
        private static (float a, float b) MapY(float d)
        {
            float cc = s_rootTop + (DesignH - s_rootBottom);
            if (d <= TopBand) return (0f, d - s_rootTop);
            if (d >= NavBand) return (1f, -(s_rootBottom - d));
            float a = (d - TopBand) / (NavBand - TopBand);
            return (a, TopBand - s_rootTop + a * (cc - (TopBand + DesignH - NavBand)));
        }

        /// <summary>판 안의 칸을 세로 비율 자리로. point = 크기는 그대로 가운데만 옮긴다.</summary>
        private static void PropY(RectTransform c, RectTransform p, bool point)
        {
            float top = p.rect.yMax - (c.localPosition.y + c.rect.yMax);
            float h = c.rect.height;
            float d0 = s_rootTop + top, d1 = d0 + h;
            if (point)
            {
                var (a, b) = MapY((d0 + d1) * 0.5f);
                c.anchorMin = new Vector2(c.anchorMin.x, 1f - a);
                c.anchorMax = new Vector2(c.anchorMax.x, 1f - a);
                c.offsetMax = new Vector2(c.offsetMax.x, -b + h * 0.5f);
                c.offsetMin = new Vector2(c.offsetMin.x, -b - h * 0.5f);
                return;
            }
            var (a0, b0) = MapY(d0);
            var (a1, b1) = MapY(d1);
            c.anchorMax = new Vector2(c.anchorMax.x, 1f - a0);
            c.anchorMin = new Vector2(c.anchorMin.x, 1f - a1);
            c.offsetMax = new Vector2(c.offsetMax.x, -b0);
            c.offsetMin = new Vector2(c.offsetMin.x, -b1);
        }

        private static void Spread(RectTransform parent, bool doY = true)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i) is not RectTransform c) continue;
                if (System.Array.IndexOf(Backdrops, c.name) >= 0)
                {
                    // 판(1280)보다 크게 — 긴 폰(20:9, 보이는 높이 1600)에서도 판 위아래 틈을 덮어야 뒤의 홈 화면이 비치지 않는다.
                    // 높이 2200 고정 · 폭은 그림 비율대로(1237) — 4:3 폭 960 도 덮는다
                    c.anchorMin = c.anchorMax = c.pivot = new Vector2(0.5f, 0.5f);
                    c.anchoredPosition = Vector2.zero;
                    c.sizeDelta = new Vector2(1240f, 2200f);
                    var f = c.GetComponent<AspectRatioFitter>();
                    if (f == null) f = c.gameObject.AddComponent<AspectRatioFitter>();
                    f.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                    f.aspectRatio = 1080f / 1920f;
                    continue;
                }
                if (System.Array.IndexOf(RuntimeFollowers, c.name) >= 0) { if (doY) PropY(c, parent, true); CenterX(c, parent); continue; }
                // 유령 성장 경로 — 런타임이 칸 x 로 표시 · 줄을 놓는다(같은 기준이어야 한다)
                if (parent.name == "GhostPath" && (c.name.StartsWith("PathLine") || c.name.StartsWith("PathNode")
                    || c.name.StartsWith("PathLabel") || c.name.StartsWith("PathMarker") || c.name.StartsWith("PathPill"))) { if (doY) PropY(c, parent, true); CenterX(c, parent); continue; }
                // 얼굴 칸 — 자리만 비율로 옮기고 크기는 그대로(넓히면 얼굴이 옆으로 늘어난다)
                if (c.name.Contains("Face") || c.name.StartsWith("NightHostSlot") || c.name.StartsWith("TowerLadderCell"))
                { if (doY) PropY(c, parent, true); PropPoint(c, parent); continue; }
                if (IsFullOf(c, parent) && IsFullY(c)) { Spread(c, doY); continue; }
                if (c.name.StartsWith("H6V5Ladder")) { CenterX(c, parent); continue; }   // 사다리는 덩어리째 가운데(런타임이 테를 별 자리로 옮긴다)
                if (doY) PropY(c, parent, false);
                Prop(c, parent);
                // 성급 탭 창 · 그 아래 묶음은 안의 칸도 화면처럼 놓여 있다(빌더가 왼쪽 위 기준으로 놓았다) — 세로는 창 위에서 고정
                if (c.name == "H6Window" || c.name == "H6WinFoot") Spread(c, false);
            }
        }

        /// <summary>가로 자리를 부모 폭에 대한 비율 앵커로(세로는 그대로).</summary>
        private static void Prop(RectTransform c, RectTransform p)
        {
            float pw = p.rect.width;
            if (pw <= 1f) return;
            float left = c.localPosition.x + c.rect.xMin - p.rect.xMin;
            float right = left + c.rect.width;
            Vector2 aMin = c.anchorMin, aMax = c.anchorMax, oMin = c.offsetMin, oMax = c.offsetMax;
            c.anchorMin = new Vector2(left / pw, aMin.y);
            c.anchorMax = new Vector2(right / pw, aMax.y);
            c.offsetMin = new Vector2(0f, oMin.y);
            c.offsetMax = new Vector2(0f, oMax.y);
        }

        /// <summary>가로 가운데 점만 비율 자리로 — 크기는 그대로.</summary>
        private static void PropPoint(RectTransform c, RectTransform p)
        {
            float pw = p.rect.width;
            if (pw <= 1f) return;
            float left = c.localPosition.x + c.rect.xMin - p.rect.xMin;
            float w = c.rect.width, cx = (left + w * 0.5f) / pw;
            Vector2 aMin = c.anchorMin, aMax = c.anchorMax, oMin = c.offsetMin, oMax = c.offsetMax;
            c.anchorMin = new Vector2(cx, aMin.y);
            c.anchorMax = new Vector2(cx, aMax.y);
            c.offsetMin = new Vector2(-w * 0.5f, oMin.y);
            c.offsetMax = new Vector2(w * 0.5f, oMax.y);
        }

        /// <summary>가로를 부모 가운데 기준 고정 자리로(덩어리가 가운데에 모인 채 남는다).</summary>
        private static void CenterX(RectTransform c, RectTransform p)
        {
            float left = c.localPosition.x + c.rect.xMin - p.rect.xMin;
            float w = c.rect.width;
            Vector2 aMin = c.anchorMin, aMax = c.anchorMax, oMin = c.offsetMin, oMax = c.offsetMax;
            c.anchorMin = new Vector2(0.5f, aMin.y);
            c.anchorMax = new Vector2(0.5f, aMax.y);
            c.offsetMin = new Vector2(left - p.rect.width * 0.5f, oMin.y);
            c.offsetMax = new Vector2(left - p.rect.width * 0.5f + w, oMax.y);
        }

        /// <summary>
        /// 빌더는 화면 판이 720 폭 고정(가운데)이라는 전제로 좌표를 잡는다 — 가로 채우기(RunWide)가 걸려 있으면 먼저 되돌린다.
        /// 다 지은 뒤 「UI v5 — 4:3 가로 채우기」 를 다시 돌린다.
        /// </summary>
        internal static void Narrow(RectTransform root)
        {
            if (root == null) return;
            var (top, bottom, topAnchored) = DesignY(root);
            root.anchorMin = new Vector2(0.5f, topAnchored ? 1f : 0.5f);
            root.anchorMax = new Vector2(0.5f, topAnchored ? 1f : 0.5f);
            root.pivot = new Vector2(0.5f, topAnchored ? 1f : 0.5f);
            root.sizeDelta = new Vector2(720f, bottom - top);
            root.anchoredPosition = new Vector2(0f, topAnchored ? -top : 0f);
        }

        /// <summary>
        /// 화면 판의 그린 세로 자리(9:16 화면 위에서 잰 위 · 아래 변) — 호스트 판은 위에 붙은 1280,
        /// 유령 판(Page)은 탭 밑 58 에서 1170, 나머지는 가운데 1280.
        /// </summary>
        private static (float top, float bottom, bool topAnchored) DesignY(RectTransform root)
            => root.name == "Page" ? (58f, 1228f, true)
             : root.name == "HostPage" ? (0f, 1280f, true)
             : (0f, 1280f, false);

        // ── 출전 ─────────────────────────────────────────────

        [MenuItem("Tools/Game/UI v5 — 출전 (mock_uiv3_sortie)")]
        public static void RunSortie()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[UI v5] 글자 본이 없다"); return; }
                var panel = lobby.transform.Find("ChapterHostPanel");
                var box = panel != null ? panel.Find("CHContent") as RectTransform : null;
                if (box == null) { Debug.LogError("[UI v5] CHContent 없음"); return; }
                Narrow(box);
                Sortie(box);
                BindSortie(panel.GetComponent<ChapterHostPanel>());
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[UI v5] 출전");
        }

        /// <summary>시안 좌표(720 폭) 상자 → 화면 좌표 상자.</summary>
        private static void M(Transform box, string name, float x0, float y0, float x1, float y1)
        {
            if (Find(box, name) is RectTransform r) Screen(r, x0, Gy(y0), x1, Gy(y1));
            else Debug.LogWarning($"[UI v5] 노드 없음: {name}");
        }

        private static void Hide(Transform box, params string[] names)
        {
            foreach (var n in names)
                if (Find(box, n) is Transform t) t.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI Txt(Transform box, string name, float size, Color color, TextAlignmentOptions align, float heavy = -1f)
        {
            var t = Find(box, name)?.GetComponent<TextMeshProUGUI>();
            if (t == null) return null;
            t.fontSize = t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Max(10f, size * 0.55f);
            t.enableAutoSizing = true;
            t.color = color;
            t.alignment = align;
            if (heavy >= 0f) Heavy(t, heavy);
            return t;
        }

        private static Image Plate(Transform parent, string name, string sprite, float ppu)
        {
            int at = -1;
            Transform holder = parent;
            if (Find(parent, name) is Transform old)
            {
                at = old.GetSiblingIndex();
                holder = old.parent;
                Object.DestroyImmediate(old.gameObject);
            }
            var img = Sliced(holder, name, sprite, 0, 0, 0, 0, ppu);
            // 처음 만들 때는 맨 앞(뒤에 그려진다) — 판은 언제나 글자 · 아이콘 밑이다
            img.transform.SetSiblingIndex(at >= 0 ? at : 0);
            return img;
        }

        private static void Sortie(RectTransform box)
        {
            // 바탕 · 머리 그림 · 제목
            if (box.Find("CHScreen")?.GetComponent<Image>() is Image screen)
            {
                screen.sprite = Spr($"{Art}/page_bg.png");
                screen.preserveAspect = false;
                C(screen.rectTransform, 360f, 640f, 1000f, 1778f);
            }
            int at = box.Find("CHScreen") is Transform sc ? sc.GetSiblingIndex() + 1 : 0;
            var head = Find(box, "CHV5Head") as RectTransform;
            if (head != null) Object.DestroyImmediate(head.gameObject);
            head = Node(box, "CHV5Head", -200f, Gy(70f), 920f, Gy(200f));
            head.SetSiblingIndex(at);
            head.gameObject.AddComponent<RectMask2D>();
            var headArt = Pic(head, "CHV5HeadArt", $"{Art}/showcase_bg.png", 0, 0, 0, 0);
            headArt.preserveAspect = false;
            Envelope(headArt, 1600f / 830f);
            if (Find(box, "CHV5Title") is Transform oldTitle) Object.DestroyImmediate(oldTitle.gameObject);
            var title = T(box, "CHV5Title", 60f, Color.white, true, TextAlignmentOptions.Left);
            Screen(title.rectTransform, 22f, Gy(112f), 330f, Gy(186f));
            Localized(title, "ui.sortie.title");
            if (title.GetComponent<Game.Module.Common.HeavyText>() is Game.Module.Common.HeavyText th) th.Set(0.25f, 0.2f, new Color32(10, 12, 30, 255));
            title.transform.SetSiblingIndex(at + 1);

            // 런타임이 켜고 끄는 옛 칸(초상 · 이름 · 설명 · 속성) — 시안에 없다. 꺼 두면 런타임이 다시 켜므로 보이지 않는 보관함으로 옮긴다
            var keep = Find(box, "CHV5Hidden") as RectTransform;
            if (keep == null) keep = Node(box, "CHV5Hidden", 0, 0, 0, 0);
            keep.gameObject.SetActive(false);
            foreach (var n in new[] { "CHInfoKindIcon", "CHInfoKindText", "CHTodayNote" })
                if (Find(box, n) is Transform t && t.parent != keep) t.SetParent(keep, true);
            // 예전에 보관함으로 옮겼던 칸은 스킬 띠로 되돌린다(유령 출전 때 쓴다 — _v5Info)
            foreach (var n in new[] { "CHInfoPortrait", "CHInfoName", "CHInfoDesc" })
                if (Find(box, n) is Transform t && t.parent == keep) t.SetParent(box, true);

            // 안 쓰는 옛 판 · 장식
            Hide(box, "CHSecChapter", "CHSecBoss", "CHSecBossFrame", "CHSecReward", "CHSecMode", "CHSecModeTick", "CHSecModeLine",
                 "CHSecHost", "CHSecHostTick", "CHSecHostLine", "CHSecInfoFrame", "CHBossBox", "CHRewardBox", "CHIconSkull",
                 "CHBossDash", "CHModeTitle", "CHHostTitle", "CHChapterDescText");

            // 챕터 카드(양피지) — 왼쪽 글자 · 오른쪽 보스 그림
            var card = Plate(box, "CHV5ChapterCard", $"{Kit}/panel_parch_big.png", 1.3f);
            Screen(card.rectTransform, 10f, Gy(198f), 710f, Gy(487f));
            card.transform.SetSiblingIndex(at + 2);
            var artFrame = Plate(box, "CHV5ArtFrame", $"{Kit}/panel_navy_mid.png", 1.8f);
            Screen(artFrame.rectTransform, 372f, Gy(209f), 698f, Gy(476f));
            artFrame.transform.SetSiblingIndex(at + 3);
            M(box, "CHChapterArtBox", 378f, 215f, 692f, 470f);
            if (Find(box, "CHChapterArtBox") is RectTransform artBox)
                foreach (Transform c in artBox)
                    if (c is RectTransform cr) { cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one; cr.offsetMin = cr.offsetMax = Vector2.zero; }
            M(box, "CHBossMask", 390f, 262f, 690f, 470f);
            // 보스는 챕터 그림 **위** — 예전엔 그림이 뒤에서 덮어 안 보였다
            if (Find(box, "CHBossMask") is Transform bm && Find(box, "CHChapterArtBox") is Transform ab)
                bm.SetSiblingIndex(ab.GetSiblingIndex() + 1);
            if (Find(box, "CHBossPortrait") is RectTransform bp)
            {
                bp.anchorMin = bp.anchorMax = bp.pivot = new Vector2(0.5f, 0f);
                bp.anchoredPosition = new Vector2(30f, -6f);
                bp.sizeDelta = new Vector2(250f, 194f);
            }
            M(box, "CHLockIcon", 490f, 290f, 580f, 400f);
            M(box, "CHBossLabel", 390f, 224f, 490f, 262f);
            Txt(box, "CHBossLabel", 24f, new Color(1f, 0.42f, 0.36f, 1f), TextAlignmentOptions.Center, 0.2f);
            M(box, "CHBossNameText", 390f, 440f, 686f, 470f);
            Txt(box, "CHBossNameText", 18f, Color.white, TextAlignmentOptions.Right);

            M(box, "CHChapterLabel", 40f, 218f, 270f, 250f);
            Txt(box, "CHChapterLabel", 28f, ChapterRed, TextAlignmentOptions.Left, 0.3f);
            Hide(box, "CHChapterNoText");
            M(box, "CHPrevArrow", 288f, 220f, 310f, 250f);
            M(box, "CHPrevButton", 278f, 214f, 322f, 256f);
            M(box, "CHNextArrow", 334f, 220f, 356f, 250f);
            M(box, "CHNextButton", 324f, 214f, 366f, 256f);
            M(box, "CHChapterNameText", 40f, 254f, 366f, 302f);
            Txt(box, "CHChapterNameText", 34f, Ink, TextAlignmentOptions.Left, 0.35f);
            M(box, "CHRecPowerText", 40f, 316f, 366f, 352f);
            Txt(box, "CHRecPowerText", 24f, Ink, TextAlignmentOptions.Left, 0.2f);
            M(box, "CHRewardLabel", 40f, 368f, 200f, 398f);
            Txt(box, "CHRewardLabel", 21f, InkSoft, TextAlignmentOptions.Left, 0.2f);
            M(box, "CHIconCoin", 42f, 398f, 86f, 434f);
            if (Find(box, "CHIconCoin")?.GetComponent<Image>() is Image coinImg) coinImg.preserveAspect = true;
            M(box, "CHRewardGoldText", 96f, 398f, 366f, 434f);
            Txt(box, "CHRewardGoldText", 26f, Ink, TextAlignmentOptions.Left, 0.25f);
            M(box, "CHRewardChestIcon", 44f, 438f, 84f, 472f);
            M(box, "CHRewardChestText", 96f, 438f, 366f, 472f);
            Txt(box, "CHRewardChestText", 22f, Ink, TextAlignmentOptions.Left, 0.15f);

            // 출전 방식 3칸
            ModeCard(box, "CHModeHost", 20f, 240f);
            ModeCard(box, "CHModeRandom", 250f, 470f);
            ModeCard(box, "CHModeGhost", 480f, 700f);

            // 호스트 카드 줄 — 카드를 키운다(시안 「큰 카드」)
            M(box, "CHHostViewport", 20f, 680f, 700f, 930f);
            M(box, "CHHostDim", 20f, 680f, 700f, 930f);
            M(box, "CHHostPrev", 2f, 760f, 20f, 850f);
            M(box, "CHHostNext", 700f, 760f, 718f, 850f);
            if (Find(box, "CHHostContent") is RectTransform content)
            {
                content.localScale = new Vector3(1.38f, 1.38f, 1f);
                content.pivot = new Vector2(0f, 0.5f);
                // 카드 테 — 등급 테(런타임이 _gradeFrames 로 갈아 끼운다) · 고른 테(카드에 박혀 있다)를 키트로
                foreach (Transform c in content)
                {
                    if (c.Find("CardFrame")?.GetComponent<Image>() is Image cf) { cf.sprite = Spr($"{Kit}/card_r.png"); cf.type = Image.Type.Sliced; cf.pixelsPerUnitMultiplier = 2.6f; }
                    if (c.Find("CardFrameSel")?.GetComponent<Image>() is Image cs) { cs.sprite = Spr($"{Kit}/card_sel.png"); cs.type = Image.Type.Sliced; cs.pixelsPerUnitMultiplier = 2.6f; }
                }
            }

            // 스킬 띠 — 필살기 · 액티브
            var strip = Plate(box, "CHV5SkillStrip", $"{Kit}/panel_parch_wide.png", 1.6f);
            Screen(strip.rectTransform, 14f, Gy(940f), 706f, Gy(1033f));
            strip.transform.SetSiblingIndex(at + 4);
            SkillCell(box, "CHInfoSkillA", 24f, 352f);
            SkillCell(box, "CHInfoSkillP", 368f, 696f);
            M(box, "CHInfoRandomFx", 30f, 940f, 120f, 1030f);
            if (Find(box, "CHInfoRandomFx") is RectTransform fx) fx.localScale = new Vector3(0.55f, 0.55f, 1f);
            M(box, "CHInfoRandomDice", 36f, 950f, 116f, 1020f);
            M(box, "CHRandomTitle", 130f, 946f, 540f, 976f);
            Txt(box, "CHRandomTitle", 24f, Ink, TextAlignmentOptions.Left, 0.3f);
            M(box, "CHRandomDesc1", 130f, 978f, 690f, 1002f);
            Txt(box, "CHRandomDesc1", 17f, InkSoft, TextAlignmentOptions.Left);
            M(box, "CHRandomDesc2", 130f, 1002f, 690f, 1026f);
            Txt(box, "CHRandomDesc2", 17f, InkSoft, TextAlignmentOptions.Left);
            M(box, "CHRandomPill", 560f, 946f, 690f, 976f);
            // 유령 출전 — 띠 왼쪽 유령 · 이름 · 규칙(런타임이 유령일 때만 켠다)
            M(box, "CHInfoPortrait", 30f, 944f, 116f, 1029f);
            M(box, "CHInfoName", 130f, 946f, 540f, 976f);
            Txt(box, "CHInfoName", 24f, Ink, TextAlignmentOptions.Left, 0.3f);
            M(box, "CHInfoDesc", 130f, 978f, 690f, 1026f);
            Txt(box, "CHInfoDesc", 17f, InkSoft, TextAlignmentOptions.TopLeft);
            if (Find(box, "CHInfoDesc")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI dsc) dsc.textWrappingMode = TextWrappingModes.Normal;

            // 상성 한 줄
            var aff = Plate(box, "CHV5AffinityStrip", $"{Kit}/panel_parch_wide.png", 2.2f);
            Screen(aff.rectTransform, 14f, Gy(1043f), 706f, Gy(1097f));
            aff.transform.SetSiblingIndex(at + 5);
            if (Find(box, "CHAffinityLine") is Transform oldLine) Object.DestroyImmediate(oldLine.gameObject);
            var line = T(box, "CHAffinityLine", 24f, Ink, true, TextAlignmentOptions.Center);
            Screen(line.rectTransform, 30f, Gy(1046f), 690f, Gy(1094f));
            line.richText = true;
            Heavy(line, 0.15f);

            // 도전하기
            M(box, "CHStartButton", 18f, 1110f, 700f, 1180f);
            if (Find(box, "CHStartButton")?.GetComponent<Image>() is Image sb)
            {
                sb.sprite = Spr($"{Kit}/btn_gold_wide.png");
                sb.type = Image.Type.Sliced;
                sb.pixelsPerUnitMultiplier = 1.4f;
                sb.preserveAspect = false;
            }
            if (Find(box, "CHStartButton") is RectTransform btn)
            {
                Mid(btn, "CHStartLabel", -150f, 0f, 300f, 50f);
                Txt(btn, "CHStartLabel", 36f, Ink, TextAlignmentOptions.Center, 0.35f);
                Mid(btn, "CHStartStaminaIcon", 52f, 0f, 22f, 32f);
                Mid(btn, "CHStartStaminaText", 66f, 0f, 44f, 40f, 0f);
                Txt(btn, "CHStartStaminaText", 28f, Ink, TextAlignmentOptions.Left, 0.3f);
                Mid(btn, "CHStartPlus", 108f, 0f, 24f, 40f, 0f);
                Txt(btn, "CHStartPlus", 28f, Ink, TextAlignmentOptions.Left, 0.3f);
                Mid(btn, "CHStartCostIcon", 146f, 0f, 26f, 26f);
                Mid(btn, "CHStartCostText", 162f, 0f, 130f, 40f, 0f);
                Txt(btn, "CHStartCostText", 28f, Ink, TextAlignmentOptions.Left, 0.3f);
            }
            M(box, "CHStartRibbon", 30f, 1092f, 190f, 1118f);
        }

        /// <summary>부모 가운데 기준 자리. px 는 pivot x(기본 가운데).</summary>
        private static void Mid(Transform parent, string name, float x, float y, float w, float h, float px = 0.5f)
        {
            if (Find(parent, name) is not RectTransform r) return;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(px, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        /// <summary>출전 방식 한 칸 — 남색 판 · 고르면 금테(card_sel) · 그림 위 · 이름 · 행동력.</summary>
        private static void ModeCard(Transform box, string name, float x0, float x1)
        {
            M(box, name, x0, 500f, x1, 665f);
            if (Find(box, name) is not RectTransform card) return;
            if (card.GetComponent<Image>() is Image plate)
            {
                plate.sprite = Spr($"{Kit}/btn_navy.png");
                plate.type = Image.Type.Sliced;
                plate.pixelsPerUnitMultiplier = 1.6f;
                plate.preserveAspect = false;
            }
            if (card.Find("ModeSel")?.GetComponent<Image>() is Image sel)
            {
                sel.sprite = Spr($"{Kit}/card_sel.png");
                sel.type = Image.Type.Sliced;
                sel.pixelsPerUnitMultiplier = 1.6f;
                sel.preserveAspect = false;
                var r = sel.rectTransform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            }
            float h = card.sizeDelta.y;
            Mid(card, "ModeArt", 0f, h * 0.18f, card.sizeDelta.x * 0.7f, h * 0.48f);
            Mid(card, "ModeTitle", 0f, -h * 0.17f, card.sizeDelta.x - 16f, 34f);
            Txt(card, "ModeTitle", 26f, Color.white, TextAlignmentOptions.Center);
            Mid(card, "ModeStaminaIcon", -22f, -h * 0.37f, 18f, 26f);
            Mid(card, "ModeStaminaText", -8f, -h * 0.37f, 60f, 30f, 0f);
            Txt(card, "ModeStaminaText", 22f, Gold, TextAlignmentOptions.Left);
            Hide(card, "ModeNote");
        }

        /// <summary>스킬 한 칸(필살기 · 액티브) — 아이콘 · 꼬리표 · 이름. 설명은 시안에 없다(육성 화면에서 본다).</summary>
        private static void SkillCell(Transform box, string name, float x0, float x1)
        {
            M(box, name, x0, 944f, x1, 1029f);
            if (Find(box, name) is not RectTransform cell) return;
            if (cell.GetComponent<Image>() is Image bg) bg.enabled = false;
            float w = cell.sizeDelta.x;
            Mid(cell, name + "Icon", -w * 0.5f + 44f, 0f, 70f, 70f);
            Mid(cell, name + "Name", -w * 0.5f + 90f, -10f, w - 100f, 40f, 0f);
            Txt(cell, name + "Name", 28f, Ink, TextAlignmentOptions.Left, 0.3f);
            Hide(cell, name + "Desc");
            string tagName = name + "Tag";
            if (cell.Find(tagName) is Transform oldTag) Object.DestroyImmediate(oldTag.gameObject);
            var tag = T(cell, tagName, 18f, name.EndsWith("A") ? new Color(0.85f, 0.18f, 0.12f, 1f) : new Color(0.1f, 0.55f, 0.35f, 1f),
                        true, TextAlignmentOptions.Left);
            Mid(cell, tagName, -w * 0.5f + 90f, 22f, 120f, 24f, 0f);
            Heavy(tag, 0.2f);
            Localized(tag, name.EndsWith("A") ? "ui.sortie.tag_ult" : "ui.sortie.tag_act");
        }

        // ── 호스트 육성 ───────────────────────────────────────

        [MenuItem("Tools/Game/UI v5 — 호스트 육성 (mock_uiv4_growth_stats · uiv5_growth_skill · uiv3_growth_star)")]
        public static void RunGrowth()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[UI v5] 글자 본이 없다"); return; }
                var growth = lobby.transform.Find("GrowthPanel");
                var hp = growth != null ? growth.Find("HostPage") as RectTransform : null;
                if (hp == null) { Debug.LogError("[UI v5] GrowthPanel/HostPage 없음 — 육성 v6 빌더부터"); return; }
                Narrow(hp);
                Growth(growth, hp);
                BindGrowth(growth.GetComponent<GrowthPanel>());
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[UI v5] 호스트 육성");
        }

        /// <summary>그룹 — 화면 크기 스트레치 판(안의 좌표가 화면 좌표 그대로다).</summary>
        private static RectTransform Group(Transform parent, string name, int at)
        {
            var g = Find(parent, name) as RectTransform;
            if (g == null)
            {
                g = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                g.gameObject.layer = parent.gameObject.layer;
                g.SetParent(parent, false);
            }
            g.anchorMin = Vector2.zero; g.anchorMax = Vector2.one; g.offsetMin = g.offsetMax = Vector2.zero;
            g.SetSiblingIndex(Mathf.Min(at, parent.childCount - 1));
            return g;
        }

        private static void Move(Transform root, string name, Transform to)
        {
            if (Find(root, name) is Transform t && t.parent != to) t.SetParent(to, true);
        }

        /// <summary>화면 좌표 상자로 — 노드가 어느 그룹에 있든 그룹이 화면 크기라 같다.</summary>
        private static void S(Transform root, string name, float x0, float y0, float x1, float y1)
        {
            if (Find(root, name) is RectTransform r) Screen(r, x0, y0, x1, y1);
            else Debug.LogWarning($"[UI v5] 노드 없음: {name}");
        }

        private static void Spr9(Transform root, string name, string sprite, float ppu)
        {
            if (Find(root, name)?.GetComponent<Image>() is not Image img) return;
            img.sprite = Spr(sprite);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppu;
            img.preserveAspect = false;
            img.color = Color.white;
        }

        private static void Growth(Transform growth, RectTransform hp)
        {
            // 바탕
            if (hp.Find("H6Bg")?.GetComponent<Image>() is Image bg)
            {
                bg.sprite = Spr($"{Art}/page_bg.png");
                C(bg.rectTransform, 360f, 640f, 1000f, 1778f);
            }
            int at = hp.Find("H6Bg") is Transform b0 ? b0.GetSiblingIndex() + 1 : 0;

            // 제목 · 진열 칸
            if (hp.Find("H6V5Title") is Transform ot) Object.DestroyImmediate(ot.gameObject);
            var title = T(hp, "H6V5Title", 46f, Color.white, true, TextAlignmentOptions.Left);
            Screen(title.rectTransform, 22f, 124f, 460f, 180f);
            Localized(title, "ui.growth5.title");
            if (title.GetComponent<Game.Module.Common.HeavyText>() is Game.Module.Common.HeavyText th) th.Set(0.25f, 0.2f, new Color32(10, 12, 30, 255));
            title.transform.SetSiblingIndex(at);

            var frame = Plate(hp, "H6V5ShowFrame", $"{Kit}/panel_navy_big.png", 1.2f);
            Screen(frame.rectTransform, 22f, 186f, 698f, 462f);
            frame.transform.SetSiblingIndex(at + 1);
            var view = Find(hp, "H6V5ShowView") as RectTransform;
            if (view != null) Object.DestroyImmediate(view.gameObject);
            view = Node(hp, "H6V5ShowView", 29f, 193f, 691f, 455f);
            view.SetSiblingIndex(at + 2);
            view.gameObject.AddComponent<RectMask2D>();
            var art = Pic(view, "H6V5ShowArt", $"{Art}/showcase_bg.png", 0, 0, 0, 0);
            art.preserveAspect = false;
            Envelope(art, 1600f / 830f);
            if (hp.Find("H6V5Ghost") is Transform og) Object.DestroyImmediate(og.gameObject);
            var ghost = Pic(hp, "H6V5Ghost", $"{Gr}/Portraits/portrait_ghost.png", 36f, 286f, 146f, 396f);
            ghost.transform.SetSiblingIndex(at + 3);
            S(hp, "H6Portrait", 100f, 196f, 436f, 458f);
            hp.Find("H6Portrait")?.SetSiblingIndex(at + 4);
            S(hp, "H6Grade", 444f, 206f, 504f, 250f);
            if (hp.Find("H6V5Kind") is Transform ok) Object.DestroyImmediate(ok.gameObject);
            var kind = Pic(hp, "H6V5Kind", $"{Ch}/ch_kind_blade.png", 512f, 200f, 566f, 254f);
            kind.transform.SetSiblingIndex(at + 5);
            S(hp, "H6Name", 444f, 254f, 692f, 314f);
            var name = Txt(hp, "H6Name", 44f, Color.white, TextAlignmentOptions.Left);
            if (name != null && name.GetComponent<Game.Module.Common.HeavyText>() is Game.Module.Common.HeavyText nh) nh.Set(0.25f, 0.2f, new Color32(10, 12, 30, 255));
            S(hp, "H6Stars", 444f, 318f, 664f, 360f);
            S(hp, "H6PowerText", 444f, 374f, 692f, 444f);
            Txt(hp, "H6PowerText", 34f, Gold, TextAlignmentOptions.Left, 0.25f);
            Hide(hp, "H6CardPlate", "H6Concept", "H6PowerIcon", "H6RankLabel", "H6ShardIcon", "H6ShardBar",
                 "H6SummaryPlate", "H6SummaryMark", "H6SummaryText", "H6StatPlate", "H6StatMark", "H6StatHeader",
                 "H6ListPlate", "H6ListMark", "H6ListHeader", "H6SortButton", "H6UltDesc", "H6ActDesc", "H6PasDesc");

            // 탭 셋
            string[] tabKeys = { "ui.growth5.tab_stats", "ui.growth5.tab_skill", "ui.growth5.tab_star" };
            float[] tx0 = { 22f, 250f, 474f }, tx1 = { 246f, 470f, 698f };
            for (int i = 0; i < 3; i++)
            {
                string tn = $"H6V5Tab{i}";
                if (hp.Find(tn) is Transform old) Object.DestroyImmediate(old.gameObject);
                var tab = Sliced(hp, tn, $"{Kit}/tab_off.png", tx0[i], 470f, tx1[i], 522f, 2f);
                tab.raycastTarget = true;
                Button(tab.gameObject);
                var on = Sliced(tab.transform, tn + "On", $"{Kit}/tab_on.png", 0, 0, 0, 0, 2f);
                StretchIn(on.rectTransform);
                var lt = T(tab.transform, tn + "Text", 24f, Color.white, true, TextAlignmentOptions.Center);
                StretchIn(lt.rectTransform);
                Localized(lt, tabKeys[i]);
            }

            // 그룹 셋 — 탭이 하나만 켠다(GrowthPanel.SelectH6Tab)
            int gAt = hp.Find("H6V5Tab2") is Transform t2 ? t2.GetSiblingIndex() + 1 : hp.childCount;
            var gStats = Group(hp, "H6GroupStats", gAt);
            var gSkill = Group(hp, "H6GroupSkill", gAt + 1);
            var gStar = Group(hp, "H6GroupStar", gAt + 2);

            // ── 기본 능력 — 상한 띠 + 2열 큰 카드 스크롤
            var cap = Plate(gStats, "H6V5CapStrip", $"{Kit}/panel_parch_wide.png", 2.2f);
            Screen(cap.rectTransform, 22f, 532f, 698f, 580f);
            cap.transform.SetAsFirstSibling();
            foreach (var n in new[] { "H6CapText", "H6StatHelp", "H6StatViewport" }) Move(hp, n, gStats);
            S(gStats, "H6CapText", 40f, 536f, 624f, 576f);
            Txt(gStats, "H6CapText", 22f, Ink, TextAlignmentOptions.Left, 0.25f);
            S(gStats, "H6StatHelp", 654f, 542f, 682f, 570f);
            S(gStats, "H6StatViewport", 22f, 588f, 698f, 1020f);
            if (Find(gStats, "H6Row") is RectTransform row)
            {
                row.anchorMin = row.anchorMax = row.pivot = new Vector2(0f, 1f);
                row.anchoredPosition = new Vector2(2f, -4f);
                row.sizeDelta = new Vector2(330f, 104f);
                Spr9(row, "Plate", $"{Kit}/panel_parch_big.png", 2.4f);
                Mid(row, "H6RowIcon", -118f, 0f, 62f, 62f);
                Mid(row, "H6RowName", -82f, 28f, 140f, 32f, 0f);
                Txt(row, "H6RowName", 24f, Ink, TextAlignmentOptions.Left, 0.3f);
                Mid(row, "H6RowLv", -82f, 0f, 140f, 28f, 0f);
                Txt(row, "H6RowLv", 20f, InkSoft, TextAlignmentOptions.Left, 0.15f);
                Mid(row, "H6RowPct", -82f, -28f, 140f, 28f, 0f);
                Txt(row, "H6RowPct", 22f, new Color(0.1f, 0.5f, 0.25f, 1f), TextAlignmentOptions.Left, 0.25f);
                Mid(row, "H6RowButton", 110f, 0f, 92f, 50f);
                Spr9(row, "H6RowButton", $"{Kit}/btn_gold_small.png", 3.2f);
                if (row.Find("H6RowButton") is RectTransform rb)
                {
                    Mid(rb, "H6RowCoin", -28f, 0f, 22f, 22f);
                    Mid(rb, "H6RowCost", -14f, 0f, 58f, 32f, 0f);
                    Txt(rb, "H6RowCost", 22f, Ink, TextAlignmentOptions.Left, 0.3f);
                }
            }

            // ── 스킬 — 필살기 · 액티브 · 패시브 · 6성 각성 · 성급 UP
            foreach (var n in new[] { "H6UltPlate", "H6UltIcon", "H6UltTag", "H6UltName", "H6UltCond", "H6UltPower", "H6UltHit",
                                      "H6ActPlate", "H6ActIcon", "H6ActTag", "H6ActName", "H6ActCond", "H6ActPower", "H6ActStep", "H6ActHit",
                                      "H6PasPlate", "H6PasIcon", "H6PasTag", "H6PasName", "H6PasPower", "H6PasHit",
                                      "H6OptPlate", "H6OptLock", "H6OptTag", "H6OptText", "H6StarUpButton",
                                      "H6UltDesc", "H6ActDesc", "H6PasDesc" })
                Move(hp, n, gSkill);
            SkillRow(gSkill, "Ult", 532f, 640f, new Color(0.85f, 0.18f, 0.12f, 1f));
            SkillRow(gSkill, "Act", 648f, 756f, new Color(0.1f, 0.55f, 0.35f, 1f));
            SkillRow(gSkill, "Pas", 764f, 856f, new Color(0.45f, 0.3f, 0.12f, 1f));
            S(gSkill, "H6ActStep", 470f, 700f, 690f, 750f);
            Spr9(gSkill, "H6ActStep", $"{Kit}/panel_navy_mid.png", 3f);
            if (Find(gSkill, "H6ActStep") is RectTransform step)
            {
                Mid(step, "H6ActStepTitle", 0f, 11f, 210f, 22f);
                Txt(step, "H6ActStepTitle", 15f, Gold, TextAlignmentOptions.Center);
                Mid(step, "H6ActStepValue", 0f, -11f, 210f, 22f);
                Txt(step, "H6ActStepValue", 15f, Color.white, TextAlignmentOptions.Center);
            }
            S(gSkill, "H6OptPlate", 22f, 864f, 698f, 910f);
            Spr9(gSkill, "H6OptPlate", $"{Kit}/panel_navy_mid.png", 2.6f);
            S(gSkill, "H6OptLock", 36f, 869f, 70f, 905f);
            S(gSkill, "H6OptTag", 78f, 868f, 380f, 906f);
            if (Find(gSkill, "H6OptTag")?.GetComponent<Image>() is Image optBg) optBg.enabled = false;
            if (Find(gSkill, "H6OptTag") is RectTransform optTag)
            {
                Mid(optTag, "H6OptTagText", 0f, 0f, 300f, 34f);
                Txt(optTag, "H6OptTagText", 18f, new Color(0.86f, 0.88f, 0.95f), TextAlignmentOptions.Left);
            }
            S(gSkill, "H6OptText", 386f, 868f, 690f, 906f);
            Txt(gSkill, "H6OptText", 15f, new Color(0.86f, 0.88f, 0.95f), TextAlignmentOptions.Right);
            S(gSkill, "H6StarUpButton", 52f, 920f, 668f, 1014f);
            Spr9(gSkill, "H6StarUpButton", $"{Kit}/btn_gold_wide.png", 1.3f);
            if (Find(gSkill, "H6StarUpButton") is RectTransform sub)
            {
                Mid(sub, "H6StarUpText", 0f, 14f, 400f, 44f);
                Txt(sub, "H6StarUpText", 34f, Ink, TextAlignmentOptions.Center, 0.35f);
                Mid(sub, "H6StarUpCoin", -40f, -22f, 24f, 24f);
                Mid(sub, "H6StarUpCost", -24f, -22f, 140f, 28f, 0f);
                Txt(sub, "H6StarUpCost", 20f, Ink, TextAlignmentOptions.Left, 0.2f);
            }

            // ── 성급 — 「성급 올리기」 창 내용을 탭 칸에 펼친다
            var win = growth.Find("H6StarUpWindow");
            var window = win != null ? win.Find("H6Window") as RectTransform : Find(gStar, "H6Window") as RectTransform;
            if (window != null)
            {
                window.SetParent(gStar, false);
                Screen(window, 22f, 530f, 698f, 1020f);
                StarTab(window);
            }
            if (win != null) win.gameObject.SetActive(false);

            // 호스트 목록 — 아래 줄
            S(hp, "H6ListViewport", 30f, 1030f, 690f, 1160f);
            if (Find(hp, "H6HostCard") is RectTransform card)
            {
                Spr9(card, "CardFrame", $"{Kit}/card_r.png", 2.8f);
                Spr9(card, "CardFrameSel", $"{Kit}/card_sel.png", 2.8f);
            }

            // 유령 · 호스트 전환 — 시안엔 탭이 없어 제목 줄 오른쪽 작은 단추 둘로
            var page = growth.Find("Page");
            foreach (var (btn, x) in new[] { ("TabGhostButton", 472f), ("TabHostButton", 588f) })
            {
                if (page == null || page.Find(btn) is not RectTransform b) continue;
                TL(b, x, 72f, 112f, 46f);
                foreach (Transform c in b)
                {
                    if (c.GetComponent<Image>() is Image ci)
                    {
                        bool on = c.name.EndsWith("On");
                        ci.sprite = Spr(on ? $"{Kit}/tab_on.png" : $"{Kit}/tab_off.png");
                        ci.type = Image.Type.Sliced;
                        ci.pixelsPerUnitMultiplier = 2.4f;
                        ci.preserveAspect = false;
                        var r = ci.rectTransform;
                        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                    }
                    if (c.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI ct)
                    {
                        var r = ct.rectTransform;
                        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                        ct.fontSize = ct.fontSizeMax = 18f; ct.fontSizeMin = 10f; ct.enableAutoSizing = true;
                        ct.alignment = TextAlignmentOptions.Center;
                    }
                }
            }
            if (page != null)
                foreach (var n in new[] { "GrowthSideL", "GrowthSideR", "GrowthFiller" })
                    if (page.Find(n)?.GetComponent<Image>() is Image side) side.enabled = false;   // 네온 이음 띠 — 바탕은 HostPage 가 깐다
        }

        // ── 유령 육성 ─────────────────────────────────────────

        [MenuItem("Tools/Game/UI v5 — 유령 육성 (mock_uiv2_d_ghost · c_ghost)")]
        public static void RunGhost()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[UI v5] 글자 본이 없다"); return; }
                var page = lobby.transform.Find("GrowthPanel/Page") as RectTransform;
                if (page == null) { Debug.LogError("[UI v5] GrowthPanel/Page 없음"); return; }
                Narrow(page);
                Ghost(page);
                var so = new SerializedObject(lobby.transform.Find("GrowthPanel").GetComponent<GrowthPanel>());
                so.FindProperty("_ghostCols").intValue = 2;
                so.FindProperty("_ghostColPitch").floatValue = 342f;
                so.FindProperty("_rowPitch").floatValue = 96f;
                so.FindProperty("_ghostStatLift").floatValue = 0f;
                so.FindProperty("_labelDone").colorValue = new Color(0.1f, 0.45f, 0.5f, 1f);
                so.FindProperty("_labelNow").colorValue = Ink;
                so.FindProperty("_labelNext").colorValue = new Color(0.62f, 0.42f, 0.02f, 1f);
                so.FindProperty("_labelLock").colorValue = InkSoft;
                so.FindProperty("_pillDone").objectReferenceValue = Spr($"{Kit}/tab_on.png");
                so.FindProperty("_pillReady").objectReferenceValue = Spr($"{Kit}/btn_gold_small.png");
                so.FindProperty("_pillLock").objectReferenceValue = Spr($"{Kit}/tab_off.png");
                so.FindProperty("_pillTextDone").colorValue = new Color(0.5f, 0.95f, 0.88f, 1f);
                so.FindProperty("_pillTextReady").colorValue = Ink;
                so.FindProperty("_pillTextLock").colorValue = new Color(0.62f, 0.67f, 0.78f, 1f);
                so.ApplyModifiedPropertiesWithoutUndo();
                // 유령 · 호스트 탭 글자 — 남색 탭 위 검은 글자는 안 보인다(PD 2026-10-19). 고른 탭 = 금빛, 안 고른 탭 = 밝은 회청
                foreach (var n in new[] { "TabGhostText", "TabHostText" })
                    if (Find(page, n)?.GetComponent<Game.Module.Common.UI.TabTextColors>() is Component tc)
                    {
                        var ts = new SerializedObject(tc);
                        ts.FindProperty("_on").colorValue = Gold;
                        ts.FindProperty("_off").colorValue = new Color(0.82f, 0.86f, 0.95f, 1f);
                        ts.ApplyModifiedPropertiesWithoutUndo();
                    }
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[UI v5] 유령 육성");
        }

        /// <summary>Page 는 화면 y 58 에서 시작한다 — 화면 좌표(왼쪽 위)로 놓는다.</summary>
        private const float PageTop = 58f;

        private static void G(Transform root, string name, float x0, float y0, float x1, float y1)
        {
            if (Find(root, name) is RectTransform r) TL(r, x0, y0 - PageTop, x1 - x0, y1 - y0);
            else Debug.LogWarning($"[UI v5] 노드 없음: {name}");
        }

        /// <summary>InkRun 조각 사이 간격 — 옛 배치 값(넓은 간격)을 새 글자 크기에 맞게 줄인다.</summary>
        private static void Gaps(Transform root, string runNode, params float[] gaps)
        {
            if (Find(root, runNode)?.GetComponent<Game.Module.Common.UI.InkRun>() is not Component run) return;
            var so = new SerializedObject(run);
            var g = so.FindProperty("_gaps");
            g.arraySize = gaps.Length;
            for (int i = 0; i < gaps.Length; i++) g.GetArrayElementAtIndex(i).floatValue = gaps[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>그 칸의 위에서부터 잰 가운데(부모 위 변 기준, 아래로 +).</summary>
        private static float CenterTd(RectTransform r) => -r.anchoredPosition.y - (0.5f - r.pivot.y) * r.rect.height;
        private static float TopTd(RectTransform r) => -r.anchoredPosition.y - (1f - r.pivot.y) * r.rect.height;

        private static void Ghost(RectTransform page)
        {
            // 시안 D(mock_uiv2_d_ghost) 배치 · 혼합 톤 — 남색 진열 칸 + 양피지 판(PD 2026-10-19 「하얀 시안처럼 레이아웃을 새로」)
            // 바탕 — 유령이 박힌 통그림 대신 홈 바탕(유령은 진열 칸에 따로 선다)
            if (page.Find("BgGhost")?.GetComponent<Image>() is Image bg)
            {
                bg.sprite = Spr($"{Art}/page_bg.png");
                bg.preserveAspect = false;
                var r = bg.rectTransform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                if (bg.GetComponent<AspectRatioFitter>() == null) Envelope(bg, 1080f / 1920f);
            }
            int at = page.Find("BgGhost") is Transform b0 ? b0.GetSiblingIndex() + 1 : 0;

            // 장식 묶음(제목 · 진열 칸) — 런타임이 유령 탭에서만 켠다
            if (page.Find("GhostV5Deco") is Transform od) Object.DestroyImmediate(od.gameObject);
            var deco = new GameObject("GhostV5Deco", typeof(RectTransform)).GetComponent<RectTransform>();
            deco.gameObject.layer = page.gameObject.layer;
            deco.SetParent(page, false);
            deco.anchorMin = Vector2.zero; deco.anchorMax = Vector2.one; deco.offsetMin = deco.offsetMax = Vector2.zero;
            deco.SetSiblingIndex(at);
            var title = T(deco, "GhostV5Title", 46f, Color.white, true, TextAlignmentOptions.Left);
            TL(title.rectTransform, 22f, 124f - PageTop, 440f, 56f);
            Localized(title, "ui.growth5.ghost_title");
            if (title.GetComponent<Game.Module.Common.HeavyText>() is Game.Module.Common.HeavyText th) th.Set(0.05f, 0.18f, new Color32(10, 12, 30, 255));

            // 진열 칸 — 왼쪽 유령 · 오른쪽 남색 정보 판
            var frame = Sliced(deco, "GhostV5Frame", $"{Kit}/panel_navy_big.png", 0, 0, 0, 0, 1.2f);
            TL(frame.rectTransform, 22f, 186f - PageTop, 676f, 280f);
            var view = new GameObject("GhostV5View", typeof(RectTransform)).GetComponent<RectTransform>();
            view.gameObject.layer = page.gameObject.layer;
            view.SetParent(deco, false);
            TL(view, 29f, 193f - PageTop, 662f, 266f);
            view.gameObject.AddComponent<RectMask2D>();
            var art = Pic(view, "GhostV5Art", $"{Art}/showcase_bg.png", 0, 0, 0, 0);
            art.preserveAspect = false;
            Envelope(art, 1600f / 830f);
            var ghost = Pic(deco, "GhostV5Portrait", $"{Gr}/Portraits/portrait_ghost.png", 0, 0, 0, 0);
            TL(ghost.rectTransform, 48f, 198f - PageTop, 300f, 258f);
            var info = Sliced(deco, "GhostV5Info", $"{Kit}/panel_navy_mid.png", 0, 0, 0, 0, 2f);
            TL(info.rectTransform, 372f, 200f - PageTop, 316f, 252f);

            // 정보 판 — 「유령」 · Lv(큰 흰 글자) · 경험치 · 막대 · 설명
            foreach (var n in new[] { "CardNameEnText", "CardStar0", "CardStar1", "CardStar2", "CardStar3", "CardStar4" })
                if (Find(page, n) is Transform h) h.localScale = Vector3.zero;   // 「GHOST」 · 꺼진 별 — 런타임이 켜도 안 보이게
            G(page, "CardNameText", 390f, 206f, 676f, 236f);
            Txt(page, "CardNameText", 24f, new Color(0.74f, 0.85f, 1f), TextAlignmentOptions.Left, 0f);
            G(page, "CardLvLabel", 390f, 244f, 428f, 302f);
            G(page, "CardLvNum", 428f, 244f, 510f, 302f);
            G(page, "CardLvMax", 510f, 244f, 620f, 302f);
            foreach (var (n, sz, c) in new[] { ("CardLvLabel", 30f, Color.white), ("CardLvNum", 56f, Color.white),
                                               ("CardLvMax", 26f, new Color(0.78f, 0.84f, 0.95f)) })
                if (Find(page, n)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t)
                {
                    t.fontSize = t.fontSizeMax = sz; t.enableAutoSizing = false; t.color = c;
                    t.alignment = TextAlignmentOptions.BaselineLeft;
                }
            Gaps(page, "CardLvRun", 6f, 8f);
            G(page, "ExpLabelText", 390f, 302f, 500f, 330f);
            Txt(page, "ExpLabelText", 20f, new Color(0.78f, 0.86f, 1f), TextAlignmentOptions.Left, 0f);
            G(page, "ExpValueText", 500f, 302f, 676f, 330f);
            Txt(page, "ExpValueText", 22f, new Color(0.5f, 0.95f, 0.88f), TextAlignmentOptions.Right, 0f);
            G(page, "ExpBarFrame", 390f, 334f, 676f, 356f);
            Spr9(page, "ExpBarFrame", $"{Kit}/bar_bg.png", 3f);
            G(page, "ExpBarFill", 393f, 337f, 673f, 353f);
            if (Find(page, "ExpBarFill")?.GetComponent<Image>() is Image ef) { ef.sprite = Spr($"{Kit}/bar_fill.png"); ef.type = Image.Type.Filled; ef.fillMethod = Image.FillMethod.Horizontal; ef.preserveAspect = false; }
            G(page, "CardDescText", 390f, 364f, 680f, 448f);
            if (Txt(page, "CardDescText", 18f, new Color(0.93f, 0.95f, 1f), TextAlignmentOptions.TopLeft) is TextMeshProUGUI dt)
            {
                dt.textWrappingMode = TextWrappingModes.Normal;
                dt.fontSizeMin = 13f;
            }

            // 능력치 — 양피지 판 하나에 머리 줄 + 2열 × 4줄(스크롤 없이 다 보인다)
            var sec = Find(page, "StatSection");
            if (sec != null)
            {
                var strip = Plate(sec, "GhostV5StatStrip", $"{Kit}/panel_parch_big.png", 1.6f);
                TL(strip.rectTransform, 22f, 474f - PageTop, 676f, 458f);
                strip.transform.SetAsFirstSibling();
            }
            G(page, "StatHeaderText", 40f, 486f, 600f, 526f);
            Txt(page, "StatHeaderText", 26f, Ink, TextAlignmentOptions.Left, 0f);
            G(page, "StatHelpButton", 656f, 492f, 684f, 520f);
            G(page, "StatViewport", 28f, 536f, 692f, 924f);
            if (Find(page, "StatRow") is RectTransform row)
            {
                row.anchorMin = row.anchorMax = row.pivot = new Vector2(0f, 1f);
                row.anchoredPosition = new Vector2(2f, -2f);
                row.sizeDelta = new Vector2(326f, 88f);
                if (row.GetComponent<Image>() is Image rp) { rp.sprite = Spr($"{Kit}/panel_parch_wide.png"); rp.type = Image.Type.Sliced; rp.pixelsPerUnitMultiplier = 2.4f; rp.preserveAspect = false; }
                Mid(row, "RowIcon", -122f, 0f, 56f, 56f);
                Mid(row, "RowNameText", -88f, 25f, 150f, 28f, 0f);
                Txt(row, "RowNameText", 20f, Ink, TextAlignmentOptions.Left, 0f);
                Mid(row, "RowLvLabel", -88f, -1f, 28f, 26f, 0f);
                Mid(row, "RowLvNum", -62f, -1f, 36f, 26f, 0f);
                Mid(row, "RowLvMax", -28f, -1f, 56f, 26f, 0f);
                foreach (var (n, sz, c) in new[] { ("RowLvLabel", 16f, InkSoft), ("RowLvNum", 20f, new Color(0.62f, 0.42f, 0.02f, 1f)), ("RowLvMax", 16f, InkSoft) })
                    if (Find(row, n)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t)
                    {
                        t.fontSize = t.fontSizeMax = sz; t.enableAutoSizing = false; t.color = c;
                        t.alignment = TextAlignmentOptions.BaselineLeft;
                    }
                Gaps(row, "RowLvRun", 4f, 4f);
                Mid(row, "RowPctText", -88f, -26f, 140f, 26f, 0f);
                Txt(row, "RowPctText", 19f, new Color(0.1f, 0.5f, 0.25f, 1f), TextAlignmentOptions.Left, 0f);
                Mid(row, "RowButton", 108f, 0f, 92f, 48f);
                if (row.Find("RowButton") is RectTransform rb)
                {
                    if (rb.GetComponent<Image>() is Image bi) { bi.sprite = Spr($"{Kit}/btn_gold_small.png"); bi.type = Image.Type.Sliced; bi.pixelsPerUnitMultiplier = 3.2f; bi.preserveAspect = false; bi.color = Color.white; }
                    foreach (Transform c in rb)
                        if (c.GetComponent<Image>() is Image ci && c.GetComponent<TextMeshProUGUI>() == null && c.name != "RowCoin") ci.enabled = false;   // 옛 버튼 그림 조각
                    Mid(rb, "RowCoin", -26f, 0f, 22f, 22f);
                    Mid(rb, "RowCostText", -12f, 0f, 56f, 30f, 0f);
                    Txt(rb, "RowCostText", 20f, Ink, TextAlignmentOptions.Left, 0f);
                }
            }

            // 성장 경로 — 양피지 판 · 제목 줄(오른쪽에 다음 보상) · Lv 글자 → 칸 → 상태 알약
            var path = Find(page, "GhostPath") as RectTransform;
            if (path != null)
            {
                var pp = Plate(path, "GhostV5PathPlate", $"{Kit}/panel_parch_big.png", 1.6f);
                TL(pp.rectTransform, 22f, 940f - PageTop, 676f, 222f);
                pp.transform.SetAsFirstSibling();
                G(path, "PathTitleText", 40f, 950f, 380f, 990f);
                Txt(path, "PathTitleText", 26f, Ink, TextAlignmentOptions.Left, 0f);
                G(path, "PathHelpButton", 658f, 956f, 686f, 984f);

                // 다음 보상 — 아래 상자 대신 제목 줄 오른쪽에 작게(받을 수 있는 칸은 알약이 알린다)
                if (Find(path, "PathRewards") is RectTransform rewards)
                {
                    rewards.anchorMin = Vector2.zero; rewards.anchorMax = Vector2.one;
                    rewards.offsetMin = rewards.offsetMax = Vector2.zero;
                }
                foreach (var (icon, text, x) in new[] { ("PathRewardIcon0", "PathRewardGold", 392f), ("PathRewardIcon1", "PathRewardGem", 496f),
                                                        ("PathRewardIcon2", "PathRewardCore", 580f) })
                {
                    G(path, icon, x, 957f, x + 26f, 983f);
                    G(path, text, x + 28f, 954f, x + (text == "PathRewardGold" ? 100f : text == "PathRewardGem" ? 80f : 72f), 986f);
                    Txt(path, text, 18f, Ink, TextAlignmentOptions.Left, 0f);
                }
                Hide(path, "PathBox", "PathBoxButton", "BoxLvLabel", "BoxLvNum", "BoxLvMax", "BoxLvRun", "BoxBarFrame", "BoxBarFill", "BoxValueText");
                foreach (var n in new[] { "PathMarker", "PathMarkerLabel" })
                    if (Find(path, n) is Transform h) h.localScale = Vector3.zero;   // 시안엔 지금 자리 표시가 없다 — 런타임이 켜도 안 보이게

                const float x0 = 92f, pitch = 135.25f, labelY = 1012f, nodeY = 1060f, pillY = 1118f;
                foreach (var n in new[] { "PathLineDone", "PathLineNow", "PathLineLock" })
                {
                    GC(path, n, x0, nodeY, 100f, 8f);
                    if (Find(path, n)?.GetComponent<Image>() is Image li) li.preserveAspect = false;   // 런타임이 폭을 늘린다 — 그림 비율을 지키면 점처럼 줄어든다
                }
                for (int i = 0; i < 5; i++)
                {
                    float cx = x0 + i * pitch;
                    GC(path, $"PathLabel{i}", cx, labelY, 110f, 34f);
                    Txt(path, $"PathLabel{i}", 22f, Ink, TextAlignmentOptions.Center, 0f);
                    GC(path, $"PathNode{i}", cx, nodeY, 56f, 56f);
                    if (Find(path, $"PathPill{i}") is Transform old) Object.DestroyImmediate(old.gameObject);
                    var pill = Sliced(path, $"PathPill{i}", $"{Kit}/tab_off.png", 0, 0, 0, 0, 2.4f);
                    GC(path, $"PathPill{i}", cx, pillY, 108f, 40f);
                    pill.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
                    var pt = T(pill.transform, $"PathPillText{i}", 20f, Color.white, true, TextAlignmentOptions.Center);
                    var pr = pt.rectTransform;
                    pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one; pr.offsetMin = pr.offsetMax = Vector2.zero;
                    pt.fontSizeMax = 20f; pt.fontSizeMin = 12f; pt.enableAutoSizing = true;
                    Heavy(pt, 0f);
                }
            }
        }

        /// <summary>Page 좌표(화면 왼쪽 위)의 가운데 점에 놓는다 — 런타임이 x 로 칸 · 줄을 잇는 경로 칸은 가운데 기준이어야 한다.</summary>
        private static void GC(Transform root, string name, float cx, float cy, float w, float h)
        {
            if (Find(root, name) is not RectTransform r) { Debug.LogWarning($"[UI v5] 노드 없음: {name}"); return; }
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(cx, -(cy - PageTop));
            r.sizeDelta = new Vector2(w, h);
        }

        /// <summary>스킬 한 줄 — 양피지 판 · 아이콘 · 꼬리표(색 글자) · 이름 · 조건 · 위력.</summary>
        private static void SkillRow(Transform g, string k, float y0, float y1, Color tagColor)
        {
            S(g, $"H6{k}Plate", 22f, y0, 698f, y1);
            Spr9(g, $"H6{k}Plate", $"{Kit}/panel_parch_wide.png", 1.8f);
            S(g, $"H6{k}Icon", 34f, y0 + 10f, 34f + (y1 - y0 - 20f), y1 - 10f);
            float ix = 34f + (y1 - y0 - 20f) + 14f;
            S(g, $"H6{k}Tag", ix, y0 + 8f, ix + 200f, y0 + 32f);
            if (Find(g, $"H6{k}Tag")?.GetComponent<Image>() is Image tagBg) tagBg.enabled = false;
            if (Find(g, $"H6{k}Tag") is RectTransform tag)
            {
                Mid(tag, $"H6{k}TagText", -100f, 0f, 200f, 24f, 0f);
                Txt(tag, $"H6{k}TagText", 17f, tagColor, TextAlignmentOptions.Left, 0.25f);
            }
            S(g, $"H6{k}Name", ix, y0 + 32f, 460f, y0 + 68f);
            Txt(g, $"H6{k}Name", 28f, Ink, TextAlignmentOptions.Left, 0.3f);
            S(g, $"H6{k}Cond", ix, y0 + 68f, 460f, y1 - 8f);
            Txt(g, $"H6{k}Cond", 17f, InkSoft, TextAlignmentOptions.Left);
            S(g, $"H6{k}Power", 400f, y0 + 14f, 690f, y0 + 50f);
            Txt(g, $"H6{k}Power", 19f, Ink, TextAlignmentOptions.Right, 0.2f);
            S(g, $"H6{k}Hit", 22f, y0, 698f, y1);
        }

        /// <summary>성급 탭 — 「성급 3 → 4」 · 1~10 사다리 · 「다음 성급에 함께 오르는 것」 줄 · 조각 · 골드 · 성급 UP.</summary>
        private static void StarTab(RectTransform w)
        {
            float W = w.sizeDelta.x;   // 676
            void At(string n, float cx, float cy, float ww, float hh)
            {
                if (Find(w, n) is not RectTransform r) return;
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(cx, -cy);
                r.sizeDelta = new Vector2(ww, hh);
            }
            if (Find(w, "H6WinBody") is RectTransform body)
            {
                body.anchorMin = Vector2.zero; body.anchorMax = Vector2.one; body.offsetMin = body.offsetMax = Vector2.zero;
                Spr9(w, "H6WinBody", $"{Kit}/panel_parch_big.png", 1.6f);
            }
            Hide(w, "H6WinTitle", "H6WinFace", "H6WinStarsNow", "H6WinStarsNext", "H6WinArrow", "H6WinLine");
            At("H6WinRank", W * 0.5f, 32f, W - 40f, 46f);
            Txt(w, "H6WinRank", 32f, Ink, TextAlignmentOptions.Center, 0.35f);

            // 사다리 — 고른 칸 테 · 별 10 · 번호 10
            foreach (var n in new[] { "H6V5LadderSel" })
                if (Find(w, n) is Transform old) Object.DestroyImmediate(old.gameObject);
            for (int i = 1; i <= 10; i++)
                foreach (var n in new[] { $"H6V5LadderStar{i}", $"H6V5LadderNum{i}" })
                    if (Find(w, n) is Transform old) Object.DestroyImmediate(old.gameObject);
            var sel = Sliced(w, "H6V5LadderSel", $"{Kit}/icon_plate.png", 0, 0, 0, 0, 2.6f);
            float step = (W - 80f) / 9f;
            for (int i = 1; i <= 10; i++)
            {
                bool big = i % 5 == 0;
                var st = Pic(w, $"H6V5LadderStar{i}", $"{Ch}/ch_star_silver.png", 0, 0, 0, 0);
                At($"H6V5LadderStar{i}", 40f + step * (i - 1), 86f, big ? 48f : 36f, big ? 48f : 36f);
                var num = T(w, $"H6V5LadderNum{i}", 17f, InkSoft, true, TextAlignmentOptions.Center);
                At($"H6V5LadderNum{i}", 40f + step * (i - 1), 122f, 40f, 22f);
                num.text = i.ToString();
            }
            At("H6V5LadderSel", 40f, 86f, 54f, 54f);

            At("H6WinRises", W * 0.5f, 150f, W - 40f, 30f);
            Txt(w, "H6WinRises", 21f, Ink, TextAlignmentOptions.Center, 0.25f);
            for (int i = 0; ; i++)
            {
                if (Find(w, $"H6WinRow{i}") is not RectTransform row) break;
                row.anchorMin = row.anchorMax = new Vector2(0f, 1f);
                row.pivot = new Vector2(0.5f, 0.5f);
                row.anchoredPosition = new Vector2(W * 0.5f, -(176f + 34f * i));
                row.sizeDelta = new Vector2(W - 40f, 32f);
                if (row.GetComponent<Image>() is Image ri)
                {
                    ri.sprite = Spr($"{Kit}/panel_parch_wide.png"); ri.type = Image.Type.Sliced; ri.pixelsPerUnitMultiplier = 3.2f;
                }
                Mid(row, "Icon", -(W - 40f) * 0.5f + 22f, 0f, 26f, 26f);
                Mid(row, "Label", -(W - 40f) * 0.5f + 44f, 0f, 260f, 30f, 0f);
                Txt(row, "Label", 21f, Ink, TextAlignmentOptions.Left, 0.25f);
                Mid(row, "Value", (W - 40f) * 0.5f - 12f, 0f, 330f, 30f, 1f);
                Txt(row, "Value", 22f, Ink, TextAlignmentOptions.Right, 0.3f);
            }
            if (Find(w, "H6WinFoot") is RectTransform foot)
            {
                foot.anchorMin = foot.anchorMax = new Vector2(0f, 0f);
                foot.pivot = new Vector2(0.5f, 0f);
                foot.anchoredPosition = new Vector2(W * 0.5f, 8f);
                foot.sizeDelta = new Vector2(W - 24f, 54f);
                void F(string n, float x0, float x1)
                {
                    if (Find(foot, n) is not RectTransform r) return;
                    r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
                    r.pivot = new Vector2(0.5f, 0.5f);
                    r.anchoredPosition = new Vector2((x0 + x1) * 0.5f, 0f);
                    r.sizeDelta = new Vector2(x1 - x0, 48f);
                }
                F("H6WinShardPlate", 0f, 170f);
                F("H6WinGoldPlate", 176f, 346f);
                F("H6WinConfirm", 354f, W - 24f);
                Spr9(foot, "H6WinShardPlate", $"{Kit}/panel_navy_mid.png", 3f);
                Spr9(foot, "H6WinGoldPlate", $"{Kit}/panel_navy_mid.png", 3f);
                Spr9(foot, "H6WinConfirm", $"{Kit}/btn_gold_wide.png", 1.6f);
                if (Find(foot, "H6WinConfirmText")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI ctx) { ctx.color = Ink; Heavy(ctx, 0.35f); }
                Hide(foot, "H6WinCancel", "H6WinNext");
            }
        }

        private static void BindGrowth(GrowthPanel gp)
        {
            if (gp == null) return;
            var so = new SerializedObject(gp);
            so.FindProperty("_h6V5").boolValue = true;
            so.FindProperty("_h6Cols").intValue = 2;
            so.FindProperty("_h6ColPitch").floatValue = 342f;
            so.FindProperty("_h6RowPitch").floatValue = 112f;
            var kinds = so.FindProperty("_kindIcons");
            string[] k = { "blade", "force", "magic" };
            kinds.arraySize = k.Length;
            for (int i = 0; i < k.Length; i++) kinds.GetArrayElementAtIndex(i).objectReferenceValue = Spr($"{Ch}/ch_kind_{k[i]}.png");
            var frames = so.FindProperty("_gradeFrames");
            string[] f = { "card_r", "card_sr", "card_ssr" };
            frames.arraySize = f.Length;
            for (int i = 0; i < f.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = Spr($"{Kit}/{f[i]}.png");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 소환 ─────────────────────────────────────────────

        /// <summary>소환 시안(720 폭) 세로 → 게임 세로. 시안 위 막대 70 → 120 · 아래 바 1140 → 1170.</summary>
        private static float Sy(float y) => 120f + (y - 70f) * (1050f / 1070f);

        [MenuItem("Tools/Game/UI v5 — 소환 (mock_uiv5_summon)")]
        public static void RunSummon()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[UI v5] 글자 본이 없다"); return; }
                var panel = lobby.transform.Find("SummonPanel");
                var box = panel != null ? panel.Find("SummonContent") as RectTransform : null;
                if (box == null) { Debug.LogError("[UI v5] SummonContent 없음"); return; }
                Narrow(box);
                Summon(box);
                var so = new SerializedObject(panel.GetComponent<SummonPanel>());
                so.FindProperty("_v5").boolValue = true;
                var badges = so.FindProperty("_gradeBadges");
                string[] g = { "b", "a", "s" };
                badges.arraySize = g.Length;
                for (int i = 0; i < g.Length; i++) badges.GetArrayElementAtIndex(i).objectReferenceValue = Spr($"{Ch}/ch_grade_{g[i]}.png");
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[UI v5] 소환");
        }

        private static void Summon(RectTransform box)
        {
            if (box.Find("SummonBackdrop")?.GetComponent<Image>() is Image bd)
            {
                bd.sprite = Spr($"{Art}/page_bg.png");
                bd.preserveAspect = false;
                C(bd.rectTransform, 360f, 640f, 1000f, 1778f);
            }
            int at = box.Find("SummonBackdrop") is Transform b0 ? b0.GetSiblingIndex() + 1 : 0;

            // 픽업 · 소환권 판 · 옛 제단 장식 — 보관함(런타임이 켜도 안 보이게)
            var keep = Find(box, "SummonV5Hidden") as RectTransform;
            if (keep == null) keep = Node(box, "SummonV5Hidden", 0, 0, 0, 0);
            keep.gameObject.SetActive(false);
            foreach (var n in new[] { "GachaPickupBanner", "PickupArt", "PickupLabelText", "PickupNameText", "PickupNoteText",
                                      "PickupTimePlate", "GachaTicketPlate", "SummonCircle", "SummonGhost", "SummonTitleIcon", "GachaPityPanel" })
                Move(box, n, keep);

            // 위 장면 — 영혼 명부(칸을 덮게 깔고 자른다)
            var view = Find(box, "SummonV5AltarView") as RectTransform;
            if (view == null) view = Node(box, "SummonV5AltarView", 0, 0, 0, 0);
            Screen(view, -120f, 120f, 840f, 744f);
            view.SetSiblingIndex(at);
            if (view.GetComponent<RectMask2D>() == null) view.gameObject.AddComponent<RectMask2D>();
            if (Find(box, "SummonAltar")?.GetComponent<Image>() is Image altar)
            {
                altar.transform.SetParent(view, false);
                altar.sprite = Spr($"{Art}/summon_altar.png");
                altar.preserveAspect = false;
                if (altar.GetComponent<AspectRatioFitter>() == null) Envelope(altar, 1440f / 1100f);
                // 아래 변 기준 — 넓은 화면(4:3)에서 그림이 커지면 위(하늘)가 잘리고 책 · 명부 판은 남는다
                var ar = altar.rectTransform;
                ar.anchorMin = ar.anchorMax = ar.pivot = new Vector2(0.5f, 0f);
                ar.anchoredPosition = Vector2.zero;
            }
            // 그림 속 「영혼 명부」 한국어 판 위에 언어팩 판을 덮는다(일 · 영 화면)
            // 덮개 판은 그림의 **자식** — 그림이 화면에 따라 커져도 그림 속 판 자리(가로 36~64 % · 세로 아래 16~23 %)에 붙는다
            if (box.Find("SummonV5LedgerPlate") is Transform stale) Object.DestroyImmediate(stale.gameObject);
            var altarRt = Find(box, "SummonAltar") as RectTransform;
            var plate = Plate(altarRt != null ? altarRt : box, "SummonV5LedgerPlate", $"{Kit}/panel_parch_wide.png", 3.2f);
            if (altarRt != null)
            {
                var pr = plate.rectTransform;
                pr.anchorMin = new Vector2(0.36f, 0.165f);
                pr.anchorMax = new Vector2(0.64f, 0.232f);
                pr.offsetMin = pr.offsetMax = Vector2.zero;
            }
            else Screen(plate.rectTransform, 246f, 642f, 474f, 690f);
            if (Find(box, "SummonV5LedgerText") is Transform ol) Object.DestroyImmediate(ol.gameObject);
            var ledger = T(plate.transform, "SummonV5LedgerText", 26f, Ink, true, TextAlignmentOptions.Center);
            StretchIn(ledger.rectTransform);
            Localized(ledger, "ui.summon.ledger");
            Heavy(ledger, 0.3f);

            // 「<」 · 제목
            if (Find(box, "SummonBackButton") is Transform ob) Object.DestroyImmediate(ob.gameObject);
            var back = Pic(box, "SummonBackButton", $"{Kit}/btn_back.png", 18f, Sy(86f), 80f, Sy(146f));
            back.raycastTarget = true;
            Button(back.gameObject);
            S(box, "SummonTitleText", 92f, Sy(82f), 420f, Sy(152f));
            var title = Txt(box, "SummonTitleText", 54f, Color.white, TextAlignmentOptions.Left);
            if (title != null)
            {
                var h = title.GetComponent<Game.Module.Common.HeavyText>();
                if (h == null) h = title.gameObject.AddComponent<Game.Module.Common.HeavyText>();
                h.Set(0.25f, 0.2f, new Color32(10, 12, 30, 255));
            }

            // 등급 확률 판
            var rates = Plate(box, "SummonV5RatePlate", $"{Kit}/panel_parch_big.png", 1.6f);
            Screen(rates.rectTransform, 20f, Sy(712f), 700f, Sy(905f));
            rates.transform.SetSiblingIndex(view.GetSiblingIndex() + 1);
            if (Find(box, "SummonV5RateBanner") is Transform orb) Object.DestroyImmediate(orb.gameObject);
            var banner = T(box, "SummonV5RateBanner", 32f, new Color(0.6f, 0.1f, 0.06f, 1f), true, TextAlignmentOptions.Center);
            Screen(banner.rectTransform, 40f, Sy(728f), 390f, Sy(780f));
            Localized(banner, "ui.summon.rate_banner");
            Heavy(banner, 0.35f);
            float[] cx = { 95f, 210f, 325f };
            for (int i = 0; i < 3; i++)
            {
                foreach (var n in new[] { $"SummonV5Badge{i}", $"SummonV5Rate{i}" })
                    if (Find(box, n) is Transform o) Object.DestroyImmediate(o.gameObject);
                Pic(box, $"SummonV5Badge{i}", $"{Ch}/ch_grade_{(i == 0 ? "b" : i == 1 ? "a" : "s")}.png", cx[i] - 40f, Sy(792f), cx[i] + 40f, Sy(848f));
                var rt = T(box, $"SummonV5Rate{i}", 28f, Ink, true, TextAlignmentOptions.Center);
                Screen(rt.rectTransform, cx[i] - 56f, Sy(852f), cx[i] + 56f, Sy(890f));
                Heavy(rt, 0.3f);
            }
            var pity = Plate(box, "SummonV5PityPlate", $"{Kit}/panel_navy_mid.png", 2f);
            Screen(pity.rectTransform, 408f, Sy(752f), 684f, Sy(892f));
            pity.transform.SetSiblingIndex(rates.transform.GetSiblingIndex() + 1);
            S(box, "GachaPityText", 422f, Sy(760f), 670f, Sy(800f));
            Txt(box, "GachaPityText", 24f, Color.white, TextAlignmentOptions.Left, 0.2f);
            S(box, "GachaPityBar", 422f, Sy(808f), 670f, Sy(832f));
            Spr9(box, "GachaPityBar", $"{Kit}/bar_bg.png", 3f);
            S(box, "GachaPityIcon", 422f, Sy(840f), 462f, Sy(884f));
            if (Find(box, "SummonV5PityNote") is Transform opn) Object.DestroyImmediate(opn.gameObject);
            var note = T(box, "SummonV5PityNote", 24f, new Color(1f, 0.55f, 0.45f, 1f), true, TextAlignmentOptions.Left);
            Screen(note.rectTransform, 470f, Sy(842f), 670f, Sy(882f));
            Localized(note, "ui.summon.pity_note");

            // 단추 — 1회 · 10회
            Pull(box, "GachaPull1Button", "Pull1", 26f, 352f, $"{Kit}/panel_parch_big.png", 1.8f);
            Pull(box, "GachaPull10Button", "Pull10", 368f, 694f, $"{Kit}/btn_gold_wide.png", 1.4f);
            var ribbon = Plate(box, "SummonV5FreeRibbon", $"{Kit}/tag_gold.png", 2f);
            Screen(ribbon.rectTransform, 14f, Sy(908f), 170f, Sy(946f));
            var rtx = T(ribbon.transform, "SummonV5FreeRibbonText", 18f, Ink, true, TextAlignmentOptions.Center);
            StretchIn(rtx.rectTransform);
            Localized(rtx, "ui.summon.free_ribbon");
            Heavy(rtx, 0.25f);
            ribbon.transform.SetAsLastSibling();

            S(box, "GachaRateButton", 110f, Sy(1064f), 346f, Sy(1120f));
            Spr9(box, "GachaRateButton", $"{Kit}/btn_navy.png", 2.4f);
            S(box, "GachaHistoryButton", 374f, Sy(1064f), 610f, Sy(1120f));
            Spr9(box, "GachaHistoryButton", $"{Kit}/btn_navy.png", 2.4f);
            foreach (var n in new[] { "GachaRateText", "GachaHistoryText" })
                if (Find(box, n) is RectTransform tr)
                {
                    tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
                    Txt(box, n, 22f, Color.white, TextAlignmentOptions.Center);
                }
        }

        private static void Pull(Transform box, string node, string key, float x0, float x1, string sprite, float ppu)
        {
            S(box, node, x0, Sy(925f), x1, Sy(1050f));
            Spr9(box, node, sprite, ppu);
            if (Find(box, node) is not RectTransform b) return;
            Mid(b, key + "TitleText", 0f, 20f, b.sizeDelta.x - 30f, 46f);
            Txt(b, key + "TitleText", 34f, Ink, TextAlignmentOptions.Center, 0.35f);
            Mid(b, "GemIcon", -70f, -24f, 34f, 30f);
            Hide(b, "TicketIcon");
            Mid(b, key + "PriceText", -48f, -24f, b.sizeDelta.x * 0.5f + 40f, 32f, 0f);
            Txt(b, key + "PriceText", 22f, Ink, TextAlignmentOptions.Left, 0.25f);
            if (b.Find("Tag") is RectTransform tag)
            {
                tag.anchorMin = tag.anchorMax = new Vector2(1f, 1f);
                tag.pivot = new Vector2(1f, 0.5f);
                tag.anchoredPosition = new Vector2(-8f, 0f);
            }
        }

        // ── 시즌 패스 ─────────────────────────────────────────

        /// <summary>패스 시안(720 폭) 세로 → 게임 세로. 시안 위 막대 75 → 120 · 아래 바 1155 → 1170.</summary>
        private static float Py(float y) => 120f + (y - 75f) * (1050f / 1080f);

        [MenuItem("Tools/Game/UI v5 — 시즌 패스 (mock_uiv4_pass)")]
        public static void RunPass()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[UI v5] 글자 본이 없다"); return; }
                var panel = lobby.transform.Find("SeasonPassPanel");
                var box = panel != null ? panel.Find("PassContent") as RectTransform : null;
                if (box == null) { Debug.LogError("[UI v5] PassContent 없음"); return; }
                Narrow(box);
                Pass(box);
                var so = new SerializedObject(panel.GetComponent<SeasonPassPanel>());
                if (so.FindProperty("_tab") is SerializedProperty tab) tab.objectReferenceValue = Spr($"{Kit}/tab_off.png");
                if (so.FindProperty("_tabSelected") is SerializedProperty tabSel) tabSel.objectReferenceValue = Spr($"{Kit}/tab_on.png");
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[UI v5] 시즌 패스");
        }

        private static void Pass(RectTransform box)
        {
            if (box.Find("PassBackdrop")?.GetComponent<Image>() is Image bd)
            {
                bd.sprite = Spr($"{Art}/page_bg.png");
                bd.preserveAspect = false;
                C(bd.rectTransform, 360f, 640f, 1000f, 1778f);
            }
            Hide(box, "PassBannerFrame", "PassNameText", "PassSloganText", "PassHeadTier");

            // 머리 — 달밤 그림 · 유령 · 갱스터 · 「시즌 패스」 · SEASON · 남은 날
            S(box, "PassBannerMask", -120f, Py(75f), 840f, Py(292f));
            if (Find(box, "PassBannerArt")?.GetComponent<Image>() is Image art)
            {
                art.sprite = Spr($"{Art}/showcase_bg.png");
                art.preserveAspect = false;
                if (art.GetComponent<AspectRatioFitter>() == null) Envelope(art, 1600f / 830f);
            }
            var mask = Find(box, "PassBannerMask");
            foreach (var n in new[] { "PassV5Ghost", "PassV5Host", "PassV5Title" })
                if (Find(box, n) is Transform o) Object.DestroyImmediate(o.gameObject);
            var ghost = Pic(box, "PassV5Ghost", $"{Gr}/Portraits/portrait_ghost.png", 200f, Py(150f), 345f, Py(292f));
            ghost.transform.SetSiblingIndex(mask.GetSiblingIndex() + 1);
            var host = Pic(box, "PassV5Host", $"{Gr}/Portraits/portrait_gangster.png", 318f, Py(108f), 512f, Py(292f));
            host.transform.SetSiblingIndex(mask.GetSiblingIndex() + 2);
            var title = T(box, "PassV5Title", 54f, Color.white, true, TextAlignmentOptions.Left);
            Screen(title.rectTransform, 26f, Py(96f), 330f, Py(170f));
            Localized(title, "ui.pass.title_v5");
            if (title.GetComponent<Game.Module.Common.HeavyText>() is Game.Module.Common.HeavyText th) th.Set(0.25f, 0.2f, new Color32(10, 12, 30, 255));
            S(box, "PassSeasonText", 46f, Py(180f), 300f, Py(216f));
            Txt(box, "PassSeasonText", 26f, Gold, TextAlignmentOptions.Left, 0.25f);
            S(box, "PassTimeText", 46f, Py(220f), 300f, Py(252f));
            Txt(box, "PassTimeText", 20f, Color.white, TextAlignmentOptions.Left, 0.15f);

            // 보상 · 미션 전환 — 머리 오른쪽(시안 「미션」 단추)
            S(box, "PassTabRewards", 404f, Py(206f), 548f, Py(262f));
            S(box, "PassTabMissions", 556f, Py(206f), 700f, Py(262f));
            foreach (var n in new[] { "PassTabRewards", "PassTabMissions" })
                if (Find(box, n) is RectTransform tb)
                {
                    Spr9(box, n, $"{Kit}/tab_off.png", 2.2f);
                    Mid(tb, "TabIcon", -48f, 0f, 26f, 24f);
                    Mid(tb, n + "Text", 12f, 0f, 100f, 34f);
                    Txt(tb, n + "Text", 22f, Color.white, TextAlignmentOptions.Center, 0.2f);
                }
            S(box, "PassMissionDot", 684f, Py(198f), 706f, Py(220f));

            // 레벨 띠 — 현재 Lv · 경험치 막대
            var strip = Plate(box, "PassV5LevelStrip", $"{Kit}/panel_parch_wide.png", 1.8f);
            Screen(strip.rectTransform, 18f, Py(300f), 702f, Py(370f));
            strip.transform.SetSiblingIndex(mask.GetSiblingIndex() + 3);
            S(box, "PassLevelText", 34f, Py(304f), 200f, Py(366f));
            Txt(box, "PassLevelText", 40f, Ink, TextAlignmentOptions.Left, 0.35f);
            S(box, "PassXpBar", 206f, Py(320f), 686f, Py(352f));
            Spr9(box, "PassXpBar", $"{Kit}/bar_bg.png", 2.6f);
            if (Find(box, "PassXpFill")?.GetComponent<Image>() is Image xf) { xf.sprite = Spr($"{Kit}/bar_fill.png"); xf.color = Color.white; }
            if (Find(box, "PassXpBar") is RectTransform xb)
            {
                Mid(xb, "PassXpText", 120f, 0f, 200f, 28f);
                Txt(xb, "PassXpText", 18f, Color.white, TextAlignmentOptions.Right, 0.2f);
            }

            // 구매 · 모두 받기 — 레벨 띠 아래 한 줄
            S(box, "PassBuyPremium", 18f, Py(378f), 256f, Py(432f));
            Spr9(box, "PassBuyPremium", $"{Kit}/btn_gold_wide.png", 2f);
            S(box, "PassBuyBundle", 262f, Py(378f), 486f, Py(432f));
            Spr9(box, "PassBuyBundle", $"{Kit}/btn_gold_small.png", 2.2f);
            S(box, "PassClaimAll", 492f, Py(378f), 702f, Py(432f));
            Spr9(box, "PassClaimAll", $"{Kit}/btn_navy.png", 2.2f);
            foreach (var (b, t) in new[] { ("PassBuyPremium", "PassBuyPremiumText"), ("PassBuyBundle", "PassBuyBundleText"), ("PassClaimAll", "PassClaimAllText") })
                if (Find(box, b) is RectTransform br && Find(br, t) is RectTransform tr)
                {
                    tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(8f, 2f); tr.offsetMax = new Vector2(-8f, -2f);
                    Txt(br, t, 21f, b == "PassClaimAll" ? Color.white : Ink, TextAlignmentOptions.Center, 0.25f);
                }

            // 목록 판 · 머리(무료 | 프리미엄)
            S(box, "PassListFrame", 18f, Py(440f), 702f, Py(1140f));
            Spr9(box, "PassListFrame", $"{Kit}/panel_parch_big.png", 1.4f);
            // 머리 판은 보상 페이지 안 맨 뒤 — 글자(PassHeadFree · PassHeadPremium)가 그 페이지에 있다
            var page = Find(box, "PassRewardsPage") ?? box;
            foreach (var n in new[] { "PassV5HeadFreePlate", "PassV5HeadPremPlate" })   // 예전 실행이 PassContent 바로 밑에 둔 것
                if (box.Find(n) is Transform stale) Object.DestroyImmediate(stale.gameObject);
            var hf = Plate(page, "PassV5HeadFreePlate", $"{Kit}/panel_navy_mid.png", 2.4f);
            if (hf.transform.parent != page) hf.transform.SetParent(page, false);
            Screen(hf.rectTransform, 44f, Py(446f), 334f, Py(488f));
            hf.transform.SetSiblingIndex(0);
            var hp = Plate(page, "PassV5HeadPremPlate", $"{Kit}/card_sr.png", 3f);
            if (hp.transform.parent != page) hp.transform.SetParent(page, false);
            Screen(hp.rectTransform, 386f, Py(446f), 676f, Py(488f));
            hp.transform.SetSiblingIndex(1);
            S(box, "PassHeadFree", 44f, Py(446f), 334f, Py(488f));
            Txt(box, "PassHeadFree", 24f, Color.white, TextAlignmentOptions.Center, 0.25f);
            S(box, "PassHeadCrown", 452f, Py(452f), 484f, Py(482f));
            S(box, "PassHeadPremium", 488f, Py(446f), 640f, Py(488f));
            Txt(box, "PassHeadPremium", 24f, Gold, TextAlignmentOptions.Left, 0.25f);
            S(box, "PassScroll", 28f, Py(494f), 692f, Py(1132f));
            S(box, "MissionScroll", 28f, Py(446f), 692f, Py(1132f));

            // 단계 줄 — 무료 | 번호 | 프리미엄, 가운데 줄
            if (Find(box, "PassScrollContent") is RectTransform content)
            {
                const float pitch = 74f, rowH = 66f;
                int n = 0;
                foreach (Transform c in content)
                {
                    if (!c.name.StartsWith("PassRow") || c is not RectTransform row) continue;
                    row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
                    row.pivot = new Vector2(0.5f, 0.5f);
                    row.anchoredPosition = new Vector2(0f, -(pitch * n + pitch * 0.5f));
                    row.sizeDelta = new Vector2(660f, rowH);
                    PassRow(row);
                    n++;
                }
                content.sizeDelta = new Vector2(content.sizeDelta.x, pitch * n + 8f);
                var track = Find(content, "PassV5Track") as RectTransform;
                if (track == null)
                {
                    track = Sliced(content, "PassV5Track", $"{Kit}/bar_bg.png", 0, 0, 0, 0, 3f).rectTransform;
                    track.SetAsFirstSibling();
                }
                track.anchorMin = track.anchorMax = new Vector2(0.5f, 1f);
                track.pivot = new Vector2(0.5f, 0.5f);
                track.localEulerAngles = new Vector3(0f, 0f, 90f);   // 가로 막대 그림을 세워 가운데 줄로
                track.sizeDelta = new Vector2(pitch * n, 14f);
                track.anchoredPosition = new Vector2(0f, -pitch * n * 0.5f);
            }
        }

        private static void PassRow(RectTransform row)
        {
            void P(string n, float x, float w, float h, float y = 0f)
            {
                if (row.Find(n) is not RectTransform r) return;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(x, y);
                r.sizeDelta = new Vector2(w, h);
            }
            void Sp(string n, string sprite, float ppu)
            {
                if (row.Find(n)?.GetComponent<Image>() is not Image img) return;
                img.sprite = Spr(sprite); img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = ppu; img.preserveAspect = false;
            }
            const float free = -176f, prem = 176f, plateW = 230f, plateH = 62f;
            P("TierPlate", 0f, 56f, 56f); Sp("TierPlate", $"{Kit}/icon_plate.png", 3f);
            P("TierNow", 0f, 66f, 66f); Sp("TierNow", $"{Kit}/card_sel.png", 3.4f);
            P("TierText", 0f, 50f, 40f);
            if (row.Find("TierText")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI tt) { tt.fontSize = tt.fontSizeMax = 24f; tt.color = Color.white; }
            foreach (var (k, x, plate, glow) in new[] { ("Free", free, $"{Kit}/panel_navy_mid.png", $"{Kit}/card_sel.png"),
                                                        ("Prem", prem, $"{Kit}/card_sr.png", $"{Kit}/card_sel.png") })
            {
                P(k + "Plate", x, plateW, plateH); Sp(k + "Plate", plate, k == "Free" ? 3f : 3.4f);
                P(k + "Glow", x, plateW + 6f, plateH + 6f); Sp(k + "Glow", glow, 3.4f);
                P(k + "Icon", x - 40f, 48f, 44f);
                P(k + "Amount", x + 24f, 100f, 30f);
                if (row.Find(k + "Amount")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI am)
                {
                    am.fontSize = am.fontSizeMax = 20f; am.alignment = TextAlignmentOptions.Left; am.color = Color.white;
                }
                if (row.Find(k + "Name") is Transform nm) nm.gameObject.SetActive(false);
                P($"{row.name}{k}Claim", x + 70f, 84f, 40f); Sp($"{row.name}{k}Claim", $"{Kit}/btn_gold_small.png", 3.4f);
                if (row.Find($"{row.name}{k}Claim") is RectTransform cl && cl.Find(k + "ClaimText")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI ct)
                {
                    ct.color = Ink; ct.fontSize = ct.fontSizeMax = 19f;
                }
                P(k + "Check", x + 86f, 34f, 34f, -10f);
                if (row.Find(k + "Check")?.GetComponent<Image>() is Image ck) { ck.sprite = Spr($"{Kit}/icon_check.png"); ck.preserveAspect = true; }
                P(k + "Lock", x + 88f, 40f, 46f);
                if (row.Find(k + "Lock") is RectTransform lk && lk.Find("LockIcon")?.GetComponent<Image>() is Image li)
                {
                    li.sprite = Spr($"{Kit}/icon_lock.png"); li.preserveAspect = true;
                }
            }
        }

        // ── 망령의 밤 ─────────────────────────────────────────

        /// <summary>밤 · 탑 시안(720 폭) 세로 → 게임 세로. 시안 위 막대 75 → 120 · 아래 바 1135 → 1170.</summary>
        private static float Ny(float y) => 120f + (y - 75f) * (1050f / 1060f);

        [MenuItem("Tools/Game/UI v5 — 망령의 밤 (mock_uiv3_night)")]
        public static void RunNight()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[UI v5] 글자 본이 없다"); return; }
                var panel = lobby.transform.Find("NightEntryPanel");
                var box = panel != null ? panel.Find("NightContent") as RectTransform : null;
                if (box == null) { Debug.LogError("[UI v5] NightContent 없음"); return; }
                Narrow(box);
                Night(panel, box);
                UnderNav(lobby.transform, panel);
                var so = new SerializedObject(panel.GetComponent<NightEntryPanel>());
                so.FindProperty("_dayCell").objectReferenceValue = Spr($"{Kit}/panel_parch_big.png");
                so.FindProperty("_dayCellToday").objectReferenceValue = Spr($"{Kit}/btn_gold_small.png");   // 짙은 글자가 읽히게 금 판
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[UI v5] 망령의 밤");
        }

        /// <summary>모드 창은 아래 바 **밑**에 둔다 — 시안처럼 아래 바가 보이고, 누르면 그 화면으로 간다(LobbyMainUI.SelectTab 이 창을 닫는다).</summary>
        private static void UnderNav(Transform lobby, Transform panel)
        {
            if (lobby.Find("BottomNav") is Transform nav && panel.GetSiblingIndex() > nav.GetSiblingIndex())
                panel.SetSiblingIndex(nav.GetSiblingIndex());
        }

        private static void PageBg(Transform panel, string bgName)
        {
            if (panel.Find(bgName)?.GetComponent<Image>() is not Image bg) return;
            bg.sprite = Spr($"{Art}/page_bg.png");
            bg.preserveAspect = false;
            bg.color = Color.white;
        }

        private static void BackButton(Transform box, string node, string textNode, float y0, float y1)
        {
            S(box, node, 16f, y0, 74f, y1);
            if (Find(box, node)?.GetComponent<Image>() is Image img)
            {
                img.sprite = Spr($"{Kit}/btn_back.png"); img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 2f; img.preserveAspect = false;
            }
            Hide(box, textNode);
        }

        private static void BigTitle(Transform box, string node, float x0, float y0, float x1, float y1, float size)
        {
            S(box, node, x0, y0, x1, y1);
            var t = Txt(box, node, size, Color.white, TextAlignmentOptions.Left);
            if (t == null) return;
            var h = t.GetComponent<Game.Module.Common.HeavyText>();
            if (h == null) h = t.gameObject.AddComponent<Game.Module.Common.HeavyText>();
            h.Set(0.25f, 0.2f, new Color32(10, 12, 30, 255));
        }

        private static void Night(Transform panel, RectTransform box)
        {
            PageBg(panel, "NightBg");
            Hide(box, "NightTitlePlate", "NightEntryBox", "NightEntryPip0", "NightEntryPip1", "NightEntryPip2", "NightStartHintText");
            for (int i = 0; i < 8; i++) Hide(box, $"NightFlowCell{i}");
            BackButton(box, "NightBackButton", "NightBackText", Ny(118f), Ny(176f));
            BigTitle(box, "NightTitleText", 86f, Ny(110f), 520f, Ny(210f), 60f);

            // 요일 7칸
            for (int i = 0; i < 7; i++)
            {
                float x0 = 15f + i * 99.5f;
                S(box, $"NightDayCell{i}", x0, Ny(240f), x0 + 94f, Ny(336f));
                if (Find(box, $"NightDayCell{i}")?.GetComponent<Image>() is Image ci) { ci.type = Image.Type.Sliced; ci.pixelsPerUnitMultiplier = 2.6f; }
                if (Find(box, $"NightDayCell{i}") is RectTransform cell)
                {
                    Mid(cell, $"NightDayName{i}", 0f, 14f, 86f, 34f);
                    Txt(cell, $"NightDayName{i}", 28f, Ink, TextAlignmentOptions.Center, 0.35f);
                    Mid(cell, $"NightDayRule{i}", 0f, -22f, 88f, 24f);
                    Txt(cell, $"NightDayRule{i}", 15f, Ink, TextAlignmentOptions.Center, 0.15f);
                }
            }

            // 오늘 카드 — 진열 그림 · 유령 · 갱스터 · 규칙 제목
            S(box, "NightTodayCard", 15f, Ny(350f), 705f, Ny(686f));
            Spr9(box, "NightTodayCard", $"{Kit}/panel_navy_big.png", 1.2f);
            var cardAt = Find(box, "NightTodayCard").GetSiblingIndex();
            var view = Find(box, "NightV5View") as RectTransform;
            if (view != null) Object.DestroyImmediate(view.gameObject);
            view = Node(box, "NightV5View", 22f, Ny(357f), 698f, Ny(679f));
            view.SetSiblingIndex(cardAt + 1);
            view.gameObject.AddComponent<RectMask2D>();
            var art = Pic(view, "NightV5Art", $"{Art}/showcase_bg.png", 0, 0, 0, 0);
            art.preserveAspect = false;
            Envelope(art, 1600f / 830f);
            Pic(view, "NightV5Ghost", $"{Gr}/Portraits/portrait_ghost.png", 0, 0, 0, 0).rectTransform.Let(r => Box(r, 0f, 0f, 150f, 90f, 150f, 150f, 0.5f, 0f));
            Pic(view, "NightV5Host", $"{Gr}/Portraits/portrait_gangster.png", 0, 0, 0, 0).rectTransform.Let(r => Box(r, 0f, 0f, 300f, 8f, 280f, 280f, 0.5f, 0f));
            Hide(box, "NightTodayArt");
            S(box, "NightDayText", 380f, Ny(372f), 690f, Ny(400f));
            Txt(box, "NightDayText", 18f, Gold, TextAlignmentOptions.Right, 0.2f);
            S(box, "NightRuleTitleText", 360f, Ny(440f), 690f, Ny(520f));
            var rule = Txt(box, "NightRuleTitleText", 48f, new Color(0.55f, 0.9f, 1f, 1f), TextAlignmentOptions.Right);
            if (rule != null) { var h = rule.GetComponent<Game.Module.Common.HeavyText>(); if (h == null) h = rule.gameObject.AddComponent<Game.Module.Common.HeavyText>(); h.Set(0.25f, 0.2f, new Color32(10, 12, 30, 255)); }
            S(box, "NightRuleText", 360f, Ny(520f), 690f, Ny(552f));
            Txt(box, "NightRuleText", 21f, Color.white, TextAlignmentOptions.Right, 0.2f);
            S(box, "NightHostFitText", 380f, Ny(566f), 690f, Ny(590f));   // 칸 위 별(+32)과 겹치지 않게 한 칸 위
            Txt(box, "NightHostFitText", 15f, Color.white, TextAlignmentOptions.Right);
            for (int i = 0; i < 12; i++)
            {
                if (Find(box, $"NightHostSlot{i}") is not RectTransform slot) break;
                float x1 = 690f - i * 44f;   // 칸이 몇 개든 오른쪽부터 한 줄(최대 8)
                if (i < 8) Screen(slot, x1 - 40f, Ny(622f), x1, Ny(662f));
                else slot.localScale = Vector3.zero;
            }

            // 최고 기록
            var best = Plate(box, "NightV5BestPlate", $"{Kit}/panel_parch_big.png", 1.6f);
            Screen(best.rectTransform, 15f, Ny(696f), 705f, Ny(826f));
            best.transform.SetSiblingIndex(view.GetSiblingIndex() + 1);
            Hide(box, "NightBestBox");
            if (Find(box, "NightV5Trophy") is Transform ot) Object.DestroyImmediate(ot.gameObject);
            Pic(box, "NightV5Trophy", $"{Kit}/icon_trophy.png", 44f, Ny(712f), 144f, Ny(812f));
            S(box, "NightBestLabel", 156f, Ny(710f), 520f, Ny(744f));
            Txt(box, "NightBestLabel", 24f, Ink, TextAlignmentOptions.Left, 0.25f);
            S(box, "NightBestText", 156f, Ny(740f), 600f, Ny(818f));
            Txt(box, "NightBestText", 66f, new Color(0.55f, 0.08f, 0.05f, 1f), TextAlignmentOptions.Left, 0.35f);

            // 조각 보상 · 오늘 입장
            var rew = Plate(box, "NightV5RewardPlate", $"{Kit}/panel_parch_big.png", 1.6f);
            Screen(rew.rectTransform, 15f, Ny(836f), 705f, Ny(1010f));
            rew.transform.SetSiblingIndex(best.transform.GetSiblingIndex() + 1);
            Hide(box, "NightRewardBox");
            S(box, "NightRewardLabel", 36f, Ny(848f), 540f, Ny(888f));
            Txt(box, "NightRewardLabel", 28f, Ink, TextAlignmentOptions.Left, 0.35f);
            S(box, "NightRewardText", 36f, Ny(892f), 540f, Ny(1000f));
            if (Txt(box, "NightRewardText", 19f, Ink, TextAlignmentOptions.TopLeft, 0.15f) is TextMeshProUGUI rt) rt.textWrappingMode = TextWrappingModes.Normal;
            var ent = Plate(box, "NightV5EntryPlate", $"{Kit}/panel_parch_wide.png", 2.4f);
            Screen(ent.rectTransform, 548f, Ny(848f), 698f, Ny(1000f));
            ent.transform.SetSiblingIndex(rew.transform.GetSiblingIndex() + 1);
            S(box, "NightEntryLabel", 556f, Ny(860f), 690f, Ny(892f));
            Txt(box, "NightEntryLabel", 20f, Ink, TextAlignmentOptions.Center, 0.2f);
            S(box, "NightEntryText", 556f, Ny(892f), 690f, Ny(956f));
            Txt(box, "NightEntryText", 50f, new Color(0.55f, 0.08f, 0.05f, 1f), TextAlignmentOptions.Center, 0.35f);
            S(box, "NightBuyButton", 562f, Ny(960f), 684f, Ny(994f));
            Spr9(box, "NightBuyButton", $"{Kit}/btn_navy.png", 3f);

            // 도전
            S(box, "NightStartButton", 30f, Ny(1022f), 690f, Ny(1126f));
            Spr9(box, "NightStartButton", $"{Kit}/btn_gold_wide.png", 1.2f);
            if (Find(box, "NightStartText")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI st) { st.color = Ink; Heavy(st, 0.4f); st.fontSize = st.fontSizeMax = 60f; }
        }

        // ── 망령의 탑 ─────────────────────────────────────────

        [MenuItem("Tools/Game/UI v5 — 망령의 탑 (mock_uiv3_tower)")]
        public static void RunTower()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[UI v5] 글자 본이 없다"); return; }
                var panel = lobby.transform.Find("TowerEntryPanel");
                var box = panel != null ? panel.Find("TowerContent") as RectTransform : null;
                if (box == null) { Debug.LogError("[UI v5] TowerContent 없음"); return; }
                Narrow(box);
                Tower(panel, box);
                UnderNav(lobby.transform, panel);
                var so = new SerializedObject(panel.GetComponent<TowerEntryPanel>());
                so.FindProperty("_cellPast").objectReferenceValue = Spr($"{Kit}/panel_navy_mid.png");
                so.FindProperty("_cellNext").objectReferenceValue = Spr($"{Kit}/panel_navy_mid.png");
                so.FindProperty("_cellNow").objectReferenceValue = Spr($"{Kit}/card_sel.png");
                so.FindProperty("_cellBoss").objectReferenceValue = Spr($"{Kit}/card_sr.png");
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[UI v5] 망령의 탑");
        }

        private static void Tower(Transform panel, RectTransform box)
        {
            PageBg(panel, "TowerBg");
            Hide(box, "TowerTitleBar", "TowerWeekBar", "TowerEntryBox", "TowerStartHintText");
            BackButton(box, "TowerBackButton", "TowerBackText", Ny(110f), Ny(168f));
            BigTitle(box, "TowerTitleText", 86f, Ny(100f), 420f, Ny(186f), 60f);
            S(box, "TowerResetText", 86f, Ny(186f), 360f, Ny(214f));
            Txt(box, "TowerResetText", 18f, Color.white, TextAlignmentOptions.Left, 0.15f);
            S(box, "TowerWeekText", 400f, Ny(120f), 700f, Ny(160f));
            Txt(box, "TowerWeekText", 24f, Color.white, TextAlignmentOptions.Right, 0.2f);
            S(box, "TowerLastWeekText", 400f, Ny(160f), 700f, Ny(192f));
            Txt(box, "TowerLastWeekText", 18f, new Color(0.86f, 0.88f, 0.95f), TextAlignmentOptions.Right);

            // 왼쪽 탑 기둥 — 층 칸 5개를 얹는다
            if (Find(box, "TowerV5Column") is Transform oc) Object.DestroyImmediate(oc.gameObject);
            if (Spr($"{Art}/tower_column.png") != null)   // 그림이 아직 없으면 바탕만(흰 상자가 뜨지 않게)
            {
                var col = Pic(box, "TowerV5Column", $"{Art}/tower_column.png", 0f, Ny(196f), 330f, Ny(1135f));
                col.preserveAspect = true;
                col.transform.SetSiblingIndex(0);
            }
            float[] cy = { 400f, 530f, 660f, 790f, 920f };
            for (int i = 0; i < 5; i++)
            {
                string cn = $"TowerLadderCell{i}";
                bool now = i == 2;
                float w = now ? 176f : 156f, h = now ? 66f : 58f;
                S(box, cn, 165f - w * 0.5f, Ny(cy[i]) - h * 0.5f, 165f + w * 0.5f, Ny(cy[i]) + h * 0.5f);
                if (Find(box, cn)?.GetComponent<Image>() is Image ci) { ci.type = Image.Type.Sliced; ci.pixelsPerUnitMultiplier = 2.6f; ci.preserveAspect = false; }
                if (Find(box, cn) is RectTransform cell)
                {
                    Mid(cell, "TowerLadderFloorText", 6f, 0f, w - 40f, h - 10f);
                    Txt(cell, "TowerLadderFloorText", now ? 30f : 24f, now ? new Color(1f, 0.9f, 0.4f) : Color.white, TextAlignmentOptions.Center, 0.25f);
                    Hide(cell, "TowerLadderNameText");
                    // 「GO」 꼬리표는 런타임이 지금 층에서 다시 켠다 — 숫자를 덮지 않게 크기를 0 으로(지금 층은 금테로 읽힌다)
                    if (cell.Find("TowerLadderBadge") is Transform badge) badge.localScale = Vector3.zero;
                    Mid(cell, "TowerLadderCheck", w * 0.5f - 18f, 0f, 24f, 24f);
                    if (cell.Find("TowerLadderCheck")?.GetComponent<Image>() is Image ck) { ck.sprite = Spr($"{Kit}/icon_check.png"); ck.preserveAspect = true; }
                    Mid(cell, "TowerLadderBossIcon", -w * 0.5f + 20f, 0f, 30f, 30f);
                }
            }

            // 오른쪽 층 정보 판
            S(box, "TowerFloorCard", 334f, Ny(328f), 706f, Ny(1130f));
            Spr9(box, "TowerFloorCard", $"{Kit}/panel_parch_big.png", 1.4f);
            S(box, "TowerFloorNumText", 354f, Ny(344f), 690f, Ny(406f));
            Txt(box, "TowerFloorNumText", 42f, Ink, TextAlignmentOptions.Left, 0.35f);
            S(box, "TowerFloorNameText", 354f, Ny(408f), 690f, Ny(440f));
            Txt(box, "TowerFloorNameText", 22f, InkSoft, TextAlignmentOptions.Left, 0.2f);
            S(box, "TowerFloorDescText", 354f, Ny(442f), 690f, Ny(472f));
            Txt(box, "TowerFloorDescText", 16f, InkSoft, TextAlignmentOptions.Left);
            S(box, "TowerFloorAdvRow", 354f, Ny(478f), 690f, Ny(514f));
            foreach (var n in new[] { "TowerFloorAdvEnemyText", "TowerFloorAdvArrow", "TowerFloorAdvText" })
                Txt(box, n, 19f, Ink, TextAlignmentOptions.Left, 0.2f);
            S(box, "TowerFloorFitText", 354f, Ny(520f), 690f, Ny(546f));
            Txt(box, "TowerFloorFitText", 16f, InkSoft, TextAlignmentOptions.Left);
            for (int i = 0; i < 3; i++) S(box, $"TowerFitFace{i}Frame", 356f + i * 70f, Ny(550f), 420f + i * 70f, Ny(614f));
            S(box, "TowerFloorFitButton", 570f, Ny(554f), 690f, Ny(610f));
            Spr9(box, "TowerFloorFitButton", $"{Kit}/btn_navy.png", 2.6f);

            S(box, "TowerFirstClearText", 354f, Ny(624f), 690f, Ny(654f));
            Txt(box, "TowerFirstClearText", 19f, Ink, TextAlignmentOptions.Left, 0.25f);
            S(box, "TowerFirstClearGoldIcon", 360f, Ny(664f), 400f, Ny(704f));
            S(box, "TowerFirstClearGoldText", 404f, Ny(660f), 500f, Ny(708f));
            Txt(box, "TowerFirstClearGoldText", 28f, Ink, TextAlignmentOptions.Left, 0.3f);
            S(box, "TowerFirstClearFace", 510f, Ny(662f), 554f, Ny(706f));
            S(box, "TowerFirstClearShardText", 558f, Ny(660f), 690f, Ny(708f));
            Txt(box, "TowerFirstClearShardText", 22f, Ink, TextAlignmentOptions.Left, 0.25f);

            S(box, "TowerEntryLabel", 354f, Ny(722f), 438f, Ny(756f));
            Txt(box, "TowerEntryLabel", 17f, Ink, TextAlignmentOptions.Left, 0.2f);
            for (int i = 0; i < 3; i++) S(box, $"TowerEntryPip{i}", 440f + i * 24f, Ny(729f), 460f + i * 24f, Ny(749f));
            S(box, "TowerEntryText", 512f, Ny(720f), 560f, Ny(758f));
            Txt(box, "TowerEntryText", 22f, Ink, TextAlignmentOptions.Left, 0.3f);
            S(box, "TowerBuyButton", 562f, Ny(720f), 692f, Ny(758f));
            Spr9(box, "TowerBuyButton", $"{Kit}/btn_navy.png", 3f);
            if (Find(box, "TowerBuyButton") is RectTransform buy)
            {
                Mid(buy, "TowerBuyPlus", -54f, 0f, 16f, 26f);
                Txt(buy, "TowerBuyPlus", 18f, Color.white, TextAlignmentOptions.Center);
                Mid(buy, "TowerBuyGem", -36f, 0f, 22f, 20f);
                Mid(buy, "TowerBuyText", -22f, 0f, 84f, 28f, 0f);
                Txt(buy, "TowerBuyText", 16f, Color.white, TextAlignmentOptions.Left, 0.15f);
            }

            S(box, "TowerStartButton", 350f, Ny(768f), 690f, Ny(852f));
            Spr9(box, "TowerStartButton", $"{Kit}/btn_gold_wide.png", 1.4f);
            if (Find(box, "TowerStartText")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI st) { st.color = Ink; Heavy(st, 0.4f); st.fontSize = st.fontSizeMax = 40f; }

            // 소탕 — 받을 것까지(PD 2026-10-18)
            S(box, "TowerSweepBox", 346f, Ny(864f), 694f, Ny(1120f));
            Spr9(box, "TowerSweepBox", $"{Kit}/panel_navy_mid.png", 1.8f);
            S(box, "TowerSweepTitleText", 362f, Ny(876f), 560f, Ny(908f));
            Txt(box, "TowerSweepTitleText", 22f, Gold, TextAlignmentOptions.Left, 0.25f);
            S(box, "TowerSweepSubText", 560f, Ny(878f), 682f, Ny(906f));
            Txt(box, "TowerSweepSubText", 15f, Color.white, TextAlignmentOptions.Right);
            S(box, "TowerSweepGoldIcon", 362f, Ny(918f), 396f, Ny(952f));
            S(box, "TowerSweepGoldText", 402f, Ny(912f), 682f, Ny(958f));
            if (Txt(box, "TowerSweepGoldText", 20f, Color.white, TextAlignmentOptions.Left, 0.2f) is TextMeshProUGUI sg) sg.textWrappingMode = TextWrappingModes.Normal;
            S(box, "TowerSweepButton", 362f, Ny(966f), 678f, Ny(1036f));
            Spr9(box, "TowerSweepButton", $"{Kit}/btn_navy.png", 1.8f);
            if (Find(box, "TowerSweepButtonText")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI sb) { sb.color = Color.white; sb.fontSize = sb.fontSizeMax = 34f; Heavy(sb, 0.3f); }
            S(box, "TowerSweepHintText", 356f, Ny(1044f), 686f, Ny(1112f));
            if (Txt(box, "TowerSweepHintText", 15f, new Color(0.8f, 0.84f, 0.95f), TextAlignmentOptions.TopLeft) is TextMeshProUGUI sh) sh.textWrappingMode = TextWrappingModes.Normal;
        }

        // ── 상점 ─────────────────────────────────────────────

        [MenuItem("Tools/Game/UI v5 — 상점 (mock_uiv2_c_shop)")]
        public static void RunShop()
        {
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[UI v5] 글자 본이 없다"); return; }
                var panel = lobby.transform.Find("LobbyShopPanel");
                var box = panel != null ? panel.Find("ShopContent") as RectTransform : null;
                if (box == null) { Debug.LogError("[UI v5] ShopContent 없음"); return; }
                Narrow(box);
                Shop(box);
                var so = new SerializedObject(panel.GetComponent<LobbyShopPanel>());
                so.FindProperty("_v5").boolValue = true;
                so.FindProperty("_tab").objectReferenceValue = Spr($"{Kit}/tab_off.png");
                so.FindProperty("_tabSelected").objectReferenceValue = Spr($"{Kit}/tab_on.png");
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[UI v5] 상점");
        }

        private static void Shop(RectTransform box)
        {
            if (box.Find("ShopBackdrop")?.GetComponent<Image>() is Image bd)
            {
                bd.sprite = Spr($"{Art}/page_bg.png");
                bd.preserveAspect = false;
                C(bd.rectTransform, 360f, 640f, 1000f, 1778f);
            }
            int at = box.Find("ShopBackdrop") is Transform b0 ? b0.GetSiblingIndex() + 1 : 0;
            Hide(box, "ShopHeader", "ShopHeaderIcon", "ShopGoldPill", "ShopGemPill");

            // 머리 — 달밤 그림 · 유령 · 「상점」
            var view = Find(box, "ShopV5Head") as RectTransform;
            if (view != null) Object.DestroyImmediate(view.gameObject);
            // 머리는 한 줄(120~250) — 탭은 예전 자리(190~242)에 둔다. 상자 · 재화 탭 화면이 그 자리 기준으로 짜여 있다
            view = Node(box, "ShopV5Head", -120f, 120f, 840f, 250f);
            view.SetSiblingIndex(at);
            view.gameObject.AddComponent<RectMask2D>();
            var art = Pic(view, "ShopV5HeadArt", $"{Art}/showcase_bg.png", 0, 0, 0, 0);
            art.preserveAspect = false;
            Envelope(art, 1600f / 830f);
            if (Find(box, "ShopV5Ghost") is Transform og) Object.DestroyImmediate(og.gameObject);
            Pic(box, "ShopV5Ghost", $"{Gr}/Portraits/portrait_ghost.png", 590f, 118f, 690f, 190f).transform.SetSiblingIndex(at + 1);
            BigTitle(box, "ShopTitleText", 26f, 124f, 400f, 186f, 50f);

            // 탭
            float[] tx0 = { 20f, 250f, 474f }, tx1 = { 246f, 470f, 700f };
            string[] tabs = { "ShopTabToday", "ShopTabChest", "ShopTabCurrency" };
            string[] labels = { "TabLabelToday", "TabLabelChest", "TabLabelCurrency" };
            for (int i = 0; i < 3; i++)
            {
                S(box, tabs[i], tx0[i], 192f, tx1[i], 244f);
                Spr9(box, tabs[i], $"{Kit}/tab_off.png", 2f);
                if (Find(box, tabs[i]) is RectTransform tb)
                {
                    Mid(tb, "TabIcon", -66f, 0f, 30f, 28f);
                    Mid(tb, labels[i], 14f, 0f, 140f, 34f);
                    Txt(tb, labels[i], 22f, Color.white, TextAlignmentOptions.Center, 0.2f);
                }
            }

            // 오늘의 상점 — 양피지 판 · 2열 큰 카드 3줄(시안)
            S(box, "TodayFrame", 14f, 252f, 706f, 1162f);
            Spr9(box, "TodayFrame", $"{Kit}/panel_parch_big.png", 1.4f);
            S(box, "TodayTitle", 36f, 262f, 420f, 302f);
            Txt(box, "TodayTitle", 30f, Ink, TextAlignmentOptions.Left, 0.35f);
            S(box, "TodayTimer", 36f, 302f, 420f, 326f);
            Txt(box, "TodayTimer", 16f, InkSoft, TextAlignmentOptions.Left);
            S(box, "TodayRefresh", 470f, 262f, 690f, 302f);
            Spr9(box, "TodayRefresh", $"{Kit}/btn_navy.png", 2.6f);
            S(box, "TodayRefreshLeft", 470f, 304f, 690f, 326f);
            Txt(box, "TodayRefreshLeft", 15f, InkSoft, TextAlignmentOptions.Right);
            for (int i = 0; i < 6; i++)
            {
                float x0 = i % 2 == 0 ? 28f : 366f, y0 = 336f + (i / 2) * 274f;
                S(box, $"DailySlot{i}", x0, y0, x0 + 326f, y0 + 264f);
                if (Find(box, $"DailySlot{i}") is not RectTransform slot) continue;
                Spr9(slot, "SlotFrame", $"{Kit}/panel_parch_big.png", 2f);
                Spr9(slot, "SlotFrameFree", $"{Kit}/panel_parch_big.png", 2f);
                Mid(slot, "SlotName", 0f, 104f, 300f, 30f);
                Txt(slot, "SlotName", 20f, Ink, TextAlignmentOptions.Center, 0.3f);
                Mid(slot, "SlotArt", -84f, 10f, 140f, 130f);
                Mid(slot, "SlotLock", -84f, 10f, 40f, 52f);
                Mid(slot, "SlotCount", 70f, 22f, 160f, 48f);
                Txt(slot, "SlotCount", 32f, Ink, TextAlignmentOptions.Center, 0.35f);
                Mid(slot, $"DailySlot{i}Buy", 0f, -100f, 290f, 46f);
                Spr9(slot, $"DailySlot{i}Buy", $"{Kit}/btn_gold_small.png", 2.6f);
                Mid(slot, "SlotNew", -128f, 104f, 64f, 56f);
                if (slot.Find("SlotSold") is RectTransform sold)
                {
                    sold.anchorMin = Vector2.zero; sold.anchorMax = Vector2.one; sold.offsetMin = sold.offsetMax = Vector2.zero;
                }
            }

            // 상자 · 재화 탭 — 판 그림만 키트로(톤 통일). 자리 · 글자는 그대로
            foreach (var t in box.GetComponentsInChildren<Image>(true))
            {
                if (t.sprite == null) continue;
                string n = t.sprite.name;
                if (n == "ch_s22e_box") { t.sprite = Spr($"{Kit}/panel_navy_mid.png"); t.type = Image.Type.Sliced; t.pixelsPerUnitMultiplier = 2f; }
                else if (n == "btn_small") { t.sprite = Spr($"{Kit}/btn_navy.png"); t.type = Image.Type.Sliced; t.pixelsPerUnitMultiplier = 2.6f; }
            }
            ShopTabs(box);
        }

        /// <summary>
        /// 상점 상자 · 재화 탭(PD 2026-10-18) — 오늘의 상점과 같은 결: 겉판은 양피지(짙은 글자), 안 칸은 남색 + 금테(흰 글자),
        /// 사는 단추는 금 단추. 네온 하늘색 칸(gem_slot · starter_slot · gold_slot · btn_rate · btn_price)을 걷는다. 자리는 그대로.
        /// </summary>
        private static void ShopTabs(RectTransform box)
        {
            foreach (var page in new[] { "ShopPageChest", "ShopPageCurrency" })
            {
                if (Find(box, page) is not Transform pg) continue;
                foreach (var img in pg.GetComponentsInChildren<Image>(true))
                {
                    if (img.sprite == null) continue;
                    string sn = img.sprite.name, n = img.name;
                    bool outer = n == "RowPlate" || n == "CardPlate" || n == "StarterPlate" || n == "GemSection" || n == "GoldSection"
                                 || (n == "Plate" && img.transform.parent != null && img.transform.parent.name == "ChapterPack");
                    string to = outer ? $"{Kit}/panel_parch_big.png"
                              : sn is "gem_slot" or "starter_slot" or "gold_slot" or "panel_navy_mid" ? $"{Kit}/panel_navy_mid.png"
                              : sn is "btn_rate" or "btn_navy" ? $"{Kit}/btn_navy.png"
                              : sn == "btn_price" ? $"{Kit}/btn_gold_small.png"
                              : null;
                    if (to == null) continue;
                    img.sprite = Spr(to);
                    img.type = Image.Type.Sliced;
                    img.pixelsPerUnitMultiplier = outer ? 1.6f : sn == "btn_price" ? 2.8f : 2.4f;
                    img.preserveAspect = false;
                    img.color = Color.white;
                }
                // 양피지 위 글자는 짙게
                foreach (var t in pg.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    switch (t.name)
                    {
                        case "RowTitle": case "CardTitle": case "StarterHead": case "ChapterPackTitle": case "GemHead": case "GoldHead":
                            t.color = Ink; Heavy(t, 0.3f); break;
                        case "RowPrice":   // 상자 값 — 단추 판이 없어 양피지 위에 바로 앉는다
                            t.color = Ink; Heavy(t, 0.3f); break;
                        case "CardDesc": case "ChapterPackDesc":
                            t.color = InkSoft; break;
                        case "ShopLegalText": case "ShopRestoreText":
                            t.color = InkSoft; break;
                    }
                }
            }
        }

        private static void BindSortie(ChapterHostPanel panel)
        {
            if (panel == null) return;
            var so = new SerializedObject(panel);
            // 호스트 카드 테 — 키트 등급 테(R · SR · SSR) · 고른 카드 테
            var frames = so.FindProperty("_gradeFrames");
            if (frames != null)
            {
                string[] f = { "card_r", "card_sr", "card_ssr" };
                frames.arraySize = f.Length;
                for (int i = 0; i < f.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = Spr($"{Kit}/{f[i]}.png");
            }
            if (so.FindProperty("_startRowCenter") is SerializedProperty rowCenter) rowCenter.floatValue = 150f;   // 「도전하기 | 비용」 오른쪽 절반
            if (so.FindProperty("_v5Info") is SerializedProperty v5) v5.boolValue = true;
            if (so.FindProperty("_cardFrameSelected") is SerializedProperty sel) sel.objectReferenceValue = Spr($"{Kit}/card_sel.png");
            if (so.FindProperty("_cardFrame") is SerializedProperty fr) fr.objectReferenceValue = Spr($"{Kit}/card_r.png");
            // 스킬 띠 필살기 아이콘 — 육성 화면과 같은 그림(ulticon_{key})
            var arts = so.FindProperty("_hostArts");
            for (int i = 0; arts != null && i < arts.arraySize; i++)
            {
                var e = arts.GetArrayElementAtIndex(i);
                string key = e.FindPropertyRelative("Key").stringValue;
                e.FindPropertyRelative("Ult").objectReferenceValue = Spr($"Assets/BaseResource/HostSelectPanel/ulticon_{key}.png");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
