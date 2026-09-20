using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>표적이 말하는 것. 색만 다르고 그림·움직임은 같다.</summary>
    public enum ReticleKind
    {
        /// <summary>갱스터 표식 — 더 아프게 맞고, 죽을 때 20% 로 즉사한다.</summary>
        Mark,
        /// <summary>저주 — 죽으면 옆으로 옮겨 붙는다.</summary>
        Curse,
        /// <summary>보스 조준 — **여기로 온다.** 피하라는 뜻이다.</summary>
        BossLock,
    }

    /// <summary>
    /// 표적 한 벌 (2026-09-20).
    ///
    /// ── 왜 묶었나 ────────────────────────────────────────────
    /// 같은 «찍혔다»를 말하는 표시가 세 갈래로 흩어져 있었다 —
    /// 갱스터 스킬만 조준환을 붙여 돌렸고, **저주 전이는 아무것도 안 그렸으며**,
    /// 보스 조준은 같은 그림을 색도 없이 띄웠다. 플레이어가 배워야 할 규칙이 셋이 된다.
    ///
    /// 이제 하나다 — **조준환이 조여들며 붙고, 천천히 돌고, 색이 뜻을 말한다.**
    ///   붉은색 = 내가 찍었다(표식) · 보라 = 저주 · 주황 = 보스가 노린다
    ///
    /// ⚠ 크기는 **덩치에 맞춘다.** 큰 몸에 작은 고리를 얹으면 가슴에 단 배지처럼 보인다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>표적 기본 지름. 덩치가 작아도 이보다 작아지지 않는다.</summary>
        private const float ReticleMinSize = 96f;

        private const float ReticleMaxSize = 230f;

        /// <summary>몸 반지름 대비 표적 지름.</summary>
        private const float ReticleBodyRatio = 2.6f;

        /// <summary>도는 속도(초당 도). 느리게 — 빠르면 «돌아가는 장식»이 된다.</summary>
        private const float ReticleSpin = 42f;

        /// <summary>조여들기 시작하는 배율과 걸리는 시간.</summary>
        private const float ReticleSnapFrom = 2.3f;
        private const float ReticleSnapSeconds = 0.18f;

        private static Color ColorOf(ReticleKind kind) => kind switch
        {
            ReticleKind.Curse    => new Color(0.78f, 0.45f, 1f),
            ReticleKind.BossLock => new Color(1f, 0.66f, 0.2f),
            _                    => new Color(1f, 0.35f, 0.3f),
        };

        /// <summary>덩치에 맞춘 표적 지름.</summary>
        private static float ReticleSizeFor(float bodyRadius)
            => Mathf.Clamp(bodyRadius * ReticleBodyRatio, ReticleMinSize, ReticleMaxSize);

        /// <summary>
        /// 표적을 띄운다. 돌려주는 것은 부른 쪽이 들고 있다가 거둔다
        /// (몸에 붙는 것은 <see cref="_markFx"/> 가 대신 따라다닌다).
        /// </summary>
        private Impact ShowReticle(Vector2 at, float size, ReticleKind kind, bool ping = true)
        {
            var color = ColorOf(kind);
            var im = TakeLoopFx("mark", at, size);
            if (im != null)
            {
                im.SetTint(color);
                im.SetSpin(ReticleSpin);
                im.SetSnap(ReticleSnapFrom, ReticleSnapSeconds);   // 조여들며 붙는다
                im.SetPulse(SkillPulseMin, 1f, SkillPulseSeconds);
            }
            if (ping)
            {
                // 조이는 고리와 반짝임 — 어디가 찍혔는지 눈이 따라간다
                _pfx?.Ring(at, color, size * 1.5f);
                _pfx?.Sparkle(at, color, 0.7f);
            }
            return im;
        }

        /// <summary>몸에 붙는 표적. 표식이 걸린 내내 따라다니다 시간이 끝나면 사라진다.</summary>
        private void ShowReticleOn(Unit target, ReticleKind kind, float seconds)
        {
            if (target == null || !target.IsAlive) return;

            // ⚠ 이미 붙어 있으면 **시간만 늘린다.** 하나 더 띄우면 고리 두 개가 겹쳐 돌아
            //   두 번 찍힌 것처럼 보이고, 먼저 것을 거둘 길도 없어진다.
            for (int i = 0; i < _markFx.Count; i++)
                if (_markFx[i].U == target)
                {
                    var (u, fx, life) = _markFx[i];
                    _markFx[i] = (u, fx, Mathf.Max(life, seconds));
                    return;
                }

            var im = ShowReticle(target.Position, ReticleSizeFor(target.BodyRadius), kind);
            if (im != null) _markFx.Add((target, im, seconds));
        }
    }
}
