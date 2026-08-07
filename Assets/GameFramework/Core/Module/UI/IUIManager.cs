using Cysharp.Threading.Tasks;

namespace GameFramework.Core.Module.UI
{
    public interface IUIManager
    {
        /// <summary>타입으로 패널을 열고 인스턴스를 반환</summary>
        UniTask<T> OpenAsync<T>() where T : UIPanel;

        /// <summary>Single 모드 패널을 타입으로 닫음</summary>
        UniTask CloseAsync<T>() where T : UIPanel;

        /// <summary>Multiple 모드 패널의 특정 인스턴스를 닫고 Destroy</summary>
        UniTask CloseAsync(UIPanel instance);

        /// <summary>스택 최상단 패널을 닫음</summary>
        UniTask CloseTopAsync();

        /// <summary>열려 있는 모든 패널을 닫음</summary>
        UniTask CloseAllAsync();

        /// <summary>Addressables 주소를 타입에 매핑해 등록</summary>
        void Register<T>(string address, UILayer layer = UILayer.Default,
                         UIInstanceMode mode = UIInstanceMode.Single) where T : UIPanel;

        /// <summary>스택에서 T 타입의 패널 중 가장 위에 있는 것을 반환. 없으면 null.</summary>
        T FindOpenPanel<T>() where T : UIPanel;

        int OpenCount { get; }
    }
}
