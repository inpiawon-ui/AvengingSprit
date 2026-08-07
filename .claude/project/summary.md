---
description: 프로젝트 개요 — 아키텍처
---

# 프로젝트 요약

## 아키텍처 개요 (3-tier)

```
Assets/
├── GameFramework/Core/    ← 엔진/형태/환경 무관 핵심 시스템 (수정 금지)
├── GameFramework/Game/    ← 어떤 게임에도 필요한 범용 기능·도구 (수정 금지)
└── Scripts/               ← 이 특정 게임/프로젝트 고유 코드 (Claude 자유 작업)
    ├── Character/          ← 캐릭터·스킬 데이터 SO 클래스 (수정 최소화)
    ├── User/               ← 유저 데이터 매니저 (수정 최소화)
    └── Module/             ← 게임 모듈 전용 작업 영역
```

- **ServiceLocator + Module 패턴**을 사용하는 GameFramework 위에 게임 레이어만 추가
- 게임 모듈 로직은 전부 `Assets/Scripts/Module/` 하위에서 작성
- 폴더는 `Assets/Scripts/`지만 네임스페이스는 `Game.*` 유지 (프레임워크의 `GameFramework.Game.*`과 구분되는 의도된 명명)

## 빌드 & 개발

- **Play mode**: 신규 프로젝트에서 씬을 생성하고 `Assets/Scenes/CLAUDE.md`에 정의된 씬 이름으로 저장 후 Play 실행
  - 씬 목록: [`.claude/project/constants.md`](constants.md) 2절(씬 구성) 참조 (프로젝트마다 재정의)
- **Tests**: `Window > General > Test Runner` — `com.unity.test-framework` 패키지로 실행
- **CI (GitHub Actions)**: Unity 테스트는 `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` GitHub Secrets 설정 필요
  - 설정 경로: Repository > Settings > Secrets and variables > Actions
  - 라이선스 파일 획득: [game-ci/unity-activate](https://game-ci.com/docs/github/activation) 참고
- **Addressables**: Unity Addressables 창에서 에셋 그룹 설정. 빌드·업로드 자동화: `Assets/GameFramework/Game/Editor/Build/`
- **AddressableModule**은 반드시 **ResourceModule보다 먼저** 등록해야 한다

## 서브 디렉토리 가이드

| 폴더 | 가이드 |
|------|--------|
| `Assets/GameFramework/Core/` | [`Core/CLAUDE.md`](../../Assets/GameFramework/Core/CLAUDE.md) — 라이프사이클, using 규칙, 아키텍처 패턴 |
| `Assets/GameFramework/Game/` | [`Game/CLAUDE.md`](../../Assets/GameFramework/Game/CLAUDE.md) — 재사용 가능한 게임 레이어, API 사용 규칙, 배치 기준 |
| `Assets/Scripts/` | [`Scripts/CLAUDE.md`](../../Assets/Scripts/CLAUDE.md) — 이 게임 전용 코드 개발 규칙 (네임스페이스, 폴더 구조, Bootstrap) |

> 상세 모듈 명세는 [`Assets/Scripts/Module/CLAUDE.md`](../../Assets/Scripts/Module/CLAUDE.md) 참조
