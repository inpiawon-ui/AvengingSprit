using Cysharp.Threading.Tasks;
using Game.Module.Common;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Data;
using GameFramework.Core.Module.Input;
using GameFramework.Core.Module.Loading;
using GameFramework.Core.Module.Network;
using GameFramework.Core.Module.Scene;
using UnityEngine;

namespace Game.Module.Boot
{
    public sealed class GameLauncher : GameFramework.Game.GameBootstrap
    {
        [SerializeField] private NetworkConfig _networkConfig;

        [Tooltip("비어있지 않으면 부트 완료 후 이 씬을 로드한다. BootScene의 런처에만 지정한다.")]
        [SerializeField] private string _loadSceneOnBoot;

        public override void RegisterConfigs()
        {
            if (_networkConfig != null)
                CoreConfig.Set<INetworkSettings>(_networkConfig);
            // SoundConfig, LocalizeConfig 등 향후 여기에 추가
        }

        public override void RegisterModules()
        {
            // 세이브 — 평문 JSON(민감정보 없음). 수동 등록 모듈은 Auto 스캔 이전에 등록한다.
            RegisterModule(new DataModule());
            // 입력 — 뒤로가기(ESC·안드로이드 백) 처리에 필요하다.
            RegisterModule(new InputModule());
            // 로딩 가림막 — 씬 전환을 `LoadingStyle.Overlay` 로 요청하면서 이 모듈을
            // 등록하지 않아, 가림막 없이 전환돼 인게임 진입 순간 흰 화면이 보였다.
            RegisterModule(new LoadingModule());

            // NetworkModule은 [Module(Layer = ModuleLayer.Core)] 어트리뷰트로
            // base.RegisterModules()의 자동 스캔에서 등록됨 — 수동 등록 불필요
            base.RegisterModules();
            // Layer 필터로 게임 전용 모듈만 스캔 — 중복 등록 차단
            AutoRegisterModules(GetType().Assembly, ModuleLayer.Game);
        }

        private void Start()
        {
            // 중복 런처는 base.Awake(CoreBootstrap)에서 이미 파괴되므로,
            // 살아남은(최초) 런처만 여기 도달한다. BootScene 런처만 다음 씬을 로드한다.
            if (!string.IsNullOrEmpty(_loadSceneOnBoot))
            {
                LoadFirstSceneAsync().Forget(); // fire-and-forget: 최초 씬 전환, 대기 불필요
            }
        }

        private async UniTaskVoid LoadFirstSceneAsync()
        {
            await CoreModule.Get<ISceneManager>()
                .LoadAsync(new SceneLoadRequest { SceneName = _loadSceneOnBoot });
        }
    }
}
