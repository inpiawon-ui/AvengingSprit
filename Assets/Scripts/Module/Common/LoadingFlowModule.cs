using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Events;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Loading;
using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// 씬 전환 가림막을 켜고 끈다.
    ///
    /// 씬 요청은 전부 `LoadingStyle.Overlay` 로 나가지만, 프레임워크
    /// `SceneManager` 는 그 값으로 **아무것도 하지 않는다** — 로딩 씬 방식만 구현돼 있다.
    /// 프레임워크는 건드리지 않기로 했으므로(3-tier 규칙) 게임 쪽에서 씬 이벤트를 듣고
    /// 직접 켠다. 어느 화면이 전환을 요청하든 여기 한 곳만 지나간다.
    ///
    /// 진행률은 두 구간으로 나눈다.
    ///   0 ~ 0.6   씬 자체 로드 (`OnSceneLoadProgress`)
    ///   0.6 ~ 1   인게임의 캐릭터 아틀라스 로드 (`BattleDirector` 가 직접 보고)
    /// 검은 화면이 길었던 진짜 원인이 뒤쪽 구간이라, 씬이 올라왔다고 가림막을 걷으면
    /// 고친 것이 없다. 인게임만 **준비 완료를 기다렸다가** 걷는다.
    /// </summary>
    [Module(Layer = ModuleLayer.Game, DependsOn = new[] { typeof(IEventBus) })]
    public sealed class LoadingFlowModule : IModule
    {
        /// <summary>씬 로드가 차지하는 진행률 몫. 나머지는 인게임 준비가 채운다.</summary>
        public const float SceneLoadShare = 0.6f;

        /// <summary>
        /// 인게임이 준비 완료를 알리지 않을 때 가림막을 강제로 걷는 시간.
        /// 로드에 실패해도 화면이 영영 막혀 있는 것보다는 낫다.
        /// </summary>
        private const float BattleReadyTimeout = 25f;

        private IDisposable _unloading, _progress, _loaded;
        private CancellationTokenSource _guard;

        public bool IsInitialized { get; private set; }

        public void Register() => IsInitialized = true;

        public void Initialize()
        {
            var bus = CoreModule.Get<IEventBus>();
            _unloading = bus.Subscribe<OnSceneUnloading>(OnUnloading);
            _progress  = bus.Subscribe<OnSceneLoadProgress>(OnProgress);
            _loaded    = bus.Subscribe<OnSceneLoaded>(OnLoaded);
        }

        public void Dispose()
        {
            _unloading?.Dispose(); _unloading = null;
            _progress?.Dispose();  _progress = null;
            _loaded?.Dispose();    _loaded = null;
            CancelGuard();
            IsInitialized = false;
        }

        // ─────────────────────────────────────────────────────────
        private static bool TryLoading(out ILoadingManager loading)
            => CoreModule.TryGet(out loading);

        private void OnUnloading(OnSceneUnloading _)
        {
            if (!TryLoading(out var loading)) return;
            CancelGuard();
            loading.SetProgress(0f);
            loading.SetMessage(string.Empty);
            loading.ShowAsync().Forget();   // fire-and-forget: 가림막은 즉시 뜬다
        }

        private void OnProgress(OnSceneLoadProgress e)
        {
            if (TryLoading(out var loading))
                loading.SetProgress(Mathf.Clamp01(e.Progress) * SceneLoadShare);
        }

        private void OnLoaded(OnSceneLoaded e)
        {
            if (!TryLoading(out var loading)) return;

            // 인게임은 씬이 올라온 뒤가 진짜 로딩이다 — 캐릭터 아틀라스를 그때 올린다.
            if (e.NextScene == SceneNames.InGame)
            {
                loading.SetProgress(SceneLoadShare);
                StartGuard(loading);
                return;
            }

            loading.SetProgress(1f);
            loading.HideAsync().Forget();   // fire-and-forget: 전환이 끝났다
        }

        /// <summary>인게임이 끝내 준비 완료를 안 알리면 시간이 지나 걷는다.</summary>
        private void StartGuard(ILoadingManager loading)
        {
            _guard = new CancellationTokenSource();
            GuardAsync(loading, _guard.Token).Forget();   // fire-and-forget: 안전장치
        }

        private static async UniTaskVoid GuardAsync(ILoadingManager loading, CancellationToken token)
        {
            bool cancelled = await UniTask
                .Delay(TimeSpan.FromSeconds(BattleReadyTimeout), DelayType.UnscaledDeltaTime,
                       cancellationToken: token)
                .SuppressCancellationThrow();
            if (cancelled || !loading.IsVisible) return;

            Debug.LogWarning("[Loading] 인게임 준비 신호가 없어 가림막을 강제로 걷는다.");
            await loading.HideAsync();
        }

        private void CancelGuard()
        {
            _guard?.Cancel();
            _guard?.Dispose();
            _guard = null;
        }
    }
}
