# 콘텐츠 세부 기획서 — 로비 (HUB)

> 기획 **Stage 2**. 상위: [`AVSR_GameComposition.md`](AVSR_GameComposition.md) #12 · 제약: [`AVSR_Decisions.md`](AVSR_Decisions.md)
> 목업 단일 권위: `Reference/Mockups/lobby_hub.jpeg` (육안 판독 완료)

- **콘텐츠명**: `로비 (HUB)` · **카테고리**: `진입·플로우` · **우선순위**: `P0`
- **Version**: `0.1` · **Last Updated**: `2026-08-07` · **Status**: `게이트 B 검토 대기`
- **UI 타입**: `LobbyMainUI` (`~UI` · LobbyScene당 1개) — [`06_ui.md`](../../.claude/rules/project/06_ui.md)

---

## 1. 목적 (플레이어 경험)

앱을 켰을 때 **"지금 뭘 해야 하는지 3초 안에 알게 하는"** 단일 허브.
포탈 위에 떠 있는 고스트의 정적 화면으로 **1991 원작의 음습한 아케이드 정서**를 먼저 각인시키고,
좌측 챕터 카드와 골드 `CONTINUE` 버튼으로 시선을 **"이어서 하기"** 한 곳에 몰아준다.
로비는 **정보를 보여주는 곳이지 결정하는 곳이 아니다.** 결정(호스트 선택)은 다음 화면에서 한다.

---

## 2. 화면 구성 (UI 요소)

> 목업 100% 재현. 아래 요소명 = Stage 2b 요소 트리 = 프리팹 GameObject 이름 = 클라 바인딩 키.
> `1차` 열: ✅ 동작 구현 / 🟡 표시만 / ⛔ 버튼 존재·비동작

### 2-1. 상단 HUD (`TopHudPanel` · 화면 상단 고정 128px [근거: constants.md 3절 `~Panel` 높이 1152 = 1280−128])

| # | 요소 | 표시 데이터 | 누르면 | 1차 |
|---|------|------------|--------|:---:|
| 1 | `GhostWidget` | 고스트 초상 · `GHOST` · `Lv.28` · 청색 게이지 + `56 / 100` | 없음 (비인터랙티브) | ✅ |
| 2 | `StaminaCounter` | ⚡ 아이콘 · `30/30` | `+` → 스태미나 충전 | 🟡 표시 |
| 3 | `GoldCounter` | 금화 아이콘 · `125,680` (3자리 콤마) | `+` → 상점 재화 탭 | 🟡 표시 |
| 4 | `GemCounter` | 젬 아이콘 · `4,250` | `+` → 상점 재화 탭 | 🟡 표시 |
| 5 | `MailButton` | 봉투 아이콘 + 적색 `!` 배지 | 우편함 | ⛔ |
| 6 | `SettingsButton` | 톱니 아이콘 (배지 없음) | 설정 팝업 | ⛔ |

> `GhostWidget`의 `56 / 100` = **EXP**다 (`Lv + EXP`). [확정 #11 — 아웃게임은 `GHOST HP` 아님]
> 게이지가 청색(EXP)이고 적색(HP)이 아닌 점이 목업에서도 이를 뒷받침한다. → Decisions §8 `GHOST HP 스케일` 미결의 **아웃게임 측은 이로써 해소**된다.

### 2-2. 중앙 (`LobbyStageArea` — UI 아님, 배경·연출 레이어)

| # | 요소 | 표시 데이터 | 누르면 | 1차 |
|---|------|------------|--------|:---:|
| 7 | `LobbyBackground` | 야간 공업도시 · 달 · `JALECO` 네온 · `FACTORY AREA 3` 간판 | — | ✅ |
| 8 | `GhostAvatar` | 대형 고스트 스프라이트 (부유 루프) | 없음 (비인터랙티브) | ✅ |
| 9 | `PortalRing` | 고스트 발밑 청색 원형 포탈 링 | — | ✅ |

> `LobbyBackground`의 `FACTORY AREA 3` 간판은 **현재 챕터와 연동되는 장식**이다 [도출: 좌측 카드 `CHAPTER 03 / FACTORY`와 동일 문구]. 1차에서는 **정적 이미지 1종**으로 두고, 챕터별 교체는 다음 마일스톤.

### 2-3. 좌측 챕터 카드 (`ChapterCard`)

| # | 요소 | 표시 데이터 | 누르면 | 1차 |
|---|------|------------|--------|:---:|
| 10 | `ChapterNumberText` | `CHAPTER 03` (청색 소문자 라벨체) | — | ✅ |
| 11 | `ChapterNameText` | `FACTORY` (백색 대형) | — | ✅ |
| 12 | `BossLabel` / `BossNameText` | `BOSS`(적색) / `MAD DOCTOR`(백색) | — | ✅ |
| 13 | `BossPortrait` | 챕터 보스 초상 (매드 닥터) | — | ✅ |
| 14 | `ProgressBar` / `ProgressText` | `PROGRESS` · `12 / 30` + 게이지 | — | ✅ |
| 15 | `ProgressRewardChest` | 보물상자 아이콘 (진행 보상 예고) | 진행 보상 목록 | ⛔ |
| 16 | `ContinueButton` | `CONTINUE` (골드 CTA) + `⚡ x5` | **호스트 선택 → 게임시작 모드** | ✅ |

### 2-4. 우측 카드 3종 (`SidePromoGroup`)

| # | 요소 | 표시 데이터 | 누르면 | 1차 |
|---|------|------------|--------|:---:|
| 17 | `BattlePassCard` | `BATTLE PASS` · `SEASON 1` · 방패 배지 `12` · 게이지 `45 / 100` · 시즌 캐릭터 아트 | 배틀패스 | ⛔ |
| 18 | `EventCard` | `EVENT` · ⏱ `6D 18H` · 선물상자 아트 · 적색 `!` 배지 | 이벤트 | ⛔ |
| 19 | `DailyLoginCard` | `DAILY LOGIN` · 체크 4개 + 진행중 1 · `DAY 5` · 상자 아트 · 적색 `!` 배지 | 일일 로그인 | ⛔ |

> 시즌명은 `GHOST REVENGE`(배틀패스 시즌1 명칭)를 쓴다. [확정 — `RE:BORN`은 정식 부제이므로 시즌명으로 쓰지 않는다]

### 2-5. 하단 탭 5종 (`FeatureTabBar` — 아이콘 + 라벨, 배경 프레임 1장)

| # | 요소 | 배지 | 누르면 | 1차 |
|---|------|------|--------|:---:|
| 20 | `MissionTab` | 적색 `!` | 미션 | ⛔ |
| 21 | `AchievementTab` | 없음 | 업적 | ⛔ |
| 22 | `RankingTab` | 없음 | 랭킹 | ⛔ |
| 23 | `InventoryTab` | 없음 | 인벤토리 | ⛔ |
| 24 | `FriendsTab` | **자물쇠 오버레이** (아이콘 위) | 잠김 안내 | ⛔ |

> `FriendsTab`만 배지가 아니라 **자물쇠**다. 목업이 "미해금 기능"과 "알림 있음"을 시각적으로 구분하고 있으므로 이 구분을 그대로 유지한다.

### 2-6. 최하단 3버튼 (`MainActionBar` — 로비에서 가장 큰 인터랙션 면적)

| # | 요소 | 색 | 표시 | 배지 | 누르면 | 1차 |
|---|------|-----|------|------|--------|:---:|
| 25 | `HostButton` | 퍼플 | 호스트 3인 아트 · `HOST` · `육성 · ULTIMATE · 도감` | 적색 `!` | **호스트 선택 → 조회·강화 모드** | ✅ |
| 26 | `ChapterButton` | **골드 (강조·발광 테두리)** | 성채 배경 아트 · `CHAPTER` · `게임 시작` | 없음 | **호스트 선택 → 게임시작 모드** | ✅ |
| 27 | `ShopButton` | 블루 | 보물상자·젬 아트 · `SHOP` · `상점 · 패키지 · 재화` | 적색 `!` | 상점 | ⛔ |

> 3버튼 중 `ChapterButton`만 발광 테두리로 시각 우선순위 1위다. 목업 그대로 유지한다.
> 호스트 선택은 **화면 1개, 진입 경로 2개**다 [확정]. `ContinueButton`·`ChapterButton`은 **동일 경로(게임시작)** 이므로 경로 수는 여전히 2개다.

### 2-7. 브랜딩

| # | 요소 | 표시 | 1차 |
|---|------|------|:---:|
| 28 | `LogoLockup` | `Avenging`(적색 필기) / `SPIRIT`(청색 비트맵) / `RE:BORN`(적색) | ✅ |
| 29 | `VersionText` | 좌하단 `v1.0.0` (`Application.version` 바인딩) | ✅ |

---

## 3. 기능·규칙 (동작)

### 3-1. 진입·표시
- 로비 진입 시 **저장소 인터페이스**로 유저 데이터 1회 조회 → 전 위젯 일괄 갱신. 각 위젯이 개별로 저장소를 읽지 않는다 [확정 §6-1].
- 재화·진행도 변경은 **`IEventBus` 이벤트 수신으로만** 갱신한다. 폴링·매 프레임 갱신 금지.
- 로비는 `Update()`에서 UI를 건드리지 않는다. 갱신은 전부 이벤트 구동.

### 3-2. 게임 시작 흐름 (✅ 1차 범위 — 이 콘텐츠의 유일한 실동작 경로)
```
ContinueButton / ChapterButton  → LobbyStartRequestedEvent { Route = ChapterStart }
HostButton                      → LobbyStartRequestedEvent { Route = HostManage }
        ↓
HostSelectPanel 오픈 (~Panel · PopupParent 상위 아님, LobbyMainUI 직하)
```
- **스태미나 검사는 로비에서 하지 않는다.** 실제 차감·검사는 호스트 선택 화면의 빙의 시작 시점이다 [도출: `CONTINUE`의 `⚡ x5`는 **비용 예고 표기**이고, 호스트를 고르기 전에는 런이 시작되지 않는다].
- 스태미나 부족 시에도 로비→호스트 선택 이동은 허용한다. 차단은 호스트 선택 문서에서 정의한다.

### 3-3. 1차 범위 밖 요소의 동작 규칙 (⛔)
- 버튼·카드·탭은 **목업 그대로 배치·표시**한다. 제거하거나 비활성 색으로 흐리지 않는다 [확정 #5 목업 100% 재현].
- 누르면 `SystemPopup`으로 **`준비 중입니다`** 1버튼 안내를 띄운다. 화면 전환·씬 이동 없음.
- 단 `FriendsTab`은 자물쇠 요소이므로 **`아직 해금되지 않았습니다`** 로 문구를 구분한다.
- 배지(`!`)는 1차에서 **정적 표시**다. 실제 알림 개수 연동은 다음 마일스톤.

### 3-4. 목업 표기 수치의 취급 ⚠️
목업의 `Lv.28` · `12 / 30` · `CHAPTER 03` · `125,680` · `4,250` · `SEASON 1 Lv.12` · `DAY 5`는
**레이아웃 검증용 샘플 데이터**이며 신규 유저 초기값이 아니다. 레이아웃은 100% 재현하되 값은 바인딩한다.

| 항목 | 목업 표기 | 신규 유저 초기값 |
|------|-----------|-----------------|
| 고스트 | `Lv.28` · `56 / 100` | `Lv.1` · `0 / 100` |
| 챕터 카드 | `CHAPTER 03 / FACTORY` | `CHAPTER 01 / [TBD — 이유: CH1·CH2 챕터명·보스명 미정]` |
| 진행도 | `12 / 30` | `0 / 30` |
| 스태미나 | `30/30` | `30/30` (동일) |
| 골드 / 젬 | `125,680` / `4,250` | `0` / `0` |

> 이 표는 목업 **변경이 아니라 데이터 바인딩**이므로 승인된 예외 4건에 해당하지 않는다.

---

## 4. 데이터 (값 · 저장 항목)

| 데이터 | 타입/범위 | 저장 | 비고 |
|--------|-----------|:---:|------|
| `saveVersion` | `int` | O | 필수 [확정 §6-5] |
| `ghostLevel` | `int` ≥ 1 | O | 위젯 `Lv.n` |
| `ghostExp` | `int` ≥ 0 | O | 위젯 분자 |
| `ghostExpToNext` | `int` | X | `GhostLevelTable`에서 조회 (분모 `100`) |
| `stamina` | `int` 0~`staminaMax` | O | 회복 계산용 `lastStaminaTickUtc` 동반 저장 |
| `staminaMax` | `int` `30` | X | `GameConfig` [근거: 목업 `30/30`] |
| `staminaPerRun` | `int` `5` | X | `GameConfig` [근거: 목업 `⚡ x5` → 만충 6런] |
| `staminaRecoverSeconds` | `int` | X | `[TBD — 이유: 경제 밸런스 미착수. Decisions §8 이월]` |
| `gold` / `gem` | `long` ≥ 0 | O | 콤마 포맷 표시 |
| `currentChapter` | `int` 1~3 | O | 카드 `CHAPTER 0n` |
| `clearedStageInChapter` | `int` 0~30 | O | 진행도 분자 |
| `chapterStageTotal` | `int` `30` | X | `ChapterTable` [근거: 확정 §1 STAGE 30/챕터] |
| `unlockedHostKeys` | — | **X** | **저장 금지.** progress로 매번 평가 [확정 §6-5] |
| `lastLoginDay` | `int` | O | `DAY 5` 표시용 (⛔ 1차 밖, 필드만 예약) |
| `battlePassLevel` / `battlePassExp` | `int` | O | ⛔ 1차 밖, 필드만 예약 |

> 마스터 테이블(`ChapterTable`·`GhostLevelTable`)은 **SO 하나에 배열** [확정 §6-3].

---

## 5. 연출 · 사운드

| 대상 | 요구 |
|------|------|
| `GhostAvatar` | 상하 부유 루프. 진폭 소폭 · 주기 `[TBD — 이유: 아트 검수 시 결정]`. **Point 필터 · 정수 배율 유지** [확정 #16] |
| `PortalRing` | 저속 회전 + 청색 발광 명멸 루프 |
| `LobbyBackground` | 네온(`JALECO`·`FACTORY AREA 3`) 미세 명멸. 패럴렉스 없음(정적 화면) |
| `ContinueButton` | 골드 테두리 발광 펄스 (로비 유일 CTA 강조) |
| `ChapterButton` | 발광 테두리 상시 유지 — 다른 두 버튼과의 우선순위 차이를 연출로 보장 |
| `!` 배지 | 정적. 깜빡임 없음 (배지가 5곳이라 동시 점멸 시 시각 노이즈) |
| 사운드 | 로비 BGM 루프 1종(어둡고 음습한 아케이드 톤) · 버튼 클릭 SFX 1종 · `준비 중` 안내 SFX 1종 · `CONTINUE` 전용 진입 SFX 1종 |

> 후처리(블룸·비네트) **최소** [확정 #16]. 배경의 발광감은 후처리가 아니라 **스프라이트에 그려 넣는다**.

---

## 6. 의존 · 연결

### 6-1. 의존
| 대상 | 관계 |
|------|------|
| 호스트 선택 (#13) | `ContinueButton`·`ChapterButton`·`HostButton`이 진입시키는 유일한 화면 |
| 재화 체계 (#22) | 스태미나·골드·젬 표시. **갱신 경로는 한 지점** [확정 §6-4] |
| 데이터 저장 (#23) | 저장소 인터페이스 경유 조회 [확정 §6-1·2] |
| 진행도 (#10) | `ChapterCard` 표시 원본 |
| 타이틀 (#11) | 로비의 직전 씬 |
| 상점·배틀패스·이벤트·일일로그인·미션·업적·랭킹·인벤토리·친구·우편 | ⛔ 1차 밖. **버튼만 존재** |

### 6-2. 발행 이벤트 (Publish)
> `public struct` · `readonly` 금지 · 발행자 프로퍼티명과 다른 필드명 [`coding_conventions.md`]

| 이벤트 | 필드 | 발행 시점 |
|--------|------|-----------|
| `LobbyReadyEvent` | — | 로비 UI 바인딩 완료 |
| `LobbyStartRequestedEvent` | `LobbyRoute Route` (`ChapterStart` / `HostManage`) | `CONTINUE`·`CHAPTER`·`HOST` 클릭 |
| `LobbyFeatureBlockedEvent` | `string BlockedFeatureKey`, `bool IsLocked` | ⛔ 요소 클릭 (`IsLocked=true`는 `FriendsTab`만) |

### 6-3. 구독 이벤트 (Subscribe)

| 이벤트 | 필드 | 반응 |
|--------|------|------|
| `OnSceneLoaded` (프레임워크) | — | `LobbyScene` 확인 후 초기 바인딩 |
| `CurrencyChangedEvent` | `CurrencyKind Kind`, `long NewAmount` | 해당 카운터 텍스트 갱신 |
| `GhostLevelChangedEvent` | `int NewLevel`, `int NewExp`, `int NewExpToNext` | `GhostWidget` 갱신 |
| `ChapterProgressChangedEvent` | `int NewChapter`, `int NewClearedStage`, `int NewStageTotal` | `ChapterCard` 갱신 |

> 구독 토큰은 **필드에 저장 후 `OnDestroy`에서 `Dispose`** [`07_framework_rules.md`]. `C# event`·`Action` 직접 선언 금지.

---

## 7. 미정 · 결정 필요

| # | 항목 | 상태 |
|---|------|------|
| 1 | 스태미나 회복 속도·상한 정책 | `[TBD — 이유: 경제 밸런스 미착수. Decisions §8 이월]` |
| 2 | CH1·CH2 챕터명·보스명 (목업에 CH3 `FACTORY`/`MAD DOCTOR`만 노출) | `[TBD — 이유: 챕터 콘텐츠 미기획]` |
| 3 | 고스트 레벨 EXP 곡선 (목업 분모 `100`이 전 레벨 고정인지) | `[TBD — 이유: 성장 밸런스 미착수]` |
| 4 | `LobbyBackground`의 챕터 연동 교체 범위 | `[TBD — 1차는 정적 1종]` |
| 5 | `!` 배지 실제 발생 조건 | `[TBD — 해당 콘텐츠 기획 시]` |
| 6 | `준비 중입니다` 문구 최종안 | `[TBD — 현지화 시]` |

**[WARNING] 목업 대비 유의**
- 목업 수치는 전부 **고레벨 유저 샘플**이다. 신규 유저 초기 상태 목업이 없다. 3-4절 바인딩 표로 대체했으나, **`CHAPTER 01` 상태의 카드 렌더 확인**은 Stage 2b 박스 목업에서 별도 검증이 필요하다.
- 목업의 `CONTINUE` 버튼은 "이미 진행 중인 챕터가 있다"는 전제다. **최초 실행 유저에게 `CONTINUE`가 적절한 문구인지** 결정이 필요하다 (`START` 대체 여부). 목업 100% 재현 원칙상 현재는 `CONTINUE` 유지로 기술했다.

---

## 개정 이력

| 버전 | 날짜 | 변경 내용 |
|------|------|----------|
| 0.1 | 2026-08-07 | 최초 작성 — 목업 육안 판독 기반 요소 29종·이벤트 7종·1차 범위 구분 정의 |
</content>
</invoke>
