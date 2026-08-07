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
        private static readonly object[][] Hosts =
        {
            new object[]{ "amazoness",  "AMAZONESS",   "아마조네스", "고속 근거리",   72, 68, 82, 90, AttackKind.Melee, 1, 0f, 0.45f, 0.55f, 0.8f, 0, 0, false, "blade_storm",     HostUnlockType.Owned,            0,  0 },
            new object[]{ "rambo",      "RAMBO",       "람보",       "중화기 사수",   80, 85, 55, 45, AttackKind.Rapid, 1, 0f, 1.0f, 0.4f, 0.48f, 0, 0, false, "bullet_hell",     HostUnlockType.StageReach,       1,  5 },
            new object[]{ "wizard",     "WIZARD",      "마법사",     "마법 원거리",   58, 88, 62, 55, AttackKind.Pierce, 1, 0f, 1.3f, 1.35f, 1.75f, 0, 0, false, "elemental_nova",  HostUnlockType.StageReach,       1, 12 },
            new object[]{ "ninja",      "NINJA",       "닌자",       "밸런스 어쌔신", 66, 74, 88, 92, AttackKind.Spread, 3, 16f, 0.9f, 0.8f, 0.6f, 0, 0, false, "shadow_burst",    HostUnlockType.StageReach,       1, 20 },
            new object[]{ "mafia",      "MAFIA",       "마피아",     "확산 사수",     70, 76, 64, 58, AttackKind.Spread, 5, 34f, 0.7f, 1.05f, 0.5f, 0, 0, false, "tommy_barrage",   HostUnlockType.ChapterBossClear, 1,  0 },
            new object[]{ "hitman",     "HITMAN",      "히트맨",     "정밀 저격",     60, 92, 68, 62, AttackKind.Snipe, 1, 0f, 1.7f, 1.7f, 2.4f, 0, 0, false, "perfect_kill",    HostUnlockType.StageReach,       2,  8 },
            new object[]{ "yogamaster", "YOGA MASTER", "요가마스터", "부양 서포트",   76, 52, 70, 74, AttackKind.Pulse, 1, 0f, 0.55f, 1.25f, 0.95f, 0, 0, false, "astral_form",     HostUnlockType.StageReach,       2, 15 },
            new object[]{ "dragon",     "DRAGON",      "드래곤",     "헤비 탱크",     95, 80, 42, 38, AttackKind.Spread, 3, 12f, 0.5f, 0.85f, 0.52f, 0, 0, false, "dragon_breath",   HostUnlockType.StageReach,       2, 22 },
            new object[]{ "robot",      "ROBOT",       "로봇",       "테크 밸런스",   82, 72, 60, 50, AttackKind.Pierce, 1, 0f, 1.1f, 1.0f, 1.15f, 0, 0, false, "system_override", HostUnlockType.ChapterBossClear, 2,  0 },
            new object[]{ "snowwoman",  "SNOW WOMAN",  "설녀",       "얼음 컨트롤",   62, 70, 74, 68, AttackKind.Single, 1, 0f, 1.05f, 0.9f, 0.85f, 0, 45, false, "absolute_zero",   HostUnlockType.StageReach,       3,  8 },
            new object[]{ "slugger",    "SLUGGER",     "슬러거",     "탄환 반사",     78, 66, 72, 70, AttackKind.Melee, 1, 0f, 0.6f, 0.75f, 1.35f, 0, 0, true, "grand_slam",      HostUnlockType.StageReach,       3, 15 },
            new object[]{ "vampire",    "VAMPIRE",     "흡혈귀",     "흡혈 전투",     74, 82, 76, 66, AttackKind.Melee, 1, 0f, 0.5f, 0.7f, 1.05f, 35, 0, false, "blood_tornado",   HostUnlockType.ChapterBossClear, 3,  0 },
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
            new[]{ "blade_storm",     "BLADE STORM",     "블레이드 스톰",   "광역 검격, 3초 지속" },
            new[]{ "bullet_hell",     "BULLET HELL",     "불릿 헬",         "전화면 제압 사격, 5초 지속" },
            new[]{ "elemental_nova",  "ELEMENTAL NOVA",  "엘리멘탈 노바",   "360° AoE, 보스에게 2배 피해" },
            new[]{ "shadow_burst",    "SHADOW BURST",    "섀도우 버스트",   "순간이동 연격 + 무적 프레임" },
            new[]{ "tommy_barrage",   "TOMMY BARRAGE",   "토미 내리사격",   "광각 확산, 높은 경직" },
            new[]{ "perfect_kill",    "PERFECT KILL",    "퍼펙트 킬",       "관통탄, 일반 적 원샷" },
            new[]{ "astral_form",     "ASTRAL FORM",     "아스트랄 폼",     "위상 이탈, 무적 + 재생 8초" },
            new[]{ "dragon_breath",   "DRAGON BREATH",   "드래곤 브레스",   "지속 화염 원뿔, 화상 DoT" },
            new[]{ "system_override", "SYSTEM OVERRIDE", "시스템 오버라이드","자동조준 터렛 6초 배치" },
            new[]{ "absolute_zero",   "ABSOLUTE ZERO",   "절대영도",        "화면 내 모든 적 4초 빙결" },
            new[]{ "grand_slam",      "GRAND SLAM",      "그랜드 슬램",     "모든 탄환을 3배 피해로 반사" },
            new[]{ "blood_tornado",   "BLOOD TORNADO",   "블러드 토네이도", "회오리 공격이 HP 흡수" },
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
