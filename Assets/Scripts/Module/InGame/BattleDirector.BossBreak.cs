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

        /// <summary>
        /// 취약 창 배수. 안 열려 있으면 1배.
        ///
        /// ⚠ 가디언은 **시간제가 아니다**(`BreakSeconds = 0`). 마디를 3개 이하로
        ///   끊으면 머리가 **영구히** 열린다 — 그 뒤로는 계속 이 배수를 받는다.
        ///   이것이 없으면 `_headOpen` 을 세워 두기만 하고 아무도 안 읽어서,
        ///   "마디를 끊어야 머리가 열린다" 는 이 보스의 정체성이 화면에서
        ///   **아무 차이도 만들지 않는다.** 실제로 그 상태였다.
        /// </summary>
        private float BreakMul(Unit victim)
        {
            if (victim == null) return 1f;
            if (IsBossBroken && victim == _breakBoss) return BreakDamageMul;
            if (_headOpen && victim.IsBoss && IsSegmented) return BreakDamageMul;
            return 1f;
        }

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
            Debug.Log($"[보스] 취약 창 {sec:0.0}초 — {why}");
        }

        private void TickBreak(float dt)
        {
            if (_breakLeft <= 0f) return;
            _breakLeft -= dt;
            if (_breakLeft <= 0f) { _breakLeft = 0f; _breakBoss = null; }
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

                // ⚠ 킹핀 「저공 활강」은 여기 없다. 발동하는 순간에 보면 **출발한 자리**를
                //   보게 된다 — 활강은 그 뒤에 날아간다. 날아가 멈춘 자리에서 보려고
                //   `TickGlideBreak` 로 옮겼다.

                // ── 파이썬 — 나온 직후에 때렸다 ───────────────────
                // 머리가 벽 밖에 나와 있는 짧은 동안만 창이 열린다.
                // 못 때리면 되들어가고 아무 일도 안 생긴다.
                case BossDraw.HeadLunge:
                    if (_bossExposedHit) OpenBreak(boss, "뻗은 목을 제때 때렸다");
                    break;

                // ── 로봇 스네이크 — 솟은 것을 되들어가기 전에 때렸다 ──
                case BossDraw.BurrowStrike:
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

        /// <summary>이 수 이하로 끊으면 머리가 열린다. 정본 「3개 이하」.</summary>
        private const int HeadOpenAt = 3;

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
            //
            // ⚠ `OpenBreak` 를 부르지 않는다. 그쪽은 `BreakSeconds` 가 0 이면
            //   맨 첫 줄에서 그냥 돌아간다 — 가디언은 0 이라 **한 번도 열린 적이 없었다.**
            //   여기서 직접 연다. 여는 값은 `_headOpen` 이고 `BreakMul` 이 그것을 읽는다.
            if (_segmentsLeft <= HeadOpenAt && !_headOpen)
            {
                _headOpen = true;

                // 눈에 보이는 순간이어야 한다. 숫자만 바뀌면 무슨 일이 났는지 모른다.
                PlayFx("burst", victim.Position, 216f, loop: false);
                var t = RentDamageText();
                if (t != null) t.Show(victim.Position, "머리 노출", HealColor);
                Debug.Log($"[보스] 가디언 머리 무적 해제 — 마디 {_segmentsLeft} 남음 "
                          + $"· 이제부터 피해 {BreakDamageMul:0.#}배");
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

        // 로봇 스네이크 — 구멍. **항상 하나는 나와 있다(100%).**
        //   5200 HP 라 55% 면 전투가 두 배로 길어진다. 대신 자리가 계속 바뀐다.

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
            // ⚠ 「부스터 강하」로 올라가 있는 동안은 건드리지 않는다.
            //   여기서 매 프레임 Show 를 부르면 올라가자마자 다시 내려앉는다.
            if (_dropLeft > 0f) return;

            var def = _brain != null ? _brain.Entry : null;
            if (def == null || !HidesAway) { Show(boss); return; }
            if (IsBossBroken) { Show(boss); return; }

            switch (def.State)
            {
                // ── 파이썬 — 벽 구멍으로 나왔다 들어간다 ──────────
                //    주기와 자리는 무대가 안다(`BattleDirector.PythonStage`).
                case BossState.Walls:
                    TickPythonPresence(dt, boss);
                    break;

                // ── 로봇 스네이크 — 늘 나와 있다. 자리는 **솟아오름**이 정한다 ──
                //
                // ⚠ 전에는 3초마다 바닥 구멍 여섯을 돌며 **순간이동**했다.
                //   솟고 들어가는 그림이 없어 그냥 튀어 다니는 것으로 보였고,
                //   내가 어디 있든 상관이 없어 쫓기는 느낌도 없었다.
                //   고정 구멍을 버렸다(기획 2026-09-07) — 이제 자리를 옮기는 것은
                //   「솟아오름」뿐이고, 그것은 **내 발밑**으로 온다.
                //   솟아오름 예고 동안만 바닥 밑으로 들어간다 — 갈라지는 바닥만 남는다.
                case BossState.Holes:
                    if (_sank) Hide(boss, shadow: false);
                    else Show(boss);
                    break;

                // ── 슬러지 — 천장 패턴 동안만 위에 있다 ────────────
                case BossState.Ceiling:
                {
                    bool onCeiling = _dangerMove != null
                        && (_dangerMove.Draw == BossDraw.CeilingCling
                         || _dangerMove.Draw == BossDraw.CeilingSpread);
                    // ⚠ 「솟아오름」도 몸이 안 보인다. 다만 **위가 아니라 아래**다 —
                    //   천장에 붙은 것은 그림자를 남기지만(어디 있는지 보여야 한다),
                    //   바닥에 가라앉은 것은 그림자도 없다. 남기면 부푸는 바닥 위에
                    //   그림자가 겹쳐 "가라앉지 않았다" 로 보인다.
                    if (_sank) Hide(boss, shadow: false);
                    else if (onCeiling) Hide(boss, shadow: true);
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

        private float Rand01(float a, float b) => Mathf.Lerp(a, b, (float)_rng.NextDouble());

        /// <summary>방을 나가거나 보스가 죽으면 걸려 있던 것을 전부 끈다.</summary>
        private void ClearBossState()
        {
            _breakLeft = 0f;
            _breakBoss = null;
            _segmentsLeft = SegmentCount;
            _segmentDamage = 0;
            _headOpen = false;
            _bossExposed = true;
            _bossExposedHit = false;
        }
    }
}
