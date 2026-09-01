using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 보스의 **상태**와 **취약 창**.
    ///
    /// 예고(`BattleDirector.Boss.cs`)가 "무엇이 오는가" 라면 이쪽은
    /// "무엇을 해야 이기는가" 다.
    ///
    /// ⚠ 취약 창은 **"잘 피했다" 가 아니라 "제대로 대응했다" 의 보상**이다.
    ///   도망만 다니면 열리지 않는다. 여섯 보스가 각각 다른 일을 요구한다 —
    ///   벽으로 유인하고, 버티고, 틈으로 빠지고, 보스끼리 때리게 만든다.
    ///   전부 "피하면 열림" 으로 만들면 보스가 여섯인 이유가 사라진다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ═══════════════════════════════════════════════════════════
        //  취약 창
        // ═══════════════════════════════════════════════════════════

        /// <summary>취약 창 동안 보스가 더 아프게 맞는다. 딜 타임이 실제로 이득이어야 한다.</summary>
        private const float BreakDamageMul = 2.0f;

        private float _breakLeft;
        private Unit _breakBoss;

        /// <summary>가디언 방패가 깨졌는가. 깨져야 정면 감소가 70% → 30% 로 내려간다.</summary>
        private bool _guardBroken;

        /// <summary>이번 「자기 유도 로켓」이 다른 머리를 맞혔는가.</summary>
        private bool _homingHitAlly;

        private bool IsBossBroken => _breakLeft > 0f && _breakBoss != null;

        /// <summary>취약 창 배수. 안 열려 있으면 1배.</summary>
        private float BreakMul(Unit victim)
            => IsBossBroken && victim == _breakBoss ? BreakDamageMul : 1f;

        private void OpenBreak(Unit boss, string why)
        {
            if (boss == null || IsBossBroken) return;
            var def = _brain != null ? _brain.Entry : null;
            float sec = def != null && def.HasBreak ? def.BreakSeconds : 2f;
            if (sec <= 0f) return;

            _breakBoss = boss;
            _breakLeft = sec;
            // 열린 동안은 굳어 있다 — 때릴 시간이라는 뜻이다.
            boss.ApplyStun(sec);
            boss.SetTelegraph(false);
            ClearDanger();

            // 가디언만 브레이크가 **필수**다. 이걸 못 내면 딜이 아예 안 들어간다.
            if (def != null && def.State == BossState.Guard) _guardBroken = true;

            PlayFx("burst", boss.Position, 216f, loop: false);
            SpawnBreakBody(boss);
            Debug.Log($"[보스] 취약 창 {sec:0.0}초 — {why}");
        }

        private void TickBreak(float dt)
        {
            if (_breakLeft <= 0f) return;
            _breakLeft -= dt;
            if (_breakLeft <= 0f) { _breakLeft = 0f; _breakBoss = null; }
        }

        /// <summary>
        /// 열린 자리에 빼앗을 몸 하나.
        ///
        /// 보스는 빙의할 수 없으니 **몸을 갈아탈 기회는 이때 부르는 것뿐**이다 —
        /// 회피와 빙의가 한 동작이 되는 지점이 여기다.
        ///
        /// ⚠ 이미 서 있으면 더 부르지 않는다. 취약 창마다 쌓이면 방이 몸으로 넘친다.
        /// </summary>
        private void SpawnBreakBody(Unit boss)
        {
            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0) return;

            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null && _enemies[i].IsAlive && _enemies[i].IsHostBody) return;

            HostEntry e = null;
            var phase = _canonRoom != null ? _canonRoom.BossPhase(_brain != null ? _brain.Phase : 1) : null;
            if (phase != null && phase.MinionPool.Count > 0)
                e = ActorProfile(phase.MinionPool[0], hosts);
            if (e == null)
                for (int i = 0; i < hosts.Count; i++)
                    if (!hosts[i].IsGhost) { e = hosts[i]; break; }
            if (e == null) return;

            var u = NewUnit($"BreakBody_{e.HostKey}");
            u.Setup(UnitSide.Enemy, e.HostKey, e.NameKr, UnitGet(e.SpriteKey),
                    EnemyHpOf(e), EnemyAtkOf(e), EnemySpeedOf(e),
                    EnemyRangeOf(e), EnemyIntervalOf(e),
                    UnitBox(84f, 78f), isBoss: false, profile: e);
            u.Position = ClampedInField(u, boss.Position + new Vector2(180f, -120f));
            u.MarkAsHostBody();
            u.MarkAsNextBody();
            u.PossessPriority = e.PossessPriority;
            u.IsAggro = true;
            u.ResetPattern();
            ApplyFacingSprites(u, e.SpriteKey);
            _enemies.Add(u);
        }

        // ── 조건 — 무엇을 해야 열리는가 ──────────────────────────

        /// <summary>보스가 벽에 닿았는가. 크러셔 돌진 · 가디언 방패 행진이 쓴다.</summary>
        private bool BossAtWall(Unit boss)
        {
            if (boss == null) return false;
            var half = ((RectTransform)boss.transform).sizeDelta * 0.5f;
            float edge = RoomEdgeMeters * _pxPerMeter + 6f;
            return boss.Position.x <= edge + half.x
                || boss.Position.x >= _roomSize.x - edge - half.x
                || boss.Position.y >= -edge - half.y
                || boss.Position.y <= -_roomSize.y + edge + half.y;
        }

        /// <summary>
        /// 패턴이 끝난 직후 취약 창 조건을 본다.
        /// <paramref name="playerHit"/> 는 이번 패턴이 플레이어를 맞혔는가.
        /// </summary>
        private void CheckBreak(Unit boss, BossMove m, bool playerHit)
        {
            if (boss == null || m == null || IsBossBroken) { _homingHitAlly = false; return; }
            var def = _brain != null ? _brain.Entry : null;
            if (def == null || !def.HasBreak) { _homingHitAlly = false; return; }

            switch (m.Draw)
            {
                // 크러셔 「벽 돌진」 — 옆으로 피하면 보스가 벽에 박는다
                case BossDraw.Dash:
                    if (!playerHit && BossAtWall(boss)) OpenBreak(boss, "돌진이 벽에 박혔다");
                    break;

                // 가디언 「방패 행진」 — 벽으로 유인하면 낀다
                case BossDraw.Line when def.State == BossState.Guard:
                    if (BossAtWall(boss)) OpenBreak(boss, "방패 행진이 벽에 걸렸다");
                    break;

                // 킹핀 「엄폐 이동 사격」 — 3점사 뒤에는 반드시 재장전한다.
                // 조건이 없는 유일한 브레이크다. 짧은 대신 자주 온다.
                case BossDraw.Burst:
                    OpenBreak(boss, "3점사 뒤 재장전");
                    break;

                // 파이썬 「조임 나선」 — 틈으로 빠져나왔을 때만 열린다.
                // ⚠ 못 빠져나오면 경직도 없다 — 대응 실패가 딜 손실로 바로 이어지는
                //   유일한 보스다. 여기에 자비를 넣으면 이 보스의 질문이 사라진다.
                case BossDraw.Ring:
                    if (!playerHit) OpenBreak(boss, "조임 나선의 틈으로 빠져나왔다");
                    break;

                // 로봇 스네이크 「자기 유도 로켓」 — 다른 머리에 맞혔을 때.
                // 보스가 보스를 때린 것이라 브레이크가 가장 길다(3초).
                case BossDraw.Homing:
                    if (_homingHitAlly) OpenBreak(boss, "로켓을 다른 머리로 유도했다");
                    break;
            }
            _homingHitAlly = false;
        }

        // ═══════════════════════════════════════════════════════════
        //  GUARD — 앞이 막혀 있다 (가디언)
        // ═══════════════════════════════════════════════════════════
        //
        // 정면 120° 는 상시 피해 감소 70%. 그냥 쏘면 안 들어가고,
        // 「반사선」이 도는 4초 동안 정면으로 쏘면 2배로 되돌아온다.
        //
        // ⚠ **이 보스만 브레이크가 필수다.** 방패를 깨야 70% 가 30% 로 내려간다 —
        //   원거리로 정면만 두들기면 영영 안 죽는다. 그것이 이 보스의 질문이다.

        private const float GuardConeDegrees = 120f;
        private const float GuardReduceIntact = 0.70f;
        private const float GuardReduceBroken = 0.30f;
        private const float ReflectLineSeconds = 4f;

        private float _reflectLineLeft;

        /// <summary>「반사선」이 도는 동안인가.</summary>
        private bool IsReflectLine => _reflectLineLeft > 0f;

        private void TickGuard(float dt)
        {
            if (_reflectLineLeft > 0f) _reflectLineLeft -= dt;
        }

        /// <summary>
        /// 가디언 정면으로 들어온 피해에 곱하는 값. 다른 보스는 언제나 1배.
        ///
        /// 방향은 **때린 몸의 자리**로 잰다. 탄이든 근접이든 결국 내가 선 쪽에서 오므로,
        /// 탄마다 방향을 따로 들고 다니지 않아도 같은 답이 나온다.
        /// </summary>
        private float GuardMul(Unit victim)
        {
            if (victim == null || !victim.IsBoss) return 1f;
            var def = _brain != null ? _brain.Entry : null;
            if (def == null || def.State != BossState.Guard) return 1f;

            var from = Avatar;
            if (from == null) return 1f;
            var to = from.Position - victim.Position;
            if (to.sqrMagnitude < 0.0001f) return 1f;
            // 등 뒤에서는 감소가 없다 — 돌아 들어가는 것이 답이다.
            if (Vector2.Angle(victim.Facing, to) > GuardConeDegrees * 0.5f) return 1f;

            return 1f - (_guardBroken ? GuardReduceBroken : GuardReduceIntact);
        }

        /// <summary>
        /// 가디언이 되받아칠 탄인가. **근접 타격은 반사되지 않는다**(정본) —
        /// 그래서 「반사선」 중에는 붙어서 때리거나 뒤로 도는 것이 답이 된다.
        /// </summary>
        private bool TryGuardReflect(Unit boss, Projectile shot)
        {
            if (!IsReflectLine || boss == null || shot == null || !shot.FromPlayer) return false;
            var to = shot.Position - boss.Position;
            if (to.sqrMagnitude < 0.0001f) return false;
            if (Vector2.Angle(boss.Facing, to) > GuardConeDegrees * 0.5f) return false;

            shot.TurnHostile(2f);   // 되돌아오는 탄은 두 배로 아프다
            PlayFx("reflect", shot.Position, 64f, loop: false);
            return true;
        }

        /// <summary>방을 나가거나 보스가 죽으면 걸려 있던 것을 전부 끈다.</summary>
        private void ClearBossState()
        {
            _breakLeft = 0f;
            _breakBoss = null;
            _guardBroken = false;
            _reflectLineLeft = 0f;
            _homingHitAlly = false;
        }
    }
}
