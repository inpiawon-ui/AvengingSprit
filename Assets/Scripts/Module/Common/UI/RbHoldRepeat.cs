using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 길게 누르면 같은 버튼을 계속 누른다(능력치 연속 강화 — 시안 「길게 누르면 연속 강화」).
    /// 0.4초 뒤부터 처음엔 느리게, 오래 누를수록 빠르게. 첫 누름은 Button 이 손을 뗄 때 한 번 낸다 —
    /// 연속으로 눌렀으면 그 한 번은 막는다(손을 떼며 한 번 더 사지 않게).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class RbHoldRepeat : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float Delay = 0.4f, SlowStep = 0.18f, FastStep = 0.06f, RampSeconds = 2f;

        private Button _button;
        private bool _held, _repeated;
        private float _heldFor, _next;

        private void Awake() => _button = GetComponent<Button>();

        private void Update()
        {
            if (!_held) return;
            if (!_button.interactable) { _held = false; return; }
            _heldFor += Time.unscaledDeltaTime;
            if (_heldFor < _next) return;
            _repeated = true;
            _button.onClick.Invoke();
            float t = Mathf.Clamp01((_heldFor - Delay) / RampSeconds);
            _next = _heldFor + Mathf.Lerp(SlowStep, FastStep, t);
        }

        private void OnDisable() => _held = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            _held = true;
            _repeated = false;
            _heldFor = 0f;
            _next = Delay;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _held = false;
            // 연속으로 샀으면 손을 뗄 때의 한 번(Button.onClick)을 막는다 — 다음 프레임에 되살린다
            if (_repeated && _button.interactable) Block().Forget();   // fire-and-forget: 한 프레임 막고 되살리기만 한다
        }

        public void OnPointerExit(PointerEventData eventData) => _held = false;

        private async Cysharp.Threading.Tasks.UniTaskVoid Block()
        {
            _button.interactable = false;
            await Cysharp.Threading.Tasks.UniTask.Yield();
            if (_button != null) _button.interactable = true;
        }
    }
}
