using System.Collections.Generic;
using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 원소판 잡몹 — 불 · 얼음 · 독(2단계 몬스터 조합, PD 2026-10-22). 같은 몸에 **탄 · 자국의 성질만** 바뀐다.
    ///
    ///   불   맞으면 화상(초당 피해, 3초)            · 불 고릴라는 착지 자리에 불 2초
    ///   얼음 맞으면 냉기(이동 35% 느려짐, 1.5초)   · 얼음 코일은 박격 자리가 느린 바닥 2초
    ///   독   맞으면 독(초당 피해, 3초)              · 독 해골은 지나간 자리에 독 자국 · 독 버섯 · 독샘 거미는 독 웅덩이
    ///
    /// 내 몸의 지속 피해는 `DamagePlayer` 를 거치지 않는다 — 그쪽은 맞을 때마다 0.4초 흘림(`_hurtGrace`)을 걸어
    /// 독 한 방울이 진짜 공격을 막아 버린다. 몸 체력에서 바로 깎고 숫자만 띄운다(무적이면 안 깎는다).
    ///
    /// ⚠ 바닥 자국의 겉모습은 지금 있는 파티클 입자(`ParticleFxPool`)로 낸 **임시 표현**이다 — 이펙트 시안(fx-art-pipeline) 뒤 갈아 끼운다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float ElementBurnSeconds = 3f;
        private const float ElementBurnDps = 4f;
        private const float ElementPoisonSeconds = 3f;
        private const float ElementPoisonDps = 3f;
        private const float ElementChillSeconds = 1.5f;
        private const float ElementChillSlow = 0.35f;
        private const int ElementDotChunk = 2;            // 한 번에 떨어뜨리는 양 — 숫자가 너무 틱틱 뜨지 않게

        private const float ZoneTickSeconds = 0.3f;       // 장판 안에 서 있으면 이만큼마다 다시 건다
        private const float FireZoneSeconds = 2f;
        private const float FireZoneMeters = 1.1f;
        private const float IceZoneSeconds = 2f;
        private const float PoisonPoolSeconds = 3f;
        private const float PoisonPoolMeters = 1.0f;
        private const float TrailEverySeconds = 0.6f;     // 독 해골 — 이만큼마다 발자국 한 점
        private const float TrailSeconds = 2f;
        private const float TrailMeters = 0.5f;

        private float _playerBurnLeft, _playerPoisonLeft, _playerChillLeft, _playerDotAccum;

        private struct ElementZone
        {
            public Vector2 At;
            public float Radius, Left, FxClock;
            public TrashElement Element;
        }

        private readonly List<ElementZone> _zones = new();
        private readonly Dictionary<Unit, float> _trailClock = new();

        /// <summary>냉기 · 얼음 바닥의 이동 배율 — 내 이동에 곱한다.</summary>
        private float ElementMoveMul => _playerChillLeft > 0f ? 1f - ElementChillSlow : 1f;

        /// <summary>이 잡몹의 원소. 원소판이 아니면 None.</summary>
        private TrashElement ElementOf(Unit u)
        {
            var d = TrashDefOf(u);
            return d != null ? d.Element : TrashElement.None;
        }

        /// <summary>원소 탄 · 근접에 맞았다. `DamagePlayer` 바로 뒤에 부른다(유령 · 무적이면 아무 일 없다).</summary>
        private void ApplyElementToPlayer(TrashElement el)
        {
            if (el == TrashElement.None || _host == null || IsInvulnerable) return;
            switch (el)
            {
                case TrashElement.Fire: _playerBurnLeft = ElementBurnSeconds; break;
                case TrashElement.Poison: _playerPoisonLeft = ElementPoisonSeconds; break;
                case TrashElement.Ice: _playerChillLeft = ElementChillSeconds; break;
            }
        }

        /// <summary>바닥에 원소 자국을 깐다. 불 착지 · 얼음 박격 · 독 웅덩이 · 독 발자국.</summary>
        private void AddElementZone(Vector2 at, float radiusMeters, float seconds, TrashElement el)
        {
            if (el == TrashElement.None) return;
            _zones.Add(new ElementZone { At = at, Radius = Meters(radiusMeters), Left = seconds, Element = el });
            _pfx?.Puff(at, ParticleOf(el), 0.8f);
        }

        private static float ZoneSecondsOf(TrashElement el) => el switch
        {
            TrashElement.Ice => IceZoneSeconds,
            TrashElement.Poison => PoisonPoolSeconds,
            _ => FireZoneSeconds,
        };

        private static ParticleElement ParticleOf(TrashElement el) => el switch
        {
            TrashElement.Ice => ParticleElement.Ice,
            TrashElement.Poison => ParticleElement.Venom,
            _ => ParticleElement.Fire,
        };

        /// <summary>매 프레임 — 내 몸의 화상 · 독 · 냉기, 바닥 자국, 독 해골 발자국. `TickGimmicks` 가 부른다.</summary>
        private void TickElements(float dt)
        {
            var me = Avatar;
            var foot = me != null ? new Vector2(me.Position.x, me.Position.y - FootDrop(me)) : Vector2.zero;

            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                var z = _zones[i];
                z.Left -= dt;
                if (z.Left <= 0f) { _zones.RemoveAt(i); continue; }
                z.FxClock -= dt;
                if (z.FxClock <= 0f)
                {
                    z.FxClock = ZoneTickSeconds;
                    var jitter = Random.insideUnitCircle * z.Radius * 0.7f;
                    _pfx?.Puff(z.At + jitter, ParticleOf(z.Element), 0.45f);
                    if (me != null && (foot - z.At).sqrMagnitude <= z.Radius * z.Radius) ApplyElementToPlayer(z.Element);
                }
                _zones[i] = z;
            }

            TickPoisonTrails(dt);

            if (_host == null) { _playerBurnLeft = _playerPoisonLeft = _playerChillLeft = _playerDotAccum = 0f; return; }
            if (_playerChillLeft > 0f) _playerChillLeft -= dt;

            float dps = 0f;
            if (_playerBurnLeft > 0f) { _playerBurnLeft -= dt; dps += ElementBurnDps; }
            if (_playerPoisonLeft > 0f) { _playerPoisonLeft -= dt; dps += ElementPoisonDps; }
            if (dps <= 0f) { _playerDotAccum = 0f; return; }
            _playerDotAccum += dps * dt;
            if (_playerDotAccum < ElementDotChunk || IsInvulnerable) return;
            _playerDotAccum -= ElementDotChunk;
            ShowDamage(_host.Position, ElementDotChunk, toEnemy: false);
            _pfx?.Puff(_host.Position, _playerBurnLeft > 0f ? ParticleElement.Fire : ParticleElement.Venom, 0.4f);
            if (_host.TakeDamage(ElementDotChunk)) LoseHost();
            else PublishHp();
        }

        /// <summary>독 해골 — 걸은 자리에 작은 독 자국을 남긴다(쫓아오는 길이 곧 피할 길).</summary>
        private void TickPoisonTrails(float dt)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsHidden || e.Key != TrashSkeletonKey) continue;
                if (ElementOf(e) != TrashElement.Poison) continue;
                _trailClock.TryGetValue(e, out float t);
                t -= dt;
                if (t <= 0f)
                {
                    t = TrailEverySeconds;
                    AddElementZone(e.Position, TrailMeters, TrailSeconds, TrashElement.Poison);
                }
                _trailClock[e] = t;
            }
        }

        /// <summary>방을 나갈 때 — 자국과 내 몸의 원소 상태를 거둔다.</summary>
        private void ClearElements()
        {
            _zones.Clear();
            _trailClock.Clear();
            _playerBurnLeft = _playerPoisonLeft = _playerChillLeft = _playerDotAccum = 0f;
        }
    }
}
