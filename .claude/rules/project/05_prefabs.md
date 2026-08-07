---
description: 프리팹 생성 규칙 — TextMeshPro, placeholder 이미지, Atlas 자동 생성, RectTransform 기준, ScrollView, PopupParent
alwaysApply: true
---

# 프리팹 생성 규칙

## 기본 원칙
- 씬에 게임오브젝트를 추가로 생성하지 않고 프리팹화한다.
- 에디터 툴로 프리팹을 생성하고 게임에서 로드하는 방식을 기본으로 한다.
- 각각의 기능에 따라 프리팹을 나눠서 생성한다.

## 텍스트
- 씬 관련 프리팹에서 텍스트는 반드시 **TextMeshPro** 컴포넌트를 사용한다.
- `Text (Legacy)` 사용 금지.

## 이미지
- Image 컴포넌트를 추가할 때, **해당 게임오브젝트의 이름과 동일한 이름의 텍스처 파일을 생성**하여 Source Image에 할당한다.
  - 예: 오브젝트명 `PlayerIcon` → `PlayerIcon.png` 생성 후 Source Image에 할당
  - 예: 오브젝트명 `BackgroundPanel` → `BackgroundPanel.png` 생성 후 Source Image에 할당
- 생성된 텍스처 파일은 `Assets/BaseResource/{프리팹이름}/` 경로에 저장한다.
  - 예: `CharacterSelectPanel` 프리팹의 `ThumbnailImage` → `Assets/BaseResource/CharacterSelectPanel/ThumbnailImage.png`
- 이 텍스처는 추후 실제 이미지로 교체하기 위한 placeholder 역할이므로, 파일명을 반드시 오브젝트명과 일치시킨다.

## Atlas 자동 생성
- 프리팹 생성 에디터 툴 실행 시, 텍스처 생성과 함께 **SpriteAtlas를 자동으로 생성**한다.
- Atlas 파일은 `Assets/BundleResource/Atlas/` 경로에 저장한다.
- Atlas 파일명은 **프리팹명과 동일**하게 지정한다.
  - 예: `CharacterSelectPanel` 프리팹 → `Assets/BundleResource/Atlas/CharacterSelectPanel.spriteatlasv2`
- Atlas 생성 규칙:
  1. `Texture2D`(32×32)를 PNG로 `Assets/BaseResource/{프리팹명}/`에 저장
     - 색상은 **GO 이름 문자열의 해시값 기반으로 자동 결정**한다 (같은 이름 = 항상 같은 색)
     - 생성 코드 예시:
       ```csharp
       int hash = goName.GetHashCode();
       Color color = new Color(
           ((hash >> 16) & 0xFF) / 255f,
           ((hash >> 8)  & 0xFF) / 255f,
           ( hash        & 0xFF) / 255f,
           1f
       );
       ```
     - GO마다 다른 색상이 되므로 placeholder 교체 전 미식별 상태를 쉽게 파악할 수 있다.
  2. `TextureImporter`로 TextureType을 `Sprite`로 설정
  3. `SpriteAtlas` 생성 후 `Assets/BaseResource/{프리팹명}/` **폴더 오브젝트** 전체를 PackingSource로 등록
  4. Atlas를 `Assets/BundleResource/Atlas/{프리팹명}.spriteatlasv2`로 저장
  5. Addressable `atlas` 그룹에 자동 등록
- 이미 Atlas 파일이 존재하면 덮어쓰지 않고 유지한다. (텍스처 추가만 반영됨)

## Canvas
- 모든 UI 프리팹에 **Canvas 컴포넌트를 추가하지 않는다.**
- Canvas는 씬의 루트 Canvas를 사용한다.

## UI 해상도
- 기준 해상도는 [`.claude/project/constants.md`](../../project/constants.md) 3절(UI 수치)을 따른다.
- 모든 UI 레이어에 동일하게 적용한다.

## CanvasScaler 설정
- `UI Scale Mode`: **Scale With Screen Size**
- `Reference Resolution`: constants.md 3절의 기준 해상도 값
- `Screen Match Mode`: **Match Width Or Height**
- `Match` 값은 **런타임에 화면 비율에 따라 자동 조정**한다. 고정값으로 설정하지 않는다.
  - 화면 비율 ≥ 1.778 (16:9 이상, 예: 21:9) → `match = 1` (Height 기준, 콘텐츠 좌우 여백)
  - 화면 비율 < 1.778 (16:9 미만, 예: 태블릿 4:3) → `match = 0` (Width 기준, 콘텐츠 상하 여백)
- 이 조정은 `SafeArea.cs` 컴포넌트가 `GetComponentInParent<CanvasScaler>()`로 자동 처리한다.
- 씬에 SafeAreaPanel을 추가할 때 반드시 `SafeArea` 컴포넌트를 부착한다.

## RectTransform 기준

> `~Panel`·`~Popup` 고정 높이(`H`)와 `SystemPopup` sortingOrder 값은
> [`.claude/project/constants.md`](../../project/constants.md) 3절(UI 수치)을 따른다.

| 타입 | Anchor | Width | Height | 비고 |
|------|--------|-------|--------|------|
| `~UI` | Stretch Full (0,0 ~ 1,1) | 0 | 0 | 씬 전체를 채움 |
| `~Panel` | 좌우 Stretch (0,0 ~ 1,1) | 0 | **H** | 상단 HUD가 보이도록 고정 높이 (constants.md) |
| `~Popup` | 좌우 Stretch (0,0 ~ 1,1) | 0 | **H** | Panel과 동일 기준 |
| `SystemPopup` | 별도 Canvas (sortingOrder는 constants.md) | — | — | 코드 생성, 예외 |

- `~Panel`, `~Popup`의 sizeDelta: `(0, H)` — Width는 0(Stretch), Height는 constants.md 값 고정.
- `~UI`의 sizeDelta: `(0, 0)` — 상하좌우 모두 Stretch.

## ScrollView
- 스크롤뷰 생성 시 **재사용 스크롤뷰(오브젝트 풀링)** 를 기본으로 한다 (오브젝트 부하 감소).
- Viewport에 **Image 컴포넌트를 추가하지 않는다** — 이미 있으면 제거한다.
- Viewport에는 **RectMask2D** 만 사용한다.

## PopupParent
- `~Panel` 프리팹 생성 시 최하위 자식 오브젝트로 **`PopupParent`** 를 반드시 생성한다.
- 해당 Panel에서 열리는 `~Popup`은 모두 `PopupParent` 하위에 Instantiate한다.
- `PopupParent`의 RectTransform은 Stretch Full (0,0 ~ 1,1), sizeDelta (0,0).
