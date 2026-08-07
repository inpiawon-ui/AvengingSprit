using Cysharp.Threading.Tasks;
using GameFramework.Core.Module.Network;
using UnityEngine;

namespace GameFramework.Core.Module.Network.Local
{
    /// <summary>
    /// <see cref="INetworkClient"/>의 로컬 파일 기반 구현체.
    /// URL을 파일 경로로 직접 매핑해 CRUD 작업을 수행한다.
    /// 서버 측 로직 시뮬레이션 없음 — 요청 그대로 저장·반환.
    /// </summary>
    internal sealed class LocalNetworkClient : INetworkClient
    {
        private readonly JsonFileApiStore _store;

        internal LocalNetworkClient(JsonFileApiStore store)
        {
            _store = store;
        }

        // 로컬 파일 저장소는 baseUrl·헤더 개념이 없으므로 no-op
        public void SetBaseUrl(string baseUrl) { }
        public void SetHeader(string key, string value) { }
        public void RemoveHeader(string key) { }

        /// <summary>파일 읽기 → 200 / 없으면 404</summary>
        public async UniTask<NetworkResponse<T>> GetAsync<T>(string path)
        {
            var json = await _store.ReadAsync(path);
            if (json == null)
                return NetworkResponse<T>.Failure(404, "Not Found");
            return NetworkResponse<T>.Success(200, JsonUtility.FromJson<T>(json));
        }

        /// <summary>파일 쓰기 (신규) → 201</summary>
        public async UniTask<NetworkResponse<T>> PostAsync<T>(string path, object body)
        {
            var json = JsonUtility.ToJson(body);
            await _store.WriteAsync(path, json);
            return NetworkResponse<T>.Success(201, JsonUtility.FromJson<T>(json));
        }

        /// <summary>파일 쓰기 (덮어쓰기) → 200</summary>
        public async UniTask<NetworkResponse<T>> PutAsync<T>(string path, object body)
        {
            var json = JsonUtility.ToJson(body);
            await _store.WriteAsync(path, json);
            return NetworkResponse<T>.Success(200, JsonUtility.FromJson<T>(json));
        }

        /// <summary>파일 삭제 → 204 / 없으면 404</summary>
        public async UniTask<NetworkResponse<T>> DeleteAsync<T>(string path)
        {
            var deleted = await _store.DeleteAsync(path);
            if (!deleted)
                return NetworkResponse<T>.Failure(404, "Not Found");
            return NetworkResponse<T>.Success(204, default);
        }
    }
}
