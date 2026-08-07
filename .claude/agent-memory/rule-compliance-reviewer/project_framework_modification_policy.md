---
name: project_framework_modification_policy
description: protect_framework.ps1 훅이 주석 처리된 상태에서 GameFramework 파일이 대량 수정된 패턴 — 이 패턴이 반복될 수 있음
metadata:
  type: project
---

훅이 비활성화된 상태(`settings.json`에서 hooks 블록 전체 주석 처리)에서 `Assets/GameFramework/Core/` 하위 파일 12개가 수정되었다. 수정 내용은 `[Module]` 어트리뷰트 추가 및 `AutoRegisterModules()` 인프라 추가로, 프레임워크 코어 변경이었다.

2026-05-24 추가: 두 번째 프레임워크 대규모 수정 발생. `protect_framework.ps1`은 활성 상태이나 `FrameworkOwners`에 `KangWooChan`이 포함되어 훅이 통과됨. 변경 내용: `ModuleAttribute.cs`, `ModuleScanner.cs`, `CoreBootstrap.cs`, Core 모듈 8개, `GameBootstrap.cs`(Game 레이어). `main` 브랜치에서 직접 수행됨.

**Why:** `01_assets.md`에 "Assets/GameFramework/ 는 절대 수정 금지"라는 규칙이 있으나, 이번 변경은 새 기능(자동 모듈 등록 시스템 + Layer 필터) 추가를 위한 의도적 프레임워크 확장이다. FrameworkOwners 계정이므로 훅 차단 없이 진행됨.

**How to apply:** 프레임워크 파일 수정을 검토할 때, 해당 변경이 버그 픽스/기능 확장인지 아니면 게임 레이어에서 해결 가능한 것인지를 구분해서 판단한다. 의도적 프레임워크 확장은 규칙 위반이 아니라 규칙 예외 처리 대상이다.
