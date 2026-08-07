using GameFramework.Core.Common;
namespace GameFramework.Core.Events
{
    /// <summary>
    /// 로드 취소
    /// </summary>
    public struct OnSceneLoadCancelled : IEvent
    {
        public string SceneName;
    }
}