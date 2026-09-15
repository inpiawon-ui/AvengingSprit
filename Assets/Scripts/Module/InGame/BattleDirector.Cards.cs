using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 특성 카드 10종 중 **새로 만든 여섯 가지**가 사는 곳 (2026-09-09).
    ///
    /// 나머지 넷(감전·흡혼·추가 발사·화염 각인)은 이미 있던 통로를 그대로 탄다 —
    /// 새로 만들 이유가 없어 여기에 없다.
    ///
    /// 카드 종류는 앞으로 더 늘어난다. 늘어나는 것을 여기에 이어 붙이면
    /// "카드가 무엇을 하는가" 가 한 파일에 모인다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 궁지 — 체력이 절반 아래면 세진다 ─────────────────────
        //
        // 「죽기 직전이 제일 세다」는 값이라 **체력 비율로만** 켜고 끈다.
        // 몸이 없으면(유령) 공격 자체를 안 하므로 볼 필요가 없다.

        private const float GritHpRatio = 0.5f;

        private float GritDamageMul
        {
            get
            {
                if (_buffs.GritMul <= 1f || _host == null) return 1f;
                return _host.Hp <= _host.HpMax * GritHpRatio ? _buffs.GritMul : 1f;
            }
        }

        // ── 처형 — 약해진 적을 단칼에 ────────────────────────────
        //
        // ⚠ **보스는 제외한다.** 보스에게 확률 즉사를 열어 두면 보스전이
        //   싸움이 아니라 뽑기가 된다 — 몇 초 만에 끝나거나 평소대로 길거나.
        // ⚠ 체력이 이미 낮은 적에게만 건다. 만피에서 터지면 「왜 죽었지」가 되고,
        //   낮을 때 터지면 「마무리했다」로 읽힌다. 같은 확률이라도 뜻이 다르다.

        private const float AssassinateHpRatio = 0.30f;

        /// <summary>즉사가 걸렸으면 true. 부르는 쪽이 곧바로 죽인다.</summary>
        private bool TryAssassinate(Unit victim)
        {
            if (SandboxBlocksExecute) return false;   // Sandbox — 지울 때 이 줄도 함께
            if (_buffs.AssassinatePercent <= 0) return false;
            if (victim == null || !victim.IsAlive || victim.IsDying) return false;
            if (victim.IsBoss) return false;
            if (victim.Hp > victim.HpMax * AssassinateHpRatio) return false;
            if (_rng.Next(100) >= _buffs.AssassinatePercent) return false;

            SpawnImpact(victim.Position, "shuriken");
            ShowDamage(victim.Position, victim.Hp, toEnemy: true);
            return true;
        }

        // ── 찰나의 불사 — 맞는 순간 2초 ──────────────────────────
        //
        // ⚠ 확률로 두지 않았다. 「가끔」을 운에 맡기면 안전할 때 터지고
        //   죽을 때 안 터진다 — 살려 주는 카드인데 살려 주는 순간을 못 고른다.
        //   **맞는 그 순간** 켜고 쿨다운으로 잦기를 막는 편이 정직하다.

        private const float GuardInvulnSeconds = 2f;
        private float _guardCooldownLeft;

        /// <summary>막았으면 true — 이번 피해는 통째로 없던 일이 된다.</summary>
        private bool TryGuardInvuln()
        {
            if (_buffs.GuardCooldown <= 0f || _guardCooldownLeft > 0f) return false;

            _guardCooldownLeft = _buffs.GuardCooldown;
            _invuln = Mathf.Max(_invuln, GuardInvulnSeconds);
            var at = Avatar != null ? Avatar.Position : Vector2.zero;
            PlayFx("shield", at, 160f, loop: false);
            return true;
        }

        // ── 수호 방패 — 내 주위를 돈다 ───────────────────────────
        //
        // 탑뷰라 사방에서 온다. 지금 근접을 막을 수단이 사실상 없어서
        // 「가까이 붙는 것」에 값을 매기는 카드가 하나 필요했다.
        //
        // 방패는 닿으면 **터지고 잠시 뒤 돌아온다.** 안 없어지면 근접이 아예
        // 성립하지 않고, 영영 없어지면 방 하나 쓰고 끝나는 카드가 된다.

        // 2026-09-09 — 반지름 1.6 m 는 몸에서 너무 떨어져 「내 방패」로 안 읽혔고,
        // 110°/s 는 한 바퀴에 3.3 초라 반대편이 늘 비어 있었다.
        // 1.05 m · 210°/s 로 좁히고 올린다 — 한 바퀴 1.7 초.
        private const float ShieldOrbitRadiusMeters = 1.05f;
        private const float ShieldOrbitDegPerSecond = 210f;
        /// <summary>
        /// 다 깨지고 나서 **한꺼번에** 돌아오기까지의 시간.
        ///
        /// 예전에는 하나씩 따로 8초를 셌다. 그러면 늘 한두 개만 도는 상태가 이어져
        /// 「방패가 몇 개인지」가 화면에서 안 읽힌다. 셋이 다 깨져야 셋이 함께 온다 —
        /// 있을 때는 온전하고 없을 때는 아예 없는 편이 판단하기 쉽다.
        /// </summary>
        private const float ShieldRespawnSeconds = 6f;

        /// <summary>다 깨진 뒤 함께 돌아오기까지 남은 시간. 0 이면 서 있다.</summary>
        private float _shieldWaveLeft;
        private const float ShieldHitRadius = 46f;
        private const float ShieldDamageMul = 0.8f;

        private readonly List<RectTransform> _shieldViews = new();
        private readonly List<float> _shieldDownLeft = new();   // 0 이면 살아 있다
        private float _shieldAngle;

        private void TickOrbitShields(float dt)
        {
            int want = _buffs.OrbitShields;
            if (want <= 0) { ClearOrbitShields(); return; }

            // ⚠ **유령에게는 안 붙는다.** 몸이 없을 때 방패가 돌면 화면에서는
            //   「죽었는데 방어막이 남아 있다」로 보인다. 방패는 몸에 딸린 것이다.
            var me = _host;
            if (me == null || !me.IsAlive) { HideOrbitShields(); return; }

            EnsureShieldViews(want);

            // 다 깨져 있는 동안은 시계만 돈다. 다 차면 셋이 함께 선다.
            if (_shieldWaveLeft > 0f)
            {
                _shieldWaveLeft -= dt;
                if (_shieldWaveLeft > 0f)
                {
                    for (int i = 0; i < _shieldViews.Count; i++)
                        if (_shieldViews[i] != null) _shieldViews[i].gameObject.SetActive(false);
                    return;
                }
                for (int i = 0; i < _shieldDownLeft.Count; i++) _shieldDownLeft[i] = 0f;
            }
            _shieldAngle = Mathf.Repeat(_shieldAngle + ShieldOrbitDegPerSecond * dt, 360f);

            float r = ShieldOrbitRadiusMeters * _pxPerMeter;
            int dmg = Mathf.Max(1, Mathf.RoundToInt(me.Atk * _buffs.AttackMul * ShieldDamageMul));

            for (int i = 0; i < want; i++)
            {
                var view = _shieldViews[i];
                if (view == null) continue;

                // 깨진 것은 그대로 누워 있는다. 되살아나는 것은 **셋이 다 깨졌을 때**뿐이다.
                if (_shieldDownLeft[i] > 0f) { view.gameObject.SetActive(false); continue; }
                view.gameObject.SetActive(true);

                float deg = _shieldAngle + 360f * i / want;
                var at = me.Position + Rotate(Vector2.up, deg) * r;
                view.anchoredPosition = at;

                // 닿은 적 하나에게 한 번. 여럿을 한 번에 치면 방패가 광역기가 된다.
                for (int k = 0; k < _enemies.Count; k++)
                {
                    var e = _enemies[k];
                    if (e == null || !e.IsAlive || e.IsDying) continue;
                    if (Vector2.Distance(e.Position, at) > ShieldHitRadius) continue;

                    HitEnemyWith(e, dmg, null);
                    SpawnImpact(at, "pulse");
                    _shieldDownLeft[i] = 1f;   // 「깨졌다」 표시. 시계는 아래에서 한꺼번에 센다
                    view.gameObject.SetActive(false);
                    break;
                }
            }

            // 다 깨졌으면 그때부터 함께 돌아올 시계를 켠다.
            if (_shieldWaveLeft <= 0f)
            {
                bool allDown = true;
                for (int i = 0; i < want && allDown; i++)
                    if (_shieldDownLeft[i] <= 0f) allDown = false;
                if (allDown) _shieldWaveLeft = ShieldRespawnSeconds;
            }
        }

        /// <summary>그림만 감춘다 — 몸을 되찾으면 그대로 다시 선다.</summary>
        private void HideOrbitShields()
        {
            for (int i = 0; i < _shieldViews.Count; i++)
                if (_shieldViews[i] != null) _shieldViews[i].gameObject.SetActive(false);
        }

        private void EnsureShieldViews(int want)
        {
            while (_shieldViews.Count < want)
            {
                var sprite = GetSprite("fx_ward_1");
                var go = new GameObject($"OrbitShield{_shieldViews.Count}",
                                        typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_unitLayer, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(64f, 64f);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.sprite = sprite;
                // 그림이 아직 없어도 자리는 돌아야 한다 — 옅은 원으로 대신한다.
                img.color = sprite != null ? Color.white : new Color(0.45f, 0.85f, 1f, 0.75f);
                _shieldViews.Add(rt);
                _shieldDownLeft.Add(0f);
            }
            // 카드가 빠질 일은 없지만(판 안에서 레벨은 내려가지 않는다) 남는 것은 끈다
            for (int i = want; i < _shieldViews.Count; i++)
                if (_shieldViews[i] != null) _shieldViews[i].gameObject.SetActive(false);
        }

        private void ClearOrbitShields()
        {
            for (int i = 0; i < _shieldViews.Count; i++)
                if (_shieldViews[i] != null) Destroy(_shieldViews[i].gameObject);
            _shieldViews.Clear();
            _shieldDownLeft.Clear();
        }

        // ── 번개 줄기 — 「감전」이 어디로 튀었는지 보이게 ──────────
        //
        // 감전·번개 사슬은 계산은 맞는데 **화면에 아무것도 안 나갔다.** 맞은 자리에
        // 작은 `pulse` 만 찍혀서, 옆 적이 왜 깎였는지 알 수가 없었다 —
        // 「전기 스킬을 배웠는데 전기가 안 나간다」는 보고가 그래서 나왔다.
        //
        // 튄 두 점을 **줄기로 잇는다.** 지그재그 세 마디로 꺾어 번개로 읽히게 하고
        // 짧게 번쩍였다 사라진다. 그림 파일 없이 흰 사각형을 돌려 늘여 쓴다.

        // 2026-09-10 — 흰 사각형을 늘여 쓰던 것을 **그림으로** 바꿨다.
        // 「전기 느낌이 아니고 그냥 줄만 나가는 느낌」이라 `fx_bolt_1~4` 를 받았다.
        // 넉 장을 조각마다 다르게 골라 쓰면 한 번 칠 때마다 다른 줄기가 된다.
        private const float BoltSeconds = 0.22f;
        private const float BoltThickness = 30f;   // 그림 세로 64 를 화면에 이만큼으로
        private const int BoltSegments = 3;
        private const float BoltJitter = 14f;
        private const int BoltFrameCount = 4;
        private static readonly Color BoltColor = Color.white;

        private Sprite[] _boltSprites;

        /// <summary>번개 그림 넉 장. 한 번만 찾아 둔다 — 매번 찾으면 Sprite 가 쌓인다.</summary>
        private Sprite BoltSprite(int i)
        {
            if (_boltSprites == null)
            {
                _boltSprites = new Sprite[BoltFrameCount];
                for (int k = 0; k < BoltFrameCount; k++)
                    _boltSprites[k] = GetSprite($"fx_bolt_{k + 1}");
            }
            return _boltSprites[Mathf.Abs(i) % BoltFrameCount];
        }

        private readonly List<RectTransform> _bolts = new();
        private readonly List<float> _boltLeft = new();

        /// <summary>두 점을 번개로 잇는다. 지그재그라 마디마다 한 조각씩 쓴다.</summary>
        private int _boltFrameSeed;

        private void DrawBolt(Vector2 from, Vector2 to)
        {
            _boltFrameSeed++;   // 칠 때마다 다른 장에서 시작한다
            var dir = (to - from);
            if (dir.sqrMagnitude < 1f) return;
            dir = dir.normalized;
            var side = new Vector2(-dir.y, dir.x);

            var prev = from;
            for (int i = 1; i <= BoltSegments; i++)
            {
                var next = Vector2.Lerp(from, to, (float)i / BoltSegments);
                // 마디를 진행 방향과 직각으로 흔든다. 마지막 마디는 정확히 목표에 닿는다.
                if (i < BoltSegments)
                    next += side * ((float)_rng.NextDouble() * 2f - 1f) * BoltJitter;
                SpawnBoltPiece(prev, next);
                prev = next;
            }
        }

        private void SpawnBoltPiece(Vector2 a, Vector2 b)
        {
            int at = -1;
            for (int i = 0; i < _bolts.Count; i++)
                if (_boltLeft[i] <= 0f && _bolts[i] != null) { at = i; break; }

            if (at < 0)
            {
                var go = new GameObject($"Bolt{_bolts.Count}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_unitLayer, false);
                var made = (RectTransform)go.transform;
                made.anchorMin = made.anchorMax = new Vector2(0f, 1f);
                made.pivot = new Vector2(0f, 0.5f);   // 왼쪽 끝·세로 가운데가 기준 (심이 지나는 줄)
                var made_img = go.GetComponent<Image>();
                made_img.raycastTarget = false;
                at = _bolts.Count;
                _bolts.Add(made);
                _boltLeft.Add(0f);
            }

            var rt = _bolts[at];
            var d = b - a;
            rt.gameObject.SetActive(true);
            rt.anchoredPosition = a;
            rt.sizeDelta = new Vector2(d.magnitude, BoltThickness);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var img = rt.GetComponent<Image>();
            // 조각마다 다른 장을 쓴다 — 같은 장을 이어 붙이면 무늬가 반복돼 눈에 띈다.
            img.sprite = BoltSprite(at + _boltFrameSeed);
            img.color = BoltColor;
            _boltLeft[at] = BoltSeconds;
        }

        private void TickBolts(float dt)
        {
            for (int i = 0; i < _bolts.Count; i++)
            {
                if (_boltLeft[i] <= 0f) continue;
                _boltLeft[i] -= dt;
                var rt = _bolts[i];
                if (rt == null) continue;
                if (_boltLeft[i] <= 0f) { rt.gameObject.SetActive(false); continue; }
                // 끝으로 갈수록 옅어진다 — 번쩍이고 사라지는 것으로 읽힌다
                var c = BoltColor;
                c.a = _boltLeft[i] / BoltSeconds;
                rt.GetComponent<Image>().color = c;
            }
        }

        private void ClearBolts()
        {
            for (int i = 0; i < _bolts.Count; i++)
                if (_bolts[i] != null) Destroy(_bolts[i].gameObject);
            _bolts.Clear();
            _boltLeft.Clear();
        }

        /// <summary>판이 새로 시작할 때. 방패는 방을 따라다니지만 판은 안 넘는다.</summary>
        private void ClearCardRuntime()
        {
            ClearOrbitShields();
            ClearBolts();
            _guardCooldownLeft = 0f;
            _shieldAngle = 0f;
            _shieldWaveLeft = 0f;
        }

        private void TickCards(float dt)
        {
            if (_guardCooldownLeft > 0f) _guardCooldownLeft -= dt;
            TickOrbitShields(dt);
            TickBolts(dt);
        }
    }
}
