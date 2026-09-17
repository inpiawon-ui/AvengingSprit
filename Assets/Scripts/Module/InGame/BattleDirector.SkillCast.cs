using System.Collections.Generic;
using Game.Module.Events;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 스킬 시전 연출 — 필드 쪽 (2026-09-17, 시안 `AVSR_SkillCast_draft`).
    ///
    /// 스킬 효과가 스킬마다 제각각이라 **「지금 스킬을 썼다」는 순간**이 화면에 안 잡혔다.
    /// 모든 몸이 **같은 순서**로 터지고, 색만 몸마다 다르다.
    ///
    ///   누름      몸이 하얗게 번쩍 · 몸 둘레로 빛줄기 · 발밑에 흰 타원 링이 퍼짐
    ///   컷인 중   **전투가 멈춘다.** 발밑에 몸 색 타원 두 겹이 깔려 있다
    ///   컷인 끝   스킬이 **이제** 나간다 · 발밑에서 흰 타원 링이 한 번 더 퍼지고 흐려진다
    ///
    /// ── 왜 컷인이 끝난 뒤에 나가나 (기획 2026-09-17) ─────────────
    /// 누르는 순간 스킬이 나가고 컷인은 따로 떠 있었더니, 컷인에 눈이 가 있는 사이
    /// 스킬이 이미 끝나 **무엇이 나갔는지 전혀 몰랐다.** 컷인 동안 전투를 세워 두고
    /// 띠가 빠지는 순간 터뜨린다 — 격투 게임 필살기처럼 컷인 때문에 손해 보는 일이 없다.
    ///
    /// ── 왜 이펙트가 이것뿐인가 ─────────────────────────────────
    /// 처음엔 빛 · 폭발 · 링 · 마법진을 다 얹었더니 「너무 과하다, 원작처럼 심플하게」였다.
    /// 시안의 타원 링 · 빛줄기만 남긴다.
    ///
    /// ⚠ **스킬 효과 코드는 건드리지 않는다.** 부르는 시점만 컷인 뒤로 옮겼다.
    /// ⚠ 그림은 전부 흰색으로 받았다. 색은 `GameConfig.CastColorOf` 가 입힌다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>
        /// 컷인 동안 전투를 세워 두는 시간(실제 시간). 화면 쪽 컷인이 들어와 서 있는 시간
        /// (`InGameMainUI.CutInSlideSeconds + CutInHoldSeconds`)과 같아야 띠가 빠지는 순간 터진다.
        /// </summary>
        private const float CastFreezeSeconds = 0.63f;

        private const float CastLinesSize = 170f;
        private const float CastRingInner = 150f;
        private const float CastRingOuter = 240f;
        private const float CastReleaseRingSize = 230f;
        private const float CastFootDrop = 34f;      // 몸 중심 → 발밑
        private const float CastRingSquash = 0.42f;  // 바닥에 누운 타원
        private const float CastShake = 3f;

        /// <summary>
        /// 발밑에 깔리는 그림(링) 전용 자리. 탄 레이어는 몸보다 **위**라
        /// 발밑 링이 몸을 덮어 버린다 — 장판 레이어(몸 아래)에 따로 둔다.
        /// </summary>
        private readonly List<Impact> _castFloorFx = new();

        private Unit _castPending;
        private float _castFreezeLeft;
        private Color _castColor = Color.white;
        private Impact _castRingA;
        private Impact _castRingB;
        private Sprite[] _castRingStill;

        /// <summary>컷인이 떠 있어 전투가 멈춰 있는가.</summary>
        private bool IsCastFrozen => _castFreezeLeft > 0f;

        /// <summary>
        /// 스킬 버튼이 눌렸다. 봉인 · 쿨 검사를 통과한 뒤다.
        /// 연출을 시작하고 전투를 세운다. 스킬은 <see cref="TickCastFreeze"/> 가 컷인 끝에 낸다.
        /// </summary>
        private void BeginCast(Unit me)
        {
            if (me == null) return;
            // 아마존 정예는 스킬 보류 — 아무 일도 안 일어나는데 연출만 터지면 거짓말이다
            if (me.Key == "amazon_elite") { CastHostSkill(me); return; }

            _castColor = _config != null ? _config.CastColorOf(me.Key) : Color.white;
            var body = me.Position;
            var feet = body - Vector2.up * CastFootDrop;

            me.BeginCastWhite();   // 몸 모양 그대로 하얗게 — 시안 「몸이 하얗게 번쩍」

            // 몸 둘레로 뻗는 흰 빛줄기 — 시안 1컷
            var lines = PlayFx("castburst", body, CastLinesSize, loop: false);
            if (lines != null) lines.SetFrameSeconds(0.06f);

            // 발밑에서 퍼지는 흰 타원
            var pop = PlayFloorFx("castring", feet, CastReleaseRingSize, loop: false);
            if (pop != null) { pop.SetFrameSeconds(0.06f); pop.SetSquash(CastRingSquash); }

            // 컷인 동안 발밑에 깔려 있는 몸 색 타원 두 겹 — 시안 2컷
            _castRingStill ??= StillRing();
            if (_castRingStill != null)
            {
                _castRingA = PlayFloorStill(feet, CastRingInner, 0.95f);
                _castRingB = PlayFloorStill(feet, CastRingOuter, 0.7f);
            }

            _castPending = me;
            _castFreezeLeft = CastFreezeSeconds;

            var skill = SkillEntryOf(me.Key);
            // 영어 화면은 큰 글씨가 곧 영문 이름이다. 번역 표에 영어 칸이 비어 있어
            // `DisplayName` 이 한국어로 떨어졌다 — 표의 영문 이름을 쓰고 아래 줄은 비운다.
            bool english = global::Game.Module.Common.Localize.Current == global::Game.Module.Common.Language.English;
            string bigName = skill == null ? string.Empty
                : english || string.IsNullOrEmpty(skill.DisplayName) ? skill.NameEn : skill.DisplayName;
            _bus.Publish(new SkillCastEvent
            {
                CastHostKey = me.Key,
                SkillName = bigName,
                SkillNameEn = english ? string.Empty : skill?.NameEn ?? string.Empty,
                CastColor = _castColor,
            });
        }

        /// <summary>
        /// 컷인 동안 전투 대신 돈다. 멈춘 판에서도 연출(링 · 몸 번쩍임)은 움직여야 한다.
        /// ⚠ 실제 시간으로 잰다 — 컷인 화면도 실제 시간으로 돈다.
        /// </summary>
        private void TickCastFreeze()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _impacts.Count; i++) _impacts[i].Tick(dt);
            TickCastPresentation(dt);
            if (_castPending != null) _castPending.TickFlash(dt);

            _castFreezeLeft -= dt;
            if (_castFreezeLeft > 0f) return;
            _castFreezeLeft = 0f;
            ReleaseCast();
        }

        /// <summary>컷인이 끝났다 — 스킬을 낸다.</summary>
        private void ReleaseCast()
        {
            var me = _castPending;
            _castPending = null;
            _castRingA?.Stop();
            _castRingB?.Stop();
            _castRingA = _castRingB = null;

            // 멈춰 있는 사이 몸이 바뀌었으면(있어선 안 되지만) 나가지 않는다
            if (me == null || !me.IsAlive || me != _host) return;

            var feet = me.Position - Vector2.up * CastFootDrop;
            // 흰 타원이 한 번 퍼지고, 안쪽 몸 색 타원이 흐려진다 — 시안 3컷
            var pop = PlayFloorFx("castring", feet, CastReleaseRingSize, loop: false);
            if (pop != null) { pop.SetFrameSeconds(0.07f); pop.SetSquash(CastRingSquash); }
            var fade = PlayFloorStill(feet, CastRingInner, 0.9f);
            fade?.SetLife(0.45f, 0.45f);

            Shake(CastShake);
            CastHostSkill(me);
        }

        /// <summary>링 넷째 장 대신 **가장 가는 둘째 장** 한 장을 멈춰 둔 그림으로 쓴다.</summary>
        private Sprite[] StillRing()
        {
            var frames = FxFrames("castring");
            return frames != null && frames.Length > 1 ? new[] { frames[1] } : frames;
        }

        private Impact PlayFloorStill(Vector2 at, float size, float alpha)
        {
            if (_castRingStill == null) return null;
            var im = TakeFloorFx(size);
            if (im == null) return null;
            im.Play(at, _castRingStill, size, loop: true);
            im.SetTint(new Color(_castColor.r, _castColor.g, _castColor.b, alpha));
            im.SetSquash(CastRingSquash);
            return im;
        }

        /// <summary>이 몸의 시전 색. 화면 쪽 준비 빛도 같은 색을 쓴다.</summary>
        public Color CastColorOf(string hostKey)
            => _config != null ? _config.CastColorOf(hostKey) : Color.white;

        private global::Game.Character.ActiveSkillEntry SkillEntryOf(string hostKey)
        {
            if (_player == null || string.IsNullOrEmpty(hostKey)) return null;
            var host = _player.GetHost(hostKey);
            return host != null ? _player.GetActiveSkill(host.ActiveSkillKey) : null;
        }

        private Impact PlayFloorFx(string name, Vector2 at, float size, bool loop)
        {
            var frames = FxFrames(name);
            if (frames == null) return null;
            var im = TakeFloorFx(size);
            im?.Play(at, frames, size, loop);
            return im;
        }

        private Impact TakeFloorFx(float size)
        {
            if (_fieldLayer == null) return null;
            for (int i = 0; i < _castFloorFx.Count; i++)
                if (!_castFloorFx[i].IsActive) return _castFloorFx[i];
            var go = new GameObject($"CastFloorFx_{_castFloorFx.Count}", typeof(RectTransform));
            var im = go.AddComponent<Impact>();
            im.Cache(_fieldLayer, size);
            _castFloorFx.Add(im);
            return im;
        }

        private void TickCastPresentation(float dt)
        {
            for (int i = 0; i < _castFloorFx.Count; i++) _castFloorFx[i].Tick(dt);
        }

        /// <summary>방을 나갈 때. 앞 방의 링이 다음 방에 남지 않게, 멈춘 판도 푼다.</summary>
        private void ClearCastPresentation()
        {
            for (int i = 0; i < _castFloorFx.Count; i++) _castFloorFx[i].Stop();
            _castRingA = _castRingB = null;
            _castPending = null;
            _castFreezeLeft = 0f;
        }
    }
}
