using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// 씬 진입 시 상시 표시되는 ~UI 프리팹을 Addressable 주소로 로드해
    /// SafeAreaPanel 하위에 배치한다. (04_scenes 규약의 LoadSceneUIAsync 진입점)
    ///
    /// 오버레이(~Panel·~Popup)는 이 로더가 아니라 UI.Register / OpenPopupAsync 로 연다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneUILoader : MonoBehaviour
    {
        [SerializeField] private RectTransform _safeAreaPanel;
        [SerializeField] private string _uiAddress;
        [SerializeField] private bool _loadOnStart = true;

        private GameObject _loadedUI;

        public GameObject LoadedUI => _loadedUI;

        private void Start()
        {
            if (_loadOnStart && !string.IsNullOrEmpty(_uiAddress))
            {
                // fire-and-forget: 씬 진입 UI 로드 완료를 이 시점에 기다릴 필요가 없다.
                LoadSceneUIAsync(_uiAddress).Forget();
            }
        }

        /// <summary>
        /// 지정한 Addressable 주소의 ~UI 프리팹을 로드해 SafeAreaPanel 자식으로 인스턴스화한다.
        /// scope 를 씬 이름으로 넘겨 씬 이탈 시 ResourceModule 이 자동 해제하도록 한다.
        /// </summary>
        public async UniTask LoadSceneUIAsync(string address)
        {
            if (_safeAreaPanel == null)
            {
                Debug.LogError("[SceneUILoader] SafeAreaPanel 이 지정되지 않았습니다.");
                return;
            }

            // 프레임워크 부트스트랩(GameLauncher) 이후에만 IResourceManager 가 등록된다.
            if (!CoreModule.TryGet<IResourceManager>(out var resource))
            {
                Debug.LogError("[SceneUILoader] IResourceManager 미등록 — 부트스트랩이 먼저 필요합니다.");
                return;
            }

            _loadedUI = await resource.InstantiateAsync(address, _safeAreaPanel, gameObject.scene.name);
        }
    }
}
