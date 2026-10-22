using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Character;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using T = Game.Character.TrashTrait;

namespace Game.EditorTools
{
    /// <summary>
    /// 몬스터 표(`TrashTable`)를 굽는다 — 2단계 몬스터 조합(PD 2026-10-22, `_exchange/plan_content_expand/step2_monsters.html`).
    ///
    /// 몬스터 = **몸(그림 · 행동 코드) + 역할 + 원소 + 성질 + 수치.** 같은 몸을 여러 종이 같이 쓴다.
    /// 예전 「챕터 단계」 행동은 전부 여기서 이름 붙은 종이 됐다(단계 문턱 상수는 걷어냈다).
    /// 늘릴 때는 아래 `Roster` 에 줄 하나를 더하고 메뉴를 다시 누른다. **키는 한 번 준 것을 바꾸지 않는다**(챕터 표가 키로 부른다).
    ///
    /// 같이 쓰는 파일 `Projects/AVSR/Rooms/trash_roster.tsv` — 방 굽기 도구(`rooms90_build.py`)가 키 → 몸 · 역할을 여기서 읽는다.
    /// </summary>
    public static class TrashImporter
    {
        private const string TablePath = "Assets/BundleResource/TableData/TrashTable.asset";
        private const string Address = "TableData/TrashTable";
        private const string Group = "tabledata";
        private const string Label = "label_tabledata";
        private const string RosterTsv = "Projects/AVSR/Rooms/trash_roster.tsv";

        /// <summary>몸 하나의 기본 수치 — Lv1 이 이 값이다(예전 `CreateTrash` 값 그대로).</summary>
        private sealed class Body
        {
            public string Key, Name;
            public AttackKind Kind;
            public TrashRole Role;
            public int Hp, Atk;
            public float Move, Engage, Range, Interval, Tell;
            public int Shots = 1;
            public float Spread;
        }

        private static Body B(string key, string name, AttackKind kind, TrashRole role, int hp, int atk,
                              float move, float engage, float range, float interval, float tell,
                              int shots = 1, float spread = 0f)
            => new() { Key = key, Name = name, Kind = kind, Role = role, Hp = hp, Atk = atk, Move = move,
                       Engage = engage, Range = range, Interval = interval, Tell = tell, Shots = shots, Spread = spread };

        private static readonly Body[] Bodies =
        {
            B("skeleton", "해골", AttackKind.Melee, TrashRole.Melee, 28, 6, 1.2f, 2.4f, 1.4f, 1.4f, 0.45f),
            B("bat", "박쥐", AttackKind.Melee, TrashRole.Flying, 18, 4, 2.6f, 4.6f, 1.2f, 0.9f, 0.25f),
            B("actor_enforcer", "돌 고릴라", AttackKind.Melee, TrashRole.Melee, 52, 9, 1.0f, 2.0f, 1.6f, 1.7f, 0.6f),
            B("scrapgunner", "고철 전갈", AttackKind.Single, TrashRole.Ranged, 24, 7, 1.4f, 2.2f, 10.4f, 1.9f, 0.75f),
            B("roadwarden", "순찰기", AttackKind.Single, TrashRole.Ranged, 34, 9, 1.1f, 1.9f, 11.2f, 1.8f, 0.75f, 3, 40f),
            B("coilwalker", "코일 보행기", AttackKind.Single, TrashRole.Ranged, 26, 10, 1.6f, 2.4f, 12.4f, 2.4f, 0.7f),
            B("obj_turret", "십자 포탑", AttackKind.Single, TrashRole.Turret, 30, 8, 0f, 0f, 99f, 2.2f, 0.8f),
            // 빨강 5종(멧돼지 · 사마귀 · 아르마딜로 · 버섯 · 원숭이)은 원작 결(기계)로 겉모습을 바꿨다(PD 2026-10-22) — 키 · 하는 일은 그대로
            B("boar", "돌진 쇄석기", AttackKind.Melee, TrashRole.Melee, 30, 8, 1.6f, 2.4f, 1.2f, 1.6f, 0.7f),
            B("mole", "드릴 두더지", AttackKind.Single, TrashRole.Ranged, 28, 8, 1.6f, 2.4f, 10f, 2.2f, 0.6f),
            B("mantis", "삼각대 저격기", AttackKind.Single, TrashRole.Ranged, 26, 9, 1.4f, 2.0f, 12f, 2.4f, 1.0f),
            B("armadillo", "방패 로봇", AttackKind.Melee, TrashRole.Melee, 40, 9, 0.9f, 1.2f, 1.1f, 1.6f, 0.5f),
            B("mushroom", "폭탄 드럼통", AttackKind.Melee, TrashRole.Melee, 18, 16, 1.5f, 2.0f, 1.3f, 9f, 1.0f),
            B("mummy", "미라", AttackKind.Single, TrashRole.Ranged, 30, 8, 0.8f, 1.0f, 11f, 3.2f, 0.7f),
            B("spider", "알 거미", AttackKind.Single, TrashRole.Support, 34, 6, 1.2f, 1.6f, 10f, 4.5f, 0.6f),
            B("spiderling", "새끼 거미", AttackKind.Melee, TrashRole.Melee, 6, 4, 2.4f, 3.0f, 0.8f, 1.0f, 0.25f),

            // ── 2단계 새 모양(그림은 라인업 시안 통과 뒤 발주 — 오기 전엔 행동만 시험한다) ──
            // 감시 눈 — 떠다니며 예고선 레이저. 맞으면 아프지만 쏘는 동안 멈춰 선다(근접 틈)
            B("watcher_eye", "감시 눈", AttackKind.Single, TrashRole.Flying, 26, 10, 1.3f, 1.8f, 11f, 3.0f, 0.8f),
            // 그림자 살쾡이 — 멀면 사라졌다 옆에 나타나 벤다. 벤 뒤 1초 선다
            B("shadow_cat", "그림자 살쾡이", AttackKind.Melee, TrashRole.Melee, 24, 9, 1.6f, 2.8f, 1.3f, 1.5f, 0.5f),
            // 톱날 투척기 — 거리를 두고 돌아오는 톱날. 톱날이 날아가 있는 동안은 발사대가 빈다
            B("saw_monkey", "톱날 투척기", AttackKind.Single, TrashRole.Ranged, 30, 9, 1.3f, 2.0f, 9f, 3.0f, 0.6f),
            // 미믹 상자 — 물건인 척 가만히. 깨면 깨물고 원형 탄
            B("mimic", "미믹 상자", AttackKind.Melee, TrashRole.Melee, 44, 11, 1.1f, 2.2f, 1.3f, 1.8f, 0.5f),
            // 수호 등불 — 곁의 적에게 방패를 씌우고 뒤로 숨는다. 직접은 약하다
            B("lantern", "수호 등불", AttackKind.Single, TrashRole.Support, 22, 4, 1.4f, 1.8f, 8f, 3.5f, 0.6f),
            // 혼 사냥개는 뺐다(PD 2026-10-22 「개념 자체가 잘 이해가 안 된다」) — 그림 · 발주문은 Projects/AVSR/_archive/soul_hound_1022
        };

        private readonly struct Row
        {
            public readonly string Key, Body, Name;
            public readonly int Level;
            public readonly TrashElement Element;
            public readonly T Traits;
            public Row(string key, string body, string name, int level, T traits, TrashElement element = TrashElement.None)
            { Key = key; Body = body; Name = name; Level = level; Traits = traits; Element = element; }
        }

        private static Row R(string key, string body, string name, int lv, T traits, TrashElement el = TrashElement.None)
            => new(key, body, name, lv, traits, el);

        private const TrashElement Fire = TrashElement.Fire, Ice = TrashElement.Ice, Poison = TrashElement.Poison;

        // 몬스터 목록 — 2단계 문서 「몬스터 목록」 표와 같은 차례.
        private static readonly Row[] Roster =
        {
            // ① 예전 단계 행동 → 변종
            R("skeleton", "skeleton", "해골", 1, T.None),
            R("skeleton_2", "skeleton", "터지는 해골", 2, T.DeathBurst4),
            R("skeleton_3", "skeleton", "숨는 해골", 3, T.Ambush | T.DeathBurst4),
            R("skeleton_4", "skeleton", "무덤 해골", 4, T.Ambush | T.DeathBurst8),
            R("skeleton_mage", "skeleton", "해골 술사", 3, T.Ring),
            R("bat", "bat", "박쥐", 1, T.Dive),
            R("bat_2", "bat", "흔드는 박쥐", 2, T.Dive | T.Weave),
            R("actor_enforcer", "actor_enforcer", "돌 고릴라", 1, T.None),
            R("actor_enforcer_2", "actor_enforcer", "찍는 고릴라", 2, T.Slam),
            R("actor_enforcer_3", "actor_enforcer", "날뛰는 고릴라", 3, T.Slam | T.GorillaMix),
            R("actor_enforcer_4", "actor_enforcer", "갈라지는 고릴라", 4, T.Slam | T.GorillaMix | T.SplitBats),
            R("gorilla_charger", "actor_enforcer", "돌격 고릴라", 4, T.Slam | T.GorillaMix | T.ChargeFirst),
            R("scrapgunner", "scrapgunner", "고철 전갈", 1, T.None),
            R("scrapgunner_2", "scrapgunner", "연발 전갈", 2, T.GunnerBurst),
            R("scrapgunner_3", "scrapgunner", "속사 전갈", 3, T.GunnerBurst | T.Burst4),
            R("scrapgunner_sniper", "scrapgunner", "저격 전갈", 3, T.GunnerBurst | T.Snipe),
            R("roadwarden", "roadwarden", "순찰기", 1, T.None),
            R("roadwarden_2", "roadwarden", "연발 순찰기", 2, T.WardenBurst),
            R("roadwarden_3", "roadwarden", "회오리 순찰기", 3, T.Spiral),
            R("coilwalker", "coilwalker", "코일 보행기", 1, T.None),
            R("coilwalker_2", "coilwalker", "튕김 코일", 2, T.BounceShot),
            R("coilwalker_3", "coilwalker", "갈래 코일", 3, T.SplitShot),
            R("coil_mortar", "coilwalker", "박격 코일", 3, T.Mortar),
            R("obj_turret", "obj_turret", "십자 포탑", 1, T.None),
            R("obj_turret_2", "obj_turret", "팔방 포탑", 2, T.CrossAlt),
            R("obj_turret_3", "obj_turret", "회전 포탑", 3, T.CrossStream),
            R("boar", "boar", "돌진 쇄석기", 1, T.Dive),
            R("boar_2", "boar", "폭주 쇄석기", 2, T.Dive | T.BoarChain),

            // ② 단계 변화가 없던 몹 + 새 Lv2
            R("mole", "mole", "드릴 두더지", 1, T.None),
            R("mole_2", "mole", "쌍굴 두더지", 2, T.MoleDouble),
            R("mantis", "mantis", "삼각대 저격기", 1, T.None),
            R("mantis_2", "mantis", "쌍발 저격기", 2, T.MantisDouble),
            R("armadillo", "armadillo", "방패 로봇", 1, T.None),
            R("armadillo_2", "armadillo", "구르는 방패 로봇", 2, T.ArmadilloRoll),
            R("mushroom", "mushroom", "폭탄 드럼통", 1, T.None),
            R("mushroom_2", "mushroom", "독 드럼통", 2, T.MushroomPool),
            R("mummy", "mummy", "미라", 1, T.None),
            R("mummy_2", "mummy", "겹고리 미라", 2, T.MummyDouble),
            R("spider", "spider", "알 거미", 1, T.None),
            R("spider_2", "spider", "독샘 거미", 2, T.SpiderPool),
            R("spiderling", "spiderling", "새끼 거미", 1, T.None),
            R("bat_bounce", "bat", "튕기는 박쥐", 2, T.BatBounce),

            // ③ 원소판
            R("gorilla_fire", "actor_enforcer", "불 고릴라", 3, T.Slam, Fire),
            R("roadwarden_fire", "roadwarden", "불 순찰기", 2, T.None, Fire),
            R("scrapgunner_ice", "scrapgunner", "얼음 전갈", 2, T.GunnerBurst, Ice),
            R("coilwalker_ice", "coilwalker", "얼음 코일", 3, T.Mortar, Ice),
            R("skeleton_poison", "skeleton", "독 해골", 2, T.None, Poison),
            R("bat_poison", "bat", "독 박쥐", 2, T.Dive, Poison),

            // ④ 새 모양 · 우리 게임만의 안
            R("watcher_eye", "watcher_eye", "감시 눈", 1, T.Laser),
            R("shadow_cat", "shadow_cat", "그림자 살쾡이", 1, T.Teleport),
            R("saw_monkey", "saw_monkey", "톱날 투척기", 1, T.Boomerang),
            R("mimic", "mimic", "미믹 상자", 1, T.Mimic),
            R("lantern", "lantern", "수호 등불", 1, T.ShieldAlly),
        };

        // Lv 가 하나 오를 때 — 체력 ×1.2 · 공격 ×1.1(원본 Lv2 · Lv3 은 체력 1.2~1.6배)
        private const float HpPerLevel = 0.2f;
        private const float AtkPerLevel = 0.1f;

        [MenuItem("Tools/Game/몬스터 표 임포트")]
        public static void Import()
        {
            var bodies = new Dictionary<string, Body>();
            foreach (var b in Bodies) bodies[b.Key] = b;

            var keys = new HashSet<string>();
            var table = AssetDatabase.LoadAssetAtPath<TrashTable>(TablePath);
            bool created = table == null;
            if (created) table = ScriptableObject.CreateInstance<TrashTable>();

            var so = new SerializedObject(table);
            var arr = so.FindProperty("_entries");
            arr.ClearArray();
            var tsv = new StringBuilder("key\tbody\trole\tlevel\telement\tname\n");
            int n = 0;
            foreach (var r in Roster)
            {
                if (!keys.Add(r.Key)) { Debug.LogError($"[몬스터 표] 키가 겹친다: {r.Key}"); return; }
                if (!bodies.TryGetValue(r.Body, out var b)) { Debug.LogError($"[몬스터 표] 몸이 없다: {r.Key} → {r.Body}"); return; }

                arr.InsertArrayElementAtIndex(n);
                var e = arr.GetArrayElementAtIndex(n++);
                float hpMul = 1f + HpPerLevel * (r.Level - 1);
                float atkMul = 1f + AtkPerLevel * (r.Level - 1);
                e.FindPropertyRelative("_key").stringValue = r.Key;
                e.FindPropertyRelative("_body").stringValue = r.Body;
                // 원소판처럼 제 그림이 있으면 그 그림 — 폴더가 아직 없으면 몸 그림으로 선다(흰 네모 대신)
                bool ownArt = r.Key != r.Body && AssetDatabase.IsValidFolder($"Assets/BaseResource/Unit/{r.Key}");
                e.FindPropertyRelative("_sprite").stringValue = ownArt ? r.Key : r.Body;
                e.FindPropertyRelative("_nameKr").stringValue = r.Name;
                e.FindPropertyRelative("_level").intValue = r.Level;
                e.FindPropertyRelative("_role").enumValueIndex = (int)b.Role;
                e.FindPropertyRelative("_element").enumValueIndex = (int)r.Element;
                e.FindPropertyRelative("_traits").longValue = (long)r.Traits;
                e.FindPropertyRelative("_attackKind").enumValueIndex = (int)b.Kind;
                e.FindPropertyRelative("_hp").intValue = Mathf.RoundToInt(b.Hp * hpMul);
                e.FindPropertyRelative("_atk").intValue = Mathf.RoundToInt(b.Atk * atkMul);
                e.FindPropertyRelative("_moveMps").floatValue = b.Move;
                e.FindPropertyRelative("_engageMps").floatValue = b.Engage;
                e.FindPropertyRelative("_rangeMeters").floatValue = b.Range;
                e.FindPropertyRelative("_interval").floatValue = b.Interval;
                e.FindPropertyRelative("_telegraph").floatValue = b.Tell;
                e.FindPropertyRelative("_shotCount").intValue = b.Shots;
                e.FindPropertyRelative("_spreadDegrees").floatValue = b.Spread;
                tsv.Append(r.Key).Append('\t').Append(r.Body).Append('\t').Append(b.Role.ToString().ToLowerInvariant())
                   .Append('\t').Append(r.Level).Append('\t').Append(r.Element.ToString().ToLowerInvariant())
                   .Append('\t').Append(r.Name).Append('\n');
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            if (created) AssetDatabase.CreateAsset(table, TablePath);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            RegisterAddressable();

            var root = Path.GetDirectoryName(Application.dataPath);
            File.WriteAllText(Path.Combine(root, RosterTsv), tsv.ToString(), new UTF8Encoding(false));
            Debug.Log($"[몬스터 표] {n}종 · 몸 {bodies.Count}개 → {TablePath} · {RosterTsv}");
        }

        private static void RegisterAddressable()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogWarning("[몬스터 표] Addressable 설정 없음"); return; }
            var group = settings.FindGroup(Group);
            if (group == null) { Debug.LogWarning($"[몬스터 표] 그룹 {Group} 없음"); return; }
            if (!settings.GetLabels().Contains(Label)) settings.AddLabel(Label);
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(TablePath), group);
            entry.address = Address;
            entry.SetLabel(Label, true);
            settings.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
        }
    }
}
