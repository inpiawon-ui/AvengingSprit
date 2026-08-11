using System.IO;
using System.Linq;
using System.Reflection;
using Game.Character;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// `HostTable` · `UltimateTable` 에셋을 확정 데이터로 생성하고 Addressable 에 등록한다.
    ///
    /// 데이터 출처는 `Projects/AVSR/AVSR_Decisions.md` §4 (호스트 12종 · 해금 조건).
    /// 얼티밋 정본은 제안서 로스터 페이지(slide_07).
    /// </summary>
    public static class CreateAssetTables
    {
        private const string Dir = "Assets/BundleResource/TableData";
        private const string Group = "tabledata";
        private const string Label = "label_tabledata";

        // hostKey, 영문명, 한글명, 역할, HP, ATK, SPD, DASH,
        // 공격종류, 탄수, 확산각, 사거리배율, 간격배율, 피해배율, 흡혈%, 둔화%, 탄반사,
        // ultimateKey, 해금유형, 챕터, 스테이지
        //
        // 배율은 GameConfig 의 기본 사거리·간격·피해에 곱한다. 역할 문구와 일치시킨다.
        // 예) 히트맨 "정밀 저격" = 사거리 1.7배 · 간격 1.7배(느림) · 피해 2.4배
        // 키·이름·등장 챕터는 정본을 따른다 — `Projects/AVSR/AVSR_Roster.md` (정본 JSON 에서 자동 생성).
        // 목업에만 있던 5종(아마조네스·히트맨·설녀·적닌자·녹마법사)은 폐기했다(확정 #10).
        //
        // 수치는 아직 우리가 튜닝한 값이다. 정본의 ATTACK_PROFILE(사거리·간격·모드)로
        // 교체하는 것은 임포터가 선 뒤에 한다 — 지금 바꾸면 플레이 검증 기준이 사라진다.
        //
        // 해금은 정본 MinChapter 를 따라 **챕터 클리어**로 통일했다.
        // 옛 StageReach 값(5·12·20 등)은 StagesPerChapter=3 에서 영원히 닿지 않았다.
        //
        // 마지막 세 칸은 빙의 방식 · 체력 임계 · 정본 EnemyID 다(정본 POSSESSION_MATRIX).
        // EnemyID 는 방 데이터의 스폰이 배우를 가리키는 열쇠다 — AVSR_Roster.md 대조표.
        // 정본 조건은 상태이상(화상3·빙결·장갑파괴)인데 그 시스템이 아직 없다.
        // 지금은 체력 임계로 대신 판정하고, 조건이 셀수록 임계를 낮게 잡았다.
        //   갱스터·폭력배·구루·야구선수 = 즉시 (정본 Immediate)
        //   ArmorBreak AND Burn3 처럼 둘 다 요구하는 것은 더 낮은 임계로 옮겼다
        private static readonly object[][] Hosts =
        {
            new object[]{ "gangster",         "GANGSTER",         "갱스터",        "확산 사수",     70, 76, 64, 58, AttackKind.Spread, 5, 34f, 0.7f, 1.05f, 0.5f, 0, 0, false, "tommy_barrage",   HostUnlockType.Owned,            0, 0, PossessKind.Immediate, 100 , "E001" },
            new object[]{ "thug",             "THUG",             "폭력배",        "중화기 사수",   80, 85, 55, 45, AttackKind.Rapid,  1, 0f,  1.0f, 0.4f,  0.48f, 0, 0, false, "bullet_hell",     HostUnlockType.Owned,            0, 0, PossessKind.Immediate, 100 , "E004" },
            new object[]{ "fighter",          "FIGHTER",          "파이터",        "돌진 근접",     68, 70, 84, 88, AttackKind.Melee,  1, 0f,  0.45f, 0.55f, 0.8f, 0, 0, false, "rush_combo",      HostUnlockType.Owned,            0, 0, PossessKind.Condition, 40 , "E002" },
            new object[]{ "salamander",       "SALAMANDER",       "샐러맨더",      "화염 돌파",     95, 80, 42, 38, AttackKind.Spread, 3, 12f, 0.5f, 0.85f, 0.52f, 0, 0, false, "dragon_breath",   HostUnlockType.Owned,            0, 0, PossessKind.Condition, 50 , "E003" },
            new object[]{ "white_wizard",     "WHITE WIZARD",     "화이트 위저드", "둔화 제어",     58, 88, 62, 55, AttackKind.Pierce, 1, 0f,  1.3f, 1.35f, 1.75f, 0, 0, false, "elemental_nova",  HostUnlockType.Owned,            0, 0, PossessKind.Condition, 50 , "E010" },
            new object[]{ "ninja",            "NINJA",            "닌자",          "순간 폭발",     66, 74, 88, 92, AttackKind.Spread, 3, 16f, 0.9f, 0.8f,  0.6f, 0, 0, false, "shadow_burst",    HostUnlockType.Owned,            0, 0, PossessKind.Condition, 50 , "E011" },
            new object[]{ "assault_gangster", "ASSAULT GANGSTER", "어설트 갱스터", "관통 레이저",   70, 78, 65, 55, AttackKind.Pierce, 1, 0f,  1.45f, 0.7f, 0.9f, 0, 0, false, "laser_storm",     HostUnlockType.ChapterBossClear, 1, 0, PossessKind.Condition, 50 , "E007" },
            new object[]{ "robot",            "ROBOT",            "로봇",          "배치 테크",     82, 72, 60, 50, AttackKind.Pierce, 1, 0f,  1.1f, 1.0f,  1.15f, 0, 0, false, "system_override", HostUnlockType.ChapterBossClear, 1, 0, PossessKind.Condition, 45 , "E008" },
            new object[]{ "guru",             "GURU",             "구루",          "부양 서포트",   76, 52, 70, 74, AttackKind.Pulse,  1, 0f,  0.55f, 1.25f, 0.95f, 0, 0, false, "astral_form",     HostUnlockType.ChapterBossClear, 1, 0, PossessKind.Immediate, 100 , "E009" },
            new object[]{ "vampire",          "VAMPIRE",          "흡혈귀",        "흡혈 지속",     74, 82, 76, 66, AttackKind.Melee,  1, 0f,  0.5f, 0.7f,  1.05f, 35, 0, false, "blood_tornado",   HostUnlockType.ChapterBossClear, 1, 0, PossessKind.Condition, 35 , "E014" },
            new object[]{ "baseball",         "BASEBALL PLAYER",  "야구선수",      "탄환 반사",     78, 66, 72, 70, AttackKind.Melee,  1, 0f,  0.6f, 0.75f, 1.35f, 0, 0, true, "grand_slam",      HostUnlockType.ChapterBossClear, 2, 0, PossessKind.Immediate, 100 , "E015" },
        };



        // bossKey, 챕터, 영문명, 한글명, 스프라이트, HP배율, ATK배율, 이동배율, 페이즈쿨다운배율
        // 패턴: (종류, 시작페이즈, 쿨다운, 탄수, 확산각, 피해배율)
        //
        // 지금은 3체 모두 **5발 135° 부채꼴을 3초마다** 쏘는 것 하나뿐이다.
        // BossBrain 은 여러 행동을 쿨다운으로 돌리고 페이즈(60%/30%)마다 레퍼토리를
        // 늘리는 구조를 그대로 갖고 있다 — 여기 배열에 줄을 더하면 바로 살아난다.
        // bossKey, 챕터, 영문명, 한글명, 스프라이트, HP배율, ATK배율, 이동배율, 페이즈쿨다운배율
        // 패턴: (종류, 시작페이즈, 쿨다운, 탄수, 확산각, 피해배율)
        //
        // 체력·공격력·이동속도·페이즈 문턱·예고 시간은 이제 **정본에서** 읽는다
        // (RoomTable.BossHp/BossAtk/BossPhaseGates). 여기 배율은 정본 방이 없을 때의
        // 대비책으로만 남는다.
        //
        // 여기서 정하는 것은 **레퍼토리**다. 정본이 못박은 것은 "페이즈마다 행동이
        // 바뀐다 — 수치만 오르는 것은 페이즈가 아니다" 이므로, 페이즈가 오를 때마다
        // 새 행동이 열리게 짰다.
        //   P1  읽을 수 있는 부채꼴 하나. 패턴을 배우는 구간
        //   P2  부채꼴이 넓어지고 **돌진**이 열린다. 자리를 지킬 수 없게 만든다
        //   P3  사방 탄막이 열린다. 붙어서도 떨어져서도 안전한 곳이 없다
        // 이름 붙은 정본 패턴(BurrowTrack·ConveyorReverse 등)은 그림과 함께 와야 해서
        // 아직 이 원시 동작으로 흉내낸다.
        private static readonly object[][] Bosses =
        {
            new object[]{ "mad_doctor", 1, "MAD DOCTOR", "매드 닥터", "unit_boss", 1.00f, 1.00f, 1.00f, 0.80f,
                new object[][] {
                    new object[]{ BossPattern.Volley,  1, 3.0f, 5, 135f, 1.0f },
                    new object[]{ BossPattern.Charge,  2, 6.0f, 1,   0f, 1.2f },
                    new object[]{ BossPattern.Volley,  2, 4.2f, 7, 180f, 0.9f },
                    new object[]{ BossPattern.Ring,    3, 5.0f, 12, 360f, 0.8f },
                } },
            new object[]{ "iron_claw",  2, "IRON CLAW",  "아이언 클로", "unit_boss", 1.25f, 1.15f, 1.30f, 0.75f,
                new object[][] {
                    new object[]{ BossPattern.Volley,     1, 3.0f, 5, 135f, 1.0f },
                    new object[]{ BossPattern.Charge,     2, 5.0f, 1,   0f, 1.3f },
                    new object[]{ BossPattern.AimedBurst, 2, 3.6f, 3,  10f, 0.7f },
                    new object[]{ BossPattern.Ring,       3, 4.4f, 14, 360f, 0.85f },
                } },
            new object[]{ "overlord",   3, "OVERLORD",   "오버로드",   "unit_boss", 1.55f, 1.30f, 0.95f, 0.72f,
                new object[][] {
                    new object[]{ BossPattern.Volley,     1, 2.8f, 7, 150f, 1.0f },
                    new object[]{ BossPattern.Summon,     2, 9.0f, 2,   0f, 1.0f },
                    new object[]{ BossPattern.AimedBurst, 2, 3.2f, 4,  12f, 0.75f },
                    new object[]{ BossPattern.Ring,       3, 3.8f, 16, 360f, 0.9f },
                } },
        };

        // buffKey, 한글명, 설명, 종류, 값, 중복가능, 강조색
        private static readonly object[][] Buffs =
        {
            new object[]{ "atk_up",     "공격력 강화", "피해량 +25%",            BuffKind.Attack,         25, true,  "#E8604A" },
            new object[]{ "aspd_up",    "연사 강화",   "발사 간격 -18%",         BuffKind.AttackSpeed,    18, true,  "#F0B428" },
            new object[]{ "range_up",   "사거리 강화", "사거리 +25%",            BuffKind.Range,          25, true,  "#4AA8E8" },
            new object[]{ "move_up",    "질주",        "이동 속도 +18%",         BuffKind.MoveSpeed,      18, true,  "#5CC850" },
            new object[]{ "ghost_hp",   "영혼 강화",   "고스트 최대 체력 +40",   BuffKind.GhostHp,        40, true,  "#5AC8F0" },
            new object[]{ "heal",       "응급 회복",   "호스트 체력 40% 회복",   BuffKind.Heal,           40, true,  "#8CD048" },
            new object[]{ "multishot",  "다중 사격",   "탄 +1 발",               BuffKind.MultiShot,       1, true,  "#C98CF0" },
            new object[]{ "pierce",     "관통탄",      "탄이 적을 관통한다",      BuffKind.Pierce,          1, false, "#A0E0FF" },
            new object[]{ "lifesteal",  "흡혈",        "피해의 15% 회복",        BuffKind.Lifesteal,      15, true,  "#E04A7A" },
            new object[]{ "slow",       "서리",        "명중 시 둔화 25%",       BuffKind.Slow,           25, true,  "#7ED8F0" },
            new object[]{ "ult_charge", "얼티밋 충전", "충전 속도 +30%",         BuffKind.UltimateCharge, 30, true,  "#F07828" },
            new object[]{ "shot_speed", "탄속 강화",   "탄속 +30%",              BuffKind.ShotSpeed,      30, true,  "#F2F4F8" },
        };

        // ultimateKey, 영문명, 한글명, 설명
        private static readonly string[][] Ultimates =
        {
            new[]{ "tommy_barrage",   "TOMMY BARRAGE",   "토미 내리사격",   "광각 확산, 높은 경직" },
            new[]{ "bullet_hell",     "BULLET HELL",     "불릿 헬",         "전화면 제압 사격, 5초 지속" },
            new[]{ "rush_combo",      "RUSH COMBO",      "러시 콤보",       "연속 돌진 타격, 마지막 일격에 경직" },
            new[]{ "dragon_breath",   "DRAGON BREATH",   "드래곤 브레스",   "지속 화염 원뿔, 화상 DoT" },
            new[]{ "elemental_nova",  "ELEMENTAL NOVA",  "엘리멘탈 노바",   "360° AoE, 보스에게 2배 피해" },
            new[]{ "shadow_burst",    "SHADOW BURST",    "섀도우 버스트",   "순간이동 연격 + 무적 프레임" },
            new[]{ "laser_storm",     "LASER STORM",     "레이저 스톰",     "관통 광선을 전방으로 난사, 4초 지속" },
            new[]{ "system_override", "SYSTEM OVERRIDE", "시스템 오버라이드","자동조준 터렛 6초 배치" },
            new[]{ "astral_form",     "ASTRAL FORM",     "아스트랄 폼",     "위상 이탈, 무적 + 재생 8초" },
            new[]{ "blood_tornado",   "BLOOD TORNADO",   "블러드 토네이도", "회오리 공격이 HP 흡수" },
            new[]{ "grand_slam",      "GRAND SLAM",      "그랜드 슬램",     "모든 탄환을 3배 피해로 반사" },
        };

        [MenuItem("Tools/Game/Create Asset Tables")]
        public static void Run()
        {
            Directory.CreateDirectory(Dir);

            var host = ScriptableObject.CreateInstance<HostTable>();
            var entries = Hosts.Select(h =>
            {
                var e = new HostEntry();
                Set(e, "_hostKey", h[0]); Set(e, "_nameEn", h[1]); Set(e, "_nameKr", h[2]);
                Set(e, "_role", h[3]);
                Set(e, "_hp", h[4]); Set(e, "_atk", h[5]); Set(e, "_spd", h[6]); Set(e, "_dash", h[7]);
                Set(e, "_attackKind", h[8]); Set(e, "_shotCount", h[9]); Set(e, "_spreadDegrees", h[10]);
                Set(e, "_rangeMul", h[11]); Set(e, "_intervalMul", h[12]); Set(e, "_damageMul", h[13]);
                Set(e, "_lifestealPercent", h[14]); Set(e, "_slowPercent", h[15]);
                Set(e, "_reflectsShots", h[16]);
                Set(e, "_ultimateKey", h[17]);
                Set(e, "_unlockType", h[18]); Set(e, "_unlockChapter", h[19]); Set(e, "_unlockStage", h[20]);
                Set(e, "_possessKind", h[21]); Set(e, "_possessHpPercent", h[22]);
                Set(e, "_enemyId", h[23]);
                return e;
            }).ToArray();
            Set(host, "_entries", entries);
            SaveAsset(host, $"{Dir}/HostTable.asset");

            var ult = ScriptableObject.CreateInstance<UltimateTable>();
            var uEntries = Ultimates.Select(u =>
            {
                var e = new UltimateEntry();
                Set(e, "_ultimateKey", u[0]); Set(e, "_nameEn", u[1]);
                Set(e, "_nameKr", u[2]); Set(e, "_description", u[3]);
                return e;
            }).ToArray();
            Set(ult, "_entries", uEntries);
            SaveAsset(ult, $"{Dir}/UltimateTable.asset");

            var boss = ScriptableObject.CreateInstance<BossTable>();
            var bossEntries = Bosses.Select(b =>
            {
                var e = new BossEntry();
                Set(e, "_bossKey", b[0]); Set(e, "_chapter", b[1]);
                Set(e, "_nameEn", b[2]); Set(e, "_nameKr", b[3]); Set(e, "_spriteName", b[4]);
                Set(e, "_hpMul", b[5]); Set(e, "_atkMul", b[6]);
                Set(e, "_moveSpeedMul", b[7]); Set(e, "_phaseCooldownMul", b[8]);
                var moves = ((object[][])b[9]).Select(m =>
                {
                    var mv = new BossMove();
                    Set(mv, "_pattern", m[0]); Set(mv, "_fromPhase", m[1]);
                    Set(mv, "_cooldown", m[2]); Set(mv, "_shotCount", m[3]);
                    Set(mv, "_spreadDegrees", m[4]); Set(mv, "_damageMul", m[5]);
                    return mv;
                }).ToArray();
                Set(e, "_moves", moves);
                return e;
            }).ToArray();
            Set(boss, "_entries", bossEntries);
            SaveAsset(boss, $"{Dir}/BossTable.asset");

            var buff = ScriptableObject.CreateInstance<BuffTable>();
            var bEntries = Buffs.Select(b =>
            {
                var e = new BuffEntry();
                Set(e, "_buffKey", b[0]); Set(e, "_nameKr", b[1]); Set(e, "_description", b[2]);
                Set(e, "_kind", b[3]); Set(e, "_value", b[4]);
                Set(e, "_stackable", b[5]); Set(e, "_colorHex", b[6]);
                return e;
            }).ToArray();
            Set(buff, "_entries", bEntries);
            SaveAsset(buff, $"{Dir}/BuffTable.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // 인게임 공통 수치 — 04_scenes.md 규약: 스크립트 하드코딩 금지, 이 SO 한 곳에서 관리
            if (AssetDatabase.LoadAssetAtPath<GameConfig>($"{Dir}/GameConfig.asset") == null)
                SaveAsset(ScriptableObject.CreateInstance<GameConfig>(), $"{Dir}/GameConfig.asset");

            RegisterAddressable($"{Dir}/GameConfig.asset", "TableData/GameConfig");
            RegisterAddressable($"{Dir}/BossTable.asset", "TableData/BossTable");
            RegisterAddressable($"{Dir}/BuffTable.asset", "TableData/BuffTable");
            RegisterAddressable($"{Dir}/HostTable.asset", "TableData/HostTable");
            RegisterAddressable($"{Dir}/UltimateTable.asset", "TableData/UltimateTable");
            Debug.Log($"[CreateAssetTables] 호스트 {entries.Length}종 · 얼티밋 {uEntries.Length}종 생성 완료");
        }

        private static void Set(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) { Debug.LogError($"[CreateAssetTables] 필드 없음: {target.GetType().Name}.{field}"); return; }
            f.SetValue(target, value);
        }

        private static void SaveAsset(Object asset, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void RegisterAddressable(string path, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogWarning("[CreateAssetTables] Addressable 설정 없음"); return; }
            var group = settings.FindGroup(Group) ?? settings.CreateGroup(
                Group, false, false, true, null,
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema),
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));
            if (!settings.GetLabels().Contains(Label)) settings.AddLabel(Label);
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
            entry.address = address;
            entry.SetLabel(Label, true);
            EditorUtility.SetDirty(settings);
        }
    }
}
