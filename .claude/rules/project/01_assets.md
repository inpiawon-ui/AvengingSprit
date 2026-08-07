---
description: 에셋 폴더 원칙 — BaseResource(원본/직접참조) vs BundleResource(Addressable), 테이블 SO 배열 관리, 스크립트 폴더 구조
alwaysApply: true
---

# 폴더 구조

## 리소스 폴더 원칙

| 폴더 | 역할 | 번들 여부 |
|------|------|-----------|
| `Assets/BaseResource/` | 원본 리소스 보존용. 빌드에 포함되며 직접 참조. Addressable 번들 대상 아님 | ✗ |
| `Assets/BundleResource/` | Addressable 번들 대상 리소스 | ✓ |

- 에디터 툴로 프리팹 생성 시 placeholder 텍스처(PNG)는 `Assets/BaseResource/{프리팹명}/`에 저장한다.
- 런타임에 로드되는 Atlas, 프리팹, 사운드 등은 반드시 `Assets/BundleResource/` 하위에 둔다.
- 프리팹은 기능과 씬에 따라 `Assets/BundleResource/Prefabs/` 하위 폴더로 나눈다.

> 폴더별 상세 규칙: [`Assets/BundleResource/CLAUDE.md`](../../../Assets/BundleResource/CLAUDE.md) / [`Assets/BaseResource/CLAUDE.md`](../../../Assets/BaseResource/CLAUDE.md)

## 테이블 데이터 규칙

- 캐릭터/스킬 데이터는 **Table SO 하나에 배열로 관리**한다.
- 개별 `char_001.asset`, `skill_001.asset` 방식 사용 금지.

## 스크립트 폴더 구조 (3-tier)

| 폴더 | 책임 |
|------|------|
| `Assets/GameFramework/Core/` | 엔진/프로젝트 형태(앱·게임)/환경(모바일·PC)에 무관한 핵심 시스템 |
| `Assets/GameFramework/Game/` | 어떤 게임 개발에도 필요한 범용 기능·도구 |
| `Assets/Scripts/` | **이 특정 게임/프로젝트 고유 코드** (Claude 자유 작업 영역) |

> 상세 구조·네임스페이스·Bootstrap 등록은 [`Assets/Scripts/CLAUDE.md`](../../../Assets/Scripts/CLAUDE.md) 참조.

```
Assets/Scripts/
├── Character/    ← 캐릭터·스킬 데이터 SO 클래스 (수정 최소화)
├── User/         ← 유저 데이터 매니저 (수정 최소화)
└── Module/       ← 게임 모듈 전용 작업 영역 (씬별 하위 폴더)
```

- 폴더명은 `Assets/Scripts/`지만 코드 네임스페이스는 `Game.*`을 유지한다.
  `GameFramework.Game.*`(프레임워크 게임 레이어)와 구분되며, 의도된 명명이다.
- `Assets/GameFramework/` 는 절대 수정 금지.
