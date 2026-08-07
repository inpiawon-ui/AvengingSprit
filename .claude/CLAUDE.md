# CLAUDE.md — 게임 프로젝트 규약

Claude Code가 이 프로젝트에서 작업 할 때 반드시 참조하는 규칙 파일입니다.

---

## ⚠️ 모드 가드 (최우선 — 모든 작업 전 확인)

이 리포는 **모든 게임의 부모(범용 템플릿)**이자, 활성화 시 특정 게임 프로젝트가 된다.
현재 모드는 [`.claude/project/project_state.md`](project/project_state.md)의 `MODE` 값을 **단일 권위**로 한다.

| 현재 MODE | Claude 행동 규칙 |
|-----------|------------------|
| `TEMPLATE` | 이 리포는 **특정 게임이 아니다.** 게임 고유 코드·에셋·기획을 **임의로 생성하지 않는다.** 프레임워크·규칙·에이전트·문서 정비만 수행한다. `constants.md`·`terminology.md`의 게임 고유 값은 **예시(EXAMPLE)**로 취급한다. |
| `GAME` | 이 프로젝트는 `project_state.md`에 기록된 **특정 게임**이다. `constants.md`·`Template/Game_Concept.md`·`Projects/[ProjCode]/`를 게임 컨텍스트로 인지하고 개발한다. |

**활성화 신호** = `Template/Game_Concept.md` 존재 **+** 사용자의 "개발 시작" 지시 (둘 다 필요).
신호가 와도 **`project_state.md`의 `MODE`가 `GAME`으로 기록되기 전까지는 TEMPLATE로 행동**한다.
활성화 절차는 PD(`wa-manager-pd`) 부트스트랩 워크플로우가 담당한다.

> 테스트용 `Game_Concept.md`가 리포에 있어도 마커가 `TEMPLATE`이면 활성화로 간주하지 않는다.

---

## 클로드 구조 (Claude Convention)

```
| 경로 | 역할 |
|---|---|
.claude/                    | 클로드 작업 폴더 |
├── settings.json           | 훅 설정 (protect_framework.ps1) |
├── rules/                  | 클로드 역할별 규칙 |
│   ├── general_rule.md     | 일반 코딩 규칙 |
│   ├── checklist.md        | 자기 점검 프로세스 |
│   ├── coding_conventions.md | C# 네이밍, 패턴, UniTask, null 처리 |
│   └── project/            | 게임 프로젝트 특화 규칙 |
│       ├── 01_assets.md    | 폴더 구조 (Game, BundleResource) |
│       ├── 02_addressables.md | Addressable 설정 |
│       ├── 03_editor_tools.md | 에디터 툴 사용 규칙 |
│       ├── 04_scenes.md    | 씬 구성 규칙 및 InGame 규약 (씬 이름·레이어는 Assets/Scenes/CLAUDE.md) |
│       ├── 05_prefabs.md   | 프리팹 생성 규칙 |
│       ├── 06_ui.md        | UI 타입, 레이어, 팝업 |
│       ├── 07_framework_rules.md | GameFramework 사용 규칙 |
│       └── module/         | 게임 모듈 전용 규칙 |
│           └── coding_rules.md | 스크립트 위치, 네임스페이스 |
├── hooks/                  | PreToolUse 훅 스크립트 |
│   └── protect_framework.ps1 | GameFramework 수정 차단 |
├── commands/               | 커스텀 슬래시 명령 (게임별 추가 가능) |
├── skills/                 | 재사용 가능한 작업 스킬 (게임별 추가 가능) |
├── agents/                 | 에이전트 팀 (2-계층 구조) |
│   ├── management/         | PM, PD, 모든 팀장 (client-lead, art-lead, plan-lead, sound-lead, server-lead) |
│   ├── client/             | 클라이언트 팀원 7명 (framework/game/ui/network/tools/qa/scribe) |
│   ├── art/                | 아트 팀원 6명 (comfyui/file/concept/ui/character/bg) — 작업 공간: Projects/[ProjCode]/ |
│   ├── plan/               | 기획 팀원 10명 — 게임 기획 문서 자동 생성 — 작업 공간: Projects/[ProjCode]/ |
│   ├── sound/              | 사운드 팀원 3명 (bgm/sfx/voice) — 사운드 방향·스펙 기획 — 작업 공간: Projects/[ProjCode]/ |
│   └── server/             | 서버 팀원 8명 (api/auth/database/realtime/infra/security/qa/scribe) + 규약 리뷰어 — 백엔드·클라-서버 계약 — 산출물: Projects/[ProjCode]/[ProjCode]_SRV_Design.md |
└── project/                | 프로젝트 참조 정보 |
    ├── constants.md        | 게임별 설계값 단일 출처 (해상도·씬·Addressable 주소/라벨) |
    ├── environment.md      | 스튜디오/인프라값 단일 출처 (ComfyUI·NAS·SSH) |
    ├── new_game_setup.md   | 새 게임 시작 커스터마이징 체크리스트 |
    ├── summary.md          | 프로젝트 요약 |
    ├── terminology.md      | 용어 정의 (구체 주소/라벨은 constants.md) |
    └── module/             | 게임 모듈 전용 참조 |
        └── lifecycle_eval.md | 모듈 라이프사이클 테스트 평가 기준 |

프로젝트 고유 설정 (씬 구조, 레이어/태그, 이벤트, 모듈 명세) 위치:
Assets/Scenes/CLAUDE.md                  | 씬 이름·레이어·태그
Assets/Scripts/Module/Events/CLAUDE.md   | IEvent struct 정의
Assets/Scripts/Module/CLAUDE.md          | 모듈별 구현 명세
```

---

## 코드 레이어 구조 (3-tier)

이 프로젝트는 3개 레이어로 코드를 분리한다.

| 레이어 | 책임 | 수정 권한 |
|---|---|---|
| `Assets/GameFramework/Core/` | 엔진·프로젝트 형태(앱·게임)·환경(모바일·PC)에 **무관한 핵심 시스템** | 프레임워크 오너만 |
| `Assets/GameFramework/Game/` | **어떤 게임 개발에도 필요한 범용 기능·도구** | 프레임워크 오너만 |
| `Assets/Scripts/` | **이 특정 게임/프로젝트 고유 코드** | Claude 자유 작업 |

폴더 이름은 `Assets/Scripts/`로 바꾸었지만, 코드 네임스페이스는 의도적으로 `Game.*`을 유지한다.
프레임워크의 `GameFramework.Game.*`(재사용 범용 레이어)와 명확히 구분된다.

---

## 프로젝트 규칙

> 아래 파일들은 `alwaysApply: true`로 자동 로드된다. 별도 `@` 참조 불필요.

- `.claude/rules/project/01_assets.md` — 폴더 구조
- `.claude/rules/project/02_addressables.md` — Addressable 설정
- `.claude/rules/project/03_editor_tools.md` — 에디터 툴 사용 규칙
- `.claude/rules/project/04_scenes.md` — 씬 구성 규칙 및 InGame 규약
- `.claude/rules/project/05_prefabs.md` — 프리팹 생성 규칙
- `.claude/rules/project/06_ui.md` — UI 타입, 레이어, 팝업
- `.claude/rules/project/07_framework_rules.md` — GameFramework 사용 규칙
- `.claude/rules/project/module/coding_rules.md` — 스크립트 위치, 네임스페이스

---

## 개발 규칙

> 아래 파일들은 `alwaysApply: true`로 자동 로드된다. 별도 `@` 참조 불필요.

- `.claude/rules/general_rule.md` — 일반 코딩 규칙
- `.claude/rules/checklist.md` — 자기 점검 프로세스
- `.claude/rules/coding_conventions.md` — C# 네이밍, MonoBehaviour 패턴, UniTask, null 처리
- `.claude/rules/unity_rule.md` — Unity 파일 관리 규칙 (.meta 동반 삭제 등)
- `.claude/rules/coding_summary_rule.md` — 실수 패턴 요약 (PowerShell 인코딩 등) — 주석 작성 전 반드시 참조

---

## 참조 정보

@.claude/project/project_state.md
@.claude/project/summary.md
@.claude/project/terminology.md
@.claude/project/constants.md

> 스튜디오/인프라값(ComfyUI·NAS·SSH): `.claude/project/environment.md` — 자동 로드 제외, 인프라 작업 시 참조
> **새 게임 시작 시**: `.claude/project/new_game_setup.md` — 커스터마이징 단계별 체크리스트
> 모듈 구현 명세: `Assets/Scripts/Module/CLAUDE.md` — 자동 로드 제외, 관련 작업 시 Claude가 참조
@Assets/Scripts/Module/Events/CLAUDE.md
> 경로·폴더 구조: 각 폴더 하위 `CLAUDE.md` 참조 (`Assets/Scripts/`, `Assets/BundleResource/`, `Assets/BaseResource/`)

---

## 코드 스타일

프로젝트 고유 스타일 오버라이드만 여기에 기록한다.
공통 규칙은 글로벌 CLAUDE.md와 `.claude/rules/general_rule.md`를 따른다.

(현재 프로젝트 고유 오버라이드 없음)
