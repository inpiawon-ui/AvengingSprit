using Cysharp.Threading.Tasks;

namespace GameFramework.Core.Module.Loading
{
    public interface ILoadingManager
    {
        UniTask ShowAsync();
        UniTask HideAsync();
        void    SetProgress(float progress);
        void    SetMessage(string message);
        bool    IsVisible { get; }
    }
}
