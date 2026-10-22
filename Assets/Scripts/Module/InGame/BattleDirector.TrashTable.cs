using System.Collections.Generic;
using Game.Character;

namespace Game.Module.InGame
{
    /// <summary>
    /// 몬스터 표(`TableData/TrashTable`)를 전투에서 읽는 자리 — 2단계 몬스터 조합(PD 2026-10-22).
    ///
    /// 몬스터 하나 = **몸 + 성질.** 몸(`TrashDef.Body`)이 그림 · 행동 코드 · 쪽을 정하고, 성질(`TrashTrait`)이
    /// 그 몸이 무엇을 더 하는지 정한다. 예전 「챕터 단계」 문턱(`…FromChapter`)은 걷어내고 전부 성질로 바꿨다 —
    /// 이제 한 방에 보통 해골과 터지는 해골이 같이 선다.
    ///
    /// 잡몹 `Unit.Key` 는 **몸 키**다(행동 코드가 `e.Key == TrashSkeletonKey` 로 몸을 가른다). 어느 몬스터인지는
    /// `Unit.Profile`(그 몬스터의 `HostEntry`)로 찾는다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private TrashTable _trashTable;
        private readonly Dictionary<string, HostEntry> _trashEntries = new();
        private readonly Dictionary<HostEntry, TrashDef> _trashDefs = new();

        /// <summary>몬스터 키(또는 몸 키) → 잡몹. 표가 없으면 예전 정의로 떨어진다.</summary>
        private HostEntry TrashEntryOf(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_trashEntries.TryGetValue(key, out var cached)) return cached;

            var def = _trashTable != null ? _trashTable.Get(key) : null;
            if (def == null) return LegacyTrashEntryOf(key);

            var e = HostEntry.CreateTrash(def.Body, def.NameKr, def.AttackKind,
                                          def.Hp, def.Atk, def.MoveMps, def.EngageMps,
                                          def.RangeMeters, def.Interval, def.Telegraph,
                                          def.ShotCount, def.SpreadDegrees, def.Sprite);
            _trashEntries[key] = e;
            _trashDefs[e] = def;
            return e;
        }

        /// <summary>몬스터 키(또는 몸 키)의 정의. 표가 없으면 null.</summary>
        private TrashDef TrashDefOf(string key) => _trashTable != null ? _trashTable.Get(key) : null;

        /// <summary>이 잡몹의 몬스터 정의. 호스트 · 보스 · 표 밖 잡몹이면 null.</summary>
        private TrashDef TrashDefOf(Unit u)
            => u != null && u.Profile != null && _trashDefs.TryGetValue(u.Profile, out var d) ? d : null;

        /// <summary>
        /// 이 잡몹이 그 성질을 가졌나.
        /// 표가 없으면(임포트 전) 몸의 Lv1 성질만 — 박쥐 · 멧돼지 돌진처럼 몸의 기본 행동은 켜 둔다.
        /// </summary>
        private bool HasTrait(Unit u, TrashTrait t)
        {
            var d = TrashDefOf(u);
            if (d != null) return d.Has(t);
            if (u == null) return false;
            return t == TrashTrait.Dive && (u.Key == TrashBatKey || u.Key == TrashBoarKey);
        }

        /// <summary>몬스터 키 → 몸 키. 표에 없으면 키 그대로.</summary>
        private string BodyOfTrash(string key)
        {
            var d = TrashDefOf(TrashKeyAlias(key));
            return d != null ? d.Body : TrashKeyAlias(key);
        }
    }
}
