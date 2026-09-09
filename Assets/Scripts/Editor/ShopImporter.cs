using Game.Character;
using Game.Module.InGame;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 상점 표를 굽는다 — **지금 있는 카드만, 여섯 챕터 전부** (2026-09-09).
    ///
    /// 예전 표(정본 v2.3 SHOP_INVENTORY 45줄)는 두 가지가 어긋나 있었다.
    ///
    ///   ① 카드 32종을 10종으로 갈면서 **45개 중 30개가 없는 카드**를 가리켰다.
    ///      상점 칸에 이름 대신 `C015` 같은 ID 가 그대로 떴다 — 살 수는 있는데
    ///      아무 일도 안 일어나는 물건이다.
    ///   ② **챕터 3까지만** 있었다. 4~6 챕터는 카드가 한 장도 안 걸리고,
    ///      회복값·회복량도 챕터 1 것으로 떨어져 후반에 42골드짜리 회복을 팔았다.
    ///
    /// 그래서 **카드 표를 읽어서** 굽는다. 카드를 늘리면 이 메뉴만 다시 누르면 된다.
    /// </summary>
    public static class ShopImporter
    {
        private const string TablePath = "Assets/BundleResource/TableData/ShopTable.asset";
        private const string CardPath = "Assets/BundleResource/TableData/BuffTable.asset";

        /// <summary>
        /// 챕터별 값. 1~3 은 **정본 그대로**(42/66 · 48/72/104 · 78/110/157),
        /// 4~6 은 그 곡선을 이어 1.44 배씩 올렸다.
        /// 열은 Common / Rare / Epic / Legendary 순이다.
        /// </summary>
        private static readonly int[][] Price =
        {
            new[] {  42,  42,  66,  66 },   // CH1
            new[] {  48,  72,  72, 104 },   // CH2
            new[] {  78, 110, 110, 157 },   // CH3
            new[] { 112, 158, 158, 226 },   // CH4
            new[] { 161, 228, 228, 326 },   // CH5
            new[] { 232, 328, 328, 469 },   // CH6
        };

        /// <summary>챕터별 회복 — 호스트% · 고스트% · 값. 뒤로 갈수록 덜 주고 비싸다.</summary>
        private static readonly int[][] Heal =
        {
            new[] { 35, 25,  42 },
            new[] { 30, 22,  48 },
            new[] { 25, 18,  78 },
            new[] { 22, 16, 112 },
            new[] { 20, 15, 161 },
            new[] { 18, 14, 232 },
        };

        private const int Chapters = 6;
        private const int OfferCount = 3;          // 한 번에 진열하는 카드 칸
        private const int TotalPurchaseLimit = 2;  // 한 상점에서 살 수 있는 총 횟수
        private const int CardPurchaseLimit = 1;   // 그중 카드는 한 장까지

        [MenuItem("Tools/Game/상점 표 임포트")]
        public static void Import()
        {
            var cards = AssetDatabase.LoadAssetAtPath<BuffTable>(CardPath);
            if (cards == null) { Debug.LogError("[상점] BuffTable 없음"); return; }
            var table = AssetDatabase.LoadAssetAtPath<ShopTable>(TablePath);
            if (table == null) { Debug.LogError("[상점] ShopTable 없음"); return; }

            var so = new SerializedObject(table);
            var chapters = so.FindProperty("_chapters");
            var offers = so.FindProperty("_offers");
            chapters.ClearArray();
            offers.ClearArray();

            for (int c = 0; c < Chapters; c++)
            {
                chapters.InsertArrayElementAtIndex(c);
                var e = chapters.GetArrayElementAtIndex(c);
                e.FindPropertyRelative("_chapter").intValue = c + 1;
                e.FindPropertyRelative("_offerCount").intValue = OfferCount;
                e.FindPropertyRelative("_hostHealPct").intValue = Heal[c][0];
                e.FindPropertyRelative("_ghostHealPct").intValue = Heal[c][1];
                e.FindPropertyRelative("_healPrice").intValue = Heal[c][2];
                e.FindPropertyRelative("_totalPurchaseLimit").intValue = TotalPurchaseLimit;
                e.FindPropertyRelative("_cardPurchaseLimit").intValue = CardPurchaseLimit;
            }

            // 카드는 **전부** 모든 챕터에 건다. 뽑기는 진열할 때 세 장을 고른다.
            int n = 0;
            for (int c = 0; c < Chapters; c++)
                for (int i = 0; i < cards.Entries.Count; i++)
                {
                    var card = cards.Entries[i];
                    offers.InsertArrayElementAtIndex(n);
                    var o = offers.GetArrayElementAtIndex(n);
                    o.FindPropertyRelative("_chapter").intValue = c + 1;
                    o.FindPropertyRelative("_cardId").stringValue = card.CardId;
                    o.FindPropertyRelative("_buffKey").stringValue = card.BuffKey;
                    o.FindPropertyRelative("_price").intValue = Price[c][(int)card.Rarity];
                    n++;
                }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[상점] 챕터 {Chapters} · 진열 후보 {n}개 (카드 {cards.Entries.Count}종 × {Chapters}챕터)");
        }
    }
}
