using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Core.Module.Sound.Backends
{
    /// <summary>AudioSource.volume / AudioListener 을 직접 제어하는 백엔드.</summary>
    public sealed class AudioSourceBackend : ISoundBackend
    {
        private readonly Dictionary<SoundChannel, float> _volumes = new()
        {
            { SoundChannel.BGM,     1f },
            { SoundChannel.SFX,     1f },
            { SoundChannel.Voice,   1f },
            { SoundChannel.Ambient, 1f },
        };

        public void SetChannelVolume(SoundChannel channel, float volume) =>
            _volumes[channel] = Mathf.Clamp01(volume);

        public float GetChannelVolume(SoundChannel channel) =>
            _volumes.TryGetValue(channel, out var v) ? v : 1f;

        public void SetMute(bool mute) =>
            AudioListener.volume = mute ? 0f : 1f;
    }
}
