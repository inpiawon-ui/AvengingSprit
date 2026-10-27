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
            so.FindProperty("_plate").objectReferenceValue = Spr($"{Dir}/reveal_plate.png");
            so.FindProperty("_stampNew").objectReferenceValue = Spr($"{Dir}/stamp_new.png");
            so.FindProperty("_ribbonPickup").objectReferenceValue = Spr($"{Dir}/ribbon_pickup.png");
            // 시험판 2(PD 10-27) — 공개 빛은 하나(금빛 6장: 1장은 도는 빛살, 6장은 제자리 반짝). 등급 사다리 빛 · 배양액 색 판은 안 쓴다
            Frames(so, "_revealLight", "lab_reveal_light_");
            // 10회 칸 밑 이름판(꽝 · 좋음) · 아래 버튼(확인 · 10회 더) — RE:BORN 키트
            so.FindProperty("_labelPlate").objectReferenceValue = RbBuild.KitSprite("Components/rb_panel_base");
            so.FindProperty("_labelPlateGood").objectReferenceValue = RbBuild.KitSprite("Components/rb_cell_line_gold");
            so.FindProperty("_btnOk").objectReferenceValue = RbBuild.KitSprite("Components/rb_btn_secondary");
            so.FindProperty("_btnAgain").objectReferenceValue = RbBuild.KitSprite("Components/rb_btn_primary");
            so.FindProperty("_additive").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseResource/Shaders/UIAdditiveSprite.mat");
            so.FindProperty("_silhouette").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseResource/Shaders/UISilhouette.mat");
            so.FindProperty("_font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BaseResource/Fonts/NotoSansKR-Bold SDF.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
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
