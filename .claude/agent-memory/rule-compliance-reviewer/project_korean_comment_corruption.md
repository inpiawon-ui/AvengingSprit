---
name: project_korean_comment_corruption
description: ScoreModule.cs의 한글 주석 깨짐이 이번 수정 이전부터 존재했던 기존 문제 — 이번 커밋에서 신규 발생한 것이 아님
metadata:
  type: project
---

`Assets/Game/Module/InGame/Score/ScoreModule.cs`의 Register() 및 Tick() 내 한글 주석이 `?` 문자로 깨진 상태로 git에 이미 커밋되어 있었다. git diff 확인 결과 이번 수정(Module 어트리뷰트 추가)은 깨진 주석 내용을 변경하지 않았다.

**Why:** `coding_summary_rule.md`에 기록된 PowerShell Set-Content 인코딩 실수(`-Encoding utf8` 누락)가 이전 세션에서 발생한 것으로 보인다.

**How to apply:** ScoreModule.cs 주석 깨짐을 이번 변경의 신규 위반으로 보고하지 않는다. 단, 기존 기술 부채로 별도 수정이 필요하다는 점을 지적한다.
