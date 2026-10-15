using Game.Module.InGame;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 추가 모드(망령의 탑 · 망령의 밤) 인게임 부품을 `InGameMainUI` 프리팹에 세운다(PD 2026-10-16).
    /// 시안: `_exchange/in/mock_mode_T5.png`(탑 판 · 층 소개 띠) · `N3`(밤 판 · 무리 칩 · 유리 몸 칩)
    ///       `T8`(탑 결과 — 챕터 결과 창 틀 그대로) · `N7`(밤 결과).
    /// 좌표는 시안 720×1280 의 왼쪽 위 기준 상자(x0, y0, x1, y1)를 그대로 적는다 — 손으로 환산해 흩뿌리지 않는다.
    /// 노드 이름 = 바인딩 키(`InGameMainUI.Modes` · `TowerResultPopup` · `NightResultPopup`).
    /// </summary>
    public static class ModeInGameBuilder
    {
        private const string InGamePrefab = "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab";
        private const string Base = "Assets/BaseResource/";
        private const string BoldFont = "Assets/BaseResource/Fonts/NotoSansKR-Bold SDF.asset";

        private static TMP_FontAsset s_font;
        private static Material s_outline;
        private static Material s_plain;

        private static readonly Color Gold = new(1f, 0.82f, 0.27f);
        private static readonly Color Cyan = new(0.45f, 0.9f, 1f);
        private static readonly Color Dim = new(0.72f, 0.8f, 0.92f);
        private static readonly Color Dark = new(0.12f, 0.1f, 0.05f);

        [MenuItem("Tools/Game/모드 UI/인게임 세우기")]
        public static void Run()
        {
            var root = PrefabUtility.LoadPrefabContents(InGamePrefab);
            try
            {
                PickFont(root);
                var top = root.transform.Find("TopHudGroup");
                BuildPlates(top);
                BuildOverlay(root.transform);
                BuildTowerResult(root.transform);
                BuildNightResult(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, InGamePrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("[모드 UI] 인게임 — 탑 판 · 밤 판 · 소개 띠 · 결과 창 둘 세움");
        }

        // ── 위 가운데 판 (T5 · N3) ──────────────────────────────
        // `TopHudGroup` 은 화면 위 가운데에 붙은 720 폭 판이라 시안 좌표가 그대로 맞는다.

        private static void BuildPlates(Transform top)
        {
            var tower = Fresh(top, "ModeTowerPlate");
            Box(Img(tower, Sp("InGameMainUI/tower_plate")).rectTransform, 292, 0, 428, 128);
            Txt(tower, "ModeTowerLabel", "TOWER", 18, Cyan, Rel(292, 0, 304, 34, 416, 54));
            Txt(tower, "ModeTowerFloorText", "13F", 40, Color.white, Rel(292, 0, 304, 54, 416, 94));
            Txt(tower, "ModeTowerEnemyText", "남은 적 9", 16, Dim, Rel(292, 0, 304, 94, 416, 114));
            tower.gameObject.SetActive(false);

            var night = Fresh(top, "ModeNightPlate");
            Box(Img(night, Sp("InGameMainUI/night_plate")).rectTransform, 288, 24, 432, 130);
            Txt(night, "ModeNightLabel", "NIGHT", 18, new Color(0.78f, 0.72f, 1f), Rel(288, 24, 312, 32, 394, 52));
            Txt(night, "ModeNightTimeText", "0:00", 40, Color.white, Rel(288, 24, 300, 54, 420, 96));
            Txt(night, "ModeNightKillText", "처치 0", 16, Dim, Rel(288, 24, 300, 98, 420, 120));
            night.gameObject.SetActive(false);

            // 무리 칩 「무리: 마법」 · 다음 일 「대형 호스트 0:12」
            var chip = Fresh(top, "ModeWaveChip");
            Box(Img(chip, Sp("InGameMainUI/mode_rule_chip")).rectTransform, 280, 137, 440, 183);
            var gem = Img(Node(chip, "ModeWaveIcon"), Sp("InGameMainUI/rps_gem_magic"));
            gem.preserveAspect = true;
            Rel(280, 137, 292, 144, 324, 176)(gem.rectTransform);
            Txt(chip, "ModeWaveText", "무리: 마법", 22, Color.white, Rel(280, 137, 328, 144, 432, 176));
            chip.gameObject.SetActive(false);

            var next = Fresh(top, "ModeNextText");
            Box(next, 280, 186, 440, 212);
            var nt = next.gameObject.AddComponent<TextMeshProUGUI>();
            Style(nt, "대형 호스트 0:12", 18, Gold);
            next.gameObject.SetActive(false);
        }

        // ── 소개 띠 · 유리 몸 칩 (T5 · N3) ──────────────────────
        // 시안 한 장 판(720×1280)을 화면 가운데 — 긴 폰 · 태블릿에서도 시안 자리 그대로.

        private static void BuildOverlay(Transform root)
        {
            var overlay = Fresh(root, "ModeOverlay");
            Full(overlay);
            var box = Node(overlay, "ModeOverlayContent");
            Box(box, 0, 0, 720, 1280, center: true);
            box.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();

            var band = Node(box, "ModeBanner");
            Box(Img(band, Sp("InGameMainUI/mode_intro_band")).rectTransform, 0, 405, 720, 612);
            var title = Txt(band, "ModeBannerTitle", "13층", 44, Color.white, Rel(0, 405, 120, 432, 600, 490));
            Gradient(title);
            Txt(band, "ModeBannerName", "마법의 무리", 32, Color.white, Rel(0, 405, 80, 500, 640, 544));
            Txt(band, "ModeBannerSub", "적 전부 마법 · 무기 몸 유리 ▲", 22, new Color(0.62f, 1f, 0.62f),
                Rel(0, 405, 60, 554, 660, 590));
            band.gameObject.SetActive(false);

            var counter = Node(box, "ModeCounterChip");
            Box(Img(counter, Sp("InGameMainUI/mode_rule_chip")).rectTransform, 270, 1018, 450, 1060);
            Txt(counter, "ModeCounterText", "무기 몸이 오면 ▲", 20, new Color(0.62f, 1f, 0.62f), Rel(270, 1018, 282, 1022, 438, 1056));
            counter.gameObject.SetActive(false);

            // 띠 · 칩은 결과 창 · 카드 창보다 아래 — 알림 줄(StageText) 바로 뒤에 둔다
            var stage = root.Find("StageText");
            if (stage != null) overlay.SetSiblingIndex(stage.GetSiblingIndex() + 1);
        }

        // ── 탑 결과 (T8 클리어 · T9 실패) — 챕터 결과 창 틀 그대로 ───

        private static void BuildTowerResult(Transform root)
        {
            var popup = Fresh(root, "TowerResultPopup");
            Full(popup);
            var dim = Img(Node(popup, "TowerResultDim"), null);
            dim.color = new Color(0f, 0f, 0f, 0.88f);
            dim.raycastTarget = true;
            Full(dim.rectTransform);
            var box = Node(popup, "TowerResultContent");
            Box(box, 0, 0, 720, 1280, center: true);
            box.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();

            Box(Img(Node(box, "TowerResultFrame"), Sp("ChapterScreens/result_frame")).rectTransform, 30, 220, 690, 1020);
            var title = Txt(box, "TowerResultTitle", "13F CLEAR", 50, Color.white, Rel(0, 0, 150, 410, 570, 466));
            Gradient(title);
            Txt(box, "TowerResultSub", "마법의 무리", 22, Color.white, Rel(0, 0, 210, 482, 510, 510));

            // 위 칸 — 첫 클리어 보상(클리어) · 맞는 키운 몸(실패)
            var row1 = Img(Node(box, "TowerResultRow1"), Sp("ChapterScreens/result_rowpanel")).rectTransform;
            Box(row1, 130, 508, 590, 648);
            Txt(row1, "TowerResultRow1Label", "첫 클리어 보상 (이번 주)", 20, Dim, Rel(130, 508, 154, 524, 570, 548), TextAlignmentOptions.MidlineLeft);
            var coin = Img(Node(row1, "TowerResultCoin"), Sp("ChapterHost/ch_icon_coin"));
            coin.preserveAspect = true;
            Rel(130, 508, 152, 572, 196, 616)(coin.rectTransform);
            Txt(row1, "TowerResultGoldText", "+50", 30, Color.white, Rel(130, 508, 204, 574, 290, 614), TextAlignmentOptions.MidlineLeft);
            for (int i = 0; i < 3; i++)
            {
                float x = 286 + i * 100;
                var frame = Img(Node(row1, $"TowerResultFace{i}Frame"), Sp("ChapterHost/ch_thumbframe")).rectTransform;
                Rel(130, 508, x, 562, x + 56, 618)(frame);
                var face = Img(Node(frame, $"TowerResultFace{i}"), null);
                face.preserveAspect = true;
                Inset(face.rectTransform, 4);
                Txt(frame, $"TowerResultFace{i}Text", "×1", 22, Color.white, Rel(x, 556, x + 58, 556, x + 100, 612),
                    TextAlignmentOptions.MidlineLeft);
            }
            Txt(row1, "TowerResultRow1Text", "", 22, Color.white, Rel(130, 508, 154, 562, 570, 618), TextAlignmentOptions.MidlineLeft);

            // 아래 칸 — 다음 층(클리어) · 어떻게 하면 쉬워지나(실패)
            var row2 = Img(Node(box, "TowerResultRow2"), Sp("ChapterScreens/result_rowpanel")).rectTransform;
            Box(row2, 130, 656, 590, 796);
            Txt(row2, "TowerResultRow2Label", "다음 층", 20, Dim, Rel(130, 656, 154, 672, 570, 698), TextAlignmentOptions.MidlineLeft);
            Txt(row2, "TowerResultRow2Name", "14층 · 첫 몸으로 끝까지", 28, Color.white, Rel(130, 656, 154, 704, 570, 740),
                TextAlignmentOptions.MidlineLeft);
            Txt(row2, "TowerResultRow2Desc", "스스로 나올 수 없다", 18, Dim, Rel(130, 656, 154, 746, 570, 772), TextAlignmentOptions.MidlineLeft);

            // 오늘 입장 ●●○ 1/3
            Txt(box, "TowerResultEntryLabel", "오늘 입장", 22, Color.white, Rel(0, 0, 200, 816, 320, 848), TextAlignmentOptions.MidlineRight);
            for (int i = 0; i < 3; i++)
                Box(Img(Node(box, $"TowerResultPip{i}"), Sp("ModeCommon/mode_entry_pip_on")).rectTransform,
                    334 + i * 30, 820, 358 + i * 30, 844);
            Txt(box, "TowerResultEntryText", "1 / 3", 22, Color.white, Rel(0, 0, 430, 816, 520, 848), TextAlignmentOptions.MidlineLeft);

            var leave = Img(Node(box, "TowerResultLeaveButton"), Sp("ChapterHost/ch_pill")).rectTransform;
            Box(leave, 108, 898, 300, 974);
            Btn(leave);
            Txt(leave, "TowerResultLeaveText", "나가기", 30, Color.white, Fill);
            var go = Img(Node(box, "TowerResultGoButton"), Sp("ChapterScreens/button_yellow_wide")).rectTransform;
            Box(go, 318, 894, 614, 978);
            Btn(go);
            var goText = Txt(go, "TowerResultGoText", "14층 도전 ▶", 30, Dark, Fill, plain: true);
            goText.color = Dark;

            popup.gameObject.AddComponent<TowerResultPopup>();
            popup.gameObject.SetActive(false);
        }

        // ── 밤 결과 (N7) ───────────────────────────────────────

        private static void BuildNightResult(Transform root)
        {
            var popup = Fresh(root, "NightResultPopup");
            Full(popup);
            var dim = Img(Node(popup, "NightResultDim"), null);
            dim.color = new Color(0f, 0f, 0f, 0.88f);
            dim.raycastTarget = true;
            Full(dim.rectTransform);
            var box = Node(popup, "NightResultContent");
            Box(box, 0, 0, 720, 1280, center: true);
            box.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();

            Box(Img(Node(box, "NightResultFrame"), Sp("NightResultPopup/night_result_frame")).rectTransform, 29, 197, 691, 1073);
            var title = Txt(box, "NightResultTitle", "밤을 버텼다", 48, Color.white, Rel(0, 0, 160, 346, 560, 406));
            Gradient(title);
            Txt(box, "NightResultSub", "화 · 원거리의 날 · 보스 크러셔 처치", 20, Color.white, Rel(0, 0, 180, 432, 540, 462));

            var kills = Img(Node(box, "NightResultKillBox"), Sp("ModeCommon/mode_frame")).rectTransform;
            Box(kills, 115, 478, 605, 662);
            Txt(kills, "NightResultKillLabel", "처치", 20, Dim, Rel(115, 478, 115, 492, 605, 516));
            Txt(kills, "NightResultKillText", "284", 76, Color.white, Rel(115, 478, 115, 516, 605, 598));
            Txt(kills, "NightResultBestText", "최고 기록! (이전 238)", 22, new Color(1f, 0.45f, 0.55f), Rel(115, 478, 115, 606, 605, 644));

            var reward = Img(Node(box, "NightResultRewardBox"), Sp("ModeCommon/mode_frame")).rectTransform;
            Box(reward, 115, 672, 605, 802);
            Txt(reward, "NightResultRewardLabel", "보상 — 처치 50마다 1 + 보스 처치 3", 18, Dim, Rel(115, 672, 135, 684, 585, 710),
                TextAlignmentOptions.MidlineLeft);
            for (int i = 0; i < 3; i++)
            {
                float x = 140 + i * 132;
                var frame = Img(Node(reward, $"NightResultFace{i}Frame"), Sp("ChapterHost/ch_thumbframe")).rectTransform;
                Rel(115, 672, x, 718, x + 64, 782)(frame);
                var face = Img(Node(frame, $"NightResultFace{i}"), null);
                face.preserveAspect = true;
                Inset(face.rectTransform, 4);
                Txt(frame, $"NightResultFace{i}Text", "×3", 24, Color.white, Rel(x, 718, x + 68, 718, x + 120, 782),
                    TextAlignmentOptions.MidlineLeft);
            }
            Txt(reward, "NightResultRewardEmpty", "", 20, Dim, Rel(115, 672, 135, 718, 585, 782));

            Txt(box, "NightResultStatText", "버틴 시간 5:12 · Lv 10 · 빙의 4 · 몸 잃음 2", 18, Dim, Rel(0, 0, 120, 816, 600, 846));
            Txt(box, "NightResultEntryLabel", "오늘 입장", 22, Color.white, Rel(0, 0, 200, 860, 320, 892), TextAlignmentOptions.MidlineRight);
            for (int i = 0; i < 3; i++)
                Box(Img(Node(box, $"NightResultPip{i}"), Sp("ModeCommon/mode_entry_pip_on")).rectTransform,
                    334 + i * 30, 864, 358 + i * 30, 888);
            Txt(box, "NightResultEntryText", "2 / 3", 22, Color.white, Rel(0, 0, 430, 860, 520, 892), TextAlignmentOptions.MidlineLeft);

            var lobby = Img(Node(box, "NightResultLobbyButton"), Sp("ChapterHost/ch_pill")).rectTransform;
            Box(lobby, 120, 935, 316, 1015);
            Btn(lobby);
            Txt(lobby, "NightResultLobbyText", "로비", 30, Color.white, Fill);
            var again = Img(Node(box, "NightResultAgainButton"), Sp("ChapterScreens/button_yellow_wide")).rectTransform;
            Box(again, 334, 931, 612, 1019);
            Btn(again);
            var againText = Txt(again, "NightResultAgainText", "다시 ▶", 32, Dark, Fill, plain: true);
            againText.color = Dark;

            popup.gameObject.AddComponent<NightResultPopup>();
            popup.gameObject.SetActive(false);
        }

        // ── 도우미 ────────────────────────────────────────────

        private static Sprite Sp(string path) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Base}{path}.png");

        /// <summary>
        /// 시안 글자는 굵다 — 굵은 글꼴(NotoSansKR-Bold)과 그 테두리 재질을 쓴다.
        /// 프리팹에서 처음 만난 「Outline」 글자를 집었더니 보통 굵기 글꼴이라 시안보다 가늘고 흐렸다(2026-10-16).
        /// </summary>
        private static void PickFont(GameObject root)
        {
            s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFont);
            s_outline = null;
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.font != s_font || !t.fontSharedMaterial.name.Contains("Outline")) continue;
                s_outline = t.fontSharedMaterial;
                break;
            }
            if (s_outline == null)
                foreach (var guid in AssetDatabase.FindAssets("NotoSansKR-Bold SDF t:Material"))
                {
                    var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (m != null && m.name.Contains("Outline")) { s_outline = m; break; }
                }
            s_plain = s_font != null ? s_font.material : null;
            if (s_outline == null) s_outline = s_plain;
        }

        private static RectTransform Fresh(Transform root, string name)
        {
            var old = root.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            return Node(root, name);
        }

        private static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image Img(Transform node, Sprite sprite)
        {
            var img = node.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        private static readonly System.Action<RectTransform> Fill = r =>
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(6, 2);
            r.offsetMax = new Vector2(-6, -2);
        };

        private static TextMeshProUGUI Txt(Transform parent, string name, string text, float size, Color color,
                                           System.Action<RectTransform> place,
                                           TextAlignmentOptions align = TextAlignmentOptions.Center, bool plain = false)
        {
            var node = Node(parent, name);
            var t = node.gameObject.AddComponent<TextMeshProUGUI>();
            Style(t, text, size, color, align, plain);
            place(node);
            return t;
        }

        private static void Style(TextMeshProUGUI t, string text, float size, Color color,
                                  TextAlignmentOptions align = TextAlignmentOptions.Center, bool plain = false)
        {
            t.font = s_font;
            t.fontSharedMaterial = plain ? s_plain : s_outline;
            t.fontSize = size;
            t.text = text;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.fontStyle = FontStyles.Bold;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            // 일본어 · 영어가 더 길다 — 칸을 넘으면 줄인다(06_ui 「글자는 삐져나가지 않는다」)
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Max(10, size * 0.55f);
        }

        /// <summary>시안의 금빛 제목 — 위 밝은 금 → 아래 주황 금(챕터 결과 창 제목과 같은 값).</summary>
        private static void Gradient(TextMeshProUGUI t)
        {
            t.color = Color.white;
            t.enableVertexGradient = true;
            t.colorGradient = new VertexGradient(new Color(1f, 0.96f, 0.62f), new Color(1f, 0.96f, 0.62f),
                                                 new Color(1f, 0.66f, 0.12f), new Color(1f, 0.66f, 0.12f));
        }

        private static void Btn(Transform node)
        {
            var g = node.GetComponent<Graphic>();
            if (g == null) g = node.gameObject.AddComponent<Image>();
            g.raycastTarget = true;
            var b = node.gameObject.AddComponent<Button>();
            b.targetGraphic = g;
        }

        private static void Full(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private static void Inset(RectTransform r, float px)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(px, px);
            r.offsetMax = new Vector2(-px, -px);
        }

        /// <summary>
        /// 시안 상자(왼쪽 위 기준) → 부모 안 자리. 부모가 시안 판(720×1280)이면 <paramref name="center"/> 로 판 가운데,
        /// 아니면 부모의 왼쪽 위(앵커 0,1)를 시안 (0,0) 으로 본다.
        /// </summary>
        private static void Box(RectTransform r, float x0, float y0, float x1, float y1, bool center = false)
        {
            if (center)
            {
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(x1 - x0, y1 - y0);
                return;
            }
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x0, -y0);
            r.sizeDelta = new Vector2(x1 - x0, y1 - y0);
        }

        /// <summary>부모가 시안 상자 (px0, py0) 에서 시작할 때, 자식 시안 상자를 부모 안 자리로.</summary>
        private static System.Action<RectTransform> Rel(float px0, float py0, float x0, float y0, float x1, float y1)
            => r => Box(r, x0 - px0, y0 - py0, x1 - px0, y1 - py0);
    }
}
