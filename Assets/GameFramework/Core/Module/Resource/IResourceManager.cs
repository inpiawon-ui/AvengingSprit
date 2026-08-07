using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Core.Module.Resource
{
    /// <summary>
    /// 에셋 로드 / 캐시 / 해제 인터페이스.
    ///
    /// ── 사용 흐름 ─────────────────────────────────────────────────
    ///
    ///   [씬 전환 LoadingScene 구간]
    ///     1. PreloadAsync(label, progress)   다음 씬 에셋 선로드 → 이후 LoadAsync 즉시 반환
    ///
    ///   [씬 내]
    ///     2. LoadAsync&lt;T&gt;(address)           단일 에셋 로드 (캐시 hit 시 즉시 반환)
    ///     3. InstantiateAsync(address)       에셋 로드 + GameObject 인스턴스 생성
    ///
    ///   [씬 이탈]
    ///     4. ReleaseAll(scope)               ResourceModule 이 OnSceneUnloading 구독으로 자동 처리
    ///
    /// ── 스코프(scope) 컨벤션 ─────────────────────────────────────
    ///   scope 를 씬 이름(SceneNames.*)으로 지정하면 씬 전환 시 자동 해제됩니다.
    ///   null 또는 생략 시 글로벌("")로 처리되어 앱 종료까지 유지됩니다.
    ///
    /// ── CDN 카탈로그 / 다운로드 ──────────────────────────────────
    ///   IAddressableDownloadManager (GameFramework.Addressable) 를 사용하세요.
    /// </summary>
    public interface IResourceManager
    {
        // ── 캐시 조회 ─────────────────────────────────────────────

        /// <summary>에셋이 메모리에 캐시되어 있으면 true 를 반환합니다.</summary>
        bool IsCached(string address);

        // ── 단일 에셋 로드 ────────────────────────────────────────

        /// <summary>
        /// Addressable 주소로 에셋을 비동기 로드합니다.
        /// 이미 캐시된 경우 즉시 반환합니다 (RefCount 증가).
        /// </summary>
        UniTask<T> LoadAsync<T>(string address, string scope = null) where T : Object;

        // ── 다중 에셋 로드 ────────────────────────────────────────

        /// <summary>Addressable 레이블에 속한 모든 에셋을 로드합니다.</summary>
        UniTask<IList<T>> LoadAllAsync<T>(string label, string scope = null) where T : Object;

        // ── 인스턴스화 ────────────────────────────────────────────

        /// <summary>
        /// 에셋을 로드하고 씬에 GameObject 인스턴스를 생성합니다.
        /// 핸들은 내부에서 추적합니다. 반드시 ReleaseInstance() 로 해제하세요.
        /// </summary>
        UniTask<GameObject> InstantiateAsync(string address, Transform parent = null, string scope = null);

        /// <summary>InstantiateAsync() 로 생성된 인스턴스를 파괴하고 핸들을 해제합니다.</summary>
        void ReleaseInstance(GameObject instance);

        // ── 선로드 ────────────────────────────────────────────────

        /// <summary>
        /// 키(주소 또는 레이블)에 해당하는 모든 에셋을 미리 캐시합니다.
        /// LoadingScene 구간에서 호출해두면 이후 LoadAsync 가 즉시 반환됩니다.
        /// progress : 0.0 ~ 1.0
        /// </summary>
        UniTask PreloadAsync(object key, IProgress<float> progress = null, string scope = null);

        // ── 해제 ─────────────────────────────────────────────────

        /// <summary>
        /// 단일 에셋의 RefCount 를 감소시킵니다.
        /// RefCount 가 0이 되면 Addressables 핸들을 해제하고 메모리에서 제거합니다.
        /// </summary>
        void Release(string address);

        /// <summary>
        /// 지정 스코프에 속한 모든 에셋 / 인스턴스를 해제합니다.
        /// ResourceModule 이 OnSceneUnloading 이벤트를 구독하여 자동으로 호출합니다.
        /// </summary>
        void ReleaseAll(string scope);

        /// <summary>스코프 구분 없이 캐시된 모든 에셋과 인스턴스를 해제합니다. 모듈 Dispose 시 사용.</summary>
        void ReleaseAll();
    }
}
