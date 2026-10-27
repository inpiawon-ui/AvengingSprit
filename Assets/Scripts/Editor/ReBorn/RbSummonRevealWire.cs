// 소환 「영혼 배양실」(SummonReveal, 10-23 4판) — RE:BORN 소환 쪽(PageSummon)에 붙이고 그림 · 재질을 꽂는다.
// 예전엔 옛 로비 SummonPanel 에 붙였다(SummonRevealBinder — 2026-10-24 옛 UI 와 함께 _archive 로). PD 2026-10-26 「기존 시안 연출을 살려서 연결」.
// 부품은 Assets/BaseResource/SummonReveal/(가이드 _exchange/guide_summon_lab_v2.md). 없는 부품은 비워 둔다 — 연출은 있는 것만 그린다.
using System.Collections.Generic;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.ReBorn
{
    public static class RbSummonRevealWire
    {
        private const string Dir = "Assets/BaseResource/SummonReveal";

        /// <summary>소환 쪽 뿌리에 연출을 붙이고 부품을 꽂는다 — RbSummonBuilder 가 부른다(로비 세우기).</summary>
        public static void Attach(GameObject page)
        {
            if (!page.TryGetComponent<SummonReveal>(out var reveal)) reveal = page.AddComponent<SummonReveal>();
            var so = new SerializedObject(reveal);
            so.FindProperty("_stage").objectReferenceValue = Spr($"{Dir}/lab_stage.png");
            so.FindProperty("_tank").objectReferenceValue = Spr($"{Dir}/lab_tank.png");
            so.FindProperty("_tankBroken").objectReferenceValue = Spr($"{Dir}/lab_tank_broken.png");
            so.FindProperty("_softGlow").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseResource/Shaders/UISoftGlow.mat");
            Frames(so, "_crack", "lab_crack_");
            // 깨짐 3차 — 속이 비치는 유리 판 · 부스러기 · 배양액, 검은 바탕 가산(자르기 Projects/AVSR/Tools/cut_summon_lab_parts3.py)
            so.FindProperty("_shardSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/lab_shards_plate_sheet.png");
            so.FindProperty("_shardGrid").vector2IntValue = new Vector2Int(5, 1);
            so.FindProperty("_shardAdditive").boolValue = true;
            so.FindProperty("_chipSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/lab_shards_chip_sheet.png");
            so.FindProperty("_chipGrid").vector2IntValue = new Vector2Int(3, 1);
            so.FindProperty("_splashSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/lab_splash_sheet.png");
            so.FindProperty("_splashGrid").vector2IntValue = new Vector2Int(3, 2);
            so.FindProperty("_spark").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/lab_spark.png");
            // 시험판 2(PD 10-27) — 공개 빛은 하나(금빛 6장: 1장은 도는 빛살, 6장은 제자리 반짝). 등급 사다리 빛 · 배양액 색 판은 안 쓴다
            Frames(so, "_revealLight", "lab_reveal_light_");
            // 결과 카드 바탕 판 · 아래 버튼(확인 · 10회 더) — RE:BORN 키트
            so.FindProperty("_labelPlate").objectReferenceValue = RbBuild.KitSprite("Components/rb_panel_base");
            // 결과 카드 — 호스트 목록 카드와 같은 부품(PD 10-27 시안 mock_summon_card). SSR 은 은빛 틀 × 금색(RbFrameFit.SsrTint)
            Many(so, "_cardFrames", "Components/rb_host_frame_r", "Components/rb_host_frame_sr", "Components/rb_host_frame_ghost");
            Many(so, "_cardTags", "Components/rb_rarity_r", "Components/rb_rarity_sr", "Components/rb_rarity_ssr");
            Many(so, "_affIcons", "Icons/rb_icon_aff_blade_128", "Icons/rb_icon_aff_force_128", "Icons/rb_icon_aff_magic_128");
            so.FindProperty("_rankGold").objectReferenceValue = RbBuild.KitSprite("Icons/rb_icon_rank_gold_64");
            so.FindProperty("_rankSilver").objectReferenceValue = RbBuild.KitSprite("Icons/rb_icon_rank_silver_64");
            so.FindProperty("_shardIcon").objectReferenceValue = RbBuild.KitSprite("Icons/rb_icon_shard_48");
            so.FindProperty("_badgeNew").objectReferenceValue = RbBuild.KitSprite("Components/rb_badge_new");
            so.FindProperty("_barTrack").objectReferenceValue = RbBuild.KitSprite("Components/rb_bar_track");
            so.FindProperty("_barFill").objectReferenceValue = RbBuild.KitSprite("Components/rb_bar_fill_cyan");
            so.FindProperty("_btnOk").objectReferenceValue = RbBuild.KitSprite("Components/rb_btn_secondary");
            so.FindProperty("_btnAgain").objectReferenceValue = RbBuild.KitSprite("Components/rb_btn_primary");
            so.FindProperty("_additive").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseResource/Shaders/UIAdditiveSprite.mat");
            so.FindProperty("_silhouette").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseResource/Shaders/UISilhouette.mat");
            so.FindProperty("_font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BaseResource/Fonts/NotoSansKR-Bold SDF.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Many(SerializedObject so, string field, params string[] paths)
        {
            var p = so.FindProperty(field);
            p.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = RbBuild.KitSprite(paths[i]);
        }

        /// <summary>
        /// 규칙 09 — 아틀라스 밖 그림: 안드로이드 ASTC 4x4 · 최대 크기 2048 · 밉맵 끔 · Read/Write 끔. 무대(일러스트)는 Bilinear, 도트 빛 프레임은 Point.
        /// 다른 값일 때만 다시 들인다(로비 세우기마다 돌지 않게 메뉴로 뺐다).
        /// </summary>
        [MenuItem("Tools/Game/RE:BORN 화면/소환 연출 그림 포맷")]
        public static void ImportSettings()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                var filter = path.EndsWith("lab_stage.png") ? FilterMode.Bilinear : FilterMode.Point;
                var android = ti.GetPlatformTextureSettings("Android");
                bool ok = ti.textureType == TextureImporterType.Sprite && ti.spriteImportMode == SpriteImportMode.Single && ti.filterMode == filter
                          && !ti.mipmapEnabled && !ti.isReadable && ti.alphaIsTransparency
                          && android.overridden && android.format == TextureImporterFormat.ASTC_4x4 && android.maxTextureSize == 2048;
                if (ok) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = filter;
                ti.mipmapEnabled = false;
                ti.isReadable = false;
                ti.alphaIsTransparency = true;
                ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "Android", overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.ASTC_4x4,
                });
                ti.SaveAndReimport();
                n++;
            }
            Debug.Log($"[소환 배양실] 그림 포맷 — 고친 것 {n}개");
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
