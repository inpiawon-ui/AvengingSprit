# Memory Index

- [Core 모듈 namespace 통일 (2026-05-25)](project_core_namespace_unification.md) — 13개 Core 모듈 namespace를 `GameFramework.Core.<Module>` 패턴으로 일괄 통일 완료. 같이 CoreDefine/Response/FirebaseBackend 제거, RefVar Core 복원, AddressableUpload Game 이동
- [IModule.ModuleId 제거 (2026-05-25)](project_moduleid_removal.md) — 호출자 0건 데드 코드로 판정. IModule + 16개 Core 모듈 + Core/CLAUDE.md 가이드에서 완전 제거. ServiceLocator 타입 기반이라 인스턴스 ID 불필요
