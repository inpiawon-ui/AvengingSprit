using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 상점에서 사는 **동료 호스트** (2026-09-10).
    ///
    /// ── 미끼를 왜 걷어냈나 ──────────────────────────────────
    /// 예전 소모품 「미끼」는 적 하나를 굳혀 세워 두고 나머지가 그것을 쫓게 했다.
    /// 화면에서는 적 하나가 갑자기 멈춰 서고 나머지가 그리로 몰려가는 것뿐이라
    /// **무슨 일이 난 건지 안 읽혔다**(「상점에서 산 미끼는 뭔가 이상해」).
    ///
    /// 그 자리에 **같이 싸우는 몸 하나**를 세운다. 무엇을 샀는지가 한눈에 보인다.
    ///
    /// ── 어떻게 진짜 아군이 되는가 ───────────────────────────
    /// 적의 공격은 `PerformAttack(..., fromPlayer: false)` 로 **플레이어에게** 가도록
    /// 짜여 있다. 그래서 적의 편만 바꾸면 자기 편을 때리는 시늉만 하고 피해는
    /// 나에게 온다 — 예전 미끼가 아군이 아니었던 이유가 이것이다.
    ///
    /// 동료는 적 목록(`_enemies`)에 넣지 않고 **새로 세운다.** 공격은
    /// `fromPlayer: true` 로 내보내므로 피해가 적에게 간다.
    ///
    /// ⚠ `fromPlayer: true` 라서 내 카드 효과(연쇄·화상·추가 발사 등)가 동료의
    ///   공격에도 실린다. 「내가 키운 것이 같이 싸운다」로 읽히므로 그대로 둔다.
    ///
    /// ── 죽으면 사라진다 ────────────────────────────────────
    /// 동료가 살아 있는 동안 **내가 맞을 피해를 대신 받는다**(`SoakWithAlly`).
    /// 적의 조준을 따로 옮기지 않고도 「앞에 서 주는 몸」이 되고, 체력이 다하면
    /// 사라진다 — 산 것이 언제 없어졌는지가 화면에 분명하다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>동료 체력. 내 최대 체력의 이만큼(%)으로 선다.</summary>
        private const int AllyHpPercent = 60;

        /// <summary>동료 공격력 배율. 나보다 약해야 「도우미」로 읽힌다.</summary>
        private const float AllyAtkMul = 0.7f;

        /// <summary>내 곁에서 이만큼(px) 안쪽을 지킨다. 너무 멀어지면 돌아온다.</summary>
        private const float AllyLeashPixels = 260f;

        private Unit _ally;

        private bool HasAlly => _ally != null && _ally.IsAlive && !_ally.IsDying;

        /// <summary>동료를 하나 세운다. 이 판에서 만난 몸 중 아무나 고른다.</summary>
        private void SpawnAlly()
        {
            ClearAlly();

            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0) return;

            // ⚠ **그림이 올라와 있는 몸만 고른다.** 호스트는 23명인데 이 런에
            //   아틀라스가 올라오는 것은 몇 종뿐이라, 아무나 고르면 깨진 사각형이 선다.
            HostEntry pick = null;
            int start = _rng.Next(hosts.Count);
            for (int i = 0; i < hosts.Count; i++)
            {
                var h = hosts[(start + i) % hosts.Count];
                if (h == null || h.IsGhost) continue;
                if (TrashSprite(h) == null) continue;
                pick = h; break;
            }
            if (pick == null) return;

            var me = Avatar;
            var at = me != null ? me.Position : Vector2.zero;

            var u = NewUnit($"Ally_{pick.HostKey}");
            int hp = Mathf.Max(1, (_host != null ? _host.HpMax : 100) * AllyHpPercent / 100);
            u.Setup(UnitSide.Player, pick.HostKey, pick.DisplayName, TrashSprite(pick),
                    hp, Mathf.Max(1, Mathf.RoundToInt(EnemyAtkOf(pick) * AllyAtkMul)),
                    EnemySpeedOf(pick), HostRangeOf(pick), EnemyIntervalOf(pick),
                    UnitBox(84f, 78f), isBoss: false, profile: pick);
            u.Position = ClampedInField(u, at + new Vector2(72f, 0f));
            u.SetState(EnemyState.Idle);
            u.ResetPattern();
            ApplyFacingSprites(u, pick.SpriteKey);
            _ally = u;

            PlayFx("shield", u.Position, 128f, loop: false);
            Debug.Log($"[동료] {pick.NameKr} HP {hp} 공격 {u.Atk}");
        }

        private void ClearAlly()
        {
            if (_ally != null) Destroy(_ally.gameObject);
            _ally = null;
        }

        /// <summary>
        /// 내가 맞을 피해를 동료가 대신 받는다. 받아 냈으면 true.
        ///
        /// 조준을 옮기는 대신 이렇게 한 이유 — 적의 공격은 전부 플레이어를 겨누도록
        /// 짜여 있어서, 표적을 바꾸려면 탄·근접·장판·보스 패턴을 전부 고쳐야 한다.
        /// 「앞에 서 주는 몸」은 이 한 곳으로 정직하게 표현된다.
        /// </summary>
        private bool SoakWithAlly(int damage)
        {
            if (!HasAlly) return false;

            ShowDamage(_ally.Position, damage, toEnemy: false);
            if (_ally.TakeDamage(damage))
            {
                PlayFx("burst", _ally.Position, 128f, loop: false);
                ClearAlly();
            }
            return true;
        }

        /// <summary>동료를 한 프레임 굴린다 — 가까운 적을 쫓아가 때리고, 멀어지면 돌아온다.</summary>
        private void TickAlly(float dt)
        {
            if (!HasAlly) return;

            _ally.TickFlash(dt);
            _ally.TickAnim(dt);

            var me = Avatar;
            if (me == null) return;

            // 가장 가까운 적
            Unit target = null;
            float best = float.MaxValue;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying) continue;
                float d = Vector2.Distance(e.Position, _ally.Position);
                if (d < best) { best = d; target = e; }
            }

            // 적이 없거나 내게서 너무 멀어지면 나에게 돌아온다.
            if (target == null || Vector2.Distance(_ally.Position, me.Position) > AllyLeashPixels)
            {
                _ally.SetState(EnemyState.Approach);
                _ally.Position = SlideMove(_ally, _ally.Position,
                                           _ally.StepToward(me.Position, dt));
                _ally.SetMoving(true);
                return;
            }

            _ally.SetFacing(target.Position - _ally.Position);

            if (best > _ally.AttackRange)
            {
                _ally.SetState(EnemyState.Approach);
                _ally.Position = SlideMove(_ally, _ally.Position,
                                           _ally.StepToward(target.Position, dt));
                _ally.SetMoving(true);
                return;
            }

            _ally.SetMoving(false);
            // ⚠ `fromPlayer: true` — 이래야 피해가 적에게 간다(파일 머리 주석 참조).
            if (_ally.TickAttack(dt)) PerformAttack(_ally, target, true);
            else _ally.SetState(EnemyState.Cooldown);
        }
    }
}
