using Game.Module.Common.Modes;
using Game.Module.InGame;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 추가 모드 표(`TableData/ModeTable`)를 만든다 — 망령의 탑 · 망령의 밤 (기획 2026-10-15).
    /// 값의 근거: Projects/AVSR/AVSR_Content_Tower.md 3-5 · 3-6 · 4-4 / AVSR_Content_NightMode.md 3-4 · 3-5 · 4-1.
    /// ⚠ 보상 숫자 · 젬 값은 가안(TBD-BAL). 밸런스는 이 메뉴만 고쳐 다시 돌린다.
    /// </summary>
    public static class ModeTableBuilder
    {
        private const string TablePath = "Assets/BundleResource/TableData/ModeTable.asset";
        private const string Address = "TableData/ModeTable";

        // 탁 트인 1챕터 첫 방(모래주머니 둘뿐) — 밤 아레나 · 탑 층 바닥. 층마다 다른 방은 라이브 때 표에서 바꾼다.
        private const string OpenRoom = "ROOM_CH1_001";

        [MenuItem("Tools/Game/모드/모드 표 만들기")]
        public static void Run()
        {
            // 거리 분류 — 사거리 배율(_rangeMul) 0.6 이하 근거리 · 0.7~1.0 중거리 · 1.05 이상 원거리.
            // 코만도(미사일)은 배율 0.5 지만 유도 폭격이라 원거리(기획서 7절 확인 필요). 사신은 히든이라 뺀다.
            var ranges = new[]
            {
                new RangeEntry("amazon", RangeClass.Melee), new RangeEntry("amazon_elite", RangeClass.Melee),
                new RangeEntry("ninja_chain", RangeClass.Melee), new RangeEntry("baseball", RangeClass.Melee),
                new RangeEntry("vampire", RangeClass.Melee), new RangeEntry("salamander", RangeClass.Melee),
                new RangeEntry("dragoon", RangeClass.Melee), new RangeEntry("dragon_blue", RangeClass.Melee),
                new RangeEntry("guru", RangeClass.Melee),
                new RangeEntry("gangster", RangeClass.Mid), new RangeEntry("thug", RangeClass.Mid),
                new RangeEntry("hopper", RangeClass.Mid), new RangeEntry("hopper_smg", RangeClass.Mid),
                new RangeEntry("commando_mg", RangeClass.Mid), new RangeEntry("ninja", RangeClass.Mid),
                new RangeEntry("commando_laser", RangeClass.Long), new RangeEntry("commando_grenade", RangeClass.Long),
                new RangeEntry("white_wizard", RangeClass.Long), new RangeEntry("medium", RangeClass.Long),
                new RangeEntry("robot", RangeClass.Long), new RangeEntry("snowwoman", RangeClass.Long),
                new RangeEntry("commando_missile", RangeClass.Long),
            };

            string[] sbb = { "skeleton", "bat", "boar" };
            string[] ssk = { "scrapgunner", "skeleton" };
            string[] bsk = { "boar", "scrapgunner", "skeleton" };
            string[] bbb = { "bat", "bat", "boar" };
            string[] sgb = { "skeleton", "scrapgunner", "boar" };
            string[] gsb = { "scrapgunner", "skeleton", "boar" };
            string[] none = System.Array.Empty<string>();

            // 처음 10층(한 바퀴) — 11층부터 되풀이하며 단단해진다(ModeTable.FloorAt · TowerHpMul)
            var floors = new[]
            {
                F(1, TowerRule.BodyRange, RangeClass.Melee, Affinity.None, "", sbb, 6),
                F(2, TowerRule.BodyRange, RangeClass.Long, Affinity.None, "", ssk, 7),
                F(3, TowerRule.EnemyAffinity, RangeClass.Mid, Affinity.Magic, "", none, 8),
                F(4, TowerRule.Elite, RangeClass.Mid, Affinity.None, "", bsk, 4),   // 탈출이 없어져(10-15) 「첫 몸으로 끝까지」 → 정예 층(PD 10-16)
                F(5, TowerRule.Boss, RangeClass.Mid, Affinity.Magic, "crusher", none, 0),
                F(6, TowerRule.BodyRange, RangeClass.Mid, Affinity.None, "", bbb, 9),
                F(7, TowerRule.SingleHost, RangeClass.Mid, Affinity.None, "", sgb, 7),
                F(8, TowerRule.EnemyAffinity, RangeClass.Mid, Affinity.Force, "", none, 9),
                F(9, TowerRule.OneShot, RangeClass.Mid, Affinity.None, "", gsb, 7),
                F(10, TowerRule.Boss, RangeClass.Mid, Affinity.Force, "kingpin", none, 0),
            };

            // 소탕 계단(가안) — 이번 주 깬 층 기준
            var sweep = new[]
            {
                new SweepStep(1, 1, 100), new SweepStep(10, 2, 200), new SweepStep(20, 3, 300),
                new SweepStep(30, 4, 400), new SweepStep(40, 5, 500),
            };

            // 밤 요일 — 0 = 월 … 6 = 일 (IResetClock 의 하루 기준 요일)
            var days = new[]
            {
                new NightDay("mon", NightFilter.Range, RangeClass.Melee, Affinity.None, ""),
                new NightDay("tue", NightFilter.Range, RangeClass.Long, Affinity.None, ""),
                new NightDay("wed", NightFilter.Range, RangeClass.Mid, Affinity.None, ""),
                new NightDay("thu", NightFilter.Affinity, RangeClass.Mid, Affinity.Force, ""),
                new NightDay("fri", NightFilter.Affinity, RangeClass.Mid, Affinity.Blade, ""),
                new NightDay("sat", NightFilter.Affinity, RangeClass.Mid, Affinity.Magic, ""),
                new NightDay("sun", NightFilter.All, RangeClass.Mid, Affinity.None, ""),
            };

            // 밤 무리 잔몹 — 설치물 · 포탑 제외(기획서 3-5)
            string[] trashBlade = { "bat", "scrapgunner", "roadwarden", "mantis", "mole" };
            string[] trashForce = { "boar", "armadillo" };
            string[] trashMagic = { "skeleton", "mushroom", "mummy", "spider", "sludge", "coilwalker" };

            var table = AssetDatabase.LoadAssetAtPath<ModeTable>(TablePath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<ModeTable>();
                AssetDatabase.CreateAsset(table, TablePath);
            }
            table.Fill(ranges, floors, sweep, days, trashBlade, trashForce, trashMagic, OpenRoom);
            EditorUtility.SetDirty(table);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings != null ? settings.FindGroup("tabledata") : null;
            if (group == null)
            {
                Debug.LogError("[모드 표] Addressable 그룹 tabledata 없음");
            }
            else
            {
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(TablePath), group);
                entry.address = Address;
                entry.SetLabel("label_tabledata", true);
                EditorUtility.SetDirty(group);
                EditorUtility.SetDirty(settings);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[모드 표] 탑 {floors.Length}층 · 밤 요일 {days.Length} · 거리 분류 {ranges.Length} — {TablePath}");
        }

        private static TowerFloor F(int floor, TowerRule rule, RangeClass range, Affinity affinity, string boss,
                                    string[] trash, int count)
            => new(floor, $"tower.floor.{floor}.name", $"tower.floor.{floor}.desc",
                   rule, range, affinity, boss, trash, count, OpenRoom);
    }
}
