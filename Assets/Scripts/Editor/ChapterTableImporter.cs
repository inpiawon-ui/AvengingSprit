using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Character;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 챕터 표(`Projects/AVSR/Rooms/chapters.tsv`)를 읽는다 — **챕터에 관한 모든 값의 단일 출처.**
    ///
    /// 같은 표를 세 곳이 읽는다: 방 배치 검사(`Tools/rooms90_build.py`), 방 임포터(`RoomImporter60`),
    /// 그리고 여기(`GameConfig._chapters`). 새 챕터는 그 표에 한 줄을 더하고 임포트 둘을 돌리면 끝이다 —
    /// 코드에 챕터 번호를 적지 않는다(목표 100챕터, 기획 2026-10-01).
    /// </summary>
    public static class ChapterTsv
    {
        public const string Path = "Projects/AVSR/Rooms/chapters.tsv";

        public sealed class Row
        {
            public int Ch;
            public string Stage, Boss, Leader, Lean, Chest, Name, Desc;
            public string[] Trash;
            public int Pattern, BossHp, BossAtk, RoomGold, MidGold, BossGold, ClearGold, Exp;
            public int Music, Art, BossArt, ChestArt;
            public float HpMul, AtkMul, PriceMul;
        }

        public static List<Row> Load()
        {
            var rows = new List<Row>();
            if (!File.Exists(Path))
            {
                Debug.LogError("[챕터 표] 파일이 없다: " + Path);
                return rows;
            }

            string[] head = null;
            var inv = CultureInfo.InvariantCulture;
            foreach (var raw in File.ReadAllLines(Path))
            {
                if (string.IsNullOrWhiteSpace(raw) || raw[0] == '#') continue;
                var t = raw.Split('\t');
                if (head == null) { head = t; continue; }

                var d = new Dictionary<string, string>();
                for (int i = 0; i < head.Length && i < t.Length; i++) d[head[i]] = t[i].Trim();
                string S(string k) => d.TryGetValue(k, out var v) ? v : string.Empty;
                int I(string k) => int.TryParse(S(k), NumberStyles.Integer, inv, out var v) ? v : 0;
                float F(string k) => float.TryParse(S(k), NumberStyles.Float, inv, out var v) ? v : 0f;

                rows.Add(new Row
                {
                    Ch = I("ch"), Stage = S("stage"), Boss = S("boss"), Leader = S("leader"),
                    Trash = S("trash").Split(','), Lean = S("lean"), Pattern = I("pattern"),
                    HpMul = F("hpMul"), AtkMul = F("atkMul"), BossHp = I("bossHp"), BossAtk = I("bossAtk"),
                    RoomGold = I("roomGold"), MidGold = I("midGold"), BossGold = I("bossGold"),
                    ClearGold = I("clearGold"), Chest = S("chest"), Exp = I("exp"), PriceMul = F("priceMul"),
                    Music = I("music"), Art = I("art"), BossArt = I("bossArt"), ChestArt = I("chestArt"),
                    Name = S("name"), Desc = S("desc").Replace("\\n", "\n"),
                });
            }
            rows.Sort((a, b) => a.Ch.CompareTo(b.Ch));
            return rows;
        }

        /// <summary>그 챕터의 줄. 없으면 null.</summary>
        public static Row Of(List<Row> rows, int chapter)
        {
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Ch == chapter) return rows[i];
            return null;
        }
    }

    public static class ChapterTableImporter
    {
        private const string ConfigPath = "Assets/BundleResource/TableData/GameConfig.asset";

        /// <summary>잡몹의 쪽 — `AffinityRule.KindOf` · `rooms90_build.py` 의 KIND 와 같아야 한다.</summary>
        private static int KindIndex(string actor) => actor switch
        {
            "bat" or "roadwarden" or "scrapgunner" or "mole" or "mantis" => 0,   // 무기
            "actor_enforcer" or "boar" or "armadillo" or "turret_cross" => 1,   // 파워
            "skeleton" or "coilwalker" or "mushroom" or "mummy" or "spider" => 2,   // 마법
            _ => -1,
        };

        [MenuItem("Tools/Game/챕터 표 임포트")]
        public static void Import()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null) { Debug.LogError("[챕터 표] GameConfig 없음: " + ConfigPath); return; }
            var rows = ChapterTsv.Load();
            if (rows.Count == 0) return;

            // 번호가 1부터 빠짐없이 이어져야 한다 — 표의 첨자가 곧 챕터 번호다.
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Ch != i + 1)
                {
                    Debug.LogError($"[챕터 표] 챕터 번호가 이어지지 않는다: {i + 1}번째 줄이 CH{rows[i].Ch}");
                    return;
                }

            // 잡몹의 무기 · 파워 · 마법 수는 손으로 적지 않는다 — 구운 방 표에서 센다.
            var mix = new Dictionary<int, int[]>();
            foreach (var kv in Rooms90Hand.Load())
            {
                int us = kv.Key.IndexOf('_', 5);          // ROOM_CH{n}_{no}
                if (us < 0 || !int.TryParse(kv.Key.Substring(7, us - 7), out int ch)) continue;
                if (!mix.TryGetValue(ch, out var m)) mix[ch] = m = new int[3];
                foreach (var s in kv.Value.Spawns)
                {
                    if (s.Host) continue;
                    int k = KindIndex(s.Actor);
                    if (k >= 0) m[k]++;
                }
            }

            var so = new SerializedObject(config);
            var arr = so.FindProperty("_chapters");
            arr.ClearArray();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                arr.InsertArrayElementAtIndex(i);
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Chapter").intValue = r.Ch;
                e.FindPropertyRelative("Name").stringValue = r.Name;
                e.FindPropertyRelative("Desc").stringValue = r.Desc;
                e.FindPropertyRelative("Stage").stringValue = r.Stage;
                e.FindPropertyRelative("Boss").stringValue = r.Boss;
                e.FindPropertyRelative("Leader").stringValue = r.Leader;
                var trash = e.FindPropertyRelative("Trash");
                trash.ClearArray();
                for (int k = 0; k < r.Trash.Length; k++)
                {
                    trash.InsertArrayElementAtIndex(k);
                    trash.GetArrayElementAtIndex(k).stringValue = r.Trash[k].Trim();
                }
                e.FindPropertyRelative("Pattern").intValue = r.Pattern;
                e.FindPropertyRelative("EnemyHpMul").floatValue = r.HpMul;
                e.FindPropertyRelative("EnemyAtkMul").floatValue = r.AtkMul;
                e.FindPropertyRelative("BossHp").intValue = r.BossHp;
                e.FindPropertyRelative("BossAtk").intValue = r.BossAtk;
                e.FindPropertyRelative("ClearGold").intValue = r.ClearGold;
                e.FindPropertyRelative("Chest").stringValue = r.Chest;
                e.FindPropertyRelative("ClearExp").intValue = r.Exp;
                e.FindPropertyRelative("PriceMul").floatValue = r.PriceMul;
                // 전투방 11 · 중간보스 1 · 보스 1
                e.FindPropertyRelative("RunGold").intValue = r.RoomGold * 11 + r.MidGold + r.BossGold;
                e.FindPropertyRelative("Music").intValue = r.Music;
                e.FindPropertyRelative("Art").intValue = r.Art;
                e.FindPropertyRelative("BossArt").intValue = r.BossArt;
                e.FindPropertyRelative("ChestArt").intValue = r.ChestArt;
                mix.TryGetValue(r.Ch, out var m);
                e.FindPropertyRelative("MixBlade").intValue = m != null ? m[0] : 0;
                e.FindPropertyRelative("MixForce").intValue = m != null ? m[1] : 0;
                e.FindPropertyRelative("MixMagic").intValue = m != null ? m[2] : 0;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[챕터 표] {rows.Count}챕터를 GameConfig 에 구웠다");
        }
    }
}
