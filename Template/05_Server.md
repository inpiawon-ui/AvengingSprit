# 05. SERVER (서버)

> **Part Code**: SV
> **Version**: 1.3.0
> **Last Updated**: 2026-04-18
> **Document Owner**: Lead Server Programmer
> **의존성**: COM-OVR-001, GD-SYS-001, GD-ECO-001
> **기반 문서 (A)**: `Unity_GameDev_Template.md` — 5. SERVER 섹션을 상세화한 파트 문서(B)

> 📌 **핵심 원칙**: 클라이언트를 신뢰하지 않는다. 모든 중요 게임 로직은 서버에서 검증.

---

## 📑 목차

- [SV-ARC-001: 아키텍처](#sv-arc-001-아키텍처)
- [SV-NET-001: 네트워크 프로토콜](#sv-net-001-네트워크-프로토콜)
- [SV-API-001: API 설계 규칙](#sv-api-001-api-설계-규칙)
- [SV-DB-001: DB 스키마 가이드](#sv-db-001-db-스키마-가이드)
- [SV-SEC-001: 보안 정책](#sv-sec-001-보안-정책)
- [SV-OPS-001: 운영 / 모니터링](#sv-ops-001-운영--모니터링)

---

## SV-ARC-001: 아키텍처

### 서버 구성 방식 선택

> **팀 규모별 권장 방식**:
> - **5인 이하 / 서버 인력 없음**: BaaS(Backend-as-a-Service) 우선 검토 → PlayFab, Firebase, Supabase
> - **5~20인 / 서버 1~2명**: BaaS + 커스텀 서버 혼합 또는 경량 자체 서버
> - **20인 이상 / 전담 서버팀**: 자체 서버 구축

| 방식 | 도구 예시 | 장점 | 단점 |
|------|---------|------|------|
| BaaS | PlayFab, Firebase, Supabase | 빠른 개발, 운영 부담 최소 | 커스텀 로직 한계, 벤더 종속 리스크 |
| Unity 멀티플레이어 스택 | NGO + Unity Relay + Lobby | Unity 에코시스템 통합, 소규모 팀 적합 | 대규모 CCU 한계 |
| 혼합 | BaaS(인증/DB) + 자체(게임 로직) | 유연성과 개발 속도 균형 | 복잡도 증가 |
| 자체 서버 | 아래 기술 스택 참조 | 완전한 제어권 | 운영 비용·인력 필요 |

#### Unity 멀티플레이어 스택 (Unity 공식 솔루션)

| 컴포넌트 | 패키지 | 용도 |
|--------|-------|------|
| 네트워크 코어 | Netcode for GameObjects (NGO) | 오브젝트 동기화, RPC |
| 릴레이 서버 | Unity Relay | P2P + 서버 부담 없는 호스팅 |
| 룸 관리 | Unity Lobby | 매치 생성, 입장 코드 |
| 고성능 대안 | Mirror / Fish-Net (오픈소스) | 더 많은 CCU, NGO 대체 |

> Unity Relay는 CCU 100명 이하 소규모 멀티에 적합. 대규모 실시간 전투는 자체 Dedicated Server 고려.

- **본 프로젝트 선택**: `[BaaS / Unity 멀티플레이어 스택 / 혼합 / 자체 서버]`

### 서버 기술 스택

| 구성 | 선택 예시 | 근거 |
|------|---------|------|
| 언어 | `[C# (.NET) / Go / Node.js]` | `[팀 역량, 성능 요구]` |
| 프레임워크 | `[ASP.NET Core / Gin / Express]` | — |
| 데이터베이스 | `[MySQL + Redis]` | 관계형 + 캐시 |
| 메시지 큐 | `[RabbitMQ / Kafka]` | 비동기 처리 |
| 인프라 | `[AWS / GCP / Azure]` | 운영 편의 |
| CDN | `[CloudFront / Fastly]` | 정적 에셋 배포 |

### 서비스 분리 구조

```
[Client]
    ↓ HTTPS / WSS
[API Gateway / Load Balancer]
    ├── [Auth Service]         — 로그인, 토큰 발급
    ├── [Game Logic Service]   — 핵심 게임 처리
    ├── [Matchmaking Service]  — 매칭 로직
    ├── [Chat Service]         — 채팅, 알림
    ├── [Shop / Billing]       — 상점, 결제 검증
    └── [Batch Server]         — 리셋, 랭킹, 통계
         ↕
[Redis Cluster]     — 세션, 캐시, 리더보드
[RDB (Primary/Replica)] — 영구 데이터
[Object Storage]    — 에셋, 이미지
[Monitoring Stack]  — 로그, 메트릭, 알림
```

### 서버 용량 기준

> 📌 **TPS(Transactions Per Second)**: 초당 처리 가능한 요청 수. 서버 용량 기준.

| 항목 | 기준값 | 근거 | 스케일 조건 |
|------|--------|------|-----------|
| 목표 DAU | `[N]명` | 사업 목표 기준 | 초과 시 서버 증설 |
| 피크 타임 TPS | `[N] TPS` | DAU × 평균 RPM ÷ 3600 | TPS × 1.5 여유 확보 |
| API 응답 시간 | P95 < 300ms | UX 이탈 임계점 | 초과 시 즉시 최적화 |
| 서버 가용성 | 99.9% (월 44분 이하 다운) | SLA 기준 | 멀티 AZ 구성 |
| DB 용량 | `[N] GB (초기)` | 유저당 예상 × DAU | 분기별 재산정 |

---

## SV-NET-001: 네트워크 프로토콜

### 프로토콜 선택

| 용도 | 프로토콜 | 근거 |
|------|---------|------|
| 로그인, 상점, 인벤토리 | HTTPS + REST | 신뢰성 우선, 재시도 안전 |
| 실시간 전투 | WebSocket / UDP | 지연 최소화 |
| 채팅 / 알림 | WebSocket | 양방향 지속 연결 |
| 파일 다운로드 (에셋) | HTTPS + CDN | 대역폭 최적화 |

### 패킷 설계 원칙

- **모든 패킷에 버전 필드 포함** (하위 호환성 보장)
- **클라이언트 전송 데이터 신뢰 금지**: 좌표, 데미지, 재화 등 서버 재계산
- **패킷 최대 크기**: `[N] KB` 이하 [근거: 모바일 네트워크 환경]
- **재전송 정책**: 중요 패킷은 ACK 확인 후 타임아웃 시 재전송

### 서버 성능 SLA

| 지표 | 목표 | 한계 | 측정 방법 |
|------|------|------|---------|
| API 응답 시간 (P50) | ≤ 50ms | ≤ 100ms | 서버 사이드 로깅 |
| API 응답 시간 (P99) | ≤ 200ms | ≤ 500ms | Grafana/Prometheus |
| 실시간 패킷 지연 (RTT) | ≤ 100ms | ≤ 200ms | 클라이언트 측정 |
| 서버 가용성 | 99.9% | 99.5% | 월간 다운타임 ≤ 43분 |
| 동시접속자 (CCU) | `[목표 CCU]` | `[최대 CCU]` | 부하 테스트 사전 검증 |

> **[WARNING]** SLA 수치는 목표 플랫폼과 장르에 따라 조정 필수. 실시간 PvP는 RTT 한계를 더 엄격하게 설정해야 함.

### Rate Limiting 정책

| 엔드포인트 | 제한 | 초과 시 응답 | 비고 |
|----------|------|------------|------|
| 로그인 | 10회/분/IP | HTTP 429 | 브루트포스 방지 |
| 상점 구매 | 30회/분/유저 | HTTP 429 | 중복 결제 방지 |
| 게임 결과 제출 | 60회/시간/유저 | HTTP 429 | 결과 위조 반복 방지 |
| 일반 API | `[N회/초/유저]` | HTTP 429 | 장르별 조정 |

- **Sliding Window** 알고리즘 권장 (Fixed Window 대비 경계 구간 버스팅 방지)
- 제한 초과 응답에 `Retry-After` 헤더 포함 필수 (클라이언트 재시도 안내)

---

## SV-API-001: API 설계 규칙

### API 명세 템플릿

파일명: `SV-API-[도메인]-[번호]_[기능명].md`

```markdown
## API ID: API_[도메인]_[번호]

**엔드포인트**: `POST /api/v1/[도메인]/[액션]`
**인증 방식**: JWT Bearer Token (헤더: `Authorization: Bearer [token]`)

### 요청 헤더
| 헤더 | 값 | 필수 |
|------|-----|------|
| Content-Type | application/json | ✅ |
| Authorization | Bearer [JWT] | ✅ |

### 요청 바디
```json
{
  "key": "value"
}
```

### 성공 응답 (HTTP 200)
```json
{
  "code": 0,
  "data": { ... }
}
```

### 실패 응답 (HTTP 4xx / 5xx)
```json
{
  "code": [에러코드],
  "message": "[설명]"
}
```

**속도 제한**: `[N] req/min` per user (초과 시 HTTP 429)
**멱등성**: POST 비멱등 / PUT 멱등 / DELETE 멱등
```

### 공통 에러 코드

| 코드 | HTTP 상태 | 의미 | 클라이언트 처리 |
|------|---------|------|--------------|
| 0 | 200 | 성공 | 정상 처리 |
| 1001 | 401 | 인증 토큰 만료 | 재로그인 유도 |
| 1002 | 401 | 유효하지 않은 토큰 | 재로그인 유도 |
| 1003 | 403 | 권한 없음 | 접근 불가 안내 |
| 2001 | 400 | 요청 파라미터 오류 | 파라미터 재확인 |
| 3001 | 404 | 리소스 없음 | 404 안내 |
| 4001 | 429 | 요청 횟수 초과 | retry-after 헤더 기반 재시도 안내 |
| 5001 | 500 | 서버 내부 오류 | 고객센터 안내 + 로그 수집 |
| 5002 | 503 | 서버 점검 중 | 점검 안내 팝업 표시 |

> [WARNING] 클라이언트는 모든 API 에러 코드를 개별 처리해야 합니다. '알 수 없는 오류' 일괄 처리 금지. → 참조: CL-ARC-001

---

## SV-DB-001: DB 스키마 가이드

### 네이밍 규칙

- **테이블명**: `snake_case` 복수형 — 예: `users`, `game_sessions`, `player_items`
- **컬럼명**: `snake_case` — 예: `created_at`, `player_id`, `item_count`
- **PK**: `id` (BIGINT AUTO_INCREMENT) 또는 UUID
- **공통 컬럼**: `created_at`, `updated_at`, `is_deleted` (논리 삭제)
- **외래키**: `[참조테이블_단수]_id` — 예: `user_id`, `item_id`

### 설계 원칙

- **정규화**: 3NF 기본, 성능상 필요 시 선택적 비정규화 (사유 문서화 필수)
- **백업 정책**: 일 1회 풀백업, 시간당 증분 백업
- **샤딩 전략**: 유저 ID 해시 기반
- **인덱스**: 조회 빈도가 높은 컬럼 (`user_id`, `session_id` 등) 인덱스 필수

### 핵심 테이블 구조

| 테이블명 | 주요 컬럼 | 용도 | 비고 |
|---------|---------|------|------|
| `users` | id, uuid, nickname, level, created_at | 유저 기본 정보 | 샤딩 기준 테이블 |
| `player_resources` | user_id, gold, silver, energy, updated_at | 재화 정보 | 트랜잭션 필수 |
| `player_items` | id, user_id, item_id, count, grade, created_at | 보유 아이템 | 인덱스: user_id |
| `game_sessions` | id, user_id, content_id, result, score, duration | 게임 결과 | 분석용 |
| `purchase_history` | id, user_id, product_id, amount, platform, receipt | 결제 내역 | 감사 로그 필수 |
| `rankings` | user_id, score, rank, season_id, updated_at | 리더보드 | Redis 캐시 연동 |

> [WARNING] `purchase_history` 테이블은 **논리 삭제도 금지**. 물리 삭제 절대 불가. 7년 보관 법적 의무 (전자상거래법). → 참조: SV-SEC-001

---

## SV-SEC-001: 보안 정책

> 📌 **JWT(JSON Web Token)**: 사용자 인증 정보를 인코딩한 토큰. 서버 세션리스 인증 방식.

| 위협 | 대응 방법 | 구현 방법 |
|------|---------|---------|
| 인증 탈취 | JWT Access 1시간 / Refresh 30일 | Redis에 Refresh Token 저장 |
| 비밀번호 유출 | bcrypt 해시 (cost factor 12) | 평문 저장 절대 금지 |
| 패킷 변조 | HMAC 서명 + TLS 1.3 | 비HTTPS 요청 리다이렉트 |
| SQL Injection | ORM / Prepared Statement 필수 | Raw Query 금지 |
| 메모리 조작 | 서버 권위 방식 (결과값 서버 계산) | 클라 전송값 신뢰 금지 |
| 계정 탈취 | 2FA, 디바이스 검증 | 신규 디바이스 이메일 인증 |
| 매크로 / 봇 | 행동 패턴 분석 + CAPTCHA | 비정상 패턴 자동 차단 |
| CORS | 화이트리스트 도메인만 허용 | 서버 CORS 설정 |
| 개인정보(PII) | AES-256 암호화 저장 | 법적 요구사항 |
| DDoS | Rate Limiting + CDN 보호 | API Gateway 레벨 설정 |

---

## SV-OPS-001: 운영 / 모니터링

### 모니터링 스택

| 항목 | 도구 | 비고 |
|------|------|------|
| 메트릭 수집 | `[Prometheus / Datadog / CloudWatch]` | CPU, 메모리, TPS, 응답 시간 |
| 시각화 | `[Grafana / Datadog Dashboard]` | 실시간 대시보드 |
| 로그 수집 | `[ELK Stack (Elasticsearch + Logstash + Kibana)]` | 에러 로그 분석 |
| 알림 | `[PagerDuty / Slack + OpsGenie]` | On-call 로테이션 연동 |
| APM | `[Datadog APM / New Relic]` | 트레이싱, 병목 탐지 |

### 알림 임계값

| 모니터링 항목 | 알림 트리거 | 대응 절차 |
|-------------|-----------|---------|
| API P95 응답시간 | 500ms 초과 | Slack 알림 → 원인 분석 |
| 에러율 | 1% 초과 | 긴급 알림 → On-call 호출 |
| 서버 CPU | 80% 이상 5분 지속 | 오토 스케일 or 알림 |
| DB Connection | 최대치의 80% 초과 | Slack 알림 → DBA 검토 |
| 결제 오류율 | 0.5% 초과 | 긴급 알림 → 즉시 조사 |
| DAU 급락 | 전일 대비 20% 이상 감소 | 긴급 알림 → 원인 분석 |

### 배포 전략

| 전략 | 설명 | 적용 시점 |
|------|------|---------|
| **Blue-Green** | 이전 환경 유지하며 신규 환경으로 트래픽 전환 | 메이저 업데이트 |
| **Canary** | 트래픽의 5~10%에만 신규 배포 후 점진 확대 | 기능 배포 |
| **Rolling** | 서버를 순차적으로 교체 | 핫픽스 |

### 장애 대응 (Runbook)

```markdown
## 장애 등급 정의
- P0: 전체 서비스 불가 → 즉시 전파, 15분 내 원인 파악
- P1: 주요 기능 불가 → 30분 내 원인 파악
- P2: 일부 기능 저하 → 2시간 내 원인 파악

## P0 대응 절차
1. 알림 수신 즉시 Slack #incident 채널 생성
2. 온콜 담당자 호출 (PagerDuty)
3. 현황 공유 (영향 범위, 발생 시각, 원인 추정)
4. 조치 실행 (롤백 / 스케일아웃 / 패치)
5. 해소 확인 및 사후 보고서 작성 (24시간 이내)
```

---

## 장르 확장 포인트 (SV)

> 서버 문서에서 장르·특성에 따라 추가 또는 변경이 필요한 항목:

| 장르 / 특성 | SV 추가 고려 항목 |
|-----------|----------------|
| 오프라인 전용 | 서버 섹션 전체 생략. 대신 "로컬 저장소 설계" 문서로 대체. `purchase_history` 등 결제 관련 테이블만 유지 (IAP 연동 시) |
| 실시간 PvP (FPS/격투) | SV-ARC-001에 Dedicated Game Server 구조 추가 (Relay 서버 vs. 권위 서버 선택 기준 명시). SV-NET-001에 UDP 기반 커스텀 프로토콜 설계 |
| 멀티플레이어 (방 기반) | SV-ARC-001에 Room Server + Matchmaking Service 분리 설계. 방 생성/참가/퇴장 API 추가 |
| 소셜 기능 없음 | SV-ARC-001 Chat Service 섹션 생략. MOD-CHAT 미사용 표시 |
| 글로벌 서비스 | SV-ARC-001에 멀티 리전 구성(Primary/Secondary Region), 레이턴시 기반 라우팅 설계 추가 |
| 드롭률 검증 필수 (가챠) | SV-DB-001에 `gacha_log` 테이블 추가 (천장 추적, 규제 대응). SV-SEC-001에 드롭률 서버 계산 필수 명시 |
| 턴제/비동기 멀티 | SV-NET-001 실시간 WebSocket → 비동기 턴 처리(폴링 or Push 알림)로 변경. 서버 부하 대폭 감소 |

---

## E 문서 작성 가이드 (SERVER)

> 이 파트의 E 문서(실제 프로젝트 서버 문서)를 작성할 때 아래 기준을 따르세요.

### 필수 포함 섹션

| 섹션 ID | 섹션명 | 완성 기준 |
|--------|--------|---------|
| SV-ARC-001 | 아키텍처 | 기술 스택 확정, 서비스 분리 구조 다이어그램 확정 |
| SV-NET-001 | 네트워크 프로토콜 | 통신 방식·패킷 포맷 확정 |
| SV-API-001 | API 설계 규칙 | 엔드포인트 네이밍·버전 정책 확정 |
| SV-DB-001 | DB 스키마 가이드 | 핵심 테이블 스키마 확정, 인덱스 전략 명시 |
| SV-SEC-001 | 보안 정책 | 인증·권한·입력 검증 정책 확정 |
| SV-OPS-001 | 운영 / 모니터링 | 알림 기준·배포 절차 확정 |

### 최소 완성 기준 (Definition of Done)

- [ ] 모든 `[대괄호]` 플레이스홀더 제거됨
- [ ] `[TBD]` 항목에 이유 명시 (해결 시점 포함)
- [ ] 크로스 파트 의존 항목 `[WARNING]` 태그 확인
- [ ] SV-DB-001 핵심 테이블 스키마 — ERD 또는 테이블 정의서 첨부
- [ ] SV-SEC-001 보안 체크리스트 — OWASP 대응 항목 전부 ☑ 처리

---

## 개정 이력

| 버전 | 날짜 | 작성자 | 변경 내용 |
|------|------|--------|----------|
| 1.0.0 | 2026-04-16 | [작성자] | 초기 템플릿 작성 |
| 1.1.0 | 2026-04-17 | [작성자] | 기반 문서 표시, 장르 확장 포인트 추가 |
| 1.2.0 | 2026-04-17 | [작성자] | E 문서 작성 가이드 (SERVER) 섹션 추가 |
| 1.3.0 | 2026-04-18 | [작성자] | SV-ARC-001 서버 구성 방식 선택(BaaS/NGO 스택) 추가, SV-NET-001 SLA 표 및 Rate Limiting 정책 추가 |
| 1.x.x | [날짜] | [작성자] | [변경 내용] |

---

*← 이전: [04_Client.md](./04_Client.md) | → 다음: [06_Sound.md](./06_Sound.md)*
