---
name: project_teststart_placement
description: 임시 테스트 스크립트(TestStart.cs)가 Assets/Game/ 루트에 배치되는 반복 패턴 — 모듈 coding_rules.md 위반
metadata:
  type: project
---

`Assets/Game/TestStart.cs`가 untracked 파일로 존재. `module/coding_rules.md`는 "새 스크립트는 반드시 `Assets/Game/Module/` 하위에 생성"을 요구한다. 테스트 스크립트는 `Assets/Game/Module/` 밖, 루트에 배치되었다.

**Why:** 빠른 테스트 목적으로 루트에 배치한 것으로 추정. 테스트 코드에 대한 예외 규칙이 문서에 없어서 임시 스크립트가 규칙 위반 위치에 생성된다.

**How to apply:** TestStart류 임시 스크립트를 발견하면 위치 위반으로 플래그. 추후 게임 스크립트 위치 규칙에 테스트/임시 스크립트 처리 방침을 추가할 필요가 있다.
