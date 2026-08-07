---
name: feedback_forget_comment_missing
description: .Forget() 호출부에 fire-and-forget 이유 주석 필수 규칙이 UIModule.Dispose()에서 누락됨 — 반복 패턴
metadata:
  type: feedback
---

`UIModule.cs`의 `_manager?.CloseAllAsync().Forget()` 호출에 `// fire-and-forget: [이유]` 주석이 없다. `coding_conventions.md` 섹션 6에서 ".Forget() 사용 시 호출부에 `// fire-and-forget: [이유]` 주석 필수"라고 명시하고 있다.

**Why:** Dispose() 메서드에서 비동기 정리 작업을 fire-and-forget으로 처리할 때 이 규칙이 자주 누락된다. Dispose() 맥락에서는 await가 불가능하므로 .Forget()이 불가피하고, 그 이유가 자명하다고 느껴 주석을 생략하는 경향이 있다.

**How to apply:** Dispose() 내 .Forget() 패턴을 발견하면 주석 누락을 반드시 지적한다. 자명해 보여도 규칙을 따라야 한다.
