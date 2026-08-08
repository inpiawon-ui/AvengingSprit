using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 프리팹의 Image 슬롯에 스프라이트를 자동 배선한다.
    ///
    /// 접착제는 네이밍 규약이다 — 요소 이름 = GameObject 이름 = 파일명(소문자).
    /// 따라서 이름만 맞으면 슬롯 배정은 기계적으로 결정된다.
    ///
    /// 9-slice 경계가 있는 스프라이트는 Image.type 을 Sliced 로 맞춘다.
    /// 호스트별 변형(`hostslotportrait_{hostKey}`)은 런타임에 교체되므로
    /// 시작 보유 호스트(amazoness)를 편집기 프리뷰용 기본값으로 넣는다.
    /// </summary>
    public static class UISpriteBinder
    {
        private const string BaseRes = "Assets/BaseResource";
        private const string DefaultHost = "amazoness";

        private static readonly (string prefab, string path)[] Targets =
        {
            ("TitleMainUI",     "Assets/BundleResource/Prefabs/UI/Title/TitleMainUI.prefab"),
            ("LobbyMainUI",     "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab"),
            ("HostSelectPanel", "Assets/BundleResource/Prefabs/UI/HostSelect/HostSelectPanel.prefab"),
            ("InGameMainUI",    "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab"),
        };

        [MenuItem("Tools/Game/Bind Sprites To UI Prefabs")]
        public static void Run()
        {
            // 전 화면 스프라이트를 한 번에 색인 — 공유 요소(NotifyBadge 등)가
            // 다른 화면 폴더에 있어도 찾을 수 있어야 한다.
            var index = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            foreach (var dir in Directory.GetDirectories(BaseRes))
            {
                foreach (var f in Directory.GetFiles(dir, "*.png"))
                {
                    var p = f.Replace('\\', '/');
                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                    if (sp != null) index[Path.GetFileNameWithoutExtension(p)] = sp;
                }
            }
            Debug.Log($"[UISpriteBinder] 스프라이트 색인 {index.Count}개");

            int totalBound = 0, totalMiss = 0;
            foreach (var (name, path) in Targets)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                {
                    Debug.LogError($"[UISpriteBinder] 프리팹 로드 실패: {path}");
                    continue;
                }

                int bound = 0;
                var miss = new List<string>();
                foreach (var img in root.GetComponentsInChildren<Image>(true))
                {
                    var key = img.gameObject.name.ToLowerInvariant();

                    // 호스트 변형 슬롯은 기본 호스트로 프리뷰
                    if (!index.ContainsKey(key) && index.ContainsKey($"{key}_{DefaultHost}"))
                        key = $"{key}_{DefaultHost}";

                    if (!index.TryGetValue(key, out var sprite))
                    {
                        // 스프라이트가 없는 Image 는 히트 영역이다(탭 버튼·TouchArea·슬롯 컨테이너).
                        // 그대로 두면 흰 사각형으로 렌더되므로 알파 0 으로 투명 처리한다.
                        img.color = new Color(1f, 1f, 1f, 0f);
                        if (img.gameObject.name != "TouchArea") miss.Add(img.gameObject.name);
                        continue;
                    }
                    img.color = Color.white;

                    img.sprite = sprite;
                    var b = sprite.border;
                    img.type = (b.x + b.y + b.z + b.w) > 0 ? Image.Type.Sliced : Image.Type.Simple;
                    if (img.type == Image.Type.Sliced) img.pixelsPerUnitMultiplier = 1f;

                    // 레이아웃 박스는 목업에서 잰 값이고 에셋 비율은 그와 다를 수 있다.
                    // 그대로 늘리면 아이콘이 눌리거나 길어진다 — 비율을 지키고 박스 안에서 맞춘다.
                    // 늘어나야 하는 것(9-slice·채움 바·배경)은 제외한다.
                    img.preserveAspect = img.type == Image.Type.Simple && !AlwaysStretch(key);
                    bound++;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);

                totalBound += bound;
                totalMiss += miss.Count;
                Debug.Log($"[UISpriteBinder] {name}: 배선 {bound}개" +
                          (miss.Count > 0 ? $" / 미배선 {miss.Count}개 — {string.Join(", ", miss.Distinct())}" : ""));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UISpriteBinder] 완료 — 배선 {totalBound}개 / 미배선 {totalMiss}개");
        }
        /// <summary>
        /// 박스를 꽉 채워야 하는 요소 — 비율을 지키면 오히려 빈 틈이 생긴다.
        /// 배경·바닥은 화면을 덮어야 하고, 채움 바는 폭을 코드가 조절한다.
        /// </summary>
        private static bool AlwaysStretch(string key)
            => key.Contains("background") || key.Contains("floor")
            || key.EndsWith("fill") || key.Contains("cooldown");

    }
}
