using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 팝업이 뜨고 사라지는 연출 (2026-09-10).
    ///
    /// 네 창(레벨업 · 상점 · 악마 · 제단)이 **툭 나타나고 툭 사라졌다.** 화면이 한 프레임에
    /// 통째로 바뀌니 무엇이 열렸는지 눈이 따라가지 못하고, 닫힐 때도 「사라졌다」가 아니라
    /// 「끊겼다」로 보였다.
    ///
    /// ── 왜 크기와 투명도만 건드리나 ─────────────────────────
    /// 자리를 옮기며 날아오게 하면 창마다 어디서 날아올지를 정해야 하고, 그 값이
    /// 액자 그림·해상도와 얽힌다. **가운데에서 살짝 커지며 밝아지는 것**은 어떤 창에도
    /// 같은 규칙으로 먹고, 프리팹 좌표를 한 줄도 안 건드린다.
    ///
    /// ⚠ 열 때는 **1.06 배에서 1.0 으로 줄어들며** 들어온다. 작은 데서 커지면 다가오는
    ///   느낌이라 「튀어나왔다」가 되고, 큰 데서 줄면 자리에 앉는 느낌이 된다.
    ///
    /// ⚠ `Time.timeScale` 을 안 쓴다. 팝업이 뜨는 동안 전투가 멈추는 창이 있어서
    ///   (`_awaitingBuff` → `Update` 이른 반환) 스케일된 시간으로 재면 연출이 얼어붙는다.
    /// </summary>
    public sealed partial class InGameMainUI
    {
        private const float PopupOpenSeconds = 0.16f;
        private const float PopupCloseSeconds = 0.11f;
        private const float PopupOvershoot = 1.06f;

        /// <summary>연출이 도는 중인 창. 같은 창에 두 번 걸리지 않게 든다.</summary>
        private readonly HashSet<string> _popupBusy = new();

        /// <summary>
        /// 창을 연출과 함께 켠다/끈다.
        ///
        /// 이미 원하는 상태면 아무것도 안 한다 — 매 프레임 같은 값을 넣는 자리
        /// (`RefreshPanels`)가 있어서, 안 걸러 내면 연출이 계속 처음부터 다시 돈다.
        /// </summary>
        private void SetPanel(string name, bool on)
        {
            var tr = _ui.Find(name);
            if (tr == null) { _ui.SetActive(name, on); return; }

            bool now = tr.gameObject.activeSelf;
            if (now == on && !_popupBusy.Contains(name)) return;
            if (_popupBusy.Contains(name)) return;

            if (on) OpenPanelAsync(name, tr).Forget();   // fire-and-forget: 연출은 기다릴 것이 없다
            else ClosePanelAsync(name, tr).Forget();     // fire-and-forget: 위와 같다
        }

        private async UniTaskVoid OpenPanelAsync(string name, Transform tr)
        {
            _popupBusy.Add(name);
            var group = EnsureGroup(tr);
            tr.gameObject.SetActive(true);
            tr.SetAsLastSibling();

            float t = 0f;
            while (t < PopupOpenSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / PopupOpenSeconds);
                // 끝에서 부드럽게 멈춘다 — 딱 끊기면 커진 것이 아니라 튄 것으로 보인다
                float e = 1f - (1f - k) * (1f - k);
                tr.localScale = Vector3.one * Mathf.Lerp(PopupOvershoot, 1f, e);
                if (group != null) group.alpha = e;
                await UniTask.Yield();
                if (tr == null) break;
            }
            if (tr != null)
            {
                tr.localScale = Vector3.one;
                if (group != null) group.alpha = 1f;
            }
            _popupBusy.Remove(name);
        }

        private async UniTaskVoid ClosePanelAsync(string name, Transform tr)
        {
            _popupBusy.Add(name);
            var group = EnsureGroup(tr);

            float t = 0f;
            while (t < PopupCloseSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / PopupCloseSeconds);
                tr.localScale = Vector3.one * Mathf.Lerp(1f, 0.94f, k);
                if (group != null) group.alpha = 1f - k;
                await UniTask.Yield();
                if (tr == null) break;
            }
            if (tr != null)
            {
                tr.gameObject.SetActive(false);
                tr.localScale = Vector3.one;
                if (group != null) group.alpha = 1f;   // 다음에 켤 때 투명한 채로 뜨지 않게
            }
            _popupBusy.Remove(name);
        }

        // ── 고른 칸을 짚어 준다 ──────────────────────────────────
        //
        // 셋 중 하나를 눌렀는데 창이 그냥 사라지면 **무엇을 골랐는지 눈에 안 남는다.**
        // 누른 칸만 잠깐 부풀렸다 지우면 「이걸 골랐다」가 손에 남는다.
        //
        // ⚠ 창 전체를 닫기 **전에** 이것부터 돌린다. 같이 돌리면 창이 줄어드는 동안
        //   칸이 커져서 두 연출이 서로를 지운다.

        private const float PickPunchSeconds = 0.13f;
        private const float PickPunchScale = 1.14f;

        /// <summary>고른 칸을 부풀렸다 되돌린다. 끝나면 <paramref name="after"/> 를 부른다.</summary>
        private void PunchThenClose(string slotName, string panelName)
        {
            var slot = _ui.Find(slotName);
            if (slot == null) { SetPanel(panelName, false); return; }
            PunchAsync(slot, panelName).Forget();   // fire-and-forget: 연출은 기다릴 것이 없다
        }

        private async UniTaskVoid PunchAsync(Transform slot, string panelName)
        {
            var start = slot.localScale;
            float t = 0f;
            while (t < PickPunchSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / PickPunchSeconds);
                // 올라갔다 내려온다 — 한 번 튕기는 모양
                float punch = Mathf.Sin(k * Mathf.PI);
                if (slot != null) slot.localScale = start * Mathf.Lerp(1f, PickPunchScale, punch);
                await UniTask.Yield();
            }
            if (slot != null) slot.localScale = start;
            SetPanel(panelName, false);
        }

        // ── 연출 없이 바로 지운다 ────────────────────────────────
        //
        // 상점에서 사면 **누르는 순간 창이 사라진다** (2026-09-17 기획).
        // 예전에는 산 칸이 떠오르며 옅어진 뒤(0.22초) 창이 줄어들며 닫혀(0.11초)
        // 연출 두 개가 이어 붙었는데, 「선택하고 이펙트가 이상하다」는 지적을 받았다.
        //
        // ⚠ 여는 연출이 아직 돌고 있을 수 있다. 크기 · 투명도를 원래대로 돌려놓고
        //   바쁜 표시도 지운다 — 안 그러면 다음 상점이 작게 · 투명한 채로 열리거나
        //   `SetPanel` 이 「연출 중」으로 알고 열기를 무시한다.

        private void HidePanelNow(string panelName)
        {
            var tr = _ui.Find(panelName);
            if (tr == null) { _ui.SetActive(panelName, false); return; }
            tr.gameObject.SetActive(false);
            tr.localScale = Vector3.one;
            var group = tr.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1f;
            _popupBusy.Remove(panelName);
        }

        /// <summary>
        /// 투명도를 다루려면 `CanvasGroup` 이 있어야 한다. 프리팹에 없으면 여기서 붙인다 —
        /// 프리팹을 고치면 네 창을 다 손봐야 하고, 그 편집이 다른 작업과 엉킨다.
        /// </summary>
        private static CanvasGroup EnsureGroup(Transform tr)
        {
            if (tr == null) return null;
            var g = tr.GetComponent<CanvasGroup>();
            return g != null ? g : tr.gameObject.AddComponent<CanvasGroup>();
        }
    }
}
