---
description: UI 접미사 용어, Addressable 주소 템플릿·라벨 목록, GameFramework API 요약, 코드 스니펫(아틀라스·캐릭터 로드)
---

# 용어 정의 및 Addressable 주소 템플릿

> ⚠️ **EXAMPLE 주의 (TEMPLATE 모드)**: 아래 표·스니펫의 게임 고유 이름(`LobbyMainUI`, `CharacterSelectPanel`, `char/{...}` 등)은
> 형식 설명용 **예시(EXAMPLE)**다. `MODE: TEMPLATE`에서는 실제 게임 자산으로 간주하지 않는다. (→ [`project_state.md`](project_state.md))

## UI 접미사 용어

| 용어 | 의미 |
|------|------|
| `~UI` | 씬 기본 베이스 UI (씬당 1개, 예: `LobbyMainUI`) |
| `~Panel` | 버튼으로 여는 콘텐츠 창 (예: `CharacterSelectPanel`) |
| `~Popup` | Panel 위에 뜨는 부가 창 (예: `CharacterDetailPopup`) |
| `SystemPopup` | 코드로 생성하는 최상위 알림 (sortingOrder 999) |

## Addressable 주소 템플릿 · 라벨 목록

> 게임별 구체 값(주소 인스턴스·라벨)은 [`constants.md`](constants.md) 4·5절로 이전했다.
> 주소/라벨 **형식 규칙**은 `rules/project/02_addressables.md` 참조.

## GameFramework API 요약

| 기능 | 인터페이스 | 사용 예 |
|------|-----------|--------|
| 씬 전환 | `ISceneManager` | `.LoadAsync(SceneLoadRequest)` |
| 오브젝트 풀 | `IObjectPoolManager` | `.Get<T>()` / `.Return(T)` |
| 이벤트 | `IEventBus` | `.Publish()` / `.Subscribe<T>()` |
| 입력 | `IInputManager` | `.RegisterAction()` / `.OnActionPressed()` |
| 리소스 | `IResourceManager` | Addressables 래퍼 |
| 사운드 | `ISoundManager` | `.Play()` / `.PlayBGM()` |
| UI | `IUIManager` | `.OpenAsync<T>()` / `.CloseAsync<T>()` |
| 저장 | `ISaveable` + `SaveManager` | `Register(this)` |
| 분석 | `IAnalyticsManager` | `.LogEvent()` |

## 코드 스니펫

```csharp
// 아틀라스 로드
var atlas = await Addressables.LoadAssetAsync<SpriteAtlas>("atlas/[파일명]").ToUniTask();
var sprite = atlas.GetSprite("스프라이트명");

// 캐릭터 로드 (하드코딩 금지 — IUserManager 경유)
var userData   = CoreModule.Get<IUserManager>().GetUserData();
var table      = await Addressables.LoadAssetAsync<CharacterTable>("TableData/[테이블명]").ToUniTask();
var charData   = table.GetCharacter(userData.EquippedCharacterId);
var go = await Addressables.LoadAssetAsync<GameObject>($"char/{charData.prefabsname}").ToUniTask();

// 번들 사전 로드 (라벨 사용)
await Addressables.DownloadDependenciesAsync("label_tabledata").ToUniTask();

// 에셋 단일 로드 (주소 사용)
var table = await Addressables.LoadAssetAsync<CharacterTable>("TableData/[테이블명]").ToUniTask();
```
