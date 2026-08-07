# GameFramework.Game — 범용 게임 레이어 개발 가이드

이 파일은 `Assets/GameFramework/Game/` 하위 작업 시 적용되는 규칙입니다.
루트 `CLAUDE.md`와 `Assets/GameFramework/Core/CLAUDE.md`의 규칙을 먼저 준수합니다.

---

## 레이어 책임 (3-tier 중 중간)

이 레이어는 **어떤 Unity 게임 개발에도 재사용 가능한 범용 기능·도구**를 담는다.
이 프로젝트만의 데이터·규칙·UI는 절대 두지 않는다.

| 레이어 | 책임 |
|---|---|
| `Assets/GameFramework/Core/` | 엔진·형태·환경 무관 핵심 시스템 |
| `Assets/GameFramework/Game/` (이 레이어) | 어떤 게임에도 필요한 범용 기능·도구 |
| `Assets/Scripts/` | 이 특정 게임/프로젝트 고유 코드 |

---

## 목적과 위치 규칙

- **수정 금지**: `Assets/GameFramework/` 전체(Core 및 이 Game/ 폴더 모두)는 `protect_framework.ps1` 훅이 자동 차단합니다. 재사용 가능한 기능 추가가 필요하면 별도 브랜치에서 작업하세요.
- **재사용 가능한 모듈**: 다른 게임 프로젝트에서도 그대로 쓸 수 있는 기능을 `Game/Module/<Name>/`에 추가합니다.
- **범용 에디터 도구**: Addressable 빌드·업로드 자동화 등 어떤 게임에도 쓸 수 있는 에디터 도구는 `Game/Editor/<Domain>/`에 둔다 (예: `Game/Editor/Build/`).
- **프로젝트 전용 코드는 `Assets/Scripts/`에 작성합니다.** → 상세: [`Assets/Scripts/CLAUDE.md`](../../Scripts/CLAUDE.md)
- **Core 서비스 활용**: `CoreModule.Get<T>()`로 Core 모듈 서비스를 가져와 사용합니다.

```
Game/
├── Common/        게임 전용 열거형·공용 정의
├── Editor/        범용 에디터 도구
│   └── Build/     Addressable 빌드·업로드 자동화 (AddressableConfig, AddressableUploadHandler, BuildEnums)
└── Module/        범용 게임 모듈
    └── <Name>/    모듈별 폴더
```

---

## 네임스페이스

**기본 규칙: 폴더 경로 = 네임스페이스 (예외 없음)**

| 폴더 경로 | 네임스페이스 |
|-----------|-------------|
| `Assets/GameFramework/Core/Base/` | `GameFramework.Core.Base` |
| `Assets/GameFramework/Core/Common/` | `GameFramework.Core.Common` |
| `Assets/GameFramework/Core/Events/` | `GameFramework.Core.Events` |
| `Assets/GameFramework/Core/Module/<Module>/` | `GameFramework.Core.Module.<Module>` |
| `Assets/GameFramework/Game/` | `GameFramework.Game` |
| `Assets/GameFramework/Game/Common/` | `GameFramework.Game.Common` |
| `Assets/GameFramework/Game/Module/<Module>/` | `GameFramework.Game.Module.<Module>` |
| `Assets/GameFramework/Game/Events/` | `GameFramework.Game.Events` |

```csharp
// Core Base 계층 (CoreBootstrap, CoreModule 등)
namespace GameFramework.Core.Base { }

// Core 기능 모듈 예시
namespace GameFramework.Core.Module.EventBus { }  // Assets/GameFramework/Core/Module/EventBus/
namespace GameFramework.Core.Module.Scene { }     // Assets/GameFramework/Core/Module/Scene/
namespace GameFramework.Core.Module.Sound { }     // Assets/GameFramework/Core/Module/Sound/

// Game 범용 모듈 예시
namespace GameFramework.Game.Module.Quest { }     // Assets/GameFramework/Game/Module/Quest/
namespace GameFramework.Game.Module.Action { }    // Assets/GameFramework/Game/Module/Action/
```

---

## 모듈 파일 구조

Core와 동일한 패턴을 따릅니다:

| 파일 | 역할 |
|------|------|
| `IXxxManager.cs` | CoreModule에 등록·조회되는 public API 인터페이스 |
| `XxxManager.cs` | 비즈니스 로직 구현체 |
| `XxxModule.cs` | `IModule` 구현 — 매니저 생성, 등록, 이벤트 구독 |

---

## Bootstrap 연결

`CoreBootstrap`을 상속한 `GameBootstrap`에서 Game 모듈을 등록합니다:

```csharp
using GameFramework.Core.Base;                   // CoreBootstrap
using GameFramework.Core.Module.EventBus;        // EventBusModule
using GameFramework.Core.Module.Scene;           // SceneModule
using GameFramework.Game.Module.Quest;           // QuestModule

public class GameBootstrap : CoreBootstrap
{
    protected override void RegisterModules()
    {
        // Core 모듈 먼저
        RegisterModule(new EventBusModule());
        RegisterModule(new LogModule());
        RegisterModule(new SceneModule());

        // Game 전용 모듈
        RegisterModule(new QuestModule());
        RegisterModule(new InventoryModule());
    }
}
```

---

## Core 서비스 활용 패턴

```csharp
public void Register()
{
    // Core 서비스를 Register()에서 가져옴
    var eventBus = CoreModule.Get<IEventBus>();
    var sceneManager = CoreModule.Get<ISceneManager>();

    _manager = new QuestManager(eventBus, sceneManager);
    CoreModule.Register<IQuestManager>(_manager);
    IsInitialized = true;
}
```

---

## 주의사항

- Game 모듈이 다른 Game 모듈에 의존할 경우 `GameBootstrap`의 등록 순서로 제어합니다.
- Core 모듈의 인터페이스(`IXxxManager`)를 직접 수정하지 말고, Game 레이어에서 래핑하거나 확장합니다.
- 라이프사이클 규칙(Register / Initialize / Dispose 역할 분리)은 Core와 동일하게 준수합니다.
  → 상세 규칙: [`Assets/GameFramework/Core/CLAUDE.md`](../Core/CLAUDE.md)

---

## GameFramework API 사용 규칙

금지 패턴·ServiceLocator·이벤트 구독·Register/Initialize 분리·using 주의사항은
`.claude/rules/project/07_framework_rules.md` (alwaysApply) 참조.

이 파일에는 Game 레이어 전용 추가 규칙만 기록한다.

### 오브젝트 풀
- `Unity ObjectPool<T>` 직접 사용 금지 → `IObjectPoolManager.Get<T>()` / `.Return(T)` 사용
- 풀에 등록되는 오브젝트는 `IPoolable` 구현 필수

### UI
- 모든 UI 패널/팝업 클래스는 `UIPanel` 상속 필수
- 열기/닫기: `IUIManager.OpenAsync<T>()` / `CloseAsync<T>()` 사용

### 저장
- 저장이 필요한 클래스는 `ISaveable` 구현
- `Awake`에서 `SaveManager.Instance.Register(this)` 호출

### ScriptableObject
- 모든 SO 클래스에 `[CreateAssetMenu]` 어트리뷰트 필수

### 데이터 보안
- `DataModule` 암호화키는 **절대 하드코딩 금지**
- 반드시 환경변수 또는 런타임 설정으로 주입: `new DataModule(encryptionPassword: env["KEY"])`
- `.env` / `secrets.json` 파일은 git 커밋 금지

---

## `GameFramework/Game/` vs `Assets/Scripts/` 배치 기준

> **핵심 질문: 다른 게임 프로젝트에서 그대로 재사용 가능한가?**
> - YES → `Assets/GameFramework/Game/Module/<Name>/` (또는 `Game/Editor/<Domain>/`)
> - NO  → `Assets/Scripts/`

| 코드 예시 | 위치 |
|-----------|------|
| 퀘스트 시스템 (범용 설계) | `GameFramework/Game/` |
| 이 게임의 퀘스트 데이터·조건 | `Assets/Scripts/` |
| 인벤토리 모듈 | `GameFramework/Game/` |
| 이 게임의 아이템 정의 | `Assets/Scripts/` |
| 캐릭터 베이스 클래스 | `GameFramework/Game/` |
| 특정 캐릭터 "minji" 구현 | `Assets/Scripts/` |
| 점수 시스템 (범용) | `GameFramework/Game/` |
| 점수 규칙 (이 게임만의 룰) | `Assets/Scripts/` |
| Addressable 빌드·업로드 자동화 | `GameFramework/Game/Editor/Build/` |
| 이 게임의 빌드 후처리 (예: 특정 SO 자동 채움) | `Assets/Scripts/Editor/` |

게임 전용 코드 개발 규칙: [`Assets/Scripts/CLAUDE.md`](../../Scripts/CLAUDE.md)
