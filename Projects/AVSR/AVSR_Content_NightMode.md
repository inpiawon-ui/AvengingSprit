# 콘텐츠 세부 기획서 — 망령의 밤 (요일 서바이벌 모드)

- **콘텐츠명**: 망령의 밤 · **카테고리**: 추가 모드(반복 콘텐츠) · **우선순위**: P1
- **Version**: `1.3` · **Last Updated**: `2026-10-15` · **Status**: 개발 스펙 — 시험판으로 재미 검증을 마쳤고, 이 문서대로 유니티에 옮긴다
- **시험판**: `Projects/AVSR/ModeLab/AVSR_Night.html` (브라우저로 바로 열림) · 원본 `night_src.html` · 굽기 `python build.py night_assets.json night`
- **UI 시안**: [`wireframes/AVSR_Screen_Modes.html`](wireframes/AVSR_Screen_Modes.html) (화면 N1~N7) · 화면 흐름 · 요소 이름 정본: [`wireframes/AVSR_Screen_Modes.md`](wireframes/AVSR_Screen_Modes.md)
- **관련**: [`AVSR_Content_Economy.md`](AVSR_Content_Economy.md) 6절(신규 모드) · `Assets/Scripts/Module/CLAUDE.md`
- **이 문서의 원칙**: **새 전투 규칙을 만들지 않는다.** 빙의 · 경직 · 상성 · 성장 · 레벨업 카드 · 구조 몸 · 유령 에너지는 본편 코드를 그대로 쓴다.
  모드가 바꾸는 것은 **판(아레나) · 등장 순서 · 목표 · 보상**뿐이다. 본편과 다른 값은 전부 `NightModeTable` 한 곳에 둔다.

---

## 0. 한 줄 요약

**유령으로 들어가 오늘의 호스트 셋 중 하나를 빼앗고, 5분 동안 내려오는 무리를 버틴 뒤 보스를 잡는다. 보상은 처치 수.**
요일마다 몸으로 나오는 호스트가 정해져 있다(월 근거리 · 화 원거리 …) — 그 호스트를 키워 둔 사람이 더 많이 잡는다.

### 사용자(PD) 결정 원문 — 이 문서의 근거

| 날짜 | 결정 |
|---|---|
| 10-08 | 「1. 키운 캐릭터를 하면 + 효과 2. 빙의 · 상성으로 + 효과 3. 속성을 잘 이용하는 플레이 — 없던 룰 금지 4. 우리 게임 특성을 살린 모드」 |
| 10-13 | 「뱀서류처럼 매시간 몬스터가 계속 내려오는 모드 · 요일마다 특정 조건(오늘은 무슨 직업)」 |
| 10-13 | 「입장할 때 무조건 유령으로 · 원하는 직업 기준으로 랜덤 호스트를 뿌리고 · 시작할 때 빙의를 한 번 해서 시작」 |
| 10-14 | 「처음 3개의 호스트 중 하나를 고르면 2개는 사라지고 · 몬스터는 기본적으로 잔몹 · 중간중간 호스트가 나올 때는 스케일을 키우거나 빙의할 수 없게 · 처치 횟수를 기록해서 보상」 |
| 10-14 | 입장 횟수 · 모드별 재화는 **모드를 다 만든 뒤에** 정한다. 지금은 재미 검증 |
| 10-15 | 「**밤도 하루 3번으로**」(탑과 같이 — 이기든 지든 1번 줄어든다) |

### 4원칙이 어디서 지켜지나

| 원칙 | 이 모드에서 |
|---|---|
| 키운 캐릭터 + 효과 | 요일 조건 호스트가 몸으로 나온다 → 그 호스트의 골드 강화 · 숙련도 · 스킬이 그대로 걸린다(본편 `EnterHost`). 시험판: 키운 몸 처치 평균 **286** vs 안 키운 몸 **104** |
| 빙의 · 상성 + 효과 | 1분마다 무리 속성이 바뀐다. 상성이 맞는 대형 호스트를 경직시켜 갈아타면 피해 ×1.3 · 체력 100% · 스킬 가득 |
| 속성을 잘 쓰는 플레이 | 「지금 몸으로 버틸까 / 대형 호스트를 깎아서 갈아탈까(−8%) / 그냥 잡을까」를 매분 판단 |
| 게임 특성 | 유령 입장 → 빙의로 시작 · 몸을 잃으면 유령 → 구조 몸 · 경직으로 빼앗기 — 전부 본편 규칙 |

---

## 1. 목적 (플레이어 경험)

- **매일 들어올 이유**: 요일마다 조건이 바뀐다. 「오늘은 원거리 날이니 설녀를 키워 둘걸」이 다음 성장 목표가 된다.
- **성장을 숫자로 확인하는 곳**: 처치 수가 곧 실력 + 성장이다. 어제보다 많이 잡으면 최고 기록이 갱신된다.
- **본편보다 짧고 굵게**: 한 판 5분 안팎. 무리를 쓸어 담는 맛 + 대형 호스트를 깎아 빼앗는 결정.
- **써 본 적 없는 몸을 맛보는 곳**: 요일 조건 안에서 무작위로 나오므로 안 키운 몸도 들어온다 — 「이 몸 키우면 세겠다」를 느끼게 한다.

---

## 2. 화면 구성 (UI 요소)

> **화면 흐름 · 요소 트리 · 위치는 화면 설계서 [`wireframes/AVSR_Screen_Modes.md`](wireframes/AVSR_Screen_Modes.md) 4절이 정본이다**(10-15 시안). 시안에서 더한 것: 입장 패널의 **요일 줄**(`NightDayStrip`, 다른 요일 미리보기) · 한 판 흐름 4칸 · 인게임 위 가운데 **밤 판**(`NightPlate` — 시간 · 처치) · 시작 안내 띠 · 골드 칸 숨김 · **입장 점 3개**(`NightEntryPips`, 탑과 같은 모양).

> 이름 = 프리팹 GameObject 이름 = 코드 바인딩 키(세 곳 동일). 글자는 전부 언어팩 키(8절). 그림에 글자를 넣지 않는다.

### 2-1. 로비 입구 — 기존 `ModeSurvivalCard` 를 쓴다

- 로비 `Assets/Scripts/Module/Lobby/LobbyMainUI.cs:110` 의 `ModeSurvivalCard` 는 지금 `NotifyNotReady` 로 막혀 있다. 이 칸을 **망령의 밤 입구**로 연다.
- 칸 글자: 이미 있는 `ui.lobby.mode.survival.name` 을 「망령의 밤」으로, `.desc` 를 오늘 조건(예: 「오늘: 원거리의 날」)으로 바꾼다.
- 누르면 `NightEntryPanel` 이 열린다.

### 2-2. `NightEntryPanel` (로비에서 여는 `~Panel`)

| 요소 | 설명 | 상태 · 입력 |
|---|---|---|
| `NightDayText` | 「화요일 · 원거리의 날」 | 오늘 요일(4-3 초기화 기준) |
| `NightRuleText` | 「원거리 호스트만 몸으로 나온다」 | 요일 표의 설명 |
| `NightHostList` > `NightHostSlot` ×N | 오늘 몸으로 나올 수 있는 호스트 초상 | 키운 몸(숙련도 ≥ 1)은 금테 + ★, 안 키운 몸은 흐리게 + 「스킬 봉인」 |
| `NightBestText` | 「이 요일 최고 처치 238」 | 요일별 최고 기록(저장) |
| `NightRewardText` | 「처치 50마다 조각 1 · 보스 처치 +3」 | 4-4 보상 규칙 |
| `NightStartButton` | 시작 | 누르면 모드를 걸고 인게임 씬으로 |
| `NightCloseButton` | 닫기 | |
| `PopupParent` | 규칙(05_prefabs) | |

- **하루 입장 3번**(10-15) — 입장 점 3개 · 「＋ 추가 입장」 · 시작 버튼 아래 「입장 1번 사용」. 규칙은 3-2 · 4-5.

### 2-3. 인게임 HUD — `InGameMainUI` 에 노드 추가

본편 HUD(유령 체력 · 몸 체력 · 스킬 · 경험치 막대 · 일시정지)는 그대로 쓴다. 밤 모드일 때만 아래를 켠다. **본편에서는 꺼 둔다.**

| 요소 | 설명 | 갱신 |
|---|---|---|
| `NightTimerText` | 「2:47」 경과 시간. 4:30부터 빨간색 | 1초마다 |
| `NightKillText` | 「처치 132」 | 처치할 때 |
| `NightWaveAffinityIcon` · `NightWaveText` | 지금 무리 속성 보석 + 「무리: 마법」 | 속성이 바뀔 때 |
| `NightCounterHint` | 「무기 몸이 유리 ▲」 — 지금 몸이 불리할 때만 | 속성 · 몸이 바뀔 때 |
| `NightNextEventText` | 「대형 호스트까지 0:12」 / 「보스까지 0:40」 | 1초마다 |
| `NightBannerText` | 화면 가운데 띠 — 「무리가 파워로 바뀐다」 · 「대형 설녀 등장」 · 「보스 크러셔」 | 사건마다 1.6~2초 |

- 레벨업 카드 창은 **본편 `BuffOfferEvent` 화면을 그대로** 쓴다.
- 대형 호스트의 경직 게이지 · 빙의 표식은 **본편 표식(`StaggerProgress`)** 을 그대로 쓴다.

### 2-4. `NightResultPopup` (인게임 결과 — Global Popup)

| 요소 | 설명 |
|---|---|
| `NightResultTitle` | 「밤을 버텼다」(보스 처치) / 「쓰러졌다」(유령 에너지 0) |
| `NightResultKillText` | 처치 수 — 가장 크게. 최고 기록이면 「최고 기록!」 띠 |
| `NightResultTimeText` | 버틴 시간 · 도달 레벨 |
| `NightResultShardList` > `NightResultShardItem` ×N | 받은 조각(호스트 초상 + ×개수) |
| `NightResultStatText` | 빙의 N · 몸 잃음 N · 스킬 N |
| `NightRetryButton` · `NightLobbyButton` | 다시 · 로비로 |

---

## 3. 기능 · 규칙 (동작)

### 3-1. 한 판의 흐름 (기본값 — 모든 시각 · 수치는 `NightModeTable`)

```
0:00  유령으로 입장 · 시작 몸 3 · 무리 첫 스폰은 2초 뒤
      ↳ 하나에 빙의하면 나머지 둘은 사라진다
0:40  대형 호스트 ①   (이후 45초마다: 1:25 · 2:10 · 2:55 · 3:40 · 4:25 · 5:10 …)
1:00  무리 속성 전환 + 회복 15%     (2:00 · 3:00 · 4:00 같음)
2:00  대장 ①  (무리 속성의 호스트)
3:30  대장 ②
5:00  보스 등장 · 무리 스폰 멈춤(대형 호스트는 계속)
      ↳ 보스 처치 = 클리어 · 유령 에너지 0 = 실패
```

### 3-2. 입장 — 언제나 유령 · 하루 3번

0. **입장 횟수**(10-15): 매일 05:00 에 **3번**으로 채운다(남은 것은 넘어가지 않는다). **판을 시작할 때 1 줄어든다 — 이기든 지든.**
   입장 0 에서 시작을 누르면 젬 추가 입장 팝업(`NightBuyPopup`, 탑 T3 과 같은 모양 · 값 · 상한은 4-5).
1. 로비 `NightStartButton` → 입장 −1 저장 → `RunModeContext.Current = RunMode.Night` (+ 오늘 요일)를 걸고 `ISceneManager.LoadAsync(InGame)`.
   - 지금 판 시작은 챕터 번호를 요청에 안 넘기고 `IPlayerDataService.SelectedChapter` 를 판이 읽는다(`ChapterHostPanel.cs:926`).
     모드도 같은 방식으로 **저장하지 않는 정적 값**으로 넘긴다. 판이 끝나면 `RunMode.Chapter` 로 되돌린다.
2. 판 시작(`BattleDirector.cs:445-468`)에서 밤이면:
   - `SpawnGhost()` 만 하고 **`EnterStartHost()` 를 건너뛴다**(로비에서 데려온 몸 없음).
   - `EnterRoom(0)` 대신 `EnterNightArena()` — 아레나 배치(3-3).
3. **시작 몸 3**: 오늘의 호스트 목록(3-4)에서 **서로 다른 셋**을 뽑아 화면 아래쪽에 좌 · 중 · 우로 세운다.
   - 해금된 몸이 목록에 있으면 **적어도 하나는 해금된 몸**(키운 몸이 들어갈 자리를 보장).
   - 시작 몸은 공격하지 않는다(무리가 오기 전 2초 동안 고르는 시간). 무리가 나온 뒤에는 보통 적처럼 움직인다.
4. **하나에 빙의하면 나머지 둘은 사라진다** — 연기와 함께 제거(처치로 안 센다 · 조각 없음 · 경험치 없음).
   - 시작 몸에 빙의할 때는 본편 빙의와 같다: 체력 100% · 무적 1.25초 · 스킬 게이지 가득 · 성장 적용.

### 3-3. 아레나

- 방 하나에서 끝까지 한다(문 · 다음 방 없음). 화면 크기는 본편 방과 같다.
- 바닥: 정본 방 표에서 **장애물이 적은 방 하나**를 지정(`NightModeTable.ArenaRoomId`). 장애물이 많으면 무리에 끼여 빠져나갈 길이 없다.
- 배경은 요일마다 바꿀 수 있게 칸을 둔다(`DayEntry.FloorKey`). 처음엔 전부 같은 바닥으로 시작해도 된다.

### 3-4. 요일 조건 — 몸으로 나오는 호스트

요일은 **기기 시각 기준, 초기화 시각 4-3**. 사신(`death`)은 히든이라 어느 요일에도 나오지 않는다(`IsHiddenHost`).

| 요일 | 이름 | 조건 | 호스트 (지금 표 기준) |
|---|---|---|---|
| 일 | 자유의 날 | 전부 | 사신 뺀 22종 |
| 월 | 근거리의 날 | 근거리 | 아마존 · 아마존 정예 · 닌자(사슬) · 슬러거 · 흡혈귀 · 샐러맨더 · 드라군 · 청룡 · 구루 |
| 화 | 원거리의 날 | 원거리 | 코만도(레이저) · 코만도(수류탄) · 화이트 위저드 · 영매 · 로봇 · 설녀 · 코만도(미사일) |
| 수 | 중거리의 날 | 중거리 | 갱스터 · 폭력배 · 호퍼 · 호퍼(기관단총) · 코만도(기관총) · 닌자 |
| 목 | 파워의 날 | 파워 | 슬러거 · 구루 · 닌자(사슬) · 코만도(수류탄) · 코만도(미사일) · 코만도(레이저) · 로봇 |
| 금 | 무기의 날 | 무기 | 갱스터 · 폭력배 · 호퍼 · 호퍼(기관단총) · 코만도(기관총) · 닌자 · 아마존 · 아마존 정예 |
| 토 | 마법의 날 | 마법 | 드라군 · 설녀 · 청룡 · 샐러맨더 · 흡혈귀 · 화이트 위저드 · 영매 |

- **속성**은 `AffinityRule.KindOf(key)`(`BattleDirector.Affinity.cs:84`)를 그대로 쓴다.
- **거리 분류**는 지금 표에 칸이 없다 → `HostEntry` 에 칸을 새로 두지 않고 `NightModeTable.RangeClassOf` 목록으로 둔다(모드 전용 분류).
  기본 분류는 사거리 배율(`_rangeMul`)로 잡았다: **0.6 이하 근거리 · 0.7~1.0 중거리 · 1.05 이상 원거리**.
  - ⚠ 코만도(미사일)은 배율 0.5 지만 유도 폭격이라 **원거리로 분류**했다. 확인 필요(7절).
- 해금 여부와 상관없이 나온다 — 안 가진 몸도 판 안에서는 쓸 수 있다(본편과 같음). 안 키운 몸은 스킬 봉인 · 강화 0.

### 3-5. 무리 (잔몹)

- 무리는 **잔몹만**(`IsHostBody` 아님 — 경직이 안 쌓이고 빼앗을 수 없다). 지금 무리 속성의 잔몹 목록(`NightModeTable.TrashOf[속성]`)에서 고른다.

| 속성 | 잔몹(기본안 — 설치물 · 포탑 제외) |
|---|---|
| 무기 | `bat` · `scrapgunner` · `roadwarden` · `mantis` · `mole` |
| 파워 | `boar` · `armadillo` |
| 마법 | `skeleton` · `mushroom` · `mummy` · `spider` · `sludge` · `coilwalker` |

- **스폰 위치**: 60% 화면 위 가장자리(가로 무작위) · 20% 왼쪽 · 20% 오른쪽(위쪽 절반 높이). 플레이어 몸 반경 2칸 안에는 세우지 않는다.
- **스폰 간격 · 수 · 세기** (`t` = 경과 초):

| 값 | 식 | 0:00 | 1:00 | 3:00 | 5:00 |
|---|---|---|---|---|---|
| 간격(초) | `max(1.6, 3.4 − t/160)` | 3.4 | 3.0 | 2.3 | 1.6 |
| 한 번에 | `1 + floor(t/75)` | 1 | 1 | 3 | 5 |
| 체력 배율 | `1 + t/300` | ×1 | ×1.2 | ×1.6 | ×2.0 |

- 잔몹 기본 능력치는 **1챕터 잡몹 값**(`SpawnHpMul` · `SpawnAtkMul` 의 1챕터 값)에 위 배율을 곱한다.
- **상한**: 아레나에 동시에 살아 있는 잔몹 `NightModeTable.EnemyCap` = **30**. 본편 상한(`MaxRoomUnits` 14 · `_roomEnemyCap` 12)은 밤에서는 이 값으로 바꿔 읽는다.
  - 무리는 처음부터 계속 나온다. 상한은 「못 잡을 때 쌓이는 끝」이다 — 30마리가 쌓이기 전에 못 잡으면 그 판은 이미 진 판이다(PD 10-15).
  - 시험판 실측: 잘 잡을 때 평균 10~27마리 · 밀릴 때만 30을 넘었다. 30이면 잘 잡는 판에는 영향이 없다.
  - 상한은 **잔몹에만**. 대형 호스트 · 구조 몸 · 대장 · 보스는 상한과 상관없이 나온다
    (시험판에서 몸까지 상한에 막혀 유령으로 떠돌다 에너지가 바닥나는 버그가 있었다).
  - ⚠ 30마리 + 대형 · 대장 · 보스 동시 — 폰 성능은 프로파일러로 확인(7절).
- 5:00 보스가 나오면 **잔몹 스폰을 멈춘다.** 이미 나온 잔몹은 남는다.
- 잔몹은 **골드를 떨구지 않는다**(`DropGold` 끔) — 이 모드의 보상은 처치 수(4-4). 재화는 모드 확정 뒤(7절).

### 3-6. 대형 호스트 — 크고 센 몸

10-14 PD 지시 「호스트가 나올 때는 스케일을 키우거나 빙의할 수 없게」의 구현.
⚠ 10-16 변경: 10-15 에 몸에서 스스로 나오는 탈출이 없어져(버튼은 필살기) 「몸에 든 채로 깎아 빼앗기」가 불가능해졌다.
PD 「죽으면 일단 유령이 되고, 유령이 되어서 빙의하는 건 있는데 그거면 안 돼?」 → **다른 몸과 같다 — 유령이면 바로 탄다.**

- **언제**: 0:40 첫 등장, 이후 45초마다(`BigHostFirst` · `BigHostInterval`).
- **누가**: 오늘의 호스트 목록에서 무작위 1. 위쪽 가장자리에서 걸어 들어온다(나오자마자 추격). 등장 띠 「대형 ○○ 등장」.
- **모습 · 세기**: `MarkAsHostBody()` + `MarkAsElite()` 표시 · 크기 ×1.35 · 체력 ×`(5 + t/90)` · 공격 ×1.3.
- **빼앗기**: 본편 그대로 — 내 몸이 쓰러져 유령이 되면 바로 빙의할 수 있다. 빼앗은 몸은 **보통 크기 · 보통 능력치**(`EnterHost` 가 호스트 표로 다시 만든다).
- 그냥 잡아도 된다 — 경험치 ×8 · 처치 수 +1.
- `BigHostStaggerMul` 표 칸은 지금 쓰지 않는다(깎아 빼앗기를 되살리면 다시 쓴다).

### 3-7. 무리 속성 전환 + 회복 (1분마다)

- 1:00 · 2:00 · 3:00 · 4:00 에 무리 속성이 **지금과 다른 속성으로** 무작위로 바뀐다. 첫 속성은 무작위.
  - 띠 「무리가 파워로 바뀐다」 · 로그. `NightCounterHint` 가 「마법 몸이 유리 ▲」를 띄운다.
  - 이미 나와 있는 잔몹은 그대로. 새로 나오는 무리만 바뀐다.
- 같은 순간 **회복 15%** — 본편 「방을 비우면 회복」(`HealOnClear(int percent)`, `BattleDirector.cs:8962`)을 그대로 부른다(유령 체력 + 몸 체력 같은 비율).
  - 시험판에서 이 회복이 없으면 유령 에너지가 깎이기만 해서 원거리 · 중거리 날은 키운 몸도 3분을 못 넘겼다.

### 3-8. 대장 · 보스

- **대장** 2:00 · 3:30: 지금 무리 속성의 호스트 하나(오늘 조건과 무관). 본편 **중간보스**(`SpawnMidBoss`, `BattleDirector.MidBoss.cs:112`)의 몸 · 패턴 · 취약 창을 쓴다.
  - 빼앗을 수 없다(`NotPossessable`). 체력 배율 `NightModeTable.ChiefHpMul`(시작값: 1챕터 중간보스 ×1.0, 시험 뒤 조정).
  - 처치하면 **회복 25%**(`HealOnClear`) · 경험치 ×12.
- **보스** 5:00: 요일 표의 `DayEntry.BossKey`(기본: 무작위 `crusher` · `kingpin` · `python`). 본편 보스방의 보스 세우기(`EnterRoom` 3055-3136줄)를 `SpawnBoss(bossKey)` 로 떼어 내어 같이 쓴다.
  - 체력 ×0.9(시험판 값). 보스는 경험치를 안 준다(본편과 같음 — 막타 뒤 레벨업 창이 연출을 가린다).
  - **보스 처치 = 클리어.**

### 3-9. 몸을 잃었을 때 · 유령 에너지

모두 본편 값 그대로. 밤에서 바꾸는 것은 구조 몸의 지연 · 쿨다운뿐이다.

| 일 | 본편 값 | 밤 |
|---|---|---|
| 유령 최대 체력 | 100 (`_ghostHpMax`) | 같음 |
| 유령일 때 감소 | 초당 3.35 (`_ghostDrainPerSecond`) | 같음 |
| 몸이 죽음 | −20% (`_ghostDeathCostPercent`) | 같음 |
| 스스로 나옴 | −8% (`AffinityRule.LeaveCostPercent`) | 같음 |
| 구조 몸 지연 | 3초 (`RescueDelaySeconds`) | **1초** (`NightModeTable.RescueDelay`) |
| 구조 몸 쿨다운 | 20초 (`RescueCooldownSeconds`) | **0초** — 무리는 계속 온다. 기다리는 동안 맞을 몸도 없이 에너지만 마른다 |
| 구조 몸 종류 | 아무 호스트 | **오늘의 호스트 목록**에서 무작위, 보통 크기 |

- 유령 에너지 0 → `Finish(false)`(본편 3668-3684) → 밤에서는 결과 창(3-12)으로.

### 3-10. 경험치 · 레벨업 카드

- 경험치 · 카드 뽑기는 본편 그대로: `GainExp` → `TickPendingLevelUp` → `OfferBuff`(`_buffTable.Draw` 3장) → `ChooseBuff`.
  - 경험치: 잔몹 `_expPerEnemy`(10) · 대형 호스트 ×8 · 대장 ×12. 레벨 필요치 `_expToLevelBase` 40 · 증가 45% (본편 값).
  - 이 값이면 5분에 Lv 10 전후 — 시험판(키운 몸 Lv 10)과 맞는다.
- **바꾸는 곳 두 군데**:
  1. 본편은 **방이 비어야** 카드 창이 뜬다(`BattleDirector.cs:7934`, `_enemies.Count > 0` 이면 보류). 밤은 방이 안 빈다 → **밤에서는 이 조건을 건너뛴다**(빙의 연출 중 · 몸이 쓰러지는 중일 때만 기다린다).
  2. 카드 창이 떠 있는 동안(`_awaitingBuff`) **전투 틱을 멈춘다**(적 이동 · 공격 · 스폰 시계 · 유령 감소 · 경과 시간 전부). 본편은 빈 방이라 필요 없었다.
- 카드는 판 전체에 걸린다(`RunBuffs`) — 지금 몸 · 앞으로 빼앗을 몸 모두. 태그 · 호스트 전용 카드는 본편처럼 그 몸일 때만 켜진다(`IsActiveFor`).

### 3-11. 성장 적용 · 상성 · 공격

- 전부 본편 그대로 — 따로 할 일 없음.
  - 성장: `EnterHost` 의 `LeveledHp/LeveledAtk` · `UpgradeMul` · `MasteryStatMulOf` · 스킬 봉인 `IsSkillSealed`.
  - 상성: `AffinityRule.WinDamageMul` 1.3 / `LoseDamageMul` 0.7.
  - 멈춰야 공격(`BattleDirector.cs:3911-3956`) · `MoveAttack` 호스트 예외 그대로.
- ⚠ **「멈춰야 공격」은 무리 모드와 부딪힌다** — 계속 도망치면 못 쏜다. 「빠졌다 → 멈춰 쏘고 → 다시 빠지는」 리듬이 이 모드의 손맛이다.
  이게 답답하면 규칙을 바꾸지 말고 **무리 밀도**(3-5 식)를 먼저 낮춘다. 카드 「빠른 재정비」(`StopDelay`)가 이 리듬을 돕는다.

### 3-12. 끝 · 결과

| 끝나는 경우 | 판정 | 결과 창 제목 |
|---|---|---|
| 보스 처치 | 클리어 | 「밤을 버텼다」 |
| 유령 에너지 0 | 실패 | 「쓰러졌다」 |
| 일시정지 → 포기 | 실패 | 「쓰러졌다」 |

- 보스가 나온 뒤에는 시간 제한이 없다 — 유령 에너지가 시계다(7절에 제한 여부).
- `Finish(bool)` 에서 밤이면: 챕터 진행도 · 챕터 상자 · 챕터 클리어 지급을 **하지 않고**, 4-4 보상 지급 → 최고 기록 저장 → `NightFinishedEvent` 발행 → `NightResultPopup`.
- 결과 창은 보스 막타 슬로우가 풀린 뒤에 띄운다(본편 `NoteBossDown` 순서 그대로).

### 3-13. 예외

- 판 도중 앱 종료 · 백그라운드 → 본편과 같다(판은 저장하지 않는다). 그날 기록 없음.
- 판 도중 날짜(초기화 시각)가 넘어가도 **입장할 때 요일**로 끝까지 한다.
- 시작 몸 셋을 고르지 않고 버티면 → 유령 감소로 30초 안에 끝난다. 별도 처리 없음.
- 오늘 목록이 3종보다 적으면(표를 고치다 생길 수 있음) → 있는 만큼만 세우고 경고 로그.
- 대형 호스트를 경직시킨 채 창이 닫히면 → 본편처럼 2초 쿨다운 뒤 다시 채울 수 있다. 체력 회복은 없다.

---

## 4. 데이터 (값 · 저장 항목)

### 4-1. `NightModeTable` (ScriptableObject — 새로 만든다)

- 위치 `Assets/BundleResource/TableData/NightModeTable.asset` · 주소 `TableData/NightModeTable` · 라벨 `label_tabledata`
- 코드 `Assets/Scripts/Module/InGame/Night/NightModeTable.cs` · 네임스페이스 `Game.Module.InGame`

| 칸 | 타입 | 기본값 | 쓰는 곳 |
|---|---|---|---|
| `ArenaRoomId` | string | (장애물 적은 방 지정) | 3-3 |
| `StartBodyCount` | int | 3 | 3-2 |
| `TrashFirstDelay` | float | 2 | 3-5 |
| `SpawnIntervalStart` · `SpawnIntervalSlope` · `SpawnIntervalMin` | float | 3.4 · 160 · 1.6 | 간격 = max(Min, Start − t/Slope) |
| `SpawnCountStep` | float | 75 | 한 번에 = 1 + floor(t/Step) |
| `TrashHpSlope` | float | 300 | 체력 배율 = 1 + t/Slope |
| `EnemyCap` | int | 30 | 잔몹 상한(동시에 살아 있는 수) |
| `TrashOf` | 속성별 string[] | 3-5 표 | 잔몹 목록 |
| `BigHostFirst` · `BigHostInterval` | float | 40 · 45 | 3-6 |
| `BigHostScale` · `BigHostHpBase` · `BigHostHpSlope` · `BigHostAtkMul` | float | 1.35 · 5 · 90 · 1.3 | 체력 ×(Base + t/Slope) |
| `BigHostStaggerMul` | float | 1.5 | 경직 기준 = 배율 전 체력 × 0.35 × 이 값 |
| `WaveInterval` · `WaveHealPercent` | float · int | 60 · 15 | 3-7 |
| `ChiefTimes` · `ChiefHpMul` · `ChiefHealPercent` | float[] · float · int | [120, 210] · 1.0 · 25 | 3-8 |
| `BossTime` · `BossHpMul` | float | 300 · 0.9 | 3-8 |
| `RescueDelay` · `RescueCooldown` | float | 1 · 0 | 3-9 |
| `ExpMulBigHost` · `ExpMulChief` | int | 8 · 12 | 3-10 (× `_expPerEnemy`) |
| `RangeClassOf` | (hostKey, 근/중/원)[] | 3-4 표 | 요일 조건 |
| `Days` | `DayEntry[7]` | 3-4 표 | 요일 |
| `KillsPerShard` · `ClearShardBonus` | int | 50 · 3 | 4-4 |
| `ResetHourLocal` | int | 5 | 4-3 |

`DayEntry`: `DayKey`(sun…sat) · `NameKey`(언어팩) · `Filter`(All / Range(근·중·원) / Affinity(파워·무기·마법)) · `BossKey`(빈칸 = 무작위) · `FloorKey`(빈칸 = 아레나 기본)

> 코드에 숫자를 적지 않는다 — 위 칸만 읽는다. 본편 값(유령 감소 · 경직 · 상성 · 경험치)은 본편 자리에서 읽는다(복사하지 않는다).

### 4-2. 판 안 상태 (저장 안 함)

| 데이터 | 타입 | 비고 |
|---|---|---|
| `RunModeContext.Current` · `NightDay` | enum · int | 로비 → 인게임으로 넘기는 정적 값. 판이 끝나면 `Chapter` 로 되돌림 |
| `_nightElapsed` | float | 카드 창 동안 멈춘다 |
| `_nightKills` | int | 잔몹 · 대형 · 대장 · 보스 전부 1. 시작 몸이 사라지는 것 · 구조 몸 사라짐은 안 센다 |
| `_nightWaveAffinity` | Affinity | |
| `_nightNextSpawn` · `_nightNextBigHost` · `_nightNextWave` | float | 시계 |
| `_nightChiefIndex` · `_nightBossOut` | int · bool | |

### 4-3. 저장 (`UserData` 버전 올림)

| 데이터 | 타입 | 비고 |
|---|---|---|
| `nightBestKills` | int[7] | 요일별 최고 처치 수 |
| `nightBestTime` | int[7] | 요일별 최고 버틴 초(동점 비교 · 표시용) |
| `nightClears` | int | 누적 클리어(나중 업적 · 배틀패스용) |
| `nightDayId` | int | 오늘(yyyyMMdd, 05:00 기준). 바뀌면 입장 · 젬 추가 횟수를 채운다 |
| `nightEntries` | int | 오늘 남은 입장(무료 + 산 것) |
| `nightBoughtToday` | int | 오늘 젬으로 산 횟수 |
| `nightLastRefill` | long | 마지막으로 채운 시각 — 시각 되돌림 감지(탑과 같은 규칙) |

- 요일 계산: `IResetClock.DayKey` 의 요일 — 하루 경계는 **일본 시각 오전 5시**(PD 10-15 「오전 5시로 통일」, 상점 · 탑 · 밤 같은 값). `NightModeTable.ResetHourLocal` 칸은 쓰지 않는다.
  서버가 붙기 전까지 시계 돌리기는 막지 못한다(TBD-SRV) — 요일을 골라 들어가는 것뿐이라 피해가 작다.

### 4-4. 보상 (가안 — 10-14)

- **처치 50마다 오늘의 호스트 조각 1** + **보스 처치 +3**.
  - 조각은 오늘 목록의 호스트들에게 무작위로 나눠 준다. 가진 몸(해금) 쪽에 무게 2배.
  - 지급은 본편 창구 `PlayerDataService.AddShards(hostKey, n)`(`PlayerDataService.cs:341`) — 다 키운 몸에게 온 조각은 정수로 바뀐다(본편 규칙).
- 본편의 판 안 조각(잡은 몸 1 · 쓰다 잃은 몸 3, `GrantShards`)은 **밤에서는 끈다.**
  - 이유: 밤은 구조 몸이 1초마다 다시 오므로 몸을 일부러 잃으면 조각이 쌓인다. 보상은 처치 수 하나로 센다.
- 실패해도 그때까지 처치 수만큼 받는다.
- 모드 재화 · 골드는 **모드를 다 만든 뒤에** 정한다(10-14). 결과 창에는 조각만 나온다.

### 4-5. 입장 (10-15)

- 무료 하루 **3번** · 젬 추가 하루 **3번까지** 50 → 100 → 200 젬(탑과 같은 값 · 가안 — 상점 붙일 때 정한다).
- 입장 · 젬 · 하루 넘김은 탑의 `TowerProgressService` 와 같은 틀로 `NightProgressService` 에 둔다(하루 기준은 `IResetClock`(`Assets/Scripts/Module/Common/Store/ResetClockModule.cs` — `DayKey` · `UntilNextDay` · `WeekKey` · `UntilNextWeek`, 값은 GameConfig 「하루 경계」 칸. 아웃게임 세션이 만든 것을 같이 쓴다)).
- `NightModeTable` 칸: `FreeEntriesPerDay` 3 · `BuyEntriesMax` 3 · `BuyEntryGem` [50, 100, 200].

---

## 5. 연출 · 사운드

> 새 그림은 없다(본편 몸 · 잔몹 · 보스 · 바닥 · 카드 창을 쓴다). 연출은 본편 공용 리소스를 같이 쓴다(공용 예고 · 노티).

| 순간 | 연출 | 사운드(큐 — 표에 있는 것) |
|---|---|---|
| 입장 | 유령이 아레나 아래에서 떠오름 · 띠 「화요일 · 원거리의 날」 | `chapter.{n}.normal` 곡 |
| 시작 몸 고름 | 본편 빙의 연출 · 나머지 둘은 연기로 사라짐 | `event.possess` |
| 대형 호스트 등장 | 띠 + 위 가장자리에서 걸어 들어옴 · 발밑 금테(엘리트 표시) | `event.emergency` |
| 대형 경직 | 본편 경직 표식(게이지 · 빙의 표식) | 본편 그대로 |
| 무리 속성 전환 | 띠 「무리가 파워로 바뀐다」 · HUD 보석 바뀜 · 회복 숫자 | `event.bossphase` |
| 대장 · 보스 | 본편 중간보스 · 보스 등장 연출 | `boss.*` · `chapter.{n}.boss` 곡 |
| 레벨업 | 본편 카드 창 | `event.levelup` |
| 결과 | 처치 수가 0부터 올라가는 숫자 · 최고 기록 띠 | `stage.clear` / `stage.fail` |

- 연출로 게임을 멈춘 듯 만들지 않는다(방 어둡게 · 적 가라앉히기 금지). 멈추는 것은 카드 창뿐이다.

---

## 6. 의존 · 연결 · 개발 작업

### 6-1. 새로 만드는 파일

| 파일 | 내용 |
|---|---|
| `Assets/Scripts/Module/InGame/BattleDirector.Night.cs` | `partial class BattleDirector` — `EnterNightArena` · `TickNight(dt)` · 스폰 · 대형 호스트 · 속성 전환 · 대장 · 보스 · 처치 수 · 결과. 사신의 수확(`BattleDirector.Reap.cs`)과 같은 끼우기 방식 |
| `Assets/Scripts/Module/InGame/Night/NightModeTable.cs` | 4-1 표 |
| `Assets/Scripts/Module/Common/RunModeContext.cs` | `RunMode { Chapter, Night }` · `Current` · `NightDay` (저장 안 하는 정적 값) |
| `Assets/Scripts/Module/Lobby/NightEntryPanel.cs` | 2-2 |
| `Assets/Scripts/Module/InGame/NightResultPopup.cs` | 2-4 |
| `Assets/Scripts/Module/Events/NightEvents.cs` | 6-3 이벤트 |

### 6-2. 본편 코드를 고치는 곳 (밤일 때만 갈라지게 — 본편 동작은 그대로)

| 자리 | 바꿀 것 |
|---|---|
| `BattleDirector.cs:445-468` 판 시작 | 밤이면 `EnterStartHost` 건너뜀 · `EnterRoom(0)` 대신 `EnterNightArena()` |
| `BattleDirector.cs:3475` 부근 매 프레임 | `TickNight(dt)` 한 줄(`TickReap` 옆) |
| `BattleDirector.cs:7934` `TickPendingLevelUp` | 밤이면 「적이 남아 있으면 보류」 조건 건너뜀 |
| 전투 틱 전체 | 밤 + `_awaitingBuff` 면 멈춤 |
| 경직 기준(`Unit.cs:1190`) | `StaggerBaseHp` 가 있으면 그 값 × 0.35 × 배율 |
| `TickRescue`(`BattleDirector.cs:3707`) | 밤이면 지연 · 쿨다운을 `NightModeTable` 에서 · 구조 몸은 오늘 목록에서 |
| 적 상한(`MaxRoomUnits` 179줄 · `_roomEnemyCap`) | 밤이면 `EnemyCap` |
| `KillEnemy` 처치 처리(7633 · 7670-7682) | 밤이면 `_nightKills++` · 대형 · 대장 경험치 배율 · `GrantShards` · `DropGold` 끔 |
| `Finish(bool)`(9744줄) | 밤이면 챕터 진행 · 상자 대신 4-4 보상 · `NightFinishedEvent` |
| `EnterRoom` 보스 세우기(3055-3136) | `SpawnBoss(bossKey)` 로 떼어 내 같이 씀 |
| `Module/Lobby/LobbyMainUI.cs:110` | `ModeSurvivalCard` → `NightEntryPanel` 열기 |

### 6-3. 이벤트

| 이벤트 | 필드 | 발행 · 구독 |
|---|---|---|
| `NightStateEvent` | `ElapsedSeconds` · `KillCount` · `WaveAffinity` · `NextEventKind` · `NextEventSeconds` | **1초마다 · 처치할 때**(매 프레임 금지) → HUD |
| `NightBannerEvent` | `BannerKey` · `BannerArg` | 사건마다 → HUD 띠 |
| `NightFinishedEvent` | `IsCleared` · `FinalKills` · `SurvivedSeconds` · `ReachedLevel` · `IsNewBest` · `ShardHostKeys[]` · `ShardCounts[]` · `PossessCount` · `LostCount` · `SkillCount` | 판 끝 → `NightResultPopup` |

- 기존 `BuffOfferEvent` · `RunExpChangedEvent` · 체력 이벤트는 그대로 쓴다. 밤에서는 `StageFinishedEvent` 를 발행하지 않는다(챕터 결과 창이 같이 뜨지 않게).
- IEvent 필드 이름은 발행자 프로퍼티와 다르게(규약) — 위 이름이 그것이다.

### 6-4. 만드는 순서와 확인

| 단계 | 할 일 | 확인 |
|---|---|---|
| 1 | `RunModeContext` · `NightModeTable` · 로비 입구 → 빈 아레나에 유령으로 입장 | 로비 → 밤 → 아레나, 본편 챕터 시작은 그대로 |
| 2 | 시작 몸 3 · 고르면 둘 사라짐 · 무리 스폰 · 상한 · 처치 수 | 무리가 식대로 늘어남(1·3·5분 스크린샷), 30마리에서 멈춤 |
| 3 | 레벨업 카드(빈 방 조건 건너뜀 + 창 동안 멈춤) | 카드 창 동안 적 · 시계 · 유령 감소 정지 |
| 4 | 대형 호스트 + 경직 빼앗기 + 구조 판정 | 유령은 대형을 못 탐 · 몸으로 깎으면 1.5초 창 · 대형이 있어도 몸 잃으면 1초 뒤 구조 몸 |
| 5 | 속성 전환 · 회복 · 대장 · 보스 · 끝 | 1분마다 속성 바뀜 + 회복 숫자 · 보스 처치로 결과 창 |
| 6 | 결과 창 · 보상 · 최고 기록 저장 | 조각 지급 · 요일별 최고 기록 · 본편 판 조각 없음 |
| 7 | HUD · 띠 · 3언어 글자 | 한 · 일 · 영 전 문장이 칸에 들어감(`GetPreferredValues`) |
| 8 | 밸런스(6-5) | 목표 표를 맞춤 |

### 6-5. 밸런스 목표 (시험판 실측 → 유니티에서 맞출 목표)

시험판 자동 조종(요일 7개 × 2판, 「빠졌다 쏘기」 포함, 잔몹 상한 30) 결과를 목표로 삼는다. 시험판은 수치가 본편과 달라서 **값이 아니라 결과를 맞춘다.**

| 지표 | 시험판 | 유니티 목표 |
|---|---|---|
| 클리어율 — 오늘 호스트 다 키움 | 10/14 (71%) | 50~70% |
| 클리어율 — 안 키움 | 0/14 | 10% 이하 |
| 처치 수 평균 — 키움 / 안 키움 | 284 / 86 | 2.5~3배 차이 |
| 5분 도달 레벨 | Lv 10 | Lv 9~11 |
| 몸 잃음 — 키움 | 판당 2~4 | 2~4 |
| 유령으로 지낸 시간 | 판당 2~8초 | 10초 이하 |
| 판당 대형 호스트 빼앗기 | 1~2회 | 1회 이상(안 쓰면 이 장치가 의미 없다) |

- 원거리 · 중거리 날이 근거리 날보다 어려웠다(시험판에서 멧돼지 무리가 몸 피해의 절반). 요일별 클리어율 차이가 20%p 넘으면 그 요일 잔몹 목록을 먼저 고친다.
- 맞추는 순서: ① 무리 밀도(3-5 식) ② 대형 호스트 체력 · 경직 배율 ③ 대장 · 보스 체력. 본편 값(유령 감소 · 상성 · 경직)은 건드리지 않는다.

---

## 7. 미정 · 결정 필요

| # | 무엇 | 지금 기본안 |
|---|---|---|
| 1 | 입장 횟수 | **정함(10-15) — 하루 3번**, 젬 추가 값 · 상한은 가안(4-5) |
| 2 | 모드 재화(특수 재화 · 상점) · 골드 | 없음 — 모드 확정 뒤 |
| 3 | 보상 숫자(처치 50마다 1 · 보스 +3) | 가안. 다른 획득처(상자 · 상점)와 비교해 정한다 |
| 4 | 난이도 | 지금은 한 단계. 나중에 **「위협 단계」** 1 · 2 · 3… — 무리 체력 · 수 배율과 보상 배율만 바뀌고, 앞 단계 보스를 잡으면 다음 단계가 열린다(10-14 제안). `NightModeTable` 에 단계 표를 붙이면 된다 |
| 5 | 코만도(미사일) 거리 분류 | 원거리로 둠(배율 0.5 지만 유도 폭격) |
| 6 | 보스 뒤 시간 제한 | 없음(유령 에너지가 시계) |
| 7 | 빼앗은 대형 호스트 | 보통 몸으로 바뀜. 「대형 그대로 쓰기」(체력 ×1.5 등)는 재미 검증 뒤 |
| 8 | 경직 창 1.5초 | 본편 값. 무리 속에서 빼앗기가 너무 어려우면 대형만 `NightModeTable` 값으로 늘린다 |
| 9 | 적 30마리 + 큰 적 성능 | 폰에서 프로파일러로 확인 |
| 10 | 요일 기준 시각 | 기기 시각 오전 5시(상점과 같이). 서버가 붙으면 서버 시각 |
| 11 | 요일별 바닥 · 보스 | 처음엔 같은 바닥 · 무작위 보스. 요일 맛을 더 내려면 요일 표 칸만 채운다 |

> 망령의 탑(층마다 호스트 조건)은 같은 시험판에 들어 있다 — 밤을 옮긴 뒤 같은 형식의 문서로 따로 쓴다. 아레나 · 시작 몸 · 요일 · 결과 창 구조를 탑도 그대로 쓴다.

---

## 개정 이력

| 버전 | 날짜 | 변경 내용 |
|---|---|---|
| 1.4 | 2026-10-16 | 대형 호스트 — 탈출 폐지로 「몸에 든 채 깎아 빼앗기」 불가 → 유령이면 바로 탄다(PD). 인게임 구현(커밋 8f00e5a9 · 4c9f83f1) |
| 1.3 | 2026-10-15 | 하루 입장 3번(이기든 지든 −1) · 젬 추가 입장 · 저장 칸 |
| 1.2 | 2026-10-15 | UI 시안 연결 — 화면 설계서 `wireframes/AVSR_Screen_Modes.md` 를 화면 정본으로 |
| 1.1 | 2026-10-15 | 잔몹 상한 40 → 30 (PD: 30마리 쌓이기 전에 못 잡으면 죽는다) |
| 1.0 | 2026-10-15 | 최초 작성 — 시험판(AVSR_Night.html, 커밋 d01df0c1) 결과 + 본편 코드 재사용 지점 조사 |
