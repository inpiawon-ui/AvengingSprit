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
    /// 로비가 **목업대로 나왔는지** 숫자로 검사한다.
    ///
    /// ⚠ 눈으로 「안 잘렸으면 됐다」로 넘기지 마라. 2026-09-16 에 그 기준으로 여러 번
    ///   통과시켰고, 크기가 어긋난 것들이 전부 그대로 남았다. 여기가 새 기준이다.
    ///
    /// 보는 것
    ///   1. 표(`_layout_Lobby.json`)가 적은 자리 ↔ 프리팹의 실제 자리
    ///   2. 글자가 칸을 넘치는가 (자동 축소가 바닥에서 멈춘 칸)
    ///   3. 그림이 안 꽂힌 `Image`
    ///   4. 빛무리가 살아 있는가 (반투명 픽셀이 0이면 알파 처리에서 깎인 것)
    /// </summary>
    public static class LobbyVerifier
    {
        private const string Prefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Spec = "Assets/Scripts/Editor/UISpec/_layout_Lobby.json";
        private const string Res = "Assets/BaseResource/LobbyMainUI";

        /// <summary>자리가 이만큼 어긋나면 알린다 (캔버스 px).</summary>
        private const float PosTolerance = 2f;

        /// <summary>빛무리가 있어야 하는 그림 — 반투명이 0이면 알파 처리에서 깎였다.</summary>
        private static readonly string[] NeedSoftEdge =
        {
            "chest_wood", "chest_silver", "chest_gold", "chest_magic",
            "panelframe", "chestslotframe", "buttonblue", "buttongold",
            "actionframe_blue", "actionframe_gold",
            "modecard_side", "modecard_center", "ghostsearchbig",
        };

        [MenuItem("Tools/Game/로비 검증")]
        public static void Run()
        {
            var problems = new List<string>();
            CheckSprites(problems);

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            if (root == null) { Debug.LogError("[검증] 프리팹 로드 실패"); return; }
            try
            {
                CheckLayout(root.transform, problems);
                CheckBinding(root.transform, problems);
                CheckTextFits(root.transform, problems);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            // 콘솔은 긴 목록을 잘라 버린다 — 파일로도 남긴다
            var report = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "claude", "lobby_verify.txt");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(report));
            File.WriteAllLines(report, problems.Count == 0
                ? new[] { "이상 없음" } : problems.ToArray());

            if (problems.Count == 0)
            {
                Debug.Log("[검증] 이상 없음 — 표와 프리팹이 일치하고, 넘치는 글자·빈 칸이 없다");
                return;
            }
            Debug.LogWarning($"[검증] {problems.Count}건 — {report}");
        }

        // ── 1·2. 표가 적은 자리 ↔ 실제 자리 ─────────────────────

        private static void CheckLayout(Transform root, List<string> outp)
        {
            if (!File.Exists(Spec)) { outp.Add("표 없음: " + Spec); return; }
            var spec = JsonUtility.FromJson<SpecFile>(File.ReadAllText(Spec));
            if (spec?.items == null) { outp.Add("표 파싱 실패"); return; }

            foreach (var it in spec.items)
            {
                var t = Resolve(root, it.name);
                if (t == null) { outp.Add($"노드 없음: {it.name}"); continue; }
                var rt = (RectTransform)t;
                // 크기만 본다 — 자리는 `ScreenFit` 이 화면마다 다시 잡으므로 표와 달라도 정상이다
                float dw = Mathf.Abs(rt.sizeDelta.x - it.w);
                float dh = Mathf.Abs(rt.sizeDelta.y - it.h);
                if (dw > PosTolerance || dh > PosTolerance)
                    outp.Add($"크기 어긋남 {it.name}: 표 {it.w:0}x{it.h:0} ≠ 실제 "
                             + $"{rt.sizeDelta.x:0}x{rt.sizeDelta.y:0}");
            }
        }

        // ── 3. 그림이 안 꽂힌 Image ──────────────────────────────

        /// <summary>그림이 비어 있는 것이 **정상**인 칸. 코드가 런타임에 채우거나 단색이다.</summary>
        private static readonly string[] FilledAtRuntime =
        {
            "ChestArt",            // 등급이 정해질 때 `LobbyMainUI` 가 끼운다
            "ModeCardArt",         // 회전할 때마다 갈아 끼운다
            "ModeCenterArt",       // 〃 (가운데 칸)
            "GameModeLabelAccent", // 노란 막대 하나 — 그림이 필요 없다
        };

        private static void CheckBinding(Transform root, List<string> outp)
        {
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (IsUnder(img.transform, "HostSelectPanel")) continue;
                if (System.Array.IndexOf(FilledAtRuntime, img.name) >= 0) continue;
                if (img.sprite != null) continue;
                // 버튼 판처럼 색만 쓰는 칸도 있다 — 투명이면 아무것도 안 그리므로 알린다
                if (img.color.a < 0.05f) continue;
                outp.Add($"그림 없음: {Path(img.transform)} (색 {img.color})");
            }
        }

        // ── 4. 글자가 칸을 넘치는가 ──────────────────────────────

        private static void CheckTextFits(Transform root, List<string> outp)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (IsUnder(t.transform, "HostSelectPanel")) continue;
                if (string.IsNullOrEmpty(t.text)) continue;
                var rt = (RectTransform)t.transform;
                if (rt.rect.width < 1f) continue;

                // 자동 축소 바닥에서 잰다 — 가장 긴 번역이 와도 이 크기면 들어가야 한다
                float min = t.enableAutoSizing ? t.fontSizeMin : t.fontSize;
                var size = t.GetPreferredValues(t.text, rt.rect.width, 0f);
                if (t.enableAutoSizing)
                {
                    float k = min / Mathf.Max(0.01f, t.fontSize);
                    size *= k;
                }
                if (size.x > rt.rect.width + 1f)
                    outp.Add($"글자 넘침 {Path(t.transform)}: 최소크기에서도 "
                             + $"{size.x:0} > 칸 {rt.rect.width:0}  «{Trim(t.text)}»");
                if (t.overflowMode == TextOverflowModes.Overflow)
                    outp.Add($"넘침모드 Overflow {Path(t.transform)} — 자동 축소가 가로를 안 본다");
            }
        }

        // ── 5. 빛무리(반투명)가 살아 있는가 ──────────────────────

        private static void CheckSprites(List<string> outp)
        {
            foreach (var name in NeedSoftEdge)
            {
                var path = $"{Res}/{name}.png";
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) { outp.Add($"그림 파일 없음: {name}"); continue; }

                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null && !ti.isReadable)
                {
                    ti.isReadable = true;
                    ti.SaveAndReimport();
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
                int soft = 0;
                foreach (var c in tex.GetPixels32())
                    if (c.a > 4 && c.a < 250) soft++;
                if (soft == 0)
                    outp.Add($"빛무리 없음 {name}: 반투명 픽셀 0 — 알파 처리에서 깎였다");
            }
        }

        // ── 도우미 ───────────────────────────────────────────────

        private static Transform Resolve(Transform root, string name)
        {
            if (!name.Contains('/')) return Find(root, name);
            var t = root;
            foreach (var part in name.Split('/'))
            {
                t = Find(t, part);
                if (t == null) return null;
            }
            return t;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = Find(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        private static bool IsUnder(Transform t, string ancestor)
        {
            for (var p = t; p != null; p = p.parent)
                if (p.name == ancestor) return true;
            return false;
        }

        private static string Path(Transform t)
            => t.parent == null ? t.name : t.parent.name + "/" + t.name;

        private static string Trim(string s)
            => s.Length <= 16 ? s : s.Substring(0, 16) + "…";

        [System.Serializable]
        private sealed class SpecItem { public string name; public float x, y, w, h; }

        [System.Serializable]
        private sealed class SpecFile { public SpecItem[] items; }
    }
}
