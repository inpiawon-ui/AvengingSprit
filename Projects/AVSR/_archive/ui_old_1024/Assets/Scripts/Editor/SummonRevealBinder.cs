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

        [MenuItem("Tools/Game/소환 — 공개 연출 그림 꽂기 (영혼 배양실)")]
        public static void Run()
        {
            ImportSettings();
            // 옛 로비(소환 패널)는 2026-10-24 Assets 밖으로 옮겼다 — 배양실 연출은 새 소환 화면(RbSummonPage)으로 옮겨 붙일 때 다시 잇는다
            if (!System.IO.File.Exists(LobbyPrefab)) { Debug.LogWarning("[소환 배양실] 옛 로비 프리팹이 없다 — 그림 임포트 설정만 했다"); return; }
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                var panel = lobby.transform.Find("SummonPanel");
                if (panel == null) { Debug.LogError("[소환 공개] SummonPanel 없음"); return; }
                var reveal = panel.GetComponent<SummonReveal>();
                if (reveal == null) reveal = panel.gameObject.AddComponent<SummonReveal>();
                var so = new SerializedObject(reveal);
                // 「영혼 배양실」(gacha_reveal_1, PD 2026-10-22) — 배양실 부품이 오기 전엔 있는 그림으로 대신
                so.FindProperty("_stage").objectReferenceValue = Spr($"{Dir}/lab_stage.png") ?? Spr("Assets/BundleResource/RoomFloor/roomfloor_env_lab_full.png");
                so.FindProperty("_tank").objectReferenceValue = Spr($"{Dir}/lab_tank.png") ?? Spr("Assets/BaseResource/InGameMainUI/obj_lab_bulk.png");
                so.FindProperty("_tankGlass").objectReferenceValue = Spr($"{Dir}/lab_tank_glass.png");
                so.FindProperty("_tankBroken").objectReferenceValue = Spr($"{Dir}/lab_tank_broken.png");
                so.FindProperty("_softGlow").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseResource/Shaders/UISoftGlow.mat");
                Frames(so, "_crack", "lab_crack_");
                // 깨짐 3차(PD 10-22 「깨진 개수 차이도 크고 다르잖아」) — 속이 비치는 유리 판 · 부스러기 · 배양액, 검은 바탕 가산.
                // 자르기: Projects/AVSR/Tools/cut_summon_lab_parts3.py
                so.FindProperty("_shardSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/lab_shards_plate_sheet.png");
                so.FindProperty("_shardGrid").vector2IntValue = new Vector2Int(5, 1);
                so.FindProperty("_shardAdditive").boolValue = true;
                so.FindProperty("_chipSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/lab_shards_chip_sheet.png");
                so.FindProperty("_chipGrid").vector2IntValue = new Vector2Int(3, 1);
                so.FindProperty("_splashSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/lab_splash_sheet.png");
                so.FindProperty("_splashGrid").vector2IntValue = new Vector2Int(3, 2);
                so.FindProperty("_spark").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/lab_spark.png");
                so.FindProperty("_plate").objectReferenceValue = Spr($"{Dir}/reveal_plate.png") ?? Spr("Assets/BaseResource/UiKit/panel_navy_big.png");   // 전용 판이 오기 전엔 남색 판
                so.FindProperty("_stampNew").objectReferenceValue = Spr("Assets/BaseResource/Summon/stamp_new.png");
                so.FindProperty("_ribbonPickup").objectReferenceValue = Spr("Assets/BaseResource/Summon/ribbon_pickup.png");
                // 4단 사다리(PD 10-23) — 흰 고리를 단계색으로 물들인다 · 단계별 배양액 유리 · 대박 무지개 빛(Tools/prep_summon_tiers.py)
                Frames(so, "_ring", "lab_ring_white_");
                Frames(so, "_revealLightRainbow", "lab_reveal_light_rainbow_");
                var glassTier = so.FindProperty("_tankGlassTier");
                glassTier.arraySize = 4;
                for (int i = 0; i < 4; i++) glassTier.GetArrayElementAtIndex(i).objectReferenceValue = Spr($"{Dir}/lab_tank_glass_t{i}.png");
                var tankTier = so.FindProperty("_tankTier");
                tankTier.arraySize = 4;
                for (int i = 0; i < 4; i++) tankTier.GetArrayElementAtIndex(i).objectReferenceValue = Spr($"{Dir}/lab_tank_t{i}.png");
                Frames(so, "_revealLight", "lab_reveal_light_");
                Frames(so, "_revealLightSr", "lab_reveal_light_sr_");
                Frames(so, "_revealLightR", "lab_reveal_light_r_");
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
                ti.filterMode = (path.EndsWith("reveal_stage.png") || path.EndsWith("lab_stage.png")) ? FilterMode.Bilinear : FilterMode.Point;
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
