using System.Collections.Generic;
using Game.Module.Events;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 스킬 시전 연출 — 필드 쪽 (2026-09-17, 시안 `AVSR_SkillCast_draft`).
    ///
    /// 스킬 효과가 스킬마다 제각각이라 **「지금 스킬을 썼다」는 순간**이 화면에 안 잡혔다.
    /// 평타와 같은 무게로 지나가서, 게이지를 모아 쓴 보람이 없었다.
    /// 모든 몸이 **같은 순서**로 터지고, 색만 몸마다 다르다.
    ///
    ///   0.00  몸에서 빛이 번쩍 · 발밑에서 링이 퍼짐 · 몸 둘레로 폭발
    ///   0.00~ 발밑 마법진이 천천히 돌다가 흐려짐 (약 1초)
    ///   컷인 띠 · 버튼 눌림은 화면 쪽(`InGameMainUI.SkillCast`)이 `SkillCastEvent` 를 듣고 그린다.
    ///
    /// ⚠ **스킬 효과 코드는 건드리지 않는다.** 여기는 보이는 것만 얹는다.
    /// ⚠ 시간을 늦추지 않는다(기획 2026-09-17 — 짧은 쿨도 매번 뜨므로 매번 느려지면 답답하다).
    /// ⚠ 그림은 전부 흰색으로 받았다. 색은 `GameConfig.CastColorOf` 가 입힌다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float CastFlashSize = 210f;
        private const float CastBurstSize = 230f;
        private const float CastRingSize = 250f;
        private const float CastRuneSize = 170f;
        private const float CastFootDrop = 34f;      // 몸 중심 → 발밑
        private const float CastRuneLife = 1.1f;
        private const float CastRuneFade = 0.45f;
        private const float CastShake = 3.2f;

        /// <summary>
        /// 발밑에 깔리는 그림(링 · 마법진) 전용 자리. 탄 레이어는 몸보다 **위**라
        /// 발밑 마법진이 몸을 덮어 버린다 — 장판 레이어(몸 아래)에 따로 둔다.
        /// </summary>
        private readonly List<Impact> _castFloorFx = new();

        private Impact _castRune;
        private Unit _castRuneOwner;

        /// <summary>스킬이 나간 직후 부른다. 봉인 · 쿨 검사를 통과한 뒤다.</summary>
        private void PlayCastPresentation(Unit me)
        {
            if (me == null) return;
            // 아마존 정예는 스킬 보류 — 아무 일도 안 일어나는데 연출만 터지면 거짓말이다
            if (me.Key == "amazon_elite") return;

            var color = _config != null ? _config.CastColorOf(me.Key) : Color.white;
            var body = me.Position;
            var feet = body - Vector2.up * CastFootDrop;

            me.BeginCastWhite();   // 몸 모양 그대로 하얗게 — 시안 「몸이 하얗게 번쩍」

            // 몸의 빛은 **흰빛이 먼저** 보여야 번쩍으로 읽힌다 — 색은 옅게만 섞는다
            var flash = PlayFx("castflash", body, CastFlashSize, loop: false);
            if (flash != null)
            {
                flash.SetFrameSeconds(0.05f);
                flash.SetTint(Color.Lerp(Color.white, color, 0.35f));
            }

            var burst = PlayFx("castburst", body, CastBurstSize, loop: false);
            if (burst != null)
            {
                burst.SetFrameSeconds(0.07f);
                burst.SetTint(color);
            }

            var ring = PlayFloorFx("castring", feet, CastRingSize, loop: false);
            if (ring != null)
            {
                ring.SetFrameSeconds(0.08f);
                ring.SetTint(color);
            }

            _castRune?.Stop();
            _castRune = PlayFloorFx("castrune", feet, CastRuneSize, loop: true);
            if (_castRune != null)
            {
                _castRune.SetTint(new Color(color.r, color.g, color.b, 0.9f));
                _castRune.SetSpin(-70f);
                _castRune.SetLife(CastRuneLife, CastRuneFade);
                _castRuneOwner = me;
            }

            Shake(CastShake);

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
                CastColor = color,
            });
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
            if (frames == null || _fieldLayer == null) return null;

            Impact im = null;
            for (int i = 0; i < _castFloorFx.Count; i++)
                if (!_castFloorFx[i].IsActive) { im = _castFloorFx[i]; break; }
            if (im == null)
            {
                var go = new GameObject($"CastFloorFx_{_castFloorFx.Count}", typeof(RectTransform));
                im = go.AddComponent<Impact>();
                im.Cache(_fieldLayer, size);
                _castFloorFx.Add(im);
            }
            im.Play(at, frames, size, loop);
            return im;
        }

        private void TickCastPresentation(float dt)
        {
            for (int i = 0; i < _castFloorFx.Count; i++) _castFloorFx[i].Tick(dt);

            // 마법진은 시전자 발밑을 따라간다 — 도약 · 돌진 스킬이면 제자리에 남으면 어색하다
            if (_castRune != null && _castRune.IsActive && _castRuneOwner != null && _castRuneOwner.IsAlive)
                _castRune.MoveTo(_castRuneOwner.Position - Vector2.up * CastFootDrop);
        }

        /// <summary>방을 나갈 때. 앞 방의 마법진이 다음 방에 남지 않게.</summary>
        private void ClearCastPresentation()
        {
            for (int i = 0; i < _castFloorFx.Count; i++) _castFloorFx[i].Stop();
            _castRune = null;
            _castRuneOwner = null;
        }
    }
}
