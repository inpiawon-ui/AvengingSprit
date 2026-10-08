using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 중간보스를 잡은 방 — 물건을 다 치우고 악마의 제단만 남긴다(기획 2026-10-08).
    ///
    /// PD 「다른 오브젝트들은 지우고 악마 사원만 남게 해줘 — 그래야 유저가 알지」.
    /// 제단이 상자 · 기둥 · 불바닥 사이에 섞여 서 있으면 방 장식으로 읽혀 지나쳐 버린다.
    /// 텅 빈 바닥 한가운데 제단 하나만 남아야 「저기 가 보라」가 된다.
    ///
    /// 한꺼번에 사라지면 화면이 툭 바뀐 것처럼 보인다 — 제단 자리에서 가까운 것부터 먼지를 내며
    /// 물결처럼 걷힌다. 벽 · 출구는 물건이 아니라 손대지 않는다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>가장 먼 물건이 걷히기까지(초). 짧아야 제단이 서는 순간과 겹친다.</summary>
        private const float AltarClearSpread = 0.6f;

        private readonly List<Obstacle> _altarClearOrder = new();

        private void ClearRoomForAltar()
        {
            if (_obstacles.Count == 0) return;

            // 함정 박자 · 기믹 표는 지금 비운다 — 물건이 걷히는 동안 피스톤이 쳐서는 안 된다
            _pistons.Clear();
            ClearGimmick2();
            ClearGimmicks();
            _hazardTimer.Clear();
            for (int i = 0; i < _obstacles.Count; i++) _obstacles[i].HazardOn = false;

            var center = RoomPropAt();
            _altarClearOrder.Clear();
            _altarClearOrder.AddRange(_obstacles);
            _altarClearOrder.Sort((a, b) => (a.ShotBounds.center - center).sqrMagnitude
                                     .CompareTo((b.ShotBounds.center - center).sqrMagnitude));
            float far = Mathf.Max(1f, (_altarClearOrder[_altarClearOrder.Count - 1].ShotBounds.center - center).magnitude);

            int room = _roomIndex;
            for (int i = 0; i < _altarClearOrder.Count; i++)
            {
                var o = _altarClearOrder[i];
                float delay = (o.ShotBounds.center - center).magnitude / far * AltarClearSpread;
                // fire-and-forget: 물건마다 제 차례에 걷힌다. 방을 나가면 그 방 물건은 이미 다 지워져 있다
                VanishAsync(o, delay, room).Forget();
            }
            _altarClearOrder.Clear();
        }

        private async UniTaskVoid VanishAsync(Obstacle o, float delay, int room)
        {
            if (delay > 0f)
                await UniTask.Delay(Mathf.RoundToInt(delay * 1000f), cancellationToken: this.GetCancellationTokenOnDestroy())
                             .SuppressCancellationThrow();
            if (this == null || room != _roomIndex || !_obstacles.Contains(o)) return;

            _pfx?.Puff(o.ShotBounds.center, ParticleElement.Dust, Mathf.Clamp(o.ShotBounds.width / 72f, 0.6f, 1.4f));
            RemoveObstacle(o);
        }
    }
}
