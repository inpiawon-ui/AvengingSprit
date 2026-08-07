using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GameFramework.Core.Module.Scene
{
    public interface ISceneManager
    {
        UniTask               LoadAsync(SceneLoadRequest request);
        UniTask               UnloadAsync(string sceneName);
        string                CurrentScene    { get; }
        IReadOnlyList<string> LoadedScenes    { get; }
        bool                  IsTransitioning { get; }
    }
}
