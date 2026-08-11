using System.Collections.Generic;
using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 보스 한 기의 행동 상태. 잡몹은 "접근 후 사거리 안이면 공격" 한 줄이면 되지만,
    /// 보스는 여러 행동을 각자의 쿨다운으로 돌리고 체력에 따라 레퍼토리가 늘어난다.
    ///
    /// 실제 발사·소환은 `BattleDirector` 가 한다. 여기서는 **언제 무엇을 할지**만 정한다.
    /// </summary>
    public sealed class BossBrain
    {
        /// <summary>
        /// 페이즈 경계 — 체력 비율이 이 값 아래로 내려가면 다음 페이즈다.
        /// 정본이 보스마다 따로 정해 뒀다(B01 0.7/0.35 · B02 0.65/0.3). 없으면 이 기본값.
        /// </summary>
        private static readonly float[] DefaultThresholds = { 0.60f, 0.30f };

        private const float DefaultTelegraph = 0.45f;

        private float[] _thresholds = DefaultThresholds;
        private float[] _telegraphs;          // 페이즈별 예고 시간(1-base 를 0-base 로)

        private BossEntry _entry;
        private readonly List<float> _timers = new();

        public int Phase { get; private set; } = 1;
        public BossEntry Entry => _entry;

        /// <summary>지금 예고 중인 행동. null 이면 대기.</summary>
        public BossMove Pending { get; private set; }
        public float TelegraphLeft { get; private set; }
        public bool IsTelegraphing => Pending != null && TelegraphLeft > 0f;

        /// <summary>돌진 남은 시간. 0 보다 크면 이동 대신 돌진 중이다.</summary>
        public float ChargeLeft { get; private set; }
        public Vector2 ChargeDir { get; private set; }

        /// <summary>
        /// 정본 페이즈를 붙인다. 문턱과 예고 시간이 보스마다 다르다 —
        /// 예고 길이는 피할 수 있느냐를 가르는 값이라 한 값으로 뭉뚱그리면 안 된다.
        /// </summary>
        public void SetCanonPhases(IReadOnlyList<float> gates, IReadOnlyList<float> telegraphs)
        {
            _thresholds = gates != null && gates.Count > 0
                ? System.Linq.Enumerable.ToArray(gates) : DefaultThresholds;
            _telegraphs = telegraphs != null && telegraphs.Count > 0
                ? System.Linq.Enumerable.ToArray(telegraphs) : null;
        }

        public void Setup(BossEntry entry)
        {
            _entry = entry;
            Phase = 1;
            Pending = null;
            TelegraphLeft = 0f;
            ChargeLeft = 0f;
            _timers.Clear();
            if (entry == null) return;
            for (int i = 0; i < entry.Moves.Count; i++)
            {
                // 시작하자마자 전탄이 동시에 나가지 않게 초기 쿨다운을 어긋나게 준다
                _timers.Add(entry.Moves[i].Cooldown * (0.35f + 0.25f * i));
            }
        }

        public void UpdatePhase(float hpRatio)
        {
            int p = 1;
            for (int i = 0; i < _thresholds.Length; i++)
                if (hpRatio <= _thresholds[i]) p = i + 2;
            Phase = p;
        }

        /// <summary>페이즈가 오를수록 쿨다운이 짧아진다.</summary>
        private float CooldownOf(BossMove m)
            => m.Cooldown * Mathf.Pow(_entry.PhaseCooldownMul, Phase - 1);

        /// <summary>
        /// 쿨다운을 굴리고, 실행할 행동이 정해지면 예고를 시작한다.
        /// 예고가 끝난 프레임에 그 행동을 돌려준다(그 외에는 null).
        /// </summary>
        public BossMove Tick(float dt)
        {
            if (_entry == null) return null;
            if (ChargeLeft > 0f) ChargeLeft -= dt;

            if (Pending != null)
            {
                TelegraphLeft -= dt;
                if (TelegraphLeft > 0f) return null;
                var ready = Pending;
                Pending = null;
                return ready;
            }

            for (int i = 0; i < _timers.Count; i++)
            {
                var m = _entry.Moves[i];
                if (m.FromPhase > Phase) continue;
                _timers[i] -= dt;
                if (_timers[i] > 0f) continue;

                _timers[i] = CooldownOf(m);
                Pending = m;
                TelegraphLeft = _telegraphs != null && Phase - 1 < _telegraphs.Length
                    ? _telegraphs[Phase - 1] : DefaultTelegraph;
                return null;   // 이번 프레임은 예고만 — 피할 시간을 준다
            }
            return null;
        }

        public void BeginCharge(Vector2 dir, float seconds)
        {
            ChargeDir = dir.sqrMagnitude < 0.0001f ? Vector2.down : dir.normalized;
            ChargeLeft = seconds;
        }
    }
}
