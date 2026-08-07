using GameFramework.Core.Base;
using GameFramework.Core.Common;

namespace GameFramework.Game
{
    /// <summary>
    /// GameFramework.Game 레이어 모듈을 자동 등록하는 중간 Bootstrap.
    /// base.RegisterModules()로 Core 레이어를 먼저 등록하고,
    /// 이후 Game 레이어([Module(Layer = ModuleLayer.Game)])를 추가 등록한다.
    ///
    /// 게임 프로젝트는 이 클래스를 상속하면 Core + Game 모듈을 모두 자동으로 얻는다.
    /// </summary>
    public abstract class GameBootstrap : CoreBootstrap
    {
        public override void RegisterModules()
        {
            base.RegisterModules();  // Core 레이어 모듈
            AutoRegisterModules(typeof(GameBootstrap).Assembly, ModuleLayer.Game);
        }
    }
}