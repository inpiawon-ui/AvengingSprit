# wa-plan 에이전트 팀

> **Team**: wa-plan  
> **Version**: 1.0.0  
> **Last Updated**: 2026-05-25  
> **진입점**: `wa-manager-plan-lead`

---

## 개요

게임 개발 문서 템플릿 저장소의 기획 단계를 자동화하는 AI 에이전트 팀입니다.  
게임 설명 텍스트를 입력하면 Concept → A계층 → C → D/E 문서까지 순차·병렬로 생성합니다.

---

## 에이전트 목록

| 에이전트 | 직군 | 담당 문서 |
|---------|------|---------|
| **wa-manager-plan-lead** | 팀장/오케스트레이터 | `wa-manager-plan-lead-log.md` |
| **wa-plan-team-concept** | 컨셉 기획자 | `[ProjCode]_Concept.md` |
| **wa-plan-team-guide** | 가이드 문서 기획자 | `Unity_GameDev_Template.md` (프로젝트 전용 재작성) |
| **wa-plan-team-project** | 총괄 기획자 | `[ProjCode]_ProjectPlan.md` |
| **wa-plan-team-system** | 시스템 기획자 | `[ProjCode]_GD_Design.md` (GD-COR/SYS 섹션) |
| **wa-plan-team-balance-combat** | 전투 밸런스 기획자 | `[ProjCode]_GD_Design.md` (전투 섹션) |
| **wa-plan-team-balance-economy** | 경제 밸런스 기획자 | `[ProjCode]_GD_Design.md` (경제 섹션) |
| **wa-plan-team-content** | 컨텐츠 기획자 | `[ProjCode]_GD_Design.md` (LVL 섹션) |
| **wa-plan-team-narrative** | 내러티브 기획자 | `[ProjCode]_GD_Design.md` (NAR 섹션) |
| **wa-plan-team-monetization** | 수익화 기획자 | `[ProjCode]_Monetization_Design.md` |
| **wa-plan-team-reviewer** | 검수 기획자 | `[ProjCode]_ReviewReport.md` |

---

## 실행 방법

### 진입점

`wa-manager-plan-lead`를 호출하고 아래 형식으로 게임 설명을 전달합니다:

```
wa-manager-plan-lead를 실행해줘.

게임 설명: [자유 형식 게임 설명 텍스트]
ProjCode: [선택 — 없으면 lead가 게임명에서 자동 생성]
```

### 사람 검토 게이트

Concept 문서 생성 후 **반드시 사람이 검토하고 승인해야** 다음 단계가 진행됩니다.  
Lead가 아래 메시지로 대기합니다:

```
⛔ HUMAN REVIEW GATE
Concept 문서가 생성되었습니다: Projects/[ProjCode]/[ProjCode]_Concept.md
검토 후 다음 중 하나로 응답해주세요:
  - "계속" → 다음 단계 진행
  - "수정: [수정 내용]" → Concept 재작성 후 재검토
```

---

## 에이전트 실행 흐름

```
게임 설명
    ↓
wa-manager-plan-lead (Phase A: 준비)
    ├── ProjCode 확정
    ├── Projects/[ProjCode]/ 생성
    └── Template/ 전체 복사

    ↓ (Phase B: 순차/병렬 실행)

    1. wa-plan-team-concept → [ProjCode]_Concept.md
    ⛔ HUMAN REVIEW GATE (사람 승인 대기)

    2. wa-plan-team-guide → Unity_GameDev_Template.md (재작성)
    3. wa-plan-team-project → [ProjCode]_ProjectPlan.md

    4. wa-plan-team-system (단독 먼저)
       → [ProjCode]_GD_Design.md (COR/SYS 섹션)

    5. 병렬 실행:
       ├── wa-plan-team-balance-combat  → GD_Design (전투 섹션)
       ├── wa-plan-team-balance-economy → GD_Design (경제 섹션)
       ├── wa-plan-team-content         → GD_Design (LVL 섹션)
       ├── wa-plan-team-narrative       → GD_Design (NAR 섹션)
       └── wa-plan-team-monetization    → Monetization_Design

    6. Lead: GD_Design.md 섹션 병합

    7. wa-plan-team-reviewer → [ProjCode]_ReviewReport.md

    ↓ (Phase C: 완료)
    wa-manager-plan-lead 최종 승인 및 완료 보고
```

---

## 생성 파일 구조 (프로젝트별)

```
Projects/[ProjCode]/
├── [ProjCode]_Concept.md           ← wa-plan-team-concept
├── Unity_GameDev_Template.md       ← wa-plan-team-guide (A계층 재작성)
├── [ProjCode]_ProjectPlan.md       ← wa-plan-team-project (C)
├── [ProjCode]_GD_Design.md         ← wa-plan-team-system + balance-combat/economy + content + narrative 병합 (D)
├── [ProjCode]_SND_Design.md        ← wa-manager-sound-lead 팀 (사운드 방향 기획)
├── [ProjCode]_Monetization_Design.md ← wa-plan-team-monetization
├── [ProjCode]_ReviewReport.md      ← wa-plan-team-reviewer
└── wa-manager-plan-lead-log.md             ← wa-manager-plan-lead (실행 로그)
```

---

## 저작 규칙 요약 (CLAUDE.md 기반)

모든 에이전트가 준수하는 공통 규칙:

| 규칙 | 형식 | 설명 |
|------|------|------|
| 플레이스홀더 | `[대괄호]` | 문서 완성 전 반드시 채울 것 |
| 미결 항목 | `[TBD — 이유: ...]` | 결정 불가 항목, 이유 명시 필수 |
| 장르 확장 | `### [GENRE-SPECIFIC]: [내용명]` | 장르 특화 내용 추가 시 사용 |
| 파트 충돌 | `[WARNING]` | 파트 간 모순 즉시 표시 |
| 수치 출처 | `[근거: ...]` | 모든 지표에 출처 병기 |
| 버전 관리 | Version + Last Updated | 변경 시 반드시 갱신 |
| E 문서 DoD | B 파트 파일 체크리스트 | `[대괄호]` 미완성 = 미완료 |

---

## 에이전트 간 의존 관계

```
wa-manager-plan-lead
    └── wa-plan-team-concept
            ↓ [HUMAN GATE]
    └── wa-plan-team-guide
    └── wa-plan-team-project (C 확정)
            ↓
    └── wa-plan-team-system (GD-COR/SYS — 다른 에이전트의 기반)
            ↓
    ├── wa-plan-team-balance-combat   (GD-SYS 기반)
    ├── wa-plan-team-balance-economy  (GD-SYS 기반)
    ├── wa-plan-team-content          (GD-SYS 기반)
    ├── wa-plan-team-narrative        (Concept 기반)
    └── wa-plan-team-monetization     (C 기반)
            ↓ [병합]
    └── wa-plan-team-reviewer
```
