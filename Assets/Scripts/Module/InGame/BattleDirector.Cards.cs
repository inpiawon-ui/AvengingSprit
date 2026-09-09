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

        private const float ShieldOrbitRadiusMeters = 1.6f;
        private const float ShieldOrbitDegPerSecond = 110f;
        private const float ShieldRespawnSeconds = 8f;
        private const float ShieldHitRadius = 46f;
        private const float ShieldDamageMul = 0.8f;

        private readonly List<RectTransform> _shieldViews = new();
        private readonly List<float> _shieldDownLeft = new();   // 0 이면 살아 있다
        private float _shieldAngle;

        private void TickOrbitShields(float dt)
        {
            int want = _buffs.OrbitShields;
            if (want <= 0) { ClearOrbitShields(); return; }

            var me = Avatar;
            if (me == null) { ClearOrbitShields(); return; }

            EnsureShieldViews(want);
            _shieldAngle = Mathf.Repeat(_shieldAngle + ShieldOrbitDegPerSecond * dt, 360f);

            float r = ShieldOrbitRadiusMeters * _pxPerMeter;
            int dmg = Mathf.Max(1, Mathf.RoundToInt(me.Atk * _buffs.AttackMul * ShieldDamageMul));

            for (int i = 0; i < want; i++)
            {
                var view = _shieldViews[i];
                if (view == null) continue;

                if (_shieldDownLeft[i] > 0f)
                {
                    _shieldDownLeft[i] -= dt;
                    if (_shieldDownLeft[i] > 0f) { view.gameObject.SetActive(false); continue; }
                    view.gameObject.SetActive(true);
                }

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
                    _shieldDownLeft[i] = ShieldRespawnSeconds;
                    view.gameObject.SetActive(false);
                    break;
                }
            }
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

        /// <summary>판이 새로 시작할 때. 방패는 방을 따라다니지만 판은 안 넘는다.</summary>
        private void ClearCardRuntime()
        {
            ClearOrbitShields();
            _guardCooldownLeft = 0f;
            _shieldAngle = 0f;
        }

        private void TickCards(float dt)
        {
            if (_guardCooldownLeft > 0f) _guardCooldownLeft -= dt;
            TickOrbitShields(dt);
        }
    }
}
