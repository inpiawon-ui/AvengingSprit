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

        // hostKey, 영문명, 한글명, 역할, HP, ATK, SPD, DASH, ultimateKey, 해금유형, 챕터, 스테이지
        private static readonly object[][] Hosts =
        {
            new object[]{ "amazoness",  "AMAZONESS",   "아마조네스", "고속 근거리",   72, 68, 82, 90, "blade_storm",     HostUnlockType.Owned,            0,  0 },
            new object[]{ "rambo",      "RAMBO",       "람보",       "중화기 사수",   80, 85, 55, 45, "bullet_hell",     HostUnlockType.StageReach,       1,  5 },
            new object[]{ "wizard",     "WIZARD",      "마법사",     "마법 원거리",   58, 88, 62, 55, "elemental_nova",  HostUnlockType.StageReach,       1, 12 },
            new object[]{ "ninja",      "NINJA",       "닌자",       "밸런스 어쌔신", 66, 74, 88, 92, "shadow_burst",    HostUnlockType.StageReach,       1, 20 },
            new object[]{ "mafia",      "MAFIA",       "마피아",     "확산 사수",     70, 76, 64, 58, "tommy_barrage",   HostUnlockType.ChapterBossClear, 1,  0 },
            new object[]{ "hitman",     "HITMAN",      "히트맨",     "정밀 저격",     60, 92, 68, 62, "perfect_kill",    HostUnlockType.StageReach,       2,  8 },
            new object[]{ "yogamaster", "YOGA MASTER", "요가마스터", "부양 서포트",   76, 52, 70, 74, "astral_form",     HostUnlockType.StageReach,       2, 15 },
            new object[]{ "dragon",     "DRAGON",      "드래곤",     "헤비 탱크",     95, 80, 42, 38, "dragon_breath",   HostUnlockType.StageReach,       2, 22 },
            new object[]{ "robot",      "ROBOT",       "로봇",       "테크 밸런스",   82, 72, 60, 50, "system_override", HostUnlockType.ChapterBossClear, 2,  0 },
            new object[]{ "snowwoman",  "SNOW WOMAN",  "설녀",       "얼음 컨트롤",   62, 70, 74, 68, "absolute_zero",   HostUnlockType.StageReach,       3,  8 },
            new object[]{ "slugger",    "SLUGGER",     "슬러거",     "탄환 반사",     78, 66, 72, 70, "grand_slam",      HostUnlockType.StageReach,       3, 15 },
            new object[]{ "vampire",    "VAMPIRE",     "흡혈귀",     "흡혈 전투",     74, 82, 76, 66, "blood_tornado",   HostUnlockType.ChapterBossClear, 3,  0 },
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
                Set(e, "_ultimateKey", h[8]);
                Set(e, "_unlockType", h[9]); Set(e, "_unlockChapter", h[10]); Set(e, "_unlockStage", h[11]);
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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
