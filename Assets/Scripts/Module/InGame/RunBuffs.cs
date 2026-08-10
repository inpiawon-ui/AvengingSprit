using System.Collections.Generic;
using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 한 런 동안 쌓인 버프의 누적 결과.
    ///
    /// 유닛 스탯을 직접 고치지 않고 **발사 시점에 곱해서 쓴다.** 유닛을 갈아끼우는
    /// 게임(빙의)이라 스탯을 덮어쓰면 호스트가 바뀔 때마다 버프를 다시 입혀야 하고,
    /// 그 과정에서 원본 수치를 잃기 쉽다.
    ///
    /// 태그형·호스트 전용 버프는 **지금 쓰는 몸에 따라 켜지고 꺼진다**(기획서 A 5-4).
    /// 그래서 값을 더해 두는 대신 획득 목록을 들고 있다가 호스트가 바뀔 때마다
    /// 처음부터 다시 센다. 껐다 켜는 것을 뺄셈으로 되돌리면 부동소수 오차가 남고,
    /// 곱셈·클램프가 섞인 항목(간격·둔화)은 역연산 자체가 성립하지 않는다.
    /// </summary>
    public sealed class RunBuffs
    {
        private readonly List<BuffEntry> _taken = new();
        private HostEntry _host;

        public float AttackMul { get; private set; } = 1f;
        public float IntervalMul { get; private set; } = 1f;
        public float RangeMul { get; private set; } = 1f;
        public float MoveMul { get; private set; } = 1f;
        public float ShotSpeedMul { get; private set; } = 1f;
        public float UltimateChargeMul { get; private set; } = 1f;
        public int GhostHpBonus { get; private set; }
        public int ExtraShots { get; private set; }
        public int LifestealPercent { get; private set; }
        public int SlowPercent { get; private set; }
        public bool Pierce { get; private set; }

        /// <summary>중복 불가 버프의 키 모음. 다음 뽑기에서 제외한다.</summary>
        public HashSet<string> ExcludedKeys { get; } = new();

        public int Count => _taken.Count;

        /// <summary>지금 켜져 있는 버프 수. 꺼진 것(안 맞는 몸)은 세지 않는다.</summary>
        public int ActiveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _taken.Count; i++)
                    if (_taken[i].IsActiveFor(_host)) n++;
                return n;
            }
        }

        public void Clear()
        {
            _taken.Clear();
            ExcludedKeys.Clear();
            _host = null;
            Recompute();
        }

        /// <summary>
        /// 쓰는 몸이 바뀌었다. 태그형·전용 버프의 온오프가 여기서 갈린다.
        /// 호스트를 잃으면 null 로 부른다 — 유령 상태에서는 범용만 남는다.
        /// </summary>
        public void SetHost(HostEntry host)
        {
            if (ReferenceEquals(_host, host)) return;
            _host = host;
            Recompute();
        }

        /// <summary>버프를 얻는다. 즉발 효과(회복)는 여기서 처리하지 않고 호출부가 맡는다.</summary>
        public void Apply(BuffEntry e)
        {
            if (e == null) return;
            _taken.Add(e);
            if (!e.Stackable) ExcludedKeys.Add(e.BuffKey);
            Recompute();
        }

        private void Recompute()
        {
            AttackMul = IntervalMul = RangeMul = MoveMul = ShotSpeedMul = UltimateChargeMul = 1f;
            GhostHpBonus = ExtraShots = LifestealPercent = SlowPercent = 0;
            Pierce = false;

            for (int i = 0; i < _taken.Count; i++)
            {
                var e = _taken[i];
                if (!e.IsActiveFor(_host)) continue;

                float v = e.Value / 100f;
                switch (e.Kind)
                {
                    case BuffKind.Attack:         AttackMul += v; break;
                    // 간격은 줄어야 빨라진다. 0 이하로 내려가지 않게 막는다.
                    case BuffKind.AttackSpeed:    IntervalMul = Mathf.Max(0.2f, IntervalMul - v); break;
                    case BuffKind.Range:          RangeMul += v; break;
                    case BuffKind.MoveSpeed:      MoveMul += v; break;
                    case BuffKind.ShotSpeed:      ShotSpeedMul += v; break;
                    case BuffKind.UltimateCharge: UltimateChargeMul += v; break;
                    case BuffKind.GhostHp:        GhostHpBonus += e.Value; break;
                    case BuffKind.MultiShot:      ExtraShots += e.Value; break;
                    case BuffKind.Lifesteal:      LifestealPercent += e.Value; break;
                    case BuffKind.Slow:           SlowPercent = Mathf.Min(80, SlowPercent + e.Value); break;
                    case BuffKind.Pierce:         Pierce = true; break;
                    case BuffKind.Heal:           break;   // 즉발 — BattleDirector 가 처리
                }
            }
        }
    }
}
