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
    /// 대신 스탯·액티브 스킬은 마스킹하고 `빙의 시작` 을 비활성화한다 (확정 사항).
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

        /// <summary>
        /// 잠금 칸을 눌러 어둡게 하는 값.
        ///
        /// 회색조만으로는 "잠겼다" 가 안 읽힌다 — 초상 원본이 이미 어두운 색이면
        /// 해금 칸과 구별이 안 된다. 그래서 채도를 뺀 위에 밝기까지 내린다.
        /// 실루엣은 남는 정도로만 누른다. 완전히 까맣게 하면 다음 목표가 누구인지
        /// 알 수 없어 잠금 칸이 하는 일이 없어진다.
        /// </summary>
        private static readonly Color LockedTint = new(0.42f, 0.44f, 0.50f, 1f);

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

        /// <summary>
        /// ⚠ 잠금 전용 테두리를 받지 않는다. **모든 칸이 같은 테두리**를 쓴다 —
        ///   잠금 여부는 딤과 자물쇠로만 말한다.
        ///
        /// 예전에는 `hostslotframe_locked` 로 갈아 끼웠는데, 그 그림의 테두리 색이
        /// `(6,14,24)` 라 패널 바닥과 거의 같아서 **테두리가 없는 것처럼** 보였다.
        /// 잠긴 칸만 격자에서 빠져 보이는 원인이었다.
        /// </summary>
        public void SetFrameSprites(Sprite normal, Sprite selected)
        {
            _frameNormal = normal;
            _frameSelected = selected;
        }

        public void Bind(HostEntry entry, Sprite portrait, bool unlocked,
                         Material grayMaterial, System.Action<string> onClick)
        {
            HostKey = entry.HostKey;
            IsUnlocked = unlocked;

            if (_portrait != null)
            {
                _portrait.sprite = portrait;
                _portrait.enabled = portrait != null;
                // 잠금 칸은 채도를 빼고(회색 머티리얼) 밝기까지 내린다.
                _portrait.color = unlocked ? Color.white : LockedTint;
                _portrait.material = grayMaterial;   // 해금이면 null — 기본 UI 머티리얼
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
            // 잠겨 있어도 테두리는 그대로 둔다. 선택 표시만 갈아 끼운다.
            var s = selected ? _frameSelected : _frameNormal;
            if (s != null) _frame.sprite = s;
        }
    }
}
