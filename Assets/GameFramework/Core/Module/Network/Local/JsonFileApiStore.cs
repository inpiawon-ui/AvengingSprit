using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameFramework.Core.Module.Network.Local
{
    /// <summary>
    /// URL 경로를 로컬 파일 시스템에 매핑하는 저장소.
    /// 저장 위치: {persistentDataPath}/local_api/{path}.json
    /// </summary>
    internal sealed class JsonFileApiStore
    {
        private readonly string _baseDir;

        internal JsonFileApiStore()
        {
            _baseDir = Path.Combine(Application.persistentDataPath, "local_api");
        }

        /// <summary>파일이 존재하면 JSON 문자열을 반환, 없으면 null 반환.</summary>
        internal UniTask<string> ReadAsync(string path)
        {
            var filePath = BuildFilePath(path);
            if (!File.Exists(filePath))
                return UniTask.FromResult<string>(null);
            return UniTask.FromResult(File.ReadAllText(filePath));
        }

        /// <summary>파일에 JSON 문자열을 덮어쓴다. 중간 디렉토리는 자동 생성.</summary>
        internal UniTask WriteAsync(string path, string json)
        {
            var filePath = BuildFilePath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, json);
            return UniTask.CompletedTask;
        }

        /// <summary>파일을 삭제한다. 파일이 존재하면 true, 없으면 false 반환.</summary>
        internal UniTask<bool> DeleteAsync(string path)
        {
            var filePath = BuildFilePath(path);
            if (!File.Exists(filePath))
                return UniTask.FromResult(false);
            File.Delete(filePath);
            return UniTask.FromResult(true);
        }

        /// <summary>경로 순회(..)와 루트 경로를 차단해 안전한 절대 파일 경로를 반환한다.</summary>
        private string BuildFilePath(string path)
        {
            // 경로 순회(..) 먼저 차단
            if (path.Contains(".."))
                throw new ArgumentException($"잘못된 경로: {path}");

            // TrimStart 먼저 → IsPathRooted 나중: REST 경로(/users/me)의 선행 /를
            // 제거한 후 검증해야 정상 API 경로를 차단하지 않는다.
            // Windows에서 Path.IsPathRooted("/users/me") → true(드라이브 루트로 해석)이므로
            // TrimStart 전에 호출하면 모든 REST 경로가 ArgumentException으로 차단된다.
            var sanitized = path.TrimStart('/', '\\')
                               .Replace('/', Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(sanitized))
                throw new ArgumentException($"잘못된 경로: {path}");

            return Path.Combine(_baseDir, sanitized + ".json");
        }
    }
}
