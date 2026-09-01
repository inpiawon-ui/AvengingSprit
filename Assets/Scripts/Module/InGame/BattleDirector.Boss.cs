using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 보스 예고 — 바닥에 그리고, 그 안을 친다.
    ///
    /// ── 순서 ────────────────────────────────────────────────────
    ///   1  두뇌가 다음 패턴을 고른다           `BossBrain.Tick` → `Pending`
    ///   2  **여기서 도형을 한 번 굳힌다**       `BeginDanger`
    ///   3  예고 동안 그 도형을 바닥에 그린다     `TickDanger`
    ///   4  예고가 끝나면 **같은 도형**으로 친다  `StrikeDanger`
    ///
    /// ⚠ **2 에서 굳히는 것이 핵심이다.** 발동 순간에 다시 계산하면
    ///   그 사이 보스가 돌거나 플레이어가 움직인 만큼 도형이 달라진다 —
    ///   그리는 동안 조금씩 옮겨 다니는 위험 구역이 되어, 예고를 보고 피한 자리가
    ///   맞는 자리가 된다. 한 번 그린 것은 끝까지 그 자리다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private DangerView _dangerView;
        private DangerView _safeView;

        private DangerShape _danger;      // 지금 예고 중인 도형. 굳어 있다
        private BossMove _dangerMove;
        private int _dangerTick;          // 회전하는 것(틈·줄·분면·섬)의 차례

        /// <summary>예고 도형이 떠 있는가.</summary>
        private bool HasDanger => _dangerMove != null && !_danger.IsNone;

        private void EnsureDangerViews()
        {
            if (_dangerView == null && _fieldLayer != null)
                _dangerView = DangerView.Create(_fieldLayer);
            if (_safeView == null && _fieldLayer != null)
                _safeView = DangerView.Create(_fieldLayer);
        }

        /// <summary>
        /// 예고를 시작한다. 도형을 여기서 한 번 정하고 끝까지 안 바꾼다.
        /// </summary>
        private void BeginDanger(Unit boss, Unit me, BossMove m)
        {
            ClearDanger();
            if (boss == null || m == null || !m.HasShape) return;

            EnsureDangerViews();
            if (_dangerView == null) return;

            var dir = me != null ? (me.Position - boss.Position) : boss.Facing;
            _danger = DangerShape.From(m, boss.Position, dir,
                                       me != null ? me.Position : boss.Position,
                                       _roomSize, _pxPerMeter, _dangerTick);
            if (_danger.IsNone) return;

            _dangerMove = m;
            _dangerView.Show(_danger, _roomSize, GetSprite("fx_danger_hatch"), safe: false);

            // 안전지대는 **위험을 그린 다음**에 그린다. 위험만 있으면 "저기 맞겠네" 지만,
            // 안전이 같이 보이면 "저기로 가면 되네" 가 된다 — 훨씬 빨리 읽힌다.
            ShowSafeZone(m, boss);
        }

        /// <summary>
        /// 안전지대. 기획이 좌표를 적어 둔 패턴만 그린다 —
        /// 없는데 억지로 그리면 "저기는 안전하다" 는 거짓말이 된다.
        /// </summary>
        private void ShowSafeZone(BossMove m, Unit boss)
        {
            if (_safeView == null) return;
            if (!m.HasSafeSpot) { _safeView.Hide(); return; }

            var at = new Vector2(m.SafeAtMeters.x * _pxPerMeter,
                                 -m.SafeAtMeters.y * _pxPerMeter);
            var safe = new DangerShape
            {
                Shape = DangerShape.Kind.Disc,
                Origin = at,
                Radius = Mathf.Max(_pxPerMeter, _pxPerMeter * 1.2f),
            };
            _safeView.Show(safe, _roomSize, GetSprite("fx_safe_hatch"), safe: true);
        }

        private void TickDanger(float dt)
        {
            if (_dangerView == null) return;
            float p = _brain != null ? _brain.TelegraphProgress : 1f;
            _dangerView.Tick(dt, p);
            if (_safeView != null) _safeView.Tick(dt, p);
        }

        private void ClearDanger()
        {
            _danger = default;
            _dangerMove = null;
            if (_dangerView != null) _dangerView.Hide();
            if (_safeView != null) _safeView.Hide();
        }

        /// <summary>
        /// 예고가 끝났다. **그려 둔 그 도형**으로 친다.
        ///
        /// 도형이 없는 패턴(아직 옛 8패턴으로 도는 것)은 <c>false</c> —
        /// 부르는 쪽이 예전 경로로 넘긴다.
        /// </summary>
        private bool StrikeDanger(Unit boss, Unit me, BossMove m)
        {
            if (!HasDanger || _dangerMove != m) { ClearDanger(); return false; }

            int dmg = Mathf.RoundToInt(boss.Atk * m.DamageMul);

            // ⚠ 판정은 **그린 것과 같은 함수**다. 여기서 반경을 조금 키우거나
            //   "관대하게" 만들지 마라 — 그 순간 그림과 판정이 갈라진다.
            bool playerHit = me != null && _danger.Contains(me.Position, _roomSize);
            if (playerHit) DamagePlayer(dmg);

            // 같은 도형으로 적도 맞는다. 「자기 유도 로켓」이 다른 머리를 때리는 것이
            // 이 게임 유일한 "보스가 보스를 때리는" 구간이다.
            if (m.Draw == BossDraw.Homing)
                for (int i = _enemies.Count - 1; i >= 0; i--)
                {
                    var e = _enemies[i];
                    if (e == null || !e.IsAlive || e == boss) continue;
                    if (!_danger.Contains(e.Position, _roomSize)) continue;
                    HitEnemyWith(e, dmg, null);
                    _homingHitAlly = true;
                }

            // 가디언 「반사선」은 때리는 패턴이 아니라 **4초짜리 상태**다.
            // 도는 동안 정면으로 들어온 내 탄이 2배로 돌아온다.
            if (m.Draw == BossDraw.Arc && m.Shape == BossShape.Line)
                _reflectLineLeft = ReflectLineSeconds;

            PlayDangerImpact(m);
            // 무엇을 했느냐에 따라 취약 창이 열린다. 그냥 피한 것만으로는 안 열리는 보스가 있다.
            CheckBreak(boss, m, playerHit);

            // 다음에 같은 패턴이 나오면 틈·줄·분면·섬이 한 칸 돌아간다.
            _dangerTick++;
            ClearDanger();
            return true;
        }

        /// <summary>도형이 터진 자리에 표시를 남긴다. 무엇이 지나갔는지 보여야 한다.</summary>
        private void PlayDangerImpact(BossMove m)
        {
            string fx = m.Shape switch
            {
                BossShape.Arc => "burst",
                BossShape.Dash => "slam",
                BossShape.Mark => "burst",
                BossShape.Zone => m.Draw == BossDraw.Trail || m.Draw == BossDraw.Split
                                ? "lava" : "shatter",
                _ => "burst",
            };
            float size = Mathf.Max(96f, _danger.Radius > 0f ? _danger.Radius : _danger.Width);
            PlayFx(fx, _danger.Origin, size, loop: false);
        }
    }
}
