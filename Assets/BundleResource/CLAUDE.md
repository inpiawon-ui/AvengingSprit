# Assets/BundleResource — Addressable 번들 구조

이 파일은 `Assets/BundleResource/` 하위 Addressable 그룹 구조의 참조 정보입니다.
Addressable 사용 규칙(라벨 vs 주소 구분, 네이밍 규칙 등)은 `.claude/rules/project/02_addressables.md` 참조.

---

## 배포 전략

> **현재: 빌드 내장(Local)** — CDN 미사용
> CDN 전환 시 각 그룹의 `Build & Load Path`만 `LocalBuildPath` → `RemoteBuildPath`로 교체.
> `RemoteLoadPath`에 CDN URL 입력. 코드·주소·라벨 체계는 변경 없음.

---

## Addressable 그룹 구조

> ※ 그룹 추가·삭제 시 이 파일만 수정한다.

| Group Name | Path | Label | Build Path | Load Path |
|---|---|---|---|---|
| `uiprefabs` > `ui/title` | `Assets/BundleResource/Prefabs/UI/Title` | `label_title` | LocalBuildPath | LocalLoadPath |
| `uiprefabs` > `ui/lobby` | `Assets/BundleResource/Prefabs/UI/Lobby` | `label_lobby` | LocalBuildPath | LocalLoadPath |
| `uiprefabs` > `ui/ingame` | `Assets/BundleResource/Prefabs/UI/InGame` | `label_ingame` | LocalBuildPath | LocalLoadPath |
| `uiprefabs` > `ui/common` | `Assets/BundleResource/Prefabs/UI/Common` | (없음) | LocalBuildPath | LocalLoadPath |
| `ingameobjects` | `Assets/BundleResource/Prefabs/InGameObjects` | `label_ingameobject` | LocalBuildPath | LocalLoadPath |
| `charprefabs` | `Assets/BundleResource/Prefabs/Characters` | `label_char` | LocalBuildPath | LocalLoadPath |
| `petprefabs` | `Assets/BundleResource/Prefabs/Pets` | `label_pet` | LocalBuildPath | LocalLoadPath |
| `props` | `Assets/BundleResource/Prefabs/Props` | `label_props` | LocalBuildPath | LocalLoadPath |
| `effects` | `Assets/BundleResource/Effects` | `label_effect` | LocalBuildPath | LocalLoadPath |
| `sounds` | `Assets/BundleResource/Sounds` | `label_sound` | LocalBuildPath | LocalLoadPath |
| `atlas` | `Assets/BundleResource/Atlas` | `label_atlas` | LocalBuildPath | LocalLoadPath |
| `tabledata` | `Assets/BundleResource/TableData` | `label_tabledata` | LocalBuildPath | LocalLoadPath |

> `ui/common` = 화면 프리팹에 nested로 포함되는 공통 UI 컴포넌트(GaugeBar·CurrencyBadge·CareButton·NavButton·PopupFrame·ItemCell·PetView).
> 주소만 부여하고 **사전로드 라벨은 두지 않는다** — 화면 프리팹 로드 시 의존성으로 자동 포함된다.
> ※ Addressables는 그룹명의 `/`를 `-`로 정규화하므로 실제 그룹 에셋명은 `ui-common`·`ui-lobby`다(주소 `UI/Common/…`·`UI/Lobby/…`는 그대로 유지).

---

## 스캔 및 등록 규칙

| 대상 | 이미 있는 경우 | 없는 경우 |
|---|---|---|
| 프리팹 `.prefab` | 덮어쓰기 | 신규 생성 |
| 테이블 `.asset` | 덮어쓰기 | 신규 생성 |
| Addressable 그룹/엔트리 | 유지 (수정 안 함) | 최초 생성 |

- `tabledata` 그룹 기본 등록 대상: `CharacterTable.asset`, `SkillTable.asset`, `GameConfig.asset`
- 그룹 구조를 임의로 변경하지 않는다.
