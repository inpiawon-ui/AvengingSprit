using System;
using System.Collections.Generic;
using System.IO;
using Game.Character;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 정본 v2.2(호스트·카드) · v3.3(방·적·보스) 를 테이블 에셋에 굽는다.
    ///
    /// 원본은 xlsx 지만 Unity 가 못 읽으므로 `Projects/AVSR/Canon/v23/` 의 JSON 을 읽는다.
    /// 그 JSON 은 파이썬으로 뽑아 둔 것이고, 원본이 갱신되면 다시 뽑아 덮으면 된다.
    ///
    /// ⚠ 기존 `CanonImporter`(v1.5) 를 지우지 않는다. v23 이 다 자리 잡기 전까지는
    ///    두 정본이 공존한다 — 지금 지우면 되돌아갈 곳이 없어진다.
    /// </summary>
    public static class CanonImporterV23
    {
        private const string Dir = "Projects/AVSR/Canon/v23";
        private const string HostTablePath = "Assets/BundleResource/TableData/HostTable.asset";
        private const string UltTablePath = "Assets/BundleResource/TableData/ActiveSkillTable.asset";

        // ── 인덱스 → 실수치 ───────────────────────────────────────
        //
        // 정본 v2.2 는 호스트 능력치를 **평균 100 기준 인덱스**로 준다.
        // 실수치로 바꿀 기준은 지금 굴러가는 21종의 평균이다 —
        // 그래야 정본으로 갈아타도 체감 난이도가 그 자리에서 유지된다.
        //   HP 142 · ATK 14 · 이동 4.2 m/s  (2026-08-19 실측 평균)
        //
        // 공격 간격(초)과 사거리(m)는 인덱스가 아니라 **실수치로 온다.** 그대로 쓴다.
        // ── 난이도 손잡이 ────────────────────────────────────────
        //
        // 아래 셋은 **정본 값이 아니라 우리가 정한 배율**이다. 정본 수치를 고치지 않고
        // "피할 수 있는가"만 조정하기 위한 자리다. 2026-08-20 사장님 지시로 넣었다.
        //
        // 왜 필요했나: 방 2/12 기준 청소 71초 vs 버티는 시간 8초였다.
        // 방 폭이 8 m 인데 적 사거리가 6.2~8.5 m 라, 가로로는 어디에 서 있든
        // 전원 사거리 안이다. 11명이 동시에 쏘면 피할 자리 자체가 없었다.

        /// <summary>적 체력 배율. 정본 85~166 을 이만큼 깎는다.</summary>
        private const float EnemyHpScale = 0.7f;

        /// <summary>적 탄속 배율. 정본 7.0~9.4 m/s 에서 1/3 을 덜어낸다.</summary>
        private const float EnemyShotSpeedScale = 0.67f;

        /// <summary>
        /// 유도 세기. 정본이 `HOMING` 이라고 적은 종에만 붙는다.
        /// 정본 표현이 "readable homing" — **읽히는** 유도라, 피할 수 있어야 한다.
        /// 0.25 면 방향이 초당 1.5 라디안쯤 돌아, 옆으로 크게 꺾으면 흘릴 수 있다.
        /// </summary>
        private const float HomingStrength = 0.25f;

        /// <summary>예고 시간 = 공격 간격 × 이 값, 아래 두 값 사이로 자른다.</summary>
        private const float TelegraphRatio = 0.50f;
        private const float TelegraphMin = 0.35f;
        private const float TelegraphMax = 0.90f;

        private const float HpBase = 142f;
        private const float AtkBase = 14f;
        private const float MoveBase = 4.2f;

        [MenuItem("Tools/Game/Canon v2.3 임포트 (호스트·액티브 스킬)")]
        public static void ImportHosts()
        {
            var hostJson = ReadJson<HostFile>("HOST_MASTER.json");
            var skillJson = ReadJson<PilsalgiFile>("PILSALGI.json");
            if (hostJson == null || skillJson == null) return;

            // ⚠ 적으로 나올 때의 수치는 HOST_MASTER 가 아니라 ENEMY_RUNTIME 에 있다.
            //   이것을 안 읽으면 사거리·간격이 0 으로 남아 **적이 영영 때리지 못한다.**
            //   실제로 그렇게 나가서 "몬스터가 공격을 안 한다" 가 됐다.
            _enemy = ReadEnemies();

            var table = AssetDatabase.LoadAssetAtPath<HostTable>(HostTablePath);
            if (table == null) { Debug.LogError($"[CanonV23] {HostTablePath} 없음"); return; }

            var so = new SerializedObject(table);
            var entries = so.FindProperty("_entries");

            // 슬러그 → 기존 항목 인덱스. 그림이 붙어 있는 항목은 **덮어쓰되 지우지 않는다.**
            var found = new Dictionary<string, int>();
            for (int i = 0; i < entries.arraySize; i++)
            {
                var key = entries.GetArrayElementAtIndex(i)
                                 .FindPropertyRelative("_hostKey").stringValue;
                if (!string.IsNullOrEmpty(key)) found[key] = i;
            }

            int updated = 0, added = 0;
            foreach (var h in hostJson.hosts)
            {
                int idx;
                if (found.TryGetValue(h.slug, out idx))
                {
                    updated++;
                }
                else
                {
                    idx = entries.arraySize;
                    entries.InsertArrayElementAtIndex(idx);
                    added++;
                }
                Write(entries.GetArrayElementAtIndex(idx), h);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);

            ImportActiveSkills(skillJson);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CanonV23] 호스트 {hostJson.hosts.Length}종 — 갱신 {updated} · 신규 {added}");
        }

        private static void Write(SerializedProperty e, HostRow h)
        {
            e.FindPropertyRelative("_hostKey").stringValue = h.slug;
            e.FindPropertyRelative("_nameEn").stringValue = h.nameEn;
            e.FindPropertyRelative("_enemyId").stringValue = h.enemyId;

            // ── 공격 방식 ────────────────────────────────────────
            // 예전에는 이 값을 옛 씨앗표가 들고 있었다. 그래서 정본이 원거리로 바꾼
            // 흡혈귀(H22 Blood Bat)가 근접으로 남아 **탄이 안 나갔다.**
            // 이제 정본 역할이 단일 출처다.
            var kind = KindOf(h);
            e.FindPropertyRelative("_attackKind").enumValueIndex = (int)kind;
            e.FindPropertyRelative("_shotCount").intValue = ShotsOf(h, kind);
            e.FindPropertyRelative("_spreadDegrees").floatValue = SpreadOf(kind);

            // 화면에 보이는 0~100 막대. 인덱스를 그대로 쓰되 100 을 넘지 않게 자른다.
            e.FindPropertyRelative("_hp").intValue = Bar(h.hpIndex);
            e.FindPropertyRelative("_atk").intValue = Bar(h.atkIndex);
            e.FindPropertyRelative("_spd").intValue = Bar(h.moveIndex);
            e.FindPropertyRelative("_atkSpeed").intValue = Bar(h.atkSpeedIndex);

            // ── 내가 탔을 때의 수치 (정본 HOST_MASTER: 기준값 × 지수) ──
            // ⚠ 이것을 `_canonHp`/`_canonAtk` 에 쓰면 안 된다. 그 칸은 아래에서
            //   적 수치로 덮어써지기 때문에, 예전에는 **내 몸이 적 체력으로 돌아다녔다** —
            //   아마존이 159 가 아니라 85 로 나가서 방을 도저히 버틸 수 없었다.
            e.FindPropertyRelative("_canonHostHp").intValue =
                Mathf.Max(1, Mathf.RoundToInt(HpBase * h.hpIndex / 100f));
            e.FindPropertyRelative("_canonHostAtk").intValue =
                Mathf.Max(1, Mathf.RoundToInt(AtkBase * h.atkIndex / 100f));

            // 정본 적 표가 없는 창작 배우를 위한 기본값. 아래에서 적 값이 있으면 덮어쓴다.
            e.FindPropertyRelative("_canonHp").intValue =
                Mathf.Max(1, Mathf.RoundToInt(HpBase * h.hpIndex / 100f));
            e.FindPropertyRelative("_canonAtk").intValue =
                Mathf.Max(1, Mathf.RoundToInt(AtkBase * h.atkIndex / 100f));
            e.FindPropertyRelative("_canonHostMoveSpeed").floatValue = MoveBase * h.moveIndex / 100f;
            e.FindPropertyRelative("_canonHostInterval").floatValue = h.attackInterval;
            e.FindPropertyRelative("_canonHostRange").floatValue = h.rangeMeters;

            // ── 적으로 나올 때의 수치 (정본 ENEMY_RUNTIME) ──────
            // 사거리는 정본 적 표에 없다 — 같은 몸이므로 호스트 사거리를 쓴다.
            // 공격 간격은 `AttackSpeed` 가 배율이라 호스트 간격을 나눈다.
            var en = _enemy != null && _enemy.TryGetValue(h.hostId, out var er) ? er : null;
            float atkSpeed = en != null ? Mathf.Max(0.2f, en.AttackSpeed) : 1f;

            e.FindPropertyRelative("_canonRange").floatValue = h.rangeMeters;
            e.FindPropertyRelative("_canonInterval").floatValue = h.attackInterval / atkSpeed;
            e.FindPropertyRelative("_canonMoveSpeed").floatValue = MoveBase * h.moveIndex / 100f;
            e.FindPropertyRelative("_canonEngageSpeed").floatValue = MoveBase * h.moveIndex / 100f * 0.85f;
            // 적 탄은 느리게. 호스트(내가 쏠 때)는 정본 속도 그대로 둔다 —
            // 내 탄까지 느려지면 맞히는 맛이 같이 죽는다.
            e.FindPropertyRelative("_canonShotSpeed").floatValue =
                (en != null && en.ProjectileSpeed > 0f ? en.ProjectileSpeed : 8f) * EnemyShotSpeedScale;
            e.FindPropertyRelative("_canonHostShotSpeed").floatValue =
                en != null && en.ProjectileSpeed > 0f ? en.ProjectileSpeed : 8f;
            e.FindPropertyRelative("_canonShotCount").intValue = ShotsOf(h, kind);
            e.FindPropertyRelative("_canonHoming").floatValue =
                en != null && en.PrimaryTrait == "HOMING" ? HomingStrength : 0f;

            // 적일 때는 정본 절대값을 그대로 쓴다. 호스트일 때(위)와 **다른 표다** —
            // 같은 아마존이 내 몸이면 159, 적이면 85 다.
            if (en != null)
            {
                e.FindPropertyRelative("_canonHp").intValue =
                    Mathf.Max(1, Mathf.RoundToInt(en.HP * EnemyHpScale));
                // 공격력은 건드리지 않는다. 한 방이 아픈 것은 유지하고
                // **맞는 횟수**를 줄이는 쪽(예고·동시 공격 상한)으로 푼다.
                e.FindPropertyRelative("_canonAtk").intValue = Mathf.Max(1, en.Damage);
            }

            // 예고 시간. 느린 공격일수록 길게 준다 — 예고는 "피할 시간" 이라
            // 아프고 느린 것일수록 길어야 공평하다.
            e.FindPropertyRelative("_canonTelegraph").floatValue =
                Mathf.Clamp(h.attackInterval * TelegraphRatio, TelegraphMin, TelegraphMax);
            // 동시에 자세를 잡을 수 있는 마릿수. 이 값은 이제 **방 전체 상한**으로
            // 쓰인다(예전에는 같은 종끼리만 셌다) — 종이 섞이면 제한이 없는 것이나
            // 같아서, 11기 방에서 전원이 한꺼번에 쏘고 있었다.
            e.FindPropertyRelative("_canonMaxConcurrent").intValue = 2;

            // 액티브 스킬는 이제 호스트마다 하나씩이다 (PSG_H01 → psg_h01).
            e.FindPropertyRelative("_activeSkillKey").stringValue = h.ultimateId.ToLowerInvariant();

            // 그림이 아직 없는 몸은 형제 몸의 그림을 빌린다. 안 빌리면 흰 사각형이 서 있다.
            e.FindPropertyRelative("_spriteKey").stringValue =
                HasAtlas(h.slug) ? string.Empty : Stand(h.slug);

            e.FindPropertyRelative("_nameKr").stringValue = NameKr(h.slug);
            e.FindPropertyRelative("_actorOnly").boolValue = false;
        }

        /// <summary>
        /// 슬러그 → 한글 이름.
        ///
        /// 정본 HOST_MASTER 는 영문 이름만 준다. 예전에는 표에 이미 들어 있던 값을
        /// 그대로 두었는데, 그러면 **새로 늘어난 몸은 앞사람 이름을 물려받는다** —
        /// 미사일 코만도와 사신이 둘 다 "아크 워든"으로 나오던 원인이다.
        /// 이름도 임포터가 정하게 해서, 다시 구우면 언제나 같은 결과가 나오게 한다.
        /// </summary>
        private static string NameKr(string slug) => slug switch
        {
            "gangster"         => "갱스터",
            "thug"             => "폭력배",
            "amazon"           => "아마존",
            "amazon_elite"     => "아마존 정예",
            "hopper"           => "호퍼",
            "hopper_smg"       => "호퍼(기관단총)",
            "commando_mg"      => "코만도(기관총)",
            "commando_laser"   => "코만도(레이저)",
            "commando_grenade" => "코만도(수류탄)",
            "commando_missile" => "코만도(미사일)",
            "salamander"       => "샐러맨더",
            "dragoon"          => "드라군",
            "dragon_blue"      => "청룡",
            "guru"             => "구루",
            "white_wizard"     => "화이트 위저드",
            "medium"           => "영매",
            "ninja"            => "닌자",
            "ninja_chain"      => "닌자(사슬)",
            "robot"            => "로봇",
            "baseball"         => "슬러거",
            "snowwoman"        => "설녀",
            "vampire"          => "흡혈귀",
            "death"            => "사신",
            _                  => slug,
        };

        /// <summary>그림이 올 때까지 빌려 쓸 몸. 실루엣이 가장 가까운 쪽으로 고른다.</summary>
        private static string Stand(string slug) => slug switch
        {
            "commando_missile" => "commando_grenade",   // 같은 코만도
            "death"            => "medium",             // 로브에 지팡이 — 사신과 실루엣이 가깝다
            _                  => string.Empty,
        };

        private static bool HasAtlas(string slug)
            => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                   $"Assets/BundleResource/Atlas/unit_{slug}.spriteatlasv2") != null;

        /// <summary>
        /// 정본 역할 → 공격 방식.
        ///
        /// `roleMain` 이 근접인지 투사체인지를 먼저 보고, 보조 역할로 갈래를 나눈다.
        /// 정본이 원거리로 바꾼 몸(H20 슬러거·H22 흡혈귀)이 여기서 제대로 갈린다.
        /// </summary>
        private static AttackKind KindOf(HostRow h)
        {
            string main = h.roleMain ?? string.Empty;
            string sub = h.roleSub ?? string.Empty;

            if (main == "Melee")
                // 광역 보조를 든 근접은 주변을 함께 친다
                return sub == "Area" || sub == "Control" ? AttackKind.Pulse : AttackKind.Melee;

            if (sub == "Area" || main == "Area" || main == "Explosive") return AttackKind.Spread;
            if (sub == "Piercing" || main == "Piercing") return AttackKind.Pierce;
            if (sub == "Rapid Fire" || main == "Rapid Fire") return AttackKind.Rapid;
            return AttackKind.Single;
        }

        /// <summary>
        /// 한 번에 몇 발 나가는가.
        ///
        /// ⚠ 정본에는 **탄 수 필드가 없다.** 여기 숫자는 전부 내가 정한 것이다.
        ///   그래서 정본이 글로 적어 둔 것을 기준으로 삼는다:
        ///
        ///     H03 수류탄  "Arc Bomb / Lobs at predicted position"
        ///        → 예측 지점에 **한 발을 던진다.** 예전에 34° 로 3발을 뿌리게 해 두었는데,
        ///          방에 수류탄병이 3기면 한 번에 9발이 날아와 피할 곳이 없었다.
        ///          정본이 말하는 것은 뿌리기가 아니라 **한 발의 예측 사격**이다.
        ///
        ///     RAPID_FIRE  "Short burst forces movement between volleys"
        ///        → 연사는 정본이 직접 말하므로 2발을 유지한다. 좁게(8°) 나간다.
        /// </summary>
        private static int ShotsOf(HostRow h, AttackKind kind) => kind switch
        {
            AttackKind.Rapid => 2,
            _ => 1,
        };

        private static float SpreadOf(AttackKind kind) => kind switch
        {
            AttackKind.Rapid => 8f,
            _ => 0f,
        };

        // ── 정본 적 표 ──────────────────────────────────────────
        private static Dictionary<string, EnemyRow> _enemy;

        [Serializable] private sealed class EnemyFile { public EnemyRow[] data; }

        [Serializable]
        private sealed class EnemyRow
        {
            public string EnemyID, HostID, Role, PrimaryTrait, AttackName;
            public int FirstChapter, HP, Damage;
            public float AttackSpeed, ProjectileSpeed;
        }

        private static Dictionary<string, EnemyRow> ReadEnemies()
        {
            var file = ReadJson<EnemyFile>("ENEMY_RUNTIME.json");
            var map = new Dictionary<string, EnemyRow>();
            if (file?.data == null) return map;
            for (int i = 0; i < file.data.Length; i++)
            {
                var r = file.data[i];
                if (!string.IsNullOrEmpty(r.HostID)) map[r.HostID] = r;
            }
            return map;
        }

        /// <summary>인덱스(평균 100)를 0~100 막대로. 130 짜리도 화면에서는 꽉 찬 막대다.</summary>
        private static int Bar(float index) => Mathf.Clamp(Mathf.RoundToInt(index * 0.72f), 1, 100);

        private static void ImportActiveSkills(PilsalgiFile file)
        {
            var table = AssetDatabase.LoadAssetAtPath<ActiveSkillTable>(UltTablePath);
            if (table == null) { Debug.LogError($"[CanonV23] {UltTablePath} 없음"); return; }

            var so = new SerializedObject(table);
            var entries = so.FindProperty("_entries");
            entries.ClearArray();

            // 액티브 스킬는 호스트마다 하나씩이다. 12종을 21명이 돌려 쓰던 것을 끝낸다.
            for (int i = 0; i < file.ultimates.Length; i++)
            {
                var u = file.ultimates[i];
                entries.InsertArrayElementAtIndex(i);
                var e = entries.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("_activeSkillKey").stringValue = u.ultimateId.ToLowerInvariant();
                e.FindPropertyRelative("_nameEn").stringValue = u.nameEn;
                e.FindPropertyRelative("_nameKr").stringValue = u.nameKr;
                e.FindPropertyRelative("_description").stringValue =
                    $"피해 계수 {u.damageCoef:0.0}";
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            Debug.Log($"[CanonV23] 액티브 스킬 {file.ultimates.Length}종");
        }

        private static T ReadJson<T>(string name) where T : class
        {
            var path = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, Dir, name);
            if (!File.Exists(path)) { Debug.LogError($"[CanonV23] {name} 없음 — {path}"); return null; }
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }

        [Serializable] private sealed class HostFile { public HostRow[] hosts; }

        [Serializable]
        private sealed class HostRow
        {
            public string hostId, slug, nameEn, family, roleMain, roleSub;
            public float hpIndex, atkIndex, atkSpeedIndex, moveIndex, rangeIndex, armorIndex;
            public float attackInterval, rangeMeters;
            public string statTier, ultimateId, ultimateKr, ultimateEn, enemyId;
        }

        [Serializable] private sealed class PilsalgiFile { public UltRow[] ultimates; }

        [Serializable]
        private sealed class UltRow
        {
            public string ultimateId, hostId, nameKr, nameEn;
            public float damageCoef;
        }
    }
}
