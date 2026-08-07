using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.ObjectPool;
using GameFramework.Core.Module.Resource;
using UnityEngine;

namespace GameFramework.Core.Module.Sound
{
    public sealed class SoundManager : ISoundManager, IDisposable
    {
        private readonly IObjectPoolManager              _pool;
        private readonly ISoundBackend                   _backend;
        private readonly Dictionary<SoundChannel, float> _volumes = new()
        {
            { SoundChannel.BGM,     1f },
            { SoundChannel.SFX,     1f },
            { SoundChannel.Voice,   1f },
            { SoundChannel.Ambient, 1f },
        };

        // 매니저 Dispose 시 진행 중인 fire-and-forget 비동기 작업을 일괄 취소하기 위한 토큰 소스
        private readonly CancellationTokenSource _lifetimeCts = new();

        private AudioSource _bgmSource;
        private bool        _muted;

        public SoundManager(IObjectPoolManager pool, ISoundBackend backend)
        {
            _pool    = pool;
            _backend = backend;

            var go = new GameObject("[SoundManager_BGM]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _bgmSource      = go.AddComponent<AudioSource>();
            _bgmSource.loop = true;
        }

        // ──────────────────────────────────────────────
        // BGM
        // ──────────────────────────────────────────────

        public void PlayBGM(string address, float fadeIn = 0f) =>
            LoadAndPlayBGMAsync(address, fadeIn).Forget();

        private async UniTaskVoid LoadAndPlayBGMAsync(string address, float fadeIn)
        {
            if (!CoreModule.TryGet<IResourceManager>(out var rm)) return;

            var clip = await rm.LoadAsync<AudioClip>(address);
            if (clip == null || _bgmSource == null) return;

            _bgmSource.clip   = clip;
            _bgmSource.volume = fadeIn > 0f ? 0f : (_muted ? 0f : _volumes[SoundChannel.BGM]);
            _bgmSource.Play();

            if (fadeIn > 0f)
            {
                float target  = _muted ? 0f : _volumes[SoundChannel.BGM];
                float elapsed = 0f;
                while (elapsed < fadeIn && _bgmSource != null)
                {
                    elapsed += Time.deltaTime;
                    _bgmSource.volume = Mathf.Lerp(0f, target, elapsed / fadeIn);
                    await UniTask.Yield();
                }
                if (_bgmSource != null) _bgmSource.volume = target;
            }
        }

        public void StopBGM(float fadeOut = 0f) =>
            FadeOutAndStopBGMAsync(fadeOut).Forget();

        private async UniTaskVoid FadeOutAndStopBGMAsync(float fadeOut)
        {
            if (_bgmSource == null) return;

            if (fadeOut > 0f)
            {
                float start   = _bgmSource.volume;
                float elapsed = 0f;
                while (elapsed < fadeOut && _bgmSource != null)
                {
                    elapsed += Time.deltaTime;
                    _bgmSource.volume = Mathf.Lerp(start, 0f, elapsed / fadeOut);
                    await UniTask.Yield();
                }
            }
            if (_bgmSource != null) _bgmSource.Stop();
        }

        // ──────────────────────────────────────────────
        // SFX / Voice / Ambient
        // ──────────────────────────────────────────────

        public void Play(string address, SoundChannel channel = SoundChannel.SFX) =>
            PlayOneShotAsync(address, channel, _lifetimeCts.Token).Forget();

        private async UniTaskVoid PlayOneShotAsync(string address, SoundChannel channel, CancellationToken cancellationToken)
        {
            if (!CoreModule.TryGet<IResourceManager>(out var rm)) return;

            var clip = await rm.LoadAsync<AudioClip>(address);
            if (clip == null || cancellationToken.IsCancellationRequested) return;

            var source = _pool.Get<AudioSource>();
            if (source == null)
            {
                Debug.LogWarning("[SoundManager] AudioSource pool exhausted.");
                return;
            }

            source.clip   = clip;
            source.volume = _muted ? 0f : _volumes[channel];
            source.Play();

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(clip.length), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 매니저 Dispose로 인한 취소 — 즉시 풀에 반환하고 종료
            }

            // 풀이 이미 Clear되었으면 Return은 조용히 무시되도록 ObjectPool<T>가 보장한다.
            _pool.Return(source);
        }

        public void Stop(SoundChannel channel)
        {
            if (channel == SoundChannel.BGM) _bgmSource?.Stop();
        }

        // ──────────────────────────────────────────────
        // 볼륨 / 음소거
        // ──────────────────────────────────────────────

        public void SetVolume(SoundChannel channel, float volume)
        {
            volume = Mathf.Clamp01(volume);
            _volumes[channel] = volume;
            _backend.SetChannelVolume(channel, volume);

            if (channel == SoundChannel.BGM && _bgmSource != null)
                _bgmSource.volume = _muted ? 0f : volume;
        }

        public float GetVolume(SoundChannel channel) =>
            _volumes.TryGetValue(channel, out var v) ? v : 1f;

        public void SetMute(bool mute)
        {
            _muted = mute;
            _backend.SetMute(mute);
            if (_bgmSource != null)
                _bgmSource.volume = mute ? 0f : _volumes[SoundChannel.BGM];
        }

        // ──────────────────────────────────────────────
        // IDisposable
        // ──────────────────────────────────────────────

        public void Dispose()
        {
            // 진행 중인 fire-and-forget 작업 전체 취소 (PlayOneShot 등)
            if (!_lifetimeCts.IsCancellationRequested)
                _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
        }
    }
}
