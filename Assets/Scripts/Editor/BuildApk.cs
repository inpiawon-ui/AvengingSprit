using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 테스트용 APK 한 방 빌드.
    ///
    /// 순서를 지켜야 한다 — **타겟을 먼저 안드로이드로 바꾸고 Addressable 을 굽는다.**
    /// 번들은 플랫폼마다 따로 구워지므로, 윈도우 타겟에서 구운 번들을 그대로 두고
    /// APK 를 만들면 실행 즉시 리소스 로드가 통째로 실패한다.
    ///
    /// 서명은 안 건드린다. 키스토어를 지정하지 않으면 Unity 가 디버그 키로 서명한다 —
    /// 폰에 설치해 확인하는 데는 충분하고, 스토어 업로드용 키는 별도 절차다.
    /// </summary>
    public static class BuildApk
    {
        private const string OutDir = "Build/Android";

        [MenuItem("Tools/Game/Build APK (테스트)")]
        public static void Run()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[Build] Android Build Support 가 없다. "
                               + "Unity Hub → Installs → Add modules 에서 "
                               + "Android Build Support + SDK/NDK + OpenJDK 를 설치한다.");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[Build] 타겟을 안드로이드로 바꾼다 — 시간이 걸린다(에셋 재임포트).");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildTargetGroup.Android, BuildTarget.Android))
                {
                    Debug.LogError("[Build] 타겟 전환 실패");
                    return;
                }
            }

            // AAB 가 아니라 APK. 스토어가 아니라 폰에 바로 넣을 것이다.
            EditorUserBuildSettings.buildAppBundle = false;

            Debug.Log("[Build] Addressable 번들을 굽는다…");
            AddressableAssetSettings.BuildPlayerContent(out var addrResult);
            if (!string.IsNullOrEmpty(addrResult.Error))
            {
                Debug.LogError($"[Build] Addressable 빌드 실패 — {addrResult.Error}");
                return;
            }

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled && File.Exists(s.path))
                .Select(s => s.path).ToArray();
            if (scenes.Length == 0) { Debug.LogError("[Build] 빌드 씬이 없다"); return; }

            var root = Directory.GetParent(Application.dataPath)!.FullName;
            var dir = Path.Combine(root, OutDir);
            Directory.CreateDirectory(dir);
            var apk = Path.Combine(dir, $"AVSR_{PlayerSettings.bundleVersion}.apk");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            });

            var s = report.summary;
            if (s.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Build] 실패 — {s.result} · 오류 {s.totalErrors}건");
                return;
            }
            Debug.Log($"[Build] 완료 — {apk} · {s.totalSize / 1024 / 1024}MB · "
                      + $"{s.totalTime.TotalMinutes:F1}분 · 경고 {s.totalWarnings}건");
        }
    }
}
