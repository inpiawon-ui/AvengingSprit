namespace GameFramework.Core.Module.Scene
{
    public enum LoadingStyle
    {
        None,           // 로딩 화면 없이 바로 전환
        LoadingScene,   // 별도 로딩 씬 진입 후 전환
        Overlay,        // 현재 씬 위에 UI 오버레이
    }
}
