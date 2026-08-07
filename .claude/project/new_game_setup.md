# 새 게임 시작 체크리스트

이 저장소를 포크·복사해 새 게임을 시작할 때 반드시 완료해야 할 커스터마이징 목록입니다.
**각 항목은 순서대로 완료** — 이전 항목이 확정되지 않으면 다음을 진행하지 마세요.

> **단일 출처 원칙**: 게임별 설계값(해상도·씬 구성·Addressable 주소/라벨)은
> [`constants.md`](constants.md) **한 곳**에서만 수정한다. 규칙·요약 문서는 이 값을 참조한다.
> 다른 스튜디오/네트워크로 옮길 때의 인프라 값(ComfyUI·NAS·SSH)은 [`environment.md`](environment.md) 한 곳에서 수정한다.

---

## 1단계 — 프로젝트 정보 확정

- [ ] **게임 이름** 결정 (Unity Project Name, 저장소명, Bundle ID에 사용)
- [ ] **씬 구성** 결정 (기본 제공: Boot → Title → Lobby → Game → Result)
  - 불필요한 씬은 제거, 추가 씬이 있으면 목록에 추가
- [ ] **주요 게임플레이 이벤트** 목록 확정 (최소: 게임 시작, 종료, 핵심 상태 변화)

---

## 2단계 — CLAUDE.md 커스터마이징 (Claude 컨텍스트 구성)

아래 파일들은 Claude가 이 프로젝트의 구조를 이해하는 핵심 참조입니다.
**게임 특성에 맞게 수정해야 Claude가 올바른 코드를 생성합니다.**

### 2-1. 씬 이름·레이어·태그 — `Assets/Scenes/CLAUDE.md`

수정 항목:
- 씬 파일명 목록 (예: `BootScene.unity`, `GameScene.unity`)
- 사용하는 레이어 목록과 역할
- 사용하는 태그 목록과 역할

### 2-2. 이벤트 정의 — `Assets/Scripts/Module/Events/CLAUDE.md`

수정 항목:
- 게임 흐름 이벤트 struct 정의 (GameStartEvent, GameOverEvent 등)
- 게임플레이 이벤트 struct 정의 (장르에 따라 ScoreChangedEvent, HealthChangedEvent 등)
- 이벤트 정의 후 `.claude/CLAUDE.md`의 주석을 해제해 `@` 인라인 참조로 복원

```
> 이벤트 정의: `Assets/Scripts/Module/Events/CLAUDE.md`
```
→ 위 줄을 `.claude/CLAUDE.md` 참조 정보 섹션에서 `@` 참조로 변경:
```
@Assets/Scripts/Module/Events/CLAUDE.md
```

### 2-3. 모듈 명세 — `Assets/Scripts/Module/CLAUDE.md`

수정 항목:
- 각 씬/기능별 주요 모듈 목록 (파일명, 역할, 의존 인터페이스)
- 모듈 구현 시작 전에 반드시 작성

### 2-4. 네임스페이스·폴더 구조 — `Assets/Scripts/CLAUDE.md`

수정 항목:
- 씬별 모듈 폴더명이 바뀌면 네임스페이스 테이블 갱신
- 폴더 구조 다이어그램 갱신

### 2-5. 프로젝트 요약 — `.claude/project/summary.md`

수정 항목:
- 게임 장르·핵심 루프 한 줄 요약
- 특이한 아키텍처 결정 사항 기록

### 2-6. 게임 설계값 — `.claude/project/constants.md`

수정 항목:
- 게임 식별(게임명·저장소명·Bundle ID)
- 씬 구성, UI 수치(해상도·Panel 높이·sortingOrder)
- Addressable 주소 템플릿·라벨 목록
- **이 파일만 고치면** 규칙·요약 문서가 갱신된 값을 참조한다.

---

## 3단계 — Unity 프로젝트 설정

- [ ] **Bundle Identifier** 변경 (`ProjectSettings/ProjectSettings.asset`)
- [ ] **씬 등록** (`Build Settings > Scenes In Build`): 1단계에서 확정한 씬 목록으로 교체
- [ ] 불필요한 씬 파일 삭제 (`Assets/Scenes/` 하위)
- [ ] **Bootstrap 등록** 확인 (`GameBootstrap.cs`) — 사용할 모듈만 남기고 미사용 모듈 제거

---

## 4단계 — Addressable 그룹 정리

- [ ] `Assets/BundleResource/CLAUDE.md`의 그룹 테이블을 게임에 맞게 수정
  - 불필요한 그룹 삭제, 신규 그룹 추가
- [ ] [`constants.md`](constants.md) 4·5절의 Addressable 주소 템플릿·라벨 갱신
- [ ] 에디터 툴 실행: `Tools > Game > CreateAssetTable` (테이블 초기화)

---

## 5단계 — 보호 대상 확인

`.claude/hooks/protect_framework.ps1`은 `Assets/GameFramework/` 수정을 자동 차단합니다.
이 동작은 변경하지 않는 것을 권장합니다.

게임 전용 코드 작업 영역: `Assets/Scripts/Module/` (자유 수정 가능)

---

## 6단계 — 에이전트 팀 설정

- [ ] `.claude/agents/` 폴더 전체 복사 (`management/`, `client/`, `art/`, `plan/`, `sound/` 모두 그대로)
- [ ] `.claude/agent-memory/` 복사 여부 결정
  - **새 게임 시작**: `agent-memory/` 통째로 복사 권장 (프레임워크 공통 지식 포함, 게임 특화 내용 없음)
  - **메모리 초기화 원할 때만**: 각 에이전트 폴더 내 `MEMORY.md` 삭제
- [ ] `management/wa-manager-pm.md` 팀장 목록 갱신
  - 현재 팀장: client-lead, art-lead, plan-lead, sound-lead (향후 server-lead)
  - 새 팀장이 추가되면 이 목록을 업데이트
- [ ] `management/wa-manager-client-lead.md` aliases 확인
  - 기본값: `"클라"`, `"클라팀"`, `"클라팀장"` — 변경 불필요 시 그대로
- [ ] `.claude/settings.json` 권한 확인
  - `"Write(.claude/agents/**)"`, `"Edit(.claude/agents/**)"` 포함 여부 확인

> **기획·아트·사운드 팀 산출물 경로**: 기획팀·사운드팀은 게임별 산출물을 `Projects/[ProjCode]/` 에 생성한다
> (예: `[ProjCode]_Concept.md`, `[ProjCode]_Content_[콘텐츠명].md`, `[ProjCode]_SND_Design.md`).
> 아트팀 완성 에셋은 클라이언트 팀이 `Assets/BundleResource/` 로 반입·통합한다 (핸드오프: `wa-manager-client-lead` 6-1절).

---

## 7단계 — 아트 파이프라인 (프롬프트 레지스트리)

이미지 생성 에셋의 **프롬프트를 툴 무관하게 영속 저장**하고, **2-티어(초안→최종)** 로 품질을 올리는 스튜디오 표준.
포맷·워크플로우는 [`Template/Prompt_Registry_Template.md`](../../Template/Prompt_Registry_Template.md) 에 고정돼 있어 **새 프로젝트가 그대로 상속**한다.

- [ ] `Template/Prompt_Registry_Template.md` → `Projects/[ProjCode]/[ProjCode]_PromptRegistry.md` 로 복사
- [ ] 레지스트리 §2 **스타일 base 3종**(comfy_positive/negative, gpt_style)만 이 게임 스타일로 교체 (스타일 락)
- [ ] 이후 에셋 생성 시마다 표에 **한 줄씩 기록**(id·subject·seed·워크플로우·경로·tier)

> **생성 도구: ChatGPT 단일 티어** (ComfyUI 미사용). 모든 에셋을 GPT에서 **text2img로 새로 생성**한다.
> final은 프롬프트에서 **새로(text2img) 생성**한다. ⚠️ draft 이미지를 **img2img/edit 레퍼런스로 넣지 않는다**(퀄 상한이 draft에 앵커링됨).
> 세트 일관성은 **final끼리(예: GPT→GPT) 레퍼런스**로 잡는다. 상세: 레지스트리 템플릿 §0~1.

> **subject 1벌 + base 2벌**: 에셋 subject는 한 번만 적고 툴별 base(comfy/gpt)만 분리해 "프롬프트 2벌"을 중복 없이 관리한다.

---

## 완료 기준

위 모든 항목을 완료한 후 Claude에게 아래 메시지로 확인을 요청하세요:

> "새 게임 설정이 완료됐어. Assets/Scenes/CLAUDE.md, Assets/Scripts/Module/CLAUDE.md,
> Assets/Scripts/Module/Events/CLAUDE.md 파일을 읽고 프로젝트 구조를 파악해줘."
