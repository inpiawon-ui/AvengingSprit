using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using GameFramework.Core.Common;

namespace GameFramework.Core.Base
{
    /// <summary>
    /// 게임 프로젝트에서 상속받아 RegisterModules()를 오버라이드한다.
    /// Tick 루프와 모듈 라이프사이클을 자동으로 관리한다.
    ///
    /// PersistAcrossScenes = true (기본값) 이면 씬 전환 시 파괴되지 않는다.
    /// 중복 인스턴스가 감지되면 나중에 생성된 쪽을 즉시 파괴한다.
    /// </summary>
    public abstract class CoreBootstrap : MonoBehaviour, IBootstrap
    {
        private readonly List<IModule> _modules = new();
        private readonly List<ITickable> _tickables = new();
        private readonly List<IPausable> _pausables = new();
        private readonly HashSet<Type> _registeredImplTypes = new();

        // ──────────────────────────────────────────────
        // 씬 유지 여부 — 서브클래스에서 false로 오버라이드 가능
        // ──────────────────────────────────────────────

        /// <summary>
        /// true(기본값): 씬 전환 후에도 파괴되지 않는다. (DontDestroyOnLoad)<br/>
        /// false: 씬과 함께 파괴된다. (씬 전용 Bootstrapper가 필요한 경우)
        /// </summary>
        protected virtual bool PersistAcrossScenes => true;

        // ──────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────

        protected virtual void Awake()
        {
            if (PersistAcrossScenes)
            {
                // 같은 타입의 인스턴스가 이미 존재하면 이 오브젝트를 파괴
                var existing = FindAnyObjectByType(GetType()) as CoreBootstrap;
                if (existing != null && existing != this)
                {
                    UnityEngine.Object.Destroy(gameObject);
                    return;
                }
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
            }

            RegisterConfigs();
            RegisterModules();
            InitializeAll();
        }

        protected virtual void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick(dt);
        }

        protected virtual void OnApplicationPause(bool paused)
        {
            for (int i = 0; i < _pausables.Count; i++)
            {
                if (paused) _pausables[i].Pause();
                else _pausables[i].Resume();
            }
        }

        protected virtual void OnDestroy() => DisposeAll();

        // ──────────────────────────────────────────────
        // IBootstrapper
        // ──────────────────────────────────────────────

        /// <summary>
        /// Core 레이어([Module(Layer = ModuleLayer.Core)]) 모듈을 자동 스캔·등록한다.
        /// 게임 프로젝트는 필요 시 override하여 추가 모듈을 등록하거나 동작을 교체한다.
        /// </summary>
        public virtual void RegisterModules()
        {
            AutoRegisterModules(typeof(CoreBootstrap).Assembly, ModuleLayer.Core);
        }

        public void InitializeAll()
        {
            for (int i = 0; i < _modules.Count; i++)
                _modules[i].Initialize();
        }

        public void DisposeAll()
        {
            // 역순 해제
            for (int i = _modules.Count - 1; i >= 0; i--)
                _modules[i].Dispose();

            _modules.Clear();
            _tickables.Clear();
            _pausables.Clear();
            _registeredImplTypes.Clear();
        }

        // ──────────────────────────────────────────────
        // Helper — 서브클래스에서 모듈 등록 시 사용
        // ──────────────────────────────────────────────

        /// <summary>
        /// 모듈 등록 전에 CoreConfig 설정을 등록하는 훅.
        /// 게임 프로젝트에서 override하여 CoreConfig.Set&lt;IXxxSettings&gt;()를 호출한다.
        /// </summary>
        public virtual void RegisterConfigs() { }

        protected void RegisterModule(IModule module)
        {
            var implType = module.GetType();
            if (!_registeredImplTypes.Add(implType))
                throw new InvalidOperationException(
                    $"[CoreBootstrap] {implType.FullName} 모듈 인스턴스가 이미 등록되었습니다. " +
                    "AutoRegisterModules 중복 호출 또는 base.RegisterModules() 재진입을 확인하세요.");
            module.Register();
            _modules.Add(module);
            if (module is ITickable t) _tickables.Add(t);
            if (module is IPausable p) _pausables.Add(p);
        }

        /// <summary>
        /// 단일 어셈블리에서 [Module] 어트리뷰트가 붙은 IModule 구현체를 수집하고
        /// DependsOn / Provides 선언 기반으로 위상 정렬한 뒤 자동 등록한다.
        /// 어트리뷰트가 없는 클래스는 무시된다 (opt-in).
        /// 수동 등록이 필요한 모듈은 이 메서드 호출 전에 RegisterModule()로 먼저 등록한다.
        /// </summary>
        protected void AutoRegisterModules(Assembly assembly)
            => AutoRegisterModules(new[] { assembly });

        /// <summary>
        /// 다중 어셈블리에서 [Module] 어트리뷰트가 붙은 IModule 구현체를 수집하고
        /// DependsOn / Provides 선언 기반으로 위상 정렬한 뒤 자동 등록한다.
        /// asmdef로 프레임워크와 게임 코드를 분리할 때 두 어셈블리를 함께 전달한다.
        ///
        /// 예시:
        ///   AutoRegisterModules(typeof(CoreBootstrap).Assembly, GetType().Assembly);
        /// </summary>
        protected void AutoRegisterModules(params Assembly[] assemblies)
        {
            var sorted = ModuleScanner.Scan(assemblies, CoreModule.GetRegisteredTypes(),
                layerFilter: null, alreadyRegisteredImplTypes: _registeredImplTypes);
            foreach (var module in sorted)
                RegisterModule(module);
        }

        /// <summary>
        /// 단일 어셈블리에서 특정 레이어 모듈만 스캔·등록한다.
        /// CoreBootstrap.RegisterModules() 기본 구현과 GameBootstrap.RegisterModules()에서 사용한다.
        /// </summary>
        protected void AutoRegisterModules(Assembly assembly, ModuleLayer layer)
            => AutoRegisterModules(new[] { assembly }, layer);

        /// <summary>
        /// 다중 어셈블리에서 특정 레이어 모듈만 스캔·등록한다.
        /// </summary>
        protected void AutoRegisterModules(Assembly[] assemblies, ModuleLayer layer)
        {
            var sorted = ModuleScanner.Scan(assemblies, CoreModule.GetRegisteredTypes(),
                layer, alreadyRegisteredImplTypes: _registeredImplTypes);
            foreach (var module in sorted)
                RegisterModule(module);
        }
    }
}