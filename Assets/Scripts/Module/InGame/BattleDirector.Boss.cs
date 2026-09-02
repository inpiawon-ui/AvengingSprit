using System.Collections.Generic;
using Game.Character;
using UnityEngine;
using UnityEngine.UI;

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

        private DangerHint _hint;

        private void EnsureDangerViews()
        {
            if (_dangerView == null && _fieldLayer != null)
                _dangerView = DangerView.Create(_fieldLayer);
            if (_safeView == null && _fieldLayer != null)
                _safeView = DangerView.Create(_fieldLayer);
            // 화살표·이름표는 도형 **위**에 온다. 나중에 만들면 나중에 그려진다.
            if (_hint == null && _fieldLayer != null)
                _hint = DangerHint.Create(_fieldLayer);
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

            // ⚠ 「마디 돌진」만 길이가 **지금 남은 마디 수**를 따라간다.
            //   표에 적힌 7.2 m 는 마디 8개일 때의 값이다. 마디를 끊을수록 짧아진다 —
            //   여기서 한 번만 고쳐 두면 그린 것과 때리는 것이 같이 짧아진다.
            if (m.Draw == BossDraw.SegmentThrust && IsSegmented)
                _danger.Length = Mathf.Max(_pxPerMeter, SegmentsLeft * 0.9f * _pxPerMeter);

            if (_danger.IsNone) return;

            _dangerMove = m;
            _dangerView.Show(_danger, _roomSize, GetSprite("fx_danger_hatch"), safe: false);
            Debug.Log($"[진단:예고] {m.NameKr} draw={m.Draw} kind={_danger.Shape} r={_danger.Radius:0} " +
                      $"origin={_danger.Origin} 활성={_dangerView.gameObject.activeInHierarchy} " +
                      $"컬링={_dangerView.canvasRenderer.cull} 크기={((RectTransform)_dangerView.transform).sizeDelta}");

            // 안전지대는 **위험을 그린 다음**에 그린다. 위험만 있으면 "저기 맞겠네" 지만,
            // 안전이 같이 보이면 "저기로 가면 되네" 가 된다 — 훨씬 빨리 읽힌다.
            ShowSafeZone(m, boss);
            ShowHint(m, boss, me);

            // 쇠사슬 파괴구만 휘두르는 물건이 따로 있다. 나머지는 바닥 도형으로 읽힌다.
            if (m.Draw == BossDraw.WreckingBall) BeginOrbit(boss, _danger.Radius);

            // 위험한 면은 **탄으로 채운다.** 바닥에 원이나 부채꼴만 생겼다 터지면
            // 무엇이 그것을 만들었는지 알 수 없다 — "예고만 하고 아무 일도 안 났다".
            // 예고 시간과 정확히 같은 시간 동안 나므로 도착이 곧 발동이다.
            BeginFlight(m, boss, _brain != null ? _brain.TelegraphTotal : 1f);

            // 램프는 **패턴을 안 가린다.** 무엇이 오든 "온다" 를 알리는 것이라
            // 크러셔의 네 패턴에 다 뜬다 — 원작이 그렇게 쓴다.
            BeginLamp(boss);
        }

        // ── ③ 화살표 · ④ 이름표 ──────────────────────────────────
        //
        // 정본이 「예고 4겹」이라 부르는 것 중 뒤 두 겹이다.
        // 앞 두 겹(몸 · 바닥)만으로는 **어디가 맞나**까지밖에 안 읽힌다.
        // 무엇인지와 어디로 가야 하는지는 따로 말해 줘야 한다.

        /// <summary>이름표를 이미 본 패턴. 처음 보는 것에만 이름이 뜬다(정본 ④겹).</summary>
        private const string SeenPatternsKey = "AVSR.SeenBossPatterns";
        private static HashSet<string> _seenPatterns;

        /// <summary>
        /// 이 패턴을 처음 보는가. 물어보는 순간 **봤다고 적는다.**
        ///
        /// ⚠ 저장은 기기에 남는다(`PlayerPrefs`). 계정에 붙이려면 유저 데이터로
        ///   옮겨야 하는데, 그러자고 저장 스키마를 늘릴 만한 값은 아니다.
        /// </summary>
        private static bool FirstSighting(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (_seenPatterns == null)
            {
                _seenPatterns = new HashSet<string>();
                var saved = PlayerPrefs.GetString(SeenPatternsKey, string.Empty);
                if (!string.IsNullOrEmpty(saved))
                    foreach (var s in saved.Split('|'))
                        if (!string.IsNullOrEmpty(s)) _seenPatterns.Add(s);
            }
            if (!_seenPatterns.Add(key)) return false;
            PlayerPrefs.SetString(SeenPatternsKey, string.Join("|", _seenPatterns));
            return true;
        }

        /// <summary>이름표 기록을 지운다. 테스트 메뉴가 부른다.</summary>
        public static void ForgetSeenPatterns()
        {
            _seenPatterns = null;
            PlayerPrefs.DeleteKey(SeenPatternsKey);
            PlayerPrefs.Save();
        }

        /// <summary>정본이 못박은 회피 8종. 화살표가 못 그리는 둘도 말로는 뜬다.</summary>
        private static string DodgeWord(DodgeHint d) => d switch
        {
            DodgeHint.Back  => "뒤로",
            DodgeHint.Side  => "옆으로",
            DodgeHint.Gap   => "틈으로",
            DodgeHint.Perp  => "직각으로",
            DodgeHint.Zone  => "안전지대로",
            DodgeHint.Close => "붙어라",
            DodgeHint.Swap  => "몸을 갈아타라",
            DodgeHint.Hold  => "쏘지 마라",
            _               => string.Empty,
        };

        private void ShowHint(BossMove m, Unit boss, Unit me)
        {
            if (_hint == null || m == null) return;
            if (me == null) { _hint.Hide(); return; }

            var dir = SafeDirection(m, boss, me);

            // 이름은 처음 볼 때만. 회피 한마디는 매번 — 이건 외우는 것이 아니라 읽는 것이다.
            string title = FirstSighting($"{boss?.Key}/{m.LabelKey}") ? m.NameKr : null;

            // 보스 머리 위. 256 짜리 몸의 절반보다 조금 더 올린다.
            var labelAt = (boss != null ? boss.Position : me.Position) + new Vector2(0f, 150f);

            _hint.Show(me.Position, dir, labelAt, title, DodgeWord(m.Dodge),
                       GetSprite("ui_dodge_arrow"));
        }

        /// <summary>
        /// 어디로 가야 안 맞나.
        ///
        /// ⚠ **도형을 다시 해석하지 않는다.** 굳어 있는 그 도형에게 직접
        ///   "여기 맞느냐" 를 물어(`Contains`) 안 맞는 가장 가까운 쪽을 찾는다.
        ///   그리는 도형·때리는 도형·가리키는 도형이 셋 다 같은 것이 되므로,
        ///   "화살표대로 갔는데 맞았다" 가 **구조적으로 불가능**해진다.
        ///   패턴마다 회피 공식을 따로 적으면 24벌이 서로 어긋난다.
        ///
        /// 방향이 없는 둘(몸을 갈아타라·쏘지 마라)은 <c>zero</c> 다 — 화살표를 숨긴다.
        /// </summary>
        private Vector2 SafeDirection(BossMove m, Unit boss, Unit me)
        {
            switch (m.Dodge)
            {
                case DodgeHint.Swap:
                case DodgeHint.Hold:
                    return Vector2.zero;

                // 「붙어라」는 도망이 아니다. 찾아 봐야 바깥으로 나가라고 가리킨다.
                case DodgeHint.Close:
                    return boss != null ? boss.Position - me.Position : Vector2.zero;

                // 기획이 좌표를 적어 둔 안전지대가 있으면 그리로 곧장 보낸다.
                case DodgeHint.Zone when m.HasSafeSpot:
                    var at = new Vector2(m.SafeAtMeters.x * _pxPerMeter,
                                         -m.SafeAtMeters.y * _pxPerMeter);
                    return at - me.Position;
            }

            // 16방향 × 0.5m 씩 8m 까지. **예고를 시작할 때 한 번만** 도는 계산이라
            // 최악 256번 물어봐도 프레임에 안 걸린다(hot path 가 아니다).
            //
            // ⚠ 반경은 재서 정했다. 24패턴 × 4상태 = 도형 96개, 위험 안에 서 있는
            //   자리 9,547개를 전부 시험한 결과다:
            //
            //     4.5 m  못 찾음 770 (8.1%)   잘못 가리킴 0
            //     6.0 m  못 찾음 232 (2.4%)   잘못 가리킴 0
            //     8.0 m  못 찾음  22 (0.2%)   잘못 가리킴 0   ← 여기가 무릎이다
            //    11.0 m  못 찾음   6 (0.1%)   잘못 가리킴 0
            //
            //   더 늘려도 6건밖에 안 줄고, 대신 "8m 밖으로 뛰어라" 라는 못 지킬
            //   지시를 하게 된다. 못 찾은 자리는 화살표를 숨기고 말만 남긴다 —
            //   **틀리게 가리키느니 안 가리키는 것이 낫다.**
            const int Rays = 16;
            float step = _pxPerMeter * 0.5f;
            float max = _pxPerMeter * 8f;
            float margin = _pxPerMeter * 0.6f;   // 벽에 코를 박는 답은 답이 아니다

            Vector2 best = Vector2.zero;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < Rays; i++)
            {
                float a = i * (Mathf.PI * 2f / Rays);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                for (float r = step; r <= max; r += step)
                {
                    var p = me.Position + dir * r;
                    if (p.x < margin || p.x > _roomSize.x - margin
                        || p.y > -margin || p.y < -_roomSize.y + margin) break;
                    if (_danger.Contains(p, _roomSize)) continue;
                    if (r < bestDistance) { bestDistance = r; best = dir; }
                    break;   // 이 방향에서 가장 가까운 탈출점을 찾았다
                }
            }
            return best;
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

        // ── 패턴의 효과 ──────────────────────────────────────────
        //
        // ⚠ 24패턴을 도형 체계로 옮기면서 **효과가 옛 코드에 남겨졌다.**
        //   `StrikeDanger` 가 도형 있는 패턴을 전부 처리하고 `true` 를 돌려주므로
        //   효과가 들어 있던 `ExecuteBossMove` 는 한 번도 안 돈다 —
        //   도형과 피해만 남고 나머지가 통째로 떨어져 나갔다.
        //   방패 전개가 방어막을 안 걸고, 컨베이어가 안 끌어당긴 것이 그래서다.
        //
        //   여기가 그 자리다. 도형·피해는 위에서 끝났고, **그 패턴을 그 패턴답게
        //   만드는 것**만 여기서 더한다.

        /// <summary>보스 패턴 동작을 끄는 배수. 0.17초 → 0.68초.</summary>
        private const float BossAttackHold = 4f;

        /// <summary>
        /// 평타 동작을 끄는 배수. 패턴보다 짧다 — 평타가 패턴만큼 길면
        /// 무엇이 큰 것인지 구별이 안 된다.
        /// </summary>
        private const float BossBasicAttackHold = 2f;

        private void ApplyMoveEffect(Unit boss, Unit me, BossMove m)
        {
            switch (m.Draw)
            {
                // 붉은 방패판을 정면에 세운다 · 4초간 피해 90% 감소
                case BossDraw.ShieldUp:
                    // 사 둔 파쇄가 첫 방어막을 그냥 없앤다(정본 EV_CH3_03).
                    // 없애 놓고도 내려찍기는 그대로 온다 — 산 것은 깨는 수단이지 안전이 아니다.
                    if (_bossShieldBreak) { _bossShieldBreak = false; _bossShield = 0f; }
                    else _bossShield = ShieldSeconds;

                    // ⚠ 정본은 「4초간 정면 120° 피해 90% 감소 **+ 그동안 압착을 연달아 두 번**」이다.
                    //   방어막만 걸면 이 구간이 **그냥 버티는 시간**이 된다 — 때려도 안 들어가는데
                    //   맞을 일도 없으니 플레이어가 할 일이 없어진다.
                    //   압착이 따라와야 "등 뒤로 돌아라" 가 지시가 된다.
                    QueueFollowUp(BossDraw.Crush, ShieldSlamCount);
                    break;

                // 그어 둔 줄을 타고 밀고 들어온다 · 그리고 곧바로 한 번 더 친다
                case BossDraw.RamCharge:
                    // ⚠ **예고 때 그린 그 방향으로 간다.** 지금 내 자리를 다시 물으면
                    //   "줄은 저기 그려졌는데 보스는 이리로 온다" 가 된다.
                    _chargeDamageMul = m.DamageMul;
                    // ⚠ 시간이 아니라 **그려 둔 줄의 길이**로 정한다.
                    //   고정 0.9초로 뒀더니 70px/s × 5.5 = 385px/s 라 584px 짜리 줄의
                    //   60% 에서 멈췄다 — 화면에서는 "가다 말았다" 로 보인다.
                    //   줄 끝까지 가야 그린 것과 간 것이 같아진다.
                    _chargeSpeedMul = ChargeSpeedMul * RamSpeedBoost;
                    float rammed = Mathf.Max(1f, boss.MoveSpeed * _chargeSpeedMul);
                    _brain.BeginCharge(_danger.Dir, Mathf.Clamp(_danger.Length / rammed, 0.3f, 3f));
                    // 「이동하고 또 공격」 — 밀고 들어온 자리에서 한 발 쏜다.
                    QueueFollowUp(BossDraw.MissileSalvo, 1);
                    break;

                // 끈적한 덩어리 · 웅덩이 4초 · 밟으면 이동 속도 절반
                case BossDraw.Spit:
                    SpawnField(_danger.Origin, Mathf.Max(_pxPerMeter, _danger.Radius),
                               PuddleSeconds, FieldEffect.Slow, 0, fromPlayer: false);
                    break;
            }
        }

        // ── 이어지는 타격 ────────────────────────────────────────
        //
        // 패턴 하나가 **다른 패턴을 불러오는** 경우가 있다.
        // 지금은 방패 전개가 압착을 두 번 부른다(정본 크러셔 P3).
        //
        // ⚠ 두뇌의 쿨다운을 건드리지 않는다. 쿨을 당기면 그 패턴의 다음 차례가
        //   통째로 흐트러져 "왜 압착이 두 배로 오지" 가 된다.
        //   여기서 **따로 세어 두고** 예고→타격을 한 번 더 돌린다.

        /// <summary>방패 전개가 부르는 압착 횟수. 정본 「연달아 두 번」.</summary>
        private const int ShieldSlamCount = 2;

        /// <summary>이어지는 타격 사이의 간격. 예고를 읽을 시간은 남겨야 한다.</summary>
        private const float FollowUpGap = 1.6f;

        private BossDraw _followUpDraw;
        private int _followUpLeft;
        private float _followUpTimer;

        private void QueueFollowUp(BossDraw draw, int times)
        {
            _followUpDraw = draw;
            _followUpLeft = times;
            _followUpTimer = FollowUpGap;
        }

        private void ClearFollowUp()
        {
            _followUpDraw = BossDraw.None;
            _followUpLeft = 0;
            _followUpTimer = 0f;
        }

        /// <summary>
        /// 예약된 타격을 굴린다. 예고 중이면 기다린다 — 두 도형이 겹치면 못 읽는다.
        /// </summary>
        private void TickFollowUp(float dt)
        {
            if (_followUpLeft <= 0 || _boss == null || !_boss.IsAlive) return;
            if (_brain != null && _brain.IsTelegraphing) return;   // 지금 다른 예고가 떠 있다

            _followUpTimer -= dt;
            if (_followUpTimer > 0f) return;

            var move = MoveOf(_followUpDraw);
            if (move == null) { ClearFollowUp(); return; }

            _followUpLeft--;
            _followUpTimer = FollowUpGap;
            BeginDanger(_boss, Avatar, move);
        }

        /// <summary>이 보스의 목록에서 그 패턴을 찾는다. 없으면 null.</summary>
        private BossMove MoveOf(BossDraw draw)
        {
            var entry = _brain != null ? _brain.Entry : null;
            if (entry == null) return null;
            for (int i = 0; i < entry.Moves.Count; i++)
                if (entry.Moves[i].Draw == draw) return entry.Moves[i];
            return null;
        }

        // ⚠ 컨베이어(끌어당기는 벨트)는 **없앴다.** 기획 2026-09-02 에서
        //   크러셔의 세 번째 패턴이 「나에게 줄을 긋고 그 줄을 타고 밀고 들어온다」로
        //   바뀌었다. 벨트를 남겨 두면 쓰지도 않는 12초짜리 상태가 매 방 돌아간다.

        private const float PuddleSeconds = 4f;

        // ── 방패판 ───────────────────────────────────────────────
        //
        // 방어막은 지금 **숫자로만** 있다. 앞에서 때리면 90% 가 깎이는데
        // 화면에는 아무 표시가 없어서, 왜 안 들어가는지 알 수가 없다.
        // 원작 시트의 붉은 판이 62차에 `obj_crusher_shield` 로 왔다.
        //
        // ⚠ 램프와 같은 규칙 — 보스 이름을 적지 않고 **`obj_{보스키}_shield` 를 묻는다.**
        //   판이 없는 보스는 그냥 안 뜬다.

        private RectTransform _shieldRt;
        private Image _shieldImg;

        private void TickBossShieldView()
        {
            bool on = _bossShield > 0f && _boss != null && _boss.IsAlive;
            if (!on)
            {
                if (_shieldRt != null) _shieldRt.gameObject.SetActive(false);
                return;
            }

            var art = GetSprite($"obj_{_boss.Key}_shield");
            if (art == null) return;

            if (_shieldRt == null)
            {
                var go = new GameObject("BossShield", typeof(RectTransform));
                _shieldRt = (RectTransform)go.transform;
                _shieldRt.SetParent(_fieldLayer, false);
                _shieldRt.anchorMin = _shieldRt.anchorMax = new Vector2(0f, 1f);
                _shieldRt.pivot = new Vector2(0.5f, 0.5f);
                _shieldImg = go.AddComponent<Image>();
                _shieldImg.raycastTarget = false;
                _shieldImg.preserveAspect = true;
            }
            _shieldImg.sprite = art;
            _shieldRt.gameObject.SetActive(true);

            // **보스가 보는 쪽**에 세운다. 정면 120° 만 막으므로 판이 선 쪽이
            // 곧 안 들어가는 쪽이어야 한다 — 등 뒤로 돌라는 지시가 그림과 맞아야 한다.
            var facing = _boss.Facing;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector2.down;
            facing = facing.normalized;
            float side = _pxPerMeter * 2f;
            _shieldRt.sizeDelta = new Vector2(side, side);
            _shieldRt.anchoredPosition = _boss.Position + facing * (_pxPerMeter * 1.6f);
            _shieldRt.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg + 90f);
        }

        // ── 경고 램프 — 원작에 내장된 예고 ────────────────────────
        //
        // 정본: 「이 보스는 예고가 그림에 내장돼 있다 — 등이 한 칸씩 차오르고
        //        다 차면 온다. 예고 4겹의 「몸」 겹을 원작이 이미 풀어 놨다」
        //
        // 원작 시트의 Light 5프레임이 그것이고, 62차에 `fx_crusher_lamp_1~5` 로 왔다.
        //
        // ⚠ 크러셔로 못 박지 않는다. **`fx_{보스키}_lamp_1` 이 있으면 켠다** —
        //   다른 보스에 램프가 오면 코드를 안 고쳐도 그대로 돈다.
        //   이름으로 물어보는 것이 이름을 적어 두는 것보다 오래 간다.

        private const int LampSteps = 5;

        private RectTransform _lampRt;
        private Image _lampImg;
        private string _lampKey;      // 지금 램프를 쓰는 보스. 없으면 이 보스는 램프가 없다

        private void BeginLamp(Unit boss)
        {
            _lampKey = null;
            if (boss == null || _fieldLayer == null) return;
            if (GetSprite($"fx_{boss.Key}_lamp_1") == null) return;   // 램프가 없는 보스다

            if (_lampRt == null)
            {
                var go = new GameObject("BossLamp", typeof(RectTransform));
                _lampRt = (RectTransform)go.transform;
                _lampRt.SetParent(_fieldLayer, false);
                _lampRt.anchorMin = _lampRt.anchorMax = new Vector2(0f, 1f);
                _lampRt.pivot = new Vector2(0.5f, 0.5f);
                _lampRt.sizeDelta = new Vector2(72f, 72f);
                _lampImg = go.AddComponent<Image>();
                _lampImg.raycastTarget = false;
                _lampImg.preserveAspect = true;
            }
            _lampKey = boss.Key;
            _lampRt.gameObject.SetActive(true);
            TickLamp(boss, 0f);
        }

        /// <param name="progress">예고 진행도 0~1. 다 차면 온다.</param>
        private void TickLamp(Unit boss, float progress)
        {
            if (_lampKey == null || _lampRt == null || boss == null) return;
            int step = Mathf.Clamp(Mathf.FloorToInt(progress * LampSteps) + 1, 1, LampSteps);
            var art = GetSprite($"fx_{_lampKey}_lamp_{step}");
            if (art != null) _lampImg.sprite = art;
            _lampImg.enabled = art != null;
            // 몸 위에 얹는다. 이름표(150)보다 낮게 둬서 글자를 가리지 않는다.
            _lampRt.anchoredPosition = boss.Position + new Vector2(0f, 108f);
        }

        private void EndLamp()
        {
            _lampKey = null;
            if (_lampRt != null) _lampRt.gameObject.SetActive(false);
        }

        // ── 휘두르는 물건 ────────────────────────────────────────
        //
        // 「붙어라」가 말이 되려면 **무엇에 안 닿는지가 보여야 한다.**
        // 지금은 바닥에 도넛만 뜨고 휘두르는 것이 없어서, 화살표가 보스를 가리키며
        // 「붙어라」라고만 하는 꼴이었다 — 무엇에 붙으라는 건지 알 수가 없다.
        //
        // ⚠ **판정은 여전히 바닥 도넛이다.** 공은 왜 그 도넛인지를 보여 줄 뿐이고,
        //   공을 따라 때리게 만들면 「그린 것 = 맞는 것」이 깨진다.
        //   원작 시트에도 사슬과 공이 따로 있고 위험 범위는 그 궤도다.
        //
        // 그림은 이미 프로젝트에 있는 것을 쓴다 — `obj_hammer` 와 `obj_hammer_chain`.
        // 새로 받지 않는다.

        private const float OrbitTurns = 2f;      // 정본 「2바퀴」

        private RectTransform _orbitBall, _orbitChain;
        private float _orbitRadius, _orbitAngle;
        private bool _orbitOn;

        private void EnsureOrbit()
        {
            if (_orbitBall != null || _fieldLayer == null) return;

            // ⚠ 사슬은 **한쪽 끝이 보스에 박혀 있어야** 한다. 피벗을 위쪽 가운데로 두면
            //   기본 상태에서 아래로 늘어지고, 그 아래 방향을 공 쪽으로 돌리면 된다.
            //   가운데 피벗으로 두면 보스를 중심으로 막대가 도는 이상한 그림이 된다.
            // 62차에 크러셔 전용 부품이 왔다. 없으면 예전에 빌려 쓰던 망치로 떨어진다 —
            // 다른 보스가 파괴구를 쓰게 되면 제 부품이 올 때까지 그것으로 버틴다.
            _orbitChain = MakeOrbitPart("OrbitChain",
                GetSprite("obj_crusher_chain") ?? GetSprite("obj_hammer_chain"), new Vector2(0.5f, 1f));
            _orbitBall  = MakeOrbitPart("OrbitBall",
                GetSprite("obj_crusher_ball")  ?? GetSprite("obj_hammer"),       new Vector2(0.5f, 0.5f));

            RectTransform MakeOrbitPart(string name, Sprite art, Vector2 pivot)
            {
                var go = new GameObject(name, typeof(RectTransform));
                var rt = (RectTransform)go.transform;
                rt.SetParent(_fieldLayer, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = pivot;
                var img = go.AddComponent<Image>();
                img.raycastTarget = false;
                img.sprite = art;
                img.enabled = art != null;
                img.preserveAspect = true;
                go.SetActive(false);
                return rt;
            }
        }

        private void BeginOrbit(Unit boss, float radiusPx)
        {
            EnsureOrbit();
            if (_orbitBall == null) return;
            _orbitOn = true;
            _orbitRadius = radiusPx;
            _orbitAngle = 0f;
            // 파괴구는 1.5 m 짜리 쇳덩이다. 1 m 로 두면 3.5 m 궤도 위에서 점처럼 보인다.
            float ball = _pxPerMeter * 1.5f;
            _orbitBall.sizeDelta = new Vector2(ball, ball);
            _orbitBall.gameObject.SetActive(true);
            _orbitChain.gameObject.SetActive(true);
            TickOrbit(boss, 0f, 0f);
        }

        /// <param name="progress">예고 진행도 0~1. 이 사이에 <see cref="OrbitTurns"/> 바퀴를 돈다.</param>
        private void TickOrbit(Unit boss, float dt, float progress)
        {
            if (!_orbitOn || _orbitBall == null || boss == null) return;

            _orbitAngle = progress * OrbitTurns * 360f;
            float rad = _orbitAngle * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            var at = boss.Position + dir * _orbitRadius;

            _orbitBall.anchoredPosition = at;

            // 사슬은 보스에서 공까지 늘어난다.
            // 피벗이 위쪽 가운데라 회전 0 일 때 **아래(0,-1)** 로 뻗는다.
            // 그 아래를 방향 dir 에 맞추는 각은 (각도 + 90°) 다 —
            //   회전 θ 에서 아래는 (sinθ, -cosθ) 이고, θ = α + 90° 이면
            //   (sin(α+90), -cos(α+90)) = (cosα, sinα) = dir 이 된다.
            _orbitChain.anchoredPosition = boss.Position;
            _orbitChain.sizeDelta = new Vector2(_pxPerMeter * 0.35f, _orbitRadius);
            _orbitChain.localRotation = Quaternion.Euler(0f, 0f, _orbitAngle + 90f);
        }

        /// <summary>
        /// 때린 뒤에도 잠깐 남는 시간. 0 이면 **맞는 순간 공이 사라져** 아무 일도
        /// 없었던 것처럼 보인다 — 휘두른 것이 끝까지 보여야 맞았다고 읽힌다.
        /// </summary>
        private const float OrbitFollowThrough = 0.35f;

        private float _orbitLinger;

        private void EndOrbit()
        {
            // 예고가 끝나서 지우는 것이면 곧바로 끄지 않고 잠깐 더 돈다.
            if (_orbitOn) { _orbitOn = false; _orbitLinger = OrbitFollowThrough; return; }
            if (_orbitLinger > 0f) return;
            if (_orbitBall != null) _orbitBall.gameObject.SetActive(false);
            if (_orbitChain != null) _orbitChain.gameObject.SetActive(false);
        }

        /// <summary>남은 여운을 굴린다. 매 프레임 부른다.</summary>
        private void TickOrbitLinger(float dt)
        {
            if (_orbitLinger <= 0f) return;
            _orbitLinger -= dt;
            // 여운 동안에도 계속 돈다 — 멈춰 서면 그게 더 이상하다.
            if (_boss != null)
            {
                _orbitAngle += 540f * dt;
                float rad = _orbitAngle * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                _orbitBall.anchoredPosition = _boss.Position + dir * _orbitRadius;
                _orbitChain.anchoredPosition = _boss.Position;
                _orbitChain.localRotation = Quaternion.Euler(0f, 0f, _orbitAngle + 90f);
            }
            if (_orbitLinger <= 0f)
            {
                if (_orbitBall != null) _orbitBall.gameObject.SetActive(false);
                if (_orbitChain != null) _orbitChain.gameObject.SetActive(false);
            }
        }

        private void TickDanger(float dt)
        {
            if (_dangerView == null) return;
            float p = _brain != null ? _brain.TelegraphProgress : 1f;
            _dangerView.Tick(dt, p);
            if (_safeView != null) _safeView.Tick(dt, p);
            if (_hint != null) _hint.Tick(dt, p);
            TickOrbit(_boss, dt, p);
            TickLamp(_boss, p);
        }

        /// <summary>
        /// 보스가 죽었다 — **화면에 남은 보스 것을 전부 지운다.**
        ///
        /// ⚠ `ClearDanger` 만으로는 안 된다. 셋이 따로 살아 있다:
        ///   · 파괴구는 예고가 끝나도 잠깐 더 도는 여운(`_orbitLinger`)이 있고
        ///   · 방패판은 예고가 아니라 `_bossShield` 초 동안 서 있고
        ///   · 이어지는 타격(`_followUp`)은 **보스가 없어도** 예고를 한 번 더 띄운다
        ///   시체도 없는 방에 도형과 화살표가 남으면 "아직 뭐가 오나" 로 읽힌다.
        /// </summary>
        private void ClearBossVisuals()
        {
            ClearDanger();
            ClearFollowUp();

            // 여운을 건너뛰고 곧바로 거둔다 — `EndOrbit` 은 여운을 새로 켠다.
            _orbitLinger = 0f;
            if (_orbitBall != null) _orbitBall.gameObject.SetActive(false);
            if (_orbitChain != null) _orbitChain.gameObject.SetActive(false);

            _bossShield = 0f;
            TickBossShieldView();

            // 돌진 중에 죽으면 시체가 계속 밀려간다.
            if (_brain != null) _brain.BeginCharge(Vector2.zero, 0f);
        }

        private void ClearDanger()
        {
            _danger = default;
            _dangerMove = null;
            if (_dangerView != null) _dangerView.Hide();
            if (_safeView != null) _safeView.Hide();
            if (_hint != null) _hint.Hide();
            EndOrbit();
            EndLamp();
            EndFlight();
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

            // 「마디 사출」로 굴러간 마디는 잡몹도 친다 — 방을 굴러다니는 물건이라
            // 누구 편인지 가리지 않는다. 이 게임에서 보스 공격이 적을 맞히는 유일한 자리다.
            if (m.Draw == BossDraw.SegmentLaunch)
                for (int i = _enemies.Count - 1; i >= 0; i--)
                {
                    var e = _enemies[i];
                    if (e == null || !e.IsAlive || e == boss) continue;
                    if (!_danger.Contains(e.Position, _roomSize)) continue;
                    HitEnemyWith(e, dmg, null);
                }

            // ⚠ **몸이 무엇을 했는지 보여 준다.** 이게 없으면 바닥에만 도형이 뜨고
            //   보스는 가만히 서 있는 것처럼 보인다 — 24패턴이 전부 그랬다.
            //   잡몹 기준 0.17초는 256px 짜리 몸에 너무 짧아 길게 끈다.
            boss.PlayAttack(BossAttackHold);

            Debug.Log($"[진단:발동] {m.NameKr} 맞음={playerHit} 피해={dmg}");
            ApplyMoveEffect(boss, me, m);
            PlayDangerImpact(m);
            // 무엇을 했느냐에 따라 취약 창이 열린다. 그냥 피한 것만으로는 안 열리는 보스가 있다.
            CheckBreak(boss, m, playerHit);

            // 다음에 같은 패턴이 나오면 틈·줄·분면·섬이 한 칸 돌아간다.
            _dangerTick++;
            ClearDanger();
            return true;
        }

        // ── ⑤ 날아오는 것 ────────────────────────────────────────
        //
        // 「어디에 떨어지는가」만 그려서는 **투사체로 안 읽힌다.** 바닥에 원이 생겼다가
        // 터질 뿐이라 무엇이 그것을 만들었는지 알 수 없다.
        //
        // ⚠ 예고 시간과 **정확히 같은 시간** 동안 난다. 도착하는 순간이 곧 발동이다 —
        //   빨리 날면 먼저 도착해 멈춰 서 있고, 늦게 날면 터진 뒤에 도착한다.
        //   둘 다 "저게 왜 저기 있지" 가 된다.
        //
        // 피해는 주지 않는다. 맞고 안 맞고는 **바닥 도형 하나가** 정한다(R1).
        // 이것을 탄으로 만들면 그리는 것과 때리는 것이 둘로 갈라진다.

        private const float MissileArcRatio = 0.22f;   // 포물선 높이 = 거리 x 이 값

        /// <summary>
        /// 부채꼴을 몇 발로 채우는가 — 이 각도마다 한 발.
        /// 30도였을 때 120도 부채꼴이 5발이라 **면이 아니라 선 다섯 개**로 보였다.
        /// 15도면 9발이라 면이 채워진다.
        /// </summary>
        private const float WedgeShotSpacingDeg = 15f;

        /// <summary>한 번에 띄우는 탄의 상한. 넘치면 화면이 탄으로 덮인다.</summary>
        private const int MaxFlight = 16;

        /// <summary>날아가는 것 한 발.</summary>
        private sealed class Flight
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 From, To;
        }

        private readonly List<Flight> _flights = new();
        private readonly List<Vector2> _flightTargets = new();
        private Sprite[] _missileFrames;
        private float _flightLeft, _flightTotal;
        private int _flying;

        /// <summary>
        /// 이 패턴에서 **탄이 어디로 날아가야 하는가**.
        ///
        /// 자리를 여기서 새로 지어내지 않는다. 굳어 있는 도형에게 묻는다 —
        /// 탄이 도착하는 자리와 위험한 자리가 어긋나면
        /// "빨간 데도 아닌 곳에서 터졌다" 가 된다.
        /// </summary>
        private void FlightTargets(BossMove m)
        {
            _flightTargets.Clear();
            switch (m.Draw)
            {
                // 착탄 원 하나마다 탄 하나.
                case BossDraw.MissileSalvo:
                case BossDraw.DebrisFall:
                case BossDraw.BoosterDrop:
                    for (int i = 0; i < _danger.PieceCount && i < MaxFlight; i++)
                        _flightTargets.Add(_danger.PieceAt(i, _roomSize));
                    break;

                // 부채꼴은 **면**이다. 탄 하나로는 그 면이 안 채워진다 —
                // 부채꼴 각도를 따라 촘촘히 뿌려 면을 탄으로 덮는다.
                case BossDraw.Crush:
                case BossDraw.ShieldUp:
                {
                    float deg = _danger.Degrees > 0f ? _danger.Degrees : 180f;
                    int n = Mathf.Clamp(Mathf.RoundToInt(deg / WedgeShotSpacingDeg) + 1, 2, MaxFlight);
                    float half = deg * 0.5f;
                    var d = _danger.Dir;
                    for (int i = 0; i < n; i++)
                    {
                        float a = Mathf.Lerp(-half, half, (float)i / (n - 1)) * Mathf.Deg2Rad;
                        var rot = new Vector2(d.x * Mathf.Cos(a) - d.y * Mathf.Sin(a),
                                              d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a));
                        _flightTargets.Add(_danger.Origin + rot * _danger.Radius);
                    }
                    break;
                }
            }
        }

        /// <summary>도형이 덮는 자리로 탄을 띄운다. 도착하는 순간이 곧 발동이다.</summary>
        private void BeginFlight(BossMove m, Unit boss, float seconds)
        {
            if (_shotLayer == null || boss == null) return;

            FlightTargets(m);
            if (_flightTargets.Count == 0) return;

            if (_missileFrames == null)
            {
                var list = new List<Sprite>(4);
                for (int i = 1; i <= 8; i++)
                {
                    var sp = GetSprite($"shot_missile_{i}");
                    if (sp == null) break;
                    list.Add(sp);
                }
                // 미사일 그림이 없으면 기본 탄으로라도 띄운다 — 안 보이는 것보다 낫다.
                if (list.Count == 0)
                {
                    var fb = GetSprite("shot_1") ?? GetSprite("shot");
                    if (fb != null) list.Add(fb);
                }
                _missileFrames = list.Count > 0 ? list.ToArray() : null;
            }
            if (_missileFrames == null)
            {
                Debug.LogWarning("[진단:비행] 탄 그림을 하나도 못 찾았다 — shot_missile_1 · shot_1 · shot");
                return;
            }

            _flying = Mathf.Min(_flightTargets.Count, MaxFlight);
            _flightTotal = Mathf.Max(0.05f, seconds);
            _flightLeft = _flightTotal;

            while (_flights.Count < _flying) _flights.Add(NewFlight());
            for (int i = 0; i < _flights.Count; i++)
            {
                var f = _flights[i];
                bool on = i < _flying;
                f.Rt.gameObject.SetActive(on);
                if (!on) continue;
                f.From = boss.Position;
                f.To = _flightTargets[i];
                f.Img.sprite = _missileFrames[0];
            }
            TickFlight(0f);   // 첫 프레임부터 제자리에 — (0,0) 에 한 프레임 뜨는 것을 막는다

            Debug.Log($"[진단:비행] {m.NameKr} {_flying}발 {_missileFrames.Length}프레임 " +
                      $"{_flights[0].From} -> {_flights[0].To} {_flightTotal:0.00}초 " +
                      $"활성={_flights[0].Rt.gameObject.activeInHierarchy}");
        }

        private Flight NewFlight()
        {
            var go = new GameObject("BossShot", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_shotLayer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(48f, 48f);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            return new Flight { Rt = rt, Img = img };
        }

        private void EndFlight()
        {
            _flightLeft = 0f;
            for (int i = 0; i < _flights.Count; i++)
                if (_flights[i].Rt != null) _flights[i].Rt.gameObject.SetActive(false);
        }

        /// <summary>지금 t 에서의 자리. 포물선을 그린다 — 직선이면 바닥을 기는 것으로 보인다.</summary>
        private static Vector2 FlightAt(Flight f, float t)
        {
            var p = Vector2.Lerp(f.From, f.To, t);
            float lift = (f.To - f.From).magnitude * MissileArcRatio;
            return p + new Vector2(0f, lift * 4f * t * (1f - t));
        }

        private void TickFlight(float dt)
        {
            if (_flightLeft <= 0f || _flights.Count == 0) return;
            _flightLeft -= dt;

            float t = Mathf.Clamp01(1f - _flightLeft / _flightTotal);
            int frame = _missileFrames != null && _missileFrames.Length > 1
                ? Mathf.Min(_missileFrames.Length - 1, (int)(t * _missileFrames.Length)) : 0;

            for (int i = 0; i < _flying && i < _flights.Count; i++)
            {
                var f = _flights[i];
                var at = FlightAt(f, t);
                f.Rt.anchoredPosition = at;

                // 진행 방향으로 코를 든다. 탄 그림은 오른쪽을 본다(Projectile 과 같은 규약).
                var ahead = FlightAt(f, Mathf.Min(1f, t + 0.03f)) - at;
                if (ahead.sqrMagnitude > 0.0001f)
                    f.Rt.localEulerAngles =
                        new Vector3(0f, 0f, Mathf.Atan2(ahead.y, ahead.x) * Mathf.Rad2Deg);

                if (_missileFrames != null) f.Img.sprite = _missileFrames[frame];
            }

            if (_flightLeft <= 0f) EndFlight();
        }

        /// <summary>도형이 터진 자리에 표시를 남긴다. 무엇이 지나갔는지 보여야 한다.</summary>
        private void PlayDangerImpact(BossMove m)
        {
            // 무엇이 지나갔는지 보여야 한다. 도형이 아니라 **패턴**으로 고른다 —
            // 같은 원이라도 독구름과 파괴구는 다른 것이 터져야 읽힌다.
            string fx = m.Draw switch
            {
                BossDraw.Crush or BossDraw.WreckingBall or BossDraw.HeadBite
                    or BossDraw.BoosterDrop or BossDraw.Emerge or BossDraw.RamCharge => "slam",
                BossDraw.VenomCloud or BossDraw.Spit
                    or BossDraw.CeilingCling or BossDraw.CeilingSpread => "lava",
                BossDraw.Conveyor or BossDraw.SegmentLaunch
                    or BossDraw.DebrisFall or BossDraw.HatchOpen
                    or BossDraw.FullEmergence => "shatter",
                _ => "burst",
            };
            float size = Mathf.Max(96f, _danger.Radius > 0f ? _danger.Radius : _danger.Width);
            var at = _danger.ImpactAt(_roomSize);
            var im = PlayFx(fx, at, size, loop: false);
            Debug.Log($"[진단:임팩트] {fx} 크기={size:0} 자리={at} 생성={(im != null)}");
        }
    }
}
