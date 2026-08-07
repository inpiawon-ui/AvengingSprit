using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 목업 실측 레이아웃(JSON)을 UI 프리팹에 적용한다.
    ///
    /// 스펙 생성: `Projects/AVSR/_layout_{화면}.py` — 목업 격자 실측이 원본이다.
    /// 스펙 경로: `Assets/Scripts/Editor/UISpec/_layout_{화면}.json`
    ///
    /// 좌표는 **720×1280 캔버스 절대 좌상단 좌표**다. 모든 대상 노드를
    /// top-left 앵커로 정규화한 뒤, 부모의 절대 좌표를 빼서 로컬로 변환한다.
    /// 부모 절대 좌표는 이 표 자신에서 얻는다(표에 없는 조상은 원점으로 본다).
    /// </summary>
    public static class UILayoutApplier
    {
        private const string SpecDir = "Assets/Scripts/Editor/UISpec";

        private static readonly (string screen, string prefab)[] Targets =
        {
            ("Lobby", "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab"),
        };

        [Serializable]
        private sealed class Item
        {
            public string name;
            public float x, y, w, h;
            public string text;
            public float size;
            public string align;
            public string color;
            public bool wrap;
            public int clone;
            public float dx;
            public string create;
            public string parent;
        }

        [Serializable]
        private sealed class Spec
        {
            public string screen;
            public string root;
            public Item[] items;
        }

        [MenuItem("Tools/Game/Apply Mockup Layout")]
        public static void Run()
        {
            foreach (var (screen, prefabPath) in Targets)
            {
                var specPath = $"{SpecDir}/_layout_{screen}.json";
                if (!File.Exists(specPath))
                {
                    Debug.LogError($"[UILayout] 스펙 없음: {specPath}");
                    continue;
                }
                var spec = JsonUtility.FromJson<Spec>(File.ReadAllText(specPath));
                if (spec?.items == null || spec.items.Length == 0)
                {
                    Debug.LogError($"[UILayout] 파싱 실패: {specPath}");
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null) { Debug.LogError($"[UILayout] 로드 실패: {prefabPath}"); continue; }

                int applied = 0, missing = 0;
                var abs = new Dictionary<Transform, Vector2>();

                foreach (var it in spec.items)
                {
                    var targets = Resolve(root.transform, it);
                    if (targets.Count == 0)
                    {
                        Debug.LogWarning($"[UILayout] {screen}: '{it.name}' 없음");
                        missing++;
                        continue;
                    }

                    for (int i = 0; i < targets.Count; i++)
                    {
                        var rt = targets[i];
                        var pos = new Vector2(it.x + it.dx * i, it.y);
                        Place(rt, pos, it.w, it.h, abs);
                        ApplyText(rt, it);
                        abs[rt] = pos;
                        applied++;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log($"[UILayout] {screen} — 적용 {applied}개 / 누락 {missing}개");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>`Parent/Child` 경로 또는 단일 이름으로 대상을 찾는다. 없고 `create` 면 만든다.</summary>
        private static List<Transform> Resolve(Transform root, Item it)
        {
            var result = new List<Transform>();
            Transform found;

            if (it.name.Contains('/'))
            {
                var parts = it.name.Split('/');
                var p = FindByName(root, parts[0]);
                found = p == null ? null : FindByName(p, parts[1]);
            }
            else
            {
                found = FindByName(root, it.name);
            }

            if (found == null)
            {
                if (string.IsNullOrEmpty(it.create) || string.IsNullOrEmpty(it.parent)) return result;
                var parent = FindByName(root, it.parent);
                if (parent == null) return result;
                var leaf = it.name.Contains('/') ? it.name.Split('/').Last() : it.name;
                var go = new GameObject(leaf, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                if (it.create == "TMP")
                {
                    var t = go.AddComponent<TextMeshProUGUI>();
                    t.raycastTarget = false;
                }
                else if (it.create == "IMG")
                {
                    var img = go.AddComponent<Image>();
                    img.raycastTarget = false;
                }
                found = go.transform;
            }

            result.Add(found);

            // 동일 이름 형제 복제 (출석 체크 5칸 등). 이미 있으면 재사용한다.
            if (it.clone > 0)
            {
                var parent = found.parent;
                var siblings = new List<Transform>();
                for (int i = 0; i < parent.childCount; i++)
                {
                    var c = parent.GetChild(i);
                    if (c != found && c.name == found.name) siblings.Add(c);
                }
                for (int i = 0; i < it.clone; i++)
                {
                    if (i < siblings.Count) { result.Add(siblings[i]); continue; }
                    var dup = UnityEngine.Object.Instantiate(found.gameObject, parent);
                    dup.name = found.name;
                    result.Add(dup.transform);
                }
            }
            return result;
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

        /// <summary>top-left 앵커로 정규화하고 절대 좌표를 부모 기준 로컬로 바꿔 배치한다.</summary>
        private static void Place(Transform t, Vector2 pos, float w, float h,
                                  Dictionary<Transform, Vector2> abs)
        {
            var rt = (RectTransform)t;
            var parentAbs = Vector2.zero;
            for (var p = t.parent; p != null; p = p.parent)
            {
                if (abs.TryGetValue(p, out var v)) { parentAbs = v; break; }
            }

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(pos.x - parentAbs.x, -(pos.y - parentAbs.y));
        }

        private static void ApplyText(Transform t, Item it)
        {
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp == null) return;

            if (!string.IsNullOrEmpty(it.text)) tmp.text = it.text;
            if (it.size > 0f)
            {
                tmp.fontSize = it.size;
                // 목업 폰트는 좁은 아케이드체, 대체 폰트(Noto)는 넓다.
                // 같은 대문자 높이를 쓰면 가로가 넘치므로 축소 여지를 넉넉히 준다.
                tmp.enableAutoSizing = true;
                tmp.fontSizeMax = it.size;
                tmp.fontSizeMin = Mathf.Max(8f, it.size * 0.45f);
            }
            tmp.alignment = it.align switch
            {
                "C" => TextAlignmentOptions.Center,
                "R" => TextAlignmentOptions.Right,
                "TL" => TextAlignmentOptions.TopLeft,
                _ => TextAlignmentOptions.Left,
            };
            SetWrap(tmp, it.wrap);
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.margin = Vector4.zero;

            if (!string.IsNullOrEmpty(it.color) &&
                ColorUtility.TryParseHtmlString(it.color, out var c))
                tmp.color = c;
        }

        /// <summary>TMP 버전에 따라 프로퍼티 이름이 다르다(3.2 에서 textWrappingMode 로 교체됨).</summary>
        private static void SetWrap(TMP_Text tmp, bool wrap)
        {
            var type = typeof(TMP_Text);
            var modern = type.GetProperty("textWrappingMode");
            if (modern != null)
            {
                var enumType = modern.PropertyType;
                modern.SetValue(tmp, Enum.Parse(enumType, wrap ? "Normal" : "NoWrap"));
                return;
            }
            type.GetProperty("enableWordWrapping")?.SetValue(tmp, wrap);
        }
    }
}
