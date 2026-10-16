using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 스테이지 레벨업 (PD 2026-10-16 「챕터 상관없이 레벨업 카드는 생성되는 경험치는 비슷해야 · 스테이지 클리어시 레벨업 카드가 한 번씩 떠야」).
    ///
    /// 예전엔 적을 잡을 때마다 같은 경험치(`ExpPerEnemy`)를 줬다. 방마다 적이 1챕터 3~5, 10챕터 10~12 라
    /// 후반 챕터는 같은 방 수에서 레벨이 3배 넘게 올랐다. 이제 **싸움이 있던 방(스테이지)을 비우면 레벨이 정확히 하나** 오르고
    /// 카드가 한 번 뜬다 — 챕터 · 적 수와 상관없다.
    ///
    /// 잡는 동안 경험치 막대는 남은 적 수에 맞춰 고르게 차오르고(가득은 안 찬다), 방이 비는 순간 가득 차며 레벨이 오른다.
    /// 마지막 방(보스)은 판이 끝나므로 주지 않는다. 밤 · 탑은 예전 규칙 그대로(밤은 방이 안 비고, 탑은 카드가 없다).
    ///
    /// ⚠ 유령 경험치(로비 · 계정 레벨)는 이것과 별개다 — 챕터 클리어 보상이다(`PlayerDataService.GrantChapterClearAsync`).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private bool _stageFought;      // 이 방에서 적을 잡았거나 빼앗았다
        private bool _stageLevelGiven;  // 이 방의 레벨업을 이미 줬다

        private bool StageLevelRule => !IsNightRun && !IsTowerRun;

        private void ResetStageLevel()
        {
            _stageFought = false;
            _stageLevelGiven = false;
        }

        /// <summary>적을 하나 잡았다 — 막대만 채운다. 이 적은 이미 적 목록에서 빠져 있다.</summary>
        private void FillStageExp()
        {
            _stageFought = true;
            int need = _config.ExpToNext(_level);
            int rest = _enemies.Count;
            // 남은 몫을 (남은 적 + 2) 로 나눠 더한다 — 고르게 차오르고, 방이 빌 때까지 가득은 안 찬다
            int gain = Mathf.Max(0, (need - _exp) / (rest + 2));
            _exp = Mathf.Min(need - 1, _exp + gain);
            PublishExp(need);
        }

        /// <summary>싸움이 있던 방이 비었다 — 레벨을 하나 올리고 카드를 한 번 띄운다(`TickPendingLevelUp`).</summary>
        private void TickStageLevel()
        {
            if (!StageLevelRule || _stageLevelGiven || !_stageFought || _exitOpen || IsLastRoom) return;
            if (_enemies.Count > 0) return;
            _stageLevelGiven = true;
            _exp = 0;
            _level++;
            PublishExp(_config.ExpToNext(_level));
            _pendingLevelUps++;
        }
    }
}
