using Cysharp.Threading.Tasks;

namespace GameFramework.Core.Module.Loading
{
    public interface ILoadingView
    {
        UniTask ShowAsync();
        UniTask HideAsync();
        void    UpdateProgress(float value);
        void    UpdateMessage(string message);
    }
}
