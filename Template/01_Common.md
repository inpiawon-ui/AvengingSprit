# 01. COMMON (공통)

> **Part Code**: COM
> **Version**: 1.3.0
> **Last Updated**: 2026-04-18
> **Document Owner**: Game Director
> **의존성**: 없음 (전 파트의 최상위 기준 문서)
> **기반 문서 (A)**: `Unity_GameDev_Template.md` — 1. COMMON 섹션을 상세화한 파트 문서(B)

> ⚠️ **범위 주의**: 본 섹션의 내용이 변경되면 **모든 하위 문서를 재검토**해야 합니다.

---

## 📑 목차

- [COM-OVR-001: 프로젝트 개요](#com-ovr-001-프로젝트-개요)
- [COM-GLS-001: 용어집](#com-gls-001-용어집)
- [COM-CVT-001: 네이밍 규약](#com-cvt-001-네이밍-규약)
- [COM-WFL-001: 워크플로우](#com-wfl-001-워크플로우)
- [COM-RSK-001: 리스크 레지스터](#com-rsk-001-리스크-레지스터)
- [COM-ANA-001: Analytics & Telemetry](#com-ana-001-analytics--telemetry)
- [COM-PVY-001: Privacy & Compliance](#com-pvy-001-privacy--compliance)

---

## COM-OVR-001: 프로젝트 개요

### 1.1 프로젝트 식별 정보

| 항목 | 내용 |
|------|------|
| 프로젝트 코드명 | `[PROJECT_CODENAME]` |
| 정식 명칭 | `[TBD]` |
| 장르 | `[예: 액션 RPG / 퍼즐 / 슈팅 등]` |
| 플랫폼 | `[iOS / Android / PC(Steam) / Console]` |
| 엔진 | Unity `[버전 명시, 예: 2022.3.x LTS]` |
| 타겟 출시일 | `[YYYY-QQ]` |
| 개발 기간 | `[N개월]` |
| 팀 규모 | 기획 `[N]` / 아트 `[N]` / 클라 `[N]` / 서버 `[N]` / 사운드 `[N]` / QA `[N]` |

### 1.2 게임 컨셉 원스테이트먼트

```
[플레이어는 ___한 세계에서 ___한 역할을 맡아,
 ___한 방식으로 ___한 목표를 달성하는 게임]
```

### 1.3 핵심 타겟

- **Primary Target**: `[연령/성별/관심사]`
- **Secondary Target**: `[연령/성별/관심사]`
- **레퍼런스 게임**: `[게임A (유사점: ___), 게임B (차별점: ___)]`

### 1.4 프로젝트 목표 KPI

> KPI(Key Performance Indicator): 핵심 성과 지표. 프로젝트 성공 여부를 판단하는 수치 기준.

| 지표 | 목표값 | 근거 |
|------|--------|------|
| D1 Retention | `[예: 40%]` | `[근거 데이터 출처]` |
| D7 Retention | `[예: 20%]` | `[근거 데이터 출처]` |
| D30 Retention | `[예: 8%]` | `[장르 평균 벤치마크]` |
| DAU 목표 | `[예: 100,000]` | `[사업 목표 기준]` |
| ARPDAU | `[예: $0.15]` | `[장르 평균 벤치마크]` |
| ARPU | `[예: $5.00]` | `[장르 평균 벤치마크]` |

> 📌 DAU(Daily Active User): 일일 활성 사용자 수. ARPDAU: 일인당 일평균 수익. ARPU: 사용자당 평균 수익.

---

## COM-GLS-001: 용어집

> **운영 원칙**: 새 용어 발생 시 본 문서에 **선등록** 후 타 문서에서 인용한다.
> 용어 추가 시 `최초 등장 문서` 컬럼에 해당 문서 ID를 반드시 기재할 것.

| 용어 | 정의 | 최초 등장 문서 |
|------|------|--------------|
| DAU | Daily Active User. 일일 활성 사용자 수 | COM-OVR-001 |
| MAU | Monthly Active User. 월간 활성 사용자 수 | COM-OVR-001 |
| KPI | Key Performance Indicator. 핵심 성과 지표 | COM-OVR-001 |
| ARPU | Average Revenue Per User. 사용자당 평균 수익 | COM-OVR-001 |
| ARPDAU | Average Revenue Per Daily Active User. 일활성유저 평균 수익 | COM-OVR-001 |
| GDD | Game Design Document. 게임 기획서 | COM-OVR-001 |
| TDD | Technical Design Document. 기술 설계 문서 | CL-ARC-001 |
| Core Loop | 플레이어가 반복적으로 수행하는 핵심 행동 사이클 | GD-COR-001 |
| Meta Loop | 코어 루프 외 장기적 성장·보상 사이클 | GD-COR-001 |
| Draw Call | GPU에 렌더링을 지시하는 CPU 호출. 수가 많을수록 성능 저하 | CL-OPT-001 |
| LOD | Level of Detail. 거리에 따라 모델 디테일을 조절하는 기법 | ART-ENV-001 |
| TTK | Time To Kill. 대상을 처치하는 데 걸리는 시간 | GD-ECO-001 |
| UV | UV Mapping. 3D 오브젝트 표면에 2D 텍스처를 매핑하는 좌표 체계 | ART-CHR-001 |
| PBR | Physically Based Rendering. 물리 기반 렌더링 | ART-CHR-001 |
| Object Pool | 오브젝트를 매번 생성/삭제 대신 재사용하는 메모리 관리 패턴 | CL-OPT-001 |
| Addressables | Unity 에셋 번들 관리 시스템. 런타임 로드/언로드 최적화 | CL-OPT-001 |
| CI/CD | Continuous Integration/Delivery. 자동 빌드·테스트·배포 파이프라인 | CL-BLD-001 |
| JWT | JSON Web Token. 사용자 인증 정보를 인코딩한 토큰 | SV-SEC-001 |
| BGM | Background Music. 배경 음악 | SND-BGM-001 |
| SFX | Sound Effects. 효과음 | SND-SFX-001 |
| VO | Voice Over. 캐릭터 음성 및 내레이션 | SND-VO-001 |
| DoD | Definition of Done. 작업 완료 판정 기준 | PM-SPR-001 |
| WBS | Work Breakdown Structure. 작업 계층 분류 체계 | PM-SCH-001 |
| `[용어 추가]` | `[정의]` | `[문서 ID]` |

---

## COM-CVT-001: 네이밍 규약

> **원칙**: 공백, 특수문자, 한글 사용 금지. 팀 전체가 동일한 규칙을 사용한다.

### 파일 / 에셋 네이밍

```
[타입약어]_[카테고리]_[상세]_[변형].[확장자]

예시:
  T_Character_Hero_Diffuse.png       (Texture)
  T_Character_Hero_Normal.png        (Normal Map — Linear 색공간 필수)
  SM_Building_House_01.fbx           (Static Mesh)
  SK_Enemy_Goblin.fbx                (Skeletal Mesh)
  A_Explosion_Large.wav              (Audio)
  P_Fire_Torch.prefab                (Prefab)
  UI_Lobby_BtnBattle_Normal.png      (UI 이미지)
  BGM_Battle_Boss.ogg                (BGM)
  VFX_FireBall_01.png                (VFX 텍스처)
```

| 타입 약어 | 의미 |
|----------|------|
| `T_` | Texture |
| `SM_` | Static Mesh |
| `SK_` | Skeletal Mesh |
| `A_` | Audio (SFX) |
| `BGM_` | Background Music |
| `P_` | Prefab |
| `UI_` | UI 이미지 |
| `VFX_` | Visual Effect 텍스처 |
| `AN_` | Animation Clip |
| `IC_` | Icon |
| `SO_` | ScriptableObject |

### C# 코드 네이밍

| 대상 | 규칙 | 예시 |
|------|------|------|
| Class / Struct | PascalCase | `PlayerController`, `GameManager` |
| Interface | I + PascalCase | `IDamageable`, `IPoolable` |
| Method | PascalCase, 동사 시작 | `TakeDamage()`, `CalculateDamage()` |
| Property | PascalCase | `CurrentHp`, `MaxHp` |
| Public Field | PascalCase | `public int MaxHealth;` |
| Private Field | _camelCase | `private int _currentHealth;` |
| Constant | UPPER_SNAKE_CASE | `MAX_PLAYER_COUNT`, `BASE_DAMAGE` |
| Event | On + PascalCase | `OnDamaged`, `OnDeath` |
| Coroutine | Co + PascalCase | `CoAttackDelay()` |
| bool 변수 | is / has / can 접두사 | `isAlive`, `hasBuff`, `canAttack` |
| Enum | PascalCase (항목도 PascalCase) | `CharacterState { Idle, Move, Attack }` |

### 브랜치 네이밍 (Git)

```
feature/[part]-[ticket]-[description]
bugfix/[part]-[ticket]-[description]
release/v[major].[minor].[patch]
hotfix/[part]-[ticket]-[description]

예시:
  feature/cl-JIRA123-add-inventory-ui
  bugfix/sv-JIRA456-fix-login-timeout
  release/v1.2.0
  hotfix/cl-JIRA789-crash-on-battle-start
```

### 문서 파일 네이밍

```
[PartCode]-[DocType]-[Number]_[Title].md

예시:
  GD-COR-001_CoreLoop.md
  ART-CHR-001_CharacterArt.md
  SV-SEC-001_Security.md
```

---

## COM-WFL-001: 워크플로우

### 개발 단계 정의

| 단계 | 기간 | 주요 산출물 | 종료 조건 |
|------|------|------------|----------|
| **Concept** | ~1개월 | 원페이저, 레퍼런스 덱 | 그린라이트 승인 |
| **Pre-Production** | 1~3개월 | Vertical Slice, 주요 GDD | 핵심 재미 검증 완료 |
| **Production** | 6~18개월 | Alpha, Beta 빌드 | Feature Complete |
| **Polish** | 2~4개월 | RC 빌드 | Zero Critical Bug |
| **Live** | 무기한 | 라이브 패치, 업데이트 | — |

> [WARNING] 각 단계 기간은 프로젝트 규모에 따라 조정. 위 수치는 중형 모바일 게임 평균, 10~30인 팀 기준 [근거: 내부 프로젝트 이력 + GDC 2024].

### 에셋 파이프라인

```
[Concept Art]
    ↓
[Modeling / Rigging]  →  [Texture / PBR]  →  [Animation]
    ↓
[Unity Import]  →  [Prefab 조립]  →  [QA 검수]
    ↓
[Perforce / Git LFS 커밋]
```

### 빌드 주기

| 빌드 유형 | 주기 | 목적 |
|----------|------|------|
| **Daily Build** | 매일 새벽 자동 (CI/CD) | 빌드 깨짐 조기 탐지 |
| **Weekly Build** | 주 1회 (금요일) | 내부 QA 테스트 |
| **Milestone Build** | 마일스톤 단위 | 외부 공개, 스테이크홀더 리뷰 |

### 문서 리뷰 사이클

- **주기**: 2주 스프린트마다 담당 파트 문서 검토 및 갱신
- **크로스 파트 리뷰**: 월 1회, 파트 리드 전체 참석
- **대규모 변경**: 영향받는 파트 리드 소집 후 즉시 반영

---

## COM-RSK-001: 리스크 레지스터

> **운영 원칙**: 스프린트마다 리스크 현황 업데이트. 새 리스크 발견 즉시 등록.

| ID | 리스크 내용 | 영향도 | 발생 확률 | 대응 전략 | 담당 | 상태 |
|----|------------|--------|----------|----------|------|------|
| R01 | 핵심 인력 이탈 | High | Medium | 지식 문서화 의무화, 페어 작업, 크로스 트레이닝 | Director | 모니터링 중 |
| R02 | 플랫폼 정책 변경 (스토어 심사 거절 포함) | High | Low | 분기별 가이드라인 리뷰, 여유 일정 확보 | OPS | 모니터링 중 |
| R03 | 기술 스택 호환성 문제 | Medium | Medium | PoC 선행 진행, 대안 기술 스택 사전 조사 | CL Lead | 모니터링 중 |
| R04 | 일정 지연 | High | High | 2주 스프린트 버퍼 적립, MVP 스코프 명확화 | PM | 모니터링 중 |
| R05 | 서버 용량 과부하 (출시 직후) | Very High | Medium | 사전 부하 테스트, 오토 스케일 구성 | SV Lead | 모니터링 중 |
| R06 | 기술 스코프 크리프 (기능 범위 확장) | High | High | 엄격한 변경 관리 프로세스, 디렉터 승인제 | Director | 모니터링 중 |
| `R[N]` | `[리스크 내용]` | `[영향도]` | `[발생 확률]` | `[대응 전략]` | `[담당]` | `[상태]` |

---

## COM-ANA-001: Analytics & Telemetry (분석/텔레메트리)

> **목적**: KPI 측정 및 플레이어 행동 이해. 모든 핵심 이벤트는 출시 전 QA 검증 필수.

### 분석 도구

| 용도 | 도구 | 비고 |
|------|------|------|
| 이벤트 로깅 | `[Firebase / GameAnalytics / 자체 서버]` | 무료 티어 한도 확인 |
| 퍼널 분석 | `[Mixpanel / Amplitude / 자체 BI]` | 리텐션·전환율 추적 |
| 크래시 리포팅 | `[Firebase Crashlytics / Sentry]` | 필수 |

### 필수 이벤트 목록 (공통)

| 이벤트 ID | 이벤트명 | 연동 KPI | 파라미터 |
|----------|---------|---------|---------|
| EVT-001 | `app_install` | UA 캠페인 귀속, 채널별 설치 수 | `source`, `campaign` |
| EVT-002 | `session_start` | DAU, 세션 길이 | `platform`, `version` |
| EVT-003 | `screen_view` | 이탈 화면·튜토리얼 스텝 분석 | `screen_name`, `prev_screen` |
| EVT-004 | `tutorial_complete` | 온보딩 완료율 | `duration_sec` |
| EVT-005 | `level_start` | 스테이지 진입률 | `level_id` |
| EVT-006 | `level_complete` | 클리어율 | `level_id`, `time_sec`, `score` |
| EVT-007 | `level_fail` | 이탈 포인트 분석 | `level_id`, `fail_reason` |
| EVT-008 | `iap_purchase` | Revenue, ARPU | `product_id`, `price_usd` |
| EVT-009 | `ad_impression` | Ad Revenue | `placement`, `ad_type` |
| EVT-010 | `ad_click` | 광고 CTR, 수익 최적화 | `placement`, `ad_type` |
| EVT-011 | `push_notification_open` | 리텐션 캠페인 효율 | `campaign_id`, `notification_type` |
| `[추가]` | `[장르별 이벤트]` | `[연동 KPI]` | `[파라미터]` |

> **[WARNING]** 개인정보보호법(GDPR/COPPA/국내 개인정보보호법) 준수 여부 법무 검토 필수. 미성년자 타겟 게임은 행동 데이터 수집 범위를 제한해야 함 — COM-PVY-001 참조.

---

## COM-PVY-001: Privacy & Compliance (개인정보 보호)

> **이 항목은 법적 의무 사항입니다.** 출시 국가별 법령에 따라 준수 여부를 사전에 법무 검토해야 합니다.

### 적용 법령 체크리스트

| 법령 | 적용 조건 | 주요 요구사항 |
|------|----------|------------|
| GDPR (EU) | EU 사용자 대상 | 동의 수집, 데이터 삭제권, DPA 계약 |
| COPPA (미국) | 13세 미만 타겟 | 행동 데이터 수집 제한, 부모 동의 필수 |
| 국내 개인정보보호법 | 국내 출시 | 개인정보처리방침 게임 내 노출, 파기 정책 |
| PIPL (중국) | 중국 출시 | 데이터 역외 이전 제한, 현지 서버 요건 |

### 게임 내 필수 구현 항목

- **개인정보처리방침**: 게임 최초 실행 시 동의 화면 표시, 메뉴에서 재접근 가능
- **이용약관**: 동의 로그 서버 저장 (날짜, 버전, 디바이스 ID)
- **데이터 수집 범위**: COM-ANA-001 이벤트 목록을 법무팀과 사전 검토
- **광고 식별자(IDFA/GAID)**: iOS 14+ ATT 동의 팝업 구현 필수
- **계정 삭제 기능**: App Store 정책상 필수 (2023년 이후 심사 기준)

### 미성년자 보호

- 13세 미만 타겟 시 행동 기반 광고·추적 데이터 수집 **전면 금지**
- 연령 확인 게이트 구현 여부: `[필요 / 불필요]`
- 과금 기능 제한 여부: `[미성년자 결제 보호 정책 명시]`

---

## 장르 확장 포인트 (COM)

> 공통 문서에서 장르·플랫폼에 따라 추가 또는 변경이 필요한 항목:

| 장르 / 특성 | COM 추가 고려 항목 |
|-----------|-----------------|
| 오프라인 전용 | KPI에서 ARPDAU 제거, 유료 판매 지표(Revenue/DAU, 다운로드 수) 추가 |
| 콘솔 (PS/Xbox/Switch) | COM-OVR-001 플랫폼 항목에 콘솔 추가, 인증 프로세스(TRC/LotCheck/TCR) 리스크 등록 |
| 글로벌 동시 출시 | COM-RSK-001에 현지화 리스크(문화적 민감 콘텐츠), 각국 등급 심의 리스크 추가 |
| 소규모 팀 (5인 이하) | COM-WFL-001 빌드 주기 간소화(Daily → Weekly), 문서 리뷰 주기 조정 |
| IP 기반 게임 | COM-RSK-001에 라이센스 계약 리스크, IP 가이드라인 준수 리스크 추가 |

---

## E 문서 작성 가이드 (COMMON)

> 이 파트의 E 문서(`[ProjCode]_Common.md`)를 작성할 때 아래 기준을 따르세요.

### 필수 포함 섹션

| 섹션 ID | 섹션명 | 완성 기준 |
|--------|--------|---------|
| COM-OVR-001 | 프로젝트 개요 | KPI 수치·근거 명시, 팀 구성·일정 확정 |
| COM-GLS-001 | 용어집 | 프로젝트 전역 용어 최소 10개 이상 등록 |
| COM-CVT-001 | 네이밍 규약 | 파일·코드·브랜치 규약 확정 |
| COM-WFL-001 | 워크플로우 | 개발 단계별 기간·산출물·종료 조건 확정 |
| COM-RSK-001 | 리스크 레지스터 | 최소 5개 리스크 등록, 담당자 지정 |
| COM-ANA-001 | Analytics & Telemetry | 필수 이벤트 11개 연동, 분석 도구 확정 |
| COM-PVY-001 | Privacy & Compliance | 출시 국가별 법령 체크리스트 ☑ 완료 |

### 최소 완성 기준 (Definition of Done)

- [ ] 모든 `[대괄호]` 플레이스홀더 제거됨
- [ ] `[TBD]` 항목에 이유 및 결정 예상 시점 명시
- [ ] 프로젝트 코드명·정식 명칭·플랫폼·엔진 버전 확정
- [ ] KPI 목표 수치에 근거 데이터 출처 명시
- [ ] 전 파트 공유 용어 COM-GLS-001에 선등록 완료

---

## 개정 이력

| 버전 | 날짜 | 작성자 | 변경 내용 |
|------|------|--------|----------|
| 1.0.0 | 2026-04-16 | [작성자] | 초기 템플릿 작성 |
| 1.1.0 | 2026-04-17 | [작성자] | 기반 문서 표시, 장르 확장 포인트 추가 |
| 1.2.0 | 2026-04-18 | [작성자] | E 문서 작성 가이드 (COMMON) 섹션 추가 |
| 1.3.0 | 2026-04-18 | [작성자] | COM-ANA-001 Analytics, COM-PVY-001 Privacy 섹션 추가 |
| 1.x.x | [날짜] | [작성자] | [변경 내용] |

---

*→ 다음 문서: [02_GameDesign.md](./02_GameDesign.md)*
