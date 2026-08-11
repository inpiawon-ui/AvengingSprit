---
description: 리포 운영 모드 단일 출처(SSOT). TEMPLATE(범용 부모) vs GAME(특정 게임 활성화). 모든 작업의 전제는 이 파일의 MODE를 따른다.
alwaysApply: true
---

# 프로젝트 상태 (Project State)

이 파일은 **이 리포가 지금 무엇인지**를 선언하는 단일 출처(SSOT)다.
Claude는 모든 작업 전에 이 `MODE` 값을 먼저 확인하고, `.claude/CLAUDE.md`의 모드 가드를 적용한다.

---

## 현재 모드

```
MODE: GAME
ProjCode: AVSR
게임명: AVENGING SPIRIT: RE:BORN
활성화일: 2026-08-07
```

> **기획 정본**: [`Projects/AVSR/Canon/`](../../Projects/AVSR/Canon/README.md) — Developer Handoff v1.5 (2026-08-11 채택)
> 문서가 서로 다르면 **`Canon/Runtime/*.json` 이 이긴다.** 우선순위·이관 계획은
> [`Projects/AVSR/AVSR_Decisions.md`](../../Projects/AVSR/AVSR_Decisions.md) 10절 참조.
>
> **확정 컨셉**: [`Template/Game_Concept.md`](../../Template/Game_Concept.md)
> **디자인 레퍼런스**: `Projects/AVSR/Reference/` (제안서 PDF 추출본 — 목업 5종·호스트 12종·슬라이드 10p)
>
> **아트 제1원칙**: 1991 아케이드 원작 **AVENGING SPIRIT의 감성 재현이 최우선**이다.
> 모던함·트렌드와 충돌하면 원작 감성을 택한다.

---

## 모드 정의

| MODE | 의미 | 작업 전제 |
|------|------|-----------|
| `TEMPLATE` | 이 리포는 **모든 게임의 부모(범용 템플릿)**. 특정 게임이 아님. | 게임 고유 코드/에셋/기획을 **임의 생성 금지**. 프레임워크·규칙·에이전트·문서 정비만 수행. |
| `GAME` | PD 부트스트랩으로 **특정 게임이 활성화**된 상태. | `Projects/[ProjCode]/`·`Template/Game_Concept.md`·`constants.md`를 게임 컨텍스트로 인지하고 개발 진행. |

---

## 전환 규칙 (TEMPLATE → GAME)

1. 사용자가 **"개발 시작"** 지시를 내린다.
2. PD(`wa-manager-pd`)가 `Template/Game_Concept.md` 존재를 확인한다. (없으면 TEMPLATE 유지·보고·중단)
3. PD가 `Game_Concept.md`에서 `ProjCode`/게임명을 취득한다.
4. PD가 이 파일의 `MODE` 블록을 `GAME` + `ProjCode`/게임명/활성화일로 갱신한다.

> **이 파일의 `MODE`가 최종 권위다.** `Template/Game_Concept.md`가 존재해도 `MODE: TEMPLATE`이면 리포는 여전히 템플릿이다.
> (테스트용 `Game_Concept.md`가 있어도 PD가 명시적으로 활성화하기 전까지는 활성화로 간주하지 않는다.)

---

## 부모(main) 브랜치 주의

- 부모/main 브랜치는 항상 `MODE: TEMPLATE`을 유지한다.
- `Template/Game_Concept.md`는 per-game 입력이므로 **부모/main에 커밋하지 않는다.**
- 새 게임 = 리포 복사 → `Game_Concept.md` 추가 → "개발 시작" → 이 파일이 `GAME`으로 전환.
