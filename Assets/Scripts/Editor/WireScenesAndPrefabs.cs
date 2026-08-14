using System.Linq;
using Game.Module.InGame;
using Game.Module.Lobby;
using Game.Module.Title;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// UI 프리팹에 화면 스크립트를 부착하고, Addressable 등록 + 씬의 SceneUILoader 주소를 배선한다.
    /// 씬 파일의 `_uiAddress` 가 템플릿 잔재(PetRoomUI/MinigameUI)로 남아 있어 함께 정정한다.
    /// </summary>
    public static class WireScenesAndPrefabs
    {
        private const string Group = "ui";
        private const string PrefabRoot = "Assets/BundleResource/Prefabs/UI";

        // 프리팹 경로, Addressable 주소, 라벨
        private static readonly (string path, string address, string label)[] Prefabs =
        {
            ($"{PrefabRoot}/Title/TitleMainUI.prefab",           "UI/Title/TitleMainUI",      "label_title"),
            ($"{PrefabRoot}/Lobby/LobbyMainUI.prefab",           "UI/Lobby/LobbyMainUI",      "label_lobby"),
            ($"{PrefabRoot}/HostSelect/HostSelectPanel.prefab",  "UI/Lobby/HostSelectPanel",  "label_lobby"),
            ($"{PrefabRoot}/InGame/InGameMainUI.prefab",         "UI/InGame/InGameMainUI",    "label_ingame"),
        };

        // 씬 이름, SceneUILoader 에 넣을 ~UI 주소.
        private static readonly (string scene, string address)[] SceneUi =
        {
            ("Assets/Scenes/TitleScene.unity", "UI/Title/TitleMainUI"),
            ("Assets/Scenes/LobbyScene.unity", "UI/Lobby/LobbyMainUI"),
            ("Assets/Scenes/GameScene.unity",  "UI/InGame/InGameMainUI"),
        };

        [MenuItem("Tools/Game/Wire Scenes And Prefabs")]
        public static void Run()
        {
            AttachScripts();
            RegisterAddressables();
            WireScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WireScenesAndPrefabs] 완료");
        }

        private static void AttachScripts()
        {
            Attach<TitleMainUI>($"{PrefabRoot}/Title/TitleMainUI.prefab");
            Attach<HostSelectPanel>($"{PrefabRoot}/HostSelect/HostSelectPanel.prefab");
            Attach<InGameMainUI>($"{PrefabRoot}/InGame/InGameMainUI.prefab");

            // 로비는 HostSelectPanel 참조를 함께 물려야 한다
            var lobbyPath = $"{PrefabRoot}/Lobby/LobbyMainUI.prefab";
            var root = PrefabUtility.LoadPrefabContents(lobbyPath);
            if (root == null) { Debug.LogError($"[Wire] 로드 실패: {lobbyPath}"); return; }

            var lobby = root.GetComponent<LobbyMainUI>() ?? root.AddComponent<LobbyMainUI>();

            // 호스트 선택 패널을 로비 프리팹 안에 중첩 인스턴스로 넣는다.
            // 별도 씬 로드 없이 로비 위에서 열고 닫기 위함이다.
            var panelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabRoot}/HostSelect/HostSelectPanel.prefab");
            var existing = root.transform.Find("HostSelectPanel");
            GameObject panelGo = existing != null ? existing.gameObject : null;
            if (panelGo == null && panelAsset != null)
            {
                panelGo = (GameObject)PrefabUtility.InstantiatePrefab(panelAsset, root.transform);
                panelGo.name = "HostSelectPanel";
                panelGo.transform.SetAsLastSibling();
            }
            if (panelGo != null)
            {
                var so = new SerializedObject(lobby);
                var prop = so.FindProperty("_hostSelectPanel");
                if (prop != null)
                {
                    prop.objectReferenceValue = panelGo.GetComponent<HostSelectPanel>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, lobbyPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[Wire] LobbyMainUI + HostSelectPanel 중첩 배선 완료");
        }

        private static void Attach<T>(string path) where T : Component
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) { Debug.LogError($"[Wire] 로드 실패: {path}"); return; }
            if (root.GetComponent<T>() == null) root.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[Wire] {typeof(T).Name} 부착 — {path}");
        }

        private static void RegisterAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("[Wire] Addressable 설정 없음"); return; }
            var group = settings.FindGroup(Group) ?? settings.CreateGroup(
                Group, false, false, true, null,
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema),
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));

            foreach (var (path, address, label) in Prefabs)
            {
                if (!settings.GetLabels().Contains(label)) settings.AddLabel(label);
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid)) { Debug.LogWarning($"[Wire] 없음: {path}"); continue; }
                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = address;
                entry.SetLabel(label, true);
                Debug.Log($"[Wire] Addressable — {address}");
            }
            EditorUtility.SetDirty(settings);
        }

        private static void WireScenes()
        {
            var active = EditorSceneManager.GetActiveScene().path;
            foreach (var (scenePath, address) in SceneUi)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var loaders = Object.FindObjectsByType<Game.Module.Common.SceneUILoader>(
                    FindObjectsInactive.Include);
                foreach (var l in loaders)
                {
                    var so = new SerializedObject(l);
                    var p = so.FindProperty("_uiAddress");
                    if (p != null) { p.stringValue = address; so.ApplyModifiedPropertiesWithoutUndo(); }
                    EditorUtility.SetDirty(l);
                }
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                Debug.Log($"[Wire] {scenePath} → _uiAddress = {address} (로더 {loaders.Length}개)");
            }
            if (!string.IsNullOrEmpty(active)) EditorSceneManager.OpenScene(active, OpenSceneMode.Single);
        }
    }
}
