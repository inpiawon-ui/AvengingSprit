---
description: GameFramework 핵심 금지·필수 패턴 요약 — 상세 규칙은 GameFramework 폴더 내 CLAUDE.md 참조
alwaysApply: true
---

# GameFramework 사용 규칙

## 금지 패턴 (절대 사용 금지)

| 금지 | 이유 | 대신 사용 |
|------|------|-----------|
| `C# event`, `Action`, `Func` 직접 선언 | 구독 추적 불가, 메모리 누수 | `IEventBus.Publish()` / `Subscribe<T>()` |
| `SceneManager.LoadScene()` 직접 호출 | 씬 라이프사이클 우회 | `ISceneManager.LoadAsync(SceneLoadRequest)` |
| `Unity ObjectPool<T>` 직접 사용 | 풀 관리 누락 | `IObjectPoolManager.Get<T>()` / `.Return(T)` |
| 모듈 인스턴스 직접 참조 | ServiceLocator 패턴 파괴 | `CoreModule.Get<IXxx>()` |
| `System.Threading.Tasks.Task`, `Coroutine` | UniTask 프로젝트 표준 위반 | `UniTask` |

## 필수 패턴

### ServiceLocator
```csharp
// 필수 의존성 (미등록 시 예외)
CoreModule.Get<IEventBus>();

// 선택적 의존성
if (CoreModule.TryGet<IFoo>(out var foo)) { ... }
```

### 이벤트 구독 — 토큰 필드 저장 필수
```csharp
// ❌ 잘못된 패턴 — 구독 취소 불가
CoreModule.Get<IEventBus>().Subscribe<SomeEvent>(_ => ...);

// ✅ 올바른 패턴
private IDisposable _token;
private void Awake() => _token = CoreModule.Get<IEventBus>().Subscribe<SomeEvent>(OnSomeEvent);
private void OnDestroy() => _token?.Dispose();
```

### Register / Initialize 역할 분리 (IModule 구현 시)
```csharp
public void Register()   { /* 인스턴스 생성 + CoreModule.Register — 이벤트 구독 금지 */ }
public void Initialize() { /* 이벤트 구독 — 이 시점에 모든 모듈 Register 완료 */ }
public void Dispose()    { /* 토큰 Dispose → 리소스 해제 → CoreModule.Unregister */ }
```

### 모듈 등록 순서 주의사항
- `AddressableModule`은 반드시 **`ResourceModule`보다 먼저** 등록해야 한다.
- 순서가 잘못되면 리소스 로드 시 런타임 오류 발생.

### using 지시문 주의사항
- `IDisposable` 사용 시 `using System;` 필수 (Unity 암시적 전역 using에 미포함)
- `using System;` + `using UnityEngine;` 동시 선언 시 `Object` 모호성 발생
  → `UnityEngine.Object.Destroy()`, `UnityEngine.Object.DontDestroyOnLoad()` 형태로 완전 한정 이름 사용

## 상세 참조

- [`Assets/GameFramework/Core/CLAUDE.md`](../../../Assets/GameFramework/Core/CLAUDE.md) — 모듈 라이프사이클, using 규칙, 아키텍처 패턴
- [`Assets/GameFramework/Game/CLAUDE.md`](../../../Assets/GameFramework/Game/CLAUDE.md) — API 사용 규칙 전체, `GameFramework/Game/` vs `Assets/Scripts/` 배치 기준
- [`Assets/Scripts/CLAUDE.md`](../../../Assets/Scripts/CLAUDE.md) — 이 게임 전용 코드 개발 규칙 (네임스페이스, 폴더 구조, Bootstrap 등록)
