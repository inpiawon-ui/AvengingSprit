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

        /// <summary>
        /// 실제로 굴리는 공격 목록.
        ///
        /// 기본값은 `BossTable` 의 것이지만, 정본 방이 제 목록을 들고 있으면
        /// 그쪽이 이긴다 — `BossTable` 은 챕터당 하나뿐이라 같은 챕터의
        /// 보스 둘이 똑같이 싸우게 된다.
        /// </summary>
        private IReadOnlyList<BossMove> _moves;

        public int Phase { get; private set; } = 1;
        public BossEntry Entry => _entry;

        /// <summary>지금 예고 중인 행동. null 이면 대기.</summary>
        public BossMove Pending { get; private set; }
        public float TelegraphLeft { get; private set; }

        /// <summary>이번 예고의 전체 길이. 게이지가 얼마나 찼는지 재는 데 쓴다.</summary>
        public float TelegraphTotal { get; private set; }
        public bool IsTelegraphing => Pending != null && TelegraphLeft > 0f;

        /// <summary>예고가 얼마나 찼는가 (0 → 1). 1 에 닿는 순간 터진다.</summary>
        public float TelegraphProgress
            => TelegraphTotal <= 0f ? 1f : 1f - Mathf.Clamp01(TelegraphLeft / TelegraphTotal);

        /// <summary>돌진 남은 시간. 0 보다 크면 이동 대신 돌진 중이다.</summary>
        public float ChargeLeft { get; private set; }
        public Vector2 ChargeDir { get; private set; }

        /// <summary>
        /// 정본 페이즈를 붙인다. 문턱과 예고 시간이 보스마다 다르다 —
        /// 예고 길이는 피할 수 있느냐를 가르는 값이라 한 값으로 뭉뚱그리면 안 된다.
        /// </summary>
        public void SetCanonPhases(IReadOnlyList<float> gates, IReadOnlyList<float> telegraphs)
        {
            // ⚠ 정본 문턱은 `1 / 0.6 / 0.3` 처럼 온다. 앞의 `1` 은 전환점이 아니라
            //   **P1 이 시작되는 지점**이다. 그대로 문턱으로 쓰면 체력이 가득한 순간에도
            //   `1 <= 1` 이 참이라 시작부터 P2 가 되고, 마지막에는 있지도 않은 P4 에 닿는다.
            //   전환점만 남긴다.
            _thresholds = DefaultThresholds;
            if (gates != null && gates.Count > 0)
            {
                _gateBuffer.Clear();
                for (int i = 0; i < gates.Count; i++)
                    if (gates[i] < 1f) _gateBuffer.Add(gates[i]);
                if (_gateBuffer.Count > 0) _thresholds = _gateBuffer.ToArray();
            }

            _telegraphs = telegraphs != null && telegraphs.Count > 0
                ? System.Linq.Enumerable.ToArray(telegraphs) : null;
        }

        private readonly List<float> _gateBuffer = new();

        public void Setup(BossEntry entry)
        {
            _entry = entry;
            Phase = 1;
            Pending = null;
            TelegraphLeft = 0f;
            ChargeLeft = 0f;
            _timers.Clear();
            if (entry == null) return;
            SetMoves(entry.Moves);
        }

        /// <summary>정본 방이 제 공격 목록을 들고 있으면 그것으로 갈아 끼운다.</summary>
        public void SetCanonMoves(IReadOnlyList<BossMove> moves)
        {
            if (moves == null || moves.Count == 0) return;
            SetMoves(moves);
        }

        private void SetMoves(IReadOnlyList<BossMove> moves)
        {
            _moves = moves;
            _timers.Clear();
            // ⚠ 첫 쿨다운을 **제 쿨다운에 비례해서** 주면 안 된다.
            //
            //   예전: `Cooldown * (0.35 + 0.25 * i)`
            //     압착   8s × 0.35 = 2.8s  + 예고 1.25s → 첫 공격이 4.05초
            //     파괴구 11s × 0.60 = 6.6s + 예고 1.50s → 둘째가 8.1초
            //
            //   그런데 첫 보스는 그 전에 죽는다. **보스가 한 대도 못 치고 끝난다** —
            //   실제로 "공격 한 번도 못 하고 맞기만 하다 끝났다" 는 보고가 왔다.
            //   쿨다운이 긴 패턴일수록 첫 등장이 늦어지는 것도 거꾸로다.
            //   느린 패턴일수록 한 번은 보여 줘야 무엇인지 배울 수 있다.
            //
            //   지금: 첫 것은 곧바로, 나머지는 일정한 간격으로 벌린다.
            //   쿨다운 길이와 무관하므로 어떤 보스든 **1초 안에 첫 예고가 뜬다.**
            const float FirstMove = 1.0f;
            const float MoveGap = 1.8f;
            for (int i = 0; i < moves.Count; i++)
                _timers.Add(FirstMove + MoveGap * i);
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
            => m.Cooldown * Mathf.Pow(_entry.PhaseCooldownMul, Phase - 1)
               * BattleDirector.BossCooldownMul;   // 테스트 스위치. 평소엔 1

        /// <summary>
        /// 쿨다운을 굴리고, 실행할 행동이 정해지면 예고를 시작한다.
        /// 예고가 끝난 프레임에 그 행동을 돌려준다(그 외에는 null).
        /// </summary>
        public BossMove Tick(float dt)
        {
            if (_entry == null || _moves == null) return null;
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
                var m = _moves[i];
                if (m.FromPhase > Phase) continue;
                _timers[i] -= dt;
                if (_timers[i] > 0f) continue;

                _timers[i] = CooldownOf(m);
                Pending = m;
                // ⚠ **패턴이 제 예고 시간을 들고 있으면 그것이 이긴다.**
                //   페이즈 하나로 뭉뚱그리면 24개가 전부 같은 길이로 예고한다 —
                //   예고 길이는 피할 수 있느냐를 가르는 값이라 패턴마다 달라야 한다.
                //   킹핀 「처형 표식」 1.8초가 24개 중 가장 길고, 그 길이 자체가
                //   "몸을 갈아탈 시간을 준다" 는 뜻이다.
                TelegraphLeft = m.HasTelegraph ? m.TelegraphSeconds
                    : _telegraphs != null && Phase - 1 < _telegraphs.Length
                        ? _telegraphs[Phase - 1] : DefaultTelegraph;
                TelegraphTotal = TelegraphLeft;
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
