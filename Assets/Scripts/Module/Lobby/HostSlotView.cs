using Game.Character;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 호스트 그리드 셀 1칸.
    ///
    /// 잠금 셀도 **선택은 가능하다** — 못 누르면 "다음 목표 확인" 기능이 죽는다.
    /// 대신 스탯·얼티밋은 마스킹하고 `빙의 시작` 을 비활성화한다 (확정 사항).
    /// </summary>
    public sealed class HostSlotView : MonoBehaviour
    {
        private Image _frame;
        private Image _portrait;
        private Image _lockIcon;
        private Image _selectMarker;
        private TextMeshProUGUI _nameText;
        private Button _button;

        private Sprite _frameNormal;
        private Sprite _frameSelected;
        private Sprite _frameLocked;

        public string HostKey { get; private set; }
        public bool IsUnlocked { get; private set; }

        public void Cache()
        {
            _frame        = transform.Find("HostSlotFrame")?.GetComponent<Image>();
            _portrait     = transform.Find("HostSlotPortrait")?.GetComponent<Image>();
            _nameText     = transform.Find("HostSlotNameText")?.GetComponent<TextMeshProUGUI>();
            _lockIcon     = transform.Find("HostSlotLockIcon")?.GetComponent<Image>();
            _selectMarker = transform.Find("HostSlotSelectMarker")?.GetComponent<Image>();
            _button       = GetComponent<Button>();
            if (_button == null) _button = gameObject.AddComponent<Button>();
        }

        public void SetFrameSprites(Sprite normal, Sprite selected, Sprite locked)
        {
            _frameNormal = normal;
            _frameSelected = selected;
            _frameLocked = locked;
        }

        public void Bind(HostEntry entry, Sprite portrait, bool unlocked,
                         System.Action<string> onClick)
        {
            HostKey = entry.HostKey;
            IsUnlocked = unlocked;

            if (_portrait != null)
            {
                _portrait.sprite = portrait;
                _portrait.enabled = portrait != null;
                // 잠금 셀은 실루엣 — 원본 형태를 남기되 검게 눌러 식별만 되게 한다
                _portrait.color = unlocked ? Color.white : new Color(0.10f, 0.10f, 0.14f, 1f);
            }
            if (_nameText != null)
                _nameText.text = unlocked ? entry.NameKr : "???";

            if (_lockIcon != null) _lockIcon.gameObject.SetActive(!unlocked);
            if (_selectMarker != null) _selectMarker.gameObject.SetActive(false);

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                var key = entry.HostKey;
                _button.onClick.AddListener(() => onClick?.Invoke(key));
            }
            ApplyFrame(false);
        }

        public void SetSelected(bool selected)
        {
            if (_selectMarker != null) _selectMarker.gameObject.SetActive(selected && IsUnlocked);
            ApplyFrame(selected);
        }

        private void ApplyFrame(bool selected)
        {
            if (_frame == null) return;
            var s = !IsUnlocked ? _frameLocked : (selected ? _frameSelected : _frameNormal);
            if (s != null) _frame.sprite = s;
        }
    }
}
