using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameFramework.Core.Module.Data.Decorators
{
    /// <summary>
    /// primary 실패(예외/null 반환) 시 fallback으로 자동 전환하는 데코레이터.
    /// </summary>
    public sealed class FallbackBackend : IDataBackend
    {
        private readonly IDataBackend _primary;
        private readonly IDataBackend _fallback;

        public FallbackBackend(IDataBackend primary, IDataBackend fallback)
        {
            _primary  = primary  ?? throw new ArgumentNullException(nameof(primary));
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        public async UniTask<bool> WriteAsync(string key, string json)
        {
            try
            {
                bool ok = await _primary.WriteAsync(key, json);
                if (ok) return true;
            }
            catch (Exception e) { Debug.LogWarning($"[FallbackBackend] Primary write failed: {e.Message}"); }

            return await _fallback.WriteAsync(key, json);
        }

        public async UniTask<string> ReadAsync(string key)
        {
            try
            {
                string result = await _primary.ReadAsync(key);
                if (result != null) return result;
            }
            catch (Exception e) { Debug.LogWarning($"[FallbackBackend] Primary read failed: {e.Message}"); }

            return await _fallback.ReadAsync(key);
        }

        public void Delete(string key)
        {
            try   { _primary.Delete(key); }  catch { /* 무시 */ }
            _fallback.Delete(key);
        }

        public bool Has(string key)
        {
            try   { if (_primary.Has(key)) return true; } catch { /* 무시 */ }
            return _fallback.Has(key);
        }
    }
}
