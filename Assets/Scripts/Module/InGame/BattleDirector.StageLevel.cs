using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 판 레벨업 — 원작(궁수의 전설)처럼 **잡아서 경험치를 모은다** (3단계 PD 2026-10-22).
    ///
    /// 필요량은 30 × Lv(30 · 60 · 90 …), 잡몹 · 적 몸은 1기 8 · 엘리트 16 · 중간보스 대장 30.
    /// 챕터마다 몹이 130~153기라 30방 끝에 Lv9(레벨업 8번)가 된다 — 원작 Lv9~10 과 같고,
    /// 챕터 · 몹 Lv 와 상관없이 비슷하다(PD 2026-10-16 「챕터 상관없이 레벨업 카드는 비슷해야」).
    ///
    /// ── 예전(2026-10-16 ~ 10-22) ───────────────────────────────
    /// 「싸움이 있던 방을 비우면 레벨 +1」이었다. 그때는 방마다 적 수가 챕터마다 3배 넘게 달라서였다.
    /// 이제 방 구성이 원작 그대로라 적 수가 챕터마다 거의 같다 — 원작 규칙으로 돌아간다.
    ///
    /// 레벨업 창은 그대로 **방이 빈 뒤** 뜬다(`TickPendingLevelUp`) — 싸우는 도중에 창이 막지 않게.
    /// 마지막 방(보스)은 판이 끝나므로 주지 않는다. 밤은 예전 공식(`ExpToNext`), 탑은 레벨업이 없다.
    ///
    /// ⚠ 유령 경험치(로비 · 계정 레벨)는 이것과 별개다 — 챕터 클리어 보상이다(`PlayerDataService.GrantChapterClearAsync`).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private bool StageLevelRule => !IsNightRun && !IsTowerRun;

        /// <summary>
        /// 녹화 도구(몬스터 · 카드 · 스킬 녹화)가 리플렉션으로 켠다 — 켜져 있으면 판 경험치를 안 준다.
        /// 레벨업 창이 녹화를 가리지 않게. 이름은 예전 「이 방 레벨업을 이미 줬다」 칸 그대로(도구들이 이 이름을 쓴다).
        /// </summary>
        private bool _stageLevelGiven;

        /// <summary>지금 판의 필요 경험치 — 일반 판은 30 × Lv, 밤은 예전 등비 공식.</summary>
        private int NeedExp(int level) => StageLevelRule ? _config.StageExpToNext(level) : _config.ExpToNext(level);

        /// <summary>방에 들어설 때. 경험치는 방을 넘어 이어진다 — 지울 것이 없다(예전 「방 하나 = 레벨 하나」의 자리).</summary>
        private void ResetStageLevel() { }

        /// <summary>적 하나를 잡았다 — 그 적의 경험치를 더한다. 이 적은 이미 적 목록에서 빠져 있다.</summary>
        private void FillStageExp(Unit u)
        {
            if (_stageLevelGiven) return;
            int exp = u == _midBoss ? _config.StageExpMidBoss
                    : u.IsElite ? _config.StageExpPerEnemy * _config.StageExpEliteMul
                    : _config.StageExpPerEnemy;
            GainExp(exp);
        }

        /// <summary>예전 「방이 비면 +1」 자리. 이제 처치 경험치로 오르므로 할 일이 없다.</summary>
        private void TickStageLevel() { }
    }
}
