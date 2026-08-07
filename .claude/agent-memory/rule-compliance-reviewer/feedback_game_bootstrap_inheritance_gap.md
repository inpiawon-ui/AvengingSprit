---
name: feedback_game_bootstrap_inheritance_gap
description: Assets/Game/Module/Boot/GameBootstrap.cs가 신규 GameFramework.Game.GameBootstrap 미상속 — 신규 추상 클래스 추가 시 게임 레이어 Bootstrap 연결 업데이트 누락 패턴
metadata:
  type: feedback
---

`Assets/GameFramework/Game/GameBootstrap.cs`(Core + Game 자동 등록 중간 계층)가 신규 추가되었으나, 게임 프로젝트의 `Assets/Game/Module/Boot/GameBootstrap.cs`는 이전 패턴(`CoreBootstrap` 직접 상속)을 유지하고 있다.

**Why:** `Assets/GameFramework/Game/CLAUDE.md`와 `Assets/Game/CLAUDE.md`의 Bootstrap 예시 코드가 구버전(수동 `RegisterModule()` 패턴 또는 `CoreBootstrap` 직접 상속)이어서, 새 계층 구조로의 이전이 자연스럽게 이루어지지 않았다.

**How to apply:** 프레임워크 레이어에 새 추상 Bootstrap 클래스가 추가될 때, 게임 레이어의 Bootstrap 상속 체인도 함께 업데이트했는지 확인한다. `Assets/Game/CLAUDE.md`의 Bootstrap 등록 예시도 함께 갱신되었는지 체크한다.
