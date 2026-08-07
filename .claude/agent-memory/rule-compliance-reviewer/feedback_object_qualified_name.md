---
name: feedback_object_qualified_name
description: using System; + using UnityEngine; 공존 시 Destroy/DontDestroyOnLoad 완전 한정 이름 미사용 반복 패턴
metadata:
  type: feedback
---

`CoreBootstrap.cs`에 `using System;`과 `using UnityEngine;`이 동시 선언된 상태에서
`Destroy(gameObject)`, `DontDestroyOnLoad(gameObject)`를 완전 한정 이름 없이 호출했다.

규칙 원문 (`07_framework_rules.md`, `Core/CLAUDE.md`):
> `using System;` + `using UnityEngine;` 동시 선언 시 `Object` 모호성 발생
> → `UnityEngine.Object.Destroy()`, `UnityEngine.Object.DontDestroyOnLoad()` 형태로 완전 한정 이름 사용

**Why:** MonoBehaviour 서브클래스에서 `Destroy(go)`가 컴파일 통과하는 경우가 있어 개발자가 "컴파일이 되면 괜찮다"고 착각한다. 규칙 문서에 "컴파일 성공 무관 강제 적용"이 명시되어 있지 않아 선택적으로 적용되는 문제가 반복된다.

**How to apply:** `using System;`이 선언된 MonoBehaviour 파일에서 `Destroy`, `DontDestroyOnLoad`, `Instantiate` 등 `UnityEngine.Object` 정적 메서드 호출을 발견하면 완전 한정 이름 사용 여부를 반드시 확인한다. 컴파일 통과 여부와 무관하게 Critical 위반으로 지적한다.
