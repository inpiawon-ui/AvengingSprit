using UnityEngine;
using UnityEngine.Audio;

namespace GameFramework.Core.Module.Sound.Backends
{
    /// <summary>
    /// AudioMixerGroup의 Exposed Parameter를 제어하는 백엔드.
    /// Inspector에서 각 채널을 Expose한 뒤 아래 상수 이름과 맞춰야 한다.
    /// </summary>
    public sealed class AudioMixerBackend : ISoundBackend
    {
        private readonly AudioMixer _mixer;

        private const string ParamBGM     = "VolumeBGM";
        private const string ParamSFX     = "VolumeSFX";
        private const string ParamVoice   = "VolumeVoice";
        private const string ParamAmbient = "VolumeAmbient";

        public AudioMixerBackend(AudioMixer mixer) => _mixer = mixer;

        public void SetChannelVolume(SoundChannel channel, float volume)
        {
            if (_mixer == null) return;

            string param = channel switch
            {
                SoundChannel.BGM     => ParamBGM,
                SoundChannel.SFX     => ParamSFX,
                SoundChannel.Voice   => ParamVoice,
                SoundChannel.Ambient => ParamAmbient,
                _                    => null,
            };
            if (param == null) return;

            // 0~1 → -80~0 dB 변환
            float db = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
            _mixer.SetFloat(param, db);
        }

        public void SetMute(bool mute) =>
            AudioListener.volume = mute ? 0f : 1f;
    }
}
