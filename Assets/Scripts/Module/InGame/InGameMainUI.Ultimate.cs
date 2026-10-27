using Game.Module.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 필살기 버튼(`UltButton`) — PD 2026-10-15 「영혼 탈출을 빼고 액티브 · 필살기 버튼 두 개」 뒤,
    /// 2026-10-16 「빙의 버튼이 따로 있어야」로 빙의(`PossessButton`)와 나눴다. 예전엔 한 버튼이 유령이면 빙의, 몸이 있으면 필살기였다.
    /// 필살기는 빙의마다 한 번 — 쓰면 회색으로 잠긴다. 유령일 때는 쓸 몸이 없어 꺼진 채로 둔다.
    /// </summary>
    public sealed partial class InGameMainUI
    {
        private static readonly Color UltUsedTint = new(0.45f, 0.45f, 0.5f, 1f);
        private const string UltLabel = "SPECIAL";

        private bool _ultShown;          // 지금 버튼이 필살기 얼굴인가
        private bool _ultShownReady;
        private bool _ultShownTrial;
        private bool _ultShownSpent;
        private string _ultShownHost;
        private Sprite _ultLockSprite;
        private Image _ultButtonImage;
        private bool _ultGlowOn;
        private int _ultGlowId;

        /// <summary>매 프레임 — 몸이 있으면 필살기 얼굴로, 쓸 수 있는지에 따라 밝기 · 빛.</summary>
        private void TickUltButton()
        {
            if (_battle == null) return;
            if (!_battle.HasHostBody)
            {
                if (_ultShown || _ultShownHost == null)
                {
                    // 유령 — 쓸 몸이 없다. 빈 칸 · 회색으로 둔다
                    _ultShown = false;
                    _ultShownHost = string.Empty;
                    var off = _ui.Get<Image>("UltIcon");
                    if (off != null) off.color = new Color(1f, 1f, 1f, 0f);
                    _ui.SetText("UltButtonLabel", UltLabel);
                    // interactable 은 켜 둔다(아래와 같은 이유) — 눌러도 `TryUltimate` 가 몸이 없으면 거른다
                    var offBtn = _ui.Get<Button>("UltButton");
                    if (offBtn != null) offBtn.interactable = true;
                    if (_ultButtonImage != null) _ultButtonImage.color = UltUsedTint;
                    SetUltGlow(false);
                }
                return;
            }
            string host = _affinityHostKey;
            bool ready = _battle.IsUltReady;
            // 못 가진 몸 — 아직 안 쓴 체험이면 「체험」, 다 썼으면 자물쇠 (PD 2026-10-15)
            bool trial = _battle.IsUltTrial;
            bool spent = _battle.IsUltTrialSpent;
            if (_ultShown && ready == _ultShownReady && trial == _ultShownTrial && spent == _ultShownSpent
                && host == _ultShownHost) return;
            _ultShown = true;
            _ultShownReady = ready;
            _ultShownTrial = trial;
            _ultShownSpent = spent;
            _ultShownHost = host;

            var icon = _ui.Get<Image>("UltIcon");
            if (icon != null)
            {
                if (spent && _ultLockSprite == null) _ultLockSprite = _battle.UltimateLockIcon;
                var sprite = spent && _ultLockSprite != null ? _ultLockSprite : _battle.UltimateIcon(host);
                if (sprite != null) icon.sprite = sprite;
                icon.preserveAspect = true;   // 자물쇠는 세로로 길다 — 정사각 칸에 늘이지 않는다
                // 쿨이 도는 중이면 덮개 · 남은 시간이 보여 준다 — 아이콘은 밝게 두어야 걷히는 쪽이 보인다
                icon.color = ready || spent || _battle.UltCooldownRemain > 0f ? Color.white : UltUsedTint;
            }
            _ui.SetText("UltButtonLabel", trial ? Localize.Get("ui.ingame.ult_trial")
                                         : spent ? Localize.Get("ui.ingame.ult_locked") : UltLabel);
            // ⚠ 다 쓴 필살기도 interactable 은 켜 둔다 — 버튼의 비활성 색(반투명)이 회색 위에 겹쳐 버튼이 통째로 사라져 보였다(녹화 tb_medium1).
            //   눌러도 `TryUltimate` 가 1회 규칙으로 거른다
            var btn = _ui.Get<Button>("UltButton");
            if (btn != null) btn.interactable = true;
            if (_ultButtonImage != null) _ultButtonImage.color = ready ? Color.white : UltUsedTint;
            SetUltGlow(ready);
        }

        private Image _ultCooldownFill;
        private bool _ultCdInit;
        private int _ultCdShown = -1;

        // 필살기 쿨 — 아이콘 원을 덮고 시계 방향으로 걷힌다 · 가운데 「4:59」(초가 바뀔 때만 글자를 고친다)
        private void TickUltCooldownView()
        {
            if (_battle == null) return;
            if (!_ultCdInit)
            {
                _ultCdInit = true;
                _ultCooldownFill = _ui.Get<Image>("UltCooldown");
                if (_ultCooldownFill != null)
                {
                    _ultCooldownFill.type = Image.Type.Filled;
                    _ultCooldownFill.fillMethod = Image.FillMethod.Radial360;
                    _ultCooldownFill.fillOrigin = (int)Image.Origin360.Top;
                    _ultCooldownFill.fillClockwise = false;
                    _ultCooldownFill.raycastTarget = false;
                }
            }
            float remain = _battle.UltCooldownRemain;
            if (_ultCooldownFill != null) _ultCooldownFill.fillAmount = remain > 0f ? 1f - _battle.UltCooldownRatio : 0f;
            int sec = remain > 0f ? Mathf.CeilToInt(remain) : 0;
            if (sec == _ultCdShown) return;
            _ultCdShown = sec;
            _ui.SetActive("UltCooldownText", sec > 0);
            if (sec > 0) _ui.SetText("UltCooldownText", $"{sec / 60}:{sec % 60:00}");
            _ultShownHost = null;   // 쿨이 끝나거나 시작하면 아이콘 밝기도 다시 정한다
        }

        private void SetUltGlow(bool on)
        {
            if (on == _ultGlowOn) return;
            _ultGlowOn = on;
            var img = _ui.Get<Image>("UltButtonGlow");
            if (img == null) return;
            img.gameObject.SetActive(on);
            if (on) PulseGlowAsync(img, ++_ultGlowId, ult: true).Forget();   // fire-and-forget: 꺼질 때까지 도는 맥박
            else _ultGlowId++;
        }
    }
}
