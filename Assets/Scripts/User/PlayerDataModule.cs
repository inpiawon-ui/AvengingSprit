using System;
using Cysharp.Threading.Tasks;
using Game.Character;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.Data;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
using UnityEngine;

namespace Game.User
{
    /// <summary>
    /// 마스터 테이블을 로드하고 `IPlayerDataService` 를 등록한다.
    ///
    /// 저장소 구현체는 여기서만 갈아끼운다 — 서버 전환 시
    /// `new LocalUserDataRepository(...)` 를 `new ServerUserDataRepository(...)` 로 바꾸면 끝이다.
    /// </summary>
    [Module(Layer = ModuleLayer.Game,
            Provides = new[] { typeof(IPlayerDataService) },
            DependsOn = new[] { typeof(IEventBus), typeof(IDataManager), typeof(IResourceManager) })]
    public sealed class PlayerDataModule : IModule
    {
        private const string HostTableAddress     = "TableData/HostTable";
        private const string UltimateTableAddress = "TableData/UltimateTable";

        private PlayerDataService _service;

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            var bus  = CoreModule.Get<IEventBus>();
            var data = CoreModule.Get<IDataManager>();

            // 테이블은 Initialize 단계에서 비동기 로드해 주입한다.
            // Register 는 동기 계약이므로 여기서는 빈 테이블로 만들어 두고 뒤에서 채운다.
            _service = new PlayerDataService(new LocalUserDataRepository(data), bus, null, null);
            CoreModule.Register<IPlayerDataService>(_service);
            IsInitialized = true;
        }

        public void Initialize()
        {
            // fire-and-forget: 테이블·세이브 로드 완료는 UserDataReadyEvent 로 통지한다.
            BootstrapAsync().Forget();
        }

        private async UniTaskVoid BootstrapAsync()
        {
            var res = CoreModule.Get<IResourceManager>();
            HostTable hosts = null;
            UltimateTable ultimates = null;
            try
            {
                hosts = await res.LoadAsync<HostTable>(HostTableAddress);
                ultimates = await res.LoadAsync<UltimateTable>(UltimateTableAddress);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerDataModule] 테이블 로드 실패 — {e.Message}");
            }

            if (hosts == null)
            {
                Debug.LogError($"[PlayerDataModule] '{HostTableAddress}' 를 찾지 못했습니다. " +
                               "Tools/Game/Create Asset Tables 로 생성하십시오.");
                return;
            }

            var bus  = CoreModule.Get<IEventBus>();
            var data = CoreModule.Get<IDataManager>();
            _service = new PlayerDataService(new LocalUserDataRepository(data), bus, hosts, ultimates);
            CoreModule.Unregister<IPlayerDataService>();
            CoreModule.Register<IPlayerDataService>(_service);

            await _service.LoadAsync();
        }

        public void Dispose()
        {
            _service = null;
            CoreModule.Unregister<IPlayerDataService>();
            IsInitialized = false;
        }
    }
}
