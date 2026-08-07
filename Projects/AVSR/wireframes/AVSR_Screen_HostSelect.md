# 화면 설계서 — 호스트 선택 (Host Select)

> 기획 **Stage 2b**. 입력: [`AVSR_Content_HostSelect.md`](../AVSR_Content_HostSelect.md) · 상위 제약: [`AVSR_Decisions.md`](../AVSR_Decisions.md)
> 목업 단일 권위: `Reference/Mockups/host_select.jpeg` (683×1024 · 육안 확인 완료)

- **화면명**: `HostSelect` · **소속 콘텐츠**: `진입·플로우 #13` · **UI 타입(최상위)**: `HostSelectPanel` (`~Panel`)
- **박스 목업**: `AVSR_Screen_HostSelect.html` · **기준 해상도**: `720 × 1280` (9:16 네이티브)
- **Addressable**: `UI/Lobby/HostSelectPanel` · 라벨 `label_lobby` · 아틀라스 `atlas/hostselectpanel`
- **Version**: `0.1` · **Last Updated**: `2026-08-07` · **Status**: `게이트 B2 검토 대기`

---

## 0. 화면 인벤토리 · 네비게이션

| ID | 화면 | UI 타입 |
|----|------|---------|
| S1 | Title | `TitleMainUI` (`~UI`) |
| S2 | Lobby | `LobbyMainUI` (`~UI`) |
| S3 | **HostSelect** | `HostSelectPanel` (`~Panel`) — `LobbyMainUI` **직하** (`PopupParent` 아님) |

```
S2 ─ ContinueButton / ChapterButton ─▶ S3 (EntryPath = ChapterStart)
S2 ─ HostButton ────────────────────▶ S3 (EntryPath = HostBrowse)
S3 ─ PossessStartButton ────────────▶ GameScene      S3 ─ 뒤로가기 ─▶ S2
```
> **화면 1개 · 진입 경로 2개.** 두 경로 모두 `PossessStartButton` 활성 (동일 화면이므로 조회 경로에서도 시작 가능).

## 1. 목적 · 진입/이탈
- 목적: **"이번 판을 어떤 몸으로 시작할 것인가"** 결정. 동시에 잠긴 11칸이 항상 보이는 **목표 게시판**.
- 진입: S2의 `ContinueButton`·`ChapterButton`(`ChapterStart`) / `HostButton`(`HostBrowse`)
- 이탈: `PossessStartButton` → `GameScene` / 뒤로가기 → `SetActive(false)`로 S2 복귀 (`06_ui.md` 3순위)

## 2. 레이아웃 개요
좌우 2열 + 상하 3단. **① 상단 HUD `0~128`**(S2 소유·공유, 이 패널 밖) → **② 헤더 `144~276`**(문구 + 고스트/포탈 장식)
→ **③ 본문 `288~932`**: 좌 `HostGrid`(3×4) / 중 `GhostArrowGroup`(시선 유도) / 우 `HostDetailCard`
→ **④ CTA 2단 `952~1148`**(우측 정렬) → **⑤ `TipBar` `1166~1244`**(전폭).

**9:16 재구성 판단** — 목업 683×1024를 폭 기준 ×1.054(→1080) 한 뒤 남는 **+200px 배분**:
`헤더 111→132 (+21)` · `그리드 셀 높이 126→140 (+14/셀)` · `CTA 2버튼 간격·높이 +34` · `TipBar 여백 +26` · **잔여는 좌하단 배경 노출로 흡수**.

**미결 해소 — 호스트 그리드 세로 여백 (5행 15칸 → 4행 12칸)**
> **결론: 셀 확대(단, 정수 배율 범위 내) + 잔여는 여백/배경 연장.**
> 가로는 3열이 폭을 결정하므로 선택의 여지가 없다(`108px` 고정). 세로만 `126 → 140`으로 올렸다.
> **근거**: 셀 내부 초상은 **목업 크롭 원본 `91×84`를 ×1로 그대로 쓴다**(`source=CROP` — 원작 캐릭터라 재생성 시 얼굴이 달라진다).
> 그러면 셀은 `초상 84 + 이름행 30 + 패딩 26 = 140`이 자연 도출되고, 폭 `91`은 `108` 셀에 좌우 8px 여유로 들어간다.
> 셀을 더 키우려면 초상을 ×2(`182×168`)로 올려야 하는데 그러면 셀 폭이 `200`을 넘어 **3열이 720에 들어가지 않는다.**
> 반대로 여백만 남기면 화면 하단이 비어 미완성으로 읽힌다.
> **확정 #16(정수 배율) + 크롭 원본 해상도(91×84)가 함께 셀 크기의 상한을 정한다** — 이것이 "셀 확대 vs 여백 유지"의 판정 기준이다.
> 4행 그리드(`330~920`)는 `HostDetailCard`(`288~932`) 하단과 12px 이내로 정렬되어 좌우 균형이 유지된다.
> 남는 좌하단(`x 12~356, y 920~1148`)은 **목업이 이미 배경을 노출하던 영역**이며, 확장분은 배경 연장으로 흡수한다(요소 왜곡 아님).

## 3. 요소 트리 (핵심)

> `이름` = 프리팹 GameObject 이름 = 클라 바인딩 키. 좌표 `(x,y,w,h)`는 **화면 절대좌표** `720×1280` 좌상단 원점 px.
> 범위: ✅ 1차 동작 / 🟡 진입만 / ⛔ 1차 밖

| 요소 이름 | UI 타입/컴포넌트 | 위치·anchor (x,y,w,h) | 표시 데이터 | 입력→이벤트 | 범위 |
|-----------|-----------------|----------------------|-------------|-------------|:---:|
| `TopHudGroup` ⭐ | — | `(0,0,720,128)` | **S2 `LobbyMainUI` 소유 · 이 패널의 자식 아님** | — | ✅ |
| **`HostSelectPanel`** | **`~Panel`** | anchorMin `(0,1)` / anchorMax `(1,1)` / pivot `(0.5,1)` / anchoredPos `(0,-128)` / sizeDelta **`(0,1152)`** → 화면 `y 128~1280` | — | — | ✅ |
| `HostSelectBackground` | Image | Stretch Full `(0,128,720,1152)` | 던전 배경 | — | ✅ |
| **`HeaderGroup`** | RectTransform 그룹 | `(0,144,720,132)` | — | — | ✅ |
| ├ `HeaderGhostDeco` | Image | `(12,146,84,92)` | 소형 고스트 장식 | — | ✅ |
| ├ `HeaderTitleText` | TMP (rich, 2줄) | `(104,148,396,92)` | `첫 번째` / `HOST 를 선택하세요!` (**`HOST`만 골드**) | — | ✅ |
| ├ `HeaderDescText` | TMP | `(104,248,496,26)` | `사망 시 유령으로 돌아가, 다른 HOST에 빙의할 수 있습니다.` | — | ✅ |
| └ `HeaderPortalDeco` | Image | `(500,138,208,144)` | 포탈 장식 (배경 레이어) | — | ✅ |
| `HostListTitle` | Image 9-slice | `(12,288,344,34)` | 해골 장식 좌우 1쌍 | — | ✅ |
| └ `HostListTitleText` | TMP | `(12,290,344,30)` | `HOST LIST` | — | ✅ |
| **`HostGrid`** | GridLayoutGroup | `(12,330,344,590)` · cell `108×140` · spacing `(10,10)` · **3열 × 4행 = 12칸 · 스크롤 없음** | — | — | ✅ |
| └ `HostSlot` ×12 | Button (프리팹 인스턴스) | 위 셀 | — | 탭 → `HostSlotSelectedEvent{SelectedKey, IsUnlocked}` | ✅ |
| &nbsp;&nbsp;├ `HostSlotFrame` | Image 9-slice | `(0,0,108,140)` | 기본/선택(골드) 2상태 · 잠금=무채색 | — | ✅ |
| &nbsp;&nbsp;├ `HostSlotPortrait` | Image | center `(9,6,91,84)` | **목업 크롭 ×1** (`Reference/Hosts/{hostKey}.png`) · 잠금=단색 실루엣 | — | ✅ |
| &nbsp;&nbsp;├ `HostSlotNameText` | TMP | `(4,104,100,30)` | 한글명 (**잠금도 표시**) | — | ✅ |
| &nbsp;&nbsp;├ `HostSlotLockIcon` | Image | center `(34,34,40,40)` | 자물쇠 (잠금 시만) | — | ✅ |
| &nbsp;&nbsp;└ `HostSlotSelectMarker` | Image | top-left `(2,2,28,28)` | 선택 마커 (선택 시만) | — | ✅ |
| **`GhostArrowGroup`** | RectTransform 그룹 | `(362,500,42,100)` | — | — | ✅ |
| ├ `GhostArrowSprite` | Image | `(362,500,42,58)` | 고스트 (상하 부유 `2s`) | — | ✅ |
| └ `GhostArrowChevron` | Image | `(364,564,38,32)` | `»` 순차 점멸 | — | ✅ |
| **`HostDetailCard`** | RectTransform 그룹 | `(408,288,300,644)` | — | — | ✅ |
| ├ `HostDetailFrame` | Image 9-slice | Stretch `(408,288,300,644)` | — | — | ✅ |
| ├ `HostNameEnText` | TMP | `(414,304,288,38)` | `AMAZONESS` / 잠금 = **표시** | — | ✅ |
| ├ `HostNameKrText` | TMP | `(414,344,288,26)` | `아마조네스` / 잠금 = **표시** | — | ✅ |
| ├ `HostPortalPedestal` | Image | `(442,548,232,58)` | 원형 발판 | — | ✅ |
| ├ `HostPortraitImage` | Image | `(467,378,182,168)` | **목업 크롭 ×2 정수 확대** · 잠금 = 실루엣 | — | ✅ |
| ├ `HostDetailLockIcon` | Image | center `(510,414,96,96)` | 대형 자물쇠 (잠금 시만) | — | ✅ |
| ├ `StatGroupLabel` | TMP | `(428,618,92,22)` | `능력치` | — | ✅ |
| ├ **`StatRow_HP`** | RectTransform 그룹 | `(424,648,270,32)` | — | — | ✅ |
| │ &nbsp;├ `StatIcon_HP` | Image | `(424,650,28,28)` | 적색 하트 | — | ✅ |
| │ &nbsp;├ `StatLabelText` | TMP | `(458,652,62,24)` | `HP` | — | ✅ |
| │ &nbsp;├ `StatBarBg` | Image 9-slice | `(526,656,124,16)` | — | — | ✅ |
| │ &nbsp;│ &nbsp;└ `StatBarFill` | Image (Filled) | `(526,656,124,16)` | `hp / 100` · 잠금 = **0% 무채색** | — | ✅ |
| │ &nbsp;└ `StatValueText` | TMP | `(656,652,38,24)` | `72` · 잠금 = **`???`** | — | ✅ |
| ├ **`StatRow_ATK`** | RectTransform 그룹 | `(424,686,270,32)` | — | — | ✅ |
| │ &nbsp;├ `StatIcon_ATK` | Image | `(424,688,28,28)` | 검 (주황) | — | ✅ |
| │ &nbsp;└ `StatLabelText`/`StatBarBg`/`StatBarFill`/`StatValueText` | 위와 동일 구성 | `y+38` | `ATK` · `68` | — | ✅ |
| ├ **`StatRow_SPD`** | RectTransform 그룹 | `(424,724,270,32)` | — | — | ✅ |
| │ &nbsp;├ `StatIcon_SPD` | Image | `(424,726,28,28)` | 부츠·바람 (청) | — | ✅ |
| │ &nbsp;└ `StatLabelText`/`StatBarBg`/`StatBarFill`/`StatValueText` | 위와 동일 구성 | `y+76` | `SPD` · `82` | — | ✅ |
| ├ **`StatRow_DASH`** ⚠️ | RectTransform 그룹 | `(424,762,270,32)` | ← **예외 ① 적용처 1/2** | — | ✅ |
| │ &nbsp;├ `StatIcon_DASH` ⚠️ | Image | `(424,764,28,28)` | **잔상 회피 모션 (녹)** — ⚠️ 점프 아이콘 재사용 금지 | — | ✅ |
| │ &nbsp;└ `StatLabelText`/`StatBarBg`/`StatBarFill`/`StatValueText` | 위와 동일 구성 | `y+114` | **`DASH`** · `90` | — | ✅ |
| └ **`UltimateCard`** | Image 9-slice | `(424,810,268,110)` | — | — | ✅ |
| &nbsp;&nbsp;├ `UltimateLabel` | TMP | `(432,816,120,22)` | `ULTIMATE` | — | ✅ |
| &nbsp;&nbsp;├ `UltimateIcon` | Image | `(434,842,60,60)` | 잠금 = 실루엣 | — | ✅ |
| &nbsp;&nbsp;├ `UltimateNameText` ⚠️ | TMP | `(502,842,184,26)` | **`BLADE STORM`** ← 예외 ④ / 잠금 = `???` | — | ✅ |
| &nbsp;&nbsp;└ `UltimateDescText` | TMP (2줄) | `(502,870,184,46)` | 얼티밋 설명 / **잠금 = 해금 조건 텍스트로 대체** | — | ✅ |
| **`HostUpgradeButton`** | Button+Image 9-slice **블루** | `(360,952,348,82)` | 잠금 = **비활성** | → `HostUpgradeEntryRequestedEvent{UpgradeHostKey}` | 🟡 |
| ├ `HostUpgradeIcon` | Image | `(372,972,40,40)` | 상승 화살표 강화 아이콘 | — | 🟡 |
| ├ `HostUpgradeTitleText` | TMP | `(422,960,220,32)` | `HOST 강화` | — | 🟡 |
| ├ `HostUpgradeSubText` | TMP | `(422,992,220,24)` | `능력치 · ULTIMATE · 속도` | — | 🟡 |
| └ `ArrowIcon` | Image | `(664,984,24,32)` | `›` 셰브런 | — | 🟡 |
| **`PossessStartButton`** | Button+Image 9-slice **골드 CTA** | `(360,1046,348,102)` | 잠금 = **비활성(무채색·탭 무반응)** | → `PossessStartRequestedEvent{PossessHostKey}` | ✅ |
| ├ `PossessGhostIcon` | Image | `(376,1069,56,56)` | 고스트 아이콘 | — | ✅ |
| ├ `PossessTitleText` | TMP | `(444,1058,200,42)` | `빙의 시작` | — | ✅ |
| ├ `PossessSubText` | TMP | `(444,1100,200,26)` | `(POSSESS)` | — | ✅ |
| └ `ArrowIcon` | Image | `(664,1081,24,32)` | `›` 셰브런 | — | ✅ |
| **`TipBar`** | Image 9-slice | `(12,1166,696,78)` | — | — | ✅ |
| ├ `TipIcon` | Image | `(24,1180,28,28)` | 💡 | — | ✅ |
| ├ `TipText` ⚠️ | TMP (2줄) | `(60,1176,400,60)` | **`HOST마다 이동속도, 대시(회피) 속도, 공격 방식이 다릅니다. / 다양한 HOST를 경험해 보세요!`** ← 예외 ① 적용처 2/2 | — | ✅ |
| ├ `OwnedHostChestIcon` | Image | `(474,1188,40,36)` | 보물상자 | — | ✅ |
| └ `OwnedHostCountText` ⚠️ | TMP | `(522,1188,180,36)` | **`보유 HOST 1/12`** ← 예외 ③ | — | ✅ |
| **`PopupParent`** | RectTransform | Stretch Full · sizeDelta `(0,0)` · **최하위 자식** | — | — | ✅ |

**목업 예외 적용 위치 (승인 4건 — 전부 반영 확인)**
① `JUMP`→`DASH`: `StatRow_DASH` + `TipText` **2곳** / ② 15칸→**12칸**: `HostGrid` 3×4 / ③ `12/12`→`1/12`: `OwnedHostCountText` / ④ 얼티밋 정본: `UltimateNameText` = `BLADE STORM`
목업의 색상 변형 4칸(`람보(레이저)`·`마법사(녹색)`·`닌자(적색)` 등)은 **코스튬으로 분리**되어 그리드에서 제외했다 (확정 #10).

## 4. 상태·분기

**그리드 순서** = `AVSR_Decisions.md` §4 표 #1~12 고정 (해금 순서 = 표시 순서).
**기본 선택** = 저장된 `SelectedHostId`가 보유 중이면 그것, 아니면 **보유 중 표 순번 최소값**. **잠금 셀을 기본 선택하지 않는다.** 신규 유저는 항상 `amazoness`.
**해금 판정** = 매 진입 시 `progress`로 재평가. `unlockedHostKeys`는 저장하지 않는다.

| 요소 | 보유 상태 | 잠금 상태 |
|------|-----------|-----------|
| `HostSlotPortrait` | 정상 초상 | 단색 실루엣 |
| `HostSlotFrame` | 기본 / 선택 시 골드 | 무채색 |
| `HostSlotLockIcon` | 숨김 | 표시 (중앙) |
| `HostSlotNameText` | 표시 | **표시 (동일)** |
| `HostPortraitImage` / `HostDetailLockIcon` | 초상 / 숨김 | 실루엣 / 표시 |
| `HostNameEnText` · `HostNameKrText` | 표시 | **표시 (동일)** |
| `StatBarFill` / `StatValueText` | 실값 | **0% 무채색 / `???`** |
| `UltimateIcon` / `UltimateNameText` | 정상 / 정본명 | 실루엣 / `???` |
| `UltimateDescText` | 얼티밋 설명 2줄 | **해금 조건 텍스트** (신규 요소 추가 없이 슬롯 재활용) |
| `PossessStartButton` · `HostUpgradeButton` | 활성 | **비활성** (무채색 · 라벨 유지 · 탭 무반응) |

**잠금 셀도 탭 가능**하다 — 상세 패널이 갱신된다 `[근거: 목표 게시판 기능. 못 누르면 목표가 되지 않는다]`.
**해금 조건 문구** (유형 2종만): `StageReach` → `CHAPTER {c} · STAGE {s} 도달 시 해금` / `ChapterBossClear` → `CHAPTER {c} 보스 처치 시 해금`

**빙의 시작 순서**: ① 보유 검증(방어) → ② **스태미나 `⚡5` 검증**(부족 시 `SystemPopup` 후 중단) → ③ `SelectedHostId` 저장소 커밋 → ④ 차감 → `GameScene` 로드.
**이벤트 페이로드로 씬을 건너 전달하지 않는다** — 단일 출처는 저장된 `SelectedHostId`.

## 5. 연결
- 발행: `HostSelectOpenedEvent{HostSelectEntry EntryPath}` · `HostSlotSelectedEvent{string SelectedKey, bool IsUnlocked}` · `PossessStartRequestedEvent{string PossessHostKey}` · `HostUpgradeEntryRequestedEvent{string UpgradeHostKey}`
- 구독: `CurrencyChangedEvent`(HUD 갱신 — S2 소관) · `HostUnlockStateChangedEvent{ChangedKeys}` (1차는 진입 시 재평가로 충분)
- 다른 화면: 부모 = S2 `LobbyMainUI` · 이 화면이 여는 `~Popup` 없음 (`PopupParent`는 규약상 빈 채로 생성)

## 6. 미정
- `[TBD — 이유: 얼티밋 12종 수치 스펙 미착수]` `UltimateDescText` 12종 확정 문구. 1차는 정본 명칭 + 목업 톤 임시 설명 2줄.
- `[TBD — 이유: 아트팀 소관]` 잠금 실루엣이 원본 스프라이트 단색화인지 별도 에셋인지 → 에셋 매니페스트에 **셰이더/머티리얼 단색화 1순위**로 기재.
- `[TBD — 이유: 경제 밸런스 미착수]` 스태미나 회복 속도·상한 (S2 소관).
- `[WARNING]` **`~Panel` anchor 규약 해석** — `05_prefabs.md`는 `~Panel`을 "좌우 Stretch (0,0~1,1) · Width 0 · Height 1152"로 적었으나, `(0,0)~(1,1)`은 상하까지 Stretch라 sizeDelta `(0,1152)`와 함께 쓰면 높이가 `1280+1152`가 된다. 의도(= 상단 HUD 128px 노출)를 만족하는 값은 **anchorMin `(0,1)` / anchorMax `(1,1)` / pivot `(0.5,1)` / anchoredPos `(0,-128)` / sizeDelta `(0,1152)`** 이며 본 설계서는 이 해석을 따랐다. **규약 문구 정정이 필요하다 (Stage 3 착수 전).**
- `[WARNING]` **두 목업의 `GhostWidget` 불일치** — `lobby_hub.jpeg`는 `GHOST + Lv.28 + 게이지 + 56/100`, `host_select.jpeg`는 **`Lv` 표기가 없다.** HUD는 공유 요소이므로 두 목업을 동시에 100% 재현할 수 없다. **로비 목업을 정본**으로 채택했다 `[근거: 확정 #11 아웃게임 = Lv + EXP. 로비판이 이를 충족하는 유일한 표기]`. → **게이트 B2에서 확인 요망.**
- `[TBD]` `HostSlot` ×12의 런타임 GameObject 이름 규칙 — `HostSlot`(동일명 12개, 인덱스 바인딩) vs `HostSlot_{hostKey}`(고유명). **후자를 제안**한다 `[근거: 요소명=바인딩 키 규약과 정합. 디버깅 시 계층에서 식별 가능]`. Stage 3 프리팹 생성 시 확정.

## 개정 이력
| 버전 | 날짜 | 변경 |
|------|------|------|
| 0.1 | 2026-08-07 | 최초 작성 — 12칸 그리드 좌표 확정, 그리드 세로 여백 미결 해소(정수 배율 기준), 잠금 2상태 전 요소 정의 |
