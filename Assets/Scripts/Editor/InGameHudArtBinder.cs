using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 인게임 HUD 의 판 · 칸에 새 테두리 그림을 꽂는다 (2026-09-17 UI 퀄업).
    ///
    /// 상단 HUD 의 PLAYER SOUL · RUN RESOURCES · 호스트 · CHAPTER 판과 골드 · 젬 칸, 레벨 배지는
    /// **그림이 아니라 프리팹에 칠한 단색 네모 두 겹**(바깥 테 색 + 안쪽 `Inner`)이었다.
    /// 통과한 시안은 얇은 금속 테의 판이라, 늘려 쓰는 테두리 한 장씩으로 바꾼다.
    ///
    ///   큰 판  → `hud_panel_frame` (120×120, 9-슬라이스 테 24)
    ///   작은 칸 → `hud_cell_frame`  (48×48,  9-슬라이스 테 12)
    ///
    /// 그림을 꽂으면 안쪽 단색 판(`Inner`)과 윗줄 색띠(`Accent`)를 끈다 — 남겨 두면
    /// 테두리 그림을 통째로 덮는다. 지우지 않고 끄는 이유: 그림이 빠지면 되돌리기 쉽게.
    ///
    /// ⚠ 레이아웃 적용 메뉴(`Apply Mockup Layout/InGame`)는 낡은 표라 쓰지 않는다.
    ///   여기서는 **그림 · 켜고 끄기만** 건드리고 자리 · 크기는 손대지 않는다.
    /// </summary>
    public static class InGameHudArtBinder
    {
        private const string Prefab = "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab";
        private const string Res = "Assets/BaseResource/InGameMainUI";

        private static readonly (string file, int border)[] Frames =
        {
            ("hud_panel_frame", 24),
            ("hud_cell_frame", 12),
        };

        private static readonly (string node, string frame)[] Targets =
        {
            ("PlayerSoulPanel", "hud_panel_frame"),
            ("RunResourcePanel", "hud_panel_frame"),
            ("CurrentHostPanel", "hud_panel_frame"),
            ("ChapterGroup", "hud_panel_frame"),
            ("GoldCell", "hud_cell_frame"),
            ("GemCell", "hud_cell_frame"),
            ("GhostLevelBadge", "hud_cell_frame"),
            ("HostLevelBadge", "hud_cell_frame"),
        };

        [MenuItem("Tools/Game/인게임 HUD 그림 꽂기")]
        public static void Run()
        {
            foreach (var (file, border) in Frames) EnsureSprite($"{Res}/{file}.png", border);

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            int bound = 0, missing = 0;
            try
            {
                foreach (var (node, frame) in Targets)
                {
                    var t = Find(root.transform, node);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Res}/{frame}.png");
                    if (t == null || sprite == null)
                    {
                        Debug.LogWarning($"[HudArt] 건너뜀 — 노드 {node} {(t == null ? "없음" : "")} · 그림 {frame} {(sprite == null ? "없음" : "")}");
                        missing++;
                        continue;
                    }
                    var img = t.GetComponent<Image>();
                    img.sprite = sprite;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                    foreach (var child in new[] { "Inner", "Accent" })
                    {
                        var c = t.Find(child);
                        if (c != null && c.TryGetComponent<Image>(out var ci)) ci.enabled = false;
                    }
                    bound++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log($"[HudArt] 꽂음 {bound} · 건너뜀 {missing}");
        }

        private static void EnsureSprite(string path, int border)
        {
            if (!File.Exists(path)) return;
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.spriteBorder = new Vector4(border, border, border, border);
            ti.SaveAndReimport();
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
    }
}
