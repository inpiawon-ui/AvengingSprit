using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 다단 히트 손맛 (PD 2026-10-16 「스킬로 여러 대 때릴 때 느낌 · 다단 히트 느낌을 살려야 · 여러 마리를 때릴 때도, 한 명을 여러 대 때릴 때도」).
    ///
    /// 던파에서 다단 히트가 맛있는 이유 세 가지를 옮긴다.
    ///   ① 한 명을 연달아 — 맞을 때마다 몸이 떨리고(Unit.HitJitter), 숫자가 쌓이고, 그 위에 **합계**가 한 대마다 튀며 커진다.
    ///      연타가 끝나면(0.5초 안 맞으면 · 죽으면) 합계가 크게 한 번 꽂힌다 — 「얼마나 넣었나」가 마지막에 읽힌다.
    ///   ② 여러 마리를 한 번에 — 같은 프레임에 맞은 숫자를 0.03초씩 늦춰 「다다다닥」 물결로 꽂는다.
    ///      한꺼번에 뜨면 숫자 무더기 한 장으로 보인다.
    ///   ③ 많이 맞으면 무겁게 — 한 프레임에 넷 이상 맞으면 화면이 한 번 더 크게 흔들리고 아주 잠깐 늦춰진다.
    ///
    /// 합계는 **스킬을 쓴 뒤 몇 초 동안, 스킬 타격만** 센다 — 평타 연사까지 합계를 띄우면 화면이 숫자로 덮이고 마무리가 안 온다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── ① 합계 ──────────────────────────────────────────────
        private const float ChainGap = 0.5f;          // 이만큼 안 맞으면 연타가 끝난다
        private const int ChainMinHits = 3;           // 세 대째부터 합계를 띄운다
        private const int MaxChains = 8;
        // 여러 마리를 한꺼번에 연타하면(난사 · 회오리) 합계가 적마다 떠서 화면이 숫자로 덮였다(녹화 mh2_thug_u).
        // 먼저 세 대를 채운 둘만 합계를 띄운다 — 나머지는 쌓이는 숫자만으로 충분하다.
        private const int MaxVisibleTotals = 2;
        private const float SkillChainWindow = 6f;    // 액티브 · 필살기를 쓴 뒤 합계를 세는 시간
        private static readonly Color TotalColor = new(1f, 0.97f, 0.8f, 1f);

        private struct HitChain
        {
            public Unit Victim;
            public int Count;
            public int Sum;
            public float Idle;
            public Vector2 Anchor;
            public DamageText Total;
        }

        private readonly HitChain[] _chains = new HitChain[MaxChains];
        private float _skillChainLeft;
        // 평타(PerformAttack) 안인가. 스킬이 끝난 뒤 평타가 합계를 계속 올리면 「다 넣었다」 마무리가 안 나왔다(녹화 mh3_nc_solo)
        private int _basicAttackDepth;

        private void OpenSkillChainWindow() => _skillChainLeft = SkillChainWindow;

        private void NoteHitChain(Unit victim, int damage)
        {
            if (_skillChainLeft <= 0f || victim == null || damage <= 0) return;
            int free = -1;
            for (int i = 0; i < MaxChains; i++)
            {
                if (_chains[i].Victim == victim)
                {
                    ref var c = ref _chains[i];
                    c.Count++;
                    c.Sum = c.Sum > int.MaxValue - damage ? int.MaxValue : c.Sum + damage;
                    c.Idle = 0f;
                    ShowChainTotal(ref c);
                    return;
                }
                if (free < 0 && _chains[i].Victim == null) free = i;
            }
            if (free < 0) return;
            _chains[free] = new HitChain { Victim = victim, Count = 1, Sum = damage, Anchor = victim.Position };
        }

        private void ShowChainTotal(ref HitChain c)
        {
            if (c.Count < ChainMinHits) return;
            string text = c.Sum.ToString();
            if (c.Total != null && c.Total.IsTotal) { c.Total.UpdateTotal(text); return; }
            if (VisibleTotals() >= MaxVisibleTotals) return;
            c.Total = RentDamageText();
            if (c.Total != null) c.Total.ShowTotal(c.Anchor, text, TotalColor);
        }

        private int VisibleTotals()
        {
            int n = 0;
            for (int i = 0; i < _damageTexts.Count; i++)
                if (_damageTexts[i].IsTotal) n++;
            return n;
        }

        private void TickHitChains(float dt)
        {
            if (_skillChainLeft > 0f) _skillChainLeft -= dt;
            for (int i = 0; i < MaxChains; i++)
            {
                ref var c = ref _chains[i];
                if (c.Victim == null && c.Total == null) continue;
                c.Idle += dt;
                if (c.Victim != null && c.Victim.IsAlive && c.Idle <= ChainGap) continue;
                if (c.Total != null && c.Total.IsTotal) c.Total.FinishTotal();
                c = default;
            }
        }

        // ── ② 여러 마리 물결 ─────────────────────────────────────
        private const float RippleStep = 0.03f;
        private const float RippleMax = 0.24f;
        private const int MaxRipples = 32;

        private struct Ripple
        {
            public Vector2 At;
            public int Damage;
            public bool Crit, Weak, Dull;
            public float Left;
        }

        private readonly Ripple[] _ripples = new Ripple[MaxRipples];
        private int _rippleCount;
        private int _rippleFrame = -1;
        private int _rippleIndex;

        /// <summary>이번 프레임 두 번째 이후 숫자면 늦춰 둔다(true). 첫 숫자는 바로 꽂는다.</summary>
        private bool QueueRipple(Vector2 at, int damage, bool crit, bool weak, bool dull)
        {
            if (Time.frameCount != _rippleFrame) { _rippleFrame = Time.frameCount; _rippleIndex = 0; }
            else _rippleIndex++;
            if (_rippleIndex == ManyHitCount - 1) ManyHitImpact();
            if (_rippleIndex == 0 || _rippleCount >= MaxRipples) return false;
            _ripples[_rippleCount++] = new Ripple
            {
                At = at, Damage = damage, Crit = crit, Weak = weak, Dull = dull,
                Left = Mathf.Min(_rippleIndex * RippleStep, RippleMax),
            };
            return true;
        }

        private void TickRipples(float dt)
        {
            for (int i = _rippleCount - 1; i >= 0; i--)
            {
                ref var r = ref _ripples[i];
                r.Left -= dt;
                if (r.Left > 0f) continue;
                var shown = r;
                _ripples[i] = _ripples[--_rippleCount];   // 순서는 상관없다 — 각자 제 시간에 꽂힌다
                ShowDamageNow(shown.At, shown.Damage, true, shown.Crit, shown.Weak, shown.Dull);
            }
        }

        // ── ③ 많이 맞으면 무겁게 ─────────────────────────────────
        private const int ManyHitCount = 4;
        private const float ShakeOnManyHit = 4.5f;
        private const float HitStopOnManyHit = 0.05f;
        // 난사처럼 매 프레임 여럿이 맞는 스킬은 매 프레임 터져 화면이 계속 흔들렸다(녹화 mh2_thug_u) — 실제 시간으로 간격을 둔다
        private const float ManyHitGap = 0.5f;
        private float _manyHitAt = -99f;

        private void ManyHitImpact()
        {
            if (Time.unscaledTime - _manyHitAt < ManyHitGap) return;
            _manyHitAt = Time.unscaledTime;
            Shake(ShakeOnManyHit);
            HitStop(HitStopOnManyHit);
        }

        // ── 틱 · 정리 ───────────────────────────────────────────

        private void TickMultiHit(float dt)
        {
            TickRipples(dt);
            TickHitChains(dt);
        }

        /// <summary>방을 나갈 때 — 숫자는 DamageText.Despawn 으로 이미 꺼졌다. 줄만 비운다.</summary>
        private void ClearMultiHit()
        {
            for (int i = 0; i < MaxChains; i++) _chains[i] = default;
            _rippleCount = 0;
            _skillChainLeft = 0f;
        }
    }
}
