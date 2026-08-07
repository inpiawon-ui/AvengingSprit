namespace GameFramework.Core.Module.UI
{
    /// <summary>
    /// UIManager에서 패널 인스턴스를 관리하는 방식.
    /// </summary>
    public enum UIInstanceMode
    {
        /// <summary>
        /// 하나의 인스턴스만 유지 (기본값).
        /// 이미 열려 있으면 스택 최상단으로 승격한다.
        /// 옵션 창, 인벤토리처럼 동시에 하나만 존재해야 하는 패널에 사용.
        /// </summary>
        Single,

        /// <summary>
        /// 호출할 때마다 새 인스턴스를 생성한다.
        /// 닫힐 때 인스턴스를 Destroy한다.
        /// 데미지 폰트, 알림 팝업처럼 동시에 여러 개가 떠야 하는 패널에 사용.
        /// </summary>
        Multiple,
    }
}
