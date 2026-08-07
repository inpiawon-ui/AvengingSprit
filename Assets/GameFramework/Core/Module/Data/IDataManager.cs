using Cysharp.Threading.Tasks;

namespace GameFramework.Core.Module.Data
{
    public interface IDataManager
    {
        UniTask<bool> SaveAsync<T>(string key, T data);
        UniTask<T>    LoadAsync<T>(string key, T defaultValue = default);
        bool          Has(string key);
        void          Delete(string key);
        void          SetBackend(IDataBackend backend);
    }
}
