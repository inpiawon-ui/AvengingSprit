# 화면 설계서 — 로비 (Lobby / HUB)

> 기획 **Stage 2b**. 입력: [`AVSR_Content_Lobby.md`](../AVSR_Content_Lobby.md) · 상위 제약: [`AVSR_Decisions.md`](../AVSR_Decisions.md)
> 목업 단일 권위: `Reference/Mockups/lobby_hub.jpeg` (683×1024 · 육안 확인 완료)

- **화면명**: `Lobby` · **소속 콘텐츠**: `진입·플로우 #12` · **UI 타입(최상위)**: `LobbyMainUI` (`~UI`)
- **박스 목업**: `AVSR_Screen_Lobby.html` · **기준 해상도**: `720 × 1280` (9:16 네이티브)
- **Addressable**: `UI/Lobby/LobbyMainUI` · 라벨 `label_lobby` · 아틀라스 `atlas/lobbymainui`
- **Version**: `0.1` · **Last Updated**: `2026-08-07` · **Status**: `게이트 B2 검토 대기`

---

## 0. 화면 인벤토리 · 네비게이션

| ID | 화면 | UI 타입 | 참고: constants.md §2 씬 — **씬 그룹핑은 Stage 3 소관, 여기서 정하지 않음** |
|----|------|---------|------------------------------|
| S1 | Title | `TitleMainUI` (`~UI`) | TitleScene |
| S2 | **Lobby** | `LobbyMainUI` (`~UI`) | LobbyScene |
| S3 | HostSelect | `HostSelectPanel` (`~Panel`) | LobbyScene · **`LobbyMainUI` 직하** |

```
S1 ──탭──▶ S2 Lobby ──┬ ContinueButton ┐
                      ├ ChapterButton  ┼─▶ S3 (Route=ChapterStart)
                      └ HostButton ────────▶ S3 (Route=HostBrowse)
```
`TopHudGroup`은 **S2·S3가 공유**한다 — `LobbyMainUI`가 소유하고, `HostSelectPanel`(높이 1152)이 그 아래 128px부터 시작하므로 항상 노출된다.

## 1. 목적 · 진입/이탈
- 목적: **"지금 뭘 해야 하는지 3초 안에"** 를 보여주는 단일 허브. 정보를 보여주는 곳이지 결정하는 곳이 아니다.
- 진입: S1 Title 탭 / S3 HostSelect 닫기(뒤로가기) · 이탈: S3 HostSelect 오픈 (씬 이동 없음)

## 2. 레이아웃 개요
5단 수직 구성. **① 상단 HUD `0~128`**(고정·공유) → **② 스테이지 연출 영역 `128~700`**(배경·고스트·포탈 + 좌 `ChapterCard` / 우 `SidePromoGroup`)
→ **③ 기능 탭바 `700~808`** → **④ 메인 액션 3버튼 `830~1090`**(로비 최대 인터랙션 면적) → **⑤ 브랜딩 `1104~1272`**.

**9:16 재구성 판단** — 목업 683×1024(2:3)을 폭 기준 ×1.054 하면 높이 1080. 목표 1280과의 차 **+200px을 아래로만 배분**했다.
`HUD 82→128 (+46, 노치 세이프에어리어 흡수)` · `HUD↔카드 사이 배경 노출 +38` · `카드↔탭바 +26` · `액션바 214→260 (+46)` · `로고 영역 88→140 (+52)`.
**어떤 요소도 세로로 늘려 왜곡하지 않았다.** 카드·버튼의 내부 비율은 목업 그대로이며, 늘어난 분은 전부 **블록 간 간격과 `LobbyBackground` 연장**으로 흡수했다.

## 3. 요소 트리 (핵심)

> `이름` = 프리팹 GameObject 이름 = 클라 바인딩 키. 좌표 `(x,y,w,h)`는 `720×1280` 좌상단 원점 px.
> 범위: ✅ 1차 동작 / 🟡 표시만(입력 무반응) / ⛔ 1차 밖(존재·비동작, `SystemPopup` 안내)

| 요소 이름 | UI 타입/컴포넌트 | 위치·anchor (x,y,w,h) | 표시 데이터 | 입력→이벤트 | 범위 |
|-----------|-----------------|----------------------|-------------|-------------|:---:|
| `LobbyMainUI` | **`~UI`** | Stretch Full · sizeDelta `(0,0)` | — | — | ✅ |
| **`LobbyStageArea`** | RectTransform 그룹 | Stretch Full | — | — | ✅ |
| ├ `LobbyBackground` | Image | `(0,0,720,1280)` | 야경 공업도시 · 달 · `JALECO` 네온 · `FACTORY AREA 3` 간판 (1차 정적 1종) | — | ✅ |
| ├ `PortalRing` | Image | center `(260,676,200,56)` | 청색 포탈 링 (저속 회전+명멸) | — | ✅ |
| └ `GhostAvatar` | Image | center `(272,430,176,220)` | 대형 고스트 (상하 부유 루프) · **목업 크롭 ×1** | 없음 | ✅ |
| **`ChapterCard`** | Image 9-slice | top-left `(12,300,222,344)` | — | — | ✅ |
| ├ `ChapterNumberText` | TMP | `(24,312,140,26)` | `CHAPTER 01` (청색) | — | ✅ |
| ├ `ChapterNameText` | TMP | `(24,340,150,44)` | `[TBD]` CH1 챕터명 | — | ✅ |
| ├ `BossLabel` | TMP | `(24,396,60,20)` | `BOSS` (적색) | — | ✅ |
| ├ `BossNameText` | TMP | `(24,418,120,26)` | `[TBD]` CH1 보스명 | — | ✅ |
| ├ `BossPortrait` | Image | `(144,388,84,84)` | 챕터 보스 초상 · **목업 크롭 ×1** | — | ✅ |
| ├ `ProgressLabel` | TMP | `(24,482,90,20)` | `PROGRESS` | — | ✅ |
| ├ `ProgressBarBg` | Image 9-slice | `(24,506,154,20)` | — | — | ✅ |
| │ &nbsp;└ `ProgressBarFill` | Image (Filled) | `(24,506,154,20)` | fill = `clearedStage / 30` | — | ✅ |
| ├ `ProgressText` | TMP | `(30,508,64,16)` | `0 / 30` | — | ✅ |
| ├ `ProgressRewardChest` | Button+Image | `(186,486,42,44)` | 보물상자 | 진행 보상 목록 | ⛔ |
| └ **`ContinueButton`** | Button+Image 9-slice **골드 CTA** | `(24,552,198,74)` | — | → `LobbyStartRequestedEvent{Route=ChapterStart}` | ✅ |
| &nbsp;&nbsp;├ `ContinueButtonText` | TMP | `(34,558,178,36)` | **`START`**(신규) / `CONTINUE`(진행 중) | — | ✅ |
| &nbsp;&nbsp;└ `ContinueCostText` | TMP | `(34,594,178,26)` | `⚡ x5` (비용 예고) | — | ✅ |
| **`SidePromoGroup`** | RectTransform 그룹 | `(470,300,238,344)` | — | — | ⛔ |
| ├ **`BattlePassCard`** | Button+Image 9-slice | `(470,300,238,112)` | — | 배틀패스 | ⛔ |
| │ &nbsp;├ `BattlePassArt` | Image | `(600,308,90,80)` | 시즌 캐릭터 아트 | — | ⛔ |
| │ &nbsp;├ `BattlePassBadge` | Image | `(478,352,40,40)` | 방패 배지 + `Lv` | — | ⛔ |
| │ &nbsp;├ `BattlePassBarBg` | Image 9-slice | `(524,364,120,14)` | — | — | ⛔ |
| │ &nbsp;└ `BattlePassBarFill` | Image (Filled) | `(524,364,120,14)` | `battlePassExp / 100` | — | ⛔ |
| ├ **`EventCard`** | Button+Image 9-slice | `(470,420,238,104)` | `EVENT` · ⏱ 잔여시간 | 이벤트 | ⛔ |
| │ &nbsp;├ `EventArt` | Image | `(618,436,70,64)` | 선물상자 | — | ⛔ |
| │ &nbsp;└ `NotifyBadge` | Image | `(686,416,24,24)` | 적색 `!` | — | ⛔ |
| └ **`DailyLoginCard`** | Button+Image 9-slice | `(470,532,238,112)` | `DAILY LOGIN` · `DAY n` | 일일 로그인 | ⛔ |
| &nbsp;&nbsp;├ `DailyLoginArt` | Image | `(618,548,70,64)` | 보상 상자 | — | ⛔ |
| &nbsp;&nbsp;├ `DailyLoginCheck` ×5 | Image | `(478,600,26,26)` 외 4칸 | 출석 체크 | — | ⛔ |
| &nbsp;&nbsp;└ `NotifyBadge` | Image | `(686,528,24,24)` | 적색 `!` | — | ⛔ |
| **`FeatureTabBar`** | RectTransform 그룹 + HorizontalLayout | `(52,700,616,108)` | — | — | ⛔ |
| ├ `FeatureTabBarBackground` | Image 9-slice | Stretch `(52,700,616,108)` | — | — | ⛔ |
| ├ **`MissionTab`** | Button | `(58,708,116,92)` | — | 미션 | ⛔ |
| │ &nbsp;├ `MissionTabIcon` | Image | `(92,716,48,48)` | 클립보드 | — | ⛔ |
| │ &nbsp;├ `MissionTabLabel` | TMP | `(58,770,116,24)` | `MISSION` | — | ⛔ |
| │ &nbsp;└ `NotifyBadge` | Image | `(158,710,20,20)` | 적색 `!` | — | ⛔ |
| ├ **`AchievementTab`** | Button | `(181,708,116,92)` | — | 업적 | ⛔ |
| │ &nbsp;├ `AchievementTabIcon` | Image | `(215,716,48,48)` | 트로피 | — | ⛔ |
| │ &nbsp;└ `AchievementTabLabel` | TMP | `(181,770,116,24)` | `ACHIEVEMENT` | — | ⛔ |
| ├ **`RankingTab`** | Button | `(304,708,116,92)` | — | 랭킹 | ⛔ |
| │ &nbsp;├ `RankingTabIcon` | Image | `(338,716,48,48)` | 시상대 | — | ⛔ |
| │ &nbsp;└ `RankingTabLabel` | TMP | `(304,770,116,24)` | `RANKING` | — | ⛔ |
| ├ **`InventoryTab`** | Button | `(427,708,116,92)` | — | 인벤토리 | ⛔ |
| │ &nbsp;├ `InventoryTabIcon` | Image | `(461,716,48,48)` | 가방 | — | ⛔ |
| │ &nbsp;└ `InventoryTabLabel` | TMP | `(427,770,116,24)` | `INVENTORY` | — | ⛔ |
| └ **`FriendsTab`** | Button | `(550,708,116,92)` | — | 잠김 안내 | ⛔ |
| &nbsp;&nbsp;├ `FriendsTabIcon` | Image | `(584,716,48,48)` | 인물 2인 | — | ⛔ |
| &nbsp;&nbsp;├ `FriendsTabLabel` | TMP | `(550,770,116,24)` | `FRIENDS` | — | ⛔ |
| &nbsp;&nbsp;└ `FriendsTabLock` | Image | `(604,736,28,28)` | 자물쇠 (**배지 아님**) | — | ⛔ |
| **`MainActionBar`** | RectTransform 그룹 | `(10,830,700,260)` | — | — | ✅ |
| ├ **`HostButton`** | Button+Image 9-slice **퍼플** | `(10,830,222,260)` | — | → `LobbyStartRequestedEvent{Route=HostBrowse}` | ✅ |
| │ &nbsp;├ `HostButtonArt` | Image | `(20,840,202,150)` | 호스트 3인 아트 | — | ✅ |
| │ &nbsp;├ `HostButtonTitleText` | TMP | `(20,996,202,44)` | `HOST` | — | ✅ |
| │ &nbsp;├ `HostButtonSubText` | TMP | `(20,1042,202,26)` | `육성 · ULTIMATE · 도감` | — | ✅ |
| │ &nbsp;└ `NotifyBadge` | Image | `(202,824,24,24)` | 적색 `!` | — | ✅ |
| ├ **`ChapterButton`** | Button+Image 9-slice **골드·발광 테두리** | `(242,830,236,260)` | — | → `LobbyStartRequestedEvent{Route=ChapterStart}` | ✅ |
| │ &nbsp;├ `ChapterButtonArt` | Image | `(252,840,216,150)` | 성채 아트 | — | ✅ |
| │ &nbsp;├ `ChapterButtonTitleText` | TMP | `(252,996,216,44)` | `CHAPTER` | — | ✅ |
| │ &nbsp;└ `ChapterButtonSubText` | TMP | `(252,1042,216,26)` | `게임 시작` | — | ✅ |
| └ **`ShopButton`** | Button+Image 9-slice **블루** | `(488,830,222,260)` | — | 상점 | ⛔ |
| &nbsp;&nbsp;├ `ShopButtonArt` | Image | `(498,840,202,150)` | 보물상자·젬 아트 | — | ⛔ |
| &nbsp;&nbsp;├ `ShopButtonTitleText` | TMP | `(498,996,202,44)` | `SHOP` | — | ⛔ |
| &nbsp;&nbsp;├ `ShopButtonSubText` | TMP | `(498,1042,202,26)` | `상점 · 패키지 · 재화` | — | ⛔ |
| &nbsp;&nbsp;└ `NotifyBadge` | Image | `(680,824,24,24)` | 적색 `!` | — | ⛔ |
| `LogoLockup` | Image | bottom-center `(263,1120,194,102)` | 정본 픽셀 락업 · **목업 크롭 ×1 (재생성 금지 — 계약 IP 로고)** | — | ✅ |
| `VersionText` | TMP | bottom-left `(14,1250,110,22)` | `Application.version` | — | ✅ |
| **`TopHudGroup`** ⭐ | RectTransform 그룹 · **형제 순서 마지막** | top-stretch `(0,0,720,128)` | **S2·S3 공유** | — | ✅ |
| ├ `TopHudBackground` | Image 9-slice | `(0,0,720,128)` | — | — | ✅ |
| ├ `GhostWidget` | Image 9-slice | `(8,18,188,96)` | — | 없음 (비인터랙티브) | ✅ |
| │ &nbsp;├ `GhostPortraitIcon` | Image | `(16,30,60,60)` | 고스트 초상 | — | ✅ |
| │ &nbsp;├ `GhostLabelText` | TMP | `(82,26,104,22)` | `GHOST` | — | ✅ |
| │ &nbsp;├ `GhostLevelText` | TMP | `(82,50,104,24)` | `Lv.1` | — | ✅ |
| │ &nbsp;├ `GhostExpBarBg` | Image 9-slice | `(82,78,106,14)` | — | — | ✅ |
| │ &nbsp;│ &nbsp;└ `GhostExpBarFill` | Image (Filled) **청색** | `(82,78,106,14)` | fill = `ghostExp / ghostExpToNext` | — | ✅ |
| │ &nbsp;└ `GhostExpText` | TMP | `(118,94,72,16)` | `0 / 100` (**EXP** — 확정 #11) | — | ✅ |
| ├ **`StaminaCounter`** | Image 9-slice | `(204,32,126,60)` | — | — | 🟡 |
| │ &nbsp;├ `StaminaIcon` | Image | `(210,44,32,32)` | ⚡ | — | 🟡 |
| │ &nbsp;├ `StaminaText` | TMP | `(246,44,50,32)` | `30/30` | — | 🟡 |
| │ &nbsp;└ `PlusButton` | Button+Image | `(298,46,28,28)` | 골드 원형 `+` | 스태미나 충전 | ⛔ |
| ├ **`GoldCounter`** | Image 9-slice | `(338,32,132,60)` | — | — | 🟡 |
| │ &nbsp;├ `GoldIcon` | Image | `(344,44,32,32)` | 🪙 | — | 🟡 |
| │ &nbsp;├ `GoldText` | TMP | `(380,44,56,32)` | `0` (3자리 콤마) | — | 🟡 |
| │ &nbsp;└ `PlusButton` | Button+Image | `(438,46,28,28)` | 골드 원형 `+` | 상점 재화 탭 | ⛔ |
| ├ **`GemCounter`** | Image 9-slice | `(478,32,112,60)` | — | — | 🟡 |
| │ &nbsp;├ `GemIcon` | Image | `(484,44,32,32)` | 💎 | — | 🟡 |
| │ &nbsp;├ `GemText` | TMP | `(520,44,40,32)` | `0` | — | 🟡 |
| │ &nbsp;└ `PlusButton` | Button+Image | `(558,46,28,28)` | 골드 원형 `+` | 상점 재화 탭 | ⛔ |
| ├ **`MailButton`** | Button+Image | `(600,34,48,56)` | 봉투 | 우편함 | ⛔ |
| │ &nbsp;└ `NotifyBadge` | Image | `(636,28,24,24)` | 적색 `!` | — | ⛔ |
| └ `SettingsButton` | Button+Image | `(658,34,48,56)` | 톱니 | 설정 | ⛔ |

- `TopHudGroup`은 **형제 순서 마지막**에 둔다 — S3 `HostSelectPanel`이 열려도 HUD가 항상 최상단으로 읽히게 보장한다
  (기하학적으로는 겹치지 않으나 `SetAsLastSibling()` 규약(`06_ui.md`)과의 충돌을 원천 차단).
- `PopupParent` **없음** — `~UI`이며, S3는 `PopupParent`가 아닌 `LobbyMainUI` **직하**에 붙는다 (`Content_HostSelect` §3 근거).
- **재사용 요소는 이름을 동일하게 짓는다** — 부모가 다르므로 형제 이름 유일성이 성립하고, 에셋이 1종으로 수렴한다.
  - `NotifyBadge` **6곳** — `MailButton`/`MissionTab`/`EventCard`/`DailyLoginCard`/`HostButton`/`ShopButton`
  - `PlusButton` **3곳** — `StaminaCounter`/`GoldCounter`/`GemCounter`
  - (S3의 `ArrowIcon` 2곳도 동일 원칙)

## 4. 상태·분기

**목업 수치 → 신규 유저 바인딩** (레이아웃 100% 유지 · 값만 치환. 승인 예외 4건에 해당하지 않음)

| 요소 | 목업 표기 | 신규 유저 |
|------|-----------|-----------|
| `GhostLevelText` / `GhostExpText` | `Lv.28` / `56 / 100` | `Lv.1` / `0 / 100` |
| `ChapterNumberText` / `ChapterNameText` | `CHAPTER 03` / `FACTORY` | `CHAPTER 01` / `[TBD]` |
| `ProgressText` | `12 / 30` | `0 / 30` |
| `GoldText` / `GemText` | `125,680` / `4,250` | `0` / `0` |
| `ContinueButtonText` | `CONTINUE` | **`START`** (진행 중이면 `CONTINUE`) — 위치·크기·색 동일 |

- **⛔ 요소**: 목업 그대로 배치·표시한다. 제거·흐림 처리 금지 `[확정 #5]`. 탭 시 `SystemPopup` **`준비 중입니다`** 1버튼.
  단 `FriendsTab`만 **`아직 해금되지 않았습니다`** (자물쇠 = 미해금, 배지 = 알림 — 목업의 시각 구분 유지).
- **로딩/에러**: 진입 시 저장소 인터페이스로 유저 데이터를 **1회 조회 → 전 위젯 일괄 갱신**. 각 위젯이 개별 조회하지 않는다.
  이후 갱신은 `IEventBus` 수신으로만. `Update()`에서 UI를 건드리지 않는다.
- **스태미나 부족**: 로비에서 **검사하지 않는다.** 부족해도 S3 이동 허용. 차단은 S3 `PossessStartButton`에서.

## 5. 연결
- 발행: `LobbyReadyEvent` · `LobbyStartRequestedEvent{LobbyRoute Route}` · `LobbyFeatureBlockedEvent{string BlockedFeatureKey, bool IsLocked}`
- 구독: `OnSceneLoaded` · `CurrencyChangedEvent{CurrencyKind Kind, long NewAmount}` · `GhostLevelChangedEvent{...}` · `ChapterProgressChangedEvent{...}`
- 다른 화면: **여는 화면 = S3 `HostSelectPanel`** (직하 배치, `PopupParent` 아님) · **돌아오는 곳 = S1에서 진입, S3에서 복귀**

## 6. 미정
- `[TBD — 이유: 챕터 콘텐츠 미기획]` `ChapterNameText` · `BossNameText` · `BossPortrait`의 CH1 값. 목업엔 CH3(`FACTORY`/`MAD DOCTOR`)만 노출 → **자리만 확보**하고 폭은 CH3 문자열 기준(`FACTORY` 7자)으로 잡았다. 더 긴 챕터명이 오면 `ChapterNameText` 오토사이징 필요.
- `[TBD — 이유: 경제 밸런스 미착수]` 스태미나 회복 속도·상한.
- `[TBD — 이유: 성장 밸런스 미착수]` `GhostExpText` 분모가 전 레벨 `100` 고정인지.
- `[TBD — 현지화 시]` `준비 중입니다` 문구 최종안.
- `[WARNING]` **HUD 요소명 조정** — `AVSR_Content_Lobby.md`는 `TopHudPanel`, `AVSR_Content_HostSelect.md`는 `TopHudGroup`으로 서로 다르게 표기했다. **`TopHudGroup`으로 통일**한다. `~Panel` 접미사는 `06_ui.md`에서 "버튼으로 여는 콘텐츠 창(고정 높이 1152)"에 예약되어 있어, 상시 노출 HUD에 붙이면 프리팹 타입 오판을 부른다.
- `[WARNING]` **`VersionText` 이름 재사용** — S1 Title과 S2 Lobby 양쪽에 존재한다. 서로 다른 프리팹의 자식이므로 형제 유일성은 성립하며 의미(`Application.version`)도 동일하므로 **의도적 재사용**이다. 클라 바인딩은 프리팹 스코프로 해석할 것.

## 개정 이력
| 버전 | 날짜 | 변경 |
|------|------|------|
| 0.1 | 2026-08-07 | 최초 작성 — 목업 좌표 9:16 재구성, 요소 트리 확정, HUD 명칭 `TopHudGroup` 통일 |
