using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameFramework.Game.Editor.Build
{
    /// <summary>
    /// Addressables 빌드 결과물을 rclone 을 통해 Cloudflare R2 에 업로드합니다.
    ///
    /// 사전 조건:
    ///   - C:\rclone\rclone.exe 설치
    ///   - Assets/ConfigData/AddressableConfig.asset 에 remoteName / bucketName 설정
    ///   - Addressables 빌드 완료 (ServerData 폴더 존재)
    /// </summary>
    public class AddressableUploadHandler
    {
        public void OnAddressableUpload(PlatformType platformType,
                                        BuildType    buildType)
        {
            var config = LoadConfig();
            if (config == null) return;

            string platform   = platformType.ToString();
            string bundlePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ServerData"));

            if (!Directory.Exists(bundlePath))
            {
                Debug.LogError("[AddressableUploadHandler] ServerData 폴더가 없습니다. " +
                               "Addressables 빌드를 먼저 실행하세요.");
                return;
            }

            string source = Path.Combine(bundlePath, platform);
            string dest   = $"{config.remoteName}:{config.bucketName}/{platform}/";

            Debug.Log($"[AddressableUploadHandler] 업로드 시작: {source} → {dest}");

            var psi = new ProcessStartInfo
            {
                FileName               = @"C:\rclone\rclone.exe",
                Arguments              = $"sync \"{source}\" {dest} --progress",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            };

            var process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            string error  = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
                Debug.Log($"[AddressableUploadHandler] 업로드 완료!\n{output}");
            else
                Debug.LogError($"[AddressableUploadHandler] 업로드 실패:\n{error}");

            process.Close();
        }

        private static AddressableConfig LoadConfig()
        {
            const string path = "Assets/ConfigData/AddressableConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<AddressableConfig>(path);
            if (config == null)
                Debug.LogError($"[AddressableUploadHandler] AddressableConfig 를 찾을 수 없습니다: {path}");
            return config;
        }
    }
}
