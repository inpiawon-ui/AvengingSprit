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
    /// 이제 하나다 — **조준 표적이 조여들며 붙고, 숨 쉬듯 커졌다 작아지고, 색이 뜻을 말한다.**
    ///   붉은색 = 내가 찍었다(표식) · 보라 = 저주 · 청록 = 보스가 노린다
    ///
    /// ⚠ 크기는 **덩치에 맞춘다.** 큰 몸에 작은 고리를 얹으면 가슴에 단 배지처럼 보인다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>표적 기본 지름. 덩치가 작아도 이보다 작아지지 않는다.</summary>
        private const float ReticleMinSize = 64f;

        private const float ReticleMaxSize = 220f;

        /// <summary>
        /// 몸 반지름 대비 표적 지름. `BodyRadius` 는 몸 폭의 0.42 배이므로
        /// **2.0 이면 몸 폭의 84%** — 캐릭터보다 살짝 작게 얹힌다(기획 2026-09-21).
        /// 크게 두면 표적이 캐릭터를 잡아먹어 누가 찍혔는지가 오히려 흐려진다.
        /// </summary>
        private const float ReticleBodyRatio = 2.0f;

        // ⚠ **돌리지 않는다**(기획 2026-09-21). 조준 기호는 각이 서 있어야 조준으로 읽히는데,
        //   돌리면 그 각이 계속 어긋나 «돌아가는 장식»이 된다. 살아 있다는 느낌은 맥박이 낸다.

        /// <summary>숨 쉬듯 커졌다 작아진다. 한 바퀴에 걸리는 시간과 폭.</summary>
        private const float ReticlePulseMin = 0.82f;
        private const float ReticlePulseMax = 1.08f;
        private const float ReticlePulseSeconds = 0.75f;

        /// <summary>조여들기 시작하는 배율과 걸리는 시간.</summary>
        private const float ReticleSnapFrom = 2.3f;
        private const float ReticleSnapSeconds = 0.18f;

        /// <summary>
        /// 갈래마다 **제 그림**을 쓴다.
        ///
        /// ⚠ 색을 곱해 가르지 않는다. 표적은 불길로 칠해져 있어서, 거기에 보라를 곱하면
        ///   밝은 데는 회색이 되고 어두운 데는 검게 죽어 **타오르는 느낌이 사라진다.**
        ///   `Tools/mark_hue.py` 로 색조만 돌려 구운 한 벌을 쓴다(붉은 불 · 보라 · 청록).
        /// </summary>
        private static string FxNameOf(ReticleKind kind) => kind switch
        {
            ReticleKind.Curse    => "markcurse",
            ReticleKind.BossLock => "markboss",
            _                    => "mark",
        };

        /// <summary>찍히는 순간 튀는 알갱이의 색. 그림과 같은 계열로 맞춘다.</summary>
        private static ParticleElement ElementOf(ReticleKind kind) => kind switch
        {
            ReticleKind.Curse    => ParticleElement.Curse,
            ReticleKind.BossLock => ParticleElement.Ice,
            _                    => ParticleElement.Fire,
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
            var element = ElementOf(kind);
            // 전용 그림이 아직 없으면 붉은 본판으로 떨어진다 — 표시가 사라지는 것보다 낫다
            var im = TakeLoopFx(FxNameOf(kind), at, size) ?? TakeLoopFx("mark", at, size);
            if (im != null)
            {
                im.SetSnap(ReticleSnapFrom, ReticleSnapSeconds);   // 조여들며 붙는다
                im.SetPulse(ReticlePulseMin, ReticlePulseMax, ReticlePulseSeconds);
            }
            if (ping)
            {
                // 조이는 고리와 반짝임 — 어디가 찍혔는지 눈이 따라간다
                _pfx?.Ring(at, element, size * 1.5f);
                _pfx?.Sparkle(at, element, 0.7f);
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
