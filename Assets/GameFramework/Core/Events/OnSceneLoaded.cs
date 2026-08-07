using GameFramework.Core.Common;

namespace GameFramework.Core.Events
{
    /// <summary>
    /// 로드 완료
    /// </summary>
    public struct OnSceneLoaded : IEvent
    {
        public string PrevScene;
        public string NextScene;
    }
}