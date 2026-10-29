using UnityEngine;

namespace Game.Module.Common.Diagnostics
{
    /// <summary>
    /// 출시 · 시험 빌드(개발 빌드가 아닌 것)에서는 오류 · 예외 로그만 남긴다(PD 2026-10-30 「빌드에서 로그가 켜져 있는 건 아니지?」).
    ///
    /// ⚠ 유니티는 일반 빌드에서도 `Debug.Log` 를 그대로 출력하고, 이 프로젝트는 모든 로그에 호출 경로(스택)를 모으게
    ///   되어 있었다 — 판 중에 불리는 로그마다 폰에서 부담이 된다. 오류 · 예외는 크래시 원인을 찾는 데 쓰므로 남긴다.
    /// 호출 경로 설정은 빌드 직전에 BuildApk 가 맞춘다. 에디터 · 개발 빌드는 그대로 다 나온다.
    /// </summary>
    public static class ReleaseLogFilter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            if (Application.isEditor || Debug.isDebugBuild) return;
            Debug.unityLogger.filterLogType = LogType.Error;   // Error · Assert · Exception 만(경고 · 일반은 버림)
        }
    }
}
