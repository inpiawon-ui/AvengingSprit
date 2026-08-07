using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameFramework.Core.Module.Addressable
{
    /// <summary>
    /// Addressables CDN 카탈로그 관리 및 번들 다운로드 구현체.
    /// AddressableDownloadModule 에 의해 생성되고 CoreModule 에 등록됩니다.
    /// </summary>
    public sealed class AddressableDownloadManager : IAddressableDownloadManager
    {
        public bool IsDownloading { get; private set; }

        // ── 전체 다운로드 파이프라인 ──────────────────────────────

        public async UniTask<long> GetTotalDownloadSizeAsync()
        {
            var initHandle = Addressables.InitializeAsync();
            await initHandle.ToUniTask();
            Addressables.Release(initHandle);

            var allKeys = CollectAllKeys();
            if (allKeys.Count == 0) return 0L;

            AsyncOperationHandle<long> sizeHandle = Addressables.GetDownloadSizeAsync(allKeys);
            await sizeHandle.ToUniTask();
            long size = sizeHandle.Status == AsyncOperationStatus.Succeeded
                ? sizeHandle.Result
                : -1L;
            Addressables.Release(sizeHandle);
            return size;
        }

        public async UniTask DownloadAllAsync(IProgress<(long downloaded, long total)> progress = null)
        {
            if (IsDownloading)
            {
                Debug.LogWarning("[AddressableDownloadManager] 이미 다운로드가 진행 중입니다.");
                return;
            }

            IsDownloading = true;
            try
            {
                // 1. Addressables 초기화
                var initHandle = Addressables.InitializeAsync();
                await initHandle.ToUniTask();
                Addressables.Release(initHandle);

                // 2. 카탈로그 업데이트 확인
                AsyncOperationHandle<List<string>> checkHandle = Addressables.CheckForCatalogUpdates(false);
                await checkHandle.ToUniTask();

                if (checkHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError("[AddressableDownloadManager] 카탈로그 업데이트 확인 실패");
                    Addressables.Release(checkHandle);
                    return;
                }

                var catalogsToUpdate = checkHandle.Result;
                Addressables.Release(checkHandle);

                // 3. 변경된 카탈로그 갱신
                if (catalogsToUpdate.Count > 0)
                {
                    var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
                    await updateHandle.ToUniTask();

                    if (updateHandle.Status != AsyncOperationStatus.Succeeded)
                    {
                        Debug.LogError("[AddressableDownloadManager] 카탈로그 갱신 실패");
                        Addressables.Release(updateHandle);
                        return;
                    }

                    Addressables.Release(updateHandle);
                }

                // 4. 전체 키 수집
                var allKeys = CollectAllKeys();
                if (allKeys.Count == 0)
                {
                    Debug.Log("[AddressableDownloadManager] 다운로드할 키가 없습니다.");
                    return;
                }

                // 5. 전체 다운로드 용량 확인
                AsyncOperationHandle<long> sizeHandle = Addressables.GetDownloadSizeAsync(allKeys);
                await sizeHandle.ToUniTask();

                if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError("[AddressableDownloadManager] 다운로드 용량 확인 실패");
                    Addressables.Release(sizeHandle);
                    return;
                }

                long downloadSize = sizeHandle.Result;
                Addressables.Release(sizeHandle);

                if (downloadSize <= 0)
                {
                    Debug.Log("[AddressableDownloadManager] 이미 모든 리소스가 캐시되어 있습니다.");
                    progress?.Report((0L, 0L));
                    return;
                }

                Debug.Log($"[AddressableDownloadManager] 다운로드 크기: {downloadSize} bytes");

                // 6. 다운로드 실행
                var downloadHandle = Addressables.DownloadDependenciesAsync(
                    allKeys,
                    Addressables.MergeMode.Union,
                    false
                );

                while (!downloadHandle.IsDone)
                {
                    long downloaded = (long)(downloadHandle.PercentComplete * downloadSize);
                    progress?.Report((downloaded, downloadSize));
                    await UniTask.Yield();
                }

                if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    progress?.Report((downloadSize, downloadSize));
                    Debug.Log("[AddressableDownloadManager] 전체 다운로드 완료!");
                }
                else
                {
                    Debug.LogError($"[AddressableDownloadManager] 다운로드 실패: {downloadHandle.OperationException}");
                }

                Addressables.Release(downloadHandle);
            }
            finally
            {
                IsDownloading = false;
            }
        }

        // ── 카탈로그 ─────────────────────────────────────────────

        public async UniTask<IReadOnlyList<string>> CheckCatalogUpdateAsync()
        {
            AsyncOperationHandle<List<string>> handle = Addressables.CheckForCatalogUpdates(false);
            await handle.ToUniTask();
            var keys = handle.Result;
            Addressables.Release(handle);
            return (IReadOnlyList<string>)keys ?? Array.Empty<string>();
        }

        public async UniTask UpdateCatalogsAsync(IReadOnlyList<string> keys)
        {
            if (keys == null || keys.Count == 0) return;

            var handle = Addressables.UpdateCatalogs(keys);
            await handle.ToUniTask();

            if (handle.Status != AsyncOperationStatus.Succeeded)
                Debug.LogError($"[AddressableDownloadManager] 카탈로그 갱신 실패\n{handle.OperationException}");

            Addressables.Release(handle);
        }

        // ── 개별 키/레이블 다운로드 ───────────────────────────────

        public async UniTask<bool> HasLocationsAsync(object key)
        {
            var handle = Addressables.LoadResourceLocationsAsync(key);
            try
            {
                await handle.ToUniTask();
                bool exists = handle.Result != null && handle.Result.Count > 0;
                Addressables.Release(handle);
                return exists;
            }
            catch
            {
                Addressables.Release(handle);
                return false;
            }
        }

        public async UniTask<long> GetDownloadSizeAsync(object key)
        {
            if (!await HasLocationsAsync(key)) return 0L;

            AsyncOperationHandle<long> handle = Addressables.GetDownloadSizeAsync(key);
            await handle.ToUniTask();
            long size = handle.Result;
            Addressables.Release(handle);
            return size;
        }

        public async UniTask DownloadDependenciesAsync(object key, IProgress<float> progress = null)
        {
            if (!await HasLocationsAsync(key))
            {
                progress?.Report(1f);
                return;
            }

            var handle = Addressables.DownloadDependenciesAsync(key, false);

            while (!handle.IsDone)
            {
                progress?.Report(handle.GetDownloadStatus().Percent);
                await UniTask.Yield();
            }

            progress?.Report(1f);

            if (handle.Status != AsyncOperationStatus.Succeeded)
                Debug.LogError($"[AddressableDownloadManager] 다운로드 실패\n{handle.OperationException}");

            Addressables.Release(handle);
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────

        private static List<object> CollectAllKeys()
        {
            var allKeys = new List<object>();
            foreach (var locator in Addressables.ResourceLocators)
                foreach (var key in locator.Keys)
                    allKeys.Add(key);
            return allKeys;
        }
    }
}
