using static GameFramework.Game.Common.Enums;

namespace GameFramework.Game.Module.Action
{
    /// <summary>
    /// 유저 행위(Action) 누적값을 관리하는 모듈 인터페이스.
    /// </summary>
    public interface IActionModule
    {
        void Initialize();
        void Release();

        long GetAction(ActionType inAction);
        void SetAction(ActionType inAction, long inAccrue);
        void AddAction(ActionType inAction, long inAccrue);
    }
}
