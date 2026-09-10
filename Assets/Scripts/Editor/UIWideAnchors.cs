using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 화면이 기준(720)보다 넓을 때 UI 가 가운데 720 칸에만 몰리는 것을 푼다 (2026-09-10).
    ///
    /// ── 무엇이 문제였나 ──────────────────────────────────────
    /// 기준 해상도는 720×1280 인데, 잘리지 않게 맞추는 규칙(contain, `SafeArea.ApplyCanvasMatch`)
    /// 때문에 태블릿 4:3 에서는 **보이는 가로 칸이 960** 이 된다.
    /// 그런데 UI 덩어리가 전부 720 폭 고정이라 좌우로 120 칸씩 빈 채로 남았다 —
    /// 화면이 잘린 것처럼 보인다는 지적이 여기서 나왔다.
    ///
    /// 배경은 이미 늘어나 있었다(로비·호스트선택 배경 모두 스트레치). 그림이 아니라
    /// **앵커** 문제다.
    ///
    /// ── 규칙 ────────────────────────────────────────────────
    /// 화면을 꽉 채워야 하는 크롬은 가로 스트레치(0~1)로 두고, 그 안에서
    /// **오른쪽에 붙어야 할 것은 오른쪽 앵커(1~1)로** 옮긴다. 왼쪽 것은 그대로 둔다.
    /// 창(레벨업·상점 등)과 플레이 필드는 720 그대로 가운데 둔다 — 방은 10 m × 13 m 고정이라
    /// 늘리면 없는 방을 보여주는 셈이 된다.
    ///
    /// ⚠ **이 패스는 `UILayoutApplier` 뒤에 반드시 다시 돌아야 한다.**
    ///   목업 적용기는 모든 노드를 top-left 앵커로 정규화하므로, 여기서 잡은 앵커를
    ///   통째로 지운다. 그래서 `UILayoutApplier.Run()` 끝에서 이 클래스를 부른다.
    ///   손으로 프리팹을 고쳐 놓으면 다음 적용 때 조용히 날아간다 — 그래서 코드로 둔다.
    /// </summary>
    public static class UIWideAnchors
    {
        /// <summary>레이아웃을 잰 기준 폭. 모든 좌표가 이 폭 위에서 찍혔다.</summary>
        private const float BaseWidth = 720f;

        private enum Mode
        {
            /// <summary>가로로 화면 끝까지 늘린다.</summary>
            Stretch,
            /// <summary>오른쪽 끝에 붙인다. 720 기준 오른쪽 여백을 그대로 지킨다.</summary>
            Right,
            /// <summary>왼쪽 끝에 붙인다. 720 기준 왼쪽 여백을 그대로 지킨다.</summary>
            Left,
        }

        private readonly struct Rule
        {
            public readonly string Name;
            public readonly Mode Mode;
            public Rule(string name, Mode mode) { Name = name; Mode = mode; }
        }

        // ── 인게임 ───────────────────────────────────────────────
        //
        // 상단 HUD 와 하단 조작바는 화면 크롬이다 — 끝까지 간다.
        // 플레이 필드(`RoomField`)와 네 창은 손대지 않는다(720 가운데 유지).
        private static readonly Rule[] InGame =
        {
            new Rule("TopHudGroup",      Mode.Stretch),
            new Rule("HudBackdrop",      Mode.Stretch),
            new Rule("HudRow2",          Mode.Stretch),
            new Rule("BossGroup",        Mode.Stretch),
            new Rule("RunResourcePanel", Mode.Right),
            new Rule("PauseButton",      Mode.Right),

            new Rule("ControlGroup", Mode.Stretch),
            new Rule("ControlGrid",  Mode.Stretch),
            new Rule("ActionFrame",  Mode.Right),
            new Rule("ActionLabel",  Mode.Right),
            new Rule("ButtonRow",    Mode.Right),

            new Rule("StageText", Mode.Stretch),
        };

        // ── 로비 ─────────────────────────────────────────────────
        //
        // 하단 탭바(`FeatureTabBar`)와 3버튼(`MainActionBar`)은 **일부러 안 늘린다.**
        // 저것들은 떠 있는 덩어리라 가운데 정렬이 자연스럽고, 늘리면 버튼 사이가
        // 허옇게 벌어진다.
        private static readonly Rule[] Lobby =
        {
            new Rule("LobbyStageArea",   Mode.Stretch),
            new Rule("TopHudGroup",      Mode.Stretch),
            new Rule("TopHudBackground", Mode.Stretch),
            new Rule("StaminaCounter",   Mode.Right),
            new Rule("GoldCounter",      Mode.Right),
            new Rule("GemCounter",       Mode.Right),
            new Rule("MailButton",       Mode.Right),
            new Rule("SettingsButton",   Mode.Right),

            new Rule("ChapterCard",     Mode.Left),
            new Rule("SidePromoGroup",  Mode.Right),
        };

        // ── 호스트 선택 ──────────────────────────────────────────
        //
        // 두 단(왼쪽 목록 · 오른쪽 상세)을 화면 양끝으로 벌린다. 가운데가 넓어질 뿐
        // 각 단의 폭은 그대로라 글자 크기가 달라지지 않는다.
        private static readonly Rule[] HostSelect =
        {
            new Rule("HostSelectBackground", Mode.Stretch),
            new Rule("PopupParent",          Mode.Stretch),
            new Rule("HeaderGroup",          Mode.Stretch),
            new Rule("HeaderPortalDeco",     Mode.Right),

            new Rule("HostListTitle", Mode.Left),
            // ⚠ 프리팹에 있는 이름은 `HostGrid` 다. 런타임에 `HostGridScroll` 로 감싸는데
            //   그 껍데기가 이 앵커를 그대로 베껴 가므로(`WrapGridInScroll`) 여기만 잡으면 된다.
            new Rule("HostGrid", Mode.Left),

            new Rule("HostDetailCard",      Mode.Right),
            new Rule("HostUpgradeButton",   Mode.Right),
            new Rule("PossessStartButton",  Mode.Right),

            new Rule("TipBar",              Mode.Stretch),
            new Rule("OwnedHostChestIcon",  Mode.Right),
            new Rule("OwnedHostCountText",  Mode.Right),
        };

        private static readonly (string prefab, Rule[] rules)[] Targets =
        {
            ("Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab", InGame),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", Lobby),
            ("Assets/BundleResource/Prefabs/UI/HostSelect/HostSelectPanel.prefab", HostSelect),
        };

        [MenuItem("Tools/Game/넓은 화면 앵커 맞추기")]
        public static void Run()
        {
            int total = 0, missing = 0;
            foreach (var (prefabPath, rules) in Targets)
            {
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null) { Debug.LogError($"[WideAnchor] 로드 실패: {prefabPath}"); continue; }

                foreach (var rule in rules)
                {
                    var t = FindByName(root.transform, rule.Name);
                    if (t == null) { missing++; Debug.LogWarning($"[WideAnchor] 없음: {rule.Name} ({prefabPath})"); continue; }
                    Apply((RectTransform)t, rule.Mode);
                    total++;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[WideAnchor] 적용 {total}건 · 못 찾음 {missing}건");
        }

        /// <summary>
        /// 앵커를 바꿔도 **720 폭에서 보이던 모습은 그대로**여야 한다.
        /// 그래서 먼저 「720 기준 왼쪽 좌표」를 되찾은 뒤 새 앵커로 다시 적는다.
        ///
        /// 지금 앵커가 무엇이든 좌표를 되찾을 수 있으므로 **몇 번을 돌려도 결과가 같다.**
        /// </summary>
        private static void Apply(RectTransform rt, Mode mode)
        {
            float x720 = LeftAt720(rt);
            float width = rt.rect.width;

            switch (mode)
            {
                case Mode.Stretch:
                    rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
                    rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
                    rt.pivot = new Vector2(0f, rt.pivot.y);
                    rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
                    rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);
                    break;

                case Mode.Right:
                    rt.anchorMin = new Vector2(1f, rt.anchorMin.y);
                    rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
                    rt.pivot = new Vector2(0f, rt.pivot.y);
                    rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
                    rt.anchoredPosition = new Vector2(x720 - BaseWidth, rt.anchoredPosition.y);
                    break;

                case Mode.Left:
                    rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
                    rt.anchorMax = new Vector2(0f, rt.anchorMax.y);
                    rt.pivot = new Vector2(0f, rt.pivot.y);
                    rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
                    rt.anchoredPosition = new Vector2(x720, rt.anchoredPosition.y);
                    break;
            }
        }

        /// <summary>지금 앵커가 무엇이든, 이 노드의 왼쪽 변이 720 폭에서 어디였는지 돌려준다.</summary>
        private static float LeftAt720(RectTransform rt)
        {
            float x = rt.anchoredPosition.x;
            float width = rt.rect.width;

            // 스트레치는 부모를 통째로 채우고 있었다 — 720 기준 왼쪽은 0 이다.
            if (rt.anchorMin.x < 0.01f && rt.anchorMax.x > 0.99f) return 0f;

            // pivot 이 0 이 아니면 anchoredPosition 이 가리키는 것은 왼쪽 변이 아니다.
            float left = x - width * rt.pivot.x;

            if (rt.anchorMin.x > 0.99f) return left + BaseWidth;   // 오른쪽 앵커
            if (rt.anchorMin.x > 0.49f) return left + BaseWidth * 0.5f;   // 가운데 앵커
            return left;                                           // 왼쪽 앵커
        }

        private static Transform FindByName(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindByName(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
