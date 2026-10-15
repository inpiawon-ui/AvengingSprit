# 콘텐츠 세부 기획서 — 망령의 탑 (층 조건 모드)

- **콘텐츠명**: 망령의 탑 · **카테고리**: 추가 모드(상시 진행 콘텐츠) · **우선순위**: P1
- **Version**: `1.0` · **Last Updated**: `2026-10-15` · **Status**: 개발 스펙 — 시험판으로 재미 검증을 마쳤고, 이 문서대로 유니티에 옮긴다
- **시험판**: `Projects/AVSR/ModeLab/AVSR_Night.html` 의 「망령의 탑」 탭 (브라우저로 바로 열림) · 원본 `night_src.html`
- **같이 보는 문서**: [`AVSR_Content_NightMode.md`](AVSR_Content_NightMode.md) — **모드 진입 · 거리 분류 · 결과 창 구조는 망령의 밤과 같은 것을 쓴다.** 이 문서에서 「밤 문서 n절」이라고 쓴 곳은 그 문서가 정본이다.
- **이 문서의 원칙**: **새 전투 규칙을 만들지 않는다.** 빙의 · 경직 · 상성 · 성장 · 구조 몸 · 유령 에너지는 본편 코드를 그대로 쓴다.
  층마다 바뀌는 것은 **어떤 몸이 나오는가 · 어떤 적이 나오는가 · 빙의를 얼마나 허락하는가**뿐이다. 값은 전부 `TowerTable` 한 곳에 둔다.

---

## 0. 한 줄 요약

**층마다 「이 조건에 맞는 몸만 나온다」. 유령으로 들어가 그 몸을 빼앗아 층을 비운다. 5층을 한 번에 올라야 구간이 열린다.**
조건에 맞는 호스트를 키워 둔 사람만 계속 오른다 — 탑이 「다음에 누구를 키울지」를 정해 준다.

### 사용자(PD) 결정 원문 — 이 문서의 근거

| 날짜 | 결정 |
|---|---|
| 10-08 | 「1. 키운 캐릭터를 하면 + 효과 2. 빙의 · 상성으로 + 효과 3. 속성을 잘 이용하는 플레이 — 없던 룰 금지 4. 우리 게임 특성을 살린 모드」 · 「탑을 올라가서 특정 퀘스트 느낌」 |
| 10-13 | 「망령의 탑이 좋은 것 같아 — **매층마다 특정 조건이 있어서 다양한 호스트를 키워서 클리어할 수 있게** 조건 맞춰 주면 좋겠다」 |
| 10-13 | 「입장할 때 무조건 유령으로 · 시작할 때 빙의를 한 번 해서 시작」 |
| 10-14 | 입장 횟수 · 모드별 재화는 **모드를 다 만든 뒤에** 정한다. 지금은 재미 검증 |

### 4원칙이 어디서 지켜지나

| 원칙 | 이 모드에서 |
|---|---|
| 키운 캐릭터 + 효과 | 층 조건에 맞는 호스트만 몸으로 나온다 → 그 호스트를 키웠는가가 곧 돌파 여부. 시험판: 전부 키움 21~25층 4/5 돌파 · 안 키움 0/5 |
| 빙의 · 상성 + 효과 | 「적이 전부 마법」 층 · 보스 층은 상성이 맞는 몸을 골라야 빨리 깬다. 유령 에너지가 구간 내내 이어지므로 늦게 깨면 다음 층이 어렵다 |
| 속성을 잘 쓰는 플레이 | 일부만 키우면 막히는 층이 생긴다 — 무기 속성만 키운 경우 15층(크러셔, 마법 몸만 나옴)에서 3/5 실패 |
| 게임 특성 | 층마다 유령 입장 → 빙의로 시작 · 「첫 몸으로 끝까지」 · 「빙의는 한 번」처럼 **빙의 자체를 조건으로 거는 층** |

---

## 1. 목적 (플레이어 경험)

- **다음에 누구를 키울지 정해 주는 곳**: 막힌 층이 「원거리 몸이 약하다」 · 「마법 몸이 없다」를 알려 준다. 성장 화면으로 가는 이유가 된다.
- **한 번 오른 층은 남는다**: 5층마다 체크포인트. 오늘 못 깬 구간은 키우고 와서 다시 도전한다.
- **퀴즈처럼 푸는 층**: 「첫 몸으로 끝까지」 → 어떤 몸을 고를지가 승부. 「빙의는 한 번」 → 몸을 잃으면 끝. 「한 호스트의 층」 → 그 몸으로 어떻게 버틸지.
- **짧은 판**: 한 층 10~60초 · 구간(5층) 1~3분.

---

## 2. 화면 구성 (UI 요소)

> 이름 = 프리팹 GameObject 이름 = 코드 바인딩 키(세 곳 동일). 글자는 전부 언어팩 키(5절). 그림에 글자를 넣지 않는다.

### 2-1. 로비 입구 — 기존 `ModeDefenseCard` 를 쓴다

- `Assets/Scripts/Module/Lobby/LobbyMainUI.cs:111` 의 `ModeDefenseCard` 는 지금 `NotifyNotReady` 로 막혀 있다. 이 칸을 **망령의 탑 입구**로 연다.
  (서바이벌 칸 `ModeSurvivalCard` 는 망령의 밤 — 밤 문서 2-1절.)
- 칸 글자: 있는 키 `ui.lobby.mode.defense.name` 「디펜스 모드」 → 「망령의 탑」, `.desc` → 지금 구간(예: 「11~15층 도전 중」). 3언어 모두 바꾼다.
- 누르면 `TowerEntryPanel` 이 열린다.

### 2-2. `TowerEntryPanel` (로비에서 여는 `~Panel`)

| 요소 | 설명 | 상태 · 입력 |
|---|---|---|
| `TowerSegmentText` | 「11 ~ 15층」 지금 도전할 구간 | 저장된 돌파 층 + 1 부터 |
| `TowerBestText` | 「최고 14층」 | 구간 도중 가장 높이 간 층 |
| `TowerFloorList` > `TowerFloorRow` ×5 | 구간 5층의 목록 — 층 번호 · 조건 이름 · 조건 설명 · 보스 초상(보스 층) | 지나간 층은 흐리게, 보스 층은 테두리 강조 |
| (각 줄 안) `TowerFloorFitText` | 「맞는 호스트 7 · 내가 키운 ★2」 | 그 층 조건에 맞는 호스트 수와 그중 숙련도 ≥ 1 인 수. **0 이면 빨간색** — 「키워 와야 한다」를 미리 알린다 |
| (각 줄 안) `TowerFloorFitButton` | 누르면 그 층에 맞는 호스트 초상 목록을 팝업으로 | `TowerFitPopup` |
| `TowerRewardText` | 「첫 돌파 보상: 층마다 조각 · 보스 층 더 많이」 | 4-4 |
| `TowerStartButton` | 「11층부터 도전」 | |
| `TowerCloseButton` · `PopupParent` | | |

`TowerFitPopup` (`~Popup`): 그 층에 맞는 호스트 초상 · 키운 몸 ★ · 숙련도. 누르면 성장 화면의 그 호스트로 간다(있으면).

### 2-3. 인게임 HUD — `InGameMainUI` 에 노드 추가

본편 HUD(유령 체력 · 몸 체력 · 스킬 · 일시정지)는 그대로. 탑일 때만 켠다. 본편 경험치 막대는 **끈다**(탑은 레벨업이 없다, 3-8).

| 요소 | 설명 | 갱신 |
|---|---|---|
| `TowerFloorText` | 「13층 · 마법의 무리」 | 층이 바뀔 때 |
| `TowerRuleText` | 「적이 전부 마법 — 무기 몸이 유리」 | 층이 바뀔 때 |
| `TowerSegmentPips` > `TowerPip` ×5 | 구간 5층 진행 점(지난 층 채움 · 지금 층 깜빡임 · 보스 층 해골) | 층이 바뀔 때 |
| `TowerPossessLeftText` | 「빙의 남음 1」 — 「빙의는 한 번」 층에서만 | 빙의할 때 |
| `TowerNoLeaveIcon` | 「나가기 잠김」 자물쇠 — 「첫 몸으로 끝까지」 층에서만. 본편 나가기 버튼 위에 얹는다 | 층이 바뀔 때 |
| `TowerBannerText` | 층 입장 때 화면 가운데 띠 「13층 · 마법의 무리」 2.2초 | |

### 2-4. `TowerResultPopup` (인게임 결과 — Global Popup)

| 요소 | 설명 |
|---|---|
| `TowerResultTitle` | 「구간 돌파!」 / 「13층에서 쓰러졌다」 |
| `TowerResultFloorText` | 도달 층 · 최고 층(갱신이면 「최고 기록!」 띠) |
| `TowerResultHintText` | 실패했을 때만 — 쓰러진 층의 조건과 「맞는 호스트 중 키운 몸 ★0」 같은 한 줄. 다음에 키울 몸을 가리킨다 |
| `TowerResultShardList` > `TowerResultShardItem` ×N | 첫 돌파 보상 조각 |
| `TowerResultStatText` | 빙의 N · 몸 잃음 N · 걸린 시간 |
| `TowerRetryButton` · `TowerGrowthButton` · `TowerLobbyButton` | 다시 · 성장으로 · 로비로 |

---

## 3. 기능 · 규칙 (동작)

### 3-1. 진행 구조 — 5층 구간 · 체크포인트

```
[구간 1] 1  2  3  4  5(보스)   ← 5층을 한 번에 이어서 올라야 돌파
[구간 2] 6  7  8  9  10(보스)
[구간 3] 11 … 15(보스)          … 끝없이(표가 있는 만큼, 3-5)
```

- **한 번의 도전 = 한 구간(5층).** 구간 첫 층에서 시작해 5층(보스)을 잡으면 구간 돌파 → 다음 구간이 열린다(저장).
- **유령 에너지는 구간 내내 이어진다.** 구간 시작 때만 가득. 층 사이 회복은 기본 0(`TowerTable.FloorHealPercent`).
  - 이게 탑의 난이도다. 시험판에서 층마다 에너지를 채워 주면 안 키운 몸도 20층까지 거의 다 깼다 — 차이가 시간에만 났다.
    이어지게 하자 차이가 돌파 여부로 났다(6-5).
- 도중에 쓰러지면 그 구간 **첫 층부터 다시**. 이미 돌파한 구간은 다시 하지 않는다.
- 최고 층(`towerBestFloor`)은 구간 도중 가장 높이 간 층 — 기록 · 표시용.

### 3-2. 한 층의 흐름

1. **유령으로 입장** — 앞 층에서 쓰던 몸은 **비용 없이 놓고 온다**(사라짐 · 에너지 감소 없음 · 조각 없음).
2. 층 방이 깔린다: **잔몹 N + 조건에 맞는 몸 3**(서로 다른 호스트, 3-4). 보스 층은 보스 + 몸 3.
   - 몸 3은 **보통 적처럼 싸운다**(본편 숙주 몸). 유령은 바로 탈 수 있다(본편 규칙). 셋 다 남아 있으므로 갈아탈 몸이 된다.
3. 띠 「13층 · 마법의 무리」 + `TowerRuleText`.
4. 층의 적이 **모두 죽으면**(본편 방 클리어 판정) → 문이 열린다(`SpawnExit`) → 문에 닿으면 다음 층(`EnterTowerFloor(next)`).
   - 5번째 층(보스)을 잡으면 문 대신 **구간 돌파 → 결과 창**.
5. 유령 에너지 0 → 실패 → 결과 창.

> 「모두 죽으면」에는 아직 빼앗지 않은 몸도 들어간다 — 남은 몸은 잡아도 되고 빼앗아도 된다(본편 방과 같음).

### 3-3. 층 조건 (규칙 종류)

층마다 `TowerFloor.Rule` 하나. 전부 **본편에 있는 길을 막거나 좁히는 것**이다 — 새 전투 동작은 없다.

| 규칙 | 뜻 | 구현 (본편 자리) |
|---|---|---|
| `BodyRange(근/중/원)` | 그 거리의 호스트만 몸으로 나온다 | 몸 3을 그 분류에서 뽑는다. 분류는 **밤 문서 3-4절**과 같은 `RangeClassOf` |
| `BodyAffinity(파워/무기/마법)` | 그 속성 호스트만 몸으로 나온다 | 몸 3을 그 속성에서 뽑는다(`AffinityRule.KindOf`) |
| `EnemyAffinity(파워/무기/마법)` | 적이 전부 그 속성. 몸은 세 속성이 섞여 나온다 | 잔몹 목록 = 그 속성 잔몹 + **그 속성 호스트(빼앗기 막음)**. 몸 3은 세 속성에서 하나씩 |
| `NoLeave` | 첫 몸으로 끝까지 — 스스로 나올 수 없다 | `CanLeaveHost`(`BattleDirector.SoulFx.cs:680`) 첫 줄에서 `false`. 몸이 죽으면 본편대로 유령 → 다른 몸 · 구조 몸은 탈 수 있다 |
| `SingleHost` | 호스트 하나만 몸으로 나온다(3명 다 같은 호스트) | 층에 들어갈 때 1명을 뽑는다(3-4) |
| `OneShot` | 빙의를 한 번만 할 수 있다 · 구조 몸 없음 | 이 층 빙의 횟수가 1이면 `IsPossessable` 이 전부 `false` · `TickRescue` 끔. 몸을 잃으면 유령으로 남아 에너지가 다할 때까지 — 사실상 실패 |
| `Boss(bossKey, 몸 조건)` | 보스 + 그 보스를 이기는 속성의 몸 3 | 보스 세우기는 밤 문서 3-8절의 `SpawnBoss(bossKey)` 를 같이 쓴다. 몸 조건은 위 `BodyAffinity` |

- **조건 밖의 호스트가 적으로 나오면 빼앗기를 막는다**(`TowerBlocked = true` → `IsPossessable` 이 `false`). 그 몸이 열리면 층 조건이 무너진다.
  표시도 「몸」 표식을 안 띄운다(`HasPossessCondition` 도 `false`).
- 구조 몸(`TickRescue`)은 **층 조건에 맞는 호스트**에서 나온다. `OneShot` 층만 없다.

### 3-4. 몸 뽑기

- 층 조건에 맞는 호스트 목록에서 **서로 다른 3명**(목록이 3명보다 적으면 겹쳐도 됨).
- **해금된 몸이 목록에 있으면 적어도 하나는 해금된 몸** — 키운 사람이 키운 몸을 쓸 자리를 보장한다(밤 문서와 같음).
- `SingleHost` 층: 목록 = 히든(사신) 뺀 전부. 1명을 뽑을 때 **해금된 몸에서 먼저**(없으면 전부에서). 셋 다 그 호스트.
- 다시 도전하면 다시 뽑는다(같은 층이어도 몸이 바뀔 수 있다).
- 사신(`death`)은 히든이라 어느 층에도 나오지 않는다(`IsHiddenHost`).

### 3-5. 층 표 — 처음 10층(한 바퀴)

| 층 | 이름 | 규칙 | 잔몹(종류 · 수) | 비고 |
|---|---|---|---|---|
| 1 | 근거리의 층 | `BodyRange(근)` | `skeleton` · `bat` · `boar` × 6 | |
| 2 | 원거리의 층 | `BodyRange(원)` | `scrapgunner` · `skeleton` × 7 | 적이 흩어진 방(원거리가 쏠 거리) |
| 3 | 마법의 무리 | `EnemyAffinity(마법)` | 마법 잔몹 + 마법 호스트(막음) × 8 | 무기 몸이 유리 |
| 4 | 첫 몸으로 끝까지 | `NoLeave` | `boar` · `scrapgunner` · `skeleton` × 7 | 몸은 전 호스트에서 |
| 5 | 보스 — 크러셔 | `Boss(crusher, 마법)` | 없음 | 크러셔(파워) · 마법 몸만 |
| 6 | 중거리의 층 | `BodyRange(중)` | `bat` · `bat` · `boar` × 9 | 빠른 박쥐 떼 |
| 7 | 한 호스트의 층 | `SingleHost` | `skeleton` · `scrapgunner` · `boar` × 7 | |
| 8 | 파워의 무리 | `EnemyAffinity(파워)` | 파워 잔몹 + 파워 호스트(막음) × 9 | 마법 몸이 유리 |
| 9 | 빙의는 한 번 | `OneShot` | `scrapgunner` · `skeleton` · `boar` × 7 | 첫 몸 고르기가 승부 |
| 10 | 보스 — 킹핀 | `Boss(kingpin, 파워)` | 없음 | 킹핀(무기) · 파워 몸만 |

- 11층부터는 **같은 10층을 되풀이하면서 단단해진다**(3-6). 라이브 중에 새 층을 쓰면 표에 줄을 더하면 된다 — 줄이 있는 층은 줄을, 없는 층은 되풀이를 쓴다.
- 보스는 처음엔 크러셔 · 킹핀 둘. 표에 `python` 등 다른 보스 · 다른 규칙 조합을 넣어 바퀴마다 다르게 만들 수 있다(7절).
- 잔몹 키는 본편 적 키 그대로(속성은 `AffinityRule.KindOf`). 배치 위치는 `TowerFloor.RoomId` 로 지정한 정본 방의 배치 칸을 쓰고 **종류만 바꾼다.** 칸이 모자라면 빈 바닥에 무작위.

### 3-6. 세기 (층이 오를수록)

| 값 | 식 (f = 층, loop = (f−1) ÷ 10 의 몫) | 1층 | 10층 | 11층 | 20층 | 30층 |
|---|---|---|---|---|---|---|
| 잔몹 · 적 호스트 체력 배율 | `1 + (f−1)×0.1 + loop×0.5` | ×1.0 | ×1.9 | ×2.5 | ×3.4 | ×4.9 |
| 보스 체력 배율 | `0.55 + loop×0.4` | — | ×0.55 | — | ×0.95 | ×1.35 |

- 기준은 **1챕터 잡몹 · 1챕터 보스 값**(`SpawnHpMul` 의 1챕터 값). 공격은 올리지 않는다(시험판 그대로) — 맞아서 지는 게 아니라 **늦게 깨서 에너지가 마르는** 탑이다.
- 몸 3(조건 호스트)은 **적일 때만** 위 배율을 받는다. 빼앗으면 본편 `EnterHost` 가 호스트 표 + 내 성장으로 다시 만든다.

### 3-7. 유령 · 빙의 · 구조 — 본편 값 그대로

| 일 | 값 (본편) | 탑에서 |
|---|---|---|
| 유령 최대 체력 | 100 | 구간 시작 때만 가득 |
| 유령일 때 감소 | 초당 3.35 | 같음 |
| 몸이 죽음 | −20% | 같음 |
| 스스로 나옴 | −8% | 같음(`NoLeave` 층은 못 나옴) |
| 층 넘어갈 때 몸 놓기 | — | **비용 없음** |
| 빙의 | 체력 100% · 무적 1.25초 · 스킬 가득 · 성장 적용 | 같음 |
| 구조 몸 | 3초 뒤 · 쿨다운 20초 | 같음. 단 조건 호스트에서, `OneShot` 층은 없음 |

### 3-8. 탑에 없는 것

- **레벨업 · 카드 없음** — `GainExp` 를 부르지 않는다. 경험치 막대도 숨긴다.
  이유: 탑은 「무엇을 키워 왔는가」를 재는 곳이다. 판 안 카드가 섞이면 운이 실력 · 성장을 덮는다. 시험판도 카드 없이 쟀다.
- **골드 떨굼 없음**(`DropGold` 끔) · **판 안 조각 없음**(`GrantShards` 끔). 보상은 첫 돌파 하나로 센다(4-4).
- 상점 · 이벤트 · 휴식 방 없음 — 층은 전부 전투(보스) 방이다.

### 3-9. 입장 · 끝

- 로비 `TowerStartButton` → `RunModeContext.Current = RunMode.Tower` + 시작 층을 걸고 `ISceneManager.LoadAsync(InGame)` (밤 문서 3-2절과 같은 정적 값 · `RunMode` 에 `Tower` 를 더한다).
- 판 시작(`BattleDirector.cs:445-468`)에서 탑이면: `SpawnGhost()` 만 하고 `EnterStartHost()` 건너뜀 · `EnterRoom(0)` 대신 `EnterTowerFloor(시작 층)`.
- 다음 층: 문에 닿으면(본편 `BattleDirector.cs:9705-9723`) 탑이면 `EnterRoom(_roomIndex + 1)` 대신 `EnterTowerFloor(층 + 1)`.

| 끝나는 경우 | 판정 | 저장 |
|---|---|---|
| 구간 보스 처치 | 돌파 | `towerClearedFloor` = 구간 끝 층 · 첫 돌파 보상 |
| 유령 에너지 0 | 실패 | 최고 층만(갱신이면) · 그 구간에서 처음 넘은 층의 보상은 **준다**(4-4) |
| 일시정지 → 포기 | 실패 | 위와 같음 |

- `Finish(bool)`(`BattleDirector.cs:9744`)에서 탑이면 챕터 진행 · 상자 · 챕터 클리어 지급을 하지 않고 `TowerFinishedEvent` → `TowerResultPopup`. 밤 문서 3-12절과 같은 모양.

### 3-10. 예외

- 앱 종료 · 백그라운드 → 판은 저장하지 않는다. 그 도전의 돌파 기록은 없다(이미 넘은 층의 보상은 넘는 순간 지급했으므로 남는다).
- 조건에 맞는 호스트가 0명인 층(표 실수) → 경고 로그 + 조건 없이 전 호스트에서 뽑는다. 층이 막혀 진행이 멈추는 일은 없게.
- `OneShot` 층에서 빙의 전에 에너지가 다하는 경우 → 그냥 실패.
- `NoLeave` 층에서 몸이 죽으면 → 본편대로 유령. 남은 몸 · 구조 몸에 다시 빙의할 수 있다(「스스로」 나오는 것만 막는다).

---

## 4. 데이터 (값 · 저장 항목)

### 4-1. `TowerTable` (ScriptableObject — 새로 만든다)

- 위치 `Assets/BundleResource/TableData/TowerTable.asset` · 주소 `TableData/TowerTable` · 라벨 `label_tabledata`
- 코드 `Assets/Scripts/Module/InGame/Tower/TowerTable.cs` · 네임스페이스 `Game.Module.InGame`

| 칸 | 타입 | 기본값 | 쓰는 곳 |
|---|---|---|---|
| `Floors` | `TowerFloor[]` | 3-5 표 10줄 | 층 정의 |
| `SegmentSize` | int | 5 | 구간 길이 |
| `LoopLength` | int | 10 | 표 줄이 없는 층은 `(f−1) % LoopLength` 줄을 되풀이 |
| `HpPerFloor` · `HpPerLoop` | float | 0.1 · 0.5 | 3-6 |
| `BossHpBase` · `BossHpPerLoop` | float | 0.55 · 0.4 | 3-6 |
| `BodyCount` | int | 3 | 3-4 |
| `FloorHealPercent` | int | 0 | 층 사이 회복(`HealOnClear`) |
| `ShardFirstClear` · `ShardFirstClearBoss` | int | 1 · 3 | 4-4 (가안) |

`TowerFloor`: `Floor`(int, 0 = 되풀이용) · `NameKey` · `DescKey`(언어팩) · `Rule`(enum `TowerRule`) · `RuleRange`(근/중/원) · `RuleAffinity`(파워/무기/마법) · `BossKey` · `TrashKeys`(string[]) · `TrashCount`(int) · `RoomId`(배치용 정본 방)

`TowerRule`: `None` · `BodyRange` · `BodyAffinity` · `EnemyAffinity` · `NoLeave` · `SingleHost` · `OneShot` · `Boss`
> 표가 enum 번호로 저장된다 — 새 규칙은 **끝에** 붙인다(BuffKind 와 같은 주의).

거리 분류(`RangeClassOf`)는 **밤 표(`NightModeTable`)의 것을 같이 읽는다** — 두 모드에서 분류가 갈리지 않게 한 곳에 둔다.

### 4-2. 판 안 상태 (저장 안 함)

| 데이터 | 타입 | 비고 |
|---|---|---|
| `_towerFloor` | int | 지금 층 |
| `_towerSegmentStart` | int | 이번 도전의 첫 층 |
| `_towerPossessThisFloor` | int | `OneShot` 판정 |
| `_towerPool` | string[] | 이번 층 조건 호스트 목록(구조 몸도 여기서) |
| `_towerRule` | `TowerFloor` | 이번 층 |

### 4-3. 저장 (`UserData` 버전 올림)

| 데이터 | 타입 | 비고 |
|---|---|---|
| `towerClearedFloor` | int | 돌파한 마지막 층(5의 배수). 다음 도전은 +1 층부터 |
| `towerBestFloor` | int | 가장 높이 간 층(기록 · 로비 표시) |
| `towerRewardedFloor` | int | 첫 돌파 보상을 받은 마지막 층 — 실패한 도전에서 넘은 층도 여기까지 지급했다 |

### 4-4. 보상 (가안 — 모드 확정 뒤 숫자를 정한다)

- **층을 처음 넘을 때** 그 층 조건에 맞는 호스트 조각 1(보스 층 3). 다시 넘을 때는 없다(`towerRewardedFloor` 로 막는다).
  - 조각은 그 층 조건 호스트 중 무작위 · 해금된 몸 쪽 무게 2배 · 지급은 `PlayerDataService.AddShards`(다 키운 몸은 정수로 — 본편 규칙).
  - **실패한 도전에서도 처음 넘은 층의 보상은 준다** — 구간을 못 깼다고 0 이면 막힌 사람이 키울 재료도 못 얻는다.
- 입장 횟수 · 특수 재화(경제 문서의 「혼석」) · 주간 보상은 모드 확정 뒤(7절).

---

## 5. 연출 · 사운드 · 글자

> 새 그림 없음 — 본편 몸 · 잔몹 · 보스 · 방 · 문을 쓴다.

| 순간 | 연출 | 사운드(큐) |
|---|---|---|
| 층 입장 | 화면이 위로 한 칸 올라가는 전환(문 → 다음 층) · 띠 「13층 · 마법의 무리」 | `chapter.{n}.normal` 곡 유지 |
| 몸 놓고 옴 | 앞 층 몸은 화면 전환과 함께 사라짐(연출 없음 — 비용이 없으니 알릴 것도 없다) | — |
| 조건 잠김 | `NoLeave` · `OneShot` 층 입장 때 자물쇠 아이콘이 HUD 로 날아가 꽂힘 | `event.offer` |
| 빼앗기 막힌 적 호스트 | 「몸」 표식 없음(본편 잡몹처럼 보인다) | — |
| 보스 층 | 본편 보스 등장 | `boss.*` · `chapter.{n}.boss` |
| 구간 돌파 | 결과 창 — 진행 점 5개가 차례로 채워짐 | `stage.clear` |
| 실패 | 결과 창 — 쓰러진 층 조건 한 줄 | `stage.fail` |

**언어팩 키** (한 · 일 · 영 3언어 전부 넣고 `GetPreferredValues` 로 칸 확인):
`ui.tower.title` · `ui.tower.segment` 「{0} ~ {1}층」 · `ui.tower.best` · `ui.tower.fit` 「맞는 호스트 {0} · 키운 ★{1}」 · `ui.tower.start` 「{0}층부터 도전」 ·
`ui.tower.floor` 「{0}층 · {1}」 · `ui.tower.possess_left` · `ui.tower.result.clear` · `ui.tower.result.fail` 「{0}층에서 쓰러졌다」 · `ui.tower.result.hint` ·
`tower.rule.{rule}.name` / `.desc` (규칙 8종) · `tower.floor.{n}.name` (표의 층 이름)
— 로비 칸 `ui.lobby.mode.defense.name/.desc` 는 「망령의 탑」으로 바꾼다.

---

## 6. 의존 · 연결 · 개발 작업

### 6-1. 새로 만드는 파일

| 파일 | 내용 |
|---|---|
| `Assets/Scripts/Module/InGame/BattleDirector.Tower.cs` | `partial class BattleDirector` — `EnterTowerFloor(int)` · 몸 뽑기 · 규칙 적용 · 층 클리어 → 다음 층 · 구간 돌파 · 결과. 밤(`BattleDirector.Night.cs`) · 사신의 수확(`BattleDirector.Reap.cs`)과 같은 끼우기 |
| `Assets/Scripts/Module/InGame/Tower/TowerTable.cs` | 4-1 표 |
| `Assets/Scripts/Module/Lobby/TowerEntryPanel.cs` · `TowerFitPopup.cs` | 2-2 |
| `Assets/Scripts/Module/InGame/TowerResultPopup.cs` | 2-4 |
| `Assets/Scripts/Module/Events/TowerEvents.cs` | 6-3 |
| (밤과 같이 씀) `Assets/Scripts/Module/Common/RunModeContext.cs` | `RunMode` 에 `Tower` · `TowerStartFloor` 를 더한다 |

### 6-2. 본편 코드를 고치는 곳 (탑일 때만 갈라지게 — 본편 동작은 그대로)

밤 문서 6-2절과 겹치는 자리는 **같은 갈림길에 탑을 한 줄 더하는 것**이다.

| 자리 | 바꿀 것 |
|---|---|
| `BattleDirector.cs:445-468` 판 시작 | 탑이면 `EnterStartHost` 건너뜀 · `EnterTowerFloor(시작 층)` |
| `BattleDirector.cs:9705-9723` 문에 닿음 | 탑이면 `EnterTowerFloor(층 + 1)` (보스 층은 문 대신 3-9 돌파) |
| 층 넘어갈 때 몸 놓기 | 비용 없이 `_host` 를 비우는 함수 `DropHostFree()` — `LeaveHostCollapse` 를 쓰면 −8% 가 나간다 |
| `Unit.IsPossessable`(`Unit.cs:264`) · `HasPossessCondition` | `TowerBlocked` 면 `false` · `OneShot` 층에서 빙의를 이미 했으면 `false` |
| `CanLeaveHost`(`BattleDirector.SoulFx.cs:680`) | `NoLeave` 층이면 `false` |
| `TickRescue`(`BattleDirector.cs:3707`) | 탑이면 구조 몸을 `_towerPool` 에서 · `OneShot` 층이면 끔 |
| `KillEnemy`(`BattleDirector.cs:7633` · 7670-7682) | 탑이면 `GainExp` · `GrantShards` · `DropGold` 끔 |
| `Finish(bool)`(`BattleDirector.cs:9744`) | 탑이면 4-4 보상 · 저장 · `TowerFinishedEvent` |
| 적 체력 배율(`SpawnHpMul` 4413줄 부근) | 탑이면 3-6 식 |
| `Assets/Scripts/Module/Lobby/LobbyMainUI.cs:111` | `ModeDefenseCard` → `TowerEntryPanel` 열기 |

### 6-3. 이벤트

| 이벤트 | 필드 | 발행 · 구독 |
|---|---|---|
| `TowerFloorEnteredEvent` | `EnteredFloor` · `SegmentStart` · `RuleKind` · `NameKey` · `DescKey` · `PossessLimit` · `IsLeaveLocked` | 층에 들어갈 때 → HUD |
| `TowerPossessLeftEvent` | `RemainingPossess` | `OneShot` 층에서 빙의할 때 → HUD |
| `TowerFinishedEvent` | `IsSegmentCleared` · `ReachedFloor` · `IsNewBest` · `FailedRuleKind` · `FailedFitTrained` · `ShardHostKeys[]` · `ShardCounts[]` · `PossessCount` · `LostCount` · `ElapsedSeconds` | 판 끝 → `TowerResultPopup` |

- 탑에서는 `StageFinishedEvent` · `RoomEnteredEvent` 를 발행하지 않는다(챕터 결과 창 · 챕터 방 UI 가 같이 반응하지 않게). `RoomEnteredEvent` 를 구독하는 곳이 많으면 대신 탑 여부를 실어 보내고 구독 쪽에서 거른다 — 구현 때 구독처를 세어 보고 정한다.

### 6-4. 만드는 순서와 확인

| 단계 | 할 일 | 확인 |
|---|---|---|
| 1 | `TowerTable` · 로비 입구 · `EnterTowerFloor` 로 1층 하나 | 유령으로 1층 입장 · 근거리 몸 3 · 잔몹 6 |
| 2 | 층 클리어 → 문 → 다음 층 · 몸 놓기(비용 0) · 에너지 이어짐 | 2층에서 유령으로 시작 · 에너지 그대로 |
| 3 | 규칙 8종 | 층마다 스크린샷 — 조건 밖 몸에 「몸」 표식 없음 · `NoLeave` 나가기 잠김 · `OneShot` 두 번째 빙의 안 됨 · 구조 없음 |
| 4 | 보스 층 · 구간 돌파 · 실패 · 저장 · 보상 | 5층 보스 처치 → 결과 → 다음 도전 6층부터 · 실패해도 처음 넘은 층 보상 |
| 5 | 되풀이(11층~) · 세기 식 | 11층 = 1층 규칙 · 체력 ×2.5 |
| 6 | 입장 패널 · 맞는 호스트 표시 · 결과 힌트 | 「키운 ★0」 빨간색 · 실패 시 쓰러진 층 조건 한 줄 |
| 7 | 3언어 글자 | 한 · 일 · 영 전 문장이 칸에 들어감 |
| 8 | 밸런스(6-5) | 목표 표를 맞춤 |

### 6-5. 밸런스 목표 (시험판 실측 → 유니티에서 맞출 목표)

시험판 자동 조종, 구간마다 5번 도전. 시험판 수치는 본편과 다르므로 **값이 아니라 결과를 맞춘다.**

| 구간 | 전부 키움 | 안 키움 | 유니티 목표 |
|---|---|---|---|
| 1~5층 | 5/5 | 2/5 (5층 보스에서 실패) | 첫 구간은 안 키워도 가끔 깬다 — 탑을 맛보는 구간 |
| 6~10층 | 3/5 (9층 실패) | 0/5 (9층) | 키우면 절반 이상 |
| 11~15층 | 4/5 | 1/5 | |
| 16~20층 | 2/5 (19층) | 0/5 | |
| 21~25층 | 4/5 | 0/5 | |
| 26~30층 | 2/5 (29층) | 0/5 | 키워도 어렵다 — 끝없는 목표 |

**일부만 키우면 막히는 층이 생긴다** (「여러 호스트를 키워야 깨진다」의 확인):

| 키운 것 | 1~5층 | 6~10층 | 11~15층 |
|---|---|---|---|
| 근거리 호스트만 | 5/5 | 2/5 (9층) | 5/5 |
| 무기 속성만 | 5/5 | 2/5 | **2/5 — 15층 크러셔(마법 몸만)에서 3번 실패** |
| 원거리 호스트만 | 5/5 | 3/5 | **2/5 — 15층에서 3번 실패** |

- **9층 「빙의는 한 번」이 벽이다**(키워도 3/5). 의도대로 「첫 몸 고르기 퀴즈」지만, 유니티에서 키운 몸 돌파율이 50% 밑이면 이 층 잔몹 수부터 줄인다.
- 맞추는 순서: ① 층별 잔몹 수 ② 체력 식(3-6) ③ 층 사이 회복(`FloorHealPercent`, 기본 0). 본편 값(유령 감소 · 상성 · 빙의)은 건드리지 않는다.

---

## 7. 미정 · 결정 필요

| # | 무엇 | 지금 기본안 |
|---|---|---|
| 1 | **진행 방식** | 5층 구간 + 체크포인트(이 문서). 경제 문서 6-2절의 옛 안은 「끝없이 오르기 · **주 1회 초기화** · 도달 층 보상」 — 초기화를 넣으면 매주 1층부터. 지금 안은 초기화 없이 쌓이고, 주간 경쟁이 필요하면 나중에 「시즌 탑」을 따로 둔다 |
| 2 | 입장 횟수 · 특수 재화(혼석) · 상점 | 모드 확정 뒤(10-14) |
| 3 | 보상 숫자(층 1 · 보스 층 3) | 가안 |
| 4 | 층 표 늘리기 | 10층 한 바퀴 되풀이로 시작. 라이브 때 20 · 30층 줄을 새로 써서 보스(`python` · `robot_snakes` · `guardian`) · 규칙 조합을 다양하게 |
| 5 | 층 사이 회복 | 0 (에너지가 이어지는 게 난이도). 너무 어려우면 5~10% |
| 6 | 코만도(미사일) 거리 분류 | 원거리 — 밤 문서와 같이 한 곳(`RangeClassOf`)에서 정한다 |
| 7 | 레벨업 카드 | 없음(3-8). 탑이 단조로우면 「구간 시작 때 카드 1장」 정도만 검토 |
| 8 | 층 전환 연출 | 문 → 위로 한 칸 올라가는 전환. 화면 그림은 디자인 단계에서 |

---

## 개정 이력

| 버전 | 날짜 | 변경 내용 |
|---|---|---|
| 1.0 | 2026-10-15 | 최초 작성 — 시험판(AVSR_Night.html 탑 탭) 층별 · 구간별 측정 + 본편 코드 재사용 지점 |
