---
name: project-moduleid-removal
description: IModule.ModuleId를 호출자 0건 데드 코드로 판정해 완전 제거함 (2026-05-25). ServiceLocator 타입 기반이라 인스턴스 ID 식별자가 구조적으로 불필요하다는 판단 근거 기록.
metadata:
  type: project
---

# IModule.ModuleId 제거 결정 (2026-05-25)

`IModule` 인터페이스의 `string ModuleId { get; }` 멤버와 16개 Core 모듈의 구현 (`=> "Xxx"`)을 완전 제거함.

**Why:**
- 프로젝트 전체 grep 결과 ModuleId를 **읽는** 코드(`.ModuleId` 접근)가 단 한 곳도 없었다.
  - ModuleRegistry, ModuleScanner, CoreBootstrap, 로그/디버그 어디에서도 사용 안 함.
- ServiceLocator(`CoreModule.Get<T>()`)는 **타입 기반** 조회 — 인스턴스 식별자가 구조적으로 불필요.
- 디버깅용이라면 `nameof(XxxModule)` 또는 `GetType().Name`이 동일 정보를 비용 없이 제공.
- Guid 등 대체 타입으로 바꿔도 호출자가 없으면 무의미.

**How to apply:**
- 향후 모듈에 "고유 ID" 또는 "식별자" 추가 요청이 들어오면, 먼저 **실제 호출자가 존재할지** 검증할 것.
  실제 사용 시나리오(런타임 동적 조회·다중 인스턴스 식별·로깅 키 등)가 명확하지 않으면 추가하지 말 것.
- 모듈은 보통 싱글톤·타입 유일이므로 ServiceLocator 타입 키만으로 충분하다는 점을 근거로 제시.
- 만약 미래에 동일 타입의 모듈을 여러 개 등록하는 요구가 나오면, 그때는 ServiceLocator 자체를
  키 기반(`CoreModule.Get<T>(string key)`)으로 확장하는 편이 IModule에 ID를 다는 것보다 깔끔.
