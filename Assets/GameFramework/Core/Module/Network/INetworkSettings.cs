namespace GameFramework.Core.Module.Network
{
    /// <summary>
    /// NetworkModule이 CoreConfig에서 읽는 설정 인터페이스.
    /// Scripts 레이어의 NetworkConfig ScriptableObject가 이 인터페이스를 구현한다.
    /// </summary>
    public interface INetworkSettings
    {
        bool   UseLocal { get; }
        string BaseUrl  { get; }
    }
}
