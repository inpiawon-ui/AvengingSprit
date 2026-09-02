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
        ///
        /// ⚠ 여섯이 **서로 다른 것을 요구한다.** 전부 "피하면 열림" 으로 만들면
        ///   보스가 여섯인 이유가 사라진다 — 벽으로 유인하고, 마디를 끊고,
        ///   나온 순간을 노리고, 구조물에 걸고, 되들어가기 전에 때리고, 떨어뜨린다.
        /// </summary>
        private void CheckBreak(Unit boss, BossMove m, bool playerHit)
        {
            if (boss == null || m == null || IsBossBroken) return;
            var def = _brain != null ? _brain.Entry : null;
            if (def == null || !def.HasBreak) return;

            switch (m.Draw)
            {
                // ── 크러셔 — 파괴구가 헛돌아 벽을 때렸다 ──────────
                // 원 궤도 안쪽으로 파고들면(플레이어를 못 맞히면) 사슬이 벽을 친다.
                case BossDraw.WreckingBall:
                    if (!playerHit && BossAtWall(boss)) OpenBreak(boss, "파괴구가 벽에 박혔다");
                    break;

                // ── 킹핀 — 활강을 옥상 구조물 쪽으로 유인했다 ─────
                // 떠 있는 동안은 근접이 안 닿는다. 끌어내리는 방법이 이것뿐이다.
                case BossDraw.StrafingRun:
                    if (!playerHit && BossAtWall(boss)) OpenBreak(boss, "활강이 구조물에 걸렸다");
                    break;

                // ── 파이썬 — 나온 직후에 때렸다 ───────────────────
                // 머리가 벽 밖에 나와 있는 짧은 동안만 창이 열린다.
                // 못 때리면 되들어가고 아무 일도 안 생긴다.
                case BossDraw.WallBurst:
                case BossDraw.TripleBurst:
                    if (_bossExposedHit) OpenBreak(boss, "나온 머리를 제때 때렸다");
                    break;

                // ── 로봇 스네이크 — 되들어가기 전에 때렸다 ────────
                case BossDraw.HatchOpen:
                case BossDraw.FullEmergence:
                    if (_bossExposedHit) OpenBreak(boss, "솟은 머리를 되들어가기 전에 때렸다");
                    break;

                // ── 슬러지 — 천장에 붙은 것을 떨어뜨렸다 ──────────
                case BossDraw.CeilingCling:
                case BossDraw.CeilingSpread:
                    if (_bossExposedHit) OpenBreak(boss, "천장에 붙은 것을 떨어뜨렸다");
                    break;
            }
            _bossExposedHit = false;
        }

        // ═══════════════════════════════════════════════════════════
        //  가디언 — 마디 여덟
        // ═══════════════════════════════════════════════════════════
        //
        // ⚠ 원작 가디언은 **지네**다. 예전 코드는 정본 텍스트만 보고 방패병으로 만들어
        //   정면 120° 피해 감소와 반사선을 붙여 놨는데, 원작 시트에 방패가 없다.
        //   그 코드는 가리킬 보스가 없어져 통째로 지웠다.
        //
        // 몸통 마디 8개가 각각 HP 200 · 머리 800 = 합 2400 (보스 HP 그대로).
        // **마디가 3 이하로 줄기 전까지 머리는 열리지 않는다.**
        // 마디를 끊을수록 짧아지고 빨라진다 — 편하게 만든 만큼 위험해진다.

        private const int SegmentCount = 8;
        private const int SegmentHp = 200;

        private int _segmentsLeft = SegmentCount;
        private int _segmentDamage;

        /// <summary>남은 마디 수. 「마디 돌진」의 길이가 이 값을 따라간다.</summary>
        public int SegmentsLeft => _segmentsLeft;

        /// <summary>이 보스가 마디를 가진 보스인가.</summary>
        private bool IsSegmented
            => _brain != null && _brain.Entry != null && _brain.Entry.State == BossState.Segments;

        /// <summary>
        /// 보스가 맞았다. 마디부터 깎이고, 3 이하로 줄면 머리가 **영구히** 열린다.
        ///
        /// 오토어택이라 플레이어가 조준할 대상을 고르지 않는다 — 그래서 마디와 머리를
        /// 다른 표적으로 두지 않고 **한 HP 풀에서 앞쪽 1600 을 마디로** 본다.
        /// 화면에서는 마디가 하나씩 떨어져 나가는 것으로 읽힌다.
        /// </summary>
        private void NoteBossDamage(Unit victim, int damage)
        {
            if (victim == null || !victim.IsBoss || damage <= 0) return;

            // 나와 있는 동안 맞았는가 — 파이썬·로봇스네이크·슬러지의 취약 창 조건이다.
            if (_bossExposed) _bossExposedHit = true;

            if (!IsSegmented || _segmentsLeft <= 0) return;

            _segmentDamage += damage;
            int left = SegmentCount - _segmentDamage / SegmentHp;
            if (left >= _segmentsLeft) return;

            _segmentsLeft = Mathf.Max(0, left);
            PlayFx("shatter", victim.Position, 96f, loop: false);

            // 3 이하가 되는 순간 머리가 열린다. 시간제가 아니라 **영구**다.
            if (_segmentsLeft <= 3 && !_headOpen)
            {
                _headOpen = true;
                Debug.Log($"[보스] 가디언 머리 무적 해제 — 마디 {_segmentsLeft} 남음");
                OpenBreak(victim, $"마디를 {_segmentsLeft} 개로 끊었다");
            }
        }

        /// <summary>가디언 머리가 열렸는가. 한 번 열리면 안 닫힌다.</summary>
        private bool _headOpen;

        // ═══════════════════════════════════════════════════════════
        //  나와 있는 동안만 맞는 보스들
        // ═══════════════════════════════════════════════════════════
        //
        // 파이썬은 벽 뒤에, 로봇 스네이크는 구멍 안에, 슬러지는 천장에 있다.
        // **나와 있는 동안에만 맞고, 그때 때린 것이 취약 창을 연다.**
        //
        // ⚠ 이 게임은 오토어택이다. 노출 시간이 곧 전투 길이라서
        //   비율을 기획이 못 박아 뒀다 — 파이썬 55% · 로봇 스네이크 100%.
        //   (로봇 스네이크는 5200 HP 라 55% 면 전투가 두 배로 길어진다)

        private bool _bossExposed = true;
        private bool _bossExposedHit;
        private float _presenceLeft;

        /// <summary>지금 보스를 때릴 수 있는가. 숨어 있는 동안은 조준에서도 뺀다.</summary>
        public bool IsBossExposed => _bossExposed;

        // 파이썬 — 벽 뒤. 나와 있는 동안만 맞는다.
        //   기획이 못 박은 값: **노출 55% 이상.** 2.5 / (2.5 + 1.5) = 62.5%.
        private const float PythonOutSeconds = 2.5f;
        private const float PythonInSeconds  = 1.5f;

        // 로봇 스네이크 — 구멍. **항상 하나는 나와 있다(100%).**
        //   5200 HP 라 55% 면 전투가 두 배로 길어진다. 대신 자리가 계속 바뀐다.
        private const float SnakeHopSeconds = 3.0f;

        /// <summary>이 보스가 숨는 보스인가.</summary>
        private bool HidesAway
        {
            get
            {
                var def = _brain != null ? _brain.Entry : null;
                if (def == null) return false;
                return def.State == BossState.Walls
                    || def.State == BossState.Holes
                    || def.State == BossState.Ceiling;
            }
        }

        /// <summary>
        /// 보스가 어디에 있는가를 굴린다.
        ///
        /// ⚠ **취약 창이 안 열려 있을 때만 돈다.** 취약 창은 "때릴 시간" 이라
        ///   그동안 보스가 숨어 버리면 창을 열어 준 의미가 없다.
        /// </summary>
        private void TickBossPresence(float dt)
        {
            var boss = _boss;
            if (boss == null || !boss.IsAlive) return;
            var def = _brain != null ? _brain.Entry : null;
            if (def == null || !HidesAway) { Show(boss); return; }
            if (IsBossBroken) { Show(boss); return; }

            switch (def.State)
            {
                // ── 파이썬 — 벽 뒤에 있다가 뚫고 나온다 ───────────
                case BossState.Walls:
                    _presenceLeft -= dt;
                    if (_presenceLeft > 0f) break;
                    if (_bossExposed) { Hide(boss, shadow: false); _presenceLeft = PythonInSeconds; }
                    else
                    {
                        // 나올 자리는 매번 다르다. 어디서 나올지 모르는 것이 이 보스다.
                        boss.Position = ClampedInField(boss, WallSpot());
                        Show(boss);
                        _presenceLeft = PythonOutSeconds;
                    }
                    break;

                // ── 로봇 스네이크 — 구멍을 옮겨 다닌다. 늘 나와 있다 ──
                case BossState.Holes:
                    Show(boss);
                    _presenceLeft -= dt;
                    if (_presenceLeft > 0f) break;
                    _presenceLeft = SnakeHopSeconds;
                    _dangerTick++;                       // 다음 구멍
                    boss.Position = ClampedInField(boss, HoleSpot(_dangerTick));
                    PlayFx("shatter", boss.Position, 120f, loop: false);
                    break;

                // ── 슬러지 — 천장 패턴 동안만 위에 있다 ────────────
                case BossState.Ceiling:
                {
                    bool onCeiling = _dangerMove != null
                        && (_dangerMove.Draw == BossDraw.CeilingCling
                         || _dangerMove.Draw == BossDraw.CeilingSpread);
                    if (onCeiling) Hide(boss, shadow: true);   // 그림자만 남는다
                    else Show(boss);
                    break;
                }
            }
        }

        private void Show(Unit boss)
        {
            if (_bossExposed && !boss.IsHidden) return;
            _bossExposed = true;
            boss.SetHidden(false);
        }

        private void Hide(Unit boss, bool shadow)
        {
            _bossExposed = false;
            boss.SetHidden(true, shadow);
        }

        /// <summary>파이썬이 나오는 벽 앞자리. 네 벽을 돌아가며 쓴다.</summary>
        private Vector2 WallSpot()
        {
            float inset = 1.6f * _pxPerMeter;
            switch (_rng.Next(4))
            {
                case 0:  return new Vector2(inset, -_roomSize.y * Rand01(0.25f, 0.75f));
                case 1:  return new Vector2(_roomSize.x - inset, -_roomSize.y * Rand01(0.25f, 0.75f));
                case 2:  return new Vector2(_roomSize.x * Rand01(0.25f, 0.75f), -inset);
                default: return new Vector2(_roomSize.x * Rand01(0.25f, 0.75f), -_roomSize.y + inset);
            }
        }

        /// <summary>
        /// 구멍 여섯 중 하나.
        /// ⚠ <see cref="DangerShape"/> 와 **같은 표**를 쓴다 — 바닥 그림에 픽셀로 박힌 자리다.
        ///   여기서 따로 계산하면 뱀이 구멍 아닌 데서 솟는다.
        /// </summary>
        private Vector2 HoleSpot(int tick) => DangerShape.HoleAtRoom(tick, _roomSize);

        private float Rand01(float a, float b) => Mathf.Lerp(a, b, (float)_rng.NextDouble());

        // ── 숨는 보스 방에는 잡몹이 상시 둘 ──────────────────────
        //
        // 보스가 벽 뒤·구멍 안·천장에 있는 동안 방에 때릴 것이 하나도 없으면
        // **그냥 기다리는 시간**이 된다. 오토어택이라 플레이어가 할 일이 없다.
        //
        // 잡몹이 있으면 그동안 싸울 것이 생기고, **빙의도 같이 산다** —
        // 「그때 빼앗는다」가 이 세 보스의 취약 창 설명이기도 하다.

        private const int BossRoomMinions = 2;

        /// <summary>
        /// 몸이 오는 보스 체력. 이 아래로 처음 내려갈 때 **한 번씩** 온다.
        /// 시간제로 계속 채우면 방 내내 몸이 끊이지 않아 긴장이 사라진다.
        /// </summary>
        private static readonly float[] MinionWaveAt = { 0.80f, 0.50f, 0.30f };
        private readonly bool[] _minionWaveDone = new bool[MinionWaveAt.Length];


        /// <summary>
        /// 이 보스 방에 빼앗을 몸이 계속 나와야 하는가.
        ///
        /// 숨는 보스 셋(파이썬·로봇스네이크·슬러지)은 **보스를 못 때리는 동안** 할 일이
        /// 있어야 해서 몸이 나온다. 크러셔는 다른 이유다 —
        /// 정본이 「컨베이어가 멈추면 딸려 오던 잡몹이 그 자리에 선다 · **그때 빼앗는다**」
        /// 라고 적어 두었다. 벨트가 실어 나르는 것이 곧 몸이다.
        ///
        /// ⚠ 이것이 없으면 크러셔 방에 **빼앗을 몸이 하나도 없다.** 원작 보스 여섯이
        ///   전부 빙의 불가라, 몸이 안 나오면 첫 보스방에서 코어 루프가 통째로 끊긴다.
        /// </summary>
        private bool WantsMinions
        {
            // ⚠ **보스방 여섯 곳 모두 그렇다.** 예전에는 "컨베이어를 가진 보스" 로
            //   골랐는데, 크러셔의 벨트가 돌진으로 바뀌자 그 조건이 통째로 꺼져
            //   80·50·30% 소환이 조용히 사라질 뻔했다. 조건이 패턴 이름에 매달려
            //   있었던 것이 문제다 — 몸이 필요한 이유는 패턴이 아니라 **빙의**다.
            get => !NoBossMinions && _boss != null && _boss.IsAlive;
        }

        private void TickBossMinions(float dt)
        {
            if (_boss == null || !_boss.IsAlive || !WantsMinions) return;

            // ⚠ **시간이 아니라 보스 체력으로 부른다.**
            //   예전에는 8초마다 머릿수를 채웠는데, 그러면 방 내내 몸이 끊이지 않아
            //   "소환 타이밍이 너무 짧다" 가 된다. 세 번만 온다 — 80% · 50% · 30%.
            //   보스를 깎는 것이 곧 몸을 부르는 것이라, 언제 오는지 읽을 수 있다.
            float hp = _boss.HpMax > 0 ? (float)_boss.Hp / _boss.HpMax : 1f;
            int wave = -1;
            for (int i = 0; i < MinionWaveAt.Length; i++)
                if (hp <= MinionWaveAt[i] && !_minionWaveDone[i]) { wave = i; break; }
            if (wave < 0) return;
            _minionWaveDone[wave] = true;

            int alive = 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e != null && e.IsAlive && !e.IsDying && e != _boss) alive++;
            }

            // ⚠ **호스트 몸이어야 한다.** 잡몹(해골·박쥐)은 아무리 깎아도 못 뺏는다
            //   (`Unit.IsHostBody` — "잡몹은 못 뺏는다").
            //   여기 잡몹을 세워 놨더니 보스가 숨은 동안 때릴 것은 생겼는데
            //   **빼앗을 것이 없었다** — 「그때 빼앗는다」가 이 세 보스의 취약 창 설명인데
            //   정작 빼앗을 몸을 안 준 셈이다.
            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0) return;

            // ⚠ **그림이 올라와 있는 몸만 고른다.**
            //   호스트는 23명인데 이 런에 아틀라스가 올라오는 것은 몇 종뿐이다
            //   (`RunUnitKeys` — 그 챕터 잡몹 + 이 방 보스 + 내가 고른 몸).
            //   아무나 고르면 `UnitGet` 이 null 을 돌려줘 **그림 없이 선다** —
            //   화면에서는 깨진 사각형으로 보인다. 실제로 그렇게 나왔다.
            HostEntry profile = null;
            for (int i = 0; i < hosts.Count; i++)
            {
                var h = hosts[(_dangerTick + alive + i) % hosts.Count];
                if (h == null || h.IsGhost) continue;
                if (TrashSprite(h) == null) continue;   // 이 런에 그림이 없는 몸이다
                profile = h;
                break;
            }
            // 올라온 몸이 하나도 없으면 **세우지 않는다.** 깨진 그림을 세우느니 안 세운다.
            if (profile == null) return;

            // 한 파도에 `BossRoomMinions` 만큼 세운다. 이미 서 있는 것은 빼고 센다.
            int want = Mathf.Max(1, BossRoomMinions - alive);
            for (int w = 0; w < want; w++) SpawnOneMinion(profile, w);
            Debug.Log($"[보스] 몸 {want}기 — 체력 {hp * 100f:0}% ({MinionWaveAt[wave] * 100f:0}% 문턱)");
        }

        private void SpawnOneMinion(HostEntry profile, int seq)
        {
            var u = NewUnit($"BossMinion_{profile.HostKey}");
            u.Setup(UnitSide.Enemy, profile.HostKey, profile.NameKr, TrashSprite(profile),
                    EnemyHpOf(profile), EnemyAtkOf(profile), EnemySpeedOf(profile),
                    EnemyRangeOf(profile), EnemyIntervalOf(profile),
                    UnitBox(84f, 78f), isBoss: false, profile: profile);
            // 보스 옆이 아니라 **방 가장자리**에서 온다. 보스에 겹쳐 세우면
            // 보스가 나오는 순간 겹쳐 보이고, 예고 도형도 가린다.
            u.Position = ClampedInField(u, WallSpot() + new Vector2(seq * _pxPerMeter * 1.2f, 0f));
            u.MarkAsHostBody();          // 이게 있어야 빼앗을 수 있다
            u.PossessPriority = profile.PossessPriority;
            u.IsAggro = true;
            u.ResetPattern();
            ApplyFacingSprites(u, profile.SpriteKey);
            _enemies.Add(u);
        }

        /// <summary>방을 나가거나 보스가 죽으면 걸려 있던 것을 전부 끈다.</summary>
        private void ClearBossState()
        {
            _breakLeft = 0f;
            _breakBoss = null;
            _segmentsLeft = SegmentCount;
            _segmentDamage = 0;
            _headOpen = false;
            for (int i = 0; i < _minionWaveDone.Length; i++) _minionWaveDone[i] = false;
            _bossExposed = true;
            _bossExposedHit = false;
        }
    }
}
