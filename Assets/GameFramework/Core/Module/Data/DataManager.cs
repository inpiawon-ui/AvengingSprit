using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameFramework.Core.Module.Data
{
    public sealed class DataManager : IDataManager
    {
        private IDataBackend _backend;

        public DataManager(IDataBackend backend)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public void SetBackend(IDataBackend backend) =>
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));

        public async UniTask<bool> SaveAsync<T>(string key, T data)
        {
            try
            {
                string json = JsonUtility.ToJson(data);
                return await _backend.WriteAsync(key, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataManager] SaveAsync failed — key:{key} err:{e.Message}");
                return false;
            }
        }

        public async UniTask<T> LoadAsync<T>(string key, T defaultValue = default)
        {
            try
            {
                if (!_backend.Has(key)) return defaultValue;
                string json = await _backend.ReadAsync(key);
                if (string.IsNullOrEmpty(json)) return defaultValue;
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataManager] LoadAsync failed — key:{key} err:{e.Message}");
                return defaultValue;
            }
        }

        public bool Has(string key)    => _backend.Has(key);
        public void Delete(string key) => _backend.Delete(key);
    }
}
