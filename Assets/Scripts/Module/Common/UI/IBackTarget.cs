namespace Game.Module.Common.UI
{
    /// <summary>
    /// 뒤로가기(ESC · 안드로이드 백)를 처리하는 화면.
    /// `~UI` 가 구현하고, 같은 오브젝트의 `BackButtonRouter` 가 호출한다.
    /// </summary>
    public interface IBackTarget
    {
        /// <summary>
        /// 뒤로가기를 처리했으면 true. false 면 라우터가 종료 확인 팝업을 띄운다.
        /// 06_ui.md 우선순위(SystemPopup → Popup → Panel → 종료) 중
        /// Popup·Panel 단계를 이 메서드가 담당한다.
        /// </summary>
        bool OnBackPressed();
    }
}
