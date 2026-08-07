# Eval: 모듈 라이프사이클 검증 기준

## 목적

GameModuleFramework의 핵심 동작인 **모듈 등록 → 초기화 → 해제** 라이프사이클이
올바르게 작동하는지 검증하는 평가 기준을 정의합니다.

---

## 평가 항목

### 1. Register → Initialize 순서 보장

| 입력 | 기대 결과 |
|------|-----------|
| 모듈 A, B, C 순서로 `RegisterModule()` 호출 | `Register()` → `Initialize()` 순서로 각각 호출됨 |
| `IsInitialized` 확인 | `Initialize()` 호출 후 `true` 반환 |

### 2. Dispose 역순 보장

| 입력 | 기대 결과 |
|------|-----------|
| A → B → C 순서로 등록 후 `DisposeAll()` 호출 | C → B → A 역순으로 `Dispose()` 호출됨 |

### 3. CoreModule 서비스 로케이터

| 입력 | 기대 결과 |
|------|-----------|
| `CoreModule.Register<IFoo>(impl)` 후 `CoreModule.Get<IFoo>()` | 등록한 `impl` 반환 |
| 미등록 타입 `Get<IBar>()` | `InvalidOperationException` 발생 |
| `TryGet<IBar>(out _)` | `false` 반환, 예외 없음 |
| `CoreModule.Reset()` 후 `TryGet<IFoo>(out _)` | 이전 등록 초기화, `false` 반환 |

### 4. 중복 Bootstrap 방지

| 입력 | 기대 결과 |
|------|-----------|
| `PersistAcrossScenes = true` 상태에서 동일 타입 Bootstrap 2개 생성 | 나중 인스턴스 즉시 파괴, 첫 번째 유지 |

### 5. ITickable / IPausable 연동

| 입력 | 기대 결과 |
|------|-----------|
| `ITickable` 구현 모듈 등록 후 `Update()` 호출 | `Tick(deltaTime)` 호출됨 |
| `IPausable` 구현 모듈 등록 후 `OnApplicationPause(true)` | `Pause()` 호출됨 |
| `OnApplicationPause(false)` | `Resume()` 호출됨 |

---

## Unity Test Runner 연동 (계획)

위 항목들은 향후 `Assets/Tests/EditMode/` 아래 Unity Test Framework(`com.unity.test-framework`)로
구현 예정. CI는 `.github/workflows/unity-ci.yml`에서 `EditMode` job으로 자동 실행하도록 추가 예정.

> 현재: 폴더(`Assets/Tests/EditMode/`)는 생성됐으나 테스트 파일과 CI 워크플로 미구현.

예상 파일 구조:
```
Assets/Tests/EditMode/
  CoreModuleTests.cs       ← 서비스 로케이터 테스트
  ModuleLifecycleTests.cs  ← Register/Initialize/Dispose 순서 테스트
  BootstrapTests.cs        ← 중복 방지, Tick/Pause 테스트
```

---

## 구현 골격 (테스트 파일 작성 시 참고)

```csharp
// Assets/Tests/EditMode/ModuleLifecycleTests.cs
using NUnit.Framework;
using GameFramework.Core;

public class ModuleLifecycleTests
{
    [SetUp] public void SetUp() => CoreModule.Reset();

    [Test]
    public void RegisterThenInitialize_CallsInOrder()
    {
        var log = new System.Collections.Generic.List<string>();
        var moduleA = new StubModule("A", log);
        var moduleB = new StubModule("B", log);

        CoreModule.RegisterModule(moduleA);
        CoreModule.RegisterModule(moduleB);
        CoreModule.InitializeAll();

        Assert.AreEqual(new[] { "A.Register", "B.Register", "A.Initialize", "B.Initialize" }, log);
    }

    [Test]
    public void DisposeAll_CallsReverseOrder()
    {
        var log = new System.Collections.Generic.List<string>();
        var moduleA = new StubModule("A", log);
        var moduleB = new StubModule("B", log);

        CoreModule.RegisterModule(moduleA);
        CoreModule.RegisterModule(moduleB);
        CoreModule.InitializeAll();
        log.Clear();
        CoreModule.DisposeAll();

        Assert.AreEqual(new[] { "B.Dispose", "A.Dispose" }, log);
    }
}

// Assets/Tests/EditMode/CoreModuleTests.cs
public class CoreModuleTests
{
    [SetUp] public void SetUp() => CoreModule.Reset();

    [Test]
    public void Get_ReturnsRegisteredService()
    {
        var impl = new StubService();
        CoreModule.Register<IStubService>(impl);
        Assert.AreSame(impl, CoreModule.Get<IStubService>());
    }

    [Test]
    public void Get_Unregistered_ThrowsInvalidOperationException()
    {
        Assert.Throws<System.InvalidOperationException>(() => CoreModule.Get<IStubService>());
    }

    [Test]
    public void TryGet_Unregistered_ReturnsFalse()
    {
        Assert.IsFalse(CoreModule.TryGet<IStubService>(out _));
    }
}
```

> StubModule, StubService 같은 테스트 더블은 `Assets/Tests/EditMode/Stubs/` 폴더에 분리해 둔다.
