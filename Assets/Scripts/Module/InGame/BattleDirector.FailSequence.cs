using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 판이 질 때 「쓰러짐 → 결과창」(PD 2026-10-30 「지금은 게임에서 튕겨 나가는 느낌」 · 박자표 `_exchange/fx_guide_run_fail_1030.md` — PD 「그렇게 잡아 봐, 봐야 알 수 있다」).
    ///
    ///   0.00  마지막 한 대 — 흔들림 · 시간 0.35배(적은 그 자리에 선다 — 판 갱신이 멈춰 있다)
    ///   0.10  메인이 아직 서 있으면 원작 죽음 자세로 쓰러진다(보통은 이미 쓰러져 유령이 나와 있다)
    ///   0.35 · 0.55  보조 1 · 2 가 엇박으로 쓰러지며 혼 알갱이로 흩어진다
    ///   0.70  유령이 위로 떠오르며 흐려진다
    ///   1.40  화면이 아래에서 위로 어두워진다
    ///   1.70  결과창(지금 그대로)
    ///
    /// 쓰러짐 자세는 원작 그대로(`Unit.BeginDeath`) · 알갱이는 파티클 풀 · 새 그림 없음. 클리어 · 모드 판 · 포기는 이 길을 안 탄다.
    /// 시계는 실제 시간으로 잰다 — 시간 배율이 0.35 라 스케일된 시간으로 재면 5초 가까이 걸린다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float FailSlowScale = 0.35f;
        private const float FailMainAt = 0.10f;
        private const float FailSub1At = 0.35f;
        private const float FailSub2At = 0.55f;
        private const float FailGhostAt = 0.70f;
        private const float FailDarkAt = 1.40f;
        private const float FailEndAt = 1.70f;
        private const float FailGhostRise = 160f;    // px/초
        private const float FailDarkAlpha = 0.99f;   // 인게임 캔버스는 선형 색이라 0.99 여야 어둡다

        private static readonly Color FailSoulColor = new(0.75f, 0.6f, 1f, 1f);

        private float _failT = -1f;
        private bool _failDone;
        private int _failStep;
        private CanvasGroup _failGhostFade;
        private RectTransform _failDark;

        private bool FailSequenceRunning => _failT >= 0f && !_failDone;

        /// <summary>진 판이면 연출을 시작하고 true — `Finish(false)` 는 연출이 끝난 뒤 다시 불린다.</summary>
        private bool TryBeginFailSequence()
        {
            if (_failDone || IsModeRun) return false;
            if (_failT >= 0f) return true;   // 이미 도는 중 — 다른 길이 또 불러도 한 번만
            _failT = 0f;
            _failStep = 0;
            if (_partyHold) ClosePartyPopup();
            _hitStopLeft = 0f;
            Time.timeScale = FailSlowScale;
            Shake(10f);
            return true;
        }

        private void TickFailSequence()
        {
            _failT += Time.unscaledDeltaTime;
            TickDying(Time.deltaTime);   // 쓰러지는 프레임은 느린 시간으로 넘어간다

            if (_failStep == 0 && _failT >= FailMainAt)
            {
                _failStep = 1;
                if (_host != null && _host.IsAlive && !_host.IsDying && _host.BeginDeath()) _dying.Add(_host);
            }
            if (_failStep == 1 && _failT >= FailSub1At) { _failStep = 2; FellSub(0); }
            if (_failStep == 2 && _failT >= FailSub2At) { _failStep = 3; FellSub(1); }
            if (_failStep == 3 && _failT >= FailGhostAt)
            {
                _failStep = 4;
                if (_ghost != null && _ghost.gameObject.activeInHierarchy)
                {
                    if (!_ghost.TryGetComponent(out _failGhostFade)) _failGhostFade = _ghost.gameObject.AddComponent<CanvasGroup>();
                    _pfx?.Spray(ParticleFxKind.GhostMote, _ghost.Position, Vector2.up, 40f, 14, 80f, 200f,
                                new Vector2(12f, 12f), 1.2f, FailSoulColor, spread: 30f);
                }
            }
            if (_failStep >= 4 && _failGhostFade != null)
            {
                float k = Mathf.Clamp01((_failT - FailGhostAt) / (FailEndAt - FailGhostAt));
                _ghost.Position += Vector2.up * (FailGhostRise * Time.unscaledDeltaTime);
                _failGhostFade.alpha = 1f - k;
            }
            if (_failStep == 4 && _failT >= FailDarkAt) { _failStep = 5; BuildFailDark(); }
            if (_failStep >= 5 && _failDark != null)
            {
                float k = Mathf.Clamp01((_failT - FailDarkAt) / (FailEndAt - FailDarkAt));
                _failDark.anchorMax = new Vector2(1f, 1f - (1f - k) * (1f - k));   // 아래에서 위로, 끝에서 천천히(OutQuad)
            }
            if (_failT < FailEndAt) return;

            _failDone = true;
            Time.timeScale = 1f;
            if (_failGhostFade != null) _failGhostFade.alpha = 1f;   // 어둠 밑이라 안 보인다 — 같은 장면을 다시 쓸 때 투명한 유령이 남지 않게
            Finish(false);
        }

        private void FellSub(int index)
        {
            if (index >= _subs.Count) return;
            var u = _subs[index].U;
            if (u == null || !u.IsAlive || u.IsDying) return;
            if (u.BeginDeath(fadeOnly: true)) _dying.Add(u);
            _pfx?.Spray(ParticleFxKind.GhostMote, u.Position, Vector2.up, 70f, 12, 60f, 180f,
                        new Vector2(10f, 10f), 1f, FailSoulColor, spread: 40f);
        }

        private void BuildFailDark()
        {
            var parent = Ui != null ? Ui.transform : _textLayer != null ? _textLayer.parent : null;
            if (parent == null) return;
            var go = new GameObject("RbFailDark", typeof(RectTransform), typeof(Image));
            _failDark = (RectTransform)go.transform;
            _failDark.SetParent(parent, false);
            _failDark.anchorMin = Vector2.zero;
            _failDark.anchorMax = new Vector2(1f, 0f);
            _failDark.offsetMin = Vector2.zero;
            _failDark.offsetMax = Vector2.zero;
            _failDark.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, FailDarkAlpha);
            img.raycastTarget = true;   // 연출 중 단추를 못 누르게
        }
    }
}
