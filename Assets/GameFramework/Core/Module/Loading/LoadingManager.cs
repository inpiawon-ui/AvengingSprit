using Cysharp.Threading.Tasks;

namespace GameFramework.Core.Module.Loading
{
    public sealed class LoadingManager : ILoadingManager
    {
        private readonly ILoadingView _view;

        public bool IsVisible { get; private set; }

        public LoadingManager(ILoadingView view) => _view = view;

        public async UniTask ShowAsync()
        {
            if (IsVisible) return;
            IsVisible = true;
            await _view.ShowAsync();
        }

        public async UniTask HideAsync()
        {
            if (!IsVisible) return;
            await _view.HideAsync();
            IsVisible = false;
        }

        public void SetProgress(float progress) => _view.UpdateProgress(progress);
        public void SetMessage(string message)   => _view.UpdateMessage(message);
    }
}
