namespace GameFramework.Core.Module.Sound
{
    public interface ISoundManager
    {
        // BGM — 단일 AudioSource 전용
        void PlayBGM(string address, float fadeIn  = 0f);
        void StopBGM(                float fadeOut = 0f);

        // SFX / Voice / Ambient — Pool에서 AudioSource 대여
        void Play(string address, SoundChannel channel = SoundChannel.SFX);
        void Stop(SoundChannel channel);

        // 채널 볼륨 제어
        void  SetVolume(SoundChannel channel, float volume);
        float GetVolume(SoundChannel channel);

        // 전체 음소거
        void SetMute(bool mute);
    }
}
