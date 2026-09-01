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
        private readonly Dictionary<string, int> _level = new();
        private HostEntry _host;

        // ── 빌드 슬롯 (정본 v2.3 BUILD_SLOT_INITIAL = 8) ─────────
        //
        // 서로 다른 카드는 8종까지만 갖는다. **같은 카드는 슬롯을 더 쓰지 않는다** —
        // 레벨이 오를 뿐이다. 이 제한이 없으면 다 주워 담게 되고,
        // "무엇을 포기할까" 가 사라져 빌드가 성립하지 않는다.
        public const int BuildSlots = 8;

        /// <summary>지금 쓰는 슬롯 수 = 서로 다른 카드 수.</summary>
        public int SlotsUsed => _level.Count;

        public bool SlotsFull => _level.Count >= BuildSlots;

        /// <summary>이 카드의 레벨. 없으면 0.</summary>
        public int LevelOf(string cardKey)
            => cardKey != null && _level.TryGetValue(cardKey, out int lv) ? lv : 0;

        /// <summary>지금 들고 있는 카드 목록(레벨 포함). UI 가 읽는다.</summary>
        public IReadOnlyDictionary<string, int> Levels => _level;

        public float AttackMul { get; private set; } = 1f;
        public float IntervalMul { get; private set; } = 1f;
        public float RangeMul { get; private set; } = 1f;
        public float MoveMul { get; private set; } = 1f;
        public float ShotSpeedMul { get; private set; } = 1f;
        public float ActiveSkillChargeMul { get; private set; } = 1f;
        public int GhostHpBonus { get; private set; }

        // ── 정본 BUFF_DB 에서 온 것들 ────────────────────────────
        /// <summary>호스트 최대 체력 배율 (BUF_U01)</summary>
        public float HostHpMul { get; private set; } = 1f;
        /// <summary>정지 → 발사 지연에서 깎을 초 (BUF_U03)</summary>
        public float StopDelayCut { get; private set; }
        /// <summary>전술 빙의 비용에서 깎을 값 (BUF_U06)</summary>
        /// <summary>받는 피해 배율. 여러 장 겹쳐도 0 이 되지 않게 곱으로 쌓는다 (BUF_A04)</summary>
        public float DamageTakenMul { get; private set; } = 1f;
        /// <summary>전술 빙의 직후 추가 무적 초 (BUF_A06)</summary>
        public float SwitchShieldSeconds { get; private set; }
        /// <summary>
        /// 보조 탄체 수. 정본 C007 `추가 발사` 의 **레벨**이 곧 발수다.
        ///
        /// ⚠ 정본 레벨표의 값(12·18·24·30·36)은 쓰지 않는다.
        ///   그 값을 발수로 읽으면 카드 한 장에 12발이 붙어 화면이 터지고,
        ///   퍼센트(확률)로 읽으면 **먹었는데 안 나가는 판**이 생겨 카드가 아니라 사고로 읽힌다.
        ///   "한 장 = 한 발" 이 화면에서 유일하게 읽히는 규칙이다.
        ///     1레벨 +1발 (기본 1발이면 2발) … 5레벨 +5발
        /// </summary>
        public int ExtraShots { get; private set; }

        public int LifestealPercent { get; private set; }
        public int SlowPercent { get; private set; }

        /// <summary>화상 3단계가 주변으로 옮는가 (정본 BUF_T02)</summary>
        public bool BurnSpreads { get; private set; }

        /// <summary>같은 적 연속 명중 1단계당 피해 증가분. 0 이면 이 버프가 없다 (정본 BUF_U04)</summary>
        public float FocusPerStack { get; private set; }

        /// <summary>장판이 더 오래 남는다 (정본 BUF_A02)</summary>
        public float FieldExtraSeconds { get; private set; }

        /// <summary>둔화 장판 가장자리 피해 (정본 BUF_T03)</summary>
        public int SlowFieldEdgeDamage { get; private set; }

        /// <summary>지뢰가 빙결 룬이 된다 (정본 BUF_S04)</summary>
        public bool MinesFreeze { get; private set; }

        /// <summary>탄이 벽에서 튕기는 횟수 (정본 BUF_T05)</summary>
        public int Bounces { get; private set; }

        /// <summary>튕긴 탄의 피해 비율 % (정본 BUF_A03). 0 이면 이 버프가 없다</summary>
        public int ReturnDamagePercent { get; private set; }

        /// <summary>범위 효과 반경 배수 (정본 BUF_U05)</summary>
        public float AoeMul { get; private set; } = 1f;

        /// <summary>표식 폭발이 더 번지는 수 (정본 BUF_T01)</summary>
        public int MarkPayload { get; private set; }

        /// <summary>흡혈 초과분이 고스트로 가는 방당 횟수 (정본 BUF_T04)</summary>
        public int BloodDebtPerRoom { get; private set; }

        /// <summary>확산 마지막 탄의 추가 피해 비율 (정본 BUF_A01)</summary>
        public float LastShotBonus { get; private set; }

        /// <summary>포탑 발사 간격 감소 비율 (정본 BUF_T06)</summary>
        public float DeployRetargetCut { get; private set; }

        /// <summary>포탑이 불을 물려받는가 (정본 BUF_S01)</summary>
        public bool DeployablesBurn { get; private set; }
        public bool Pierce { get; private set; }

        // ── 정본 v2.3 카드 ───────────────────────────────────────
        /// <summary>C002 정밀 조준 — 유효 타깃이 하나뿐일 때의 추가 피해 비율</summary>
        public float SingleTargetBonus { get; private set; }
        /// <summary>C003 마무리 본능 — 체력이 낮은 적에게 붙는 추가 피해 비율</summary>
        public float ExecuteBonus { get; private set; }
        /// <summary>C015 보스 압축 — 보스에게 붙는 추가 피해 비율</summary>
        public float BossBonus { get; private set; }
        /// <summary>C017 생명 회수 — 적을 잡을 때마다 회복할 최대 체력 비율</summary>
        public int RegenPercentPerKill { get; private set; }
        /// <summary>C024 전투 스텝 — 공격 직후 1.2초 동안 붙는 이동 속도 비율</summary>
        public float CombatStepBonus { get; private set; }
        /// <summary>C025·C026·C027 각인 — 기본 공격에 상태이상이 붙을 확률(%)</summary>
        public int FrostImprint { get; private set; }
        public int FlameImprint { get; private set; }
        public int CurseImprint { get; private set; }
        /// <summary>C005 연속 압박 — 같은 적 4타째에 붙는 최대 추가 피해 비율</summary>
        public float SustainBonus { get; private set; }
        /// <summary>C009 유도 보정 — 탄이 대상 쪽으로 꺾이는 세기(0 이면 직진)</summary>
        public float HomingStrength { get; private set; }
        /// <summary>C032 영혼 복제 — 8타마다 복제되는 공격의 피해 비율</summary>
        public float EchoPercent { get; private set; }
        /// <summary>C013 폭발 메아리 — 폭발 뒤 2차 충격의 피해 비율(0 이면 없음)</summary>
        public float ExplosiveEchoPercent { get; private set; }
        /// <summary>C030 유령 포대 — 포대 한 발의 피해 비율(공격력 대비, 0 이면 없음)</summary>
        public float GhostTurretPercent { get; private set; }
        /// <summary>C004 갑옷 분쇄 — 중첩 한 겹마다 대상이 더 받는 피해 비율</summary>
        public float ArmorBreakPerStack { get; private set; }
        /// <summary>C014 연쇄 번짐 — 상태이상이 옆으로 번질 확률(%)</summary>
        public int StatusChainPercent { get; private set; }
        /// <summary>C018 위기 방벽 — 방벽이 막아 주는 최대 체력 비율</summary>
        public float CrisisBarrierPercent { get; private set; }
        /// <summary>C023 회피 잔상 — 잔상 한 발의 피해 비율(공격력 대비)</summary>
        public float AfterimagePercent { get; private set; }
        /// <summary>C031 과충전 회로 — 전기 한 방의 피해 비율(그 타격 대비)</summary>
        public float OverchargePercent { get; private set; }

        /// <summary>중복 불가 버프의 키 모음. 다음 뽑기에서 제외한다.</summary>
        public HashSet<string> ExcludedKeys { get; } = new();

        public int Count => _taken.Count;

        /// <summary>이 버프를 갖고 있는가.</summary>
        public bool Has(string buffKey)
        {
            for (int i = 0; i < _taken.Count; i++)
                if (_taken[i].BuffKey == buffKey) return true;
            return false;
        }

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
            _level.Clear();
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

        /// <summary>
        /// 카드를 얻는다. 즉발 효과(회복)는 여기서 처리하지 않고 호출부가 맡는다.
        ///
        /// 이미 가진 카드면 **레벨이 오른다**(최대 5). 새 카드면 슬롯을 하나 쓴다.
        /// 최대 레벨에 닿은 카드는 다음 뽑기에서 뺀다 — 더 올릴 수 없는 것을
        /// 3택1 에 넣으면 그 칸이 버려진다.
        /// </summary>
        public void Apply(BuffEntry e)
        {
            if (e == null) return;

            int lv = LevelOf(e.BuffKey);
            if (lv == 0) _taken.Add(e);              // 새 카드만 목록에 넣는다
            lv = Mathf.Min(lv + 1, e.MaxLevel);
            _level[e.BuffKey] = lv;

            if (lv >= e.MaxLevel || !e.Stackable) ExcludedKeys.Add(e.BuffKey);
            Recompute();
        }

        private void Recompute()
        {
            AttackMul = IntervalMul = RangeMul = MoveMul = ShotSpeedMul = ActiveSkillChargeMul = 1f;
            GhostHpBonus = ExtraShots = LifestealPercent = SlowPercent = 0;
            Pierce = false;
            HostHpMul = DamageTakenMul = 1f;
            StopDelayCut = SwitchShieldSeconds = 0f;
            BurnSpreads = MinesFreeze = false;
            FocusPerStack = FieldExtraSeconds = LastShotBonus = 0f;
            SlowFieldEdgeDamage = Bounces = ReturnDamagePercent = 0;
            MarkPayload = BloodDebtPerRoom = 0;
            SingleTargetBonus = ExecuteBonus = BossBonus = CombatStepBonus = 0f;
            RegenPercentPerKill = FrostImprint = FlameImprint = CurseImprint = 0;
            SustainBonus = HomingStrength = EchoPercent = 0f;
            ExplosiveEchoPercent = GhostTurretPercent = 0f;
            ArmorBreakPerStack = CrisisBarrierPercent = 0f;
            AfterimagePercent = OverchargePercent = 0f;
            StatusChainPercent = 0;
            DeployRetargetCut = 0f;
            DeployablesBurn = false;
            AoeMul = 1f;

            for (int i = 0; i < _taken.Count; i++)
            {
                var e = _taken[i];
                if (!e.IsActiveFor(_host)) continue;

                // 카드는 레벨마다 수치가 다르다. 중첩 합산이 아니라 **레벨표를 읽는다.**
                int value = e.ValueAt(LevelOf(e.BuffKey));
                float v = value / 100f;
                switch (e.Kind)
                {
                    case BuffKind.Attack:         AttackMul += v; break;
                    // 간격은 줄어야 빨라진다. 0 이하로 내려가지 않게 막는다.
                    case BuffKind.AttackSpeed:    IntervalMul = Mathf.Max(0.2f, IntervalMul - v); break;
                    case BuffKind.Range:          RangeMul += v; break;
                    case BuffKind.MoveSpeed:      MoveMul += v; break;
                    case BuffKind.ShotSpeed:      ShotSpeedMul += v; break;
                    case BuffKind.ActiveSkillCharge: ActiveSkillChargeMul += v; break;
                    case BuffKind.GhostHp:        GhostHpBonus += value; break;
                    // 값이 아니라 **레벨**을 쓴다. 한 장 = 한 발.
                    case BuffKind.MultiShot:      ExtraShots += LevelOf(e.BuffKey); break;
                    case BuffKind.Lifesteal:      LifestealPercent += value; break;
                    case BuffKind.Slow:           SlowPercent = Mathf.Min(80, SlowPercent + value); break;
                    case BuffKind.Pierce:         Pierce = true; break;
                    case BuffKind.Heal:           break;   // 즉발 — BattleDirector 가 처리

                    // 정본 BUFF_DB 에서 온 것들
                    case BuffKind.HostMaxHp:       HostHpMul += v; break;
                    case BuffKind.StopDelay:       StopDelayCut += value / 100f; break;
                    // 여러 장 겹쳐도 무적이 되지 않게 곱으로 쌓는다
                    case BuffKind.DamageReduction: DamageTakenMul *= 1f - v; break;
                    case BuffKind.SwitchShield:    SwitchShieldSeconds += value / 100f; break;
                    case BuffKind.BurnSpread:      BurnSpreads = true; break;
                    case BuffKind.FocusedSoul:     FocusPerStack += value / 100f; break;
                    case BuffKind.FieldDuration:   FieldExtraSeconds += value / 10f; break;
                    case BuffKind.SlowFieldEdge:   SlowFieldEdgeDamage += value; break;
                    case BuffKind.FreezeRune:      MinesFreeze = true; break;
                    case BuffKind.Ricochet:        Bounces += value; break;
                    case BuffKind.ReturnDamage:    ReturnDamagePercent = Mathf.Max(ReturnDamagePercent, value); break;
                    case BuffKind.AoeRadius:       AoeMul += v; break;
                    case BuffKind.MarkPayload:     MarkPayload += value; break;
                    case BuffKind.BloodDebt:       BloodDebtPerRoom += value; break;
                    case BuffKind.LastShot:        LastShotBonus += v; break;
                    case BuffKind.DeployRetarget:  DeployRetargetCut = Mathf.Min(0.7f, DeployRetargetCut + v); break;
                    case BuffKind.DeployFire:      DeployablesBurn = true; break;

                    // ── 정본 v2.3 카드 ───────────────────────────
                    case BuffKind.SingleTarget:    SingleTargetBonus += v; break;
                    case BuffKind.Execute:         ExecuteBonus += v; break;
                    case BuffKind.BossFocus:       BossBonus += v; break;
                    case BuffKind.Regen:           RegenPercentPerKill += value; break;
                    case BuffKind.CombatStep:      CombatStepBonus += v; break;
                    case BuffKind.FrostImprint:    FrostImprint += value; break;
                    case BuffKind.FlameImprint:    FlameImprint += value; break;
                    case BuffKind.CurseImprint:    CurseImprint += value; break;
                    case BuffKind.SustainStack:    SustainBonus += v; break;
                    case BuffKind.Homing:          HomingStrength += v; break;
                    case BuffKind.SpectralEcho:    EchoPercent += v; break;
                    case BuffKind.ExplosiveEcho:   ExplosiveEchoPercent += v; break;
                    case BuffKind.GhostTurret:     GhostTurretPercent += v; break;
                    case BuffKind.ArmorBreak:      ArmorBreakPerStack += v; break;
                    case BuffKind.StatusChain:     StatusChainPercent += value; break;
                    case BuffKind.CrisisBarrier:   CrisisBarrierPercent += v; break;
                    case BuffKind.Afterimage:      AfterimagePercent += v; break;
                    case BuffKind.Overcharge:      OverchargePercent += v; break;
                }
            }
        }
    }
}
