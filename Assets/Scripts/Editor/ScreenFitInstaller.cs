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
