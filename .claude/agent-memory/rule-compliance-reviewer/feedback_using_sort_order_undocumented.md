---
name: feedback_using_sort_order_undocumented
description: using 정렬 순서 규칙이 어떤 문서에도 명시되어 있지 않은 문서 공백
metadata:
  type: feedback
---

`ModuleRegistry.cs`에서 `System.Collections.Generic`이 `GameFramework.Core.Common` 뒤에 선언되어 `System.*` 계열이 연속 배치되지 않았다.
`coding_conventions.md`, `general_rule.md`, `Core/CLAUDE.md` 어디에도 using 정렬 순서 규칙이 없다.

**Why:** 기존 파일들이 `System.*` → 외부 순서를 묵시적으로 따르고 있으나, 신규 파일 작성 시 이를 보장할 규칙이 없어 불일치가 발생한다.

**How to apply:** using 정렬 불일치를 Convention 위반으로 지적할 때, 이것이 문서 공백에 기인함을 함께 명시하고 `coding_conventions.md` §9 코드 포맷에 using 정렬 순서 추가를 제안한다.
