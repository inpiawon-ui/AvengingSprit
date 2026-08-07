using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Events;
using GameFramework.Core.Module.EventBus;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFramework.Core.Module.Scene
{
    public sealed class SceneManager : ISceneManager
    {
        private readonly IEventBus    _bus;
        private readonly List<string> _loadedScenes = new();

        public string                CurrentScene    { get; private set; }
        public IReadOnlyList<string> LoadedScenes    => _loadedScenes;
        public bool                  IsTransitioning { get; private set; }

        public SceneManager(IEventBus bus)
        {
            _bus         = bus;
            CurrentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            _loadedScenes.Add(CurrentScene);
        }

        // ──────────────────────────────────────────────
        // ISceneManager
        // ──────────────────────────────────────────────

        public async UniTask LoadAsync(SceneLoadRequest req)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning("[SceneManager] Load request ignored — already transitioning.");
                return;
            }
            if (string.IsNullOrEmpty(req.SceneName))
                throw new ArgumentException("SceneName must not be null or empty.");
            if (req.LoadingStyle == LoadingStyle.LoadingScene && string.IsNullOrEmpty(req.LoadingSceneName))
                throw new ArgumentException("LoadingSceneName must be set when LoadingStyle is LoadingScene.");

            IsTransitioning = true;
            string prevScene = CurrentScene;

            try
            {
                // ① BeforeUnload 콜백 + 이벤트
                req.BeforeUnload?.Invoke();
                _bus.Publish(new OnSceneUnloading { SceneName = prevScene });

                req.CancelToken.ThrowIfCancellationRequested();

                // ② LoadingScene 방식: 로딩 씬 Additive 로드 → 이전 씬 언로드
                if (req.LoadingStyle == LoadingStyle.LoadingScene)
                {
                    await LoadInternalAsync(req.LoadingSceneName, LoadSceneMode.Additive, null, req.CancelToken);
                    if (req.Mode == LoadSceneMode.Single)
                        await UnloadInternalAsync(prevScene, CancellationToken.None);
                }

                req.CancelToken.ThrowIfCancellationRequested();

                // ③ 목적지 씬 로드
                // LoadingScene 방식에서 Single 모드를 사용하면 Unity가 LoadingScene까지
                // 자동 언로드하므로, 이 방식에서는 항상 Additive로 로드한다.
                var destMode = req.LoadingStyle == LoadingStyle.LoadingScene
                    ? LoadSceneMode.Additive
                    : req.Mode;

                await LoadInternalAsync(
                    req.SceneName, destMode,
                    p =>
                    {
                        req.OnProgress?.Invoke(p);
                        _bus.Publish(new OnSceneLoadProgress { Progress = p });
                    },
                    req.CancelToken);

                // LoadingScene + Single: 목적지 씬을 활성 씬으로 승격
                if (req.LoadingStyle == LoadingStyle.LoadingScene && req.Mode == LoadSceneMode.Single)
                {
                    var dest = UnityEngine.SceneManagement.SceneManager.GetSceneByName(req.SceneName);
                    if (dest.IsValid())
                        UnityEngine.SceneManagement.SceneManager.SetActiveScene(dest);
                }

                // ④ 로딩 씬 제거
                if (req.LoadingStyle == LoadingStyle.LoadingScene)
                    await UnloadInternalAsync(req.LoadingSceneName, CancellationToken.None);

                // ⑤ 상태 갱신
                if (req.Mode == LoadSceneMode.Single)
                {
                    CurrentScene = req.SceneName;
                    _loadedScenes.Clear();
                    _loadedScenes.Add(req.SceneName);
                }
                else
                {
                    if (!_loadedScenes.Contains(req.SceneName))
                        _loadedScenes.Add(req.SceneName);
                }

                // ⑥ AfterLoad 콜백 + 이벤트
                req.AfterLoad?.Invoke();
                _bus.Publish(new OnSceneLoaded { PrevScene = prevScene, NextScene = req.SceneName });
            }
            catch (OperationCanceledException)
            {
                _bus.Publish(new OnSceneLoadCancelled { SceneName = req.SceneName });
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        public async UniTask UnloadAsync(string sceneName)
        {
            await UnloadInternalAsync(sceneName, CancellationToken.None);
            _loadedScenes.Remove(sceneName);
        }

        // ──────────────────────────────────────────────
        // 내부 헬퍼
        // ──────────────────────────────────────────────

        private static async UniTask LoadInternalAsync(
            string sceneName, LoadSceneMode mode,
            Action<float> onProgress, CancellationToken ct)
        {
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, mode);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                ct.ThrowIfCancellationRequested();
                onProgress?.Invoke(op.progress / 0.9f);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            onProgress?.Invoke(1f);
            ct.ThrowIfCancellationRequested();
            op.allowSceneActivation = true;

            await op.ToUniTask(cancellationToken: ct);
        }

        private static async UniTask UnloadInternalAsync(string sceneName, CancellationToken ct)
        {
            var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
            if (op != null)
                await op.ToUniTask(cancellationToken: ct);
        }
    }
}
