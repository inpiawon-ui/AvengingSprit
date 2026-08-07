# Network 모듈 개발 가이드

`Assets/GameFramework/Core/Module/Network/` 하위 작업 시 참조한다.

---

## 구성 파일

| 파일 | 역할 |
|------|------|
| `INetworkClient.cs` | CoreModule에 등록되는 public API 인터페이스 |
| `NetworkClient.cs` | UnityWebRequest 기반 실서버 HTTP 구현체 |
| `NetworkResponse<T>.cs` | 요청 결과 래퍼 (성공/실패 팩토리 메서드 제공) |
| `NetworkModule.cs` | `INetworkClient`를 CoreModule에 등록하는 모듈 |
| `INetworkSettings.cs` | CoreConfig에 등록하는 설정 인터페이스 |
| `Local/` | 로컬 파일 기반 개발용 구현체 |

---

## INetworkSettings — CoreConfig 패턴

### 흐름

```
① CoreBootstrap.RegisterConfigs()
     GameLauncher.RegisterConfigs() override:
       CoreConfig.Set<INetworkSettings>(_networkConfig)   ← NetworkConfig SO가 구현체

② CoreBootstrap.RegisterModules()
     NetworkModule.Register() 내부:
       CoreConfig.TryGet<INetworkSettings>(out var s)
       → s.UseLocal == true  → LocalNetworkClient
       → s.UseLocal == false → NetworkClient(s.BaseUrl)
       → s == null(미등록)   → NetworkClient("") 기본값
```

### GameLauncher 설정 방법

```csharp
// Assets/Scripts/Module/Boot/GameLauncher.cs
public override void RegisterConfigs()
{
    if (_networkConfig != null)
        CoreConfig.Set<INetworkSettings>(_networkConfig);
    // SoundConfig, LocalizeConfig 등 향후 여기에 추가
}
```

### NetworkConfig ScriptableObject 생성

1. Unity Editor → Project 우클릭 → `Create > Game > NetworkConfig`
2. 생성된 에셋을 `Assets/BaseResource/Config/`에 저장
3. GameLauncher Inspector의 `_networkConfig` 필드에 연결

---

## NetworkModule

`[Module(Layer = ModuleLayer.Core)]` 어트리뷰트로 자동 등록된다.
생성자 파라미터 없음 — `CoreConfig`에서 설정을 읽으므로 수동 등록 불필요.

```csharp
// ✅ 자동 등록 — 별도 RegisterModule 호출 불필요
// base.RegisterModules()의 AutoRegisterModules 스캔에서 포함됨
```

---

## NetworkClient — 리소스 해제 규칙

`UnityWebRequest`는 IDisposable이며 네이티브 메모리를 사용한다.
`SendAsync` 내부에서 try/finally로 항상 `request.Dispose()`를 호출한다.

```csharp
// ✅ 현재 구현 패턴
private async UniTask<NetworkResponse<T>> SendAsync<T>(UnityWebRequest request)
{
    try   { /* 요청 처리 */ }
    finally { request.Dispose(); }  // ← 성공/실패/예외 모든 경로에서 보장
}
```

---

## Local/ 폴더

로컬 파일 기반 개발·테스트용 구현체. **프로덕션 빌드에서 사용 금지.**

| 클래스 | 역할 |
|--------|------|
| `LocalNetworkClient` (internal) | HTTP 메서드를 파일 CRUD로 매핑. `NetworkModule`이 `INetworkSettings.UseLocal == true`일 때 내부적으로 생성 |
| `JsonFileApiStore` (internal) | `persistentDataPath/local_api/{path}.json` 저장소 |

> **이전 `LocalNetworkModule` 클래스는 삭제됨.** `NetworkModule` 자동 등록과의 이중 등록 사고를 예방하기 위함. Local 모드 사용은 `NetworkConfig.UseLocal = true`로만 활성화한다.

LocalNetworkClient의 HTTP↔파일 매핑:

| Method | 동작 |
|--------|------|
| GET    | 파일 읽기 → 200 / 없으면 404 |
| POST   | 파일 쓰기(신규) → 201 |
| PUT    | 파일 쓰기(덮어쓰기) → 200 |
| DELETE | 파일 삭제 → 204 / 없으면 404 |

---

## 향후 확장 — CoreConfig 패턴 적용 대상

동일 패턴으로 확장 예정:

| 모듈 | 인터페이스 | Config SO |
|------|-----------|-----------|
| SoundModule | `ISoundSettings` | `SoundConfig` |
| LocalizationModule | `ILocalizationSettings` | `LocalizeConfig` |
| DataModule | `IDataSettings` | `DataConfig` |
