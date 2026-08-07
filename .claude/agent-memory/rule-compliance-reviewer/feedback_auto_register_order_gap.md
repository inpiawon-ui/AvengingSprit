---
name: feedback_auto_register_order_gap
description: [Module] 어트리뷰트 도입 후 EventBus 우선 등록 규칙이 AutoRegisterModules에서 어떻게 보장되는지 문서에 설명이 없음
metadata:
  type: feedback
---

`AutoRegisterModules()`가 도입되면서 기존 수동 등록 규칙("EventBusModule 반드시 첫 번째")이 자동 위상 정렬로 대체되었다. `07_framework_rules.md`와 `Assets/GameFramework/Core/CLAUDE.md`의 모듈 등록 순서 제약 섹션은 아직 수동 등록 기준으로 작성되어 있다.

**Why:** [Module] 어트리뷰트의 `DependsOn`이 EventBus 의존성을 선언하면 자동으로 EventBus가 먼저 등록되므로, 수동 순서 제약이 자동화되었다. 그러나 이 사실이 문서에 없어서 개발자가 혼란을 겪을 수 있다.

**How to apply:** 새 모듈이 EventBus에 의존할 때 `DependsOn = new[] { typeof(IEventBus) }`를 선언하도록 유도하는 문서 개선이 필요하다. 수동 등록 시 순서 규칙과 자동 등록 시 DependsOn 선언 방식을 구분해서 문서화해야 한다.
