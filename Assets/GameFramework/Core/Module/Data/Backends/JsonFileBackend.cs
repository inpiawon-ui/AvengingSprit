using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameFramework.Core.Module.Data.Backends
{
    /// <summary>복잡한 게임 데이터를 Application.persistentDataPath에 JSON 파일로 저장</summary>
    public sealed class JsonFileBackend : IDataBackend
    {
        private readonly string _directory;

        public JsonFileBackend(string subdirectory = "SaveData")
        {
            _directory = Path.Combine(Application.persistentDataPath, subdirectory);
            Directory.CreateDirectory(_directory);
        }

        private string FilePath(string key) =>
            Path.Combine(_directory, $"{SanitizeKey(key)}.json");

        public async UniTask<bool> WriteAsync(string key, string json)
        {
            try
            {
                // File API는 Task만 제공하므로 AsUniTask로 래핑한다.
                await File.WriteAllTextAsync(FilePath(key), json).AsUniTask();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonFileBackend] Write failed — key:{key} err:{e.Message}");
                return false;
            }
        }

        public async UniTask<string> ReadAsync(string key)
        {
            string path = FilePath(key);
            if (!File.Exists(path)) return null;
            try   { return await File.ReadAllTextAsync(path).AsUniTask(); }
            catch { return null; }
        }

        public void Delete(string key)
        {
            string path = FilePath(key);
            if (File.Exists(path)) File.Delete(path);
        }

        public bool Has(string key) => File.Exists(FilePath(key));

        private static string SanitizeKey(string key) =>
            string.Concat(key.Split(Path.GetInvalidFileNameChars()));
    }
}
