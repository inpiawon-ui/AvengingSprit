using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 바닥 예고 — 잡몹과 장애물이 쓰는 「곧 여기가 아프다」.
    ///
    /// ── 왜 생겼나 (2026-09-28) ────────────────────────────────────
    /// 예고 도형은 보스만 썼다(`_dangerView` 한 장). 잡몹의 돌진·내려찍기와
    /// 낙하물·지뢰·화염 분사 같은 장애물도 **피할 자리를 먼저 보여 줘야** 하는데,
    /// 방 하나에 여럿이 동시에 뜨므로 한 장으로는 안 된다. 그래서 풀로 둔다.
    ///
    /// 도형은 보스와 같은 것(<see cref="DangerShape"/>)을 그대로 쓴다 —
    /// **그린 자리와 맞는 자리가 같은 함수**라는 그 약속을 잡몹에도 지킨다.
    /// 그림도 보스 예고와 같은 빗금(`fx_danger_hatch`)이다. 새로 그린 것이 없다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private sealed class Warn
        {
            public DangerView View;
            public DangerShape Shape;
            public float Left, Total;
            /// <summary>플레이어가 받는 피해. 0 이면 보여 주기만 한다.</summary>
            public int Damage;
            /// <summary>적이 잃는 체력 비율. 0 이면 적은 안 다친다.</summary>
            public float EnemyHpRatio;
            /// <summary>터질 때 띄울 폭발 그림(`SpawnImpact` 종류). 없으면 안 띄운다.</summary>
            public string ImpactKind;
            /// <summary>터질 때 띄울 이펙트(`PlayFx` 이름). 없으면 안 띄운다.</summary>
            public string FxName;
            public float FxSize;
            /// <summary>이 예고를 건 몸. 그 몸이 죽으면 예고도 거둔다.</summary>
            public Unit Owner;
            public bool Active;
        }

        private readonly List<Warn> _warns = new();

        private static DangerShape DiscShape(Vector2 at, float radius)
            => new DangerShape { Shape = DangerShape.Kind.Disc, Origin = at, Radius = radius };

        private static DangerShape BandShape(Vector2 from, Vector2 dir, float width, float length)
            => new DangerShape
            {
                Shape = DangerShape.Kind.Band, Origin = from,
                Dir = dir.sqrMagnitude < 0.0001f ? Vector2.down : dir.normalized,
                Width = width, Length = length,
            };

        /// <summary>
        /// 예고를 건다. <paramref name="seconds"/> 뒤에 그 도형 안을 친다.
        /// 풀에서 빈 것을 꺼내 쓰므로 교전 중에 새로 만들지 않는다(처음 몇 번만 만든다).
        /// </summary>
        private Warn StartWarn(DangerShape shape, float seconds, int damage, Unit owner = null,
                               string impactKind = null, string fxName = null, float fxSize = 0f,
                               float enemyHpRatio = 0f)
        {
            Warn w = null;
            for (int i = 0; i < _warns.Count; i++)
                if (!_warns[i].Active) { w = _warns[i]; break; }
            if (w == null)
            {
                w = new Warn();
                if (_fieldLayer != null) w.View = DangerView.Create(_fieldLayer);
                _warns.Add(w);
            }

            w.Shape = shape;
            w.Left = w.Total = Mathf.Max(0.05f, seconds);
            w.Damage = damage;
            w.EnemyHpRatio = enemyHpRatio;
            w.ImpactKind = impactKind;
            w.FxName = fxName;
            w.FxSize = fxSize;
            w.Owner = owner;
            w.Active = true;
            if (w.View != null) w.View.Show(shape, _roomSize, GetSprite("fx_danger_hatch"), safe: false);
            return w;
        }

        private void TickWarns(float dt)
        {
            for (int i = 0; i < _warns.Count; i++)
            {
                var w = _warns[i];
                if (!w.Active) continue;
                w.Left -= dt;
                if (w.View != null) w.View.Tick(dt, 1f - w.Left / w.Total);
                if (w.Left > 0f) continue;

                w.Active = false;
                if (w.View != null) w.View.Hide();
                ResolveWarn(w);
            }
        }

        /// <summary>예고가 찼다 — 그 도형 안을 친다. 그린 도형과 **같은** `Contains` 로 잰다.</summary>
        private void ResolveWarn(Warn w)
        {
            var at = w.Shape.ImpactAt(_roomSize);
            if (!string.IsNullOrEmpty(w.ImpactKind)) SpawnImpact(at, w.ImpactKind, w.FxSize);
            if (!string.IsNullOrEmpty(w.FxName)) PlayFx(w.FxName, at, w.FxSize, loop: false);

            var me = Avatar;
            if (w.Damage > 0 && _host != null && me != null)
            {
                // 발밑으로 잰다 — 이동 판정·가시 판정과 같은 자다. 몸 가운데도 같이 본다
                // (도형 가장자리에 발만 걸친 것과 몸만 걸친 것 둘 다 「안」이다).
                var foot = new Vector2(me.Position.x, me.Position.y - FootDrop(me));
                if (w.Shape.Contains(foot, _roomSize) || w.Shape.Contains(me.Position, _roomSize))
                    DamagePlayer(w.Damage);
            }

            if (w.EnemyHpRatio <= 0f) return;
            // ⚠ 뒤에서부터 돈다 — `KillEnemy` 가 목록에서 뺀다.
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsBoss) continue;
                if (!w.Shape.Contains(e.Position, _roomSize)) continue;
                int dmg = Mathf.Max(1, Mathf.RoundToInt(e.HpMax * w.EnemyHpRatio));
                e.IsAggro = true;
                ShowDamage(e.Position, dmg, toEnemy: true);
                if (e.TakeDamage(dmg)) KillEnemy(e);
            }
        }

        /// <summary>그 몸이 건 예고를 거둔다. 돌진을 예고하던 박쥐가 죽었는데 띠만 남아 치면 안 된다.</summary>
        private void CancelWarnsOf(Unit owner)
        {
            if (owner == null) return;
            for (int i = 0; i < _warns.Count; i++)
            {
                var w = _warns[i];
                if (!w.Active || w.Owner != owner) continue;
                w.Active = false;
                if (w.View != null) w.View.Hide();
            }
        }

        /// <summary>방을 나갈 때 — 남은 예고를 전부 거둔다.</summary>
        private void ClearWarns()
        {
            for (int i = 0; i < _warns.Count; i++)
            {
                _warns[i].Active = false;
                if (_warns[i].View != null) _warns[i].View.Hide();
            }
        }
    }
}
