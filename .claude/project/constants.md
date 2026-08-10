---
description: 게임별 고유 설계값 단일 출처 — 게임 식별, 씬 구성, UI 수치, Addressable 주소 템플릿·라벨. 새 게임 시작 시 이 파일만 수정하면 규칙·에이전트 파일에 반영된다.
---

# 게임별 설계값 (Constants)

이 파일은 **게임마다 바뀌는 설계값의 단일 출처**다.
다른 규칙·문서 파일은 구체 값을 인라인으로 박지 않고 이 파일을 참조한다.
새 게임을 시작할 때는 **이 파일만 수정**하면 된다. (스튜디오/인프라 값은 [`environment.md`](environment.md) 참조)

> 변경 영향 범위: 이 파일의 값만 고치면 `rules/project/*.md`, `terminology.md`, `summary.md`가 모두 갱신된 값을 참조한다.

> ✅ **활성화됨 (`MODE: GAME`)** → [`project_state.md`](project_state.md)
> 아래 값은 **AVENGING SPIRIT: RE:BORN (AVSR)** 의 실제 설계값이다.
> 확정 컨셉: [`Template/Game_Concept.md`](../../Template/Game_Concept.md)

---

## 1. 게임 식별

| 항목 | 값 |
|------|-----|
| 게임 이름 | `AVENGING SPIRIT: RE:BORN` — ProjCode `AVSR` |
| 저장소명 | `AvengingSprit` |
| Bundle Identifier | `(미정 — ProjectSettings.asset)` |
| 장르 | 방 기반 로그라이크 액션 RPG (탑다운 · 오토어택 · 빙의) |
| 원작 IP | Avenging Spirit (1991, Jaleco / City Connection) — 계약 완료 |
| 화면 방향 | 세로 (Portrait) |

## 2. 씬 구성

```
Boot → Title → Lobby → Game → Result
```

| 단계 | 씬 파일명 | 모듈 폴더 | 1차 범위 |
|------|----------|-----------|---------|
| Boot | `BootScene` | `Module/Boot/` | ✅ |
| Title | `TitleScene` | `Module/Title/` | ✅ |
| Lobby | `LobbyScene` | `Module/Lobby/` | ✅ (호스트 선택 포함) |
| Game | `GameScene` | `Module/InGame/` | ✅ 진입까지만 |
| Result | `ResultScene` (미생성) | `Module/Result/` | ⛔ 다음 마일스톤 |

> 씬 이름·레이어·태그의 **정식 정의**는 `Assets/Scenes/CLAUDE.md`. 위 목록은 설계 기준값이다.
> `SampleScene.unity`는 Unity 기본 생성 씬으로 미사용 — 정리 대상.

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
| UI 프리팹 (Lobby) | `UI/Lobby/{파일명}` | `UI/Lobby/LobbyMainUI`, `UI/Lobby/HostSelectPanel` |
| UI 프리팹 (InGame) | `UI/InGame/{파일명}` | `UI/InGame/InGameMainUI` |
| **호스트 프리팹** | `host/{호스트키}` | `host/amazoness`, `host/rambo` |
| **고스트 프리팹** | `ghost/{파일명}` | `ghost/player` |
| 인게임 오브젝트 | `InGameObjects/{파일명}` | `InGameObjects/Room01` |
| 아틀라스 (화면) | `atlas/{파일명 소문자}` | `atlas/hostselectpanel`, `atlas/lobbymainui` |
| **아틀라스 (캐릭터)** | `atlas/unit_{캐릭터키}` | `atlas/unit_rambo`, `atlas/unit_boss`, `atlas/unit_ghost` |
| 테이블 데이터 | `TableData/{파일명}` | `TableData/HostTable` |
| GameConfig | `TableData/GameConfig` | — |

**호스트 키 12종** (`host/{키}` · 아틀라스·테이블 공통 식별자, 소문자):

`amazoness` · `rambo` · `wizard` · `ninja` · `mafia` · `hitman` ·
`yogamaster` · `dragon` · `robot` · `snowwoman` · `slugger` · `vampire`

**캐릭터 스프라이트 규약** (인게임 몸통 — 초상·아이콘과 별개):

| 항목 | 규약 |
|---|---|
| 텍스처 | `Assets/BaseResource/Unit/{키}/unit_{키}[_{접미}].png` |
| 아틀라스 | `Assets/BundleResource/Atlas/unit_{키}.spriteatlasv2` → `atlas/unit_{키}` |
| 방향 접미 | `s` `se` `e` `ne` `n` (5장). 왼쪽 3방향은 코드가 좌우 반전으로 만든다 |
| 캔버스 | 96×96 고정 · 발밑은 아래에서 8px 위 · **발 중심 x=48** |

> 발 중심이 48이어야 하는 이유: 좌우 반전축이 캔버스 중심이다. 48이 아니면
> 왼쪽을 볼 때 몸이 옆으로 튄다.
>
> 캐릭터 키 = 호스트 키 12종 + `boss` + `ghost`.
> 새 캐릭터는 폴더를 만들고 `Tools > Game > Import Loose Sprites And Repack` 만 돌리면
> 임포터·아틀라스·주소가 자동으로 붙는다.

## 5. Addressable 라벨 목록

> 라벨 네이밍 **규칙**(`label_{그룹명}`, 사전 로드 전용)은 `rules/project/02_addressables.md`.

| 라벨 | 대상 그룹 |
|------|-----------|
| `label_title` | UI/Title 프리팹 |
| `label_lobby` | UI/Lobby 프리팹 |
| `label_ingame` | UI/InGame 프리팹 |
| `label_ingameobject` | InGameObjects 프리팹 |
| `label_host` | 호스트 프리팹 (12종) |
| `label_ghost` | 고스트 프리팹 |
| `label_effect` | 이펙트 (빙의·얼티밋 등) |
| `label_sound` | 사운드 |
| `label_atlas` | 아틀라스 |
| `label_tabledata` | 테이블 데이터 |

## 6. 데이터 저장 · 서버 정책 — ✅ 확정

> **로컬 우선 → 추후 서버 교체.** 테이블 구조와 시스템은 처음부터 최종 형태로 간다.

| 항목 | 값 |
|------|-----|
| **현재 저장 방식** | **로컬 전용** — `DataModule`(Core/Module/Data) 영속화 |
| **서버 전환 시점** | 추후. 게임 로직·테이블 구조 변경 없이 **저장 구간만 교체** |
| 멀티플레이 | **없음** — `wa-server-team-realtime` 휴면 |
| 프로토콜 버전 | `(서버 착수 시 확정)` |
| 예상 엔드포인트 | `(추후 — /auth, /profile, /progress, /battlepass)` |

**필수 설계 제약 (서버 전환 시 재작업 방지):**

1. 유저 데이터 접근은 **반드시 저장소 인터페이스를 경유**한다. 게임 로직이 로컬 파일을 직접 읽지 않는다.
2. 로컬 구현체 → 서버 구현체 **교체만으로 전환**되어야 한다.
3. 마스터 테이블(호스트·얼티밋·버프·챕터)은 **테이블 SO 하나에 배열**로 관리 — 개별 asset 금지.
4. 재화·진행도 갱신 경로는 한 지점에 모은다 (추후 서버 권위 검증 지점이 됨).

> 서버 착수 시 전체 계약은 `Projects/AVSR/AVSR_SRV_Design.md`에 기록한다. 시크릿·토큰 값은 적지 않는다.

> 전체 API 계약·스키마·인증 흐름의 정식 정의는 `Projects/[ProjCode]/[ProjCode]_SRV_Design.md`.
> 위 표는 게임 고유 설계 기준값이며, 시크릿·토큰 값은 적지 않는다.
