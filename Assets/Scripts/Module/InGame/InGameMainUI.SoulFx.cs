using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 빙의 버튼의 두 얼굴 (기획 2026-10-02).
    ///
    /// 같은 버튼이 유령일 때는 **빙의**, 몸을 입고 있을 때는 **나가기**다. 예전에는 두 상황에
    /// 같은 유령 얼굴을 써서 지금 누르면 무슨 일이 나는지가 그림으로 안 갈렸다.
    ///
    ///   빙의 · 탈 몸이 잡혔다   — 들어가는 아이콘, 둘레가 숨쉬듯 빛난다(`PossessButtonGlow`)
    ///   빙의 · 탈 몸이 없다     — 들어가는 아이콘, 회색
    ///   나가기                 — 나가는 아이콘, 값(-N) 표시
    ///   나가기 · 옮겨 탈 몸 없음 — 나가는 아이콘, 회색. **누를 수는 있다** — 눌러야 이유를 듣는다.
    ///                           「빙의할 호스트가 없습니다」(`PossessDeniedToast`)
    /// </summary>
    public sealed partial class InGameMainUI
    {
        private const float GlowPulseSeconds = 0.9f;
        private const float GlowMinAlpha = 0.45f;
        private const float DeniedToastSeconds = 1.4f;
        private const float DeniedToastFade = 0.25f;

        private static readonly Color ButtonOff = new(0.45f, 0.45f, 0.5f, 1f);
        private static readonly Color IconOff = new(0.55f, 0.55f, 0.6f, 0.85f);

        private bool _possessGlowOn;
        /// <summary>겹쳐 불릴 때 앞 맥박을 그만두게 하는 번호.</summary>
        private int _possessGlowId;
        private int _deniedToastId;
        private bool _possessIsLeave;
        private bool _possessFaceSet;

        /// <summary>버튼 얼굴(아이콘 · 글자)을 빙의 / 나가기로 바꾼다.</summary>
        private void SetPossessFace(bool isLeave, bool dimmed)
        {
            var icon = _ui.Get<Image>("PossessGhostIcon");
            if (icon != null)
            {
                if (!_possessFaceSet || isLeave != _possessIsLeave)
                {
                    var sprite = _battle != null
                        ? _battle.UiSprite(isLeave ? "possessicon_leave" : "possessicon_enter") : null;
                    if (sprite != null) icon.sprite = sprite;
                }
                icon.color = dimmed ? IconOff : Color.white;
            }
            if (!_possessFaceSet || isLeave != _possessIsLeave)
                _ui.SetText("PossessButtonLabel", isLeave ? "LEAVE" : "POSSESS");
            _possessIsLeave = isLeave;
            _possessFaceSet = true;
        }

        private void SetPossessGlow(bool on)
        {
            if (on == _possessGlowOn) return;
            _possessGlowOn = on;

            var img = _ui.Get<Image>("PossessButtonGlow");
            if (img == null) return;
            img.gameObject.SetActive(on);
            if (on) PulseGlowAsync(img, ++_possessGlowId).Forget();   // fire-and-forget: 꺼질 때까지 도는 맥박
            else _possessGlowId++;
        }

        private async UniTaskVoid PulseGlowAsync(Image img, int id)
        {
            float t = 0f;
            while (this != null && img != null && id == _possessGlowId)
            {
                float k = Mathf.PingPong(t / GlowPulseSeconds * 2f, 1f);
                img.color = new Color(1f, 1f, 1f, Mathf.Lerp(GlowMinAlpha, 1f, k));
                img.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.06f, k);
                await UniTask.Yield();
                t += Time.unscaledDeltaTime;
            }
        }

        /// <summary>나가기가 거절됐다 — 버튼 위에 이유를 잠깐 띄운다.</summary>
        private void OnPossessDenied(PossessDeniedEvent e)
        {
            var text = _ui.Get<TMP_Text>("PossessDeniedToast");
            if (text == null) return;
            text.text = Localize.Get("ui.ingame.no_host_to_possess");
            ShowDeniedToastAsync(text, ++_deniedToastId).Forget();   // fire-and-forget: 잠깐 떴다 사라지는 알림
        }

        private async UniTaskVoid ShowDeniedToastAsync(TMP_Text text, int id)
        {
            text.gameObject.SetActive(true);
            float t = 0f;
            while (t < DeniedToastSeconds)
            {
                if (this == null || text == null || id != _deniedToastId) return;
                float a = Mathf.Min(Mathf.Clamp01(t / DeniedToastFade),
                                    Mathf.Clamp01((DeniedToastSeconds - t) / DeniedToastFade));
                text.alpha = a;
                await UniTask.Yield();
                t += Time.unscaledDeltaTime;
            }
            if (this != null && text != null && id == _deniedToastId) text.gameObject.SetActive(false);
        }
    }
}
