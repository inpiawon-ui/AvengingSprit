using System;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Events;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.ObjectPool;
using GameFramework.Core.Module.Sound.Backends;
using UnityEngine;

namespace GameFramework.Core.Module.Sound
{
    public sealed class SoundModule : IModule, IPausable
    {
        private readonly ISoundBackend _backend;
        private readonly int           _sfxPoolInitial;
        private readonly int           _sfxPoolMax;

        private SoundManager       _manager;
        private IObjectPoolManager _pool;
        private GameObject         _sfxPrefab;  // AudioSource 풀 프리팹 (모듈 수명 동안 유지)
        private IDisposable        _sceneUnloadSub;

        /// <summary>
        /// 백엔드 미지정 시 AudioSourceBackend 사용.
        /// sfxPoolInitial / sfxPoolMax 로 SFX AudioSource 풀 크기를 조정할 수 있다.
        /// </summary>
        public SoundModule(ISoundBackend backend = null, int sfxPoolInitial = 4, int sfxPoolMax = 16)
        {
            _backend        = backend ?? new AudioSourceBackend();
            _sfxPoolInitial = sfxPoolInitial;
            _sfxPoolMax     = sfxPoolMax;
        }

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            _pool = CoreModule.Get<IObjectPoolManager>();

            // SFX 재생용 AudioSource 풀 등록
            _sfxPrefab = new GameObject("[SFX AudioSource]");
            _sfxPrefab.AddComponent<AudioSource>().playOnAwake = false;
            _sfxPrefab.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(_sfxPrefab);
            _pool.Register<AudioSource>(_sfxPrefab, _sfxPoolInitial, _sfxPoolMax);

            _manager = new SoundManager(_pool, _backend);
            CoreModule.Register<ISoundManager>(_manager);
            IsInitialized = true;
        }

        public void Initialize()
        {
            // 씬 전환 시 BGM 페이드아웃
            _sceneUnloadSub = CoreModule.Get<IEventBus>()
                .Subscribe<OnSceneUnloading>(_ => _manager.StopBGM(fadeOut: 0.5f));
        }

        public void Pause()  => _manager?.SetMute(true);
        public void Resume() => _manager?.SetMute(false);

        public void Dispose()
        {
            _sceneUnloadSub?.Dispose();
            _manager?.StopBGM();
            _manager?.Dispose();           // 진행 중인 fire-and-forget 작업 취소
            _pool?.Clear<AudioSource>();   // 풀 인스턴스 파괴 및 매니저 등록 해제
            CoreModule.Unregister<ISoundManager>();

            if (_sfxPrefab != null) UnityEngine.Object.Destroy(_sfxPrefab);
            _sfxPrefab      = null;
            _pool           = null;
            _manager        = null;
            _sceneUnloadSub = null;
            IsInitialized   = false;
        }
    }
}
