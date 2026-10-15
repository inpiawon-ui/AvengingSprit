# 화면 설계서 — 추가 모드 (망령의 탑 · 망령의 밤)

- **화면**: 로비 모드 칸 → 입장 패널 → 팝업 → 인게임 HUD → 결과 · **소속 콘텐츠**: [`AVSR_Content_Tower.md`](../AVSR_Content_Tower.md) · [`AVSR_Content_NightMode.md`](../AVSR_Content_NightMode.md)
- **시안**: [`AVSR_Screen_Modes.html`](AVSR_Screen_Modes.html) — 프레임 L · T1~T9 · N1~N7 (그림 바탕 `modes_mock/`)
- **Version**: `0.2` · **Status**: 시안 — PD 확인 대기 · **Last Updated**: 2026-10-15
- 규약: 요소 이름 = 프리팹 GameObject 이름 = 클라 바인딩 키. 좌표는 기준 해상도 720×1280(constants.md 3절) 대략값. 가로 앵커는 가운데 또는 스트레치(05_prefabs 「왼쪽에 못 박지 않는다」).
- 글자는 전부 언어팩 키(6절) — 그림에 글자를 넣지 않는다. 3언어 전 문장을 `GetPreferredValues` 로 칸 확인.

---

## 1. 흐름 · 이동 규칙

### 1-1. 망령의 탑

| 지금 | 누름 · 일 | 조건 | 다음 |
|---|---|---|---|
| 로비 PLAY 탭 | `ModeDefenseCard` | — | T1 `TowerEntryPanel` |
| T1 | `TowerFloorFitButton` (지금 층 카드의 「전체 보기」) | — | T2 `TowerFitPopup` |
| T1 | `TowerSweepButton` | 오늘 안 받음 · 이번 주 깬 층 ≥ 1 | T4 `TowerSweepPopup` (지급 후 열림) |
| T1 | `TowerBuyButton` (「＋ 추가 입장」) | 오늘 산 횟수 < 3 | T3 `TowerBuyPopup` (사면 T1 로 돌아옴) |
| T1 | `TowerStartButton` | 입장 ≥ 1 | 입장 −1 저장 → 로딩 → T5 |
| T1 | `TowerStartButton` | 입장 0 | T3 (사면 바로 로딩 → T5) |
| T1 | `TowerCloseButton` · 뒤로가기 | — | 로비 |
| T2 | 초상 · `TowerFitGrowthButton` | — | 성장 화면(그 호스트) |
| T5 → T6 | 층 소개 띠 2.2초 | — | 전투 |
| T6 | 층의 적 전멸 | — | 저장 · 첫 클리어 지급 → 0.6초 → T8 |
| T6 | 유령 에너지 0 · 일시정지 → 나가기 | — | T9 |
| T8 | `TowerNextButton` 「N+1층 도전」 | 입장 ≥ 1 | 입장 −1 → 로딩 → T5 |
| T8 · T9 | 같은 버튼 | 입장 0 | T3 |
| T9 | `TowerRetryButton` 「다시」 | 입장 ≥ 1 | 입장 −1 → 로딩 → T5 (같은 층) |
| T9 | `TowerGrowthButton` · 초상 | — | 성장 화면 |
| T8 · T9 | `TowerLobbyButton` 「나가기」 | — | 로비 → T1 을 다시 연 상태 |

### 1-2. 망령의 밤

| 지금 | 누름 · 일 | 다음 |
|---|---|---|
| 로비 PLAY 탭 | `ModeSurvivalCard` | N1 `NightEntryPanel` |
| N1 | 요일 칸(다른 요일) | 그 요일 조건 · 호스트를 미리보기(시작 버튼은 「오늘만」으로 잠김) |
| N1 | `NightStartButton` (입장 ≥ 1) | 입장 −1 저장 → 로딩 → N2 |
| N1 | `NightStartButton` (입장 0) · `NightBuyButton` | `NightBuyPopup`(T3 과 같은 모양) — 사면 바로 로딩 / 입장만 +1 |
| N2 | 시작 몸 셋 중 하나에 빙의 | 나머지 둘 사라짐 → N3 |
| N3 | 45초마다 / 1분마다 / 레벨업 | N4 / N5 / N6(카드 고르는 동안 전투 정지) |
| N3 | 보스 처치 · 유령 에너지 0 | N7 |
| N7 | `NightRetryButton` (입장 ≥ 1 → 입장 −1 → 로딩 · 0 → `NightBuyPopup`) / `NightLobbyButton` | N2 / 로비 → N1 |

> 뒤로가기(06_ui): 팝업 → 패널 → 로비 순. 인게임에서는 본편 그대로(일시정지 창).

---

## 2. 로비 — 모드 칸 (프레임 L)

지금 `LobbyMainUI` 의 두 칸을 연다(`LobbyMainUI.cs:110-111` — 지금은 `NotifyNotReady`). 칸 크기 · 그림은 그대로.

| 요소 이름 | 컴포넌트 | 위치 | 표시 | 입력 |
|---|---|---|---|---|
| `ModeSurvivalCard` | Button (있음) | 아래 왼쪽 칸 | 그림 `modeart_survival` | → N1 |
| └ `ModeSurvivalTitleText` | TMP | 칸 아래 1/3 | 「망령의 밤」 `ui.lobby.mode.survival.name` | — |
| └ `ModeSurvivalSubText` | TMP + 보석 Image | 제목 아래 칩 | 「원거리의 날 · 입장 2/3」 | — |
| └ `ModeSurvivalBadge` | Image | 칸 오른쪽 위 | 빨간 점 — 오늘 입장이 남음 | — |
| `ModeDefenseCard` | Button (있음) | 아래 오른쪽 칸 | 그림 `modeart_defense` (교체 후보) | → T1 |
| └ `ModeDefenseTitleText` | TMP | | 「망령의 탑」 `ui.lobby.mode.defense.name` | — |
| └ `ModeDefenseSubText` | TMP | | 「이번 주 12층 · 입장 2/3」 | — |
| └ `ModeDefenseBadge` | Image | 칸 오른쪽 위 | 빨간 점 — 입장 남음 또는 오늘 소탕 안 받음 | — |
| 자물쇠 `modelockicon` | Image | | **끈다** | — |

---

## 3. 망령의 탑 화면

### T1. `TowerEntryPanel` (`~Panel`, 로비 위 · 상단 HUD 128px 아래)

| 요소 이름 | 컴포넌트 | 위치(720×1280) | 표시 데이터 | 입력 → 일 |
|---|---|---|---|---|
| `TowerEntryPanel` | ~Panel | 05_prefabs `~Panel` 기준(상단 고정 · H 1152) | — | — |
| `TowerBackButton` | Button | 왼쪽 위 (20,146) 64×56 | ◀ | 닫기 |
| `TowerTitleText` | TMP | 가운데 위 y142 | 「망령의 탑」 | — |
| `TowerResetText` | TMP | 오른쪽 위 y160 | 「초기화까지 3일 4시간」 — 다음 월 05:00 까지 | 1분마다 갱신 |
| `TowerWeekBar` | Image | (20,214) 680×64 | — | — |
| └ `TowerWeekText` | TMP | 왼쪽 | 「이번 주 12층」 `towerWeekCleared` | — |
| └ `TowerLastWeekText` | TMP | 오른쪽 | 「지난주 최고 18층」 `towerLastWeekBest` (없으면 숨김) | — |
| `TowerLadder` | RecyclingScrollView (세로) | (20,296) 270×500 | 층 칸 목록 — 기본은 지금 층이 가운데(아래 2 · 위 2) | 위아래 밀기 · 칸 누르면 그 층 카드 미리보기(도전은 지금 층만) |
| └ `TowerLadderCell` | 프리팹 (재사용) | 270×92 | 층 번호 · 이름 · 상태(지난 ✓ / 지금 금테 + 「도전」 / 앞 흐림) · 보스 층은 보스 초상 | — |
| `TowerFloorCard` | Image(금테) | (304,296) 396×500 | 고른 층(기본 = 지금 층) | — |
| └ `TowerFloorNumText` · `TowerFloorNameText` | TMP | 위 | 「13층」 「마법의 무리」 | — |
| └ `TowerFloorDescText` | TMP | | 조건 한 줄 `tower.rule.{rule}.desc` | — |
| └ `TowerFloorAdvIcon` · `TowerFloorAdvText` | Image ×2 + TMP | | 적 속성 보석 → 유리한 속성 보석 · 「무기 몸 유리 ▲」(규칙에 속성이 없으면 숨김) | — |
| └ `TowerFloorFitText` | TMP | | 「맞는 호스트 22 · 내가 키운 ★5」 — 키운 몸 0 이면 빨간색 | — |
| └ `TowerFloorFitFaces` > `TowerFitFace` ×3 | Image | | 키운 몸 먼저 3명(금테 ★ / 안 키움 흐림) | — |
| └ `TowerFloorFitButton` | Button | 얼굴 줄 끝 | 「전체 보기 ›」 | → T2 |
| └ `TowerFirstClearText` · `TowerFirstClearItems` | TMP · 아이템 칸 ×2 | 카드 아래 | 「첫 클리어 보상 (매주 다시)」 골드 · 조각 — 이번 주 이미 깬 층을 보면 「받음 ✓」 | — |
| `TowerSweepBox` | Image | (20,816) 680×120 | — | — |
| └ `TowerSweepTitleText` | TMP | | 「오늘의 소탕 · 12층 기준」 | — |
| └ `TowerSweepItems` | 아이템 칸 ×2 | | 계단 보상(골드 · 조각 수) | — |
| └ `TowerSweepButton` | Button(청록) | 오른쪽 | 「소탕」 / 받았으면 「오늘 받음 ✓」(잠김) / 깬 층 0 이면 잠김 + 「1층을 깨면 받을 수 있다」 | → 지급 → T4 |
| `TowerSweepHintText` | TMP | 소탕 칸 아래 | 「하루 1번 · 입장 횟수를 쓰지 않는다」 | — |
| `TowerEntryPips` > `TowerEntryPip` ×3 | Image | (20,986) | 남은 입장 점(무료 3 기준, 산 입장은 점 뒤에 「+1」) | — |
| `TowerEntryText` | TMP | 점 오른쪽 | 「2 / 3」 | — |
| `TowerBuyButton` | Button | 오른쪽 | 「＋ 💎 추가 입장」 — 오늘 산 횟수 3이면 잠김 | → T3 |
| `TowerStartButton` | Button(금) | 가운데 (110,1072) 500×120 | 「13층 도전 ▶」 / 입장 0 이면 「💎50 추가 입장」 | 입장 −1 → 인게임 / → T3 |
| `TowerStartHintText` | TMP | 버튼 아래 | 「입장 1번 사용 · 이기든 지든 줄어든다」 | — |
| `PopupParent` | — | 맨 아래 자식 | 규약(05_prefabs) | — |

상태: 이번 주 깬 층이 표 끝을 넘으면 되풀이 층을 그대로 보여 준다. 입장 0 + 산 횟수 3 이면 도전 버튼 잠김 「내일 05:00에 3번」.

### T2. `TowerFitPopup` (`~Popup`)

| 요소 이름 | 컴포넌트 | 표시 | 입력 |
|---|---|---|---|
| `TowerFitTitleText` | TMP | 「13층에 맞는 호스트」 | — |
| `TowerFitAdvText` + 보석 2 | TMP · Image | 「적이 전부 마법 → 무기 몸이 유리 ▲」 | — |
| `TowerFitList` > `TowerFitItem` | RecyclingScrollView 4열 | 초상(키운 몸 금테 ★) · 이름 · 이 층 상성(▲ 유리 / ▼ 불리 / 없음). 정렬: 키운 몸 → 유리 → 나머지 | 초상 → 성장 화면 그 호스트 |
| `TowerFitCloseButton` · `TowerFitGrowthButton` | Button | 「닫기」 · 「육성하러 가기」 | 닫기 / 성장 화면 |

### T3. `TowerBuyPopup` (`~Popup`)

| 요소 이름 | 표시 | 입력 |
|---|---|---|
| `TowerBuyTitleText` | 「오늘 입장을 다 썼다」(입장 0 에서 열렸을 때) / 「입장을 더 산다」(＋ 에서) | — |
| `TowerBuyDescText` | 「무료 입장은 내일 05:00에 3번으로 다시 찬다」 | — |
| `TowerBuyCostText` | 「💎50 으로 1번 더 입장」 | — |
| `TowerBuyCountText` | 「오늘 추가 입장 0 / 3 · 다음 값 100 → 200」 | — |
| `TowerBuyCancelButton` · `TowerBuyOkButton` | 「취소」 · 「💎50 입장」 | 젬 차감 → 입장 +1 → (도전에서 열렸으면) 바로 로딩 / 젬 부족 → 젬 상점 |

### T4. `TowerSweepPopup` (`~Popup`)

| 요소 이름 | 표시 | 입력 |
|---|---|---|
| `TowerSweepResultTitle` | 「소탕 완료」 · 「이번 주 12층 기준」 | — |
| `TowerSweepResultItems` > `TowerSweepResultItem` | 골드 · 조각(초상 + 「로봇 ×1」) | — |
| `TowerSweepNextText` | 「다음 계단 20층 — 조각 ×3 · 골드 300」(최고 계단이면 숨김) | — |
| `TowerSweepOkButton` | 「받기」 | 닫기 → 아이콘이 재화 줄로 날아감 |

### T5 · T6. 인게임 — `InGameMainUI` 에 노드 추가 (탑일 때만 켬)

본편 HUD(유령 · 호스트 패널 · 조이스틱 · 스킬 · 나가기)는 그대로.

| 요소 이름 | 위치 | 표시 | 비고 |
|---|---|---|---|
| (본편) 챕터 · 방 칸 | 위 가운데 | **숨김** | |
| `TowerPlate` | 위 가운데 (306,28) 108×100 | 「TOWER」 · 「13F」 · 「남은 적 9」 | `TowerEnemiesLeftEvent` |
| (본편) 골드 칸 | 왼쪽 둘째 줄 | **숨김** — 일시정지가 그 자리로 | 탑은 골드를 안 떨군다 |
| (본편) 경험치 막대 | | **숨김** | 탑은 레벨업 없음 |
| `TowerRuleChip` | 위 가운데 둘째 줄 (240,146) | 보석 · 「마법의 무리 · 무기 ▲」 | 누르면 조건 설명 2초 |
| `TowerIntroBanner` | 화면 가운데 띠 (0,420) 720폭 | 「13층」 · 「마법의 무리」 · 「적 전부 마법 · 무기 몸 유리 ▲」 | 2.2초, 입장 무적 1초 동안 적 정지 |
| `TowerPossessLeftText` | 조이스틱 위 가운데 | 「빙의 남음 1」 | `OneShot` 층만 · 0 이 되면 몸 표식 전부 꺼짐 |
| `TowerNoLeaveIcon` | 나가기 버튼 위 덮개 | 자물쇠 | `NoLeave` 층만 · 누르면 「이 층은 나갈 수 없다」 토스트 |

### T8 · T9. `TowerResultPopup` (Global Popup — 본편 챕터 결과 창과 같은 프레임)

| 요소 이름 | T8 클리어 | T9 실패 |
|---|---|---|
| `TowerResultTitle` | 「13F CLEAR」 | 「13층 실패」 |
| `TowerResultFloorName` | 「마법의 무리」 | 같음 |
| `TowerResultRewardBox` > 아이템 | 첫 클리어 보상(골드 · 조각) | 숨김 |
| `TowerResultNextBox` | 「다음 층 · 14층 · 첫 몸으로 끝까지 — 스스로 나올 수 없다」 | 숨김 |
| `TowerResultHintBox` | 숨김 | 「이 층에 맞는 키운 몸 ★2 / 22 · 무기 호스트를 키우면 쉬워진다」 + 키울 몸 초상 3(`TowerSuggestFace`, 누르면 성장 화면) |
| `TowerResultStatText` | — | 「빙의 3 · 몸 잃음 2 · 1분 12초」 |
| `TowerResultEntryPips` | 「오늘 입장 ●○○ 1 / 3」 | 같음 |
| `TowerLobbyButton` | 「나가기」 | 같음 |
| `TowerGrowthButton` | 숨김 | 「육성」 |
| `TowerNextButton` / `TowerRetryButton` | 「14층 도전 ▶」 | 「다시 ▶」 — 입장 0 이면 「💎50 추가 입장」 |

---

## 4. 망령의 밤 화면

### N1. `NightEntryPanel` (`~Panel`)

| 요소 이름 | 위치 | 표시 | 입력 |
|---|---|---|---|
| `NightBackButton` · `NightTitleText` | 위 | ◀ · 「망령의 밤」 | 닫기 |
| `NightDayStrip` > `NightDayCell` ×7 | (20,220) 680 폭 7칸 | 월~일 · 조건 한 단어(근거리 · 원거리 …). 오늘 금테 | 누르면 그 요일 미리보기 |
| `NightTodayCard` | (20,330) 680×470 | 바탕에 칸 그림(`modeart_survival`) 흐리게 | — |
| └ `NightDayText` · `NightRuleTitleText` · `NightRuleText` | | 「오늘 · 화요일」 「원거리의 날」 「원거리 호스트만 몸으로 나온다」 | — |
| └ `NightHostFitText` | | 「오늘 나오는 호스트 7 · 내가 키운 ★2」 | — |
| └ `NightHostList` > `NightHostSlot` | 7칸 한 줄(넘치면 가로 밀기) | 키운 몸 먼저(금테 ★) | 초상 → 성장 화면 |
| └ `NightBestBox` | | 「화요일 최고 처치 238」 | — |
| └ `NightRewardBox` | | 「처치 50마다 조각 1 · 보스 처치 +3」 | — |
| `NightFlowRow` > 4칸 | (20,820) | 「0:00 몸 셋 중 하나」 「매 1분 속성 바뀜 · 회복」 「45초마다 대형」 「5:00 보스」 | — |
| `NightEntryPips` > `NightEntryPip` ×3 · `NightEntryText` | (20,986) | 오늘 남은 입장 점 3 · 「2 / 3」 | — |
| `NightBuyButton` | 오른쪽 | 「＋ 💎 추가 입장」 — 오늘 산 횟수 3이면 잠김 | → `NightBuyPopup` |
| `NightStartButton` | (110,1072) 500×120 | 「시작 ▶」 / 입장 0 이면 「💎50 추가 입장」 — 다른 요일을 보는 중이면 「오늘만 시작할 수 있다」로 잠김 | 입장 −1 → 로딩 → N2 |
| `NightStartHintText` | 버튼 아래 | 「입장 1번 사용 · 이기든 지든 줄어든다」 | — |

### N2 ~ N5. 인게임 — `InGameMainUI` 에 노드 추가 (밤일 때만 켬)

| 요소 이름 | 위치 | 표시 | 갱신 |
|---|---|---|---|
| (본편) 챕터 · 방 칸 · 골드 칸 | | **숨김**(일시정지는 골드 칸 자리로) | |
| `NightPlate` | 위 가운데 (296,28) 128×100 | 「NIGHT」 · `NightTimerText` 「2:47」(4:30부터 빨강) · `NightKillText` 「처치 132」 | 1초마다 · 처치할 때 (`NightStateEvent`) |
| `NightWaveChip` | 위 가운데 둘째 줄 | 보석 + 「무리: 마법」 | 속성 바뀔 때 — 한 번 번쩍 |
| `NightNextEventText` | 칩 아래 | 「대형 호스트 0:12」 / 「보스 0:40」 | 1초마다 |
| (본편) 레벨 · 경험치 막대 | 상단 패널 아래 한 줄 | 「Lv 6」 막대 | 본편 `RunExpChangedEvent` |
| `NightStartHintBanner` | 가운데 띠 | 「셋 중 하나에 빙의 · 고른 몸 말고 나머지 둘은 사라진다」 + 「무리가 내려오기까지 2초」 | 빙의하면 꺼짐 (N2) |
| `NightBannerText` | 가운데 띠 | 「대형 설녀 등장 · 몰아쳐서 경직시키면 빼앗는다」 / 「3:00 · 마법 → 파워 · 15% 회복 · 마법 몸 유리 ▲」 / 「보스 크러셔」 | 사건마다 1.6~2초 (`NightBannerEvent`) |
| `NightCounterHint` | 조이스틱 위 | 「무기 몸이 오면 ▲」 — 지금 몸이 무리에 불리할 때만 | 속성 · 몸이 바뀔 때 |
| 대형 호스트 머리 위 | 몸 위 | 이름 「대형 · 설녀」 · 체력 · **경직 게이지**(본편 `StaggerProgress` 표식) · 발밑 금테 | 본편 |
| 회복 숫자 | 유령 · 호스트 패널 위 | 「+15%」 초록 | 1분마다 · 대장 처치 |
| N6 레벨업 카드 | | **본편 화면 그대로** — 열려 있는 동안 전투 정지 | |

### N7. `NightResultPopup` (Global Popup)

| 요소 이름 | 표시 |
|---|---|
| `NightResultTitle` | 「밤을 버텼다」 / 「쓰러졌다」 |
| `NightResultSubText` | 「화 · 원거리의 날 · 보스 크러셔 처치」 |
| `NightResultKillText` | 처치 수 — 가장 크게, 0 부터 올라감 |
| `NightResultBestRibbon` | 「최고 기록! (이전 238)」 — 갱신일 때만 |
| `NightResultShardList` > `NightResultShardItem` | 「보상 — 처치 50마다 1 (×5) + 보스 처치 3」 초상 × 개수 |
| `NightResultStatText` | 「버틴 시간 5:12 · Lv 10 · 빙의 4 · 몸 잃음 2」 |
| `NightResultEntryPips` | 「오늘 입장 ●○○ 1 / 3」 |
| `NightLobbyButton` · `NightRetryButton` | 「로비」 · 「다시 ▶」 — 입장 0 이면 「💎50 추가 입장」 |

---

## 5. 발주할 그림 (시안 통과 뒤 — 이 배치 그대로)

| 이름 | 쓰는 곳 | 비고 |
|---|---|---|
| `tower_plate` · `night_plate` | 인게임 위 가운데 판 | 지금 챕터 칸과 같은 계열 · 탑 청록 / 밤 보라 |
| `mode_rule_chip` | 조건 칩 · 무리 칩 | 둥근 칩 · 9분할 |
| `mode_intro_band` | 층 소개 띠 · 사건 띠 | 가로 띠 · 9분할 |
| `tower_ladder_cell_past` · `_now` · `_next` · `_boss` | T1 층 사다리 | 지금 칸 금테 |
| `tower_floor_card` · `night_today_card` | T1 · N1 큰 카드 | 금테 |
| `night_day_cell` · `_today` | N1 요일 칸 | |
| `mode_entry_pip_on` · `_off` | 입장 점 | |
| `modeart_tower` | 로비 탑 칸 그림 | 지금 포탑 그림 교체 후보 — 탑 · 유령 |
| (같이 씀) 챕터 결과 창 프레임 · 아이템 칸 · 금 버튼 · 청록 버튼 · 재화 아이콘 · 상성 보석 · 초상 | | 있는 것 |

---

## 6. 언어팩 키

탑: `ui.tower.title` · `ui.tower.reset_in` 「초기화까지 {0}」 · `ui.tower.week` 「이번 주 {0}층」 · `ui.tower.last_week` 「지난주 최고 {0}층」 · `ui.tower.floor` 「{0}층」 · `ui.tower.floor_f` 「{0}F」 ·
`ui.tower.fit` 「맞는 호스트 {0} · 내가 키운 ★{1}」 · `ui.tower.fit_all` 「전체 보기」 · `ui.tower.fit_title` 「{0}층에 맞는 호스트」 · `ui.tower.adv` 「{0} 몸 유리 ▲」 ·
`ui.tower.first_clear` 「첫 클리어 보상 (매주 다시)」 · `ui.tower.first_clear_done` 「받음 ✓」 · `ui.tower.sweep` 「오늘의 소탕 · {0}층 기준」 · `ui.tower.sweep_btn` 「소탕」 · `ui.tower.sweep_done` 「오늘 받음 ✓」 · `ui.tower.sweep_locked` 「1층을 깨면 받을 수 있다」 · `ui.tower.sweep_hint` · `ui.tower.sweep_next` 「다음 계단 {0}층 — {1}」 ·
`ui.tower.entries` 「{0} / {1}」 · `ui.tower.buy` 「추가 입장」 · `ui.tower.buy_title_empty` · `ui.tower.buy_desc` · `ui.tower.buy_cost` 「💎{0} 으로 1번 더 입장」 · `ui.tower.buy_count` 「오늘 추가 입장 {0} / {1}」 ·
`ui.tower.start` 「{0}층 도전 ▶」 · `ui.tower.start_hint` · `ui.tower.no_entries` 「내일 05:00에 3번」 · `ui.tower.enemies_left` 「남은 적 {0}」 · `ui.tower.possess_left` 「빙의 남음 {0}」 · `ui.tower.no_leave` 「이 층은 나갈 수 없다」 ·
`ui.tower.result.clear` 「{0}F CLEAR」 · `ui.tower.result.fail` 「{0}층 실패」 · `ui.tower.result.next` 「다음 층」 · `ui.tower.result.hint` 「이 층에 맞는 키운 몸 ★{0} / {1}」 · `ui.tower.result.suggest` 「{0} 호스트를 키우면 쉬워진다」 · `ui.tower.retry` 「다시 ▶」 · `ui.tower.growth` 「육성」 · `ui.tower.leave` 「나가기」 ·
`tower.rule.{rule}.name` / `.desc`(8종) · `tower.floor.{n}.name`

밤: `ui.night.title` · `ui.night.day.{mon…sun}` · `ui.night.day_rule.{mon…sun}` · `ui.night.today` 「오늘 · {0}」 · `ui.night.fit` 「오늘 나오는 호스트 {0} · 내가 키운 ★{1}」 · `ui.night.best` 「{0} 최고 처치」 · `ui.night.reward` ·
`ui.night.flow.{start,wave,big,boss}` · `ui.night.entries` 「{0} / {1}」 · `ui.night.buy` 「추가 입장」 · `ui.night.start_hint` 「입장 1번 사용 · 이기든 지든 줄어든다」 · `ui.night.start` 「시작 ▶」 · `ui.night.today_only` 「오늘만 시작할 수 있다」 · `ui.night.kills` 「처치 {0}」 · `ui.night.wave` 「무리: {0}」 · `ui.night.next_big` 「대형 호스트 {0}」 · `ui.night.next_boss` 「보스 {0}」 ·
`ui.night.start_hint` · `ui.night.start_hint_sub` · `ui.night.banner.big` 「대형 {0} 등장」 · `ui.night.banner.big_sub` · `ui.night.banner.wave` 「무리가 {0}로」 · `ui.night.banner.heal` 「버텼다 — {0}% 회복」 · `ui.night.counter` 「{0} 몸이 오면 ▲」 ·
`ui.night.result.clear` 「밤을 버텼다」 · `ui.night.result.fail` 「쓰러졌다」 · `ui.night.result.best` 「최고 기록! (이전 {0})」 · `ui.night.result.reward` · `ui.night.result.stat`

로비: `ui.lobby.mode.survival.name/.desc` → 「망령의 밤」 · 「{0} · 입장 {1}/{2}」 / `ui.lobby.mode.defense.name/.desc` → 「망령의 탑」 · 「이번 주 {0}층 · 입장 {1}/{2}」

---

## 7. 미정

- 탑 칸 로비 그림 교체(`modeart_tower`) · 소탕 · 첫 클리어 · 젬 값 숫자 · 인게임 판 모양(TOWER / NIGHT).

## 개정 이력

| 버전 | 날짜 | 변경 |
|---|---|---|
| 0.2 | 2026-10-15 | 밤도 하루 입장 3번 — N1 입장 점 · 추가 입장 · 결과 창 입장 표시 · 로비 칸 |
| 0.1 | 2026-10-15 | 최초 작성 — 시안 L · T1~T9 · N1~N7 |
