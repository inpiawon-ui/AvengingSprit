using System;
using System.Threading;
using UnityEngine.SceneManagement;

namespace GameFramework.Core.Module.Scene
{
    public class SceneLoadRequest
    {
        public string            SceneName    { get; set; }
        public LoadSceneMode     Mode         { get; set; } = LoadSceneMode.Single;
        public LoadingStyle      LoadingStyle { get; set; } = LoadingStyle.None;
        public Action            BeforeUnload { get; set; }
        public Action            AfterLoad    { get; set; }
        public Action<float>     OnProgress   { get; set; }
        public CancellationToken CancelToken  { get; set; } = CancellationToken.None;
        /// <summary>
        /// LoadingStyle.LoadingScene 사용 시 진입할 씬 이름.
        /// 호출자가 게임 측 씬 이름 상수(예: SceneNames.Loading)를 명시해야 한다.
        /// 비어 있는 채로 LoadingStyle.LoadingScene을 요청하면 SceneManager가 ArgumentException을 던진다.
        /// </summary>
        public string            LoadingSceneName { get; set; }
    }
}
