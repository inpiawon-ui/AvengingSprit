using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Module.Common.Store;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// `Projects/AVSR/Store/*.tsv` → `StoreTable.asset`(주소 `TableData/StoreTable`). 값은 TSV 만 고친다.
    /// </summary>
    public static class StoreTableImporter
    {
        private const string Dir = "Projects/AVSR/Store";
        private const string TablePath = "Assets/BundleResource/TableData/StoreTable.asset";
        private const string Address = "TableData/StoreTable";
        private const string GachaPath = "Assets/BundleResource/TableData/GachaTable.asset";
        private const string GachaAddress = "TableData/GachaTable";
        private const string SeasonPath = "Assets/BundleResource/TableData/SeasonTable.asset";
        private const string SeasonAddress = "TableData/SeasonTable";

        [MenuItem("Tools/Game/로비 상점 표 임포트 (Store)")]
        public static void Run()
        {
            var products = new List<StoreProduct>();
            foreach (var r in Rows("store_products.tsv"))
                products.Add(new StoreProduct
                {
                    Id = r["id"], Tab = r["tab"], Kind = r["kind"], PriceType = r["priceType"], Price = r["price"],
                    Limit = r["limit"], Show = r["show"], Rewards = r["rewards"], Badge = r["badge"],
                    Order = Int(r["order"]), StoreId = r["storeId"], NameKey = r["nameKey"], DescKey = r["descKey"],
                });
            var tiers = new List<PriceTier>();
            foreach (var r in Rows("price_tiers.tsv"))
                tiers.Add(new PriceTier
                {
                    Tier = r["tier"], Krw = Int(r["krw"]),
                    Usd = float.TryParse(r["usd"], NumberStyles.Float, CultureInfo.InvariantCulture, out var u) ? u : 0f,
                });
            var cards = new List<StoreCard>();
            foreach (var r in Rows("cards.tsv"))
                cards.Add(new StoreCard
                {
                    Key = r["key"], ProductId = r["productId"], Days = Int(r["days"]),
                    InstantGem = Int(r["instantGem"]), DailyGem = Int(r["dailyGem"]), Perks = r["perks"],
                });
            products.Sort((a, b) => a.Order.CompareTo(b.Order));

            var table = AssetDatabase.LoadAssetAtPath<StoreTable>(TablePath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<StoreTable>();
                AssetDatabase.CreateAsset(table, TablePath);
            }
            table.Fill(products.ToArray(), tiers.ToArray(), cards.ToArray());
            EditorUtility.SetDirty(table);

            Register(TablePath, Address);

            // 조각 뽑기 표 — gacha.tsv(kind: setting · grade · amount)
            var sets = new List<GachaTable.Setting>();
            var grades = new List<GachaTable.GradeRate>();
            var amounts = new List<GachaTable.AmountRate>();
            foreach (var r in Rows("gacha.tsv"))
            {
                float.TryParse(r["value"], NumberStyles.Float, CultureInfo.InvariantCulture, out var pct);
                switch (r["kind"])
                {
                    case "setting": sets.Add(new GachaTable.Setting { Key = r["key"], Value = r["value"] }); break;
                    case "grade": grades.Add(new GachaTable.GradeRate { Key = r["key"], Percent = pct }); break;
                    case "amount": amounts.Add(new GachaTable.AmountRate { Shards = Int(r["key"]), Percent = pct }); break;
                }
            }
            var gacha = AssetDatabase.LoadAssetAtPath<GachaTable>(GachaPath);
            if (gacha == null)
            {
                gacha = ScriptableObject.CreateInstance<GachaTable>();
                AssetDatabase.CreateAsset(gacha, GachaPath);
            }
            gacha.Fill(sets.ToArray(), grades.ToArray(), amounts.ToArray());
            EditorUtility.SetDirty(gacha);
            Register(GachaPath, GachaAddress);

            // 시즌 패스 단계 · 미션 · 프로필
            var tiersS = new List<SeasonTable.Tier>();
            foreach (var r in Rows("pass_s1.tsv"))
                tiersS.Add(new SeasonTable.Tier { Level = Int(r["tier"]), Xp = Int(r["xp"]), Free = r["free"], Premium = r["premium"] });
            var missions = new List<SeasonTable.Mission>();
            foreach (var r in Rows("missions.tsv"))
                missions.Add(new SeasonTable.Mission
                {
                    Id = r["id"], Period = r["period"], Type = r["type"], Target = Int(r["target"]),
                    PassXp = Int(r["passXp"]), Rewards = r["rewards"],
                });
            var profile = new List<SeasonTable.ProfileItem>();
            foreach (var r in Rows("profile.tsv"))
                profile.Add(new SeasonTable.ProfileItem
                {
                    Key = r["key"], Type = r["type"], Art = r["art"], Source = r["source"], NameKey = r["nameKey"], Order = Int(r["order"]),
                });
            var season = AssetDatabase.LoadAssetAtPath<SeasonTable>(SeasonPath);
            if (season == null)
            {
                season = ScriptableObject.CreateInstance<SeasonTable>();
                AssetDatabase.CreateAsset(season, SeasonPath);
            }
            season.Fill(tiersS.ToArray(), missions.ToArray(), profile.ToArray());
            EditorUtility.SetDirty(season);
            Register(SeasonPath, SeasonAddress);

            AssetDatabase.SaveAssets();
            Debug.Log($"[상점 표] 상품 {products.Count} · 가격 등급 {tiers.Count} · 계약 {cards.Count} · 뽑기 설정 {sets.Count} · 등급 {grades.Count} · 조각 수 {amounts.Count}");
        }

        private static void Register(string path, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup("tabledata") ?? settings.CreateGroup("tabledata", false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
            entry.address = address;
            entry.SetLabel("label_tabledata", true);
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(settings);
        }

        private static int Int(string s) => int.TryParse(s, out var v) ? v : 0;

        /// <summary>`#` 줄은 설명. 첫 줄(머리)이 열 이름 — 파일 안에 머리가 여럿이면 그때마다 바뀐다(cards.tsv).</summary>
        private static IEnumerable<Dictionary<string, string>> Rows(string file)
        {
            string[] head = null;
            foreach (var raw in File.ReadAllLines(Path.Combine(Dir, file)))
            {
                var line = raw.TrimEnd('\r');
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var cells = line.Split('\t');
                if (head == null || cells[0] == head[0]) { head = cells; continue; }
                var d = new Dictionary<string, string>();
                for (int i = 0; i < head.Length; i++) d[head[i]] = i < cells.Length ? cells[i].Trim() : string.Empty;
                yield return d;
            }
        }
    }
}
