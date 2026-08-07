using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;

namespace GameFramework.Core.Module.Data.Decorators
{
    /// <summary>
    /// AES-256 암호화 데코레이터.
    /// key/IV는 생성자에서 주입 (빈 값이면 기본 키 사용 — 프로덕션에서는 반드시 교체).
    /// </summary>
    public sealed class EncryptedBackend : IDataBackend
    {
        private readonly IDataBackend _inner;
        private readonly byte[]       _key;
        private readonly byte[]       _iv;

        public EncryptedBackend(IDataBackend inner, string password)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            using var sha = SHA256.Create();
            _key = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            _iv  = new byte[16];
            Array.Copy(_key, _iv, 16);
        }

        public async UniTask<bool> WriteAsync(string key, string json)
        {
            string encrypted = Encrypt(json);
            return await _inner.WriteAsync(key, encrypted);
        }

        public async UniTask<string> ReadAsync(string key)
        {
            string encrypted = await _inner.ReadAsync(key);
            if (string.IsNullOrEmpty(encrypted)) return null;
            return Decrypt(encrypted);
        }

        public void Delete(string key) => _inner.Delete(key);
        public bool Has   (string key) => _inner.Has(key);

        private string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _key; aes.IV = _iv;
            using var ms        = new MemoryStream();
            using var cs        = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs)) sw.Write(plainText);
            return Convert.ToBase64String(ms.ToArray());
        }

        private string Decrypt(string cipherText)
        {
            using var aes = Aes.Create();
            aes.Key = _key; aes.IV = _iv;
            byte[] buffer = Convert.FromBase64String(cipherText);
            using var ms  = new MemoryStream(buffer);
            using var cs  = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr  = new StreamReader(cs);
            return sr.ReadToEnd();
        }
    }
}
