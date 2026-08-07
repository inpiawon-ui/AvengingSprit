using System;

namespace GameFramework.Core.Base
{
    /// <summary>
    /// Bootstrap 레이어를 나타낸다.
    /// AutoRegisterModules 호출 시 레이어 필터로 사용된다.
    /// </summary>
    public enum ModuleLayer { Core, Game }

    /// <summary>
    /// CoreBootstrap.AutoRegisterModules() 의 자동 등록 대상임을 선언한다.
    /// 어트리뷰트가 없으면 스캐너가 무시한다 (opt-in).
    ///
    /// 조건: public 파라미터 없는 생성자가 있어야 자동 인스턴스화된다.
    ///       생성자 파라미터가 필요한 모듈은 RegisterModule(new XxxModule(params)) 로 수동 등록한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ModuleAttribute : Attribute
    {
        /// <summary>이 모듈이 CoreModule에 등록하는 인터페이스 타입 목록.</summary>
        public Type[] Provides  { get; set; } = Array.Empty<Type>();

        /// <summary>이 모듈이 Register() 전에 CoreModule에 등록되어 있어야 하는 인터페이스 타입 목록.</summary>
        public Type[] DependsOn { get; set; } = Array.Empty<Type>();

        /// <summary>
        /// 이 모듈이 속하는 Bootstrap 레이어.
        /// CoreBootstrap은 Core, GameBootstrap은 Game 레이어 모듈만 스캔한다.
        /// </summary>
        public ModuleLayer Layer { get; set; } = ModuleLayer.Core;
    }
}
