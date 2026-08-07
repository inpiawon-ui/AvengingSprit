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
    /// </summary>
    public sealed class RunBuffs
    {
        private readonly HashSet<string> _taken = new();
        private readonly List<string> _order = new();

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

        public IReadOnlyList<string> Order => _order;
        public int Count => _order.Count;

        public void Clear()
        {
            _taken.Clear();
            _order.Clear();
            ExcludedKeys.Clear();
            AttackMul = IntervalMul = RangeMul = MoveMul = ShotSpeedMul = UltimateChargeMul = 1f;
            GhostHpBonus = ExtraShots = LifestealPercent = SlowPercent = 0;
            Pierce = false;
        }

        /// <summary>버프를 적용한다. 즉발 효과(회복)는 여기서 처리하지 않고 호출부가 맡는다.</summary>
        public void Apply(BuffEntry e)
        {
            if (e == null) return;
            _taken.Add(e.BuffKey);
            _order.Add(e.BuffKey);
            if (!e.Stackable) ExcludedKeys.Add(e.BuffKey);

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
