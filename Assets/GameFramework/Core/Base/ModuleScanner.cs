using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFramework.Core.Common;

namespace GameFramework.Core.Base
{
    /// <summary>
    /// [Module] 어트리뷰트가 붙은 IModule 구현체를 어셈블리에서 수집하고
    /// DependsOn / Provides 선언을 기반으로 위상 정렬한 뒤 인스턴스 목록을 반환한다.
    /// </summary>
    public static class ModuleScanner
    {
        /// <summary>단일 어셈블리 스캔 — 다중 어셈블리 오버로드에 위임한다 (하위 호환).</summary>
        /// <param name="assembly">스캔할 어셈블리.</param>
        /// <param name="alreadySatisfied">수동 사전 등록으로 이미 충족된 인터페이스 타입 목록.</param>
        /// <param name="layerFilter">null이면 전체 스캔, 값이 있으면 해당 레이어만 스캔.</param>
        /// <param name="alreadyRegisteredImplTypes">이미 인스턴스화되어 등록된 모듈 구현 타입 목록. candidates에서 자동 제외된다.</param>
        public static IReadOnlyList<IModule> Scan(Assembly assembly, IEnumerable<Type> alreadySatisfied,
            ModuleLayer? layerFilter = null, IEnumerable<Type> alreadyRegisteredImplTypes = null)
            => Scan(new[] { assembly }, alreadySatisfied, layerFilter, alreadyRegisteredImplTypes);

        /// <summary>
        /// 다중 어셈블리 스캔 — asmdef로 분리된 프로젝트에서 프레임워크 어셈블리와
        /// 게임 어셈블리를 함께 전달할 때 사용한다.
        /// </summary>
        /// <param name="assemblies">스캔할 어셈블리 목록.</param>
        /// <param name="alreadySatisfied">수동 사전 등록으로 이미 충족된 인터페이스 타입 목록.</param>
        /// <param name="layerFilter">null이면 전체 스캔, 값이 있으면 해당 레이어만 스캔.</param>
        /// <param name="alreadyRegisteredImplTypes">이미 인스턴스화되어 등록된 모듈 구현 타입 목록. candidates에서 자동 제외된다.</param>
        public static IReadOnlyList<IModule> Scan(IEnumerable<Assembly> assemblies, IEnumerable<Type> alreadySatisfied,
            ModuleLayer? layerFilter = null, IEnumerable<Type> alreadyRegisteredImplTypes = null)
        {
            var satisfied            = new HashSet<Type>(alreadySatisfied);
            var skipImplementations  = new HashSet<Type>(alreadyRegisteredImplTypes ?? Enumerable.Empty<Type>());

            // 1. 후보 수집 — [Module] + IModule + public 파라미터 없는 생성자
            // 이미 등록된 구현 타입은 다중 AutoRegisterModules 호출 안전성을 위해 자동 제외
            var candidates = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass
                         && !t.IsAbstract
                         && typeof(IModule).IsAssignableFrom(t)
                         && t.GetCustomAttribute<ModuleAttribute>() != null
                         && t.GetConstructor(Type.EmptyTypes) != null
                         && !skipImplementations.Contains(t))
                .Select(t => new ModuleMeta(t, t.GetCustomAttribute<ModuleAttribute>()))
                .ToList();

            // 레이어 필터 적용 — null이면 전체, 값이 있으면 해당 레이어만
            if (layerFilter.HasValue)
                candidates = candidates.Where(m => m.Attr.Layer == layerFilter.Value).ToList();

            // 2. Provides 중복 검사 및 provider map 구축
            var providerMap = new Dictionary<Type, ModuleMeta>();
            foreach (var meta in candidates)
            {
                foreach (var provided in meta.Attr.Provides)
                {
                    if (providerMap.TryGetValue(provided, out var existing))
                        throw new ModuleDependencyException(
                            $"[ModuleScanner] '{provided.Name}'을 제공하는 모듈이 중복됩니다: " +
                            $"{existing.Type.Name}, {meta.Type.Name}");
                    providerMap[provided] = meta;
                }
            }

            // 3. 의존성 그래프 구성 (Kahn's algorithm)
            var inDegree = candidates.ToDictionary(m => m, _ => 0);
            var edges    = candidates.ToDictionary(m => m, _ => new List<ModuleMeta>());

            foreach (var meta in candidates)
            {
                foreach (var dep in meta.Attr.DependsOn)
                {
                    // 수동 사전 등록으로 이미 충족된 의존성은 그래프에서 제외
                    if (satisfied.Contains(dep)) continue;

                    if (!providerMap.TryGetValue(dep, out var provider))
                        throw new ModuleDependencyException(
                            $"[ModuleScanner] {meta.Type.Name}이 요구하는 {dep.Name}을 제공하는 [Module]이 없습니다. " +
                            $"해당 모듈에 [Module]을 추가하거나, AutoRegisterModules 호출 전에 수동으로 등록하세요.");

                    edges[provider].Add(meta);
                    inDegree[meta]++;
                }
            }

            // 4. 위상 정렬
            var queue  = new Queue<ModuleMeta>(candidates.Where(m => inDegree[m] == 0));
            var result = new List<IModule>(candidates.Count);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add((IModule)Activator.CreateInstance(current.Type));

                foreach (var next in edges[current])
                {
                    if (--inDegree[next] == 0)
                        queue.Enqueue(next);
                }
            }

            // 5. 순환 의존성 감지
            if (result.Count < candidates.Count)
            {
                var cycleNames = candidates
                    .Where(m => inDegree[m] > 0)
                    .Select(m => m.Type.Name);
                throw new ModuleDependencyException(
                    $"[ModuleScanner] 순환 의존성 감지: {string.Join(", ", cycleNames)}");
            }

            return result;
        }

        private sealed class ModuleMeta
        {
            public Type            Type { get; }
            public ModuleAttribute Attr { get; }
            public ModuleMeta(Type type, ModuleAttribute attr) { Type = type; Attr = attr; }
        }
    }

    public sealed class ModuleDependencyException : Exception
    {
        public ModuleDependencyException(string message) : base(message) { }
    }
}
