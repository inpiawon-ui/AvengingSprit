using Game.Module.Common.UI;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// `ScreenFit` 을 UI 프리팹 루트에 달아 준다. 한 번 달면 끝이라 평소에는 쓸 일이 없다 —
    /// 새 `~UI` · `~Panel` 프리팹을 만들었을 때만 다시 돌린다.
    ///
    /// ⚠ 예전에는 에디터에서 **앵커 값을 구워** 넣었는데, `UILayoutApplier` 가 모든 노드를
    ///   top-left 로 정규화하면서 그 값을 통째로 지웠다. 지금은 런타임 컴포넌트가 제 몸을
    ///   맞추므로 목업을 다시 적용해도 안 풀린다 — 적용기는 RectTransform 만 건드린다.
    /// </summary>
    public static class ScreenFitInstaller
    {
        private static readonly string[] Roots =
        {
            "Assets/BundleResource/Prefabs/UI/Title/TitleMainUI.prefab",
            "Assets/BundleResource/Prefabs/UI/Opening/OpeningMainUI.prefab",
            "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab",
            "Assets/BundleResource/Prefabs/UI/HostSelect/HostSelectPanel.prefab",
            "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab",
        };

        /// <summary>부모의 남는 폭을 형제와 나눠 갖는 판. 「프리팹 : 노드 이름」.</summary>
        private static readonly (string prefab, string node)[] Shares =
        {
            // 상단 HUD 두 줄. 안 붙이면 1줄은 화면 끝까지 벌어지는데 2줄은 가로 레이아웃
            // 그룹이 가운데로 모아 두어 **줄마다 끝이 안 맞는다.**
            ("Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab", "PlayerSoulPanel"),
            ("Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab", "RunResourcePanel"),
            ("Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab", "CurrentHostPanel"),
            ("Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab", "ChapterGroup"),

            // 로비 — 태블릿 4:3 에서 판이 화면 끝까지 늘어나고, **그 안의 칸들이 남는 폭을
            // 나눠 갖는다.** 가운데로 모으면 좌우 120 px 씩이 그냥 빈다(2026-09-16 지적).
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestSlot1"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestSlot2"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestSlot3"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "HostButton"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChapterButton"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ShopButton"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ModeCardLeft"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ModeCardCenter"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ModeCardRight"),
        };

        /// <summary>
        /// 나눠 갖되 **폭은 그대로 두고 간격만** 벌리는 판.
        ///
        /// 모드 카드는 기운 사다리꼴 낱장이라 9-슬라이스가 안 된다 — 폭을 늘리면
        /// 테가 뭉개지고 안쪽 그림이 카드 밖으로 나간다(2026-09-16).
        /// </summary>
        private static readonly (string prefab, string node)[] SpaceOnly =
        {
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ModeCardLeft"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ModeCardCenter"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ModeCardRight"),
        };

        /// <summary>
        /// 부모 가운데에 두는 칸. 「프리팹 : 노드 이름」 — 이름이 같은 것은 **전부**.
        ///
        /// 나눠 갖는 판 안쪽은 기본이 좌·우 둘로만 가르는 것인데, 상자 칸·하단 바 칸처럼
        /// **작고 가운데로 모인 칸**에서는 그러면 한 줄이 두 동강 난다 — 태블릿에서
        /// 시계는 왼쪽 끝, 남은 시간 글자는 오른쪽 끝에 붙었다(2026-09-16).
        /// </summary>
        private static readonly (string prefab, string node)[] Centers =
        {
            // 상자 칸 속 — 그림·시간 줄·버튼 속 글자
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestArt"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestEmptyText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestTimePlate"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestTimeIcon"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestTimeText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestReadyBanner"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestReadyText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestActionButton"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestActionGemIcon"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestActionCostText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestActionLabelText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestReadyLabelText"),

            // 하단 바 칸 속 — 아이콘과 두 줄 글자
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "HostButtonArt"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "HostButtonTitleText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "HostButtonSubText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChapterButtonArt"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChapterButtonTitleText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChapterButtonSubText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ShopButtonArt"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ShopButtonTitleText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ShopButtonSubText"),
        };

        /// <summary>
        /// 남는 **세로**를 나눠 벌릴 줄. 「프리팹 : 노드 이름」 — 그린 순서 무관.
        ///
        /// 20:9 폰은 기준(1280)보다 320 px 이 남는데, 그 몫이 통째로 상단 HUD 아래
        /// 한 곳에 고여 거기만 텅 비어 보였다(2026-09-16 지적). 여기 적은 줄들이
        /// 남는 만큼을 칸 사이에 고르게 나눠 갖는다.
        /// </summary>
        /// <summary>
        /// 남는 세로를 **간격이 아니라 제 키로** 먹는 줄. `Spreads` 의 부분집합이다.
        ///
        /// 비워 두면 남는 만큼을 칸 사이 간격으로 고르게 나눈다.
        /// </summary>
        /// <summary>
        /// 판이 세로로 커져도 **위쪽 변에 붙어 있을** 칸. 「프리팹 : 노드 이름」.
        ///
        /// 안 붙이면 위쪽 글자는 위에, 아래쪽 글자는 아래에 붙어 한 덩어리였던 글이
        /// 위아래로 찢어진다 — 유령 수색 판에서 설명글만 금화 더미까지 내려갔다(2026-09-16).
        /// </summary>
        private static readonly (string prefab, string node)[] Tops =
        {
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GhostSearchScrim"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GhostSearchIcon"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GhostSearchTitleText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GhostSearchHelpButton"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GhostSearchHelpText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GhostSearchTimerText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GhostSearchGoldIcon"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GhostSearchGoldText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GhostSearchDescText"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GhostSearchBigGhost"),
        };

        private static readonly (string prefab, string node)[] Grows =
        {
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GhostSearchPanel"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestBand"),
        };

        private static readonly (string prefab, string node)[] Spreads =
        {
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "TopHudGroup"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GhostSearchPanel"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "ChestBand"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GameModeGroup"),
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "MainActionBar"),
        };

        /// <summary>폭을 늘리면 안 되는 판. 「프리팹 : 노드 이름」.</summary>
        private static readonly (string prefab, string node)[] Locks =
        {
            // 방이 10 m × 13 m 고정이라 필드는 720 px 여야만 한다.
            // 늘리면 픽셀/미터가 달라져 사거리·이동 속도·캐릭터 크기가 전부 어긋난다.
            ("Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab", "RoomField"),
        };

        /// <summary>
        /// 늘리면 안 되는데 **그린 폭이 기준 폭과 똑같아** 배경으로 오해받는 판.
        /// 가운데 앵커로 바꿔 적은 뒤 잠근다. 「프리팹 : 노드 이름」.
        ///
        /// ⚠ 잠그기만 하면 안 된다. `ScreenFit` 이 손을 떼는 순간 **RectTransform 에 적힌
        ///   그대로** 그려지므로, 「가운데」라는 뜻이 앵커에 남아 있어야 한다.
        ///   그린 값은 위·왼쪽 기준(`pivot (0,1)`)이라 잠그면 키 큰 폰에서 위에 붙어 버린다.
        /// </summary>
        private static readonly (string prefab, string node)[] CenterLocks =
        {
            // ⚠ 로비 게임모드 줄을 여기에 넣지 마라. 잠그면 720 폭에 갇혀 태블릿에서
            //   좌우 120 px 씩이 빈다 — 그건 4:3 문제를 **덮은 것**이지 푼 것이 아니다.
            //   대신 세 카드에 `ScreenFitShare` 를 붙여 남는 폭을 나눠 갖게 했다(위 `Shares`).
        };

        /// <summary>화면을 그린 기준 크기. <see cref="ScreenFit"/> 와 같은 9:16 이다.</summary>
        private const float BaseWidth = 720f;
        private const float BaseHeight = 1280f;

        [MenuItem("Tools/Game/ScreenFit 달기")]
        public static void Run()
        {
            int added = 0, locked = 0, shared = 0, centered = 0, spread = 0, topped = 0;

            foreach (var path in Roots)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) { Debug.LogError($"[ScreenFit] 로드 실패: {path}"); continue; }

                if (root.GetComponent<ScreenFit>() == null)
                {
                    root.AddComponent<ScreenFit>();
                    added++;
                }

                foreach (var (p, node) in Locks)
                {
                    if (p != path) continue;
                    var t = FindByName(root.transform, node);
                    if (t == null) { Debug.LogWarning($"[ScreenFit] 잠글 노드 없음: {node}"); continue; }
                    if (t.GetComponent<ScreenFitLock>() == null)
                    {
                        t.gameObject.AddComponent<ScreenFitLock>();
                        locked++;
                    }
                }

                foreach (var (p, node) in CenterLocks)
                {
                    if (p != path) continue;
                    var t = FindByName(root.transform, node) as RectTransform;
                    if (t == null) { Debug.LogWarning($"[ScreenFit] 가운데로 잠글 노드 없음: {node}"); continue; }
                    CenterAnchor(t);
                    if (t.GetComponent<ScreenFitLock>() == null)
                    {
                        t.gameObject.AddComponent<ScreenFitLock>();
                        locked++;
                    }
                }

                // ⚠ 목록에서 뺀 것은 **컴포넌트도 떼어 낸다.** 한 번 붙은 잠금은 저절로
                //   사라지지 않는다 — 로비 게임모드 판이 그래서 계속 720 에 갇혀 있었고,
                //   잠긴 판은 `ScreenFit` 이 통째로 건너뛰어 안쪽 나눠갖기도 안 돌았다(2026-09-16).
                foreach (var lockComp in root.GetComponentsInChildren<ScreenFitLock>(true))
                {
                    bool wanted = false;
                    foreach (var (p, node) in Locks)
                        if (p == path && node == lockComp.name) { wanted = true; break; }
                    foreach (var (p, node) in CenterLocks)
                        if (p == path && node == lockComp.name) { wanted = true; break; }
                    if (!wanted)
                    {
                        Debug.Log($"[ScreenFit] 잠금 뗌: {lockComp.name}");
                        UnityEngine.Object.DestroyImmediate(lockComp, true);
                    }
                }

                // 판이 커져도 위쪽에 붙어 있을 칸
                var topNodes = new System.Collections.Generic.List<Transform>();
                foreach (var (p, node) in Tops)
                {
                    if (p != path) continue;
                    CollectByName(root.transform, node, topNodes);
                }
                foreach (var t in topNodes)
                    if (t.GetComponent<ScreenFitTop>() == null)
                    {
                        t.gameObject.AddComponent<ScreenFitTop>();
                        topped++;
                    }
                // ⚠ 목록에서 뺀 것은 컴포넌트도 뗀다 — 잠금과 같은 이유다
                foreach (var c in root.GetComponentsInChildren<ScreenFitTop>(true))
                    if (!topNodes.Contains(c.transform))
                        UnityEngine.Object.DestroyImmediate(c, true);

                // 남는 세로를 나눠 벌릴 줄
                var spreadNodes = new System.Collections.Generic.List<Transform>();
                foreach (var (p, node) in Spreads)
                {
                    if (p != path) continue;
                    var t = FindByName(root.transform, node);
                    if (t == null) { Debug.LogWarning($"[ScreenFit] 벌릴 줄 없음: {node}"); continue; }
                    spreadNodes.Add(t);
                }
                foreach (var t in spreadNodes)
                {
                    var mark = t.GetComponent<ScreenFitSpread>();
                    if (mark == null) { mark = t.gameObject.AddComponent<ScreenFitSpread>(); spread++; }
                    bool grow = false;
                    foreach (var (gp, gn) in Grows)
                        if (gp == path && gn == t.name) { grow = true; break; }
                    mark.SetGrow(grow);
                }
                // ⚠ 목록에서 뺀 것은 컴포넌트도 뗀다 — 잠금과 같은 이유다
                foreach (var c in root.GetComponentsInChildren<ScreenFitSpread>(true))
                    if (!spreadNodes.Contains(c.transform))
                        UnityEngine.Object.DestroyImmediate(c, true);

                // 가운데에 둘 칸 — 이름이 같은 것이 여럿이므로 전부 찾는다
                var centerNodes = new System.Collections.Generic.List<Transform>();
                foreach (var (p, node) in Centers)
                {
                    if (p != path) continue;
                    CollectByName(root.transform, node, centerNodes);
                }
                foreach (var t in centerNodes)
                    if (t.GetComponent<ScreenFitCenter>() == null)
                    {
                        t.gameObject.AddComponent<ScreenFitCenter>();
                        centered++;
                    }
                // ⚠ 목록에서 뺀 것은 컴포넌트도 뗀다 — 잠금과 같은 이유다
                foreach (var c in root.GetComponentsInChildren<ScreenFitCenter>(true))
                    if (!centerNodes.Contains(c.transform))
                        UnityEngine.Object.DestroyImmediate(c, true);

                foreach (var (p, node) in Shares)
                {
                    if (p != path) continue;
                    var t = FindByName(root.transform, node);
                    if (t == null) { Debug.LogWarning($"[ScreenFit] 나눌 노드 없음: {node}"); continue; }
                    var share = t.GetComponent<ScreenFitShare>();
                    if (share == null)
                    {
                        share = t.gameObject.AddComponent<ScreenFitShare>();
                        shared++;
                    }
                    bool spaceOnly = false;
                    foreach (var (sp, sn) in SpaceOnly)
                        if (sp == path && sn == node) { spaceOnly = true; break; }
                    share.SetSpaceOnly(spaceOnly);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ScreenFit] 루트 {added}개 · 잠금 {locked} · 나눠갖기 {shared} · 가운데 {centered} · 세로 벌리기 {spread} · 위쪽 붙이기 {topped}");
        }

        /// <summary>
        /// 그린 값을 「9:16 부모 안에서의 변과 크기」로 되돌린 뒤 **가운데 앵커**로 다시 적는다.
        /// 자리·크기는 그대로고 기준점만 바뀌므로, 부모가 넓어져도 가운데를 지킨다.
        ///
        /// 여러 번 돌려도 같은 값이 나온다 — 되돌리는 계산이 가운데 앵커에도 그대로 들어맞는다.
        /// </summary>
        private static void CollectByName(Transform root, string name,
                                         System.Collections.Generic.List<Transform> into)
        {
            if (root.name == name && !into.Contains(root)) into.Add(root);
            for (int i = 0; i < root.childCount; i++) CollectByName(root.GetChild(i), name, into);
        }

        private static void CenterAnchor(RectTransform rt)
        {
            float w = rt.sizeDelta.x, h = rt.sizeDelta.y;
            float minX = rt.anchorMin.x * BaseWidth + rt.anchoredPosition.x - w * rt.pivot.x;
            float minY = rt.anchorMin.y * BaseHeight + rt.anchoredPosition.y - h * rt.pivot.y;

            var half = new Vector2(0.5f, 0.5f);
            rt.anchorMin = half;
            rt.anchorMax = half;
            rt.pivot = half;
            // ⚠ 앵커를 바꾸면 유니티가 크기를 다시 계산하므로 **크기를 다시 넣어 준다.**
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(minX + w * 0.5f - BaseWidth * 0.5f,
                                              minY + h * 0.5f - BaseHeight * 0.5f);
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
