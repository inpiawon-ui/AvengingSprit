using System;

namespace GameFramework.Core.Module.Timer
{
    public interface ITimerManager
    {
        TimerHandle Delay   (float seconds, Action onComplete);
        TimerHandle Interval(float seconds, Action onTick, int count = -1);
        void Cancel    (TimerHandle handle);
        void CancelAll ();
        void Pause     (TimerHandle handle);
        void Resume    (TimerHandle handle);
        void PauseAll  ();
        void ResumeAll ();
    }
}
