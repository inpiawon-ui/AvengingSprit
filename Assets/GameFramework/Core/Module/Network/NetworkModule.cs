using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.Network.Local;

namespace GameFramework.Core.Module.Network
{
    /// <summary>
    /// INetworkClient를 CoreModule에 등록하는 모듈.
    /// CoreConfig에서 INetworkSettings를 읽어 LocalNetworkClient 또는 NetworkClient를 선택한다.
    /// INetworkSettings 미등록 시 기본값(real client, 빈 BaseUrl)으로 동작한다.
    /// </summary>
    [Module(Layer = ModuleLayer.Core, Provides = new[] { typeof(INetworkClient) })]
    public sealed class NetworkModule : IModule
    {
        private INetworkClient _client;

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            CoreConfig.TryGet<INetworkSettings>(out var settings);

            _client = (settings != null && settings.UseLocal)
                ? (INetworkClient)new LocalNetworkClient(new JsonFileApiStore())
                : CreateRealClient(settings?.BaseUrl ?? "");

            CoreModule.Register<INetworkClient>(_client);
            IsInitialized = true;
        }

        public void Initialize() { }

        public void Dispose()
        {
            CoreModule.Unregister<INetworkClient>();
            _client       = null;
            IsInitialized = false;
        }

        private static INetworkClient CreateRealClient(string baseUrl)
        {
            var client = new NetworkClient();
            if (!string.IsNullOrEmpty(baseUrl))
                client.SetBaseUrl(baseUrl);
            return client;
        }
    }
}
