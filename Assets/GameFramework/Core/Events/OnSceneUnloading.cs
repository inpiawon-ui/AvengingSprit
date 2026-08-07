using GameFramework.Core.Common;
namespace GameFramework.Core.Events
{
    /// <summary>
    /// 언로드
    /// </summary>
    public struct OnSceneUnloading : IEvent
    {
        public string SceneName;
    }
}
