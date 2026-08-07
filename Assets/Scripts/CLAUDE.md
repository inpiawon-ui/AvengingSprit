# Assets/Scripts — 이 게임 전용 코드 개발 가이드

이 파일은 `Assets/Scripts/` 하위 작업 시 적용되는 규칙입니다.

> **GameFramework API 사용 규칙 및 GameFramework/Game vs Assets/Scripts 배치 기준은 반드시 참조:**
> [`Assets/GameFramework/Game/CLAUDE.md`](../GameFramework/Game/CLAUDE.md)

---

## 3-tier 레이어 책임

| 레이어 | 책임 |
|---|---|
| `Assets/GameFramework/Core/` | 엔진·프로젝트 형태(앱·게임)·환경(모바일·PC)에 **무관한 핵심 시스템** |
| `Assets/GameFramework/Game/` | **어떤 게임 개발에도 필요한 범용 기능·도구** |
| `Assets/Scripts/` (이 폴더) | **이 특정 게임/프로젝트 고유 코드** |

---

## 목적

`Assets/Scripts/`는 **이 게임 전용** 코드 공간입니다.
다른 프로젝트에서 재사용할 수 없는 로직, 데이터, UI가 여기에 위치합니다.

범용 재사용 가능 여부로 배치를 결정합니다:
- 재사용 가능 → `Assets/GameFramework/Game/Module/<Name>/`
- 이 게임 전용 → `Assets/Scripts/`

---

## 네임스페이스 명명 규칙

**폴더는 `Assets/Scripts/`지만, 네임스페이스는 의도적으로 `Game.*`을 유지합니다.**
이유: 프레임워크의 `GameFramework.Game.*`(재사용 가능한 범용 게임 레이어)와 명확히 구분하기 위함.

| 의미 | 네임스페이스 | 위치 |
|---|---|---|
| 이 프로젝트 전용 | `Game.*` | `Assets/Scripts/` |
| 어떤 게임에도 재사용 | `GameFramework.Game.*` | `Assets/GameFramework/Game/` |
| 엔진·환경 무관 핵심 | `GameFramework.Core.*` | `Assets/GameFramework/Core/` |

| 폴더 | 네임스페이스 |
|------|-------------|
| `Assets/Scripts/Module/` (Events 등 최상위) | `Game.Module` |
| `Assets/Scripts/Module/Boot/` | `Game.Module.Boot` |
| `Assets/Scripts/Module/Common/` | `Game.Module.Common` |
| `Assets/Scripts/Module/Common/UI/` | `Game.Module.Common.UI` |
| `Assets/Scripts/Module/Common/Dto/` | `Game.Module.Common.Dto` |
| `Assets/Scripts/Module/InGame/` (및 하위 전체) | `Game.Module.InGame` |
| `Assets/Scripts/Module/Lobby/` (및 하위 전체) | `Game.Module.Lobby` |
| `Assets/Scripts/Module/Result/` (및 하위 전체) | `Game.Module.Result` |
| `Assets/Scripts/Module/Title/` (및 하위 전체) | `Game.Module.Title` |
| `Assets/Scripts/Character/` | `Game.Character` |
| `Assets/Scripts/User/` | `Game.User` |

> ※ Boot/InGame/Lobby/Result/Title 은 씬 구조 기준. 신규 프로젝트에서 씬 이름에 맞게 추가/수정한다.
>
> ※ **씬 이름과 모듈 폴더명은 다를 수 있다.** 예: 씬 파일명은 `GameScene.unity`이지만 모듈 폴더는 `InGame/`으로 관리한다.
> 씬 이름은 `Assets/Scenes/CLAUDE.md`에서, 모듈 폴더명은 이 파일의 네임스페이스 테이블에서 각각 관리한다.
>
> ※ 빌드·업로드 자동화 도구(AddressableConfig, AddressableUploadHandler 등)는 범용 도구이므로 `Assets/GameFramework/Game/Editor/Build/`에 위치한다.

---

## 폴더 구조

```
Assets/Scripts/
├── Character/          ← 캐릭터·스킬 데이터 SO 클래스 (수정 최소화)
├── User/               ← 유저 데이터 매니저 (수정 최소화)
└── Module/             ← 게임 모듈 전용 작업 영역
    ├── Boot/           ← 부트씬 로직
    ├── Common/         ← 씬 공통 유틸리티 (SafeArea, SystemPopup 등)
    │   ├── Dto/        ← API DTO (서버·클라이언트 공유 데이터 계약)
    │   └── UI/         ← 재사용 UI 컴포넌트 (RecyclingScrollView 등)
    ├── Events/         ← IEvent struct 정의
    ├── InGame/         ← 게임플레이 로직 (하위 폴더는 게임 장르에 맞게 추가)
    │   └── UI/
    ├── Lobby/          ← 로비씬 로직
    │   └── UI/
    ├── Result/         ← 결과씬 로직
    │   └── UI/
    └── Title/          ← 타이틀씬 로직
        └── UI/
```

> ※ InGame 하위 기능 폴더는 게임 장르에 따라 신규 프로젝트 시작 시 추가한다.

---

## Bootstrap 등록

`Assets/Scripts/Module/` 모듈은 `GameLauncher`에서 등록합니다.

### 자동 등록 모듈 ([Module] 어트리뷰트 보유, 파라미터 없는 생성자)

다음 Core 모듈은 `AutoRegisterModules`로 자동 수집·등록됩니다 (DependsOn 위상 정렬 적용):

`EventBusModule`, `LogModule`, `AddressableDownloadModule`, `ResourceModule`, `SceneModule`,
`TimerModule`, `UIModule`, `ObjectPoolModule`, `NetworkModule`

> **NetworkModule**: 자동 등록 대상. `INetworkSettings`(`NetworkConfig` ScriptableObject가 구현)를
> `GameLauncher.RegisterConfigs()`에서 `CoreConfig.Set<INetworkSettings>(_networkConfig)`로 등록하면
> NetworkModule이 `Register()` 단계에서 읽어 Local/Remote 클라이언트를 자동 선택한다.
> NetworkConfig 에셋을 GameLauncher 인스펙터 `_networkConfig` 필드에 연결만 하면 끝.
> 설정이 등록되지 않은 경우 기본값(real client, 빈 BaseUrl)으로 동작한다.

### 옵션 모듈 (수동 등록 — 생성자 파라미터 필요로 [Module] 어트리뷰트 없음)

다음 6개 모듈은 자동 등록 대상이 아니므로 사용 시 `RegisterModule(new XxxModule(...))`로 수동 등록합니다:

| 모듈 | 생성자 | 등록 순서 제약 |
|------|--------|---------------|
| `LoadingModule(ILoadingView view = null)` | 커스텀 뷰 또는 기본 FullLoadingView | EventBus 이후 (Initialize에서 사용) |
| `SoundModule(ISoundBackend backend = null, int sfxPoolInitial, int sfxPoolMax)` | 백엔드/풀 크기 | ObjectPool, EventBus, Resource 이후 |
| `AnalyticsModule(IAnalyticsBackend backend = null)` | NoOp 또는 커스텀 백엔드 | EventBus 이후 권장 |
| `DataModule(string encryptionPassword = "", IDataBackend backend = null)` | 암호화 키/백엔드 | EventBus 이후 |
| `LocalizationModule(string defaultLocale = "ko")` | 기본 로케일 | EventBus 이후 (Register에서 IEventBus 사용) |
| `InputModule(IInputManager manager = null)` | 커스텀 입력 구현체 | 순서 자유 |

> **수동 등록 모듈은 반드시 `AutoRegisterModules` 호출 이전에 `RegisterModule(new XxxModule(...))`로 등록한다.**
> 수동 등록된 타입은 ModuleScanner의 `alreadySatisfied`로 처리되어 자동 스캔에서 중복 제외된다.

### 예시

```csharp
public sealed class GameLauncher : GameFramework.Game.GameBootstrap
{
    [SerializeField] private NetworkConfig _networkConfig;

    public override void RegisterConfigs()
    {
        // NetworkConfig SO가 INetworkSettings를 구현. CoreConfig에 등록하면
        // NetworkModule이 Register() 단계에서 읽어 Local/Remote 분기를 처리한다.
        if (_networkConfig != null)
            CoreConfig.Set<INetworkSettings>(_networkConfig);
    }

    public override void RegisterModules()
    {
        // 1. 옵션 모듈 (필요한 것만 골라서 — Auto 호출 전에 등록)
        RegisterModule(new LoadingModule());                                     // 씬 전환 로딩 UI
        RegisterModule(new SoundModule(sfxPoolInitial: 8, sfxPoolMax: 32));      // 사운드 (ObjectPool 자동 등록 후에 등록되므로 SoundModule이 더 뒤에 와야 함 — 자동 등록 직후 사용 시 주의)
        RegisterModule(new DataModule(encryptionPassword: "your-key"));          // 세이브 데이터
        RegisterModule(new LocalizationModule(defaultLocale: "ko"));             // 현지화
        // RegisterModule(new AnalyticsModule(new FirebaseAnalyticsBackend()));  // 애널리틱스 (필요 시)
        // RegisterModule(new InputModule());                                    // 입력 (Default InputManager 사용 시)

        // 2. Core + GameFramework.Game + 게임 전용 모듈을 자동 등록
        //    NetworkModule은 [Module(Layer = ModuleLayer.Core)] 어트리뷰트로 이 단계에서 자동 등록됨
        base.RegisterModules(); // Core + GameFramework.Game 자동
        AutoRegisterModules(GetType().Assembly, ModuleLayer.Game);
    }
}
```

> ⚠️ **순서 주의 — SoundModule**: `Register()`에서 `CoreModule.Get<IObjectPoolManager>()`를 즉시 호출한다.
> ObjectPoolModule이 자동 등록되므로 SoundModule을 수동 등록하면 ObjectPool보다 먼저 등록되어 예외가 발생한다.
> SoundModule을 사용하려면 ObjectPoolModule을 함께 수동 등록하거나, `base.RegisterModules()` 이후에 SoundModule을 등록하는 별도 패턴이 필요하다.

---

## CoreModule.Get<T>() 사용 패턴

```csharp
// 모듈 Register() 단계에서 의존성 주입
public void Register()
{
    var eventBus = CoreModule.Get<IEventBus>();
    var sceneManager = CoreModule.Get<ISceneManager>();

    _manager = new MyManager(eventBus, sceneManager);
    CoreModule.Register<IMyManager>(_manager);
    IsInitialized = true;
}
```

- `CoreModule.Get<T>()`는 등록된 서비스만 반환합니다. 미등록 시 `InvalidOperationException`.
- 안전하게 조회하려면 `CoreModule.TryGet<T>(out var svc)` 사용.

---

## 이 게임의 모듈 목록

> [`Assets/Scripts/Module/CLAUDE.md`](Module/CLAUDE.md) — 게임 장르 확정 후 모듈별 역할·의존·멤버를 여기에 기록한다.
