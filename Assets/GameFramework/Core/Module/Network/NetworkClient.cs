using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GameFramework.Core.Module.Network
{
    /// <summary>
    /// UnityWebRequest 기반 HTTP 클라이언트.
    /// JSON 직렬화에 UnityEngine.JsonUtility를 사용합니다.
    /// </summary>
    public sealed class NetworkClient : INetworkClient
    {
        private string                     _baseUrl = "";
        private readonly Dictionary<string, string> _headers = new();

        public void SetBaseUrl   (string baseUrl) => _baseUrl = string.IsNullOrEmpty(baseUrl) ? "" : baseUrl.TrimEnd('/');
        public void SetHeader    (string key, string value) => _headers[key] = value;
        public void RemoveHeader (string key) => _headers.Remove(key);

        public UniTask<NetworkResponse<T>> GetAsync<T>(string path) =>
            SendAsync<T>(UnityWebRequest.Get(BuildUrl(path)));

        public UniTask<NetworkResponse<T>> PostAsync<T>(string path, object body) =>
            SendAsync<T>(BuildPostRequest("POST", path, body));

        public UniTask<NetworkResponse<T>> PutAsync<T>(string path, object body) =>
            SendAsync<T>(BuildPostRequest("PUT", path, body));

        public UniTask<NetworkResponse<T>> DeleteAsync<T>(string path) =>
            SendAsync<T>(UnityWebRequest.Delete(BuildUrl(path)));

        // ── 내부 ──────────────────────────────────────────────

        private string BuildUrl(string path) =>
            string.IsNullOrEmpty(_baseUrl) ? path : $"{_baseUrl}/{path.TrimStart('/')}";

        private UnityWebRequest BuildPostRequest(string method, string path, object body)
        {
            string json    = body != null ? JsonUtility.ToJson(body) : "{}";
            byte[] bytes   = Encoding.UTF8.GetBytes(json);
            var    request = new UnityWebRequest(BuildUrl(path), method)
            {
                uploadHandler   = new UploadHandlerRaw(bytes),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        private async UniTask<NetworkResponse<T>> SendAsync<T>(UnityWebRequest request)
        {
            try
            {
                ApplyHeaders(request);

                await request.SendWebRequest();

                int    code = (int)request.responseCode;
                string text = request.downloadHandler?.text;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    T data = default;
                    if (!string.IsNullOrEmpty(text))
                    {
                        try   { data = JsonUtility.FromJson<T>(text); }
                        catch (Exception e) { Debug.LogWarning($"[NetworkClient] JSON 파싱 실패: {text}\n{e}"); }
                    }
                    return NetworkResponse<T>.Success(code, data);
                }

                Debug.LogWarning($"[NetworkClient] 요청 실패 ({code}): {request.error}");
                return NetworkResponse<T>.Failure(code, request.error);
            }
            finally
            {
                request.Dispose();
            }
        }

        private void ApplyHeaders(UnityWebRequest request)
        {
            foreach (var (key, value) in _headers)
                request.SetRequestHeader(key, value);
        }
    }
}
