using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GameFramework.Core.Module.Addressable
{
    /// <summary>
    /// Addressables CDN 카탈로그 관리 및 번들 다운로드 인터페이스.
    ///
    /// ── 사용 흐름 ──────────────────────────────────────────────────
    ///
    ///   [전체 다운로드 파이프라인]
    ///     1. GetTotalDownloadSizeAsync()   전체 키 기준 미다운로드 용량 확인
    ///     2. DownloadAllAsync(progress)    초기화 → 카탈로그 갱신 → 전체 다운로드
    ///
    ///   [씬/레이블 단위 제어 (BootController 등)]
    ///     1. CheckCatalogUpdateAsync()     원격 카탈로그 변경 확인
    ///     2. UpdateCatalogsAsync(keys)     변경된 카탈로그만 갱신
    ///     3. GetDownloadSizeAsync(key)     레이블 단위 용량 확인
    ///     4. DownloadDependenciesAsync()   레이블 단위 다운로드
    ///
    /// ── 구분 ───────────────────────────────────────────────────────
    ///   IResourceManager   : 에셋 로드 / 캐시 / 해제
    ///   IAddressableDownloadManager : CDN 카탈로그 / 다운로드 전담
    /// </summary>
    public interface IAddressableDownloadManager
    {
        /// <summary>현재 다운로드 진행 중이면 true.</summary>
        bool IsDownloading { get; }

        // ── 전체 다운로드 파이프라인 ──────────────────────────────

        /// <summary>
        /// 모든 ResourceLocator 키를 합산한 미다운로드 용량(bytes)을 반환합니다.
        /// 0이면 이미 전부 캐시된 상태입니다.
        /// </summary>
        UniTask<long> GetTotalDownloadSizeAsync();

        /// <summary>
        /// 전체 다운로드 파이프라인을 실행합니다.
        ///   Addressables 초기화 → 카탈로그 업데이트 → 전체 키 수집 → 다운로드
        /// progress : (downloadedBytes, totalBytes)
        /// </summary>
        UniTask DownloadAllAsync(IProgress<(long downloaded, long total)> progress = null);

        // ── 카탈로그 ─────────────────────────────────────────────

        /// <summary>
        /// 원격 카탈로그와 로컬을 비교합니다.
        /// 반환값: 업데이트가 필요한 카탈로그 키 목록. 비어 있으면 최신 상태입니다.
        /// </summary>
        UniTask<IReadOnlyList<string>> CheckCatalogUpdateAsync();

        /// <summary>
        /// 지정 카탈로그 키를 원격 최신 버전으로 갱신합니다.
        /// CheckCatalogUpdateAsync() 의 반환값을 그대로 전달하세요.
        /// </summary>
        UniTask UpdateCatalogsAsync(IReadOnlyList<string> keys);

        // ── 개별 키/레이블 다운로드 ───────────────────────────────

        /// <summary>
        /// 키/레이블에 해당하는 Location 이 Addressables에 등록되어 있는지 확인합니다.
        /// false 이면 해당 키로 GetDownloadSizeAsync / DownloadDependenciesAsync 를 호출할 필요가 없습니다.
        /// </summary>
        UniTask<bool> HasLocationsAsync(object key);

        /// <summary>
        /// 키에 해당하는 번들의 미다운로드 용량(bytes)을 반환합니다.
        /// 키가 등록되지 않은 경우 0을 반환합니다.
        /// key : string(주소/레이블), IEnumerable 모두 지원합니다.
        /// </summary>
        UniTask<long> GetDownloadSizeAsync(object key);

        /// <summary>
        /// 키에 해당하는 번들을 CDN에서 다운로드합니다.
        /// progress : 0.0 ~ 1.0
        /// </summary>
        UniTask DownloadDependenciesAsync(object key, IProgress<float> progress = null);
    }
}
