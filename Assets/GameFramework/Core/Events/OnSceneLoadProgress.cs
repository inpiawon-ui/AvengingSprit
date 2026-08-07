using GameFramework.Core.Common;
namespace GameFramework.Core.Events
{
    /// <summary>
    /// 로드 진행률
    /// </summary>
    public struct OnSceneLoadProgress : IEvent
    {
        public float Progress;
    }
}