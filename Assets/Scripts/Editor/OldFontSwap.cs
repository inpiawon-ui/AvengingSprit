using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// UI 프리팹에 남은 옛 폰트(NotoSans KR · JP)를 지금 폰트(Pretendard)로 바꾼다. 효과 재질은 같은 이름 꼬리 짝으로.
    ///
    /// ⚠ 왜(2026-10-30 메모리 최적화) — 인게임 화면 글자 칸 68개가 옛 폰트를 물고 있어 인게임에 들어갈 때마다
    ///   옛 폰트 원본(약 16MB) + 글자 아틀라스가 따로 올라왔다. 게다가 언어팩은 옛 폰트 칸을 「본문 폰트 칸」으로
    ///   알아보지 못해 언어를 바꿔도 폰트가 안 바뀌었다(일본어 판에서도 한국어 폰트로 그려짐).
    ///   Pretendard 로 바꾸면 언어팩이 지금 언어 폰트로 갈아 끼운다.
    /// 빌더 몇 개가 아직 옛 폰트를 박으므로 APK 빌드 직전에 늘 한 번 돌린다(<see cref="BuildApk"/>). 여러 번 돌려도 같다.
    /// </summary>
    public static class OldFontSwap
    {
        private const string PrefabDir = "Assets/BundleResource/Prefabs/UI";
        private const string OldDir = "Assets/BaseResource/Fonts/";
        private const string NewDir = "Assets/BaseResource/Fonts/ReBorn/";

        [MenuItem("Tools/Game/최적화/옛 폰트 → 지금 폰트 바꾸기")]
        public static void RunMenu() => Debug.Log($"[옛 폰트] 바꾼 연결 {Run()}개");

        /// <returns>바꾼 연결 수</returns>
        public static int Run()
        {
            var map = BuildMap();
            if (map.Count == 0) return 0;

            int total = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                int n = 0;
                try
                {
                    foreach (var c in root.GetComponentsInChildren<Component>(true))
                    {
                        if (c == null) continue;
                        var so = new SerializedObject(c);
                        var it = so.GetIterator();
                        bool changed = false;
                        while (it.Next(true))
                        {
                            if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                            var v = it.objectReferenceValue;
                            if (v == null || !map.TryGetValue(v, out var to)) continue;
                            it.objectReferenceValue = to;
                            changed = true;
                            n++;
                        }
                        if (changed) so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                if (n > 0) Debug.Log($"[옛 폰트] {path} — {n}개");
                total += n;
            }
            return total;
        }

        private static Dictionary<Object, Object> BuildMap()
        {
            var map = new Dictionary<Object, Object>();
            var body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NewDir + "Pretendard-SemiBold SDF.asset");
            var heavy = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NewDir + "Pretendard-Bold SDF.asset");
            if (body == null || heavy == null)
            {
                Debug.LogError("[옛 폰트] 새 폰트를 못 찾았다 — 「언어별 글꼴 만들기 · 걸기」를 먼저 돌린다");
                return map;
            }
            foreach (var old in new[] { "NotoSansKR SDF", "NotoSansJP SDF" })
            {
                AddFont(map, OldDir + old + ".asset", body);
                foreach (var guid in AssetDatabase.FindAssets(old + " - t:Material", new[] { "Assets/BaseResource/Fonts" }))
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    string file = System.IO.Path.GetFileNameWithoutExtension(p);
                    if (!file.StartsWith(old + " - ")) continue;
                    var to = AssetDatabase.LoadAssetAtPath<Material>(NewDir + body.name + file.Substring(old.Length) + ".mat");
                    if (to != null) map[AssetDatabase.LoadAssetAtPath<Material>(p)] = to;
                }
            }
            foreach (var old in new[] { "NotoSansKR-Bold SDF", "NotoSansJP-Bold SDF" })
                AddFont(map, OldDir + old + ".asset", heavy);
            return map;
        }

        private static void AddFont(Dictionary<Object, Object> map, string path, TMP_FontAsset to)
        {
            var from = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (from == null) return;
            map[from] = to;
            if (from.material != null && to.material != null) map[from.material] = to.material;   // 폰트 기본 재질
        }
    }
}
