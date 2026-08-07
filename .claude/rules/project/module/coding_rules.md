---
description: 게임 모듈 전용 규칙 — 스크립트는 Assets/Scripts/Module/ 하위, 네임스페이스는 Game.Module.<씬명>
alwaysApply: true
---

# 게임 모듈 코딩 규칙

게임 모듈 레이어(`Assets/Scripts/Module/`)에만 적용되는 규칙이다.
프레임워크 전체 규칙은 `07_framework_rules.md` 참조.
폴더 구조·전체 네임스페이스 테이블·Bootstrap 등록 패턴은 [`Assets/Scripts/CLAUDE.md`](../../../../Assets/Scripts/CLAUDE.md) 참조.

## 스크립트 위치

- 새 스크립트는 **반드시 `Assets/Scripts/Module/` 하위에 생성**
- `Assets/Scripts/Character/`, `Assets/Scripts/User/` 기존 코드 수정은 최소화

## 네임스페이스 패턴

폴더는 `Assets/Scripts/`로 바뀌었지만 네임스페이스는 의도적으로 `Game.*`을 유지한다.
프레임워크 레이어 `GameFramework.Game.*`과 명확히 구분된다.

```
Game.Module.<씬명>   예: Game.Module.InGame / Game.Module.Lobby
Game.Module.Common
Game.Character
Game.User
```

> 전체 폴더↔네임스페이스 매핑: [`Assets/Scripts/CLAUDE.md`](../../../../Assets/Scripts/CLAUDE.md)
