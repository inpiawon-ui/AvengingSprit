using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameFramework.Core.Module.Data.Backends
{
    /// <summary>간단한 설정값(볼륨, 언어 등) 저장에 적합</summary>
    public sealed class PlayerPrefsBackend : IDataBackend
    {
        public UniTask<bool> WriteAsync(string key, string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
            return UniTask.FromResult(true);
        }

        public UniTask<string> ReadAsync(string key) =>
            UniTask.FromResult(PlayerPrefs.GetString(key, null));

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public bool Has(string key) => PlayerPrefs.HasKey(key);
    }
}
