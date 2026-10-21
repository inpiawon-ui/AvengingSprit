using System.Collections.Generic;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 소환 「실루엣 → 공개」(`SummonReveal`) — 로비 프리팹 SummonPanel 에 붙이고 그림 · 재질을 꽂는다.
    /// 부품은 `Assets/BaseResource/SummonReveal/`(코덱스 납품을 잘라 넣은 것, 가이드 `_exchange/guide_summon_reveal.md`).
    /// 없는 부품은 비워 둔다 — 연출은 있는 것만 그린다.
    /// </summary>
    public static class SummonRevealBinder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Dir = "Assets/BaseResource/SummonReveal";

        [MenuItem("Tools/Game/소환 — 공개 연출 그림 꽂기 (실루엣 → 공개)")]
        public static void Run()
        {
            ImportSettings();
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                var panel = lobby.transform.Find("SummonPanel");
                if (panel == null) { Debug.LogError("[소환 공개] SummonPanel 없음"); return; }
                var reveal = panel.GetComponent<SummonReveal>();
                if (reveal == null) reveal = panel.gameObject.AddComponent<SummonReveal>();
                var so = new SerializedObject(reveal);
                so.FindProperty("_stage").objectReferenceValue = Spr($"{Dir}/reveal_stage.png");
                so.FindProperty("_revealStage").objectReferenceValue = Spr("Assets/BaseResource/HomeV5Art/showcase_bg.png");
                so.FindProperty("_plate").objectReferenceValue = Spr($"{Dir}/reveal_plate.png") ?? Spr("Assets/BaseResource/UiKit/panel_navy_big.png");   // 전용 판이 오기 전엔 남색 판
                so.FindProperty("_rays").objectReferenceValue = Spr("Assets/BaseResource/PopupFx/fx_levelup_emblem_rays_1.png");
                so.FindProperty("_stampNew").objectReferenceValue = Spr("Assets/BaseResource/Summon/stamp_new.png");
                so.FindProperty("_ribbonPickup").objectReferenceValue = Spr("Assets/BaseResource/Summon/ribbon_pickup.png");
                Frames(so, "_pillar", "reveal_pillar_");
                Frames(so, "_ring", "reveal_ring_");
                Frames(so, "_burst", "reveal_burst_");
                Frames(so, "_impact", "reveal_impact_");
                so.FindProperty("_additive").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseResource/Shaders/UIAdditiveSprite.mat");
                so.FindProperty("_silhouette").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseResource/Shaders/UISilhouette.mat");
                so.FindProperty("_font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BaseResource/Fonts/NotoSansKR-Bold SDF.asset");
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[소환 공개] 그림 꽂기");
        }

        /// <summary>
        /// 규칙 09 — 아틀라스 밖 그림: 안드로이드 ASTC 4x4 · 최대 크기 ≥ 원본 긴 변 · 밉맵 끔 · Read/Write 끔.
        /// 무대(일러스트)는 Bilinear, 도트 빛 프레임은 Point.
        /// </summary>
        private static void ImportSettings()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = path.EndsWith("reveal_stage.png") ? FilterMode.Bilinear : FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.isReadable = false;
                ti.alphaIsTransparency = true;
                ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "Android", overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.ASTC_4x4,
                });
                ti.SaveAndReimport();
            }
        }

        private static Sprite Spr(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static void Frames(SerializedObject so, string prop, string prefix)
        {
            var list = new List<Sprite>();
            for (int i = 1; i <= 12; i++)
                if (Spr($"{Dir}/{prefix}{i:00}.png") is Sprite s) list.Add(s);
            var p = so.FindProperty(prop);
            p.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        }
    }
}
