# 콘텐츠 세부 기획서 — 호스트 선택 (HOST SELECT)

> 기획 **Stage 2**. 상위: [`AVSR_GameComposition.md`](AVSR_GameComposition.md) #13 · 제약: [`AVSR_Decisions.md`](AVSR_Decisions.md)
> 목업: `Reference/Mockups/host_select.jpeg` (육안 확인 완료)

---

- **콘텐츠명**: `호스트 선택` · **카테고리**: `진입·플로우` · **우선순위**: `P0`
- **Version**: `0.1` · **Last Updated**: `2026-08-07` · **Status**: `게이트 B 검토 대기`
- **UI 타입**: `HostSelectPanel` (`~Panel`) — `LobbyScene` 소속 · 주소 `UI/Lobby/HostSelectPanel`

---

## 1. 목적 (플레이어 경험)

**"이번 판을 어떤 몸으로 시작할 것인가"를 결정하는 순간.** 매 STAGE 도전마다 반복 진입하는 1차 범위 최고 빈도 화면이다.
동시에 **잠긴 11칸이 항상 보이는 목표 게시판**으로 기능한다 — 그리드를 볼 때마다 "다음은 무엇을 얻는가"가 드러나야 한다.
신규 유저는 `amazoness` 1종만 보유하므로, 이 화면의 **첫인상 = 비어 있음이 아니라 채워야 할 12칸**이어야 한다.

## 2. 화면 구성 (UI 요소)

> 요소 이름 = 프리팹 GameObject 이름 = 클라 바인딩 키. 레이아웃·좌표는 Stage 2b.

| 요소 | 설명 | 상태 표시/입력 |
|------|------|---------------|
| `TopHudGroup` | 상단 HUD — 고스트 위젯 · ⚡ · 골드 · 젬 · 우편 · 설정 | **로비와 공통.** 정의는 로비 문서 소관 |
| `HeaderTitleText` | `첫 번째` + `HOST 를 선택하세요!` (HOST만 골드 강조) | 표시 전용 |
| `HeaderDescText` | `사망 시 유령으로 돌아가, 다른 HOST에 빙의할 수 있습니다.` | 표시 전용 |
| `HostListTitle` | `HOST LIST` 헤더 (해골 장식 좌우 1쌍) | 표시 전용 |
| `HostGrid` | **3열 × 4행 = 12칸.** 스크롤 없음, 전 칸 동시 노출 | 셀 탭 = 선택 |
| `HostSlot` (×12) | 초상 · 한글명 · 프레임. 선택 시 골드 프레임 + 좌상단 선택 마커 | 보유/잠금 2상태 |
| `GhostArrowGroup` | 그리드↔상세 사이 고스트 스프라이트 + `»` 화살표 | 표시 전용(루프 애니) |
| `HostDetailCard` | 우측 상세 카드 프레임 | — |
| `HostNameEnText` / `HostNameKrText` | `AMAZONESS` / `아마조네스` | 표시 전용 |
| `HostPortraitImage` | 대형 초상 (원형 발판 위) | 보유/실루엣 2상태 |
| `StatRow` (×4) | `HP` · `ATK` · `SPD` · **`DASH`** — 아이콘 + 라벨 + 바 + 수치 | 표시 전용 |
| `UltimateCard` | 아이콘 + 얼티밋명(정본) + 설명 2줄 | 보유/마스킹 2상태 |
| `HostUpgradeButton` | 파랑. `HOST 강화` + 부제 `능력치 · ULTIMATE · 속도` + `›` | 🟡 **1차 = 진입만** |
| `PossessStartButton` | 골드. `빙의 시작` + `(POSSESS)` + 고스트 아이콘 + `›` | ✅ **인게임 진입** |
| `TipBar` | `💡 TIP` 2줄 문구 | 표시 전용 |
| `OwnedHostCountText` | 보물상자 아이콘 + `보유 HOST {n}/12` | 신규 유저 **`1/12`** |

**목업 대비 변경 — 승인된 4건만 적용:**
① `JUMP`/`점프력` → `DASH`/`대시(회피) 속도` (`StatRow` + `TipBar` 문구 양쪽) · ② 15칸 → **12칸** ·
③ `12/12` → 진행도 실값(신규 `1/12`) · ④ 얼티밋 = 정본 표기 (`AMAZONESS FURY` → **`BLADE STORM`**)

`TipBar` 정정 문구: `HOST마다 이동속도, 대시(회피) 속도, 공격 방식이 다릅니다. / 다양한 HOST를 경험해 보세요!`
목업의 색상 변형 4칸(`람보(레이저)`·`마법사(녹색)`·`닌자(적색)` 등)은 **코스튬으로 분리**되어 그리드에서 제외된다 (확정 #10).

## 3. 기능·규칙 (동작)

**진입 — 화면 1개, 경로 2개** (확정)

| 경로 | 진입 버튼 | 목적 | `빙의 시작` |
|------|----------|------|:---:|
| `ChapterStart` | 로비 `CHAPTER` (주 경로) | 게임 시작 | 활성 |
| `HostBrowse` | 로비 `HOST` | 조회·강화 | 활성 (동일 화면이므로 시작도 가능) |

- **그리드 순서 = `AVSR_Decisions.md` 4절 표 #1~12 고정.** 해금 순서 = 표시 순서 `[근거: 목표의 근접도가 위→아래로 읽히게]`
- **기본 선택**: ① 저장된 `SelectedHostId`가 **현재 보유 중**이면 그것 → ② 아니면 **보유 호스트 중 표 순번 최소값**.
  **잠금 호스트를 기본 선택하지 않는다** `[근거: 진입 즉시 시작 가능한 상태가 기본]`. 신규 유저는 항상 `amazoness`.
- **해금 판정은 매 진입 시 진행도로 재평가한다.** 해금 목록을 저장하지 않는다 (데이터 제약 6-5).

**잠금 셀 표현 — 신규 정의** (목업에 없음. 확정 #22 "실루엣 + 자물쇠 + 해금 조건"의 구체화)

| 항목 | 결정 |
|------|------|
| 셀 선택 | **가능하다.** 잠금 셀도 탭하면 상세 패널이 갱신된다 `[근거: 목표 게시판 기능 — 못 누르면 목표가 안 된다]` |
| 셀 표현 | 초상 = **단색 실루엣**, 중앙 자물쇠 아이콘, 프레임 = 무채색, 한글명은 **그대로 표시** |
| 상세 초상 | 실루엣 + 자물쇠 (대형) |
| 이름 | `HostNameEnText`·`HostNameKrText` **표시** `[근거: 무엇을 얻는지 알아야 동기가 된다]` |
| 스탯 4종 | **마스킹.** 바 = 0% 무채색, 수치 = `???` |
| 얼티밋 카드 | 아이콘 = 실루엣, 명칭 = `???`, **설명 슬롯을 해금 조건 텍스트로 대체** `[근거: 목업 레이아웃 유지 — 신규 요소 추가 없음]` |
| `PossessStartButton` | **비활성** (무채색 · 상호작용 차단 · 라벨 유지). 탭 무반응 |
| `HostUpgradeButton` | **비활성** (강화 대상이 없음) |

**해금 조건 텍스트 형식** (조건 유형 2종만)

- `StageReach` → `CHAPTER {c} · STAGE {s} 도달 시 해금`  (예: `CHAPTER 1 · STAGE 5 도달 시 해금`)
- `ChapterBossClear` → `CHAPTER {c} 보스 처치 시 해금`  (예: `CHAPTER 3 보스 처치 시 해금`)

**빙의 시작 (`PossessStartButton`)**

1. 선택 호스트가 **보유 상태인지** 검증 (비활성이므로 정상 경로에선 항상 통과 — 방어용)
2. **스태미나 `⚡5` 검증** `[근거: GameComposition C-2]` → 부족하면 `SystemPopup` 표시 후 중단
3. `SelectedHostId`를 **저장소 인터페이스 경유로 커밋(저장)**
4. 스태미나 차감 → `GameScene` 로드
   > ⚠️ 스태미나 **회복 속도·상한**은 로비(경제) 문서 소관 `[TBD — 이유: 경제 밸런스 미착수, AVSR_Decisions 8절]`

**`SelectedHostId` 전달 경로 (호스트 선택 → 인게임)** — 확정 #19

```
PossessStartButton
  → PossessStartRequestedEvent 발행
  → Lobby 모듈 수신: 스태미나 검증·차감 + 저장소에 SelectedHostId 커밋
  → ISceneManager.LoadAsync(GameScene)
  → GameScene 초기화: 저장소에서 SelectedHostId 조회 → Addressable `host/{hostKey}` 로드
```

**이벤트 페이로드로 씬을 건너 전달하지 않는다.** 씬 전환 시 구독자가 소멸하므로 **단일 출처는 저장된 `SelectedHostId`** 다.
이 규칙이 서버 전환 시에도 그대로 유지된다 (데이터 제약 6-1·6-2).

**1차 범위 구분**

- ✅ 12칸 그리드 · 잠금 표현 · 상세 패널 · 스탯/얼티밋 표시 · `빙의 시작` → 인게임 진입
- 🟡 `HOST 강화` — **화면 이동까지만.** 강화 기능·화면 내용은 다음 마일스톤
- ⛔ 실제 강화 · 마스터리 · 코스튬 · 색상 변형

## 4. 데이터 (값 · 저장 항목)

| 데이터 | 타입/범위 | 저장 | 비고 |
|--------|-----------|:---:|------|
| `HostTable` | 테이블 SO **1개에 배열 12** | X | 주소 `TableData/HostTable`. 개별 `.asset` 금지 (제약 6-3) |
| ├ `hostKey` | string (소문자 12종) | X | `host/{hostKey}` 주소·아틀라스 공통 식별자 |
| ├ `nameEn` / `nameKr` | string | X | `AMAZONESS` / `아마조네스` |
| ├ `hp` `atk` `spd` `dash` | int **0~100** | X | 표시 스케일. `dash` = 구 `jump` |
| ├ `ultimateNameEn` | string | X | **정본(slide_07)** — `BLADE STORM` 등 12종 |
| ├ `ultimateDescKr` | string (2줄) | X | 수치 스펙은 `[TBD — 이유: 얼티밋 12종 수치 미착수]` |
| └ `unlockType` / `unlockChapter` / `unlockStage` | `StageReach`\|`ChapterBossClear` / int / int | X | `amazoness`는 시작 보유 (조건 없음) |
| `UserData.SelectedHostId` | string (hostKey) | **O** | 인게임이 소비하는 단일 출처 |
| `UserData.saveVersion` | int | **O** | 필수 (제약 6-5) |
| `UserData.progress` (최고 CHAPTER·STAGE, 클리어한 챕터보스) | — | **O** | 해금 평가 입력 |
| `unlockedHostKeys` | — | **X (금지)** | 저장하지 않고 progress로 **매 진입 재평가** |
| 보유 수 `{n}/12` | 파생값 | X | 해금 평가 결과에서 도출 |
| `⚡5` 소모량 | int | X | `GameConfig` 단일 관리 |

## 5. 연출 · 사운드

- **셀 선택**: 골드 프레임 점등 + 선택 마커 등장 · 짧은 셀렉트 SFX. 상세 초상 크로스페이드 `0.15s`, 스탯 바 fill 트윈 `0.25s` ease-out
- **잠금 셀 선택**: 자물쇠 좌우 흔들림 `0.2s` + 둔탁한 잠금 SFX. `빙의 시작` 버튼은 반응하지 않는다
- **`GhostArrowGroup`**: 고스트 상하 부유 루프 `2s`, `»` 화살표 순차 점멸 — 시선을 그리드→상세로 유도 (목업 요소)
- **빙의 시작**: 골드 버튼 플래시 → 고스트가 상세 초상으로 빨려드는 빙의 이펙트 `0.6s` → 화면 전환 페이드
- **픽셀 규율 (확정 #16)**: Point 필터 · **정수 배율** · 후처리 최소. 트윈은 알파/위치만, 비정수 스케일 금지
- **BGM**: 로비 BGM 지속 (화면 전환음 없음) · **빙의 시작 순간에만** 스팅어 1회

## 6. 의존 · 연결

| 대상 | 관계 |
|------|------|
| **로비(HUB)** | 상단 HUD 공통 · 진입 경로 2개(`CHAPTER`/`HOST`) · 스태미나 차감 실행 주체 |
| **진행도** | 해금 평가 입력 (`StageReach` / `ChapterBossClear`) |
| **호스트 강화** | 🟡 1차 = 진입만 |
| **데이터 저장** | 저장소 인터페이스 경유 (`SelectedHostId` 커밋) |
| **인게임** | `SelectedHostId` 소비 → `host/{hostKey}` 로드 |

### 발행 이벤트

> `IEventBus` 사용. `C# event`·`Action` 직접 선언 금지. `public struct`(readonly 금지).
> 필드명은 발행자 프로퍼티명과 **다르게** 지었다.

| 이벤트 | 필드 | 발행 시점 |
|--------|------|----------|
| `HostSelectOpenedEvent` | `EntryPath` (`HostSelectEntry`: `ChapterStart`\|`HostBrowse`) | 패널 열림 |
| `HostSlotSelectedEvent` | `SelectedKey` (string) · `IsUnlocked` (bool) | 그리드 셀 탭 → 상세 카드 갱신 |
| `PossessStartRequestedEvent` | `PossessHostKey` (string) | `빙의 시작` 탭 (보유 상태에서만) |
| `HostUpgradeEntryRequestedEvent` | `UpgradeHostKey` (string) | `HOST 강화` 탭 — 1차는 화면 이동만 |

### 구독 이벤트

| 이벤트 | 용도 |
|--------|------|
| 재화 변경 이벤트 (스태미나·골드·젬) | 상단 HUD 갱신. **명칭은 로비 문서를 따른다** `[TBD — 이유: 로비 콘텐츠 문서에서 정의]` |
| `HostUnlockStateChangedEvent { ChangedKeys }` | 진행도 변화로 그리드 갱신. **1차는 진입 시 재평가로 충분** — 발행자(진행도)는 2차 |

## 7. 미정 · 결정 필요

- `[WARNING]` **고스트 위젯**: 확정 #11은 아웃게임 `Lv + EXP`이나 목업엔 `56/100` 바만 있고 **Lv 숫자가 없다.** 표기 위치는 로비 문서에서 결정.
- `[TBD — 이유: 레이아웃은 Stage 2b 소관]` 15칸(5행) → 12칸(4행)으로 줄어든 세로 여백 처리 — 셀 확대 vs 여백 유지.
- `[TBD — 이유: 얼티밋 12종 수치 스펙 미착수]` `ultimateDescKr` 12종 확정 문구. 1차는 정본 명칭 + 목업 톤의 임시 설명.
- `[TBD — 이유: 아트팀 소관]` 잠금 실루엣 = 원본 스프라이트 단색화인가, 별도 실루엣 에셋인가.
- `[TBD — 이유: 경제 밸런스 미착수]` 스태미나 회복 속도·상한 (로비 문서).

## 개정 이력

| 버전 | 날짜 | 변경 내용 |
|------|------|----------|
| 0.1 | 2026-08-07 | 최초 작성 — 목업 재현 + 승인 예외 4건 적용 + 잠금 셀 표현·이벤트 4종 신규 정의 |
