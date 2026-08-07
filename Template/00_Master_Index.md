# Unity_GameDev_Template — 마스터 인덱스

> **Version**: 1.5.0
> **Last Updated**: 2026-06-07
> **Document Owner**: Game Director
> **Status**: Template (프로젝트별 복제 후 사용)

---

## 📑 문서 구조 개요

본 템플릿은 **Unity 기반 게임 개발 전 파트**를 커버하는 범용 문서 세트입니다.
5계층 구조로 구성됩니다:

| 계층 | 파일 | 역할 | 생성 시점 | 내용 깊이 |
|------|------|------|---------|---------|
| **A** 가이드 문서 | `Unity_GameDev_Template.md` | 장르·형태 무관한 범용 개요. C 생성 기준점. | 공통 (1회 작성) | 파트당 개념 수준 |
| **B** 파트 문서 | `01_Common.md` ~ `09_Modules.md` | A 파생 + 파트 고유 세부 템플릿. E 생성 기준점. | 공통 (1회 작성) | 파트별 완전한 템플릿 |
| **C** 계획서 | `[ProjCode]_ProjectPlan.md` | 전체 방향·KPI·파트 개요. A 기반 생성. | 킥오프 전 | 파트당 5~10줄 방향 |
| **D** 세부 설계서 | `[ProjCode]_[PartCode]_Design.md` | 파트별 상세 스펙 확정. C 확정 후 파트당 1파일. | C 확정 후 | 파트당 완전한 설계 |
| **E** 실무 문서 | `[ProjCode]_[PartCode].md` | 작업자가 실제 사용하는 문서. D + B 기반 생성. | D 확정 후 | B 템플릿 + D 내용 합성 |

> **사용 흐름**: A → C → D → E (4단계 생성 흐름. 아래 AI 활용 워크플로우 참조)

---

## 🤖 AI 활용 워크플로우

### Step 1. C (전체 계획서) 생성

`Unity_GameDev_Template.md`(A)를 Claude에게 전달하면서 요청:
```
이 Unity 게임 개발 가이드 템플릿(가이드 문서 A)을 기반으로,
다음 게임의 프로젝트 계획서(C)를 작성해줘.
각 파트는 방향·KPI·핵심 결정사항 위주로 간결하게 작성해줘.
장르 특화 내용은 [GENRE-SPECIFIC] 태그로 추가해줘.

게임 설명: [예: 쿠키런 스타일 횡스크롤 2D 액션 게임, 모바일, 전 연령]
추가 조건: [예: 오프라인 전용 / 글로벌 동시 출시]
```

→ 결과물: `[ProjCode]_ProjectPlan.md` (C)

### Step 2. D (파트별 세부 설계서) 생성

C 확정 후, 파트별로 B 템플릿 + C의 해당 파트 섹션을 Claude에게 전달:
```
첨부된 프로젝트 계획서(C)와 이 파트 템플릿(B)을 기반으로,
[파트명] 세부 설계서(D)를 작성해줘.
설계상 미확정 항목은 [TBD — 이유: ...] 로 표시해줘.

※ SND(사운드) 파트는 사운드 디렉터 또는 전담 인력이 있을 경우에만 작성. 인력이 없으면 해당 파트 생략 가능.
```

→ 결과물: `[ProjCode]_[PartCode]_Design.md` (D, 파트당 1파일)

### Step 3. E (파트 실무 문서) 생성

D 확정 후, D + 파트 B 템플릿을 Claude에게 전달:
```
이 세부 설계서(D)와 파트 템플릿(B)을 기반으로,
[파트명] 실무 문서(E)를 작성해줘.
B 템플릿의 모든 섹션을 포함하고, D의 확정 수치로 [대괄호] 플레이스홀더를 전부 채울 것.
B 문서의 "E 문서 작성 가이드" DoD 체크리스트를 만족해야 함.
```

→ 결과물: `[ProjCode]_[PartCode].md` (E)

### Step 4. 지속 갱신

개발 진행에 따라 D, E 문서를 갱신.
파트 간 충돌(`[WARNING]` 태그) 발생 시 양 파트 리드 공동 리뷰.
C는 전체 방향 변경 시에만 수정.

---

## 🔁 에이전트 자동화 범위 (인덱스 ⊇ 에이전트 GD 슬라이스)

본 인덱스는 **사람이 운용하는 전체 문서 세트(A~E 전 파트)**를 정의한다.
반면 **기획팀 에이전트(`wa-manager-plan-lead` + `wa-plan-team-*`) 자동화 파이프라인**은 그 부분집합인 **기획(GD) 중심 슬라이스**만 생성한다:

```
[ProjCode]_Concept.md  →  Unity_GameDev_Template.md(프로젝트 재작성)  →  [ProjCode]_ProjectPlan.md(C)
   →  [ProjCode]_GD_Design.md(GD 세부)  →  [ProjCode]_Monetization_Design.md(MON)  →  [ProjCode]_ReviewReport.md
```

- **인덱스 ⊇ 에이전트 산출물**: 인덱스가 정의한 ART/CL/SV/SND/QA/PM 파트의 D·E 문서는 기획 에이전트가 생성하지 않는다. 해당 파트는 구현 단계에서 각 팀(art/client/server/sound 등)이 담당한다.
- 서버 계약 산출물은 별도로 `Projects/[ProjCode]/[ProjCode]_SRV_Design.md`(섹션 ID SRV-*)로 관리한다(서버팀).
- 즉, 인덱스는 **목표 전체 구조**, 에이전트 자동화는 **현재 자동 생성되는 기획 슬라이스**다. 둘의 차이는 의도된 것이며 시간이 지나며 자동화 범위가 인덱스 쪽으로 확장될 수 있다.

---

## 📂 파일 목록

### 가이드 문서 (A) — 범용 개요, C 생성 기준점

| 파일명 | 역할 | 핵심 내용 |
|--------|------|----------|
| `Unity_GameDev_Template.md` | **가이드 문서 (A)** | 전 파트 한눈 파악. Claude에 전달하여 C (계획서) 생성. |
| `Unity_GameDev_Concept.md` | **컨셉 템플릿 (A)** | Concept 문서 7개 섹션(CONCEPT/GAMEPLAY/PRESENTATION/MONETIZATION/MARKET/PRODUCTION/AUDIO) 구조 기준. `wa-plan-team-concept`가 `[ProjCode]_Concept.md` 생성/확장 시 참조. |

### 파트 문서 (B) — 파트별 세부 템플릿, E 생성 기준점

| 파일명 | 파트 | 파트 코드 | 핵심 내용 |
|--------|------|----------|-----------|
| `00_Master_Index.md` | 전체 | — | 본 문서. 문서 구조·역할·AI 워크플로우 안내 |
| `01_Common.md` | 공통 | COM | 프로젝트 개요, 용어집, 네이밍 규약, 워크플로우, 리스크 |
| `02_GameDesign.md` | 기획 | GD | 코어루프, 시스템 설계, 레벨 디자인, 밸런스, 내러티브 |
| `03_Art.md` | 아트 | ART | 스타일 가이드, 캐릭터/환경/UI/VFX/애니메이션·리소스 마스터 목록 |
| `04_Client.md` | 클라이언트 | CL | 아키텍처, 렌더링, 입력, 최적화, 빌드 파이프라인 |
| `05_Server.md` | 서버 | SV | 아키텍처, 네트워크, DB, 보안, 운영 |
| `06_Sound.md` | 사운드 | SND | 오디오 디렉션, BGM, SFX, VO, Unity 통합 |
| `07_QA.md` | QA | QA | 테스트 계획, TC 양식, 버그 프로세스, 릴리즈 기준 |
| `08_ProjectManagement.md` | 프로젝트 관리 | PM | 마일스톤, 스프린트, DoD, 리스크, 커뮤니케이션 |
| `09_Modules.md` | 모듈 카탈로그 | MOD | 재사용 가능 독립 모듈 목록 및 상세 템플릿 |

### 계획서 (C) — 프로젝트별 전체 방향서

| 파일명 패턴 | 역할 | 핵심 내용 |
|------------|------|----------|
| `[ProjCode]_ProjectPlan.md` | **계획서 (C)** | 전체 KPI·일정·팀 구성 + 파트별 5~10줄 방향 요약. A 기반 생성. |

### 세부 설계서 (D) — 파트별 스펙 확정, E 생성 입력값

| 파일명 패턴 | 역할 | 핵심 내용 |
|------------|------|----------|
| `[ProjCode]_[PartCode]_Design.md` | **세부 설계서 (D)** | 파트별 상세 스펙 확정. C 확정 후 파트당 1파일. |

### 실무 문서 (E) — 작업자 실사용 문서

| 파일명 패턴 | 역할 | 핵심 내용 |
|------------|------|----------|
| `[ProjCode]_[PartCode].md` | **실무 문서 (E)** | D + B 합성. 모든 플레이스홀더 채운 완성 형태. |

---

## 🗂 문서 ID 체계

```
[PartCode]-[DocType]-[Number]_[Title].md

예시: GD-COR-001_CoreLoop.md
     → 기획(GD)의 코어(COR) 1번 문서
```

| 파트 코드 | 파트명 | 담당자 |
|----------|--------|--------|
| COM | Common | Game Director |
| GD | Game Design | Game Designer |
| ART | Art | Art Director |
| CL | Client | Lead Client Programmer |
| SV | Server | Lead Server Programmer |
| SND | Sound | Sound Director |
| QA | Quality Assurance | QA Lead |
| PM | Project Management | Project Manager |
| MOD | Module Catalog | 해당 파트 리드 |
| MON | Monetization | Game Designer / Monetization |
| OPS | Live Operations | Operations Manager |

> **MON**: `wa-plan-team-monetization`이 생성하는 `[ProjCode]_Monetization_Design.md`의 파트 코드. GD에서 분리된 수익화 전용 슬라이스다.
> **OPS**: 현재 에이전트 자동화 파이프라인에서 **미사용**(B 파트 문서 없음). 라이브 운영 단계 도입 시 활성화한다.

---

## ✅ 문서 생성 순서 (A→C→D→E)

### Phase 1: C (계획서) 생성

- [ ] A (`Unity_GameDev_Template.md`) + 게임 설명 → Claude로 C (`[ProjCode]_ProjectPlan.md`) 생성
- [ ] C 검토 및 방향 확정 (팀 합의)

### Phase 2: D (세부 설계서) 생성 — 파트별 순서대로

- [ ] GD 파트: C의 GD 섹션 + `02_GameDesign.md`(B) → `[ProjCode]_GD_Design.md` (D)
- [ ] ART 파트: C의 ART 섹션 + `03_Art.md`(B) → `[ProjCode]_ART_Design.md` (D)
- [ ] CL 파트: C의 CL 섹션 + `04_Client.md`(B) → `[ProjCode]_CL_Design.md` (D)
- [ ] SV 파트: C의 SV 섹션 + `05_Server.md`(B) → `[ProjCode]_SV_Design.md` (D)
- [ ] SND / QA / PM 파트: 동일 방식으로 D 생성

### Phase 3: E (실무 문서) 생성 — 파트별

- [ ] 각 파트: D + B → E (`[ProjCode]_[PartCode].md`)
- [ ] B의 "E 문서 작성 가이드" DoD 체크리스트 충족 확인

### Phase 4: 지속 갱신

- [ ] **지속적 업데이트** — 개발 진행에 따라 D, E 현행화
- [ ] **지속적 업데이트** — `01_Common.md` COM-RSK-001 (리스크 레지스터)
- [ ] **지속적 업데이트** — `08_ProjectManagement.md` 마일스톤/스프린트 현행화

---

## 📏 전 파트 공통 일관성 규칙

| 규칙 | 설명 | 예시 |
|------|------|------|
| **용어 정의** | 전문 용어는 최초 등장 시 반드시 정의 | `DAU(Daily Active User, 일일 활성 사용자)` |
| **수치 근거** | 모든 수치는 근거 병기 | `목표 FPS: 60 [근거: 모바일 액션 장르 표준, GDC 2024]` |
| **상충 표시** | 문서 간 충돌 시 `[WARNING]` 태그 즉시 표시 | `[WARNING] CL-OPT-001은 Draw Call ≤100 요구, ART-VFX-001은 평균 150 사용` |
| **버전 관리** | 변경 시 Version·Last Updated 반드시 갱신 | `v1.1.0 / 2026-05-01` |
| **파트 간 참조** | 다른 파트 문서 참조 시 문서 ID 명시 | `→ 참조: CL-OPT-001` |

---

## 🔗 파트 간 의존 관계

```
COM-OVR-001 (프로젝트 개요)
    ├── GD-COR-001 (코어 루프)
    │       ├── GD-SYS-001 (시스템 설계)
    │       │       ├── CL-ARC-001 (클라 아키텍처)
    │       │       └── SV-ARC-001 (서버 아키텍처)
    │       └── GD-ECO-001 (밸런스)
    │               └── SV-NET-001 (네트워크 프로토콜)
    ├── ART-STY-001 (스타일 가이드)
    │       ├── ART-CHR-001 / ART-ENV-001 / ART-VFX-001
    │       └── SND-STY-001 (오디오 스타일)
    └── COM-GLS-001 (용어집) ←── 전 파트 공유
```

---

## 📝 문서 관리 원칙

1. 본 문서 세트는 **Living Document**입니다. 프로젝트 진행에 따라 지속 갱신.
2. 변경 시 반드시 **Version** 및 **Last Updated** 갱신.
3. 큰 변경은 각 파일 하단 **개정 이력** 테이블에 기록.
4. 파트 간 상충 발생 시 즉시 **`[WARNING]`** 태그 표시 후 관련 파트 리드 소집.
5. 용어 신규 등장 시 `01_Common.md`의 COM-GLS-001에 선등록 후 타 문서 인용.

---

## 개정 이력

| 버전 | 날짜 | 작성자 | 변경 내용 |
|------|------|--------|----------|
| 1.0.0 | 2026-04-16 | [작성자] | 초기 작성 |
| 1.1.0 | 2026-04-17 | [작성자] | 파일 목록, 파트 간 의존 관계 추가 |
| 1.2.0 | 2026-04-17 | [작성자] | A/B/C/D/E 5계층 구조 정의, AI 워크플로우 4단계로 업데이트 |
| 1.3.0 | 2026-04-18 | [작성자] | 프로젝트별 문서 현황 테이블 제거 (템플릿 오염 방지 — 해당 정보는 C 문서에 관리) |
| 1.4.0 | 2026-04-18 | [작성자] | OPS 파트 코드 추가, Step 1 프롬프트에 [GENRE-SPECIFIC] 태그 안내 추가, Step 2 프롬프트에 SND 인력 주의사항 및 TBD 표기 형식 반영 |
| 1.5.0 | 2026-06-07 | [작성자] | `Unity_GameDev_Concept.md` 가이드 목록 등재, MON 파트 코드 추가, OPS 미사용 주석, 에이전트 자동화 범위(인덱스 ⊇ GD 슬라이스) 안내 절 추가 |

---

*End of Master Index*
