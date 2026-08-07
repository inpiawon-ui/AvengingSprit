using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 화면 설계서(Stage 2b)에서 추출한 JSON 스펙으로 UI 프리팹 골격을 생성한다.
    /// 스펙 경로: Assets/Scripts/Editor/UISpec/*.json
    /// 산출 경로: Assets/BundleResource/Prefabs/UI/{화면}/{루트이름}.prefab
    ///
    /// 좌표 규약: 스펙의 rect 는 720×1280 좌상단 원점 절대 좌표다.
    /// 부모 기준 로컬로 변환해 top-left 앵커로 배치한다.
    /// </summary>
    public static class UIPrefabBuilder
    {
        private const string SpecDir = "Assets/Scripts/Editor/UISpec";
        private const string OutDir = "Assets/BundleResource/Prefabs/UI";
        private const int RefWidth = 720;
        private const int RefHeight = 1280;
        private const int HudHeight = 128;   // ~Panel 상단 오프셋 (constants.md: 1280 − 1152)
        private const int PanelHeight = 1152;

        [Serializable]
        private sealed class Node
        {
            public string name;
            public int parentIdx = -1;   // 이름 중복(NotifyBadge 6곳 등)이 있어 인덱스로 지정한다
            public string comp;
            public bool stretch;
            public int repeat = 1;
            public int[] rect;

            public bool HasRect => rect != null && rect.Length == 4;
        }

        [Serializable]
        private sealed class Spec
        {
            public string screen;
            public string root;
            public Node[] nodes;
        }

        [MenuItem("Tools/Game/Build UI Prefabs From Spec")]
        public static void BuildAll()
        {
            var files = Directory.GetFiles(SpecDir, "*.json");
            if (files.Length == 0)
            {
                Debug.LogError($"[UIPrefabBuilder] 스펙이 없다: {SpecDir}");
                return;
            }

            foreach (var file in files)
                BuildOne(file);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UIPrefabBuilder] 완료 — 화면 {files.Length}개");
        }

        private static void BuildOne(string specPath)
        {
            var spec = JsonUtility.FromJson<Spec>(File.ReadAllText(specPath));
            if (spec == null || spec.nodes == null || spec.nodes.Length == 0)
            {
                Debug.LogError($"[UIPrefabBuilder] 파싱 실패: {specPath}");
                return;
            }

            // 스펙은 부모가 항상 자식보다 앞에 오도록 정렬되어 있다(설계서 트리 순서).
            var created = new GameObject[spec.nodes.Length];
            created[0] = CreateRoot(spec.nodes[0]);
            int made = 1, failed = 0;

            for (int i = 1; i < spec.nodes.Length; i++)
            {
                var n = spec.nodes[i];
                var pi = n.parentIdx;
                if (pi < 0 || pi >= i || created[pi] == null)
                {
                    Debug.LogWarning($"[UIPrefabBuilder] {spec.screen}: '{n.name}' 부모 인덱스 이상 ({pi})");
                    failed++;
                    continue;
                }

                // 로컬 변환에 필요한 부모 절대좌표 — 가장 가까운 rect 보유 조상을 찾는다
                int px = 0, py = 0;
                for (int a = pi; a >= 0; a = spec.nodes[a].parentIdx)
                {
                    if (spec.nodes[a].HasRect)
                    {
                        px = spec.nodes[a].rect[0];
                        py = spec.nodes[a].rect[1];
                        break;
                    }
                    if (a == 0) break;
                }
                if (spec.nodes[0].comp == "RootPanel" && px == 0 && py == 0)
                    py = HudHeight;   // ~Panel 은 y=128 부터 시작

                created[i] = CreateNode(n, created[pi], px, py);
                made++;
            }

            if (failed > 0)
                Debug.LogWarning($"[UIPrefabBuilder] {spec.screen}: 실패 {failed}개");
            var root = created[0];

            var dir = $"{OutDir}/{spec.screen}";
            EnsureFolder(dir);
            var path = $"{dir}/{spec.root}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[UIPrefabBuilder] {spec.screen}: {made}/{spec.nodes.Length}개 → {path}");
        }

        private static GameObject CreateRoot(Node n)
        {
            var go = new GameObject(n.name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;

            if (n.comp == "RootPanel")
            {
                // 05_prefabs.md — 상단 HUD를 가리지 않는 고정 높이 창
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -HudHeight);
                rt.sizeDelta = new Vector2(0f, PanelHeight);
            }
            else
            {
                // ~UI — Stretch Full, sizeDelta (0,0)
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
            }
            return go;
        }

        private static GameObject CreateNode(Node n, GameObject parent, int parentAbsX, int parentAbsY)
        {
            var go = new GameObject(n.name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rt = (RectTransform)go.transform;

            if (n.stretch || !n.HasRect)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
            }
            else
            {
                // 설계서 좌표는 720×1280 절대값 → 부모 절대좌표를 빼서 로컬로
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(n.rect[0] - parentAbsX, -(n.rect[1] - parentAbsY));
                rt.sizeDelta = new Vector2(n.rect[2], n.rect[3]);
            }

            AddComponents(go, n);
            return go;
        }

        private static void AddComponents(GameObject go, Node n)
        {
            switch (n.comp)
            {
                case "Image":
                    go.AddComponent<Image>();
                    break;

                case "ImageSliced":
                    var sliced = go.AddComponent<Image>();
                    sliced.type = Image.Type.Sliced;
                    break;

                case "Button":
                    go.AddComponent<Image>();
                    go.AddComponent<Button>();
                    break;

                case "TMP":
                    var tmp = go.AddComponent<TextMeshProUGUI>();
                    tmp.text = n.name;
                    tmp.fontSize = 22f;
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;
                    tmp.raycastTarget = false;
                    break;

                case "Grid":
                    var grid = go.AddComponent<GridLayoutGroup>();
                    grid.cellSize = new Vector2(108f, 140f);
                    grid.spacing = new Vector2(10f, 10f);
                    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = 3;
                    break;

                // Group / RootUI / RootPanel — RectTransform 만
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            var cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{cur}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
