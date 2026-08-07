using GameFramework.Core.Base;
using GameFramework.Core.Common;

namespace GameFramework.Core.Module.Input
{
    public sealed class InputModule : IModule, ITickable
    {
        private readonly IInputManager _manager;

        /// <summary>커스텀 구현체를 주입하거나 기본 레거시 InputManager를 사용합니다.</summary>
        public InputModule(IInputManager manager = null)
        {
            _manager = manager ?? new InputManager();
        }

        public bool   IsInitialized { get; private set; }

        public void Register()
        {
            CoreModule.Register<IInputManager>(_manager);
            IsInitialized = true;
        }

        public void Initialize() { }

        public void Tick(float deltaTime)
        {
            // 기본 구현(InputManager)만 액션 콜백 발화가 필요. 커스텀 구현은 위임 안 함.
            if (_manager is InputManager defaultImpl)
                defaultImpl.Tick(deltaTime);
        }

        public void Dispose()
        {
            CoreModule.Unregister<IInputManager>();
            IsInitialized = false;
        }
    }
}
