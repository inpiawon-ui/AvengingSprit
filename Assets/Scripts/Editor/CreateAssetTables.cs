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
        // ultimateKey, 해금유형, 챕터, 스테이지, 빙의방식, 빙의체력임계, 정본EnemyID, 유지훅
        //
        // **로스터는 원작 22종이다** (2026-08-12 전환). 대조표는
        // `Projects/AVSR/AVSR_Roster_Original.md`, 그림은 `Reference/Original/`.
        //
        // 정본 v1.5 의 적 18종은 원작의 부분집합이었다. 목업에만 있다며 내가 폐기했던
        // 아마존·설녀·적닌자·녹마법사가 전부 원작에 있었다 — 폐기 판단이 좁았다.
        //
        // 정본 방 데이터(RoomTable)의 스폰은 EnemyID 로 배우를 가리킨다. 원작에만 있고
        // 정본 ID 가 없는 종(호퍼·머신건 코만도·청드래곤 등)은 빈 문자열이다 —
        // 방 데이터에 안 나오지만 로비·빙의 대상으로는 쓸 수 있다.
        //
        // 수치는 아직 우리가 튜닝한 값이다. 정본 ATTACK_PROFILE 로 교체하는 것은
        // 임포터가 선 뒤에 한다 — 지금 바꾸면 플레이 검증 기준이 사라진다.
        // 해금은 정본 MinChapter 를 따라 챕터 클리어로 통일했다.
        private static readonly object[][] Hosts =
        {
            new object[]{ "gangster", "GANGSTER", "갱스터", "단발 정밀", 70, 76, 64, 58, AttackKind.Single, 1, 0f, 1.0f, 0.9f, 1.0f, 0, 0, false, "tommy_barrage", HostUnlockType.Owned, 0, 0, PossessKind.Immediate, 100, "E001", "표식 릴레이" },
            new object[]{ "thug", "THUG", "폭력배", "확산 제압", 80, 70, 60, 50, AttackKind.Spread, 5, 34f, 0.7f, 1.05f, 0.5f, 0, 0, false, "bullet_hell", HostUnlockType.Owned, 0, 0, PossessKind.Immediate, 100, "E004", "제압 사격" },
            new object[]{ "amazon", "AMAZON", "아마존", "돌진 근접", 68, 70, 84, 88, AttackKind.Melee, 1, 0f, 0.45f, 0.55f, 0.8f, 0, 0, false, "rush_combo", HostUnlockType.Owned, 0, 0, PossessKind.Condition, 40, "E002", "콤보 미터" },
            new object[]{ "amazon_elite", "AMAZON ELITE", "아마존 정예", "중장 근접", 96, 84, 72, 70, AttackKind.Melee, 1, 0f, 0.5f, 0.7f, 1.2f, 0, 0, false, "rush_combo", HostUnlockType.ChapterBossClear, 1, 0, PossessKind.Condition, 40, "E006", "콤보 미터" },
            new object[]{ "hopper", "HOPPER", "호퍼", "도약 사수", 62, 68, 88, 92, AttackKind.Single, 1, 0f, 0.95f, 0.8f, 0.85f, 0, 0, false, "bullet_hell", HostUnlockType.Owned, 0, 0, PossessKind.Immediate, 100, "", "도약 연사" },
            new object[]{ "hopper_smg", "HOPPER SMG", "호퍼(기관단총)", "도약 연사", 64, 66, 86, 90, AttackKind.Rapid, 1, 0f, 0.9f, 0.4f, 0.45f, 0, 0, false, "bullet_hell", HostUnlockType.Owned, 0, 0, PossessKind.Immediate, 100, "", "도약 연사" },
            new object[]{ "commando_mg", "COMMANDO MG", "코만도(기관총)", "중화기 사수", 82, 78, 56, 46, AttackKind.Rapid, 1, 0f, 1.0f, 0.4f, 0.48f, 0, 0, false, "bullet_hell", HostUnlockType.ChapterBossClear, 1, 0, PossessKind.Condition, 50, "", "제압 사격" },
            new object[]{ "commando_laser", "COMMANDO LASER", "코만도(레이저)", "관통 레이저", 70, 78, 65, 55, AttackKind.Pierce, 1, 0f, 1.45f, 0.7f, 0.9f, 0, 0, false, "laser_storm", HostUnlockType.ChapterBossClear, 1, 0, PossessKind.Condition, 50, "E007", "집속 레이저" },
            new object[]{ "commando_grenade", "COMMANDO GRENADE", "코만도(수류탄)", "곡사 지역", 74, 82, 58, 48, AttackKind.Spread, 3, 22f, 1.2f, 1.35f, 1.3f, 0, 0, false, "bullet_hell", HostUnlockType.Owned, 0, 0, PossessKind.Condition, 45, "E005", "지뢰 네트워크" },
            new object[]{ "salamander", "SALAMANDER", "샐러맨더", "화염 돌파", 95, 80, 42, 38, AttackKind.Spread, 3, 12f, 0.5f, 0.85f, 0.52f, 0, 0, false, "dragon_breath", HostUnlockType.Owned, 0, 0, PossessKind.Condition, 50, "E003", "열기 · 장갑 용해" },
            new object[]{ "dragoon", "DRAGOON", "드라군", "네이팜 마무리", 110, 92, 44, 40, AttackKind.Spread, 3, 14f, 0.55f, 0.95f, 0.62f, 0, 0, false, "dragon_breath", HostUnlockType.ChapterBossClear, 2, 0, PossessKind.Condition, 25, "E016", "효과 계승 피니셔" },
            new object[]{ "dragon_blue", "DRAGON BLUE", "청룡", "냉기 브레스", 92, 78, 46, 42, AttackKind.Spread, 3, 12f, 0.5f, 0.85f, 0.52f, 0, 30, false, "dragon_breath", HostUnlockType.ChapterBossClear, 2, 0, PossessKind.Condition, 50, "", "냉기 축적" },
            new object[]{ "guru", "GURU", "구루", "부양 서포트", 76, 52, 70, 74, AttackKind.Pulse, 1, 0f, 0.55f, 1.25f, 0.95f, 0, 0, false, "astral_form", HostUnlockType.ChapterBossClear, 1, 0, PossessKind.Immediate, 100, "E009", "가드 오라" },
            new object[]{ "white_wizard", "WHITE WIZARD", "화이트 위저드", "둔화 제어", 58, 88, 62, 55, AttackKind.Pierce, 1, 0f, 1.3f, 1.35f, 1.75f, 0, 25, false, "elemental_nova", HostUnlockType.Owned, 0, 0, PossessKind.Condition, 50, "E010", "장판 제어" },
            new object[]{ "medium", "MEDIUM", "영매", "저주 회로", 62, 86, 58, 52, AttackKind.Pierce, 1, 0f, 1.25f, 1.4f, 1.6f, 0, 0, false, "elemental_nova", HostUnlockType.ChapterBossClear, 2, 0, PossessKind.Condition, 45, "E012", "저주 회로" },
            new object[]{ "ninja", "NINJA", "닌자", "순간 폭발", 66, 74, 88, 92, AttackKind.Spread, 3, 16f, 0.9f, 0.8f, 0.6f, 0, 0, false, "shadow_burst", HostUnlockType.Owned, 0, 0, PossessKind.Condition, 50, "E011", "처형 모멘텀" },
            new object[]{ "ninja_chain", "NINJA CHAIN", "닌자(사슬)", "사슬 근접", 70, 78, 82, 86, AttackKind.Melee, 1, 0f, 0.6f, 0.65f, 1.1f, 0, 0, false, "shadow_burst", HostUnlockType.ChapterBossClear, 1, 0, PossessKind.Condition, 50, "", "처형 모멘텀" },
            new object[]{ "robot", "ROBOT", "로봇", "배치 테크", 82, 72, 60, 50, AttackKind.Pierce, 1, 0f, 1.1f, 1.0f, 1.15f, 0, 0, false, "system_override", HostUnlockType.ChapterBossClear, 1, 0, PossessKind.Condition, 45, "E008", "배치 네트워크" },
            new object[]{ "baseball", "SLUGGER", "슬러거", "탄환 반사", 78, 66, 72, 70, AttackKind.Melee, 1, 0f, 0.6f, 0.75f, 1.35f, 0, 0, true, "grand_slam", HostUnlockType.ChapterBossClear, 2, 0, PossessKind.Immediate, 100, "E015", "바운스 경로" },
            new object[]{ "snowwoman", "SNOW WOMAN", "설녀", "빙결 제어", 62, 70, 74, 68, AttackKind.Single, 1, 0f, 1.05f, 0.9f, 0.85f, 0, 45, false, "elemental_nova", HostUnlockType.ChapterBossClear, 2, 0, PossessKind.Condition, 50, "", "빙결 장판" },
            new object[]{ "vampire", "VAMPIRE", "흡혈귀", "흡혈 지속", 74, 82, 76, 66, AttackKind.Melee, 1, 0f, 0.5f, 0.7f, 1.05f, 35, 0, false, "blood_tornado", HostUnlockType.ChapterBossClear, 1, 0, PossessKind.Condition, 35, "E014", "흡혈 경제" },
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
            new object[]{ "robot_snakes", 1, "ROBOT SNAKES", "로봇 스네이크", "unit_robot_snakes", 1.00f, 1.00f, 1.00f, 0.80f,
                new object[][] {
                    new object[]{ BossPattern.Volley,     1, 3.4f, 5, 135f, 1.0f },
                    new object[]{ BossPattern.PopupLaser, 1, 5.0f, 1,   0f, 1.1f },
                    new object[]{ BossPattern.Charge,     2, 6.0f, 1,   0f, 1.2f },
                    new object[]{ BossPattern.Volley,  2, 4.2f, 7, 180f, 0.9f },
                    new object[]{ BossPattern.Ring,    3, 5.0f, 12, 360f, 0.8f },
                } },
            new object[]{ "demolisher",  2, "DEMOLISHER", "데몰리셔",     "unit_demolisher", 1.25f, 1.15f, 1.30f, 0.75f,
                new object[][] {
                    new object[]{ BossPattern.Volley,     1, 3.0f, 5, 135f, 1.0f },
                    new object[]{ BossPattern.ShieldCycle, 1, 7.0f, 1,   0f, 1.4f },
                    new object[]{ BossPattern.Charge,     2, 5.0f, 1,   0f, 1.3f },
                    new object[]{ BossPattern.AimedBurst, 2, 3.6f, 3,  10f, 0.7f },
                    new object[]{ BossPattern.Ring,       3, 4.4f, 14, 360f, 0.85f },
                } },
            new object[]{ "python",      3, "PYTHON",     "파이썬",       "unit_python", 1.55f, 1.30f, 0.95f, 0.72f,
                new object[][] {
                    new object[]{ BossPattern.Volley,     1, 2.8f, 7, 150f, 1.0f },
                    new object[]{ BossPattern.VenomCloud, 1, 6.0f, 2,   0f, 1.0f },
                    new object[]{ BossPattern.Summon,     2, 9.0f, 2,   0f, 1.0f },
                    new object[]{ BossPattern.AimedBurst, 2, 3.2f, 4,  12f, 0.75f },
                    new object[]{ BossPattern.Ring,       3, 3.8f, 16, 360f, 0.9f },
                } },
        };

        // buffKey, 한글명, 설명, 종류, 값, 중복가능, 강조색,
        // [정본] BuffID, 효과 원문, Pool, 등장 챕터, 가중치, 구현 여부
        //
        // 정본 BUFF_DB 24종을 **전부** 담는다. 그중 우리 시스템으로 실제 동작하는 것은
        // 6종뿐이다 — 나머지는 표식·화상·빙결·저주·도탄·설치물 같은 시스템이 있어야 산다.
        // 미구현은 마지막 칸을 false 로 두어 3택1 풀에서 뺀다. 고르면 아무 일도
        // 일어나지 않는 카드가 섞이면 선택 자체가 거짓이 된다.
        //
        // 효과 원문을 그대로 담아 두는 이유: 나중에 그 시스템을 만들 때 무엇을 만들어야
        // 하는지가 여기 적혀 있다. 이름만 남기면 다시 정본을 뒤져야 한다.
        //
        // 우리가 쓰던 것도 남긴다. 정본에서 지금 살릴 수 있는 것이 6종뿐이라 갈아치우면
        // 뽑히는 카드가 6장이 되어 3택1 이 사실상 의미가 없어진다. 시스템이 붙는 대로
        // 정본 것을 켜고 우리 것을 하나씩 뺀다.
        private static readonly object[][] Buffs =
        {
            // ── 우리 것 (정본 대응 없음) ───────────────────────
            new object[]{ "atk_up", "공격력 강화", "피해량 +25%", BuffKind.Attack, 25, true, "#E8604A", "", "", "", 1, 1.0f, true },
            new object[]{ "aspd_up", "연사 강화", "발사 간격 -18%", BuffKind.AttackSpeed, 18, true, "#F0B428", "", "", "", 1, 1.0f, true },
            new object[]{ "range_up", "사거리 강화", "사거리 +25%", BuffKind.Range, 25, true, "#4AA8E8", "", "", "", 1, 1.0f, true },
            new object[]{ "move_up", "질주", "이동 속도 +18%", BuffKind.MoveSpeed, 18, true, "#5CC850", "", "", "", 1, 1.0f, true },
            new object[]{ "heal", "응급 회복", "호스트 체력 40% 회복", BuffKind.Heal, 40, true, "#8CD048", "", "", "", 1, 1.0f, true },
            new object[]{ "multishot", "다중 사격", "탄 +1 발", BuffKind.MultiShot, 1, true, "#C98CF0", "", "", "", 1, 1.0f, true },
            new object[]{ "pierce", "관통탄", "탄이 적을 관통한다", BuffKind.Pierce, 1, false, "#A0E0FF", "", "", "", 1, 1.0f, true },
            new object[]{ "lifesteal", "흡혈", "피해의 15% 회복", BuffKind.Lifesteal, 15, true, "#E04A7A", "", "", "", 1, 1.0f, true },
            new object[]{ "slow", "서리", "명중 시 둔화 25%", BuffKind.Slow, 25, true, "#7ED8F0", "", "", "", 1, 1.0f, true },
            new object[]{ "ult_charge", "얼티밋 충전", "충전 속도 +30%", BuffKind.UltimateCharge, 30, true, "#F07828", "", "", "", 1, 1.0f, true },
            new object[]{ "shot_speed", "탄속 강화", "탄속 +30%", BuffKind.ShotSpeed, 30, true, "#F2F4F8", "", "", "", 1, 1.0f, true },

            // ── 정본 BUFF_DB 24종 ─────────────────────────────
            new object[]{ "vital_shell", "생명의 껍질", "호스트 최대 체력 +12%", BuffKind.HostMaxHp, 12, true, "#E8604A", "BUF_U01", "Max HP +12%", "Universal", 1, 1.0f, true },
            new object[]{ "spirit_reserve", "영혼 예비", "고스트 최대 체력 +8", BuffKind.GhostHp, 8, true, "#5AC8F0", "BUF_U02", "Ghost HP max +8 and heal 4", "Universal", 1, 0.8f, true },
            new object[]{ "quick_reset", "빠른 재정비", "정지 → 발사 지연 -0.03초", BuffKind.StopDelay, 3, true, "#5CC850", "BUF_U03", "Stop-Attack delay -0.03s", "Universal", 1, 0.9f, true },
            new object[]{ "focused_soul", "집중한 영혼", "같은 적 4회 명중마다 피해 +6% (3단계)", BuffKind.FocusedSoul, 6, false, "#E8604A", "BUF_U04", "same target 4 hits: damage +6% stack max3", "Universal", 1, 0.9f, true },
            new object[]{ "wide_echo", "넓은 울림", "광역 반경 +10%", BuffKind.Range, 10, true, "#4AA8E8", "BUF_U05", "AoE radius +10%", "Universal", 1, 0.8f, false },
            new object[]{ "tactical_mercy", "전술적 자비", "전술 빙의 비용 -2", BuffKind.TacticalCost, 2, false, "#A886FF", "BUF_U06", "first tactical cost per room -2", "Universal", 2, 0.7f, true },
            new object[]{ "marked_payload", "표식 탄두", "표식 대상 명중 시 릴레이 탄 +1", BuffKind.Attack, 0, true, "#F0B428", "BUF_T01", "Mark consume: Relay Bullet +1", "Tag", 2, 1.0f, false },
            new object[]{ "burning_circuit", "타오르는 회로", "화상 3단계가 주변으로 번진다", BuffKind.BurnSpread, 1, false, "#F07828", "BUF_T02", "Burn3 spreads 1 stack nearby", "Tag", 2, 0.9f, true },
            new object[]{ "cold_geometry", "차가운 기하", "둔화 장판 가장자리가 지속 피해", BuffKind.SlowFieldEdge, 8, false, "#7ED8F0", "BUF_T03", "Slow field edges deal tick damage", "Tag", 2, 0.9f, true },
            new object[]{ "blood_debt", "피의 부채", "흡혈 초과분이 고스트 체력으로", BuffKind.Lifesteal, 0, true, "#E04A7A", "BUF_T04", "Leech overheal to Ghost HP, room cap2", "Tag", 2, 0.8f, false },
            new object[]{ "bank_shot", "뱅크 샷", "첫 도탄이 60% 피해로 복제", BuffKind.Attack, 0, true, "#C98CF0", "BUF_T05", "first bounce duplicates at 60% damage", "Tag", 2, 0.9f, false },
            new object[]{ "smart_deployment", "스마트 배치", "설치물 재조준 25% 빠르게", BuffKind.Attack, 0, true, "#9AA4B4", "BUF_T06", "deployables retarget 25% faster", "Tag", 2, 0.8f, false },
            new object[]{ "fire_firmware", "화염 펌웨어", "로봇 설치물이 화염·과열을 물려받는다", BuffKind.Attack, 0, false, "#F07828", "BUF_S01", "Robot deployables inherit Fire and Overheat", "Synergy", 2, 1.0f, false },
            new object[]{ "arcane_execution", "비전 처형", "빙결·표식 대상에 순간이동 폭발 연쇄", BuffKind.Attack, 0, false, "#A886FF", "BUF_S02", "Frozen/Marked target blink detonation chains", "Synergy", 2, 1.0f, false },
            new object[]{ "crimson_combo", "핏빛 연격", "흡혈 표식이 콤보 마무리마다 회복", BuffKind.Lifesteal, 0, false, "#E04A7A", "BUF_S03", "Leech marks heal on each combo finisher", "Synergy", 2, 0.9f, false },
            new object[]{ "mine_alchemy", "지뢰 연금술", "지뢰가 빙결 룬으로 바뀐다", BuffKind.FreezeRune, 1, false, "#7ED8F0", "BUF_S04", "Mine seed becomes Freeze Rune", "Synergy", 2, 0.9f, true },
            new object[]{ "curse_inferno", "저주 화염", "저주 회로가 네이팜 기둥으로", BuffKind.Attack, 0, false, "#F07828", "BUF_S05", "Curse circuit becomes napalm pillars", "Synergy", 3, 0.9f, false },
            new object[]{ "guarded_rush", "수호 돌진", "가드 오라를 태워 장갑 돌진", BuffKind.DamageReduction, 0, false, "#5CC850", "BUF_S06", "consume Guard aura for armored dash", "Synergy", 2, 0.8f, false },
            new object[]{ "last_magazine", "마지막 탄창", "마지막 탄·마무리 +25%", BuffKind.Attack, 25, true, "#E8604A", "BUF_A01", "last shot/finisher +25%", "AttackStyle", 1, 0.8f, false },
            new object[]{ "persistent_field", "지속하는 장판", "장판이 2초 더 남는다", BuffKind.FieldDuration, 20, false, "#4AA8E8", "BUF_A02", "fields +2s, max unchanged", "AttackStyle", 2, 0.8f, true },
            new object[]{ "return_path", "귀환 경로", "되돌아오는 탄이 50% 피해", BuffKind.Attack, 0, true, "#A0E0FF", "BUF_A03", "returning projectile deals 50%", "AttackStyle", 2, 0.7f, false },
            new object[]{ "heavy_frame", "중장 프레임", "받는 피해 -10%", BuffKind.DamageReduction, 10, true, "#9AA4B4", "BUF_A04", "damage taken -10%, move -4%", "AttackStyle", 2, 0.7f, true },
            new object[]{ "quick_hands", "빠른 손", "공격 간격 -7%", BuffKind.AttackSpeed, 7, true, "#F0B428", "BUF_A05", "attack interval -7%", "AttackStyle", 1, 0.9f, true },
            new object[]{ "safe_exit", "안전한 이탈", "전술 빙의 직후 0.6초 무적", BuffKind.SwitchShield, 60, false, "#A886FF", "BUF_A06", "0.6s shield after tactical exit", "AttackStyle", 2, 0.7f, true },
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
                Set(e, "_enemyId", h[23]); Set(e, "_maintainHook", h[24]);
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
                Set(e, "_canonId", b[7]); Set(e, "_canonEffect", b[8]);
                Set(e, "_pool", b[9]); Set(e, "_fromChapter", b[10]);
                Set(e, "_weight", b[11]); Set(e, "_implemented", b[12]);
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
