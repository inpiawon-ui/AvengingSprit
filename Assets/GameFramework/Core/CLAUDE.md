# GameFramework.Core — 개발 가이드

이 파일은 `Assets/GameFramework/Core/` 하위 작업 시 루트 `CLAUDE.md`를 보완하는 규칙입니다.
루트 `CLAUDE.md`의 모든 규칙을 먼저 준수하고, 이 파일의 규칙을 추가로 적용합니다.

---

## 레이어 책임 (3-tier 중 최하위)

이 레이어는 **엔진·프로젝트 형태(앱·게임)·환경(모바일·PC)에 무관한 핵심 시스템**만 담는다.

- 게임 도메인 용어(Character / Score / Quest / Inventory 등)의 등장 **금지**
- "어떤 게임에나 필요한" 기능이라도 게임 도메인 의존성이 있다면 `GameFramework/Game/`에 둔다
- 프로젝트 고유 코드는 `Assets/Scripts/`에 둔다 — 이 레이어로의 역참조 금지

| 레이어 | 책임 |
|---|---|
| `Assets/GameFramework/Core/` (이 레이어) | 엔진·형태·환경 무관 핵심 시스템 |
| `Assets/GameFramework/Game/` | 어떤 게임에도 필요한 범용 기능·도구 |
| `Assets/Scripts/` | 이 특정 게임/프로젝트 고유 코드 |

---

## 디렉토리 구조

```
Core/
├── Common/       공통 인터페이스·유틸리티 (IModule, ITickable, IPausable, IEvent, INotify, RefVar 등)
├── Events/       프레임워크 내장 이벤트 구조체
├── Base/         CoreBootstrap, CoreModule, ModuleRegistry, ModuleScanner, ModuleAttribute
└── Module/       각 기능 모듈 (EventBus, Log, Scene, UI, ...)
```

---

## 네임스페이스

**기본 규칙: 폴더 경로 = 네임스페이스 (예외 없음)**

| 폴더 경로 | 네임스페이스 |
|-----------|-------------|
| `Assets/GameFramework/Core/Common/` | `GameFramework.Core.Common` |
| `Assets/GameFramework/Core/Events/` | `GameFramework.Core.Events` |
| `Assets/GameFramework/Core/Base/` | `GameFramework.Core.Base` |
| `Assets/GameFramework/Core/Module/<Module>/` | `GameFramework.Core.Module.<Module>` |
| `Assets/GameFramework/Core/Module/<Module>/<SubFolder>/` | `GameFramework.Core.Module.<Module>.<SubFolder>` |
| `Assets/GameFramework/Game/` | `GameFramework.Game` |
| `Assets/GameFramework/Game/Common/` | `GameFramework.Game.Common` |
| `Assets/GameFramework/Game/Module/<Module>/` | `GameFramework.Game.Module.<Module>` |

```csharp
// Core Base 계층 (CoreBootstrap, CoreModule, ModuleRegistry 등)
namespace GameFramework.Core.Base { }

// Core 각 기능 모듈
namespace GameFramework.Core.Module.EventBus { }
namespace GameFramework.Core.Module.Scene { }
namespace GameFramework.Core.Module.Sound { }
// ... 동일 패턴

// Game 레이어 범용 모듈
namespace GameFramework.Game.Module.Quest { }
namespace GameFramework.Game.Module.Action { }
```

---

## 모듈 파일 구조

`Module/<Name>/` 폴더 아래 아래 패턴을 따릅니다:

| 파일 | 역할 |
|------|------|
| `IXxxManager.cs` | CoreModule에 등록·조회되는 public API 인터페이스 |
| `XxxManager.cs` | 비즈니스 로직 구현체 |
| `XxxModule.cs` | `IModule` 구현 — 매니저 생성, 등록, 이벤트 구독 |
| `Backends/` | 교체 가능한 백엔드 구현 (Strategy 패턴) |

### XxxModule이 구현해야 할 인터페이스

> ⚠️ **`ICoreModule`은 존재하지 않는다.** 모든 모듈은 `IModule`(단일 인터페이스)을 구현한다.

`IModule`의 **필수 멤버 전체** (누락 시 컴파일 오류):

```csharp
public sealed class XxxModule : IModule          // IModule이 유일한 모듈 인터페이스
{
    public bool IsInitialized { get; private set; } // ← 필수

    public void Register()   { ... }             // ← 필수
    public void Initialize() { ... }             // ← 필수
    public void Dispose()    { ... }             // ← 필수
}
```

추가 기능이 필요하면 선택적으로 함께 구현한다:
- `IPausable` — `OnApplicationPause` 전파가 필요한 모듈 (예: SoundModule)
- `ITickable` — 매 프레임 처리가 필요한 모듈 (예: InputModule, TimerModule)

### XxxModule 필수 using 선언

모든 Module 파일에 아래 using이 필요하다. 누락 시 각 오류가 발생한다.

| using | 제공 심볼 | 누락 시 오류 |
|---|---|---|
| `using GameFramework.Core.Base;` | `CoreModule`, `[Module]`, `ModuleLayer` | CS0103, CS0246 |
| `using GameFramework.Core.Common;` | `IModule`, `ITickable`, `IPausable` | CS0246 |
| `using System;` | `IDisposable` | CS0246 |

```csharp
// ✅ 모든 Module 파일의 최소 using 선언
using System;                                   // IDisposable
using GameFramework.Core.Base;                  // CoreModule, [Module] 어트리뷰트, ModuleLayer
using GameFramework.Core.Common;                // IModule, ITickable, IPausable
// + 모듈별 추가 using (예: GameFramework.Core.Module.EventBus 등)
```

---

## 모듈 라이프사이클 규칙

### Register() / Initialize() 역할 분리 (필수)

```csharp
public void Register()
{
    // 1. 인스턴스 생성
    // 2. CoreModule.Register<IXxxManager>(_manager) 호출
    // 3. IsInitialized = true
    // ❌ 이벤트 구독 금지 — Initialize() 로 이동
}

public void Initialize()
{
    // 1. 이벤트 구독 (구독 토큰 반드시 필드에 저장)
    // 2. 초기 상태 설정
    // ✅ 이 시점에 모든 모듈의 Register()가 완료됨
}

public void Dispose()
{
    // 1. 이벤트 구독 토큰 Dispose (null 할당 전)
    // 2. 리소스 해제
    // 3. CoreModule.Unregister<IXxxManager>()
    // 4. 모든 필드 null 할당
    // 5. IsInitialized = false
}
```

### 이벤트 구독 토큰 관리 (필수)

`IEventBus.Subscribe<T>()` 반환값(`IDisposable`)은 **반드시 필드에 저장**하고 `Dispose()`에서 해제합니다.

```csharp
// ❌ 잘못된 패턴 — 토큰 누락, 구독 취소 불가
CoreModule.Get<IEventBus>().Subscribe<OnSceneUnloading>(_ => ...);

// ✅ 올바른 패턴
_sceneUnloadSub = CoreModule.Get<IEventBus>()
    .Subscribe<OnSceneUnloading>(_ => ...);

// Dispose()에서
_sceneUnloadSub?.Dispose();
_sceneUnloadSub = null;
```

---

## C# using 지시문 규칙 (필수)

Unity 프로젝트는 전역 using이 제한적입니다. 아래 타입 사용 시 **반드시** 명시적으로 추가합니다:

| 타입 | 필요한 using |
|------|-------------|
| `IDisposable`, `Array`, `Exception` | `using System;` |
| `List<T>`, `Dictionary<K,V>` | `using System.Collections.Generic;` |
| `UniTask` | `using Cysharp.Threading.Tasks;` |

> **⚠️ CS0246 방지**: `IDisposable`은 `System` 네임스페이스 소속이며 Unity의 암시적 전역 using에
> 포함되지 않습니다. `IDisposable` 타입의 필드나 변수를 선언할 때 `using System;`이 없으면
> `CS0246: The type or namespace name 'IDisposable' could not be found` 오류가 발생합니다.
>
> **이벤트 구독 토큰(`IDisposable`)을 필드로 선언하는 모든 Module 파일에 `using System;`을 추가하세요.**

> **⚠️ CS0104 방지 — `Object` 모호성**: `using System;`과 `using UnityEngine;`을 동시에 선언하면
> `Object`가 `System.Object`와 `UnityEngine.Object` 사이에서 모호해져
> `CS0104: 'Object' is an ambiguous reference` 오류가 발생합니다.
>
> **`UnityEngine.Object`의 정적 메서드(`Destroy`, `DontDestroyOnLoad`, `Instantiate` 등)는
> 반드시 `UnityEngine.Object.Xxx(...)` 형태로 완전한 한정 이름을 사용하세요.**

```csharp
// ❌ CS0104 — using System; + using UnityEngine; 동시 선언 시 모호
Object.DontDestroyOnLoad(go);
Object.Destroy(go);

// ✅ 완전한 한정 이름으로 모호성 제거
UnityEngine.Object.DontDestroyOnLoad(go);
UnityEngine.Object.Destroy(go);
```

```csharp
// ✅ IDisposable 필드를 가진 모든 모듈의 올바른 using 선언
using System;                                   // IDisposable
using GameFramework.Core.Base;                  // CoreModule, IModule
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;       // IEventBus

namespace GameFramework.Core.Module.Xxx
{
    public sealed class XxxModule : IModule
    {
        private IDisposable _sub;      // using System; 없으면 CS0246
        ...
    }
}
```

---

## CoreModule 사용 가이드

```csharp
// 필수 의존성 — 미등록 시 예외 발생, Register()에서 사용
CoreModule.Get<IEventBus>();

// 선택적 의존성 — 미등록 시 false 반환
if (CoreModule.TryGet<IFoo>(out var foo)) { ... }
```

---

## Bootstrap

`CoreBootstrap` (추상 `MonoBehaviour`)이 모듈 라이프사이클을 구동합니다. 게임 프로젝트는 이를 상속하고 `RegisterModules()`를 오버라이드합니다.

### 수동 등록 패턴 (생성자 파라미터가 필요한 모듈 포함 시)

```csharp
public class GameBootstrap : CoreBootstrap {
    public override void RegisterModules() {
        RegisterModule(new EventBusModule());          // 반드시 첫 번째
        RegisterModule(new LogModule());
        RegisterModule(new SceneModule());
        RegisterModule(new DataModule("encryptionKey")); // 생성자 파라미터 필요
        // ...
    }
}
```

### 자동 등록 패턴 (`[Module]` 어트리뷰트 기반)

파라미터 없는 생성자를 가진 모듈은 `AutoRegisterModules`로 자동 수집·위상 정렬·등록할 수 있습니다.

```csharp
// 단일 어셈블리 (asmdef 미사용 시)
public class GameBootstrap : CoreBootstrap {
    public override void RegisterModules() {
        AutoRegisterModules(GetType().Assembly);
    }
}

// 다중 어셈블리 (asmdef로 프레임워크/게임 코드 분리 시)
public class GameBootstrap : CoreBootstrap {
    public override void RegisterModules() {
        AutoRegisterModules(
            typeof(CoreBootstrap).Assembly,  // 프레임워크 어셈블리
            GetType().Assembly               // 게임 어셈블리
        );
    }
}
```

- 생성자 파라미터가 필요한 모듈은 `AutoRegisterModules` 호출 **전에** `RegisterModule()`로 수동 등록한다.
  수동 등록된 타입은 `alreadySatisfied`로 처리되어 자동 스캔에서 제외된다.
- `CoreBootstrap.Update()` — 모든 `ITickable` 모듈을 매 프레임 tick합니다.
- `OnApplicationPause()` — 모든 `IPausable` 모듈에 전파됩니다.
- `OnDestroy()` — 등록 역순으로 모듈을 Dispose합니다.

---

## Event Bus

타입 안전 pub/sub, 우선순위 지원. 모든 이벤트는 `IEvent`를 구현합니다 (GC 최소화를 위해 `struct` 권장):

```csharp
var bus = CoreModule.Get<IEventBus>();
bus.Publish(new OnSceneLoaded { SceneName = "Game" });
IDisposable sub = bus.Subscribe<OnSceneLoaded>(e => ..., EventPriority.High);
```

- `IEventBusDeferred` — 이벤트를 큐에 쌓고 매 프레임(`ITickable`) flush합니다.
- 내장 씬 이벤트: `Assets/GameFramework/Core/Events/`

---

## 백엔드 / 확장성 패턴

여러 모듈이 생성자를 통해 백엔드를 주입받습니다 (Strategy 패턴):

```csharp
// Data 모듈: EncryptedBackend가 JsonFileBackend를 데코레이트
new DataModule(encryptionPassword: "secret");
// Analytics: NoOp 대신 실제 백엔드로 교체
new AnalyticsModule(new MyAnalyticsBackend());
// Sound: 교체 가능한 ISoundBackend
```

---

## 비동기 패턴

장시간 실행 작업은 모두 **UniTask**를 사용합니다 (`Task` 또는 `Coroutine` 사용 금지):

```csharp
await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest { SceneName = "Game" });
```

---

## [Module] 어트리뷰트 — DependsOn 의미

`DependsOn`은 **Register() 단계 전용**이다.

```
DependsOn = "내 Register()가 실행되기 전에 CoreModule에 등록되어 있어야 하는 타입 목록"
```

| 케이스 | DependsOn 선언 여부 |
|--------|-------------------|
| `Register()`에서 `CoreModule.Get<T>()` 직접 호출 | ✅ 필수 |
| `Register()` 시점 이전 초기화 순서 강제 (직접 미사용) | ✅ 허용 — 반드시 주석으로 이유 명시 |
| `Initialize()`에서만 사용 | ❌ 불필요 (Initialize()는 모든 Register() 완료 후 실행 보장) |

```csharp
// ✅ Register()에서 직접 사용 — DependsOn 필수
[Module(Provides = new[] { typeof(ISceneManager) },
        DependsOn = new[] { typeof(IEventBus) })]
public sealed class SceneModule : IModule {
    public void Register() {
        var bus  = CoreModule.Get<IEventBus>(); // 직접 사용
        _manager = new SceneManager(bus);
        CoreModule.Register<ISceneManager>(_manager);
    }
}

// ✅ 순서 강제 목적 — DependsOn 허용, 주석 필수
// IAddressableDownloadManager는 Register()에서 직접 사용하지 않지만
// Addressable 카탈로그 초기화 순서를 보장하기 위해 선언한다.
[Module(Provides  = new[] { typeof(IResourceManager) },
        DependsOn = new[] { typeof(IAddressableDownloadManager) })]
public sealed class ResourceModule : IModule {
    public void Register() {
        _manager = new ResourceManager(); // IAddressableDownloadManager 미사용
        CoreModule.Register<IResourceManager>(_manager);
    }
}

// ❌ Initialize()에서만 사용 — DependsOn 불필요
[Module(Provides = new[] { typeof(ITimerManager) })]  // DependsOn 없음
public sealed class TimerModule : IModule {
    public void Register() {
        _manager = new TimerManager();
        CoreModule.Register<ITimerManager>(_manager);
    }
    public void Initialize() {
        // Initialize()는 모든 Register() 완료 후 실행 보장 → IEventBus 사용 가능
        _sub = CoreModule.Get<IEventBus>().Subscribe<OnSceneUnloading>(...);
    }
}
```

---

## 모듈 등록 순서 제약

1. `EventBusModule` — **반드시 첫 번째** (모든 모듈이 의존)
2. `AddressableDownloadModule` — `ResourceModule` 앞에 위치
3. `ObjectPoolModule` — `SoundModule` 앞에 위치 (SFX 풀 의존)
4. 나머지 — 순서 자유 (`LogModule`은 가급적 앞쪽 권장)

> **`AutoRegisterModules` 사용 시**: 위 순서 제약은 각 모듈의 `DependsOn` 선언으로 자동 보장된다.
> 위상 정렬 로직이 DependsOn 그래프를 따라 등록 순서를 결정하므로 수동으로 순서를 맞출 필요가 없다.
> 단, `IEventBus`에 의존하는 모듈(`SceneModule` 등)은 `DependsOn = new[] { typeof(IEventBus) }`를 반드시 선언해야 한다.

---

## 파일 이동 시 네임스페이스 검증 체크리스트

폴더 간 파일을 이동할 때 반드시 아래 항목을 확인한다.

### 이동하는 파일 자체 검증
- [ ] `namespace` 선언이 새 폴더 경로를 반영하는가?
  - 규칙: 폴더 경로 = 네임스페이스 (예외 없음)
  - 예: `Core/Base/` → `namespace GameFramework.Core.Base`
- [ ] 파일 내 `using` 구문이 여전히 유효한가? (이전 namespace 참조가 없는가)
- [ ] `.meta` 파일도 함께 이동/삭제됐는가?

### 원본 파일이 삭제된 경우 — 참조 파일 업데이트
- [ ] 이동한 타입(`class`, `struct`, `enum`, `interface`)을 `using`하는 파일을 grep으로 찾았는가?
  ```
  Grep: using <구버전 네임스페이스>;
  ```
- [ ] 찾은 파일 전체에 새 `using <신버전 네임스페이스>;`를 추가하고 구버전 using을 제거했는가?
- [ ] 구버전 namespace 잔존 여부를 재확인했는가? → 0건이어야 함

### 최종 확인
- [ ] Unity 컴파일 오류 없음 확인 (CS0103, CS0246 등)
- [ ] 이동된 타입이 속한 어셈블리의 다른 파일들도 영향 없음 확인

---

## 코드 스타일

- 주석은 **한국어**로 작성
- 모든 Module / Manager 클래스에 `sealed` 적용
- 이벤트 구조체는 `struct`로 선언 (GC 최소화)
- 게임 범용 확장 모듈은 `Assets/GameFramework/Game/Module/`에 위치
- 이 프로젝트 고유 모듈은 `Assets/Scripts/Module/`에 위치
