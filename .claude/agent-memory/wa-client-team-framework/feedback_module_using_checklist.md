---
name: feedback-module-using-checklist
description: Module 파일 신규 작성 또는 이동 시 using GameFramework.Core.Base 누락 패턴
metadata:
  type: feedback
---

Module 파일에 `using GameFramework.Core.Base;`가 없으면 `CoreModule`, `[Module]`, `ModuleLayer` 심볼을 찾지 못해 CS0103/CS0246 컴파일 오류가 발생한다.

**Why:** `CoreModule.Get<T>()`, `CoreModule.Register<T>()`, `CoreModule.Unregister<T>()`, `[Module(...)]` 어트리뷰트, `ModuleLayer` 열거형은 모두 `GameFramework.Core.Base` 네임스페이스에 있다. `AppLifecycleModule.cs`가 이 using 없이 제출되어 오류 발생 사례가 실제로 있었다.

**How to apply:** 신규 `XxxModule.cs` 파일 작성 시 using 3종 세트를 먼저 선언한다.
```csharp
using System;                        // IDisposable
using GameFramework.Core.Base;       // CoreModule, [Module], ModuleLayer
using GameFramework.Core.Common;     // IModule, ITickable, IPausable
```
`[Module]` 어트리뷰트를 포함하는 파일은 반드시 `using GameFramework.Core.Base;` 유무를 grep으로 확인한다.
