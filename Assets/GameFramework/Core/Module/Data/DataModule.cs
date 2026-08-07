using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.Data.Backends;
using GameFramework.Core.Module.Data.Decorators;

namespace GameFramework.Core.Module.Data
{
    public sealed class DataModule : IModule
    {
        private readonly IDataBackend _backend;
        private DataManager           _manager;

        /// <summary>
        /// 암호화 키와 선택적 커스텀 백엔드를 주입합니다.
        /// 기본 구성: EncryptedBackend(JsonFileBackend, encryptionPassword)
        /// </summary>
        public DataModule(string encryptionPassword = "", IDataBackend backend = null)
        {
            _backend = backend ?? new EncryptedBackend(new JsonFileBackend(), encryptionPassword);
        }

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            _manager = new DataManager(_backend);
            CoreModule.Register<IDataManager>(_manager);
            IsInitialized = true;
        }

        public void Initialize() { }

        public void Dispose()
        {
            CoreModule.Unregister<IDataManager>();
            _manager = null;
            IsInitialized = false;
        }
    }
}
