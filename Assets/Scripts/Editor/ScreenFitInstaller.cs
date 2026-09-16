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
            // 로비 게임모드 줄. 720 폭 안에 「◀ 카드 카드 카드 ▶」가 가운데 정렬로 그려져 있다.
            //
            // ⚠ 태블릿 4:3(캔버스 960)에서 이 판이 통째로 늘어나면서 **카드가 갈라졌다.**
            //   판 안의 것들이 양끝(2 px · 718 px)에 닿아 있어 `ScreenFit` 이 「가로지르는 줄」로
            //   보고 가장 넓은 틈에서 좌·우로 갈라 붙이는데, 그러면 **가운데 카드가 오른쪽 무리에
            //   끼어** 중앙을 벗어난다(2026-09-16 사용자 지적 「해상도 대응이 안 된다」).
            //   이 줄은 상단 재화 줄과 달리 갈라지면 안 되는 **한 덩어리 캐러셀**이다.
            ("Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab", "GameModeGroup"),
        };

        /// <summary>화면을 그린 기준 크기. <see cref="ScreenFit"/> 와 같은 9:16 이다.</summary>
        private const float BaseWidth = 720f;
        private const float BaseHeight = 1280f;

        [MenuItem("Tools/Game/ScreenFit 달기")]
        public static void Run()
        {
            int added = 0, locked = 0, shared = 0;

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

                foreach (var (p, node) in Shares)
                {
                    if (p != path) continue;
                    var t = FindByName(root.transform, node);
                    if (t == null) { Debug.LogWarning($"[ScreenFit] 나눌 노드 없음: {node}"); continue; }
                    if (t.GetComponent<ScreenFitShare>() == null)
                    {
                        t.gameObject.AddComponent<ScreenFitShare>();
                        shared++;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ScreenFit] 루트 {added}개에 달았고 {locked}개를 잠갔고 {shared}개를 나눠갖기로 했다");
        }

        /// <summary>
        /// 그린 값을 「9:16 부모 안에서의 변과 크기」로 되돌린 뒤 **가운데 앵커**로 다시 적는다.
        /// 자리·크기는 그대로고 기준점만 바뀌므로, 부모가 넓어져도 가운데를 지킨다.
        ///
        /// 여러 번 돌려도 같은 값이 나온다 — 되돌리는 계산이 가운데 앵커에도 그대로 들어맞는다.
        /// </summary>
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
