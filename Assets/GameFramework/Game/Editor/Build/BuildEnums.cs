namespace GameFramework.Game.Editor.Build
{
    /// <summary>
    /// 빌드·업로드 대상 플랫폼.
    /// </summary>
    public enum PlatformType
    {
        Android,
        iOS,
    }

    /// <summary>
    /// 빌드 환경 구분.
    /// </summary>
    public enum BuildType
    {
        Dev,
        Live,
    }

    /// <summary>
    /// 업로드 대상 환경 구분.
    /// </summary>
    public enum UploadType
    {
        Dev,
        Live,
    }
}
