---
description: 게임별 고유 설계값 단일 출처 — 게임 식별, 씬 구성, UI 수치, Addressable 주소 템플릿·라벨. 새 게임 시작 시 이 파일만 수정하면 규칙·에이전트 파일에 반영된다.
---

# 게임별 설계값 (Constants)

이 파일은 **게임마다 바뀌는 설계값의 단일 출처**다.
다른 규칙·문서 파일은 구체 값을 인라인으로 박지 않고 이 파일을 참조한다.
새 게임을 시작할 때는 **이 파일만 수정**하면 된다. (스튜디오/인프라 값은 [`environment.md`](environment.md) 참조)

> 변경 영향 범위: 이 파일의 값만 고치면 `rules/project/*.md`, `terminology.md`, `summary.md`가 모두 갱신된 값을 참조한다.

> ⚠️ **EXAMPLE 주의 (TEMPLATE 모드)**: `MODE: TEMPLATE`(→ [`project_state.md`](project_state.md))에서는
> 아래 표의 구체 값(`char/minji`, `pet/rabbit`, `CharacterSelectPanel`, `1280×720` 등)은 모두 **형식 설명용 예시(EXAMPLE)**이며
> 실제 게임 에셋·설계값이 아니다. 새 게임 활성화(`MODE: GAME`) 시 해당 게임 값으로 교체한다.

---

## 1. 게임 식별

| 항목 | 값 |
|------|-----|
| 게임 이름 | `(미정)` — ProjCode `(none)` |
| 저장소명 | `AvengingSprit` |
| Bundle Identifier | `(미정 — ProjectSettings.asset)` |
| 장르 | `(미정)` |

## 2. 씬 구성

기본 제공 흐름 (불필요한 씬 제거, 추가 씬은 목록에 추가):

```
Boot → Title → Lobby → Game → Result
```

| 단계 | 씬 파일명 (예) |
|------|---------------|
| Boot | `BootScene` |
| Title | `TitleScene` |
| Lobby | `LobbyScene` |
| Game | `GameScene` |
| Result | `ResultScene` |

> 씬 이름·레이어·태그의 **정식 정의**는 `Assets/Scenes/CLAUDE.md`. 위 목록은 설계 기준값이다.

## 3. UI 수치

| 항목 | 값 | 적용처 |
|------|-----|--------|
| 기준 해상도 | `720 × 1280` (세로) | 모든 UI 레이어 / CanvasScaler Reference Resolution |
| `~Panel`·`~Popup` 높이 | `1152` | RectTransform sizeDelta `(0, 1152)` — 상단 HUD 노출용 고정 높이 (세로 1280 − 상단 128) |
| `SystemPopup` sortingOrder | `999` | 독립 Canvas, 항상 최상단 |

## 4. Addressable 주소 템플릿

> 주소 형식 **규칙**(`{그룹}/{에셋}`, 소문자 등)은 `rules/project/02_addressables.md`. 아래는 이 게임의 **구체 인스턴스**다.

| 에셋 종류 | 주소 형식 | 예시 |
|-----------|-----------|------|
| UI 프리팹 (Title) | `UI/Title/{파일명}` | `UI/Title/TitleMainUI` |
| UI 프리팹 (Lobby) | `UI/Lobby/{파일명}` | `UI/Lobby/CharacterSelectPanel` |
| UI 프리팹 (InGame) | `UI/InGame/{파일명}` | `UI/InGame/InGameMainUI` |
| 캐릭터 프리팹 | `char/{파일명}` | `char/minji` |
| 펫 프리팹 | `pet/{파일명}` | `pet/rabbit` |
| 인게임 오브젝트 | `InGameObjects/{파일명}` | `InGameObjects/Obstacle` |
| 아틀라스 | `atlas/{파일명 소문자}` | `atlas/characterselect` |
| 테이블 데이터 | `TableData/{파일명}` | `TableData/CharacterTable` |
| GameConfig | `TableData/GameConfig` | — |

## 5. Addressable 라벨 목록

> 라벨 네이밍 **규칙**(`label_{그룹명}`, 사전 로드 전용)은 `rules/project/02_addressables.md`.

| 라벨 | 대상 그룹 |
|------|-----------|
| `label_title` | UI/Title 프리팹 |
| `label_lobby` | UI/Lobby 프리팹 |
| `label_ingame` | UI/InGame 프리팹 |
| `label_ingameobject` | InGameObjects 프리팹 |
| `label_char` | 캐릭터 프리팹 |
| `label_pet` | 펫 프리팹 |
| `label_props` | Props 프리팹 |
| `label_effect` | 이펙트 |
| `label_sound` | 사운드 |
| `label_atlas` | 아틀라스 |
| `label_tabledata` | 테이블 데이터 |

## 6. 서버 API (게임 고유) — 플레이스홀더

이 게임이 사용하는 서버 계약의 게임 고유 값. 서버 계약 확정 시 채운다. (`wa-server-team-api` 관리)
인프라 값(환경별 URL·배포 타겟)은 [`environment.md`](environment.md) 4절을 단일 출처로 한다.

| 항목 | 값 |
|------|-----|
| 프로토콜 버전 | `(미정)` |
| 멀티플레이 여부 | `(미정 — realtime 활성/휴면 결정)` |
| 주요 엔드포인트 | `(미정 — 예: /scores, /ranking, /inventory)` |

> 전체 API 계약·스키마·인증 흐름의 정식 정의는 `Projects/[ProjCode]/[ProjCode]_SRV_Design.md`.
> 위 표는 게임 고유 설계 기준값이며, 시크릿·토큰 값은 적지 않는다.
