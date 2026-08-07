namespace GameFramework.Core.Module.Sound
{
    public interface ISoundBackend
    {
        void SetChannelVolume(SoundChannel channel, float volume);
        void SetMute(bool mute);
    }
}
