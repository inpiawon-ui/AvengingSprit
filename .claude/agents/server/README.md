# wa-server 에이전트 팀

> **Team**: wa-server
> **진입점**: `wa-manager-server-lead`

---

## 개요

게임 서버 백엔드와 클라-서버 계약을 담당하는 AI 에이전트 팀입니다.
클라이언트 팀과 동일한 2-계층 구조(팀장 + 팀원 + 규약 리뷰어)와 검증 파이프라인을 따릅니다.

서버팀은 **서버 백엔드 본체 + 클라-서버 계약 스펙(`SRV_Design.md`)** 을 산출합니다.
클라이언트 측 Unity 통합 구현(범용 `GameFramework/Game/Module/Server`·프로젝트 `Scripts/Module/Server`)은
`wa-manager-server-lead` → `wa-manager-client-lead` → 클라이언트 팀원 흐름으로 처리합니다.
(`protect_framework.ps1`이 `Assets/GameFramework/` 전체를 차단하므로 서버팀은 클라 측 코드를 직접 쓰지 않습니다.)

기술 스택은 **미정/일반론**(언어 무관, 역할 중심)으로 설계되어 있으며, 스택 확정 시 규약을 보강합니다.

---

## 에이전트 목록

| 에이전트 | 직군 | 담당 |
|---------|------|------|
| **wa-manager-server-lead** | 팀장/서버 디렉터 | 요청 분해·위임, 검증 파이프라인, 클라팀 핸드오프 |
| **wa-server-team-api** | API/게임 로직 | REST/gRPC 엔드포인트, 서버 권위 로직(랭킹·상점·인벤토리 등) |
| **wa-server-team-auth** | 인증 | 계정·로그인·토큰/세션·소셜 연동·인가 |
| **wa-server-team-database** | 데이터 | 스키마·영속화·마이그레이션·쿼리 최적화·캐시 |
| **wa-server-team-realtime** | 실시간 | 소켓·룸·매치메이킹·상태 동기화 (단일플레이는 휴면) |
| **wa-server-team-infra** | 인프라/운영 | 배포·CI/CD·컨테이너·모니터링·시크릿 운용 |
| **wa-server-team-security** | 보안 | 치팅 방지·입력 검증·시크릿·암호화 (opus) |
| **wa-server-team-qa** | QA | API 계약·부하·동시성·회귀 검증 (opus) |
| **wa-server-team-scribe** | 서기 | API·스키마·인증 문서, SRV_Design.md 통합 |
| **server-rule-compliance-reviewer** | 규약 리뷰어 | 규약·계약 정적 대조 (opus) |

---

## 실행 방법

```
wa-manager-server-lead를 호출하고 서버 요구사항 또는 게임 설명을 전달합니다.
```

팀장이 요청을 도메인별로 분해하여 api/auth/database/realtime/infra/security 팀원에게 위임하고,
코드/계약 변경 후 QA → security(해당 시) → reviewer 검증 게이트를 거칩니다.

---

## 에이전트 실행 흐름

```
서버 요구사항 / 게임 설명
    ↓
wa-manager-server-lead (요청 분해·위임)
    ├── wa-server-team-api       → 엔드포인트·게임 로직
    ├── wa-server-team-auth      → 인증·세션
    ├── wa-server-team-database  → 스키마·영속화
    ├── wa-server-team-realtime  → 실시간·매치메이킹 (해당 시)
    └── wa-server-team-infra     → 배포·운영
    ↓
품질 게이트: wa-server-team-qa → wa-server-team-security(민감 변경) → server-rule-compliance-reviewer
    ↓ (최대 5회 수정 루프)
wa-manager-server-lead (통합 검수·승인)
    ↓
wa-manager-client-lead (클라 구현 스펙 전달, 필요 시)
    └── wa-client-team-framework / game / network
```

---

## 산출물

```
Projects/[ProjCode]/
└── [ProjCode]_SRV_Design.md
    ├── SRV-API-001   (API 계약·엔드포인트 — api 담당)
    ├── SRV-AUTH-001  (인증 흐름·세션 — auth 담당)
    ├── SRV-DATA-001  (데이터 모델·스키마 — database 담당)
    ├── SRV-RT-001    (실시간 프로토콜 — realtime 담당, 해당 시)
    ├── SRV-INFRA-001 (배포·환경 — infra 담당)
    └── SRV-SEC-001   (보안·위협 모델 — security 담당)

서버 백엔드 본체: 별도 경로 (스택 확정 후 지정 — environment.md 참조)
```

> 서버 코딩 규칙: `.claude/rules/project/server/coding_rules.md`
> 클라 측 통합 폴더: `Assets/Scripts/Module/Server/`(프로젝트), `Assets/GameFramework/Game/Module/Server/`(범용·프레임워크 오너 영역)
