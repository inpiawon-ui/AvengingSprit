using System;

namespace GameFramework.Core.Module.Timer
{
    internal struct TimerEntry
    {
        public int    Id;
        public float  Elapsed;
        public float  Duration;
        public int    RemainCount;   // -1 = 무한
        public bool   IsPaused;
        public bool   IsCancelled;
        public Action Callback;
    }
}
