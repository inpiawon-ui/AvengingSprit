using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 코만도(기관총) 액티브 「제압 사격」 연출 도우미 — 시안 `_exchange/mock1030_skill/mock_suppress_free2.png`(PD 2026-10-30 2번).
    /// 총구 위 화염(`supflare`)을 겨눈 쪽으로 돌려 띄우고, 둔화된 적 발밑에 회색 소용돌이(`supswirl`)가 둔화 동안 따라다닌다.
    /// 탄 줄기 조각은 탄막 요새와 같은 `SpawnFortressDash` 를 쓴다(같은 총 · 같은 총알 그림).
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>위를 향해 그린 그림을 dir 쪽으로 돌려 띄운다.</summary>
        private Impact PlayAimedFx(string name, Vector2 at, Vector2 dir, float size)
        {
            var fx = PlayFx(name, at, size, loop: false);
            if (fx == null) return null;
            fx.SetFrameSeconds(0.04f);
            fx.SetAngle(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            return fx;
        }

        /// <summary>from 에서 dir 로 뻗은 폭 width 줄 위에서 가장 가까운 적.</summary>
        private Unit NearestInPath(Vector2 from, Vector2 dir, float length, float width)
        {
            Unit best = null;
            float bestD = float.MaxValue;
            foreach (var e in EnemiesInPath(from, dir, length, width))
            {
                if (e == null || !e.IsAlive) continue;
                float d = (e.Position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        // ── 둔화 소용돌이 — 적마다 하나, 둔화가 이어지는 동안 발밑을 따라다닌다 ──
        private readonly List<Unit> _suppressedUnits = new();
        private readonly List<Impact> _suppressedFx = new();
        private readonly List<float> _suppressedLeft = new();

        private void MarkSuppressed(Unit e)
        {
            if (e == null || !e.IsAlive) return;
            _pfx?.Burst(ParticleFxKind.Smoke, ParticleElement.Dust, e.Position, 4, 60f, 22f, 0.5f, 0f);
            int at = _suppressedUnits.IndexOf(e);
            if (at >= 0) { _suppressedLeft[at] = SuppressSlowSeconds; return; }
            var fx = PlayFloorFx("supswirl", e.Position, SuppressSwirlSize, loop: true);
            if (fx == null) return;
            fx.SetFrameSeconds(0.09f);
            _suppressedUnits.Add(e);
            _suppressedFx.Add(fx);
            _suppressedLeft.Add(SuppressSlowSeconds);
        }

        private void TickSuppressSwirls(float dt)
        {
            for (int i = _suppressedUnits.Count - 1; i >= 0; i--)
            {
                var e = _suppressedUnits[i];
                _suppressedLeft[i] -= dt;
                if (e == null || !e.IsAlive || _suppressedLeft[i] <= 0f)
                {
                    _suppressedFx[i]?.Stop();
                    _suppressedUnits.RemoveAt(i);
                    _suppressedFx.RemoveAt(i);
                    _suppressedLeft.RemoveAt(i);
                    continue;
                }
                _suppressedFx[i]?.MoveTo(e.Position);
            }
        }
    }
}
