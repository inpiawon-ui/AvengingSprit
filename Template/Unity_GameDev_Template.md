# Unity 기반 게임 개발 범용 템플릿 문서

> **Version**: 1.7.0
> **Last Updated**: 2026-04-18
> **Document Owner**: Game Director
> **Status**: Template (프로젝트별 복제 후 사용)

---

## 🤖 AI 활용 프로토콜

> **이 문서의 역할**: 가이드 문서(A). 장르·형태에 종속되지 않는 Unity 게임 개발 전 파트의 범용 기준.
> 파트별 세부 템플릿은 `01_Common.md` ~ `09_Modules.md` (파트 문서 B) 를 참조.

### 문서 계층 구조 및 역할

| 문서 | 기호 | 역할 | 내용 깊이 | 생성 시점 |
|------|------|------|---------|---------|
| 가이드 문서 | **A** | 전 파트 범용 기준. Claude에게 전달해 C 생성 | 파트당 개요 수준 | 템플릿 (1회 작성) |
| 파트 템플릿 | **B** | 파트별 세부 규칙·포맷 정의. E 생성의 구조 기반 | 파트별 완전한 템플릿 | 템플릿 (1회 작성) |
| 프로젝트 계획서 | **C** | 프로젝트 전체 방향·KPI·파트 개요 (A 기반 생성) | 파트당 5~10줄 요약 | 킥오프 전 |
| 세부 설계서 | **D** | 파트별 상세 스펙 확정 (C 확정 후, 파트당 1파일) | 파트당 완전한 설계 | C 확정 후 |
| 파트 실무 문서 | **E** | 실제 작업자가 사용하는 문서 (D + B 기반 생성) | B 템플릿 + D 내용 합성 | D 확정 후 |

> 파일명 관례: C = `[ProjCode]_ProjectPlan.md` / D = `[ProjCode]_[PartCode]_Design.md` / E = `[ProjCode]_[PartCode].md`

---

### Claude를 통한 4단계 문서 생성 워크플로우

#### Step 1 — C (프로젝트 계획서) 생성

이 문서(A)를 Claude에게 첨부하고 아래 형식으로 요청:

```
이 Unity 게임 개발 가이드 템플릿(가이드 문서 A)을 기반으로,
다음 게임의 프로젝트 계획서(C)를 작성해줘.
각 파트는 방향·KPI·핵심 결정사항 위주로 간결하게 작성해줘.
장르 특화 내용은 [GENRE-SPECIFIC] 태그로 추가해줘.

게임 설명: [예: 쿠키런 스타일의 횡스크롤 2D 자동 달리기 게임, 모바일, 가족 유저]
추가 조건: [예: 오프라인 전용 / 멀티플레이어 지원 / PC+모바일 크로스플랫폼]
```

#### Step 2 — D (파트별 세부 설계서) 생성

C 확정 후, 해당 파트의 B 문서와 C를 Claude에게 전달:

```
첨부된 프로젝트 계획서(C)와 이 파트 템플릿(B)을 기반으로,
[파트명] 세부 설계서(D)를 작성해줘.
설계상 미확정 항목은 [TBD — 이유: ...] 로 표시해줘.

※ SND(사운드) 파트는 사운드 디렉터 또는 전담 인력이 있을 경우에만 작성. 인력이 없으면 해당 파트 생략 가능.
```

#### Step 3 — E (파트 실무 문서) 생성

D 확정 후, D와 해당 파트 B를 Claude에게 전달:

```
첨부된 [파트명] 세부 설계서(D)와 파트 템플릿(B)을 기반으로,
실제 작업자가 사용할 [파트명] 파트 실무 문서(E)를 작성해줘.
B 템플릿의 모든 섹션을 포함하고, [TBD] 항목이 없도록 D 내용으로 채워줘.
B 문서의 "E 문서 작성 가이드" DoD 체크리스트를 모두 충족해야 함.
```

#### Step 4 — 반복 갱신

개발 진행에 따라 D, E를 지속 갱신. 파트 간 충돌 발생 시 `[WARNING]` 태그 표시 후 관련 파트 리드 공동 리뷰.

---

### Claude 작성 지침 (이 문서를 받은 Claude가 따르는 규칙)

1. **구조 유지**: 섹션 순서와 ID 체계(`COM-OVR-001` 등)를 그대로 유지한다
2. **플레이스홀더 채우기**: 모든 `[대괄호]` 항목을 게임 특성에 맞는 구체적인 값으로 대체한다
3. **장르 확장**: 장르 특화 내용은 관련 섹션 끝에 `### [GENRE-SPECIFIC]: [내용명]` 형태로 추가한다
4. **근거 명시**: 수치 제시 시 반드시 `[근거: ...]` 형식으로 출처를 표기한다
5. **충돌 표시**: 파트 간 잠재적 충돌 발생 시 즉시 `[WARNING]` 태그로 표시한다
6. **미결 항목**: 결정 불가한 항목은 `[TBD — 이유: ...]`로 표시하고, 결정 필요 이유를 명시한다
7. **문서 계층 준수**: C 생성 시 각 파트를 간결 요약 수준으로 작성. D 생성 시 해당 파트를 완전히 설계. E 생성 시 B 템플릿 구조를 빠짐없이 채운다

### 장르별 주요 확장 포인트

| 장르 | 기획(GD) 추가 항목 | 클라이언트(CL) 추가 항목 | 서버(SV) 추가 항목 |
|------|-----------------|--------------------|--------------------|
| 횡스크롤 액션 | 자동 이동/점프 물리, 장애물 패턴 생성 로직, 카메라 추적 | Rigidbody2D 설정, 카메라 추적 스크립트 | 점수·리플레이 저장 |
| RPG | 스킬 트리, 파티 시스템, 퀘스트 체인 | 스킬 이펙트 시스템, 파티 UI | 파티 동기화, 필드 서버 |
| 퍼즐 | 퍼즐 메카닉 규칙, 힌트 시스템, 콤보 보상 | 물리 시뮬레이션, 퍼즐 상태 머신 | 진행 상태 저장 |
| 전략/디펜스 | 유닛 밸런스 매트릭스, 건설 시스템, 웨이브 설계 | 그리드 시스템, 유닛 AI, 패스파인딩 | 실시간 전장 동기화 |
| 실시간 멀티플레이어 | PvP 밸런스, 매칭 기준, 관전 기능 | 지연보상(Lag Compensation), 예측 이동 | 권위 서버, 룸 관리 |
| 오프라인 전용 | 오프라인 콘텐츠·저장 설계 | 로컬 저장소 암호화, 치트 방지 로컬 | 해당 없음 — 서버 섹션 전체 생략 |
| 가챠/컬렉션 | 드롭률 테이블, 천장 시스템, 등급 배율 | 가챠 연출 UI, 확률 표시 | 드롭률 서버 검증, 이력 보관 |
| 타워 디펜스 | 웨이브 패턴, 타워 업그레이드 트리 | 그리드·패스파인딩, 타워 배치 UI | 진행 저장 (스테이지 단위) |
| 방치형(Idle) | 오프라인 누적 보상, 자동화 루프 설계 | 시간 경과 계산, 백그라운드 처리 | 서버 시간 검증 (클라 시간 조작 방지) |

---

## 📑 목차

- [0. 문서 사용 가이드](#0-문서-사용-가이드)
- [1. COMMON (공통)](#1-common-공통)
- [2. GAME DESIGN (기획)](#2-game-design-기획)
- [3. ART (아트)](#3-art-아트)
- [4. CLIENT (클라이언트)](#4-client-클라이언트)
- [5. SERVER (서버)](#5-server-서버)
- [6. SOUND (사운드)](#6-sound-사운드)
- [7. QA (품질 보증)](#7-qa-품질-보증)
- [8. PROJECT MANAGEMENT (프로젝트 관리)](#8-project-management-프로젝트-관리)
- [9. 모듈 카탈로그 (재사용 가능 블록)](#9-모듈-카탈로그-재사용-가능-블록)

---

## 0. 문서 사용 가이드

### 0.1 문서 철학
본 템플릿은 **"한 번 작성하여 여러 번 재사용"**을 목표로 합니다. 각 섹션은 **독립적인 파일로 분리 가능**하며, 다른 프로젝트에서 그대로 가져가 사용할 수 있도록 설계되었습니다.

### 0.2 문서 명명 규칙

> **규칙 A — 섹션을 독립 파일로 분리할 때** (단일 파트 문서를 여러 파일로 쪼갤 경우):
```
[PartCode]-[DocType]-[Number]_[Title].md

예시: GD-COR-001_CoreLoop.md
     → 기획파트(GD)의 코어(COR) 1번 문서, 제목은 CoreLoop
```

> **규칙 B — 프로젝트 문서(C/D/E) 파일명** (Claude 워크플로우로 생성되는 파일):
```
C (프로젝트 계획서):  [ProjCode]_ProjectPlan.md
D (세부 설계서):      [ProjCode]_[PartCode]_Design.md
E (파트 실무 문서):   [ProjCode]_[PartCode].md
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
| OPS | Live Operations | Operations Manager |

### 0.3 일관성 규칙 (전 파트 공통)

| 규칙 | 설명 | 예시 |
|-----|------|------|
| **용어 정의** | 전문 용어는 최초 등장 시 반드시 정의 | `DAU(Daily Active User, 일일 활성 사용자)` |
| **수치 근거** | 모든 수치는 근거 병기 | `프레임 목표: 60 FPS [근거: 모바일 액션 장르 업계 표준, GDC 2024]` |
| **상충 표시** | 문서 간 충돌 시 [WARNING] 태그 | `[WARNING] CL-OPT-001은 Draw Call 100 이하를 요구하나, ART-VFX-001은 평균 150을 사용함` |

---

## 1. COMMON (공통)

> **범위**: 전 파트가 공유하는 최상위 기준. 변경 시 모든 하위 문서 재검토 필수.

- **COM-OVR-001**: 프로젝트 개요 — 식별정보·컨셉·타겟·KPI (D1/D7 리텐션, ARPDAU)
- **COM-GLS-001**: 용어집 — 신규 용어 선등록 원칙. DAU, KPI, Core Loop, Draw Call 등
- **COM-CVT-001**: 네이밍 규약 — 에셋 `[타입]_[카테고리]_[상세]`, C# PascalCase/camelCase, Git 브랜치 명명
- **COM-WFL-001**: 워크플로우 — Concept→Pre-Production→Production→Polish→Live 5단계, 에셋 파이프라인, 빌드 주기
- **COM-RSK-001**: 리스크 레지스터 — 인력이탈/플랫폼정책/기술호환/일정지연 4대 리스크
- **COM-ANA-001**: Analytics & Telemetry — 분석 도구 선택, EVT-001~011 필수 이벤트, GDPR/COPPA 준수 경고
- **COM-PVY-001**: Privacy & Compliance — GDPR·COPPA·국내법·PIPL 체크리스트, ATT 동의·계정 삭제 구현 필수
→ 세부: [01_Common.md](./01_Common.md) 참조

🔌 장르 확장 포인트: → [01_Common.md](./01_Common.md) 참조

---

## 2. GAME DESIGN (기획)

> **의존성**: COM-OVR-001, COM-GLS-001
> **핵심 원칙**: 모든 기획 수치는 밸런스 시뮬레이션 결과로 증명

- **GD-COR-001**: Core Loop — 목표→행동→보상→성장 사이클 + 메타 루프 (단/중/장기)
- **GD-SYS-001**: System Design — 시스템 목록 (우선순위/MVP 여부) + 시스템 상세 템플릿
- **GD-LVL-001**: Level Design — 학습→연습→심화→보상 4단 구조, 난이도 곡선, 튜토리얼 온보딩 설계
- **GD-ECO-001**: Economy & Balance — Soft/Hard/Premium 재화 체계, 전투 데미지 공식
- **GD-NAR-001**: Narrative — 세계관 바이블, 대사 톤 가이드, 스토리-시스템 연동 표
- **GD-PTT-001**: Playtest Protocol — 내부알파→클로즈드→오픈베타→소프트론치 4단계, Think-Aloud, AB 테스트 가이드
→ 세부: [02_GameDesign.md](./02_GameDesign.md) 참조

🔌 장르 확장 포인트: → [02_GameDesign.md](./02_GameDesign.md) 참조

---

## 3. ART (아트)

> **의존성**: COM-OVR-001, GD-COR-001
> **핵심 원칙**: 모든 아트는 Style Guide 기준으로 일관성 유지

- **ART-STY-001**: Style Guide — 비주얼 톤·컬러 팔레트·조명·실루엣 원칙, 레퍼런스 직접 사용 금지
- **ART-CHR-001**: Character Art — 폴리곤/스프라이트 예산 (CL-OPT-001 연동), 텍스처 해상도, 리깅 방식
- **ART-ENV-001**: Environment Art — LOD 정책, Baked Lighting, Static Batching/Sprite Atlas
- **ART-UI-001**: UI Art — 기준 해상도, Canvas Scaler, 접근성 기준 (터치 44pt / 대비 4.5:1 / 폰트 14pt)
- **ART-VFX-001**: Visual Effects — 파티클 예산 (CL-OPT-001 연동), 우선순위 분류, Object Pool 필수
- **ART-ANM-001**: Animation — 3D 본/2D 스프라이트 스왑 선택, 최소 5종 필수 상태 (Idle/Move/Action/Hit/Dead)
→ 세부: [03_Art.md](./03_Art.md) 참조

🔌 장르 확장 포인트: → [03_Art.md](./03_Art.md) 참조

---

## 4. CLIENT (클라이언트)

> **의존성**: COM-OVR-001, GD-SYS-001, ART-STY-001
> **핵심 원칙**: 성능 예산 준수, 플랫폼 최적화

- **CL-ARC-001**: Architecture — 4계층 (Presentation/Application/Domain/Infrastructure), MVP/MVVM + Event Bus + DI 패턴, 폴더 구조
- **CL-COD-001**: C# Coding Standards — `#nullable enable`, 비동기 패턴 (UniTask 권장), GC Allocation 금지 (Update 루프), MonoBehaviour 원칙, struct vs class 기준, sealed 클래스, Span\<T\>
- **CL-RND-001**: Rendering Pipeline — URP 필수, Linear 컬러 스페이스, Shader Graph 우선 (Built-in 사용 금지)
- **CL-INP-001**: Input System — Unity Input System 패키지 (Legacy 미사용), `IInputProvider` 추상화
- **CL-OPT-001**: Optimization — 성능 예산 (FPS 60 / Draw Call ≤100 / 메모리 ≤1.5GB / 빌드 ≤150MB), Addressables 번들 전략
- **CL-SAV-001**: Save System — PlayerPrefs/파일직렬화/클라우드 방식 선택, 버전 필드 필수, 암호화 원칙
- **CL-LOC-001**: Localization — Unity Localization/CSV/써드파티 선택, 7개 구현 체크리스트, 폰트 라이센스 확인
- **CL-BLD-001**: Build Pipeline — CI/CD, Dev/Staging/Production 빌드 변형, 코드 사이닝
→ 세부: [04_Client.md](./04_Client.md) 참조

🔌 장르 확장 포인트: → [04_Client.md](./04_Client.md) 참조

---

## 5. SERVER (서버)

> **의존성**: COM-OVR-001, GD-SYS-001, GD-ECO-001
> **핵심 원칙**: 클라이언트 신뢰 금지, 모든 중요 로직 서버 검증

- **SV-ARC-001**: Architecture — 팀 규모별 방식 선택 (BaaS / Unity NGO 스택 / 혼합 / 자체), Unity Relay CCU 100 이하 적합, 서비스 분리 구조
- **SV-NET-001**: Network Protocol — REST(로그인/상점) / WebSocket(실시간/채팅), 패킷 버전 필드 필수, SLA (P50 ≤50ms / P99 ≤200ms / 가용성 99.9%), Rate Limiting (Sliding Window + Retry-After)
- **SV-DB-001**: DB Schema — 3NF 정규화, 일 1회 풀백업, 유저 ID 해시 샤딩
- **SV-SEC-001**: Security — 5대 위협 대응 (메모리조작/패킷변조/계정탈취/매크로/SQLi)
- **SV-OPS-001**: Operations — Prometheus+Grafana, ELK 로그, Blue-Green/Canary 배포
→ 세부: [05_Server.md](./05_Server.md) 참조

🔌 장르 확장 포인트: → [05_Server.md](./05_Server.md) 참조

---

## 6. SOUND (사운드)

> **의존성**: COM-OVR-001, GD-NAR-001, ART-STY-001
> **핵심 원칙**: 장르 재미를 강화하는 사운드 디자인. 전담 인력 없으면 파트 생략 가능.

- **SND-STY-001**: Audio Style Guide — 미들웨어 선택 (Unity/FMOD/Wwise), 압축 포맷 기준 (BGM→Vorbis Streaming / SFX→ADPCM or Vorbis), AudioSource/Listener 설정 원칙
- **SND-BGM-001**: Music — 씬별 BGM 표, 심리스 루프, Adaptive Music (레이어 크로스페이드 / 수직 리믹싱 / 수평 시퀀싱)
- **SND-SFX-001**: Sound Effects — UI/캐릭터/환경 동시 재생 예산, 샘플레이트 기준
- **SND-VO-001**: Voice Over — 48kHz 24bit WAV 스펙, 지원 언어, 립싱크 여부
→ 세부: [06_Sound.md](./06_Sound.md) 참조

🔌 장르 확장 포인트: → [06_Sound.md](./06_Sound.md) 참조

---

## 7. QA (품질 보증)

> **의존성**: 전 파트
> **핵심 원칙**: 출시 전 Critical 버그 0, 전 디바이스 성능 예산 충족

- **QA-PLN-001**: Test Plan — Unit/Integration/System/Acceptance 4레벨, iOS/Android 고·중·저 등급 디바이스 매트릭스
- **QA-CAS-001**: Test Cases — TC 템플릿 (선행조건·단계·기대결과·우선순위)
- **QA-ACC-001**: Accessibility — 터치 44pt / 대비 4.5:1 / 색맹 시뮬 / 폰트 14pt / 자막 / 텍스트 크기 조절 6항목 체크리스트
- **QA-BUG-001**: Bug Process — Critical(24h) / High(3일) / Medium(1주) / Low(2주) 대응 SLA
→ 세부: [07_QA.md](./07_QA.md) 참조

🔌 장르 확장 포인트: → [07_QA.md](./07_QA.md) 참조

---

## 8. PROJECT MANAGEMENT (프로젝트 관리)

> **의존성**: COM-OVR-001, GD-COR-001
> **핵심 원칙**: 마일스톤 기반 관리, 스프린트 단위 진행, DoD 명시

- **PM-MST-001**: Milestones — Greenlight / Vertical Slice / Alpha / Beta / RC 5단계 + DoD 정의
- **PM-SPR-001**: Sprint — 2주 주기, Planning→Standup→Review→Retro 의식, 스프린트당 1일 버퍼
- **PM-COM-001**: Communication — 일간 스탠드업 / 주간 파트 리드 회의 / 마일스톤 리뷰 / AAR
→ 세부: [08_ProjectManagement.md](./08_ProjectManagement.md) 참조

🔌 장르 확장 포인트: → [08_ProjectManagement.md](./08_ProjectManagement.md) 참조

---

## 9. 모듈 카탈로그 (재사용 가능 블록)

> **목적**: 프로젝트 간 이식 가능한 독립 모듈. 각 모듈은 단독 문서로 관리.

**기본 모듈** (8개): MOD-AUTH ⭐⭐⭐ / MOD-INV ⭐⭐⭐ / MOD-IAP ⭐⭐⭐ / MOD-LOC ⭐⭐⭐ / MOD-ACH ⭐⭐⭐ / MOD-CHAT ⭐⭐ / MOD-MATCH ⭐⭐ / MOD-QUEST ⭐⭐

**확장 모듈** (9개): MOD-SAVE ⭐⭐⭐ / MOD-ADS ⭐⭐ / MOD-ANALYTICS ⭐⭐⭐ / MOD-NOTIFICATION ⭐⭐ / MOD-SOCIAL ⭐⭐ / MOD-CS ⭐⭐ / MOD-RATING ⭐⭐⭐ / MOD-DEEPLINK ⭐⭐ / MOD-UPDATE ⭐⭐⭐

→ 세부 및 장르별 선택 가이드: [09_Modules.md](./09_Modules.md) 참조

---

## 📌 부록: 문서 생성 체크리스트

#### C (프로젝트 계획서) 생성
- [ ] 이 문서(A) + 게임 설명 → Claude에게 요청하여 `[ProjCode]_ProjectPlan.md` 생성
- [ ] 계획서 내 COM-OVR-001 (KPI, 팀, 일정) 확정
- [ ] 계획서 내 GD-COR-001 (코어 루프) 방향 확정
- [ ] 계획서 팀 리뷰 → C 확정

#### D (파트별 세부 설계서) 생성 — C 확정 후
- [ ] `[ProjCode]_GD_Design.md` — 코어루프·밸런스·시스템 상세 설계
- [ ] `[ProjCode]_ART_Design.md` — 스타일 가이드·리소스 수량·테마 확정
- [ ] `[ProjCode]_CL_Design.md` — 아키텍처·성능 예산 수치 확정
- [ ] `[ProjCode]_SV_Design.md` — 기술 스택·서비스 구조 확정
- [ ] `[ProjCode]_SND_Design.md` — 오디오 무드·BGM/SFX 목록 확정 (선택)

#### E (파트 실무 문서) 생성 — 각 D 확정 후
- [ ] `[ProjCode]_Art.md` — D(ART) + `03_Art.md`(B) 기반 생성
- [ ] `[ProjCode]_GameDesign.md` — D(GD) + `02_GameDesign.md`(B) 기반 생성
- [ ] `[ProjCode]_Client.md` — D(CL) + `04_Client.md`(B) 기반 생성
- [ ] `[ProjCode]_Server.md` — D(SV) + `05_Server.md`(B) 기반 생성
- [ ] `[ProjCode]_Sound.md` — D(SND) + `06_Sound.md`(B) 기반 생성

#### 지속 갱신
- [ ] COM-RSK-001 (리스크 레지스터) — 스프린트마다 갱신
- [ ] 모듈 카탈로그 (`09_Modules.md`) — 재사용 모듈 확정 시 등록

---

#### 소프트론치 체크리스트 (전 세계 출시 전 선행)

> 모바일 게임은 **소프트론치(일부 국가 선출시) → KPI 검증 → 글로벌 출시** 순서를 권장합니다.
> 대표 소프트론치 국가: 캐나다, 호주, 필리핀 (문화적 중립성 + 영어권 피드백)

- [ ] 소프트론치 국가 선정 및 스토어 지역 제한 설정
- [ ] D1/D7 리텐션 목표 달성 확인 (COM-OVR-001 KPI 기준)
- [ ] 코어 루프 이탈 포인트 분석 (COM-ANA-001 EVT-005 기반)
- [ ] 크래시율 `[목표: 0.5% 이하]` 달성 확인
- [ ] 수익화 지표(ARPDAU / 전환율) 검증
- [ ] 소프트론치 피드백 반영 후 글로벌 출시 승인

---

#### Go-Live 체크리스트 (출시 준비)

**빌드 & 기술**
- [ ] Release 빌드 서명 완료 (iOS: Distribution 인증서 / Android: keystore)
- [ ] 버전·빌드번호 최종 확정
- [ ] Crashlytics 등 크래시 리포팅 도구 연결 확인
- [ ] 분석 이벤트(COM-ANA-001) 전수 검증 완료

**스토어 심사**
- [ ] 스토어 에셋 완성 (아이콘, 스크린샷, 프로모 영상, 설명문)
- [ ] 연령 등급 심의 완료 (GRAC · ESRB · PEGI · 해당 국가)
- [ ] 개인정보처리방침 URL 등록 및 게임 내 노출
- [ ] 이용약관 최종 확인 (법무 검토)
- [ ] 인앱 결제 상품 스토어 등록 및 검수 완료

**운영 준비**
- [ ] CS(고객센터) 채널 오픈 및 응대 스크립트 준비
- [ ] 서버 스케일링 계획 확정 (출시 직후 트래픽 대응)
- [ ] 모니터링 대시보드(SV-OPS-001) 정상 작동 확인
- [ ] 긴급 패치 배포 절차 문서화 (Hotfix Runbook)
- [ ] 출시일 기념 이벤트·프로모션 콘텐츠 준비 완료

---

> **문서 관리 원칙**
> 1. 본 문서는 **Living Document**입니다. 프로젝트 진행에 따라 지속 갱신.
> 2. 변경 시 반드시 **Version** 및 **Last Updated** 갱신.
> 3. 큰 변경은 Change Log에 기록.
> 4. 파트 간 상충 발생 시 즉시 **[WARNING]** 태그로 표시 후 관련 파트 리드 소집.

---

**End of Document**
