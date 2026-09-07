using System.Collections.Generic;
using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 방을 이루는 것 — **배경 · 무대 소품 · 챕터별 잡몹 배치.**
    ///
    /// ── 왜 파일을 갈랐나 ────────────────────────────────────────
    /// `BattleDirector.cs` 가 8,700줄이라 보스 작업과 배경·스테이지 작업이
    /// 같은 파일 안에서 만난다. 둘을 **나란히 진행할 때 서로 밟는다** —
    /// 한쪽이 반쯤 쓴 코드가 다른 쪽 검증을 컴파일 오류로 세운다.
    /// 방·배경 쪽을 여기로 떼어 두면 파일 단위로 갈린다(기획 2026-09-07).
    ///
    /// **이 파일은 배경·스테이지 담당이 소유한다.** 보스 쪽은 안 건드린다.
    ///
    /// ⚠ `BossEnvOf` 만은 경계다 — 보스와 무대를 잇는 유일한 자리다.
    ///   보스 순서가 바뀌면 여기도 같이 바뀐다.
    /// </summary>
    public sealed partial class BattleDirector
    {

        // ══ 무대 소품 그림 고르기 ══════════════════════════════════

        /// <summary>
        /// 지금 무대의 장애물 그림. 없으면 **연구소 세트로 떨어진다.**
        ///
        /// 배경이 무대마다 바뀌는데 장애물이 전부 연구소 것이면 방이 따로 논다.
        /// 그렇다고 다섯 무대 × 일곱 종류를 한꺼번에 받을 수도 없으므로,
        /// **온 것부터 쓰고 안 온 것은 연구소 것으로 버틴다.**
        ///
        ///   `obj_junkyard_block_1`  ← 무대 것이 있으면 이걸 쓰고
        ///   `obj_block_1`           ← 없으면 이것 (연구소 · 지금 다 있는 세트)
        /// </summary>
        private Sprite EnvSprite(string baseName)
        {
            if (!baseName.StartsWith("obj_")) return GetSprite(baseName);
            string tail = baseName.Substring(4);

            // ① 이 무대 것
            if (!string.IsNullOrEmpty(_floorEnv))
            {
                var s = GetSprite("obj_" + _floorEnv + "_" + tail);
                if (s != null) return s;
            }
            // ② 연구소(기본) 것
            var baseSprite = GetSprite(baseName);
            if (baseSprite != null) return baseSprite;

            // ③ ⚠ 도중에 생긴 도형(`bulk`·`rail`)은 **기본형이 아예 없다.**
            //    그대로 두면 그림 없이 색 사각형만 뜬다. 아무 무대 것이라도 빌려 온다 —
            //    색이 안 맞는 것이 빈 상자보다 낫다. 그 무대 그림이 오면 자동으로 ①이 이긴다.
            for (int i = 0; i < FallbackEnvs.Length; i++)
            {
                var s = GetSprite("obj_" + FallbackEnvs[i] + "_" + tail);
                if (s != null) return s;
            }
            return null;
        }

        // ══ 챕터별 잡몹 배치 ═════════════════════════════════════

        /// <summary>
        /// 챕터마다 잡몹 **짝이 달라진다.**
        ///
        /// 예전에는 세 챕터가 전부 해골·박쥐 둘만 썼다 — 20시간을 가도 같은 둘이
        /// 같은 방식으로 달려오니 챕터가 깊어진 느낌이 안 났다.
        ///
        ///   CH1  해골 · 박쥐 · 폐품 사수      벽 · 추격 · 사격 — 셋을 다 배우는 자리
        ///   CH2  집행자 · 박쥐 · 순찰기        벽이 무거워지고 사격도 세진다
        ///   CH3  집행자 · 해골 · 코일 보행기   둘 다 벽인데 멀리서 쏘는 놈까지 붙는다
        ///
        /// 순서대로 돌려 쓰므로 **목록에 몇 번 넣었는지가 곧 등장 비율**이다.
        /// CH1 은 셋이 고르게 나와 원거리가 셋에 하나꼴이다 —
        /// 방 하나에 4~7기가 서므로 쏘는 놈이 한둘 섞인다.
        ///
        /// CH2·CH3 원거리는 아직 그림이 없다(43차 발주 진행 중).
        /// 오면 그 챕터 목록에 한 자리 넣으면 그만이다.
        private static HostEntry[] TrashPool(int chapter) => chapter switch
        {
            1 => new[] { Skeleton, Bat, Scrapgunner },
            2 => new[] { Enforcer, Bat, Roadwarden },
            _ => new[] { Enforcer, Skeleton, Coilwalker, Cross },
        };

        private static HostEntry TrashAt(int i, int chapter)
        {
            var pool = TrashPool(chapter);
            return pool[i % pool.Length];
        }

        /// <summary>
        /// 방 데이터가 **이 자리에 무엇을 세울지 직접 적어 둔** 경우.
        ///
        /// 손으로 짠 레이아웃(`RoomLayoutTable`)은 자리마다 역할이 있다 —
        /// RANGED 자리는 쏘는 놈이어야 그 자리가 의미를 갖는다. 회전 목록으로 채우면
        /// 원거리 자리에 근접 해골이 서서 방 설계가 통째로 무너진다.
        ///
        /// ⚠ **그 챕터에 나오는 잡몹만** 받는다. 그림을 미리 올리는 목록이
        ///   `TrashKeysFor(chapter)` 라, 목록에 없는 놈을 세우면 그림 없이 선다.
        ///   CH2·CH3 레이아웃 배정이 끝나면 이 제한이 저절로 풀린다.
        /// </summary>
        private static HostEntry TrashByKey(string key, int chapter)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var allow = TrashKeysFor(chapter);
            for (int i = 0; i < allow.Length; i++)
            {
                if (allow[i] != key) continue;
                return key == TrashSkeletonKey ? Skeleton
                     : key == TrashBatKey      ? Bat
                     : key == TrashGunnerKey   ? Scrapgunner
                     : key == TrashEnforcerKey ? Enforcer
                     : key == TrashWardenKey   ? Roadwarden
                     : key == TrashCoilKey     ? Coilwalker
                     : key == TrashCrossKey    ? Cross
                     : null;
            }
            return null;
        }

        /// <summary>쏘는 잡몹인가. 자리를 대체할 때 **역할**을 살리려고 본다.</summary>
        private static bool IsRangedTrashKey(string key)
            => key == TrashGunnerKey || key == TrashWardenKey
            || key == TrashCoilKey || key == TrashCrossKey;

        /// <summary>
        /// 이 자리에 세울 잡몹. 방 데이터가 지정한 키를 **그 챕터 것으로 갈아 끼운다.**
        ///
        /// ⚠ `AVSR_RoomSpawnRoles.js` 의 유닛 키는 **CH1 기준**이다. CH2·CH3 방에
        ///   그대로 쓰면 폐품 사수가 서서 그림이 없다. 그렇다고 회전 목록에 맡기면
        ///   **자리의 뜻이 사라진다** — RANGED 자리에 근접 해골이 서면
        ///   손으로 짠 레이아웃이 통째로 무의미해진다.
        ///
        ///   그래서 키가 아니라 **역할**로 대체한다.
        ///     FRONT · FLANK (근접 키) → 그 챕터 근접
        ///     RANGED · BACK (원거리 키) → 그 챕터 원거리
        /// </summary>
        private static HostEntry TrashForSlot(string key, int chapter, int seq)
        {
            var exact = TrashByKey(key, chapter);
            if (exact != null) return exact;
            if (string.IsNullOrEmpty(key)) return null;   // 지정 없음 — 회전 목록에 맡긴다

            bool wantRanged = IsRangedTrashKey(key);
            var pool = TrashPool(chapter);
            // 같은 역할끼리만 돌려 쓴다. 여럿이면 등장 비율이 목록에 든 수만큼이다.
            int n = 0;
            for (int i = 0; i < pool.Length; i++)
                if (IsRangedTrashKey(pool[i].HostKey) == wantRanged) n++;
            if (n == 0) return null;

            int pick = ((seq % n) + n) % n;
            for (int i = 0; i < pool.Length; i++)
            {
                if (IsRangedTrashKey(pool[i].HostKey) != wantRanged) continue;
                if (pick-- == 0) return pool[i];
            }
            return null;
        }

        /// <summary>이 챕터에 나오는 잡몹 그림 목록. 안 나오는 것은 안 올린다.</summary>
        private static string[] TrashKeysFor(int chapter) => chapter switch
        {
            1 => new[] { TrashSkeletonKey, TrashBatKey, TrashGunnerKey },
            2 => new[] { TrashBatKey, TrashEnforcerKey, TrashWardenKey },
            _ => new[] { TrashSkeletonKey, TrashEnforcerKey, TrashCoilKey, TrashCrossKey },
        };

        // ══ 보스가 서는 무대 (주석) ════════════════════════════════

        /// <summary>
        /// 보스가 서 있는 무대. **소품이 여기서 갈린다.**
        ///
        /// 보스 아레나는 배경이 보스마다 따로 있어(`roomfloor_crusher`) 이름에서
        /// 무대를 뽑을 수가 없다. 원작 스테이지 순서가 곧 답이다 —
        /// 크러셔가 선 곳이 원작 1스테이지 쓰레기 집적장이고, 우리 `junkyard` 가 그것이다.
        ///
        ///   CH1 로봇 스네이크   쓰레기 집적장        junkyard
        ///   CH2 크러셔          미사일 저장·정비     missile
        ///   CH3 파이썬          밤의 도시 거리       street
        ///   CH4 슬러지          야간 정유소          refinery
        ///   CH5 가디언          포로 수용실          holding
        ///   CH6 킹핀            공중기지 옥상        rooftop
        ///
        /// ⚠ 이 짝은 **사용자가 원작 스샷을 보고 확인해 준 것**이다(2026-09-07).
        ///   그 전 값은 기획 문서가 스스로 "소거법"이라고 적어 둔 유추였고,
        ///   실제로 절반이 틀렸다. 유추한 값을 정본처럼 쓰면 이렇게 된다.
        ///
        /// ⚠ `lab`(연구소)은 이제 어느 보스도 안 쓴다. 파일은 남겨 둔다 —
        ///   일반 방 배경으로는 여전히 나온다.
        ///
        /// ⚠ 없는 세트는 `EnvSprite` 가 알아서 기본형으로 떨어뜨린다. 여기서 걱정하지 않는다.
        /// </summary>

        // ══ 보스 무대 짝 · 방 바닥 ════════════════════════════════

        private static string BossEnvOf(string slug) => slug switch
        {
            "robot_snakes" => "junkyard",   // CH1
            "crusher"      => "missile",    // CH2
            "python"       => "street",     // CH3
            "sludge"       => "rooftop",    // CH4
            "guardian"     => "lab",        // CH5
            "kingpin"      => "refinery",   // CH6
            _              => string.Empty,
        };

        // ── 방 바닥 ──────────────────────────────────────────────
        //
        // 바닥은 스프라이트가 아니라 **화면 한 장짜리 배경**이라 아틀라스에 넣지 않는다.
        // 720×1530 무압축 한 장이 4MB 를 넘으므로 18장을 묶으면 챕터마다 수십 MB 가
        // 상주한다. 대신 방마다 한 장씩 불러오고 **직전 것을 놓아 준다** —
        // 어느 순간에도 살아 있는 바닥은 한 장이다.
        //
        // 불러오는 동안에는 지난 방 바닥을 그대로 둔다. 먼저 지우면 방을 넘길 때마다
        // 바닥이 한 번 깜빡인다.

        private const string RoomFloorPrefix = "roomfloor/";

        /// <summary>
        /// 새 배경이 다 올 때까지 **모든 방이 함께 쓰는 배경**.
        /// 규격(720×936) · 바닥 줄눈 36 px · 밝기가 모두 맞는 유일한 한 장이다.
        /// 나머지 17 장이 규격에 맞춰 다시 오면 이 상수와 `ApplyRoomFloor` 의 임시 분기를 지운다.
        /// </summary>
        private const string InterimRoomFloor = "roomfloor_ch1_twin_platform";

        /// <summary>
        /// 지형 배경 6종이 **다 들어온 챕터까지의 번호**. 여기까지는 방마다 제 배경을 쓴다.
        /// 그 위 챕터는 아직 그림이 없어 `InterimRoomFloor` 한 장으로 버틴다.
        /// CH2 6장이 들어오면 2, CH3 까지 오면 3 으로 올린다.
        /// </summary>
        private const int FloorReadyChapters = 1;

        /// <summary>
        /// 이 방이 쓸 배경.
        ///
        /// ── 왜 지형(Template)이 아니라 무대(Environment)로 나누나 ──────────
        /// 지형별로 배경을 따로 그려 봤더니 **화면에서 구별이 안 됐다.**
        /// CH1 여섯 장을 재 보면 서로 평균 픽셀차가 0.09~3.37 이다
        /// (`pillar_cross` 는 `twin_platform` 과 사실상 같은 그림이다).
        /// 반면 무대가 바뀌면 14~25 로 벌어진다. 눈에 보이는 축은 무대뿐이다.
        ///
        /// ── 스테이지 6구간 ────────────────────────────────────────
        /// 원작이 스테이지 6곳이다. 우리 48방을 **챕터 3개 × 앞뒤**로 갈라
        /// 여섯 구간을 만들고, 구간마다 무대를 바꾼다. 원작 순서를 따른다.
        ///
        ///   1  CH1 001~006   유령 연구소            (우리 설정 · 이야기의 출발)
        ///   2  CH1 007~012   쓰레기 집적장          (원작 1)
        ///   3  CH2 001~008   미사일 저장·정비 기지  (원작 2)
        ///   4  CH2 009~016   밤의 도시 거리         (원작 3)
        ///   5  CH3 001~010   밤의 공중기지 옥상     (원작 4)
        ///   6  CH3 011~020   야간 정유소·굴뚝       (원작 6)
        ///   보스 6방         포로 수용실·빙의 코어  (원작 5) — 챕터와 무관한 전용 아레나
        ///
        /// 지형 차이는 배경이 아니라 그 위에 얹히는 장애물 배치가 만든다.
        /// </summary>
        private static string FloorKeyOf(RoomEntry room, int fallbackChapter)
        {
            if (room == null) return InterimRoomFloor;

            // ⚠ **배정표가 적어 뒀으면 그것이 이긴다.** 아래 유추는 3챕터 시절 것이라
            //   챕터를 1~3 으로 자른다 — 6챕터에서는 CH4~6 이 CH3 바닥을 받는다.
            //   배정표가 없는 방(절차 생성)만 유추로 내려간다.
            if (!string.IsNullOrEmpty(room.Floor)) return room.Floor;

            // 보스는 챕터와 무관하게 **제 전용 바닥**으로 간다. 무게가 다른 자리다.
            //
            // 여섯 장이 다 와 있다(720×936). 아직 안 온 보스가 생기면
            // `LoadRoomFloorAsync` 의 대비책이 공용 아레나로 떨어뜨린다.
            if (room.IsBoss) return $"roomfloor_{BossSlug(room.BossId)}";

            int ch = Mathf.Clamp(room.Chapter, 1, 3);

            // 방 번호로 챕터의 앞·뒤를 가른다. `ROOM_CH2_007` → 7.
            // 깊이 값을 따로 들고 있지 않으므로 ID 가 가장 확실한 순서다.
            int no = RoomNumberOf(room.RoomId);
            int total = ch == 1 ? Ch1RoomCount : ch == 2 ? Ch2RoomCount : Ch3RoomCount;
            bool late = no > total / 2;
            int stage = (ch - 1) * 2 + (late ? 2 : 1);   // 1~6

            // ── 테스트 — CH1 한 챕터에서 여섯 테마를 다 보여 준다 ──────────
            //
            // 평소 배치대로면 6구간을 다 보려면 **48방을 끝까지 깨야 한다.**
            // 배경만 훑어보고 싶을 때 그건 너무 멀다. 이 스위치를 켜면
            // CH1 12방 안에서 여섯 테마가 차례로 나온다 — 한 챕터만 깨면 다 본다.
            //
            // ⚠ CH1 의 보스는 006 · 012 다. 보스는 위에서 이미 전용 아레마로 빠졌으므로
            //   006 을 건너뛴 만큼 번호를 하나 당겨야 007 이 6번째 테마가 된다.
            if (CycleThemesInChapter1 && ch == 1)
            {
                int i = no - 1 - (no > 6 ? 1 : 0);
                stage = i % 6 + 1;
            }

            return StageFloorKey(stage, room);
        }
    }
}
