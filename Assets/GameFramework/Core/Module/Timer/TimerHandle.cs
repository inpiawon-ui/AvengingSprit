namespace GameFramework.Core.Module.Timer
{
    public readonly struct TimerHandle
    {
        public readonly int Id;
        public bool IsValid => Id != 0;

        internal TimerHandle(int id) => Id = id;

        public static readonly TimerHandle Invalid = new TimerHandle(0);
    }
}
