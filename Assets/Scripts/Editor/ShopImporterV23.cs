using System.Collections.Generic;
using System.IO;
using Game.Module.InGame;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 정본 v2.3 SHOP_MASTER(챕터 3) + SHOP_INVENTORY(45줄)를 `ShopTable` 로 굽는다.
    ///
    /// 회복 값은 정본이 준다(CH1 호스트 35 / 고스트 25 …). 다만 **회복 값(가격)** 은
    /// 정본에 없다 — 표에 파는 물건만 있고 서비스 값이 빠져 있다.
    /// 그 챕터 커먼 카드 값을 그대로 쓴다. 카드 한 장과 회복 한 번이 같은 무게라야
    /// "카드를 살까 몸을 추스를까" 가 진짜 갈림길이 된다.
    /// </summary>
    public static class ShopImporterV23
    {
        private const string Dir = "Projects/AVSR/Canon/v23";
        private const string TablePath = "Assets/BundleResource/TableData/ShopTable.asset";

        [MenuItem("Tools/Game/Canon v2.3 임포트 (상점)")]
        public static void Import()
        {
            var master = ReadRows("SHOP_MASTER.json");
            var inv = ReadRows("SHOP_INVENTORY.json");
            if (master == null || inv == null) return;

            var table = AssetDatabase.LoadAssetAtPath<ShopTable>(TablePath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<ShopTable>();
                AssetDatabase.CreateAsset(table, TablePath);
            }

            // 챕터별 커먼 카드 값 = 회복 값
            var commonPrice = new Dictionary<int, int>();
            for (int i = 0; i < inv.Count; i++)
            {
                int ch = ChapterNo(Str(inv[i], "PoolID"));
                if (Str(inv[i], "ActualRarity") != "COMMON") continue;
                if (!commonPrice.ContainsKey(ch)) commonPrice[ch] = Int(inv[i], "Price");
            }

            var so = new SerializedObject(table);

            var chapters = so.FindProperty("_chapters");
            chapters.ClearArray();
            for (int i = 0; i < master.Count; i++)
            {
                var m = master[i];
                int ch = ChapterNo(Str(m, "Chapter"));
                chapters.InsertArrayElementAtIndex(i);
                var e = chapters.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("_chapter").intValue = ch;
                e.FindPropertyRelative("_offerCount").intValue = Int(m, "OfferCount");
                e.FindPropertyRelative("_hostHealPct").intValue = Int(m, "HostHeal");
                e.FindPropertyRelative("_ghostHealPct").intValue = Int(m, "GhostHeal");
                // CH2·CH3 는 커먼이 아예 없다(에픽·레전더리 위주). 그때는 가장 싼 값을 쓴다.
                e.FindPropertyRelative("_healPrice").intValue =
                    commonPrice.TryGetValue(ch, out var p) ? p : CheapestOf(inv, ch);
                e.FindPropertyRelative("_totalPurchaseLimit").intValue = Int(m, "TotalPurchaseLimit");
                e.FindPropertyRelative("_cardPurchaseLimit").intValue = Int(m, "CardPurchaseLimit");
            }

            var offers = so.FindProperty("_offers");
            offers.ClearArray();
            int n = 0;
            for (int i = 0; i < inv.Count; i++)
            {
                var r = inv[i];
                offers.InsertArrayElementAtIndex(n);
                var e = offers.GetArrayElementAtIndex(n++);
                var cardId = Str(r, "CardID");
                e.FindPropertyRelative("_chapter").intValue = ChapterNo(Str(r, "PoolID"));
                e.FindPropertyRelative("_cardId").stringValue = cardId;
                // 카드 키는 소문자 카드 ID 다 (`CardImporterV23` 와 같은 규칙)
                e.FindPropertyRelative("_buffKey").stringValue = cardId.ToLowerInvariant();
                e.FindPropertyRelative("_price").intValue = Int(r, "Price");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShopV23] 챕터 {master.Count} · 진열 {n}종");
        }

        private static int CheapestOf(List<Dictionary<string, string>> inv, int chapter)
        {
            int best = int.MaxValue;
            for (int i = 0; i < inv.Count; i++)
            {
                if (ChapterNo(Str(inv[i], "PoolID")) != chapter) continue;
                best = Mathf.Min(best, Int(inv[i], "Price"));
            }
            return best == int.MaxValue ? 50 : best;
        }

        private static int ChapterNo(string s)
            => s != null && s.Contains("CH2") ? 2 : s != null && s.Contains("CH3") ? 3 : 1;

        private static List<Dictionary<string, string>> ReadRows(string file)
        {
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, Dir, file);
            if (!File.Exists(path)) { Debug.LogError($"[ShopV23] {file} 없음"); return null; }
            return FlatJson.Rows(File.ReadAllText(path));
        }

        private static string Str(Dictionary<string, string> m, string k)
            => m.TryGetValue(k, out var v) ? v : string.Empty;

        private static int Int(Dictionary<string, string> m, string k)
            => m.TryGetValue(k, out var v) && int.TryParse(v, out var i) ? i : 0;
    }
}
