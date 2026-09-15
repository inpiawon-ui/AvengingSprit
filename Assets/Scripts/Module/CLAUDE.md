---
description: 이 게임의 모듈 구현 명세 — 새 모듈 추가 시 코드 작성 전에 먼저 이 파일에 기록
---

# 게임 모듈 구현 명세

> **이 파일의 역할**: 이 게임에 어떤 모듈이 있고 각각 무엇을 하는지 기록한다.
> 개발 규칙·네임스페이스·Bootstrap 패턴은 [`Assets/Scripts/CLAUDE.md`](../CLAUDE.md) 참조.

새 모듈을 추가할 때는 코드 작성 전에 이 파일에 먼저 명세를 기록한다.

---

## 작성 방법

```
## [번호]. [모듈 이름]

**파일**: `Assets/Scripts/Module/[씬]/[파일명].cs`

- 역할: [이 모듈이 담당하는 책임]
- 의존: [CoreModule.Get<T>()로 주입받는 인터페이스 목록]
- 핵심 멤버: [주요 메서드/프로퍼티]
- 주의사항: [구현 시 특이 사항, 금지 패턴]
```

---

## 모듈 목록

## 1. ScoreModule

**파일**: `Assets/Scripts/Module/InGame/Score/ScoreModule.cs`

- 역할: 경과 시간을 점수로 환산하고 1초 단위로 `ScoreChangedEvent` 발행. `ITickable` 구현으로 매 프레임 누적.
- 의존: `IEventBus`
- 핵심 멤버: `Score` (현재 점수 읽기 전용), `Tick(float deltaTime)`
- 주의사항: 서비스 인터페이스를 CoreModule에 노출하지 않음. 점수가 필요한 모듈은 `ScoreChangedEvent`·`GameEndEvent`를 구독해 수신.

---

## 2. LobbyModule

**파일**: `Assets/Scripts/Module/Lobby/LobbyModule.cs`

- 역할: `OnSceneLoaded` 이벤트를 수신해 LobbyScene 진입 시점을 감지. 로비 초기화 로직의 진입점.
- 의존: `IEventBus`
- 핵심 멤버: `HandleSceneLoaded(OnSceneLoaded)`
- 주의사항: 씬 이름은 `SceneNames.Lobby` 상수로 참조. 문자열 직접 비교 금지.

---

## 3. NetworkModule (Local 옵션 포함)

**파일**: `Assets/GameFramework/Core/Module/Network/NetworkModule.cs`

- 역할: `INetworkClient`를 CoreModule에 등록. `CoreConfig`에서 `INetworkSettings`를 읽어 실서버 클라이언트(`NetworkClient`)와 로컬 파일 기반 클라이언트(`LocalNetworkClient`) 중 하나를 자동 선택.
- 의존: 없음 (Register()에서 `CoreConfig.TryGet`로 설정 조회 후 분기)
- 등록 방식: `[Module(Layer = ModuleLayer.Core, Provides = new[] { typeof(INetworkClient) })]` — **자동 등록**. GameLauncher에서 수동 호출 불필요.
- 핵심 멤버:
  - `NetworkClient` — UnityWebRequest 기반 실서버 HTTP 구현 (`SendAsync`의 `try/finally`로 `request.Dispose()` 보장)
  - `LocalNetworkClient` (internal) — HTTP 메서드를 파일 CRUD로 매핑한 로컬 구현
  - `JsonFileApiStore` (internal) — `persistentDataPath/local_api/{path}.json` 파일 R/W
- 설정 흐름:

  ```
  ① GameLauncher.RegisterConfigs()
       CoreConfig.Set<INetworkSettings>(_networkConfig)   ← NetworkConfig SO가 구현
  ② NetworkModule.Register()
       CoreConfig.TryGet<INetworkSettings>(out var s)
       → s.UseLocal == true  → LocalNetworkClient
       → s.UseLocal == false → NetworkClient(s.BaseUrl)
       → s == null(미등록)   → NetworkClient("") 기본값
  ```

- Local 모드 동작 규칙:

  | Method | 동작 |
  |--------|------|
  | GET    | 파일 읽기 → 200 / 없으면 404 |
  | POST   | 파일 쓰기 → 201 |
  | PUT    | 파일 쓰기(덮어쓰기) → 200 |
  | DELETE | 파일 삭제 → 204 / 없으면 404 |

- 주의사항:
  - `NetworkConfig` 에셋을 GameLauncher Inspector `_networkConfig` 필드에 연결해야 Local 모드로 동작
  - Local 모드는 서버 측 로직(검증, ID 자동생성 등) 시뮬레이션 없음 — 개발/테스트 단계 전용. 프로덕션 빌드에서는 `UseLocal = false`로 전환
  - DTO는 JsonUtility 제약으로 `public 필드` + `[Serializable]` 사용. 최상위 배열은 `{ "items": [...] }` wrapper 필요
  - 프레임워크 가이드는 [`Assets/GameFramework/Core/Module/Network/CLAUDE.md`](../../GameFramework/Core/Module/Network/CLAUDE.md) 참조

> **참고 — Deprecated 모듈 제거**: 과거 `LocalNetworkModule` 클래스(수동 등록용)는 `NetworkModule` 자동 등록과의 이중 등록 사고를 예방하기 위해 삭제됨. 현재는 `NetworkModule` + `INetworkSettings.UseLocal` 패턴이 유일한 진입점.

---

## 4. LanguageModule (언어팩)

**파일**: `Assets/Scripts/Module/Common/Localization/LanguageModule.cs` (+ 같은 폴더의 `StringTable` · `ILanguageService` · `Localize` · `LocalizedText` · `Language`)

- 역할: 게임 글자를 언어별로 꺼내 준다(한국어 · 일본어 · 영어). 언어가 바뀌면 본문 폰트·재질 프리셋을 갈아 끼우고 `LanguageChangedEvent` 를 발행한다.
- 의존: `IEventBus`, `IResourceManager`
- 등록 방식: `[Module(Layer = ModuleLayer.Game)]` — 자동 등록. `ILanguageService` 를 제공한다.
- 데이터: `Assets/BundleResource/TableData/StringTable.asset` · 주소 `TableData/StringTable` · 라벨 `label_tabledata`
- 핵심 멤버:
  - `Get(key)` / `Format(key, args)` — 화면 글자. 번역이 비면 **한국어 원문**, 키가 없으면 키 자체
  - `FromTable(key, korean)` — 표에서 온 글자. 원문은 표의 `*Kr` 칸(임포터가 덮어씀), 번역만 문자열 표에 둔다
  - `SetLanguage(language)` — 저장(PlayerPrefs `game.language`) · 폰트 교체 · 이벤트 발행
  - `ApplyFonts(root)` — 화면이 생길 때 한 번. `~UI` 의 `Awake` 에서 부른다
  - 정적 창구 `Localize.Get(...)` — `CoreModule.Get` 을 매번 쓰지 않게
- 키 규칙: `ui.{화면}.{이름}` · `host.{hostKey}.name` · `askill.{key}.name/.desc` · `pskill.{key}.name/.desc` · `card.{buffKey}.name/.desc` · `boss.{bossKey}.name` · `event.{eventId}.title/.body/.accept/.decline` · `stage.{chapter}.{fromRoom}.name` · `tip.loading.{번호}` · `opening.line.{전체 컷 순번}`
- 에디터 도구 (`Tools/Game/언어팩/`):
  - `표 글자 → 문자열 표 동기화` — 호스트·스킬·카드·보스·이벤트·무대 이름의 한국어 원문을 모은다. 표 임포터를 돌린 뒤 한 번 더
  - `TSV → 문자열 표 반영` / `문자열 표 → TSV 내보내기` — 번역 작업 파일 `Projects/AVSR/Localization/AVSR_Strings.tsv` (열 `key · ko · ja · en`, 줄바꿈은 `\n`). 빈 칸은 덮어쓰지 않는다
- 주의사항:
  - 프레임워크의 `LocalizationModule` 은 쓰지 않는다 — `Resources` 평면 JSON 만 읽는데, 이 프로젝트는 Addressables 표가 규칙이고 프레임워크는 수정 금지다
  - 일본어를 한글 폰트로 그리면 한자가 한국식 자형으로 나오고 闘·撃·霊·択·険 이 빠진다. 언어마다 `StringTable._fonts` 의 본문 폰트를 쓴다
  - `NotoSansJP SDF` 의 대체 폰트에 `NotoSansKR SDF` 를 걸어 둔다 — 번역이 빈 줄은 한국어 원문으로 떨어지는데, 일본어 폰트엔 한글이 없어 네모로 나온다
  - 재질 프리셋은 이름 꼬리(` - Outline` 등)로 짝을 찾는다. 새 프리셋을 만들면 **양쪽 언어에 같은 꼬리로** 만든다
  - 픽셀 폰트(영문)의 대체 목록을 런타임에 바꾼다 — 에디터에서 에셋이 더럽혀지지 않게 모듈 `Dispose` 에서 되돌린다
  - `LocalizedText` 는 프리팹에 **고정으로 박힌** 글자에만 붙인다. 코드가 `SetText` 로 채우는 칸에 붙이면 두 곳이 같은 칸을 쓴다

---

## API DTO 작성 규칙

**위치**: `Assets/Scripts/Module/Common/Dto/`  
**네임스페이스**: `Game.Module.Common.Dto`

서버·클라이언트가 공유하는 데이터 계약. `INetworkClient`의 제네릭 인자 `T`로 사용한다.  
새 API 엔드포인트를 추가할 때 대응하는 DTO를 이 폴더에 함께 생성한다.

### 작성 규칙

| 규칙 | 이유 |
|------|------|
| `[Serializable]` 어트리뷰트 필수 | JsonUtility 직렬화 조건 |
| `public 필드`만 선언 (프로퍼티 불가) | JsonUtility는 프로퍼티 직렬화 미지원 |
| 최상위 배열 불가 → `items` 필드로 감싸기 | JsonUtility 최상위 배열 직렬화 미지원 |
| `using System;` 추가 | `[Serializable]` 어트리뷰트 소속 |
| `using System.Collections.Generic;` 추가 | `List<T>` 사용 시 |

### 예시 파일

- `UserProfileDto.cs` — `/users/me` 응답, 단순 필드 패턴
- `InventoryDto.cs` — `/users/me/inventory` 응답, `items` wrapper 패턴
