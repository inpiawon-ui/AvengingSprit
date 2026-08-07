using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace GameFramework.Core.Module.Resource
{
    /// <summary>
    /// Addressables 기반 에셋 로드 / 캐시 / 해제 구현체.
    ///
    /// CDN 카탈로그 업데이트 / 번들 다운로드는 IAddressableDownloadManager 를 사용하세요.
    /// </summary>
    public sealed class ResourceManager : IResourceManager
    {
        // ── 캐시 ──────────────────────────────────────────────────

        // 에셋 핸들: address → CacheEntry (RefCount + Scope)
        private readonly Dictionary<string, CacheEntry> _assetCache = new();

        // 인스턴스 핸들: GameObject.GetInstanceID() → (Handle, Scope)
        // Addressables.InstantiateAsync 로 생성된 오브젝트만 추적
        private readonly Dictionary<ulong, (AsyncOperationHandle<GameObject> Handle, string Scope)> _instanceCache = new();

        // ── IResourceManager : 캐시 조회 ─────────────────────────

        public bool IsCached(string address) => _assetCache.ContainsKey(address);

        // ── IResourceManager : 단일 에셋 로드 ─────────────────────

        public async UniTask<T> LoadAsync<T>(string address, string scope = null)
            where T : UnityEngine.Object
        {
            if (_assetCache.TryGetValue(address, out var cached))
            {
                cached.RefCount++;
                _assetCache[address] = cached;
                return (T)cached.Handle.Result;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle.ToUniTask();

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceManager] 로드 실패: {address}\n{handle.OperationException}");
                return null;
            }

            _assetCache[address] = new CacheEntry
            {
                Handle   = handle,
                RefCount = 1,
                Scope    = scope ?? string.Empty,
            };

            return handle.Result;
        }

        // ── IResourceManager : 다중 에셋 로드 ─────────────────────

        public async UniTask<IList<T>> LoadAllAsync<T>(string label, string scope = null)
            where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetsAsync<T>(label, null);
            await handle.ToUniTask();

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceManager] 레이블 로드 실패: {label}\n{handle.OperationException}");
                return Array.Empty<T>();
            }

            // 레이블 그룹 핸들은 "__label__{name}" 키로 단일 캐싱
            _assetCache[$"__label__{label}"] = new CacheEntry
            {
                Handle   = handle,
                RefCount = 1,
                Scope    = scope ?? string.Empty,
            };

            return handle.Result;
        }

        // ── IResourceManager : 인스턴스화 ─────────────────────────

        public async UniTask<GameObject> InstantiateAsync(string address, Transform parent = null,
            string scope = null)
        {
            var handle = parent != null
                ? Addressables.InstantiateAsync(address, parent)
                : Addressables.InstantiateAsync(address);

            await handle.ToUniTask();

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceManager] 인스턴스화 실패: {address}\n{handle.OperationException}");
                return null;
            }

            var go = handle.Result;
            _instanceCache[EntityId.ToULong(go.GetEntityId())] = (handle, scope ?? string.Empty);
            return go;
        }

        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null) return;

            var id = EntityId.ToULong(instance.GetEntityId());
            if (!_instanceCache.TryGetValue(id, out _)) return;

            Addressables.ReleaseInstance(instance);
            _instanceCache.Remove(id);
        }

        // ── IResourceManager : 선로드 ─────────────────────────────

        public async UniTask PreloadAsync(object key, IProgress<float> progress = null,
            string scope = null)
        {
            // 키에 속한 모든 리소스 위치를 먼저 조회
            AsyncOperationHandle<IList<IResourceLocation>> locHandle =
                Addressables.LoadResourceLocationsAsync(key);
            try
            {
                await locHandle.ToUniTask();
            }
            catch (InvalidKeyException)
            {
                Addressables.Release(locHandle);
                progress?.Report(1f);
                return;
            }

            var locations = locHandle.Result;
            Addressables.Release(locHandle);

            if (locations == null || locations.Count == 0)
            {
                Debug.LogWarning($"[ResourceManager] 선로드할 에셋 없음: {key}");
                progress?.Report(1f);
                return;
            }

            int total  = locations.Count;
            int loaded = 0;

            foreach (var location in locations)
            {
                string address = location.PrimaryKey;

                if (!_assetCache.ContainsKey(address))
                {
                    var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(location);
                    await handle.ToUniTask();

                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        _assetCache[address] = new CacheEntry
                        {
                            Handle   = handle,
                            RefCount = 1,
                            Scope    = scope ?? string.Empty,
                        };
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[ResourceManager] 선로드 실패: {address}\n{handle.OperationException}");
                        Addressables.Release(handle);
                    }
                }

                loaded++;
                progress?.Report((float)loaded / total);
            }
        }

        // ── IResourceManager : 해제 ───────────────────────────────

        public void Release(string address)
        {
            if (!_assetCache.TryGetValue(address, out var entry)) return;

            entry.RefCount--;
            if (entry.RefCount <= 0)
            {
                Addressables.Release(entry.Handle);
                _assetCache.Remove(address);
            }
            else
            {
                _assetCache[address] = entry;
            }
        }

        public void ReleaseAll(string scope)
        {
            // 에셋 캐시 해제 — 중간 컬렉션 생성 없이 직접 순회 후 일괄 제거
            var assetKeysToRemove = new List<string>();
            foreach (var kv in _assetCache)
            {
                if (kv.Value.Scope != scope) continue;
                Addressables.Release(kv.Value.Handle);
                assetKeysToRemove.Add(kv.Key);
            }
            foreach (var key in assetKeysToRemove)
                _assetCache.Remove(key);

            // 인스턴스 해제 — 씬 언로드 시 Unity 가 오브젝트를 파괴하지만
            // Addressables 핸들은 직접 해제해야 메모리 누수가 없음
            var instanceIdsToRemove = new List<ulong>();
            foreach (var kv in _instanceCache)
            {
                if (kv.Value.Scope != scope) continue;
                if (kv.Value.Handle.IsValid())
                    Addressables.Release(kv.Value.Handle);
                instanceIdsToRemove.Add(kv.Key);
            }
            foreach (var id in instanceIdsToRemove)
                _instanceCache.Remove(id);
        }

        public void ReleaseAll()
        {
            // 전체 에셋 일괄 해제 (모듈 Dispose 전용)
            foreach (var entry in _assetCache.Values)
                if (entry.Handle.IsValid()) Addressables.Release(entry.Handle);
            _assetCache.Clear();

            foreach (var (handle, _) in _instanceCache.Values)
                if (handle.IsValid()) Addressables.Release(handle);
            _instanceCache.Clear();
        }
    }
}
