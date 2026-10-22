using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 엘리트 잡몹 — 2단계 몬스터 조합(PD 2026-10-22 「엘리트는 제안대로」).
    ///
    /// 같은 몬스터의 강한 판이다. 몸 그림은 같고 **크기 ×1.25 · 체력 ×2.4 · 공격 ×1.5**(GameConfig 엘리트 배율),
    /// 떨구는 골드 4배 · 무더기 6(예전부터 있던 엘리트 보상). 챕터 표 `elite` 칸 = 방 하나에 세우는 수 —
    /// **챕터 뒤쪽 절반의 전투방에만** 선다(앞 방에서 몬스터를 배우고, 뒤 방에서 강한 판을 만난다).
    ///
    /// ⚠ 표식(발밑 고리 · 불티)은 이펙트 시안(fx-art-pipeline, `_exchange/guide_elite.md`) 통과 뒤 넣는다 — 지금은 크기로만 읽힌다(미검).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float EliteScale = 1.25f;
        private const float EliteFromProgress = 0.5f;

        private readonly List<Unit> _elitePick = new();

        /// <summary>방을 세운 뒤 — 그 챕터 몫만큼 잡몹을 엘리트로 올린다. 덩치 큰 놈부터.</summary>
        private void PromoteElites(RoomEntry room)
        {
            if (_config == null || IsModeRun) return;
            if (room != null && (room.IsMidBoss || room.IsBoss)) return;   // 관문 방의 부하는 그대로 — 대장이 주인공이다
            int ch = _canonRoom != null ? _canonRoom.Chapter : _runChapter;
            int n = ChapterRow(ch).Elite;
            if (n <= 0 || RoomProgress(ch) < EliteFromProgress) return;

            _elitePick.Clear();
            for (int i = 0; i < _enemies.Count; i++)
            {
                var u = _enemies[i];
                if (u == null || !u.IsAlive || u.IsBoss || u.IsHostBody || u.IsElite || u == _midBoss) continue;
                if (TrashDefOf(u) == null) continue;   // 표 밖 몸(소환물 · 모드 몸)은 안 올린다
                _elitePick.Add(u);
            }
            _elitePick.Sort((a, b) => b.HpMax.CompareTo(a.HpMax));
            for (int i = 0; i < n && i < _elitePick.Count; i++) MakeElite(_elitePick[i]);
            _elitePick.Clear();
        }

        private void MakeElite(Unit u)
        {
            u.MarkAsElite();
            int hp = Mathf.RoundToInt(u.HpMax * _config.EliteHpMul);
            u.SetHpMax(hp);
            u.Heal(hp);
            u.ScaleAttack(_config.EliteAtkMul);
            u.transform.localScale = Vector3.one * EliteScale;
        }
    }
}
