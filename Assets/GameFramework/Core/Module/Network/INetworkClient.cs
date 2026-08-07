using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GameFramework.Core.Module.Network
{
    public interface INetworkClient
    {
        void SetBaseUrl(string baseUrl);
        void SetHeader (string key, string value);
        void RemoveHeader(string key);

        UniTask<NetworkResponse<T>> GetAsync <T>(string path);
        UniTask<NetworkResponse<T>> PostAsync <T>(string path, object body);
        UniTask<NetworkResponse<T>> PutAsync  <T>(string path, object body);
        UniTask<NetworkResponse<T>> DeleteAsync<T>(string path);
    }
}
