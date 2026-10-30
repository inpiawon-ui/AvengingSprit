# AVSR 안정성 점검 — 2026-10-30 (읽기 전용 · 코드 정적 점검)

- 대상: `git log --since=2026-10-28` 커밋 101개 중 Assets/Scripts 변경분(빙의 파티 · 따라 하기 · 셋 고르기 · 설명 창 · 일시정지 · 재사용 목록 · 로비 RE:BORN) + 문자열 표 + GameConfig
- 방법: 코드 · 에셋을 읽어서 따져 본 것. **게임을 실제로 돌려 재현한 것은 없다** → 아래 「추정」 항목은 모두 **미검**(기기 · 에디터에서 재현 확인 필요).
- 표기: **[확실]** = 코드 · 에셋 값으로 확인됨 / **[추정]** = 코드 구조상 그렇게 될 것으로 보이나 실행 확인 안 함

---

## 1. 위험도 순 목록

### 높음

| # | 파일:줄 | 무엇이 | 언제 터지나(재현 조건) | 고치는 방향 | 확실/추정 |
|---|---|---|---|---|---|
| H1 | `Module/InGame/BattleDirector.Coach.cs:184-196` (오버레이를 루트 캔버스 맨 위에 붙임) + `InGameMainUI.cs:1981` (`OnBackPressed` → 항상 `OnPause`) + `RbPausePanel.cs:50-53` | 따라 하기 · **셋 고르기** 오버레이(0.95 검은 막, 구멍 밖 터치 차단)가 루트 캔버스 최상단에 붙는다. 일시정지 창은 InGameMainUI(SafeArea 아래) 안에 있어 **오버레이 밑에 깔린다.** | 유령 출전 판에서 셋 고르기(매번 뜬다) 또는 첫 판 따라 하기 중 **안드로이드 뒤로 키** → 일시정지 창이 검은 막 밑에서 열리고 시간 0. 구멍을 눌러도 밑의 일시정지 바탕이 받는다(적 선택 안 됨). 뒤로 키를 다시 눌러도 `Open` 만 다시 불러 닫히지 않는다 → **앱을 꺼야 하는 멈춤** | 따라 하기 · 셋 고르기 중엔 `OnBackPressed` 를 무시(또는 일시정지를 오버레이보다 위로) · 일시정지가 열려 있으면 뒤로 키 = 닫기 | 추정(구조 근거 확실, 실행 미검) |

### 중간

| # | 파일:줄 | 무엇이 | 언제 터지나 | 고치는 방향 | 확실/추정 |
|---|---|---|---|---|---|
| M1 | `Module/Common/UI/FpsOverlay.cs:20` | FPS · 메모리 표시 `s_enabled = true` — 납품 빌드 화면 맨 아래에 늘 뜬다. 글자도 한국어 고정(「최저 · 메모리」, 94줄) | 모든 빌드, 항상 | 납품 빌드 전 `false` (또는 `Debug.isDebugBuild` 일 때만) | 확실 |
| M2 | `BattleDirector.Party.cs:546-557` (사망 선택 시계 `unscaledDeltaTime`) + `RbPausePanel` | 메인 사망 선택창의 제한 시간이 **실제 시간**으로 흐르고, 일시정지는 `RbGuide.PausingOpen` 같은 깃발을 안 세운다(시간 배율 0 만) → 일시정지 중에도 시계가 줄어 0 이 되면 `_ghostHp = 0` · `Finish(false)` 로 **판이 패배로 끝난다** | 메인이 쓰러져 선택창이 뜬 상태에서 뒤로 키(일시정지) 후 몇 초 그대로 두기. 일시정지 창은 선택창 밑에 깔려 결과 화면이 일시정지 위/아래로 겹칠 수 있음 | 일시정지가 열려 있으면 `TickPartyHold` 시계 멈춤(일시정지도 정적 깃발 세우기) | 추정 |
| M3 | `BattleDirector.Juice.cs:168-175` (`TickHitStop` 이 실제 시간으로 풀며 `timeScale = 1`) + `RbPausePanel.cs:52` · `InGameMainUI.RbExtras.cs:298` | 일시정지 · 밤 호스트 고르기는 **시간 배율 0 만으로** 판을 멈춘다. 타격 멈칫(0.25배) 진행 중에 열면 `TickHitStop` 이 1 로 되돌려 **창 뒤에서 판이 돈다**(RbGuide 는 같은 문제를 겪어 `PausingOpen` 으로 고쳤음 — 주석 2026-10-30) | 큰 타격 직후 0.1초 안에 일시정지 / 밤 고르기 창이 열릴 때 | 일시정지 · 밤 고르기도 `PausingOpen` 같은 깃발로 `BattleDirector.Update` 를 세우기 | 추정(같은 원인이 RbGuide 에서 실제로 났음) |
| M4 | `RbPausePanel.cs:52` · `InGameMainUI.RbExtras.cs:298,334` | 밤 호스트 고르기(시간 0) 중 일시정지를 열면 `_scaleBefore` 가 0 대신 **1** 로 저장 → 일시정지를 닫으면 고르기 창이 떠 있는데 시간이 1 로 돌아 무리가 다가온다. 설명 창 위에서도 같음(설명은 `PausingOpen` 이 막아 피해는 적음) | 밤 모드 시작 고르기 창에서 뒤로 키 → 계속하기 | 밤 고르기도 깃발로 멈추기(M3 과 같은 수정), 일시정지는 0 이었으면 0 으로 되돌리기 | 추정 |
| M5 | `BattleDirector.PartyHud.cs:116` (`TickPossessBubble`) · `BattleDirector.PartySyn.cs:118-122` (`CountSynergy`, `TickParty` 에서 매 프레임) | **매 프레임 문자열 할당.** 말풍선 서명 `target.Key + "\|" + count + "\|" + _inyeonKey`(빙의 가능한 적이 있는 동안 매 프레임), 무리 시너지 줄 `lines.Add(Tf(...))`(무리 2명 이상이면 매 프레임 `string.Format` + params 배열 박싱) — 규칙 coding_conventions 10절 위반 | 파티가 2명 이상이고 같은 무리가 있으면 판 내내 → GC 스파이크 · 메모리 증가 | 서명은 정수 · 참조 비교로, 시너지 줄은 값이 바뀔 때만 다시 만들기 | 확실(할당 발생) · 프레임 영향은 미검 |
| M6 | `Module/Common/UI/RecyclingList.cs:72-77` | 칸 수를 **뷰포트 높이로 한 번 정한다.** 목록을 처음 채울 때 레이아웃이 아직 안 잡혀 높이가 0 이면 칸이 2개만 생기고, 다음 `SetCount` 전까지 늘지 않아 **스크롤하면 줄이 비어 보인다** | 비활성 · 방금 켠 창에서 같은 프레임에 `SetCount` 를 부를 때(결과창 보상 줄 등) | `EnsureItems` 를 `Refresh` 에서도 부르거나, 높이 0 이면 `Canvas.ForceUpdateCanvases` 후 계산 | 추정 |

### 낮음

| # | 파일:줄 | 무엇이 | 언제 | 고치는 방향 | 확실/추정 |
|---|---|---|---|---|---|
| L1 | `BattleDirector.StartTrio.cs:50-54` · `Coach.cs:59` | 셋 고르기가 서기 전(첫 0.8~4초)에 플레이어가 적을 직접 눌러 빙의하면 `_trioPending` 이 영영 true → `TickCoach` 가 늘 false 를 돌려 **그 판 따라 하기(보조 안내)가 전부 안 뜬다**(멈춤은 아님) | 첫 판, 설명 창이 뜨기 전 적을 빠르게 누름 | `_host != null` 이면 `_trioPending = false` | 추정 |
| L2 | `InGameMainUI.cs:1981-1985` | 일시정지가 열려 있어도 뒤로 키 = 다시 열기(닫기가 아님). 06_ui 뒤로가기 2·3순위(열린 창 닫기)와 다름 | 일시정지 중 뒤로 키 | 열려 있으면 닫기 | 확실 |
| L3 | `BattleDirector.Keyboard.cs` (`TickKeyboardMove`, `BattleDirector.cs:3677` 에서 호출, `#if` 없음) | 「임시 · 테스트 전용」 WASD 이동이 빌드에 들어간다 | 블루투스 키보드 · 크롬북 등 키보드가 붙은 기기 | `#if UNITY_EDITOR` 로 감싸거나 지우기 | 확실(빌드 포함) · 영향 작음 |
| L4 | `BattleDirector.Coach.cs:184-189` · `PartyPopup.cs:44-67` | 따라 하기 · 파티 선택창이 화면 위 일시정지 단추까지 덮는다(의도). H1 과 묶어서 볼 것 | — | H1 수정에 포함 | 확실 |
| L5 | `Assets/BundleResource/TableData/StringTable.asset` | **쓰레기 항목 33개** — 여러 줄 문장이 예전에 잘못 들어가 문장 조각이 키가 됨(키 `다`·`す`·`보기 ›`·`ui.result.clear챕터 {0} 클리어!`·`ui.lobby.search.claim보상 받기` 등). 진짜 키(`ui.rb.guide.*.body`·`ui.result.clear`·`ui.lobby.search.claim`)는 정상 값이 따로 있어 화면 노출은 없음 | 화면 영향 없음 · 표만 지저분 | TSV → 표 반영 시 TSV 에 없는 키 정리 | 확실 |
| L6 | 같은 표 | 중국어(번체 · 간체) 빈 키 일부 — `ui.summon.grade` · `ui.shop.reroll` · `ui.lobby.season_pass(.state)` · `ui.lobby.event(.state)`(TSV 에도 없음). 일본 납품과 무관 | 중국어 선택 시 한국어 원문 | TSV 에 추가 | 확실 |
| L7 | 에디터 스위치(`PartyRule.Enabled` 등 EditorPrefs) | 에디터 값과 빌드 값이 다를 수 있다. 빌드 값은 파티 켬 · 상성 켬 · 사신 수확 끔 · 테마 순환 끔 · 방당 적 1 끔 · 방마다 보스 끔으로 **모두 정상** | 에디터에서 끈 채 시험하면 빌드와 다르게 보임 | 시험 전 에디터 스위치 확인 | 확실 |
| L8 | 시간 배율 전역 복구 없음 | 인게임 진입 시 `Time.timeScale = 1` 을 강제하는 곳이 없다(창마다 스스로 되돌림에 의존) | 위 M3·M4 처럼 한 곳이 0 을 남기면 로비 연출까지 멈춤 | 씬 로드 시 1 로 되돌리는 한 줄(게임 코드 쪽) | 추정 |

---

## 2. 점검했고 문제 없던 것

- **에디터 전용 코드**: `BattleDirector.AutoPilot.cs` · `BattleDirector.TestGui.cs` 는 파일 전체가 `#if UNITY_EDITOR`, 호출부(`BattleDirector.cs:3662-3664`)도 감싸져 있음. `PerfProbe` · `LanguageFontSet` · 판정 상자(F1) 도 감쌈. EditorPrefs 스위치는 전부 `#else` 빌드 값이 있음 → 빌드 컴파일 오류 경로 없음(정적 확인, 실제 빌드 컴파일은 미검).
- **테스트 스위치(GameConfig.asset)**: `_testMode 0` · `_openAllHosts 0` · `_powerTest 0` · `_sandboxMode 0` · `_sandboxNoExecute 0` · `_testPlayerDamage 0` — 전부 꺼짐. 코드 고정 스위치: `OpeningMainUI.AlwaysShow = false`, `EventOffersDisabled = false`, 빌드 옵션 `BuildOptions.None`(개발 빌드 아님) · 안드로이드 IL2CPP. 무적 `_invuln = 999` 는 샌드박스에서만.
- **일본어 빈칸**: TSV(`AVSR_Strings.tsv`) 1993줄 중 일본어 빈칸 0. 표 에셋에서 일본어가 빈 33개는 전부 L5 의 쓰레기 항목. 코드가 글자로 적은 키(`Tx`·`Tf`·`Localize.Get/Format/FromTable`)는 표에 모두 있음. 새 키(`ui.tut.coach.*` · `ui.rb.guide.*` · `ui.party.p2.*`) 일본어 있음.
- **이벤트 구독 해제**: `InGameMainUI`(OnDisable 에서 토큰 전부 Dispose) · `RbLobbyUI`(OnEnable 구독 · OnDisable 해제) 짝 맞음. `BattleDirector` 는 EventBus 구독 없음.
- **비동기**: 이번 변경의 UniTask 는 `GetCancellationTokenOnDestroy` + `SuppressCancellationThrow` 또는 `this == null` 확인을 거침(설명 창 지연 · 경고 띠 · 소환 · 상자 열기 · 육성 띠). `LoadEventHostAsync` 는 await 뒤 `u == null || this == null` 확인, 아틀라스 실패는 잡아서 그림 없이 진행.
- **Addressables 해제**: 유닛 아틀라스는 씬 범위(판을 나가면 풀림), 방 바닥 · 옆벽 · 구름 · 앞치마는 교체 시 `Release`.
- **설명 창(RbGuide)**: 닫기 · 씬 전환 · 비활성 어느 길이든 `OnDisable` 에서 시간 · `PausingOpen` 을 되돌림. 따라 하기 선택창 줄 오버레이는 선택창 자식이라 선택창과 같이 사라짐.
- **세이브**: 키 `avsr_userdata_0116`, `saveVersion 8`. 10-29 육성 개편(`hostStatLevels` → `hostSkillLevels` · `inyeonClaimed`)은 `NormalizeSkills` 로 길이를 맞추고 null 을 채움 → 옛 저장을 읽어도 예외 없음(옛 강화 값은 버려짐 — 개발 단계 결정대로). JsonUtility 라 없는 칸은 기본값.
- **포기 · 판 끝 중복**: `GiveUp` · `Finish` 모두 `_running` 확인 → 결과 화면에서 포기를 눌러도 두 번 지급 안 됨.

## 3. 미검(못 잰 것)과 채울 방법

- 위 「추정」 전부 — 실행 재현이 없다. 특히 **H1 은 기기에서 첫 판(또는 유령 출전) 셋 고르기 화면에서 뒤로 키 한 번**으로 바로 확인 가능.
- 실제 빌드 컴파일(`#if` 밖 참조) — 정적 대조만 했다. 납품 APK 를 구울 때 콘솔 오류 0 확인 필요.
- M5 의 실제 GC 양 — 프로파일러 `GC.Alloc` 로 판 중 확인 필요.
- 로비 RE:BORN 화면(`RbGrowthPage` 등) 전체 줄 단위 점검은 하지 않았다(비동기 · 구독 패턴만 훑음).
