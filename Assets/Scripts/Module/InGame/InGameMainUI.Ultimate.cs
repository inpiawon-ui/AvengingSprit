using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 필살기 버튼 — 예전 빙의 · 탈출 버튼 자리(PD 2026-10-15 「영혼 탈출을 빼고 액티브 · 필살기 버튼 두 개」).
    /// 유령이면 「빙의」, 몸이 있으면 「필살기」. 필살기는 빙의마다 한 번 — 쓰면 회색으로 잠긴다.
    /// </summary>
    public sealed partial class InGameMainUI
    {
        private static readonly Color UltUsedTint = new(0.45f, 0.45f, 0.5f, 1f);
        private const string UltLabel = "SPECIAL";

        private bool _ultShown;          // 지금 버튼이 필살기 얼굴인가
        private bool _ultShownReady;
        private string _ultShownHost;

        /// <summary>매 프레임 — 몸이 있으면 필살기 얼굴로, 쓸 수 있는지에 따라 밝기 · 빛.</summary>
        private void TickUltButton()
        {
            if (_battle == null) return;
            if (!_battle.HasHostBody)
            {
                if (_ultShown)
                {
                    // 유령으로 돌아왔다 — 빙의 얼굴로 되돌린다
                    _ultShown = false;
                    _possessFaceSet = false;
                    SetPossessReady(false);
                }
                return;
            }
            string host = _affinityHostKey;
            bool ready = _battle.IsUltReady;
            if (_ultShown && ready == _ultShownReady && host == _ultShownHost) return;
            _ultShown = true;
            _ultShownReady = ready;
            _ultShownHost = host;

            var icon = _ui.Get<Image>("PossessGhostIcon");
            if (icon != null)
            {
                var sprite = _battle.UltimateIcon(host);
                if (sprite != null) icon.sprite = sprite;
                icon.color = ready ? Color.white : UltUsedTint;
            }
            _ui.SetText("PossessButtonLabel", UltLabel);
            _ui.SetText("PossessCostText", string.Empty);
            // ⚠ 다 쓴 필살기도 interactable 은 켜 둔다 — 버튼의 비활성 색(반투명)이 회색 위에 겹쳐 버튼이 통째로 사라져 보였다(녹화 tb_medium1).
            //   눌러도 `TryUltimate` 가 1회 규칙으로 거른다
            var btn = _ui.Get<Button>("PossessButton");
            if (btn != null) btn.interactable = true;
            if (_possessButtonImage != null) _possessButtonImage.color = ready ? Color.white : UltUsedTint;
            SetPossessGlow(ready);
        }
    }
}
