# 콘텐츠 세부 기획서 + 개발 스펙 — 로비 상점 · 시즌 패스 · 조각 뽑기 · 미션 · 결제

- **콘텐츠명**: 로비 상점 — 판매 넷(**시즌 패스 · 재화 · 상자 · 조각 뽑기**) · 매일/매주 미션 · 월정액(계약) · 프로필 꾸미기 · 결제 · **카테고리**: 아웃게임 수익 · **우선순위**: P0
- **Version**: `0.5` · **Last Updated**: `2026-10-15` · **Status**: PD 답 반영(0-2) — 조각 뽑기는 **하단 바 「소환」으로 상점과 분리**(A안) · 상자를 보상에서 뺄지만 검토 중(0-3)
- 관련: [`AVSR_Content_Economy.md`](AVSR_Content_Economy.md)(재화 · 오늘의 상점 · 정수 — 구현됨) · [`AVSR_Balance10.md`](AVSR_Balance10.md) · `Assets/Scripts/Module/CLAUDE.md` 6절(상자) · 7절(오늘의 상점)
- **장면 기획**(화면 19장면 · 판매 구성표): [`_exchange/store_scenes.md`](_exchange/store_scenes.md) · **소환 연출 박자표**: [`_exchange/guide_summon_fx.md`](_exchange/guide_summon_fx.md) — 장면 시안 `_exchange/in/scene_s01~s18_*.png`
- 데이터: [`Store/`](Store/) — `price_tiers.tsv` · `store_products.tsv` · `cards.tsv` · `pass_s1.tsv` · `missions.tsv` · `gacha.tsv` · `profile.tsv`
- 시안(정답): 로비 `_exchange/in/mock_store_lobby.png` · 재화 탭 `mock_store_currency.png` · 패스 `mock_store_pass.png` · 프로필 `mock_store_profile.png` · **소환 `mock_summon_a.png`(PD 고름 — 하단 바 4칸)**. 대체된 시안(옛 추천 탭 · 상점 안 뽑기 탭 · B안)은 지웠다

이 문서는 둘로 나뉜다.
- **A. 참고 · 방향** — 다른 게임은 어떻게 파는가, 우리는 무엇을 가져오고 무엇을 안 가져오나.
- **B. 개발 스펙** — 모듈 · 표 · 저장 · 결제 흐름 · 이벤트 · 화면 이름표 · 언어팩 · 그림 · 작업 순서 · 완료 조건. **B 만 보고 바로 만들 수 있게** 썼다.

---

## 0. 한눈에

### 0-1. 요약

| 무엇 | 어떻게 | 왜 |
|---|---|---|
| **상점** `LobbyShopPanel` (하단 SHOP) | **확정 구매만** — 탭 셋 **오늘**(오늘의 상점, 구현됨) · **상자**(젬) · **재화**(계약 · 입문 · 챕터 돌파 · 젬 · 골드). 남색 · 하늘 네온 | 값을 내면 무엇을 받는지 다 보이는 것만 |
| **소환** `SummonPanel` (하단 **SUMMON**, 새 칸) | **조각 뽑기 전용 화면** — 보라 제단 · 영혼 불꽃. 한 번에 「몸 하나 + 조각 ×2/×5/×20」. 젬 60 · 10회 540(A급 1회 보장) · 소환권 · 하루 무료 1회 · 대박 천장 50회 · 주간 픽업 · 확률 보기 · 뽑기 기록 | PD 2026-10-15 「가챠랑 일반 구매랑 구분돼야」 → A안. 확률 상품은 문 · 색 · 분위기가 다르다(법 · 신뢰) |
| **시즌 패스** `SeasonPassPanel` | 28일 · 30단계 · 무료 줄 + 프리미엄 줄(₩12,000) · 묶음(₩25,000 = 프리미엄 + 15단계). 소환권 · 젬 · 정수 · 상자, 마지막 단계 = 프로필 테두리(무료) · 칭호(프리미엄) | 궁수의 전설 2 의 패스는 28일 · 매일/매주 목표로 오른다. 시즌 1 이름은 정해 둔 「GHOST REVENGE」 |
| **미션** | 패스 화면의 탭. 매일 6 + 전부 완료 1 · 매주 5 + 전부 완료 1. **빙의 · 유리한 상성 · 여러 몸 쓰기**에 묶는다 | 패스 경험치의 길이자 「이 게임다운 놀이」를 매일 시키는 장치 |
| **월정액(계약)** | 작은 계약(₩5,900) — 즉시 젬 300 + 30일 매일 90 / 큰 계약(₩19,000) — 즉시 1,000 + 매일 200 + 편의 셋 | 일본 · 한국 모바일에서 가장 많이 팔리는 상품. 매일 접속 이유 |
| **결제** | Unity IAP · 안드로이드 · 아이폰 동시 · 서버 없이 기기 검증(서버는 나중) · 유상/무상 젬 분리 저장 | 결정 4(서버 나중) · 6(동시 출시). 일본 납품이라 유상 재화 따로 센다 |
| **프로필 꾸미기** | 테두리 · 대표 얼굴(유령 · 가진 호스트) · 칭호 — 능력치 없음. 패스 · 챕터 클리어로 얻는다(판매 없음) | PD 2026-10-15 「외형 구입은 추후, 프로필을 꾸밀 수 있게」 |
| **행동력** | 판(챕터) 입장마다 5 · 최대 20 · 12분에 1 회복 · 챕터를 깨면 5 돌려줌 · 상단 바에 표시 · 젬 100 으로 20 채우기(하루 6회) · 계약 · 패키지 · 패스 · 미션이 준다(B-12) | PD 2026-10-15 「궁수의 전설처럼 입장마다 스태미나」 — 궁수의 전설 1 · 2 와 같은 값 |
| **안 하는 것** | **광고** · **외형(코스튬) 판매** | PD 2026-10-15 — 광고 제외(그래서 궁수의 전설의 「광고로 행동력」은 없다) · 외형 판매는 추후 |

### 0-2. 정한 것 (PD 2026-10-15)

| # | 질문 | 정함 | 어디에 |
|---|---|---|---|
| 1 | 광고 | **일단 제외** | 상품 · 보상에 광고 자리 없음 |
| 2 | 값 | **1차 값 확정(위임)** — 한국 기준 P1 ₩1,200 ~ P9 ₩129,000 | `price_tiers.tsv` · `store_products.tsv` |
| 3 | 하루 초기화 | **한국 00:00**(UTC 15:00) — 칸으로 두어 나중에 바꾼다. 오늘의 상점도 이 시계로 | `GameConfig._dailyResetUtcHour = 15` · `_weekResetDay = Monday` |
| 4 | 큰 계약 편의 | 상자 시간 단축은 뺐다(상자 검토 중 — 0-3). **무료 새로고침 1 · 판 골드 +10% · 판마다 레벨업 카드 다시 뽑기 1** | `cards.tsv` |
| 5 | 외형 | **외형 판매는 추후 · 뺀다.** 대신 **프로필 꾸미기**(테두리 · 대표 얼굴 · 칭호, B-9) | `profile.tsv` |
| 6 | 젬 쓰는 순서 | **무상 먼저**, 칸 하나로 바꿀 수 있게 | `GameConfig._gemSpendOrder = FreeFirst`(B-3) |
| 7 | 판매 품목 | **시즌 패스 · 재화 · 상자 · 조각 뽑기** 넷. 계약(월정액) · 입문 · 챕터 돌파 패키지는 「재화」 탭 안에 둔다 | `store_products.tsv` · `gacha.tsv` |
| 8 | 가챠 | **조각 뽑기는 넣는다** — 옛 원칙 「가챠 없음」을 이 결정이 대체한다 | B-11 |
| 9 | 뽑기 자리 | **상점과 분리 — 하단 바를 HOST · PLAY · SUMMON · SHOP 네 칸으로**, 소환 전용 화면(A안 · 시안 `mock_summon_a.png`) | B-6-1 · B-11 |
| 10 | 행동력 | **넣는다 — 궁수의 전설과 같은 규칙**(입장 5 · 최대 20 · 12분 1 · 클리어 +5) · 상단 바 · 판매 | B-12 · `GameConfig` 「행동력」 |

**아직 정할 것 — 골드 입장료**: 지금 판에 들어갈 때 몸값(골드, 로비 「132 도전하기」)을 낸다. 행동력이 들어오면 입장이 두 번 막힌다. **제 의견: 골드 입장료는 빼고 행동력만**(궁수의 전설도 행동력 하나). 남기려면 「몸 고용비」처럼 이름을 바꿔 성격을 가른다.

**6번 참고 — 「보통은 유료부터 쓰지 않나」**: 게임마다 다르다. 일본 게임은 무상을 먼저 쓰는 쪽이 많고(유상 잔고가 남아 있어야 플레이어에게 유리하다는 인식), 회사 쪽 회계 · 법정 잔고(유상 미사용분)만 보면 유상을 먼저 쓰는 쪽이 줄어들어 유리하다. 그래서 순서를 칸으로 두고, 출시 전 법무 · 회계와 정한다.

### 0-3. 검토 중 — 상자를 보상에서 빼고 상점에서 판다 (PD 2026-10-15 「생각 중」)

지금 상자는 **챕터 클리어 보상**이자 **무과금 조각의 가장 큰 길**이다(칸 3 · 해제 시간 · 젬 즉시 열기). 빼면 이렇게 된다.

| 바뀌는 것 | 영향 | 대신 |
|---|---|---|
| 클리어 보상에서 상자가 빠진다 | 무과금 조각이 판 안 조각(잡은 몸 1 · 잃은 몸 3) · 오늘의 상점 · 정수뿐 → 성장이 크게 느려진다 | **클리어 보상에 조각을 바로 준다**(챕터 표 `chest` 열 → `clearShards` 열: 은 2~3명분 · 금 · 백금처럼 등급별 수량). 상자 그림 연출은 결과창에서 그대로 써도 된다 |
| 로비 상자 칸 3 · 해제 시간이 필요 없어진다 | 로비가 단순해진다. 「기다림을 젬으로 산다」 젬 소비처가 사라진다 | 젬 소비는 상점 상자 · 골드 · 패스 건너뛰기가 맡는다. `ChestModule` 의 칸 · 시간 부분은 퇴역, 상자 굴리기는 `OpenImmediate` 로 남긴다 |
| 상자를 결제(인앱)로 판다 | 무작위 상자는 확률 상품이다 — 조각 뽑기가 들어오면서(0-2 #8) 원칙 문제는 없어졌지만 **확률 공개 의무**는 똑같이 붙는다 | 상자 「확률 보기」를 뽑기와 같은 창(`GachaRatePopup`)으로 띄운다. 상자 표(`ChestTable`)의 골드 범위 · 몸 수 · 등급 무게에서 % 를 계산 |

**제 의견**: 빼는 쪽이 좋다. 다만 클리어 보상을 「조각 직접」으로 바꾸는 것과 함께. 조각 뽑기가 들어오면 상자와 뽑기가 같은 일(무작위 조각)을 하게 되니, 상자는 **골드 · 정수 · 소환권 묶음 쪽**으로 성격을 바꾸는 것도 방법이다. 정해 주시면 챕터 표 · 결과창 · 로비 상자 칸 · 상점 상자 탭을 한 번에 바꾼다(그 전까지 이 문서의 상자 상품 · 패스 상자는 지금 상자 그대로 둔다).

---

## A. 참고 · 방향

### A-1. 참고 게임

같은 장르(방 단위 · 피하며 쏘기 · 레벨업 스킬 고르기)의 모바일 게임만 본다. 가장 가까운 궁수의 전설 2 를 기준으로 삼는다.

| 항목 | 궁수의 전설 2 (Habby) | 궁수의 전설 1 · 생존자의 전설 (Habby) | 우리 |
|---|---|---|---|
| 상점 칸 | 챕터 선물 패키지 · 오늘의 상점 · 장비 상자 · 소원 · 매일/매주/매월 특가 · 젬 · 골드 | 비슷한 구성 + 한정 패키지 | 추천 · 오늘의 상점 · 상자 · 재화 |
| 유료 재화 | 젬(다이아). 특권 화면에서 매일 20 무료 | 젬 | 젬(유상/무상 분리 저장) |
| 시즌 패스 | 28일, 매일·매주 목표로 오름, 단계 건너뛰기 가능. 작은 값($9.99) — 유물 + 끝에 젬 1,000 / 큰 값($29.99) — 캐릭터 조각 + 끝에 젬 3,000 | 생존자의 전설: 무료 줄 + 유료 줄($19.99), 매일 활동으로 경험치 | 28일 · 30단계 · 무료 + 프리미엄(P4) · 묶음(P6) |
| 월정액 · 특권 | 30일 카드(매일 젬 + 사냥 편의 + 도전 횟수) · 영구 카드(광고 제거 + 매일 젬) | 월정액 | 작은 계약 · 큰 계약(편의) — 광고가 없으니 영구 카드는 없다 |
| 입문 패키지 | 세 값, 초반에만 보이고 늦으면 비싼 칸부터 사라진다 | 첫 결제 · 초보 패키지 | 입문 3단, 첫 실행 7일 |
| 챕터 패키지 | 챕터를 처음 깰 때마다 영구 상품이 하나씩 늘고, 뒤로 갈수록 비싸진다 | — | 챕터 돌파 10종 |
| 광고 | 상점 특가 · 골드 · 재도전 · 에너지 · 빠른 사냥 — 보상은 일부러 약하게(결제를 갉아먹지 않게) | 비슷함 | **제외**(PD 2026-10-15) |
| 기타 | 캐릭터 직접 판매 — 패스 값의 기준점이자 늦게 온 사람 따라잡기 | — | 조각은 상자 · 오늘의 상점 · 정수로 — 몸 직접 판매는 안 함 |

출처는 문서 끝 「참고 자료」.

### A-2. 우리 게임에 맞게 고른 것

**가져온다**
- **챕터 돌파 패키지** — 「막 깼다 = 다음 챕터가 어렵다」 순간에 맞는 상품. 우리는 챕터 표가 있어 값 · 내용을 챕터 따라 올리기 쉽다.
- **입문 3단(7일 한정)** — 초반 전환. 늦으면 사라진다.
- **28일 패스 + 쉬운 매일·매주 목표** — 대부분이 끝까지 가게(완주율이 높아야 다음 시즌도 산다).
- **월정액** — 매일 접속 이유. 받는 칸은 상점 「계약」 칸.
- **상자를 젬으로** — 이미 있는 상자 3종(은 · 금 · 백금)을 그대로 판다. 상점에서 산 상자는 **칸에 안 넣고 바로 연다**(칸 3개가 차 있어도 산다).

**바꿔서 가져온다**
- 궁수의 전설 2 의 「캐릭터 조각 직접 판매」 → 우리는 **조각 뽑기**(무작위 몸 + 조각) + **정수**(원하는 몸으로 교환, B 2 · A 3 · S 5)로 나눈다 — 뽑기에서 겹친 · 다 키운 몸은 정수가 되어 원하는 몸으로 간다(버리는 결과가 없다).
- 미션을 **우리 핵심 놀이**에 묶는다 — 빙의 · 유리한 상성 처치 · 서로 다른 몸 5종 빙의. 상점 · 강화 같은 아웃게임 미션은 하루 2개만.

**안 가져온다**
- 광고 · 영구 광고 제거 카드 — 광고는 일단 제외(PD 2026-10-15).
- 외형(코스튬) 판매 — 추후(PD 2026-10-15). 지금은 프로필 꾸미기만.

### A-3. 재화 흐름

| 재화 | 새로 생기는 길 | 새로 생기는 쓸 곳 |
|---|---|---|
| 골드 | 패스 무료 줄(판 골드 12판분) · 챕터 돌파 · 골드 상품(젬) | — |
| 젬 | **결제**(유상) · 패스 · 미션 전부 완료 · 계약 매일 | 상자 구매 · 골드 구매 · 패스 단계 건너뛰기 |
| 소환권 | 패스 · 입문 · 챕터 돌파 | 조각 뽑기 1회 |
| 행동력 | 12분에 1 · 챕터 클리어 +5 · 젬 · 계약 매일 · 입문 · 패스 · 미션 전부 완료 | 판 입장 5 |
| 프로필 꾸미기 | 패스 30단계 · 챕터 클리어 | 장착(능력치 없음) |
| 정수 | 패스 · 입문 · 챕터 돌파 | (있던 대로) 원하는 몸의 조각 |
| 상자 | 패스 · 패키지 · 젬 구매 — **바로 열림** | — |
| 패스 경험치 | 미션 | 패스 단계 |

**무과금 젬 수지(시즌 28일)**
- 들어옴: 매일 미션 전부 완료 20 × 28 = 560 · 매주 전부 완료 100 × 4 = 400 · 패스 무료 줄 250 → **약 1,210**(+ 성장 경로 보상).
- 나감 예: 백금 상자 한 주 하나(320 × 4 = 1,280) 또는 은 상자 즉시 열기(약 18) 매일 + 오늘의 상점 새로고침 1회(10) 매일(약 780).
- → 무과금도 「한 주에 백금 하나」 정도는 고를 수 있다. 큰 계약(매일 200)은 이걸 다섯 배로 만든다.

**값 대비 젬**: 젬 묶음 ₩1,000 당 50~58 젬 · 입문 1단 약 5배 · 작은 계약 ₩1,000 당 약 510 젬(10배) · 패스 프리미엄 ₩1,000 당 약 85 젬 + 상자 · 정수 · 외형.

### A-4. 화면 흐름

```
로비 ─ ShopButton(하단 SHOP) ─────────▶ LobbyShopPanel [오늘 | 상자 | 재화]
     ─ SummonButton(하단 SUMMON, 새) ──▶ SummonPanel (조각 뽑기 · 확률 보기 · 뽑기 기록)
     ─ GemPlusButton / GoldPlusButton ─▶ LobbyShopPanel [재화] (골드 칸 / 젬 칸으로 스크롤)
     ─ PassButton (새) ───────────────▶ SeasonPassPanel [보상 | 미션]
     ─ LobbyProfile 길게 누름 / 육성(유령) ProfileEditButton (새) ─▶ ProfilePopup [테두리 | 대표 얼굴 | 칭호]
상품 누름 ─▶ PurchaseConfirmPopup ─▶ (결제 / 젬 차감) ─▶ StoreRewardPopup (받은 것 · 상자는 그 자리에서 열림)
```

빨간 점(새 소식): 받을 미션 · 받을 패스 단계 · 계약 오늘 받기 · 오늘의 상점 무료 칸 · 새 챕터 돌파 상품.

---

## B. 개발 스펙

> 규칙: 새 코드는 `Assets/Scripts/Module/Common/Store/`(네임스페이스 `Game.Module.Common`) · 화면은 `Assets/Scripts/Module/Lobby/`.
> 프레임워크(`Assets/GameFramework/`) 수정 금지 · C# event 금지(EventBus) · UniTask · 숫자는 표에(코드에 적지 않는다) · 글자는 언어팩 키.
> 이름 = 프리팹 GameObject 이름 = 바인딩 키. ⚠ 인게임 판 안 상점이 이미 `ShopTable` · `ShopPanel` 이라 로비 것은 **`Store`** 로 이름을 가른다(`StoreTable` · `LobbyShopPanel`).

### B-1. 모듈

전부 `[Module(Layer = ModuleLayer.Game)]` 자동 등록. 의존은 `Initialize` 에서 `CoreModule.Get`.

| 모듈 | 제공 | 의존 | 한 줄 |
|---|---|---|---|
| `ResetClockModule` | `IResetClock` | — | 하루 · 주 경계를 한 곳에서 잰다(한국 00:00 = `GameConfig._dailyResetUtcHour` 15 · 주 시작 `_weekResetDay`). 오늘의 상점(`DailyShopModule`)도 기기 자정 대신 이것을 쓰게 바꾼다 |
| `IapModule` | `IIapService` | `IEventBus` | 스토어 결제 감싸기. 에디터 · 테스트는 가짜 스토어(`FakeStoreBackend`) |
| `RewardGrantModule` | `IRewardGranter` | `IPlayerDataService` · `IChestService` · `IPassService`(선택) | 보상 문자열(`gem:120;chest:gold`)을 풀어 지급 — 상점 · 패스 · 미션 · 계약이 같은 길을 쓴다 |
| `StoreModule` | `IStoreService` | 위 넷 + `IResourceManager` | 상품 목록 · 보이기 조건 · 구매 제한 · 젬 구매 · 결제 상품 지급 · 계약 |
| `PassModule` | `IPassService` | `IResetClock` · `IRewardGranter` · `IEventBus` | 시즌 · 경험치 · 단계 받기 · 프리미엄 |
| `MissionModule` | `IMissionService` | `IResetClock` · `IRewardGranter` · `IPassService` · `IEventBus` | 이벤트를 듣고 카운터를 올린다 · 받기 |
| `StaminaModule` | `IStaminaService` | `IPlayerDataService` · `IEventBus` | 행동력 — 회복 시계 · 입장 차감 · 클리어 반환 · 젬 채우기(B-12) |
| `GachaModule` | `IGachaService` | `IPlayerDataService` · `IResetClock` · `IEventBus` | 조각 뽑기 — 확률 · 천장 · 픽업 · 무료 · 기록(B-11) |
| `ProfileModule` | `IProfileService` | `IPlayerDataService` · `IEventBus` | 가진 테두리 · 칭호 · 대표 얼굴 · 장착(B-9) |

```csharp
public interface IResetClock {
    int DayKey { get; }                 // 하루 경계(지금 한국 00:00) 기준 yyyyMMdd
    int WeekKey { get; }                // 그 주 월요일의 DayKey
    TimeSpan UntilNextDay { get; }
    TimeSpan UntilNextWeek { get; }
}

public interface IRewardGranter {
    /// <summary>보상 문자열을 지급하고, 화면에 보여 줄 목록을 돌려준다. 상자는 그 자리에서 열어 결과까지 담는다.</summary>
    RewardResult Grant(string rewards, RewardSource source);
}
public enum RewardSource { Store, Pass, Mission, Card }
public readonly struct RewardLine { public readonly RewardKind Kind; public readonly string Key; public readonly int Amount; }
public enum RewardKind { Gold, Gem, Essence, Ticket, Stamina, Shard, Chest, PassXp, PassPremium, Card, Profile }
public sealed class RewardResult { public IReadOnlyList<RewardLine> Lines; public IReadOnlyList<ChestReward> OpenedChests; }

public interface IStoreService {
    IReadOnlyList<StoreProduct> Visible(StoreTab tab);      // 보이기 조건 · 제한을 거른 것
    bool IsSoldOut(string productId);
    string PriceLabel(string productId);                    // 결제: 스토어 지역 가격 / 젬: 숫자 / 무료: 키
    bool IsFirstDouble(string productId);                   // 젬 묶음 첫 구매 2배가 남았나
    UniTask<PurchaseOutcome> BuyAsync(string productId, CancellationToken ct = default);
    // 계약
    CardState Card(string cardKey);                         // 남은 날 · 오늘 받았나
    bool TryClaimCard(string cardKey, out RewardResult result);
}
public enum PurchaseOutcome { Granted, NotEnoughGem, SoldOut, Cancelled, Failed, Pending }

public interface IIapService {
    bool IsReady { get; }
    string LocalizedPrice(string storeId);                  // 「¥160」 「₩1,200」 — 스토어가 준 그대로
    UniTask<IapResult> PurchaseAsync(string storeId, CancellationToken ct = default);
    UniTask RestoreAsync(CancellationToken ct = default);   // 아이폰 「구매 복원」
}

public interface IPassService {
    string SeasonId { get; }
    bool IsActive { get; }
    TimeSpan UntilEnd { get; }
    int Xp { get; } int Tier { get; } int TierMax { get; }
    bool HasPremium { get; }
    bool CanClaim(int tier, bool premium);
    bool TryClaim(int tier, bool premium, out RewardResult result);
    bool TryClaimAll(out RewardResult result);
    int SkipGemCost { get; }                                // 단계당
    bool TrySkipTier();
    void AddXp(int xp);                                     // 미션 · 보상에서
    void GrantPremium();                                    // 결제 성공 때 StoreModule 이 부른다
}

public interface IMissionService {
    IReadOnlyList<MissionState> List(MissionPeriod period);
    bool TryClaim(string missionId, out RewardResult result);
    bool HasClaimable { get; }
}
```

### B-2. 데이터 표

TSV(사람이 고친다) → 임포터 → ScriptableObject(어드레서블). 메뉴 하나 `Tools/Game/상점 표 임포트` 가 다섯 표를 다 굽는다.

| TSV (`Projects/AVSR/Store/`) | SO · 주소 | 내용 |
|---|---|---|
| `price_tiers.tsv` | `StoreTable`(같이) | P1~P9 · 기준 USD — **값은 등급으로만** 적는다. 나라별 실제 값은 스토어 |
| `store_products.tsv` | `TableData/StoreTable` | 상품 29종 — id · 탭 · 종류 · 값 종류 · 값 · 제한 · 보이기 · 보상 · 띠 · 순서 · storeId · 글자 키 |
| `cards.tsv` | `StoreTable`(같이) | 계약 2종 — 기간 · 즉시 젬 · 매일 젬 · 편의 |
| `pass_s1.tsv` | `TableData/PassTable` | 30단계 — 단계 경험치 · 무료 보상 · 프리미엄 보상 |
| `missions.tsv` | `TableData/MissionTable` | 13개 — 주기 · 종류 · 목표 · 패스 경험치 · 젬 |
| `gacha.tsv` | `TableData/GachaTable` | 뽑기 설정 · 등급 확률 · 조각 수 확률 |
| `profile.tsv` | `TableData/ProfileTable` | 테두리 3 · 칭호 3 |

라벨 `label_tabledata` 그대로. 각 TSV 머리말에 칸 설명이 있다.

**보상 문자열 문법**(상품 · 패스 · 미션 공통): 항목을 `;` 로 잇는다.

| 항목 | 뜻 |
|---|---|
| `gem:N` | 젬 N(무상). **결제 상품의 젬은 유상**으로 넣는다(`RewardSource.Store` + 결제 상품일 때) |
| `gold:N` | 골드 N |
| `goldrun:N` | 판 골드 N판분 = `232 × N × 열린 가장 높은 챕터 priceMul`(`balance10.py` RUN_GOLD_CH1 과 같은 자) — 뒤 챕터에서도 쓸모 있게 |
| `essence:N` | 정수 N |
| `ticket:N` | 소환권 N(조각 뽑기 1회 = 1장) |
| `stamina:N` | 행동력 N — **최대를 넘어 쌓인다**(자연 회복만 최대까지) |
| `chest:키[*N]` | 상자 N개 — **그 자리에서 열어** 결과를 지급(`IChestService.OpenImmediate`) |
| `passxp:N` · `passpremium:1` | 패스 경험치 · 프리미엄 열기 |
| `card:키` | 계약 시작(또는 기간 연장) |
| `profile:키` | 프로필 꾸미기 하나(`profile.tsv`) |

**`GameConfig` 에 더할 칸**(「상점 · 패스」 머리):

| 칸 | 값 | 뜻 |
|---|---|---|
| `_dailyResetUtcHour` | 15 | 하루 경계(UTC). 15 = 한국 00:00(PD 2026-10-15). 일본 05:00 으로 바꾸려면 20 |
| `_weekResetDay` | Monday | 주 경계 요일 |
| `_gemSpendOrder` | FreeFirst | 젬 쓰는 순서 — `FreeFirst`(무상 먼저) · `PaidFirst`(유상 먼저) |
| `_seasonId` · `_seasonStartUtc` · `_seasonDays` | `s1` · `2026-11-01T20:00Z` · 28 | 시즌. 끝나면 다음 시즌 표로 |
| `_passSkipGem` | 100 | 단계 건너뛰기 젬 |
| `_firstDoubleGemPacks` | true | 젬 묶음 첫 구매 2배 |
| `_starterDays` | 7 | 입문 패키지가 보이는 날 |
| `_cardStackMaxDays` | 180 | 계약 기간 이어 붙이기 상한 |

### B-3. 저장 (`UserData` v6 → **v7**)

`CurrentSaveVersion = 7`. 옛 저장은 빈 값으로 채운다(`Normalize`). 기존 `gem` 은 **무상 젬으로 옮긴다**.

| 필드 | 타입 | 뜻 |
|---|---|---|
| `gemPaid` · `gemFree` | int | 유상 · 무상 젬. `Gem` = 합. 옛 `gem` → `gemFree` |
| `installUtcDay` | int | 첫 실행 날(입문 패키지 7일) |
| `store.ids[]` · `store.counts[]` · `store.lastDay[]` · `store.lastWeek[]` | 배열 | 상품별 산 횟수 · 마지막으로 산 날/주(제한 계산) |
| `store.firstDoubleUsed[]` | string[] | 첫 구매 2배를 쓴 젬 묶음 |
| `cards.keys[]` · `cards.endDay[]` · `cards.lastClaimDay[]` | 배열 | 계약 끝나는 날 · 마지막으로 받은 날 |
| `pass.seasonId` · `pass.xp` · `pass.premium` | string · int · bool | 시즌이 바뀌면 xp · 받기 · 프리미엄을 비운다 |
| `pass.claimedFree` · `pass.claimedPremium` | long(비트 30) | 받은 단계 |
| `missions.dayKey` · `missions.weekKey` | int | 경계가 바뀌면 그 주기 카운터를 비운다 |
| `missions.ids[]` · `missions.progress[]` · `missions.claimed[]` | 배열 | 미션별 진행 · 받았나 |
| `missions.hostsThisWeek[]` | string[] | 「서로 다른 몸 5종」용 |
| `tickets` | int | 소환권 |
| `stamina` · `staminaMax` | int | 이미 있다(30/30) → 기본 최대 20 으로. 표의 값으로 덮는다 |
| `staminaTickUtc` | long | 마지막으로 회복을 계산한 시각(UTC 초). 열 때마다 지난 시간만큼 채운다 |
| `staminaRefillDay` · `staminaRefillCount` | int | 젬 채우기 하루 횟수 |
| `gacha.pity` · `gacha.freeDay` · `gacha.total` | int | 대박까지 센 수 · 무료를 쓴 날 · 뽑은 수 |
| `gacha.hist[]` | string[] | 최근 100회 「dayKey,hostKey,amount」 — 뽑기 기록 창 |
| `profile.owned[]` | string[] | 가진 테두리 · 칭호 키(기본은 처음부터) |
| `profile.frame` · `profile.title` · `profile.face` | string | 장착 — 대표 얼굴은 `ghost` 또는 호스트 키 |
| `iap.pending[]` | string[] | 결제는 됐는데 지급 전에 꺼진 거래 id(다음 실행 때 지급) |

`IPlayerDataService` 에 더할 것: `GemPaid` · `GemFree` · `AddGem(int amount, bool paid)` · `AddEssence(int)` · 골드는 있는 `AddCurrency` 를 쓴다.
`TrySpendGem(n)` 은 `GameConfig._gemSpendOrder` 를 따라 한쪽부터 깎고 모자라면 다른 쪽에서 채운다(합이 모자라면 아무것도 안 깎고 false).
`IPlayerDataService` 에 소환권: `Tickets` · `AddTickets(int)` · `TrySpendTickets(int)`.
`IChestService` 에 더할 것: `ChestReward OpenImmediate(string chestKey)` — 칸을 안 거치고 보상을 굴려 바로 지급(상자 굴리기는 `TryClaim` 과 같은 함수를 쓴다).

### B-4. 결제

#### B-4-1. 패키지 · 상품 등록
- `com.unity.purchasing`(Unity IAP)을 설치한다 — 버전은 설치 때 고정해 `Packages/manifest.json` 에 적는다.
- 스토어 상품 ID = `store_products.tsv` 의 `storeId`(`avsr.gem60` …). 종류: 젬 · 패키지 · 계약 · 패스 **전부 소모형(Consumable)** — 받은 것은 우리 저장이 기억한다(서버가 붙으면 서버로).
- 구글 · 애플 콘솔에 같은 ID 로 등록한다. 값은 콘솔에서 등급(P1~P9)에 맞는 가격을 고른다.

#### B-4-2. 흐름
```
BuyAsync(productId)
 ├ 젬 상품: 제한 확인 → TrySpendGem → Grant → Granted
 └ 결제 상품: 제한 확인 → PurchaseAsync(storeId)
      ├ 성공: 영수증 기기 검증(Unity IAP CrossPlatformValidator) → iap.pending 에 거래 id 저장 → Grant(유상)
      │       → 저장 → pending 에서 지움 → 스토어에 「지급 완료」 알림(ConfirmPendingPurchase) → Granted
      ├ 사용자가 취소: Cancelled (토스트 없음)
      ├ 보류(가족 승인 등): Pending — 「승인 대기 중」 토스트, 나중에 들어오면 같은 길로 지급
      └ 실패: Failed — 토스트 ui.store.fail.*
시작할 때: iap.pending 에 남은 거래가 있으면 먼저 지급(꺼져도 잃지 않게)
```

#### B-4-3. 에디터 · 테스트
- `FakeStoreBackend` — 에디터에서는 결제 창 없이 0.5초 뒤 성공(메뉴 `Tools/Game/상점/가짜 결제 실패로` 로 실패 · 취소 · 보류를 골라 볼 수 있게).
- 테스트 모드(`GameConfig.TestMode`)에서는 결제 상품을 「무료로 받기」 버튼으로 바꿔 흐름만 돈다.

#### B-4-4. 서버 없이 막지 못하는 것 (TBD-SRV)
- 기기 시계 돌리기(하루 제한 · 계약 날짜), 저장 파일 조작, 환불 뒤 회수. → 서버가 붙을 때 영수증 서버 검증 · 서버 시간 · 환불 알림(구글 Voided Purchases · 애플 서버 알림)을 붙인다.
- 그 전까지 계약 날짜는 **마지막으로 본 날짜보다 뒤로 가면 무시**(상자 시간과 같은 방어).

#### B-4-5. 출시 전 확인 (일본)
- **유상 · 무상 재화 구분** — 자금결제법상 유상 재화 잔고를 따로 셀 수 있어야 한다(그래서 `gemPaid` 를 나눈다). 쓰는 순서 · 표시는 법무 확인.
- **특정상거래법 표기** · **자금결제법 표기** 화면 — 상점 아래 「표기」 버튼(`StoreLegalButton`)으로 연다. 문구는 법무가 준다.
- **미성년 결제 상한** — 업계 지침(연령별 월 상한)을 따를지, 따른다면 생년월 입력 창이 필요하다. 법무 확인.
- **확률 공개** — 조각 뽑기 · 무작위 상자는 확률형 상품이다. 한국(게임산업법 확률형 아이템 정보 공개) · 일본(업계 지침)에 맞춰 화면에 몸별 % · 조각 수 % · 천장 · 10회 보장을 그대로 보여 준다(`GachaRatePopup`). 표기 방식은 법무 확인.
- 「컴플리트 가챠」(여러 개를 다 모으면 보상)는 만들지 않는다 — 일본에서 금지.

### B-5. 이벤트 · 미션 카운터

**새 이벤트**(`Assets/Scripts/Module/Events/StoreEvents.cs`) — 규칙대로 `public struct`, 필드에 `readonly` 금지, 발행자 프로퍼티와 다른 이름.

| 이벤트 | 필드 | 발행 |
|---|---|---|
| `StoreChangedEvent` | `ChangedProductId` | 구매 · 날 바뀜 · 계약 받기 |
| `PurchaseCompletedEvent` | `PurchasedProductId` · `WasPaid` | 구매 성공 |
| `PassChangedEvent` | `NewXp` · `NewTier` · `PremiumOn` | 경험치 · 받기 · 프리미엄 |
| `MissionChangedEvent` | `ChangedMissionId` · `NowClaimable` | 진행 · 받기 |
| `EnemyKilledEvent` | `KilledKey` · `WithAdvantage` · `WasBoss` | 인게임 — 적이 죽을 때(`BattleDirector.KillEnemy`) |
| `HostStatUpgradedEvent` | `UpgradedHostKey` · `IsGhostTab` | 육성 강화 성공 |
| `GachaPulledEvent` | `PullCount` · `GotJackpot` | 조각 뽑기 결과가 나왔다 |
| `ProfileChangedEvent` | `EquippedProfileKey` | 프로필 장착이 바뀌었다 — 로비 프로필이 다시 그린다 |

**미션 종류 → 듣는 이벤트**

| `type` | 이벤트 · 조건 |
|---|---|
| `login` | 그날 첫 `UserDataReadyEvent` |
| `run_start` | 판 시작(`RoomEnteredEvent` 의 첫 방) |
| `run_clear` | `StageFinishedEvent.IsCleared` |
| `possess` | `PossessedEvent`(유령 키 제외) |
| `distinct_hosts_possessed` | `PossessedEvent` — `missions.hostsThisWeek` 에 없는 키만 |
| `kill_advantage` | `EnemyKilledEvent.WithAdvantage` |
| `kill_boss` | `EnemyKilledEvent.WasBoss` |
| `daily_shop_buy` | `DailyShopChangedEvent.BoughtSlot >= 0`(−1 은 날 바뀜 · 새로고침) |
| `stat_upgrade` | `HostStatUpgradedEvent` |
| `chest_open` | `ChestOpenedEvent` + 상점에서 바로 연 상자 |
| `daily_all_done` · `weekly_all_done` | 같은 주기의 다른 미션을 **다 받았을 때** 열린다 |

⚠ hot path 규칙: `EnemyKilledEvent` 는 판 안에서 자주 나온다 — 미션 모듈은 카운터만 올리고 저장은 **판이 끝날 때 · 로비로 올 때** 한 번만 한다(매 처치 저장 금지). 문자열 조립 · LINQ 금지.

### B-6. 화면

> UI 규칙: `~Panel` 은 상단 HUD 아래 고정 높이(`constants.md` 3절 H), 맨 아래 자식 `PopupParent`. `~Popup` 은 `PopupParent` 아래, 닫을 때 `SetActive(false)`.
> 뒤로가기: `LobbyMainUI.OnBackPressed` 에 팝업 → 패널 순서로 더한다. 글자는 3언어 전 문장이 그림 속 칸에 들어가야 한다(`06_ui.md`).
> 모양은 **코덱스 시안을 먼저**(B-8) — 아래는 노드 이름과 동작이다.

#### B-6-1. 로비 진입점 (`LobbyMainUI`)

| 요소 | 지금 | 바꿀 것 |
|---|---|---|
| 하단 바 | HOST · PLAY · SHOP 세 칸 | **HOST · PLAY · SUMMON · SHOP 네 칸** — 칸 폭을 넷으로 나눈다. 로비 · 육성 공통 바(빌더 「위 · 아래 고정 꽂기」)도 같이 |
| `SummonButton` (새) > `SummonButtonBadge` | 없음 | `SummonPanel` 열기. 보라 불꽃 칸(고르면 금색 테두리는 공통 규칙). 오늘 무료 뽑기 남으면 빨간 점 |
| `ShopButton` | 「준비 중」 토스트 | `LobbyShopPanel` 열기(「오늘」 탭). 빨간 점 `ShopButtonBadge` |
| `LobbyProfile` | 유령 육성 열기 | 그대로 + 테두리 · 대표 얼굴 · 칭호를 그린다(`ProfileFrameImage` · `ProfileFaceImage` · `ProfileTitleText`) |
| `GemPlusButton` · `GoldPlusButton` | 「준비 중」 | `LobbyShopPanel` 재화 탭 · 해당 칸으로 스크롤 |
| `PassButton` (새) > `PassButtonBadge` · `PassTierText` | 없음 | `SeasonPassPanel` 열기. 「SEASON 1 · GHOST REVENGE · Lv 12」 작은 띠 — 1차 시안 ①처럼 **위 재화 줄 바로 아래 오른쪽** · 받을 것 있으면 빨간 점 |

#### B-6-2. `LobbyShopPanel` (~Panel)

| 요소 | 설명 · 동작 |
|---|---|
| `StoreGoldText` · `StoreGemText` · `StoreEssenceText` | 보유 재화 — `CurrencyChangedEvent` |
| `StoreTabDaily` · `StoreTabChest` · `StoreTabCurrency` | 탭 셋. 고른 탭은 금색 테두리(공통 규칙). 탭마다 빨간 점 `*Badge` |
| `StoreScroll` > `Viewport`(RectMask2D) > `Content` | 탭 내용 — 재사용 스크롤(오브젝트 풀) |
| `StoreCardSection` (재화 탭 맨 위) > `CardSmall` · `CardBig` | 계약 칸 둘 — 안에 `CardTitleText` · `CardDescText` · `CardDaysText`(「남은 12일」) · `CardBuyButton` > `CardPriceText` · `CardClaimButton`(「오늘 받기」, 오늘 받았으면 흐림) |
| `StoreProductCard` (반복 프리팹) | 상품 한 칸 — 아래 표 |
| 오늘 탭 | `DailyShopSection` — 이미 구현된 `IDailyShopService` 화면(`AVSR_Content_Economy.md` 3-2 의 `DailyShopSlot0~5` 등)을 이 탭에 넣는다 |
| 상자 탭 | 상자 칸 아래 `ChestRateButton`(「확률 보기」) |
| `StoreLegalButton` | 표기 화면(B-4-5) |
| `StoreRestoreButton` | 아이폰만 보임 — 구매 복원 |
| `PopupParent` | — |

`StoreProductCard` 안(이름은 칸마다 같다):

| 요소 | 설명 |
|---|---|
| `ProductArt` | 상품 그림(상자 · 젬 더미 · 패키지 그림) |
| `ProductNameText` · `ProductDescText` | 이름 · 내용 한 줄(「젬 600 · 백금 상자 · 정수 50」 — 보상 문자열로 만든다) |
| `ProductBadge` > `ProductBadgeText` | 「추천」 · 「첫 구매 2배」 |
| `ProductLimitText` | 「1회 한정」 · 「오늘 2/3」 · 「6일 남음」(입문) |
| `ProductBuyButton` > `ProductPriceIcon` · `ProductPriceText` | 젬 아이콘 + 숫자 / 지역 가격 문자열 |
| `ProductSoldMark` | 「구매 완료」 덮개 |

#### B-6-3. `PurchaseConfirmPopup` · `StoreRewardPopup` (~Popup)

| 요소 | 설명 |
|---|---|
| `ConfirmArt` · `ConfirmNameText` · `ConfirmContentText` · `ConfirmPriceText` | 무엇을 · 얼마에 |
| `ConfirmBuyButton` · `ConfirmCancelButton` | 사기 · 닫기. 결제 중에는 `ConfirmBusy`(빙글) 켜고 버튼 막음 |
| `RewardList` > `RewardItem`(반복: `RewardIcon` · `RewardAmountText`) | 받은 것. 상자는 이미 있는 상자 열기 연출(`ChestRewardPopup`)을 이어서 띄운다 |
| `RewardCloseButton` | 닫기 |

#### B-6-4. `SeasonPassPanel` (~Panel)

| 요소 | 설명 |
|---|---|
| `PassBanner` > `PassSeasonText` · `PassTimeText` | 「SEASON 1 · GHOST REVENGE」 · 「남은 12일 4시간」 |
| `PassLevelText` · `PassXpBar` > `PassXpFill` · `PassXpText` | 지금 단계 · 다음 단계까지 |
| `PassTabRewards` · `PassTabMissions` | 탭 둘 |
| `PassBuyButton` > `PassBuyPriceText` · `PassBundleButton` > `PassBundlePriceText` | 프리미엄 · 묶음. 샀으면 `PassPremiumMark` |
| `PassSkipButton` > `PassSkipCostText` | 단계 건너뛰기(젬) |
| `PassClaimAllButton` | 받을 것 다 받기 |
| `PassTierScroll` > … > `PassTierRow`(반복) | 단계 한 줄 — `TierNumberText` · `FreeSlot`(`SlotIcon` · `SlotAmountText` · `SlotClaimedMark` · `SlotLockMark`) · `PremiumSlot`(같은 이름, 프리미엄 아니면 `PremiumLock`) |
| `MissionList` > `MissionRow`(반복) | `MissionIcon` · `MissionText`(「빙의 5회」) · `MissionProgressText`(「3/5」) · `MissionBar` > `MissionFill` · `MissionRewardText`(「+15」) · `MissionClaimButton` · `MissionDoneMark` |
| `MissionDailyResetText` · `MissionWeeklyResetText` | 「초기화까지 3:12:05」 |
| `PopupParent` | — |

#### B-6-5. 빨간 점 규칙
- `ShopButtonBadge`: 계약 오늘 안 받음 ∨ 오늘의 상점 무료 칸 안 받음 ∨ 새 챕터 돌파 상품 안 봄.
- `SummonButtonBadge`: 오늘 무료 뽑기 남음.
- `PassButtonBadge`: 받을 패스 단계 ∨ 받을 미션.
- 탭 점은 그 탭 안의 같은 조건.

### B-7. 언어팩 키 (`Projects/AVSR/Localization/AVSR_Strings.tsv` 에 더한다 · 한국어 · 일본어 · 영어)

| 키 | 한국어 예 |
|---|---|
| `ui.store.title` · `ui.store.tab.daily/chest/currency` | 상점 · 오늘 / 상자 / 재화 |
| `ui.nav.summon` · `ui.summon.title` | SUMMON(하단 바 큰 글씨) · 소환 |
| `ui.stamina.name` · `ui.stamina.next` (`{0} 후 +1`) · `ui.stamina.full` · `ui.stamina.short.title` · `ui.stamina.short.body` (`필요 {0} · 보유 {1}`) · `ui.stamina.refill` (`젬 {0}로 {1} 채우기 · 오늘 {2}/{3}`) · `ui.stamina.refill.limit` | 행동력 · 1 회복까지 · 가득 참 · 행동력이 부족해요 · … · 오늘은 더 채울 수 없어요 |
| `ui.gacha.pickup` (`이번 주 픽업: {0}`) · `ui.gacha.pickup.note` · `ui.gacha.free` · `ui.gacha.pull1` · `ui.gacha.pull10` · `ui.gacha.ten_guarantee` · `ui.gacha.pity` (`대박까지 {0} / {1}`) · `ui.gacha.tickets` · `ui.gacha.rates` · `ui.gacha.history` · `ui.gacha.jackpot` | 이번 주 픽업 · A급이 나오면 50% 확률로 픽업 · 오늘 무료 · 1회 · 10회 · A급 1회 보장 · 대박까지 · 소환권 · 확률 보기 · 뽑기 기록 · 대박! |
| `ui.gacha.rate.*` | 확률 창 머리 · 칸 이름(등급 · 몸 · 조각 수 · 천장 · 보장) |
| `ui.profile.title` · `ui.profile.tab.frame/face/title` · `ui.profile.equip` · `ui.profile.equipped` · `ui.profile.nostat` · `ui.profile.source.*` · `profile.{key}.name` | 프로필 · 테두리 / 대표 얼굴 / 칭호 · 장착 · 장착 중 · 능력치 변화 없음 · 꾸미기 전용 · 얻는 곳 · 이름 |
| `ui.store.buy` · `ui.store.free` · `ui.store.soldout` · `ui.store.limit.once` · `ui.store.limit.daily` (`{0}/{1}`) · `ui.store.limit.days` (`{0}일 남음`) | 구매 · 무료 · 구매 완료 · 1회 한정 · 오늘 {0}/{1} · {0}일 남음 |
| `store.badge.best` · `store.badge.first_double` | 추천 · 첫 구매 2배 |
| `store.{id}.name` · `store.{id}.desc` | 상품마다(29종) — 이름만 쓰고 desc 는 보상 문자열로 자동이면 비워도 된다 |
| `ui.store.card.claim` · `ui.store.card.claimed` · `ui.store.card.days` | 오늘 받기 · 받음 · 남은 {0}일 |
| `ui.store.confirm.title` · `ui.store.confirm.buy` · `ui.store.reward.title` | 구매 확인 · 구매 · 획득 |
| `ui.store.fail.gem` · `ui.store.fail.soldout` · `ui.store.fail.network` · `ui.store.fail.unknown` · `ui.store.pending` | 젬이 부족해요 · 이미 샀어요 · 연결을 확인해 주세요 · 구매하지 못했어요 · 승인을 기다리는 중이에요 |
| `ui.store.legal` · `ui.store.restore` · `ui.store.restored` | 표기 · 구매 복원 · 복원했어요 |
| `ui.pass.title` · `ui.pass.season` (`SEASON {0}`) · `pass.s1.name` · `ui.pass.remain` · `ui.pass.level` | 시즌 패스 · SEASON {0} · GHOST REVENGE · 남은 {0} · Lv {0} |
| `ui.pass.free` · `ui.pass.premium` · `ui.pass.buy` · `ui.pass.bundle` · `ui.pass.skip` · `ui.pass.claimall` | 무료 · 프리미엄 · 프리미엄 열기 · 프리미엄 + 15단계 · 단계 올리기 · 모두 받기 |
| `ui.mission.tab` · `ui.mission.daily` · `ui.mission.weekly` · `ui.mission.reset` · `ui.mission.claim` | 미션 · 매일 · 매주 · 초기화까지 {0} · 받기 |
| `mission.{type}.text` | `mission.possess.text` 「빙의 {0}회」 · `mission.kill_advantage.text` 「유리한 상성으로 {0}마리 처치」 · `mission.distinct_hosts_possessed.text` 「서로 다른 몸 {0}종 빙의」 … (11종) |
| `ui.reward.gem/gold/essence/ticket/chest.*` | 젬 · 골드 · 정수 · 소환권 · 은/금/백금 상자 (있는 키가 있으면 그것을 쓴다) |

### B-8. 그림 (코덱스 — 시안 먼저)

순서는 스킬 `fx-art-pipeline` 과 같다. **완성 화면 시안 → PD 선택 → 그 화풍으로 부품 → 반영 → 시안과 나란히 대조.**

1. **시안**(완성 화면, 2026-10-15): 로비(패스 배지) · 재화 탭 · 패스 보상 · 프로필 꾸미기 · **소환(A안 — 하단 바 4칸)**. 바탕 = 지금 로비 캡처 · 기준 = HUD 3차 · 하단 바 화풍.
   → 받음: 1차 `_exchange/in/mock_store_{lobby,shop,pass}.png` · 2차 `mock_store_{gacha,currency,profile}.png` · 모음 `_exchange/ref/store_mock/store_mocks_2nd.png`. 시안 속 패스 칸 내용 · 숫자는 예시다 — **보상 · 값은 표(`Store/*.tsv`)가 정답.** 산 계약 칸은 「구매 완료」 덮개 대신 「남은 n일 · 오늘 받기」만 보인다.
2. 고른 시안을 앵커로 부품 발주(글자 없이 — 글자는 언어팩):

| 부품 | 수 |
|---|---|
| 탭 판 · 고른 탭 테두리 | 2 |
| 상품 칸 판(보통 · 추천 · 구매 완료 덮개) | 3 |
| 계약 칸 판(작은 · 큰) | 2 |
| 상품 그림 — 젬 더미 6단 · 골드 더미 3단 · 입문 3 · 챕터 돌파 1(공통) · 계약 2 · 패스 2 | 17 |
| 패스 띠(시즌 1) · 단계 줄 판(무료 · 프리미엄) · 자물쇠 · 받음 표시 · 프리미엄 표시 | 6 |
| 미션 줄 판 · 미션 아이콘(빙의 · 상성 · 보스 · 상자 · 상점 · 강화 · 판 · 몸) | 9 |
| 빨간 점 · 확인 팝업 판 · 보상 팝업 판 | 3 |
| 하단 바 4칸 — 소환 칸 판(보라) · 소환 아이콘(유령 + 영혼 불꽃) · 네 칸에 맞춘 다른 칸 판 | 3 |
| 행동력 — 아이콘(영혼 불꽃 번개) · 상단 바 칸 판 · 행동력 부족 창 판 | 3 |
| 소환 — 보라 제단 배경 · 뽑기 버튼 판 3 · 천장 막대 · 소환권 아이콘 · 결과 칸(×2 · ×5 · ×20 대박) · 결과 연출 그림 | 약 10 |
| 프로필 — 테두리 3(기본 · 시즌 1 · 챕터 10) · 프로필 창 판 · 칭호 띠 | 5 |

포맷: 도트 그림은 Sprite · Point · 밉맵 끔 → 인게임/로비 아틀라스(ASTC 4x4) · 만든 뒤 `resource_audit.py` 통과(`09_import_settings.md`).

### B-9. 프로필 꾸미기

**원칙**: 능력치 없음 · 판매 없음(외형 판매는 추후 — PD 2026-10-15). 패스와 챕터 클리어가 준다.

| 칸 | 무엇 | 얻는 곳 |
|---|---|---|
| **테두리** `frame` | 로비 왼쪽 위 프로필 테두리 · 결과창 | 기본 · 시즌 1 패스 무료 30단계 「GHOST REVENGE」 · 챕터 10 클리어 |
| **대표 얼굴** | 프로필 얼굴 — 유령 또는 **해금한 호스트 얼굴**(`Growth/Faces/face_{키}`) | 표가 없다 — 호스트를 해금하면 자동으로 고를 수 있다 |
| **칭호** `title` | 프로필 이름 아래 한 줄(글자는 언어팩) | 기본(없음) · 시즌 1 패스 프리미엄 30단계 「시즌 1 복수자」 · 챕터 10 클리어 |

**동작** — `IProfileService`: `Owned(type)` · `Equipped(type)` · `TryEquip(key)` · `TrySetFace(hostKeyOrGhost)`. 바뀌면 `ProfileChangedEvent`. 챕터 클리어(`StageFinishedEvent`)를 듣고 `chapter_clear:N` 조건이 맞으면 지급.

**화면 — `ProfilePopup` (~Popup, 로비 `LobbyProfile` 길게 누름 · 유령 육성의 `ProfileEditButton`)**

| 요소 | 설명 |
|---|---|
| `ProfilePreview` > `PreviewFrame` · `PreviewFace` · `PreviewName` · `PreviewTitle` | 지금 고른 것을 입힌 미리보기 |
| `ProfileTabFrame` · `ProfileTabFace` · `ProfileTabTitle` | 탭 셋 |
| `ProfileGrid` > `ProfileCell`(반복: `CellIcon` · `CellEquippedMark` · `CellLock` · `CellSourceText`) | 가진 것 · 장착 중 · 잠김(얻는 곳) |
| `ProfileNoStatText` · `ProfileEquipButton` · `ProfileCloseButton` | 「능력치 변화 없음 · 꾸미기 전용」 · 장착 · 닫기 |

### B-10. 작업 순서 · 완료 조건

| 단계 | 할 일 | 완료 조건 |
|---|---|---|
| 1 | 표 · 임포터 · `ResetClock` · `RewardGranter` · 저장 v7 | 임포트 후 SO 29 · 30 · 13 줄 · 옛 저장이 v7 로 열리고 `gem` 이 `gemFree` 로 옮겨짐 |
| 2 | `StoreModule` · `IapModule`(가짜 스토어) · `IChestService.OpenImmediate` | 에디터에서 젬 상품 · 결제 상품(가짜) 전부 사서 지급 확인 · 제한(1회 · 하루 N · 7일 · 챕터) 확인 · 결제 중 앱 종료 → 다음 실행 지급 |
| 3 | `PassModule` · `MissionModule` · `GachaModule` · `ProfileModule` · 새 이벤트 발행(인게임 2곳 · 육성 1곳) | 미션 13개 전부 실제 플레이로 오름 · 하루/주 경계(한국 00:00) 넘김 초기화 · 단계 받기 · 건너뛰기 · 프리미엄 · **뽑기 10만 회 모의 실행으로 표 확률과 실제 비율이 0.5%p 안 · 천장 · 10회 보장 확인** · 프로필 받기 · 장착 |
| 4 | 시안 3컷 → PD 선택 → 부품 발주 | PD 가 고른 시안 파일 경로 |
| 5 | 화면(`LobbyShopPanel` · `SummonPanel` · `SeasonPassPanel` · 뽑기 팝업 셋 · `ProfilePopup` · 팝업 둘 · 하단 바 4칸 · 로비 진입점) | 시안과 나란히 대조 시트 · 3언어 글자 점검 0 넘침(하단 바 「SUMMON」 포함) · 뒤로가기 순서 · 확률 창 숫자 = 표 |
| 6 | 실제 스토어(구글 내부 테스트 · 애플 샌드박스) | 젬 묶음 하나 · 패스 · 계약 실결제 → 지급 · 아이폰 복원 |
| 7 | 출시 전 확인(B-4-5) | 법무 회신 반영 |

**하지 말 것**: 상품 값 · 보상을 코드에 적기 · 결제 성공 전에 지급하기 · 지급 전에 스토어에 완료 알리기 · 판 안에서 매 처치 저장 · 글자를 그림에 넣기.

### B-11. 조각 뽑기

**규칙**(값은 전부 `gacha.tsv`)
- 한 번 = **몸 하나 + 조각 수**. 몸: 등급 확률(B 75% · A 25%) → 그 등급 안에서 같은 확률. A급이 나오면 50% 로 **이번 주 픽업** 몸. 조각 수: ×2 70% · ×5 25% · **×20 대박 5%**.
- 몸별 확률(지금 표 기준): B급 11명 각 6.82% · 픽업(A) 12.5% · 나머지 A급 10명 각 1.25%. 사신(히든)은 안 나온다. 해금 안 된 몸도 나온다(조각으로 해금).
- **10회**: 마지막 칸 전에 A급이 없었으면 마지막 칸은 A급 안에서 굴린다.
- **천장**: 대박 없이 49회면 50번째는 대박 확정. 대박이 나오면 0 부터. (천장 없이도 50회 안에 대박이 나올 확률 92.3%)
- 값: 1회 젬 60 / 소환권 1 · 10회 젬 540 / 소환권 10 · **하루 무료 1회**(하루 경계 = 한국 00:00).
- 다 키운 몸의 조각은 그 자리에서 **정수**로 바뀐다(이미 있는 규칙) — 결과 칸에 「정수 +n」으로 보여 준다.
- 한 번 평균 조각 3.65 개. A급 한 명을 끝까지(452개) 모으는 데 평균 약 124회(픽업이면 그 몸에 몰린다).

```csharp
public interface IGachaService {
    GachaBanner Banner { get; }                       // 픽업 몸 · 남은 시간 · 값
    bool FreeAvailable { get; }
    int PityCount { get; } int PityMax { get; }
    int Tickets { get; }
    UniTask<GachaResult> PullAsync(int count, GachaPay pay, CancellationToken ct = default);   // count 1 · 10
    IReadOnlyList<GachaRateLine> Rates();             // 화면 「확률 보기」 — 표에서 계산
    IReadOnlyList<GachaHistoryLine> History();        // 최근 100회
}
public enum GachaPay { Free, Ticket, Gem }
public readonly struct GachaPull { public readonly string HostKey; public readonly int Shards; public readonly bool Jackpot; public readonly bool Pickup; public readonly int ToEssence; }
```

- 굴리기는 `System.Random`(시드 = 기기 시간) — 서버가 붙으면 서버에서 굴린다(TBD-SRV). 저장 조작은 막지 못한다.
- 결과는 **값을 받은 뒤 · 저장한 뒤** 화면에 낸다(연출 도중 꺼져도 결과는 이미 들어가 있다).

**화면 — `SummonPanel` (~Panel, 하단 바 SUMMON) + 팝업 셋** — 시안 `mock_summon_a.png`. 상점과 다른 **보라 · 검붉은 영혼 불꽃 제단** 판(상점은 남색 · 하늘 네온)

| 요소 | 설명 |
|---|---|
| `SummonTitleText` | 「소환」 |
| `GachaPickupBanner` > `PickupArt` · `PickupNameText` · `PickupNoteText` · `PickupTimeText` | 이번 주 픽업 몸 · 「A급이 나오면 50% 확률로 픽업」 · 남은 시간 |
| `SummonAltar` | 제단 · 유령 · 영혼 불꽃(은은히 도는 장식 — 연출 규칙) |
| `GachaFreeButton` | 오늘 무료 1회 — 썼으면 숨김 |
| `GachaPull1Button` > `Pull1PriceText` · `GachaPull10Button` > `Pull10PriceText` · `Pull10NoteText` | 1회 · 10회(「A급 1회 보장」). 소환권이 있으면 소환권 값이 먼저 보인다 |
| `GachaTicketText` | 보유 소환권 |
| `GachaPityBar` > `GachaPityFill` · `GachaPityText` | 「대박까지 37 / 50」 |
| `GachaRateButton` · `GachaHistoryButton` | 확률 보기 · 뽑기 기록 — **또렷하게**(법) |
| `GachaResultPopup` (~Popup) | 결과 1칸 · 10칸(`ResultCell` 반복: `CellFace` · `CellAmountText` · `CellJackpotMark` · `CellPickupMark` · `CellEssenceText`) · `ResultAgainButton` · `ResultCloseButton` |
| `GachaRatePopup` (~Popup) | 등급 % · 몸별 % 목록 · 조각 수 % · 천장 · 10회 보장 문구. 상점 상자 탭 「확률 보기」도 같은 창 프리팹을 쓴다 |
| `PopupParent` | — |
| `GachaHistoryPopup` (~Popup) | 최근 100회(날짜 · 몸 · 조각 수) |

연출은 [`_exchange/guide_summon_fx.md`](_exchange/guide_summon_fx.md) 박자표대로 — 1회 약 2.5초 · 10회 약 4초 · 0.4초 뒤부터 건너뛰기 · **불꽃 기둥 색 = 그 판 최고 등급 예고**(하늘 B · 보라 A · 금 대박, 속이지 않음) · 10회는 대박/픽업 카드를 맨 뒤로 미뤄 따로 뒤집는다 · 대박 > 픽업 > 새 몸 > 정수 전환 순으로 힘준다. 콘티 시안 `scene_s09~s11`.

### B-12. 행동력

**궁수의 전설 1 · 2 와 같은 규칙**(PD 2026-10-15): 최대 20 · 판 입장 5(= 가득 차면 네 판) · 12분에 1 회복(0 → 20 은 4시간) · 젬 100 으로 20 채우기 · 챕터를 깨면 5 돌려줌. 우리는 광고가 없으니 「광고 보고 5」 길은 없다.

| 규칙 | 값(`GameConfig` 「행동력」) | 비고 |
|---|---|---|
| 최대 | `_staminaMax` 20 | 큰 계약 동안 +20(40) |
| 판 입장 | `_staminaPerRun` 5 | 「도전하기」를 누를 때 깎는다. 모자라면 들어가지 않고 부족 창 |
| 자연 회복 | `_staminaRegenSeconds` 720 | 최대까지만. 기기 시간 기준(서버 전까지 시계 조작은 못 막음 — 뒤로 가면 무시) |
| 클리어 반환 | `_staminaOnClear` 5 | 그 판 챕터를 깨면 바로 돌려준다(궁수의 전설 2 와 같다). 죽거나 포기하면 없음 |
| 넘침 | — | 구매 · 보상으로 받은 것은 최대를 넘어 쌓인다(999 까지). 넘친 동안 자연 회복은 멈춘다 |
| 젬 채우기 | `_staminaRefillGem` 100 · `_staminaRefillPerDay` 6 | 한 번에 최대치(20)만큼. 하루 경계 = 하루 초기화 |

**들어오는 길**: 자연 회복(하루 최대 약 120) · 클리어 반환 · 젬 채우기 · 작은 계약 매일 +20 · 큰 계약 매일 +40 · 최대 +20 · 입문 패키지 40/100/200 · 패스 무료 줄 60 · 미션 매일 전부 완료 +10 · 매주 전부 완료 +20.

```csharp
public interface IStaminaService {
    int Current { get; } int Max { get; }
    TimeSpan UntilNext { get; }                 // 가득 차 있거나 넘쳤으면 Zero
    bool CanEnter { get; }                      // Current >= 입장 값
    bool TryPayEntry();                          // 판 시작 — 모자라면 false(부족 창은 화면이 띄운다)
    void RefundClear();                          // 클리어 반환
    int RefillGemCost { get; } int RefillLeftToday { get; }
    bool TryRefillWithGem(out string failKey);
}
```
- 매 프레임 계산하지 않는다 — 로비가 열릴 때 · 1초 타이머(상단 바 글자)에서 `staminaTickUtc` 와 지금의 차이로 채운다. 바뀌면 이미 있는 `CurrencyChangedEvent.NewStamina`.
- 판 입장 흐름: `ChapterHostPanel` 「도전하기」 → `TryPayEntry` → 실패면 `StaminaShortPopup` → 성공이면 지금처럼 판 시작. 클리어는 `StageFinishedEvent.IsCleared` 에서 `RefundClear`.

**화면**

| 요소 | 설명 |
|---|---|
| 상단 바 `StaminaCounter` > `StaminaIcon` · `StaminaText`(「18/20」, 넘치면 「45/20」 금색) · `StaminaTimerText`(「09:12」, 가득이면 숨김) · `StaminaPlusButton` | 유령 Lv 과 골드 사이(또는 골드 왼쪽). 자리는 시안 S20 으로 정한다. `+` → 부족 창(채우기) |
| 「도전하기」 버튼 | 값 표시를 행동력 「⚡5」로(골드 입장료를 빼면) |
| `StaminaShortPopup` (~Popup) > `ShortText` · `ShortTimerText` · `RefillButton` > `RefillCostText` · `RefillLeftText` · `ShortCloseButton` | 「행동력이 부족해요 — 필요 5 · 보유 3 · 다음 회복 08:12」 · 「젬 100 으로 20 채우기 · 오늘 5/6」 · 젬도 모자라면 버튼이 「젬 사러 가기」로 |

---

## 7. 미정 · 결정 필요

- **골드 입장료를 뺄지**(0-2 아래) — 제 의견: 빼고 행동력만.
- **상자를 보상에서 뺄지**(0-3) — 정해 주시면 챕터 표 · 결과창 · 로비 상자 칸 · 상점 상자를 한 번에 바꾼다.
- 2시즌부터 시즌 표 — 새 TSV `pass_s2.tsv` 를 더하고 `GameConfig._seasonId` 만 바꾸는 구조로 만들어 둔다.
- 외형(코스튬) 판매 — 추후(PD 2026-10-15). 그때 유령 외형 · 호스트 외형을 함께 본다.
- 픽업 몸 돌리는 순서 · 주기(지금 매주 · 표의 `pickup_host` 한 칸).
- 신규 모드(요일 던전 등)의 특수 재화 상점 — `AVSR_Content_Economy.md` 6절, 모드를 만들 때.
- 출시 전 법무(B-4-5) · 젬 쓰는 순서(0-2 #6).

## 참고 자료
- [Tiny Teardown — Archero 2 (Habby Games)](https://someselectedstories.substack.com/p/tiny-teardown-archero-2-habby-games) — 상점 칸 · 패스 두 값 · 특권 카드 · 입문 · 챕터 패키지 · 광고
- [Archero 2 Smart Spending Guide](https://archero2hub.com/en/guide/best-plan) — 혜택 카드 · 프리미엄 패스 · 젬 가격표
- [Archero 2 Shop Guide — Theria Games](https://theriagames.com/guide/archero-2-shop-guide/) · [PocketGamer — Archero 2 guide](https://www.pocketgamer.com/archero-2/guide/) — 오늘의 상점
- [Survivor Pass — Survivor.io 위키](https://survivorio.fandom.com/wiki/Survivor_Pass) — 무료 + 유료 줄, 매일 활동 경험치
- [Battle pass — Wikipedia](https://en.wikipedia.org/wiki/Battle_pass)
- 값 · 구성은 지역 · 시기마다 다르다(가이드 기준 2024~2026). 출시 전 실제 게임에서 다시 본다.

## 개정 이력

| 버전 | 날짜 | 변경 내용 |
|---|---|---|
| 0.1 | 2026-10-15 | 최초 작성 — 참고 게임 정리 · 상점 · 패스 · 미션 · 계약 · 결제 스펙 · 데이터 초안 5종 |
| 0.2 | 2026-10-15 | PD 답 반영 — 광고 제외 · 값 1차 확정(한국 기준) · 한국 00:00 초기화(칸) · 큰 계약 편의 교체 · 외형 설계 · 젬 순서 칸 · 상자 검토(0-3) · 시안 4컷 발주 |
| 0.5 | 2026-10-15 | PD 「궁수의 전설처럼 입장마다 스태미나 · 상단 바 · 판매」 — 행동력(B-12, 입장 5 · 최대 20 · 12분 1 · 클리어 +5 · 젬 100 = 20 하루 6회) · 상품 `stamina_refill` · 계약(작은 매일 +20 · 큰 최대 +20 · 매일 +40) · 입문 · 패스 · 미션 보상에 행동력 · 미션 보상 칸을 보상 문자열로 |
| 0.4 | 2026-10-15 | PD 「가챠랑 일반 구매랑 구분돼야」 → A안: 하단 바 HOST · PLAY · SUMMON · SHOP · 소환 전용 `SummonPanel` · 상점 탭 셋(오늘 · 상자 · 재화) |
| 0.3 | 2026-10-15 | PD 「조각 가챠 넣는다 · 판매는 패스 · 재화 · 상자 · 조각 뽑기 · 외형 판매는 추후, 프로필을 꾸미게」 — 조각 뽑기(B-11, `gacha.tsv`) · 탭 재구성(오늘 · 상자 · 조각 뽑기 · 재화) · 소환권 · 외형 → 프로필 꾸미기(B-9, `profile.tsv`) · 확률 공개 · 2차 시안 3컷 |
