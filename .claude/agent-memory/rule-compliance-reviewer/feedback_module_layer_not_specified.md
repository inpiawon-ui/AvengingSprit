---
name: feedback_module_layer_not_specified
description: 게임 레이어 모듈([Module] 어트리뷰트)에 Layer = ModuleLayer.Game 미명시 반복 패턴
metadata:
  type: feedback
---

`Assets/Game/Module/` 하위 모듈(`ScoreModule`, `LobbyModule`)에서 `[Module]` 어트리뷰트를 작성할 때 `Layer` 프로퍼티를 생략하는 패턴이 확인됐다. `ModuleAttribute.Layer`의 기본값은 `ModuleLayer.Core`이므로 생략하면 게임 모듈이 Core로 잘못 분류된다.

**Why:** `Core/CLAUDE.md`와 `module/coding_rules.md` 어디에도 "게임 모듈은 `Layer = ModuleLayer.Game`을 명시해야 한다"는 규칙이 없다. `Provides`/`DependsOn` 예시만 있고 `Layer` 사용 예시가 전혀 없어 개발자가 누락하기 쉬운 구조다.

**How to apply:** `Assets/Game/Module/` 하위 `[Module]` 어트리뷰트를 검토할 때 `Layer = ModuleLayer.Game` 명시 여부를 반드시 확인한다. 미명시 발견 시 Convention 위반으로 지적한다.
