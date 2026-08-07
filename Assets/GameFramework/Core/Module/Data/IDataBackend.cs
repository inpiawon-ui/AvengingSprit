using Cysharp.Threading.Tasks;

namespace GameFramework.Core.Module.Data
{
    public interface IDataBackend
    {
        UniTask<bool>   WriteAsync(string key, string json);
        UniTask<string> ReadAsync (string key);
        void            Delete    (string key);
        bool            Has       (string key);
    }
}
