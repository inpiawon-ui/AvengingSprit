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
- `Match` 값은 **런타임에 자동 조정**한다. 고정값으로 설정하지 않는다.

### 규칙은 하나 — 다 들어오는 쪽으로 맞춘다 (contain)

가로·세로 배율을 각각 재고 **작은 쪽**을 고른다. 그러면 기준 해상도의 모든 칸이 화면 안에 남는다.

```
scaleByWidth  = Screen.width  / referenceResolution.x
scaleByHeight = Screen.height / referenceResolution.y

scaleByHeight < scaleByWidth  →  match = 1 (Height 기준, 콘텐츠 좌우 여백)
그 외                          →  match = 0 (Width  기준, 콘텐츠 상하 여백)
```

| 기기 | match | 보이는 칸 | 결과 |
|---|---|---|---|
| 폰 16:9 720×1280 | 0 | 720 × 1280 | 안 잘림 |
| 폰 20:9 1080×2400 | 0 | 720 × 1600 | 안 잘림 (세로 여유) |
| 태블릿 4:3 768×1024 | **1** | 960 × 1280 | 안 잘림 (좌우 여백) |
| 태블릿 16:10 1200×1920 | **1** | 800 × 1280 | 안 잘림 |

> ⚠ **비율 문턱값(1.778 등)으로 가르지 않는다.**
>
> 예전 규약은 「비율 < 1.778 (태블릿 4:3) → match = 0」이었는데 **이대로 하면 화면이 잘린다.**
> 태블릿에서 `match = 0` 은 상하 여백이 아니라 **상하 잘림**이다 —
> 콘텐츠(720×1280)가 화면(768×1024)보다 상대적으로 더 길쭉하기 때문이다.
>
> ```
> 가로에 맞추면  배율 768÷720 = 1.067
>               세로로 보이는 칸 = 1024÷1.067 = 960
>               1280 중 320칸이 아래로 잘려 나간다
> ```
>
> 게다가 구현이 비율을 `width/height` 로 재고 있어서 세로 화면에서는 언제나 0.5625 였다 —
> 문턱을 넘을 수가 없어 **갈림길 자체가 죽어 있었다.** 두 실수가 겹쳐 태블릿에서 아래가 잘렸다.
> 배율을 직접 비교하면 문턱값도 화면 방향도 따질 필요가 없다.

- 이 조정은 `SafeArea.cs` 의 `ApplyCanvasMatch()` 가 `GetComponentInParent<CanvasScaler>()` 로 자동 처리한다.
- 기준 해상도는 **스케일러에서 읽는다.** 코드에 720×1280 을 다시 적지 않는다 — 두 곳에 있으면 한쪽이 낡는다.
- 씬에 SafeAreaPanel을 추가할 때 반드시 `SafeArea` 컴포넌트를 부착한다.

## RectTransform 기준

> `~Panel`·`~Popup` 고정 높이(`H`)와 `SystemPopup` sortingOrder 값은
> [`.claude/project/constants.md`](../../project/constants.md) 3절(UI 수치)을 따른다.

| 타입 | anchorMin | anchorMax | pivot | anchoredPosition | sizeDelta |
|------|-----------|-----------|-------|------------------|-----------|
| `~UI` | `(0, 0)` | `(1, 1)` | `(0.5, 0.5)` | `(0, 0)` | `(0, 0)` |
| `~Panel` | `(0, 1)` | `(1, 1)` | `(0.5, 1)` | `(0, -T)` | `(0, H)` |
| `~Popup` | `(0, 1)` | `(1, 1)` | `(0.5, 1)` | `(0, -T)` | `(0, H)` |
| `SystemPopup` | 별도 Canvas (sortingOrder는 constants.md) | — | — | — | 코드 생성, 예외 |

- **`H`** = `~Panel`·`~Popup` 고정 높이 (constants.md 3절)
- **`T`** = 상단 HUD 오프셋 = `기준 해상도 세로 − H` (constants.md 값 기준: `1280 − 1152 = 128`)

### 왜 상단 앵커인가

`~Panel`·`~Popup`은 **상단 HUD를 가리지 않는 고정 높이 창**이다.
- `anchorMin.y = anchorMax.y = 1` → **세로는 Stretch가 아니라 상단 고정**. 이래야 `sizeDelta.y`가 실제 높이가 된다.
- `anchorMin.x = 0`, `anchorMax.x = 1` → **가로만 Stretch**. `sizeDelta.x = 0`이 곧 화면 폭.
- `pivot.y = 1` → 기준점이 위쪽 변. `anchoredPosition.y = -T` 로 HUD 높이만큼 아래로 내린다.

> ⚠️ **`(0,0) ~ (1,1)`(Stretch Full)에 `sizeDelta (0, H)`를 함께 쓰면 안 된다.**
> Stretch Full에서 `sizeDelta`는 **부모 대비 증감분**이므로 실제 높이가 `화면 높이 + H`(1280 + 1152 = 2432)가 된다.
> `~UI`만 Stretch Full이며, 이때 `sizeDelta`는 반드시 `(0, 0)`이다.

## ScrollView
- 스크롤뷰 생성 시 **재사용 스크롤뷰(오브젝트 풀링)** 를 기본으로 한다 (오브젝트 부하 감소).
- Viewport에 **Image 컴포넌트를 추가하지 않는다** — 이미 있으면 제거한다.
- Viewport에는 **RectMask2D** 만 사용한다.

## PopupParent
- `~Panel` 프리팹 생성 시 최하위 자식 오브젝트로 **`PopupParent`** 를 반드시 생성한다.
- 해당 Panel에서 열리는 `~Popup`은 모두 `PopupParent` 하위에 Instantiate한다.
- `PopupParent`의 RectTransform은 Stretch Full (0,0 ~ 1,1), sizeDelta (0,0).
