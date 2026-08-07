---
name: project-namespace-migration
description: 2026-05-25 네임스페이스 일괄 변경 — 폴더 경로 = 네임스페이스 규칙 적용 완료
metadata:
  type: project
---

2026-05-25 네임스페이스 전면 변경 완료 (폴더 경로 = 네임스페이스, 예외 없음).

**변경 내역:**
- `Core/Manager/` 폴더 → `Core/Base/` 로 이름 변경 (GUID 보존: Rename-Item + .meta 동반)
- `namespace GameFramework.Core` (Base/ 파일 4개) → `namespace GameFramework.Core.Base`
- `namespace GameFramework.Core.Xxx` (Module/ 파일 83개) → `namespace GameFramework.Core.Module.Xxx`
- `using GameFramework.Core;` → `using GameFramework.Core.Base;` (프로젝트 전체)
- `using GameFramework.Core.Xxx;` → `using GameFramework.Core.Module.Xxx;` (프로젝트 전체)
- `Core/CLAUDE.md`, `Game/CLAUDE.md` 네임스페이스 테이블 업데이트

**현재 네임스페이스 규칙 (예외 없음):**
```
GameFramework.Core.Base          ← Core/Base/ (CoreBootstrap, CoreModule, ModuleRegistry, ModuleScanner)
GameFramework.Core.Common        ← Core/Common/
GameFramework.Core.Events        ← Core/Events/
GameFramework.Core.Module.Xxx    ← Core/Module/Xxx/
GameFramework.Game               ← Game/Common/ 루트 파일
GameFramework.Game.Common        ← Game/Common/
GameFramework.Game.Module.Xxx    ← Game/Module/Xxx/
GameFramework.Game.Events        ← Game/Events/
```

**Why:** 기존에는 Module/, Manager/ 폴더가 네임스페이스에서 생략되는 예외 규칙이 있어 폴더 구조와 네임스페이스가 일치하지 않아 혼란이 있었다.

**How to apply:** 신규 Core 모듈 작성 시 반드시 `using GameFramework.Core.Base;` (CoreModule, IModule 접근)와 `using GameFramework.Core.Module.EventBus;` (IEventBus 접근) 패턴 사용. `using GameFramework.Core;` 단독 선언은 더 이상 유효하지 않음.
