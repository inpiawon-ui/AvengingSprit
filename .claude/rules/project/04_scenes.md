---
description: 씬 구성 규칙(SafeAreaPanel 배치), InGame 규약(Addressable 그룹·GameConfig·캐릭터 로드). 씬 이름·레이어·태그는 Assets/Scenes/CLAUDE.md 참조.
alwaysApply: true
---

# 씬 규칙

> 씬 이름·레이어·태그 등 프로젝트 고유 설정은 아래 파일에서 관리한다.

@Assets/Scenes/CLAUDE.md

---

## 씬 구성 규칙

### UI 프리팹 배치
- 씬에 UI 프리팹을 로드할 경우 반드시 해당 씬의 `SafeAreaPanel` 자식 오브젝트로 배치한다.
- `SafeAreaPanel`은 씬마다 직접 링크로 연결하여 사용한다.

---

## InGame 씬 규약

> 아래 Addressable 주소(`InGameObjects/...`, `TableData/GameConfig`, `host/...`, `ghost/...`)는
> [`.claude/project/constants.md`](../../project/constants.md) 4절(주소 템플릿)을 단일 출처로 한다.
> 본 문서의 주소 표기는 규약 설명용이며 구체 값은 constants.md를 따른다.

### 인게임 오브젝트 Addressable 그룹
- 인게임에서 로드되는 3D 오브젝트 프리팹은 모두 `ingameobjects` 그룹으로 관리한다.
- Addressable 주소: `InGameObjects/{프리팹명}`
- 임의 경로(예: `Stage/`, `Backgrounds/`) 사용 금지.

### 게임 설정 (GameConfig)
- 인게임 공통 수치(속도, 난이도 등)는 각 스크립트에 하드코딩하지 않는다.
- `GameConfig` ScriptableObject 한 곳에서 단일 관리한다.
- 위치: `Assets/BundleResource/TableData/GameConfig.asset`
- Addressable 주소: `TableData/GameConfig`

### 인게임 UI 로드 방식
- 씬 상시 표시 UI: `LoadSceneUIAsync`
- 이벤트로 열리는 오버레이: `UI.Register` (Global Popup, Single)
- 오버레이 UI는 `LoadSceneUIAsync` 사용 금지.

### 호스트 로드

- 호스트 주소 하드코딩 금지.
- `UserDataManager`에서 `SelectedHostId`를 조회 → `HostTable`에서 `hostKey` 필드를 취득 → 주소 결정.
- 주소 형식: `host/{hostKey}`
  - 예: `hostKey = "amazoness"` → 주소 `host/amazoness`
- 호스트 키 12종은 [`constants.md`](../../project/constants.md) 4절을 단일 출처로 한다.

> 고스트(플레이어 본체)는 `ghost/{파일명}` 주소를 사용한다.

### 인게임 오브젝트 프리팹 참조
- 인게임 오브젝트의 하위 구성요소(장애물 등) 직접 참조(Inspector 연결) 금지.
- 반드시 Addressable 주소로 런타임 로드한다.
