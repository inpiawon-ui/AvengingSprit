---
name: project-core-namespace-unification
description: 2026-05-25 GameFramework.Core 모듈 13개 namespace를 GameFramework.Core.<Module>로 일괄 통일 완료
metadata:
  type: project
---

2026-05-25 기준, `Assets/GameFramework/Core/Module/` 하위 13개 모듈 namespace를 전부 `GameFramework.Core.<Module>`로 통일하는 대규모 리팩터링을 완료했다.

**Why:** 직전 작업에서 Log만 `GameFramework.Log` → `GameFramework.Core.Log`로 옮겨졌고 나머지 13개는 옛 `GameFramework.<Module>`로 남아 있어 명명 일관성이 깨진 상태였다. 사용자는 `Assets/` 폴더 경로 = 네임스페이스 원칙을 강하게 선호하며, "기존 예외" 표기 자체를 제거하길 원했다.

**How to apply:**
- 같은 사용자가 Core 외 영역에 새 모듈을 추가할 때 — 무조건 `GameFramework.Core.<Module>` 패턴(Core 하위) 또는 `GameFramework.Game.<Module>` 패턴(Game 하위)을 강제한다. 옛 짧은 형식(`GameFramework.<Module>`)을 다시 도입하면 안 된다.
- Core/CLAUDE.md의 네임스페이스 테이블에 "기존 예외" 항목이 부활하지 않도록 PR 리뷰 시 확인한다.
- 같은 종류의 일괄 치환을 다시 수행할 때 PowerShell `-Encoding utf8` no-BOM 강제는 필수 (관련 메모: [[feedback-framework-folder-namespace]] 와 `.claude/rules/coding_summary_rule.md`)
- 같이 진행된 부수 정리:
  - `Core/Common/CoreDefine.cs` 삭제 + Game/Editor/Build/BuildEnums.cs로 이관
  - `Core/Common/Response.cs` 삭제 (미사용)
  - `Core/Module/Data/Backends/FirebaseBackend.cs` 삭제 (미사용 스텁)
  - `Core/Editor/` 폴더 자체 제거 (AddressableUploadHandler가 게임/배포 인프라 종속이라 Game으로 이동)
  - `Core/Common/RefVar.cs` Game/Module/DataType/에서 복원 (INotify<T>와 같은 어셈블리 유지)
  - `InputManager`에 `ITickable` 의도 변경 — InputModule이 ITickable을 유지하고 시그니처만 `Tick(float deltaTime)`로 통일

**관련 결정:**
- Q3에서 AddressableUploadHandler/AddressableConfig를 `Assets/Game/Editor/Build/`로 이동(`Game.Editor.Build` namespace). 향후 다른 게임에서 재사용 시 GameFramework/Game/Module/AddressableBuild/로 다시 옮기는 옵션은 살아 있음.
- Editor 전용 코드 폴더 규칙: `Assets/Game/Editor/` 하위는 자동으로 Editor-only 컴파일됨 (asmdef 불요).
