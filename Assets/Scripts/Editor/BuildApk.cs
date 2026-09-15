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
    ///
    /// **버전은 구울 때마다 끝자리가 올라간다**(`BumpVersion`). 파일명이 버전을 따르므로
    /// 지난 빌드가 지워지지 않는다 — 폰에 든 것이 어느 빌드인지 알 수 있고,
    /// 문제가 생기면 직전 것으로 되돌려 볼 수 있다.
    ///
    /// 다만 **심볼 폴더는 최신 하나만 남긴다**(`SweepOldSymbolFolders`). 한 번에 836 MB 라
    /// 몇 번만 구워도 몇 GB 가 된다. APK 는 그대로 쌓인다.
    /// </summary>
    public static class BuildApk
    {
        private const string OutDir = "Build/Android";

        /// <summary>
        /// 직전 빌드가 끝난 시각(EditorPrefs). 연달아 부르는 것을 막는 데 쓴다.
        ///
        /// ⚠ 자동화(MCP)로 메뉴를 부르면 **응답이 늦어 타임아웃 → 재시도**가 일어난다.
        ///   빌드는 몇 분씩 걸리므로 이 재시도가 그대로 두 번째·세 번째 빌드가 되어,
        ///   같은 내용의 APK 가 버전만 올라간 채 여러 개 쌓였다(0.1.1·0.1.2·0.1.3).
        /// </summary>
        private const string LastBuildKey = "AVSR.LastApkBuildTicks";
        private const int CooldownSeconds = 90;

        [MenuItem("Tools/Game/Build APK (테스트)")]
        public static void Run()
        {
            var last = new System.DateTime(
                System.Convert.ToInt64(EditorPrefs.GetString(LastBuildKey, "0")));
            var since = (System.DateTime.UtcNow - last).TotalSeconds;
            if (since < CooldownSeconds)
            {
                Debug.LogWarning($"[Build] {since:F0}초 전에 이미 구웠다 — 건너뛴다. "
                                 + $"{CooldownSeconds}초 뒤에 다시 부르면 굽는다. "
                                 + "(자동화 재시도로 같은 빌드가 여러 개 쌓이는 것을 막는다)");
                return;
            }
            EditorPrefs.SetString(LastBuildKey, System.DateTime.UtcNow.Ticks.ToString());

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

            // ⚠ **버전을 올리고 굽는다. 덮어쓰지 않는다.**
            //   예전에는 `AVSR_0.1.0.apk` 한 이름으로만 나와서, 새로 구울 때마다
            //   지난 것이 사라졌다 — 「이 폰에 있는 게 어느 빌드인지」를 알 수가 없고
            //   문제가 생겨도 직전 것으로 되돌려 볼 수가 없었다.
            string version = BumpVersion();
            var apk = Path.Combine(dir, $"AVSR_{version}.apk");

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

            SweepOldSymbolFolders(dir, version);
            MoveOlderApksAside(dir, version);
        }

        /// <summary>
        /// 지난 APK 를 `old/` 로 옮긴다. **지우지 않는다.**
        ///
        /// 버전마다 파일이 남으니 폴더에 0.1.6 · 0.1.7 이 나란히 있어
        /// 「어느 게 최신이냐」로 헷갈렸다(기획 2026-09-15). 최신 하나만 앞에 두고
        /// 지난 것은 한 칸 안으로 넣는다 — 되돌려 볼 일이 생기면 거기서 꺼낸다.
        /// </summary>
        private static void MoveOlderApksAside(string dir, string keepVersion)
        {
            string keep = $"AVSR_{keepVersion}.apk";
            string old = Path.Combine(dir, "old");
            int n = 0;
            foreach (var path in Directory.GetFiles(dir, "AVSR_*.apk"))
            {
                var name = Path.GetFileName(path);
                if (name == keep) continue;
                Directory.CreateDirectory(old);
                var to = Path.Combine(old, name);
                if (File.Exists(to))
                {
                    Debug.LogWarning($"[Build] old 에 {name} 이 이미 있어 안 옮겼다");
                    continue;
                }
                try { File.Move(path, to); n++; }
                catch (System.Exception e) { Debug.LogWarning($"[Build] {name} 을 old 로 못 옮겼다 · {e.Message}"); }
            }
            if (n > 0) Debug.Log($"[Build] 지난 APK {n}개를 old/ 로 옮겼다 — 앞에는 {keep} 하나만 남는다");
        }

        /// <summary>
        /// 지난 버전의 심볼 폴더를 치운다.
        ///
        /// Unity 는 IL2CPP 빌드마다 `<이름>_BackUpThisFolder_ButDontShipItWithYourGame`
        /// 를 새로 만든다. **한 번에 836 MB** 이고 자동으로 안 지워져서, 몇 번만 구우면
        /// 몇 GB 가 쌓인다(실제로 일곱 개 5.8 GB 가 쌓였다).
        ///
        /// 크래시 로그를 사람이 읽을 수 있게 풀어 주는 심볼이라 **가장 최근 것 하나는
        /// 남긴다** — 방금 만든 APK 에서 크래시가 나면 그게 필요하다.
        ///
        /// ⚠ **APK 는 건드리지 않는다.** 버전을 남겨 두는 것이 이 스크립트의 목적이다.
        /// ⚠ 이름이 정확히 맞는 폴더만 지운다. 사람이 그 자리에 둔 다른 폴더까지
        ///   쓸어 가면 안 된다.
        /// </summary>
        private static void SweepOldSymbolFolders(string dir, string keepVersion)
        {
            const string Suffix = "_BackUpThisFolder_ButDontShipItWithYourGame";
            string keep = $"AVSR_{keepVersion}{Suffix}";

            long freed = 0;
            int n = 0;
            foreach (var path in Directory.GetDirectories(dir))
            {
                var name = Path.GetFileName(path);
                if (!name.EndsWith(Suffix, System.StringComparison.Ordinal)) continue;
                if (name == keep) continue;

                long size = 0;
                try
                {
                    foreach (var f in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                        size += new FileInfo(f).Length;
                    Directory.Delete(path, true);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Build] 심볼 폴더를 못 지웠다 — {name} · {e.Message}");
                    continue;
                }
                freed += size;
                n++;
            }

            if (n > 0)
                Debug.Log($"[Build] 지난 심볼 폴더 {n}개 정리 — {freed / 1024 / 1024}MB 확보 "
                          + $"(최신 {keep} 은 남긴다)");
        }

        /// <summary>
        /// 끝자리를 하나 올리고 저장한다. `0.1.0` → `0.1.1` → `0.1.2` …
        ///
        /// `bundleVersionCode` 도 함께 올린다 — 안드로이드는 이 숫자로 새 버전인지를
        /// 판단한다. 안 올리면 폰이 「같은 버전」으로 보고 덮어 설치를 거절한다.
        ///
        /// 자리 수가 셋이 아니거나 숫자가 아니면(예: `0.2-beta`) 손대지 않고 그대로 쓴다 —
        /// 사람이 일부러 적어 둔 이름을 코드가 마음대로 바꾸지 않는다.
        /// </summary>
        private static string BumpVersion()
        {
            var cur = PlayerSettings.bundleVersion ?? string.Empty;
            var parts = cur.Split('.');
            if (parts.Length == 3
                && int.TryParse(parts[0], out int major)
                && int.TryParse(parts[1], out int minor)
                && int.TryParse(parts[2], out int patch))
            {
                var next = $"{major}.{minor}.{patch + 1}";
                PlayerSettings.bundleVersion = next;
                PlayerSettings.Android.bundleVersionCode += 1;
                AssetDatabase.SaveAssets();
                Debug.Log($"[Build] 버전 {cur} → {next} "
                          + $"(versionCode {PlayerSettings.Android.bundleVersionCode})");
                return next;
            }

            Debug.LogWarning($"[Build] 버전 '{cur}' 은 x.y.z 꼴이 아니라 그대로 쓴다 — "
                             + "덮어쓸 수 있으니 직접 확인한다.");
            return cur;
        }
    }
}
