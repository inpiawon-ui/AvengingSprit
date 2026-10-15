using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 적 호스트 액티브의 바닥 예고 (PD 2026-10-15 「적으로 나온 호스트가 액티브를 쓰면 피할 수 있어야 한다 ·
    /// 예고제로 · 너무 과하지 않지만 잘 보이게」). 필살기는 적이 쓰지 않는다.
    ///
    /// 쓰기 전에 맞을 범위를 바닥에 그리고(보스 패턴과 같은 `DangerView` · `DangerShape` — 그린 것 = 맞는 것),
    /// 다 차면 그 범위만 친다. 범위 밖으로 나가면 안 맞는다.
    ///
    /// | 무리 | 몸 | 예고 | 맞는 것 |
    /// |---|---|---|---|
    /// | 부채꼴 | 드라군 · 청룡 · 샐러맨더 · 코만도(수류탄) | 부채꼴 | 부채꼴 안 |
    /// | 사방 | 아마존 정예 · 사신 · 설녀 · 화이트 위저드 | 원 | 원 안 |
    /// | 관통 | 영매 · 코만도(레이저 · 미사일) | 화면 끝까지 띠 | 띠를 따라 나가는 탄 |
    /// | 돌진 | 아마존 · 닌자 · 닌자(사슬) · 호퍼 | 짧은 띠 | 띠 안(돌진 길) |
    /// | 그 밖 | 나머지 | 화면 끝까지 띠 | 띠를 따라 나가는 연사 |
    ///
    /// 겨냥은 예고를 띄우는 순간 한 번만 한다 — 다시 겨누면 피하는 것이 답이 아니게 된다.
    /// 위력은 그 몸의 공격력(챕터 배율이 이미 들어 있다) × 무리별 배율.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private enum EnemyTellKind { Wedge, Radial, Line, Dash, Burst }

        // 예고 시간 — 보스(0.8 · 대장)보다 짧고 평타 예고보다 길다. 보이고 비킬 수 있는 만큼
        private const float EnemySkillTellSeconds = 0.7f;
        // 보스 예고보다 옅게(「과하지 않게」) — 같은 그림 · 같은 판정, 진하기만 낮춘다
        private const float EnemyTellStrength = 0.75f;

        // 크기(미터) — 대장은 키운다
        private const float TellWedgeRadius = 3.6f, TellWedgeRadiusBoss = 5f;
        private const float TellWedgeDeg = 55f, TellWedgeDegBoss = 80f;
        private const float TellRadialRadius = 2.4f, TellRadialRadiusBoss = 3.2f;
        private const float TellLineLength = 7f, TellLineWidth = 0.9f;
        private const float TellDashLength = 3f, TellDashLengthBoss = 4.5f, TellDashWidth = 1.3f;

        // 위력 배율 — 범위 한 번으로 치는 것(부채꼴 · 사방 · 돌진)은 예전 탄 여러 발 몫을 한 대에 담는다
        private const float TellWedgeDamageMul = 1.2f;
        private const float TellRadialDamageMul = 1f;
        private const float TellDashDamageMul = 1f;

        private DangerView _enemyTellView;
        private DangerShape _enemyTell;
        private EnemyTellKind _enemyTellKind;
        private float _enemyTellTotal;

        private static EnemyTellKind TellKindOf(string hostKey)
        {
            switch (hostKey)
            {
                case "dragoon":
                case "dragon_blue":
                case "salamander":
                case "commando_grenade":
                    return EnemyTellKind.Wedge;
                case "amazon_elite":
                case "death":
                case "snowwoman":
                case "white_wizard":
                    return EnemyTellKind.Radial;
                case "medium":
                case "commando_laser":
                case "commando_missile":
                    return EnemyTellKind.Line;
                case "amazon":
                case "ninja_chain":
                case "hopper":
                case "ninja":
                    return EnemyTellKind.Dash;
                default:
                    return EnemyTellKind.Burst;
            }
        }

        /// <summary>예고를 띄운다 — 겨냥을 여기서 못 박는다. 돌려주는 값이 예고 시간이다.</summary>
        private float BeginEnemyTell(Unit e, Unit me, bool boss)
        {
            var dir = me.Position - e.Position;
            dir = dir.sqrMagnitude < 0.0001f ? e.Facing : dir.normalized;
            e.SetFacing(dir);
            e.PatternTo = me.Position;
            _enemyTellKind = TellKindOf(e.Key);

            var s = new DangerShape { Origin = e.Position, Dir = dir };
            switch (_enemyTellKind)
            {
                case EnemyTellKind.Wedge:
                    s.Shape = DangerShape.Kind.Wedge;
                    s.Radius = Meters(boss ? TellWedgeRadiusBoss : TellWedgeRadius);
                    s.Degrees = boss ? TellWedgeDegBoss : TellWedgeDeg;
                    break;
                case EnemyTellKind.Radial:
                    s.Shape = DangerShape.Kind.Disc;
                    s.Radius = Meters(boss ? TellRadialRadiusBoss : TellRadialRadius);
                    break;
                case EnemyTellKind.Dash:
                    s.Shape = DangerShape.Kind.Band;
                    s.Length = Meters(boss ? TellDashLengthBoss : TellDashLength);
                    s.Width = Meters(TellDashWidth);
                    break;
                default:
                    s.Shape = DangerShape.Kind.Band;
                    // 관통 · 연사 탄은 화면 끝까지 날아간다(`FireFan` 사거리 = 방 대각선) — 띠도 끝까지 그린다.
                    //   7m 로 끊었더니 띠 끝 너머 같은 줄에 서 있으면 예고 밖인데 맞았다(2026-10-16 점검)
                    s.Length = Mathf.Max(Meters(TellLineLength), _roomSize.magnitude);
                    s.Width = Meters(TellLineWidth);
                    break;
            }
            _enemyTell = s;

            if (_enemyTellView == null && _fieldLayer != null)
                _enemyTellView = DangerView.Create(_fieldLayer);
            if (_enemyTellView != null)
            {
                _enemyTellView.Strength = EnemyTellStrength;
                _enemyTellView.Show(_enemyTell, _roomSize, GetSprite("fx_danger_hatch"), safe: false);
            }
            _enemyTellTotal = boss ? MidBossSkillTelegraph : EnemySkillTellSeconds;
            return _enemyTellTotal;
        }

        /// <summary>예고가 차오른다 — 남은 시간으로 진행도를 넘긴다.</summary>
        private void TickEnemyTell(float remaining, float dt)
        {
            if (_enemyTellView == null || _enemyTellTotal <= 0f) return;
            _enemyTellView.Tick(dt, 1f - Mathf.Clamp01(remaining / _enemyTellTotal));
        }

        /// <summary>예고를 거둔다. 터져서 거두면 한 번 번쩍인다(`DangerView.Hide`).</summary>
        private void EndEnemyTell()
        {
            if (_enemyTellView != null) _enemyTellView.Hide();
            _enemyTell = default;
        }

        /// <summary>예고한 범위 안에 내가 있는가 — 그린 것과 같은 도형으로 잰다.</summary>
        private bool InEnemyTell(Unit me)
            => me != null && !_enemyTell.IsNone && _enemyTell.Contains(me.Position, _roomSize);
    }
}
