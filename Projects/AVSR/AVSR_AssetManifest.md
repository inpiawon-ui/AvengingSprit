# 에셋 매니페스트 — AVSR (1차 범위 3화면) · **ChatGPT 납품 명세서**

> 기획 **Stage 2b → 3 다리.** 화면 설계서 3종의 **Image 요소**를 ChatGPT가 **그대로 실행할 수 있는 납품 명세**로 정리한 것.
> 상위 제약: [`AVSR_Decisions.md`](AVSR_Decisions.md) · 입력: `wireframes/AVSR_Screen_{Title,Lobby,HostSelect}.md`
> 워크플로우 표준: [`Template/Prompt_Registry_Template.md`](../../Template/Prompt_Registry_Template.md)
> **Version** `0.3` · **Last Updated** `2026-08-07` · **Status** `게이트 B2 재검토 대기`

**분업 구조**
```
ChatGPT :  시안 → 승인 → 전량 생성 → 개별 PNG 분리 → 파일명 지정 → 규격·투명배경 검수 → ZIP 납품
사용자   :  목업 첨부 + §A 지시문 붙여넣기 → ZIP 수령
우리     :  ZIP 검수(§B 표로 대조) → Assets/ import → 아틀라스·Addressable → 프리팹 슬롯 배선
```

---

## §A. ChatGPT 작업 지시문 (그대로 복사해 붙여넣기)

> 사용자는 **§C의 앵커 이미지를 첨부**하고 아래 블록만 붙여넣으면 된다.

```
당신은 1991년 아케이드 게임의 UI 아트를 복원하는 픽셀아트 아티스트입니다.
첨부한 목업 이미지가 정본입니다. 이것을 재현하되, 해상도만 새로 그립니다.

■ 아트 디렉션
- 스타일: 1991년 아케이드 HD 픽셀아트. Jaleco 계열의 어둡고 음습한 오컬트 액션 정서.
- 팔레트(목업 실측, 이 범위를 벗어나지 마십시오):
    배경        #050812 / #010711 / #060512
    골드·앰버   #D89000 / #E49000 / #FCB418 / #CC8400   ← 1순위 강조(CTA·재화·프레임)
    블루        #003C9C / #00489C / #489CFC             ← 2순위
    적색        #9C180C / #C00C00                       ← 경고·HP 한정
- 색 수 상한: 에셋 1장당 16색 이하. 그라데이션 남용 금지, 단계적 디더링으로 대체.
- 금지: 네온 퍼플, 마젠타, 현대적 광택(glossy/glassmorphism), 소프트 그림자.
  (HOST 버튼의 어두운 자주색은 목업 실측값이므로 허용 — 채도 낮은 딥 퍼플만.)
- 발광은 후처리가 아니라 스프라이트 픽셀에 직접 그려 넣습니다.

■ 픽셀 규율 (가장 중요 — 위반 시 전량 반려)
- 다운스케일은 반드시 Nearest Neighbor만 사용. Bilinear/Bicubic/Lanczos 금지.
- 축소 배율은 정수만 (×2, ×3, ×4). 비정수 축소 금지.
- 안티앨리어싱 금지. 가장자리는 계단 형태로 딱 떨어져야 합니다.
- 최종 파일에 반투명 픽셀이 있으면 안 됩니다 (알파는 0 또는 255).

■ 텍스트 금지
- 이미지 안에 글자·숫자·기호를 절대 넣지 마십시오. 모든 문자는 게임 엔진이 렌더합니다.
- 단 하나의 예외: logolockup.png (계약 IP 로고). 이 파일만 글자를 포함합니다.

■ 앵커 이미지 사용 규칙
- 첨부 목업은 톤·색·프레임 스타일·구도의 참조용입니다.
- 목업을 확대해서 쓰지 마십시오. 목업을 보고 같은 디자인을 목표 해상도로 새로 그립니다.
- **모든 항목을 생성합니다.** `source` 열은 앵커 첨부 여부를 뜻합니다 (아래 참조).

■ source 열의 의미
- GPT       : 새로 그립니다.
- GPT+ANCHOR: 해당 영역을 잘라낸 앵커 이미지를 첨부합니다. 정체성(로고 글자·원작 얼굴)이 흔들리면 안 되는 항목입니다.
              (계약 IP 로고·원작 캐릭터 초상 — 재생성하면 형태가 달라져 사용할 수 없습니다.)
              앵커는 형태 참조용입니다. 노이즈를 따라 그리지 말고 목표 규격으로 선명하게 새로 그리십시오.
              원본의 실루엣·색 배치·비율을 유지하십시오.

■ 작업 순서
1) 먼저 아래 "히어로" 5장만 시안으로 만들어 보여주십시오:
     lobbybackground / continuebutton / goldicon / hostslotframe / hostdetailframe
2) 제가 승인하면, 승인된 시안을 톤 레퍼런스로 삼아 나머지를 전량 생성하십시오.
   (톤 정렬은 완성본끼리만 합니다. 초안을 재료로 쓰지 마십시오.)
3) 전량 생성 후 규격·투명배경·색 수를 자체 검수하고 어긋난 것은 재생성하십시오.

■ 납품 형식
- 개별 PNG 파일 (파일명은 명세표의 filename 열 그대로. 대문자·공백·한글 금지)
- manifest.csv : filename,width,height,alpha,slice9,pivot 6개 열
- 위 전부를 ZIP 1개로 묶어 주십시오.
- width/height는 명세표와 1px도 달라서는 안 됩니다.

■ 명세표
(아래 §B 표를 여기에 붙여넣습니다)
```

---

## §B. 납품 파일 명세표

> **열 이름을 바꾸지 마십시오** — 검수 스크립트가 이 표를 그대로 읽는다.
> `pivot`: 게이지 fill은 전부 `left`(채워지는 방향), 선택 마커는 `top-left`, 그 외 `center`.
> `slice9`: 9-slice Border `L,R,T,B` / 해당 없으면 `-`.

| filename | element | width | height | alpha | slice9 | pivot | source | note |
|----------|---------|------:|-------:|:---:|--------|-------|--------|------|
| `backgroundimage.png` | `BackgroundImage` | 720 | 1280 | N | - | center | GPT | S1 배경. 야경 도시 실루엣, 로비보다 더 어둡고 요소 적게. 상단 절반은 여백 |
| `titlelogo.png` | `TitleLogo` | 582 | 306 | Y | - | center | **GPT+ANCHOR** | `logolockup` **×3 정수 확대**. 앵커: `ui_crops/logolockup_x3.png` — 로고 형태를 최대한 보존 |
| `tophudbackground.png` | `TopHudBackground` | 720 | 128 | Y | 8,8,4,6 | center | GPT | 어두운 반투명 바 + 하단 골드 1px 라인 |
| `ghostwidget.png` | `GhostWidget` | 188 | 96 | Y | 10,10,10,10 | center | GPT | 청색 테두리 패널, 내부 비움 |
| `ghostportraiticon.png` | `GhostPortraitIcon` | 60 | 60 | Y | - | center | GPT+ANCHOR | 고스트 얼굴 버스트. 크롭 원본 ~48×52로 작고 JPEG 노이즈 지배 → 리드로우 |
| `ghostexpbarbg.png` | `GhostExpBarBg` | 106 | 14 | Y | 4,4,4,4 | center | GPT | 빈 트랙 (어두운 슬롯) |
| `ghostexpbarfill.png` | `GhostExpBarFill` | 106 | 14 | Y | 4,4,4,4 | **left** | GPT | 청색 `#489CFC` 채움. EXP(적색 HP 아님) |
| `staminacounter.png` | `StaminaCounter` | 126 | 60 | Y | 20,20,12,12 | center | GPT | 골드 테두리 캡슐. 9-slice라 골드/젬과 동일 소스 복제 가능 |
| `goldcounter.png` | `GoldCounter` | 132 | 60 | Y | 20,20,12,12 | center | GPT | 위와 동일 디자인 |
| `gemcounter.png` | `GemCounter` | 112 | 60 | Y | 20,20,12,12 | center | GPT | 위와 동일 디자인 |
| `staminaicon.png` | `StaminaIcon` | 32 | 32 | Y | - | center | GPT | 번개 (청백) |
| `goldicon.png` | `GoldIcon` | 32 | 32 | Y | - | center | GPT | 금화. **히어로 — 아이콘 톤 기준** |
| `gemicon.png` | `GemIcon` | 32 | 32 | Y | - | center | GPT | 컷 젬. 보라 채도 억제 |
| `plusbutton.png` | `PlusButton` | 28 | 28 | Y | - | center | GPT | 골드 원형 `+`. **3곳 재사용** |
| `notifybadge.png` | `NotifyBadge` | 24 | 24 | Y | - | center | GPT | 적색 원 + 느낌표 형태. **6곳 재사용** |
| `mailbutton.png` | `MailButton` | 48 | 56 | Y | - | center | GPT | 봉투 |
| `settingsbutton.png` | `SettingsButton` | 48 | 56 | Y | - | center | GPT | 톱니 |
| `lobbybackground.png` | `LobbyBackground` | 720 | 1280 | N | - | center | GPT | 야간 공업도시·달·네온 간판. 중앙은 캐릭터가 뜨므로 비움. **히어로 — 배경 톤 기준** |
| `ghostavatar.png` | `GhostAvatar` | 176 | 220 | Y | - | center | **GPT+ANCHOR** | 앵커: `ui_crops/ghostavatar_x3.png` — 원작 마스코트. 형태 보존 최우선 |
| `portalring.png` | `PortalRing` | 200 | 56 | Y | - | center | GPT+ANCHOR | 청색 타원 포탈 링(부감). 크롭 해상도 부족 |
| `chaptercard.png` | `ChapterCard` | 222 | 344 | Y | 24,24,24,24 | center | GPT | 석재 패널 + 청 테두리, 내부 비움 |
| `bossportrait.png` | `BossPortrait` | 84 | 84 | Y | - | center | **GPT+ANCHOR** | 앵커: `ui_crops/bossportrait_x3.png`. CH1 보스 미정이라 `MAD DOCTOR`(CH3)를 placeholder로 사용 |
| `progressbarbg.png` | `ProgressBarBg` | 154 | 20 | Y | 6,6,6,6 | center | GPT | 빈 트랙 |
| `progressbarfill.png` | `ProgressBarFill` | 154 | 20 | Y | 6,6,6,6 | **left** | GPT | 골드 `#D89000` 채움 |
| `progressrewardchest.png` | `ProgressRewardChest` | 42 | 44 | Y | - | center | GPT+ANCHOR | 보물상자. 크롭 ~40px로 작음 |
| `continuebutton.png` | `ContinueButton` | 198 | 74 | Y | 20,20,16,16 | center | GPT | 골드 CTA, 두꺼운 골드 테두리, 내부 비움. **히어로 — 버튼 톤 기준** |
| `battlepasscard.png` | `BattlePassCard` | 238 | 112 | Y | 16,16,16,16 | center | GPT | 골드 테두리 카드 프레임 |
| `battlepassart.png` | `BattlePassArt` | 90 | 80 | Y | - | center | GPT+ANCHOR | 시즌 대표 캐릭터 버스트(목업 = 군인) |
| `battlepassbadge.png` | `BattlePassBadge` | 40 | 40 | Y | - | center | GPT+ANCHOR | 방패 배지. 보라 채도 억제 |
| `battlepassbarbg.png` | `BattlePassBarBg` | 120 | 14 | Y | 4,4,4,4 | center | GPT | 빈 트랙 |
| `battlepassbarfill.png` | `BattlePassBarFill` | 120 | 14 | Y | 4,4,4,4 | **left** | GPT | 골드 채움 |
| `eventcard.png` | `EventCard` | 238 | 104 | Y | 16,16,16,16 | center | GPT | 청 테두리 카드 프레임 |
| `eventart.png` | `EventArt` | 70 | 64 | Y | - | center | GPT+ANCHOR | 적·금 리본 선물상자 |
| `dailylogincard.png` | `DailyLoginCard` | 238 | 112 | Y | 16,16,16,16 | center | GPT | 녹 테두리 카드 프레임 |
| `dailyloginart.png` | `DailyLoginArt` | 70 | 64 | Y | - | center | GPT+ANCHOR | 젬이 든 나무 상자 |
| `dailylogincheck.png` | `DailyLoginCheck` | 26 | 26 | Y | - | center | GPT | 녹색 원 + 체크. 5칸 반복 사용 |
| `featuretabbarbackground.png` | `FeatureTabBarBackground` | 616 | 108 | Y | 16,16,12,12 | center | GPT | 어두운 반투명 탭바 패널 |
| `missiontabicon.png` | `MissionTabIcon` | 48 | 48 | Y | - | center | GPT | 클립보드 + 별 |
| `achievementtabicon.png` | `AchievementTabIcon` | 48 | 48 | Y | - | center | GPT | 골드 트로피 |
| `rankingtabicon.png` | `RankingTabIcon` | 48 | 48 | Y | - | center | GPT | 3단 시상대 |
| `inventorytabicon.png` | `InventoryTabIcon` | 48 | 48 | Y | - | center | GPT | 가방 |
| `friendstabicon.png` | `FriendsTabIcon` | 48 | 48 | Y | - | center | GPT | 인물 2인 실루엣 |
| `friendstablock.png` | `FriendsTabLock` | 28 | 28 | Y | - | center | GPT | 자물쇠 오버레이. **배지와 시각적으로 구분** (잠금 ≠ 알림) |
| `hostbutton.png` | `HostButton` | 222 | 260 | Y | 20,20,20,20 | center | GPT | 딥 퍼플 테두리 프레임, 내부 비움 |
| `hostbuttonart.png` | `HostButtonArt` | 202 | 150 | Y | - | center | GPT+ANCHOR | 호스트 3인(닌자·군인·흡혈귀) 그룹. 크롭 해상도 부족 |
| `chapterbutton.png` | `ChapterButton` | 236 | 260 | Y | 20,20,20,20 | center | GPT | 골드 테두리 + 외곽 발광. 3버튼 중 가장 두드러지게 |
| `chapterbuttonart.png` | `ChapterButtonArt` | 216 | 150 | Y | - | center | GPT+ANCHOR | 성채 실루엣 + 붉은 하늘 |
| `shopbutton.png` | `ShopButton` | 222 | 260 | Y | 20,20,20,20 | center | GPT | 블루 테두리 프레임 |
| `shopbuttonart.png` | `ShopButtonArt` | 202 | 150 | Y | - | center | GPT+ANCHOR | 보물상자 + 젬 + 금화 |
| `logolockup.png` | `LogoLockup` | 194 | 102 | Y | - | center | **GPT+ANCHOR** | **계약 IP 로고.** 앵커: `ui_crops/logolockup_x3.png`. 유일하게 글자를 포함 — 자간·획 형태를 앵커 그대로 |
| `hostselectbackground.png` | `HostSelectBackground` | 720 | 1152 | N | - | center | GPT | 던전 석벽 + 우상단 포탈 광원. 채도 억제 |
| `headerghostdeco.png` | `HeaderGhostDeco` | 84 | 92 | Y | - | center | GPT+ANCHOR | 소형 고스트. `ghostavatar`와 동일 캐릭터 |
| `headerportaldeco.png` | `HeaderPortalDeco` | 208 | 144 | Y | - | center | GPT+ANCHOR | 석조 아치 게이트 + 소용돌이 포탈 |
| `hostlisttitle.png` | `HostListTitle` | 344 | 34 | Y | 40,40,6,6 | center | GPT | 좌우 해골 장식 1쌍 + 골드 라인. 중앙은 비움 |
| `hostslotframe.png` | `HostSlotFrame` | 108 | 140 | Y | 12,12,12,12 | center | GPT | 석재 셀 + 무광 강철 테두리. **히어로 — 셀 톤 기준** |
| `hostslotframe_selected.png` | `HostSlotFrame` (선택) | 108 | 140 | Y | 12,12,12,12 | center | GPT | 위와 동일 형태, 테두리만 발광 골드 |
| `hostslotframe_locked.png` | `HostSlotFrame` (잠금) | 108 | 140 | Y | 12,12,12,12 | center | GPT | 위와 동일 형태, 무채색 |
| `hostslotportrait_{hostKey}.png` | `HostSlotPortrait` | 91 | 84 | Y | - | center | **GPT+ANCHOR** | **×12.** `Reference/Hosts/{hostKey}.png` **추출 완료 ×1**. 원작 캐릭터 — 재생성 금지 |
| `hostslotlockicon.png` | `HostSlotLockIcon` | 40 | 40 | Y | - | center | GPT | 자물쇠 |
| `hostslotselectmarker.png` | `HostSlotSelectMarker` | 28 | 28 | Y | - | **top-left** | GPT | 좌상단 모서리 골드 마커 |
| `ghostarrowsprite.png` | `GhostArrowSprite` | 42 | 58 | Y | - | center | GPT+ANCHOR | 소형 고스트, 우측 향함 |
| `ghostarrowchevron.png` | `GhostArrowChevron` | 38 | 32 | Y | - | center | GPT | `»` 이중 화살표, 청 발광 |
| `hostdetailframe.png` | `HostDetailFrame` | 300 | 644 | Y | 24,24,32,24 | center | GPT | 골드 테두리 + 상단 중앙 마름모 장식, 내부 비움. **히어로 — 패널 톤 기준** |
| `hostportraitimage_{hostKey}.png` | `HostPortraitImage` | 182 | 168 | Y | - | center | **GPT+ANCHOR** | **×12.** 위 크롭의 **×2 정수 확대**. 재생성 금지 |
| `hostportalpedestal.png` | `HostPortalPedestal` | 232 | 58 | Y | - | center | GPT+ANCHOR | 원형 마법 발판(부감). 보라 채도 억제 |
| `hostdetaillockicon.png` | `HostDetailLockIcon` | 96 | 96 | Y | - | center | GPT | 대형 자물쇠 |
| `staticon_hp.png` | `StatIcon_HP` | 28 | 28 | Y | - | center | GPT | 적색 하트. **히어로 — 스탯 아이콘 톤 기준** |
| `staticon_atk.png` | `StatIcon_ATK` | 28 | 28 | Y | - | center | GPT | 주황 검 |
| `staticon_spd.png` | `StatIcon_SPD` | 28 | 28 | Y | - | center | GPT | 청색 날개 부츠 |
| `staticon_dash.png` ⚠️ | `StatIcon_DASH` | 28 | 28 | Y | - | center | GPT | **녹색 잔상 회피 스트릭**(옆으로 피하는 실루엣 + 잔상). ⚠️ **점프 아이콘 금지** — 목업의 `JUMP`가 `DASH`(패시브 회피)로 바뀐 항목이라 목업에 원본이 없다 |
| `statbarbg.png` | `StatBarBg` | 124 | 16 | Y | 4,4,4,4 | center | GPT | 빈 트랙 |
| `statbarfill.png` | `StatBarFill` | 124 | 16 | Y | 4,4,4,4 | **left** | GPT | 흰색 단색 채움. **런타임 머티리얼 tint 4색**(적·주황·청·녹)이므로 1종만 |
| `ultimatecard.png` | `UltimateCard` | 268 | 110 | Y | 16,16,16,16 | center | GPT | 보라 테두리 패널(채도 억제), 내부 비움 |
| `ultimateicon_{hostKey}.png` | `UltimateIcon` | 60 | 60 | Y | - | center | GPT | **×12.** 정본 = 제안서 `slide_07`. 목업엔 `amazoness` 1종만 존재 |
| `hostupgradebutton.png` | `HostUpgradeButton` | 348 | 82 | Y | 24,24,16,16 | center | GPT | 딥 블루 버튼, 내부 비움 |
| `hostupgradeicon.png` | `HostUpgradeIcon` | 40 | 40 | Y | - | center | GPT | 배지 안 상승 화살표 |
| `arrowicon.png` | `ArrowIcon` | 24 | 32 | Y | - | center | GPT | `›` 우측 셰브런. **2곳 재사용** |
| `possessstartbutton.png` | `PossessStartButton` | 348 | 102 | Y | 24,24,20,20 | center | GPT | 골드 CTA + 발광 테두리. 화면 최우선 버튼 |
| `possessghosticon.png` | `PossessGhostIcon` | 56 | 56 | Y | - | center | GPT+ANCHOR | 고스트 아이콘 (빙의 상징) |
| `tipbar.png` | `TipBar` | 696 | 78 | Y | 16,16,12,12 | center | GPT | 어두운 반투명 바 + 청 테두리 |
| `tipicon.png` | `TipIcon` | 28 | 28 | Y | - | center | GPT | 청색 전구 |
| `ownedhostchesticon.png` | `OwnedHostChestIcon` | 40 | 36 | Y | - | center | GPT | 소형 보물상자 |

### §B-1. `{hostKey}` 전개

위 표에서 `{hostKey}`를 포함한 **3행은 각각 12파일로 전개된다** (`AVSR_Decisions.md` §4 순서 = `constants.md` §4):

```
amazoness · rambo · wizard · ninja · mafia · hitman ·
yogamaster · dragon · robot · snowwoman · slugger · vampire
```

예: `hostslotportrait_amazoness.png` · `hostportraitimage_amazoness.png` · `ultimateicon_amazoness.png`

| 구분 | 값 |
|------|-----|
| 표 행 수 | **82행** |
| 전개 후 실제 파일 수 | **82 − 3 + 36 = 115개** |
| `source` 집계 (행 기준) | `GPT` **61** / `GPT+ANCHOR` **21** |
| `source` 집계 (파일 기준) | `GPT` **72** / `GPT+ANCHOR` **43** — **전량 생성 115개** |
| `alpha=N` | **3** (배경 3종만) |
| `slice9` 지정 | **31행** |
| `pivot=left` | **4** (게이지 fill 4종 전부 — `ghostexpbarfill` `progressbarfill` `battlepassbarfill` `statbarfill`) |

### §B-2. 앵커 크롭 — `source = GPT+ANCHOR` 에셋의 참조 영역

> ⚠️ **전량 생성으로 방침 변경(사용자 지시).** 목업에서 잘라 쓰지 않는다. 아래 좌표로 잘라낸 이미지를
> **앵커(참조 이미지)로 첨부**해 GPT가 새로 그린다. 잘라낸 앵커는 `03_anchors/ui_crops/` 에 준비되어 있다.
> 좌표는 목업 원본 픽셀 공간(`683 × 1024`) 기준 `(x, y, w, h)`.

| filename | 목업 파일 | 크롭 `(x, y, w, h)` | 배율 | 최종 규격 | 비고 |
|----------|-----------|---------------------|:---:|-----------|------|
| `logolockup.png` | `lobby_hub.jpeg` | `(248, 916, 194, 102)` | ×1 | 194×102 | **육안 검증 완료** — `RE:BORN` 하단까지 포함 |
| `titlelogo.png` | *(위 크롭 재사용)* | 동일 | **×3** | 582×306 | 별도 크롭 불필요 |
| `ghostavatar.png` | `lobby_hub.jpeg` | `(256, 294, 176, 220)` | ×1 | 176×220 | **육안 검증 완료** — 양팔·꼬리 포함 |
| `bossportrait.png` | `lobby_hub.jpeg` | `(130, 348, 84, 84)` | ×1 | 84×84 | **육안 검증 완료** — `MAD DOCTOR` 두상 |
| `hostslotportrait_{hostKey}.png` | `host_select.jpeg` | **추출 완료** → `Reference/Hosts/{hostKey}.png` | ×1 | 91×84 | 그리드 원점 `(16.7, 236.7)` · 셀 피치 `106.7×125.0` · 스프라이트 영역 `(x+8, y+4, x+99, y+88)` |
| `hostportraitimage_{hostKey}.png` | *(위 크롭 재사용)* | 동일 | **×2** | 182×168 | 별도 크롭 불필요 |

**앵커 사용 규칙**

- **모든 에셋을 GPT로 생성한다.** 목업에서 잘라 쓰지 않는다 [사용자 지시 2026-08-07].
- 위 표의 에셋은 **정체성이 흔들리면 안 되는 것들**이다 — 계약 IP 로고, 원작 마스코트, 원작 캐릭터 초상.
  따라서 목업 전체가 아니라 **해당 영역만 잘라낸 앵커**를 첨부해 GPT가 형태를 정확히 참조하게 한다.
- 앵커는 `03_anchors/ui_crops/` 에 원본과 ×3 확대본 두 벌로 준비되어 있다. **프롬프트에는 `_x3` 를 첨부**한다.
- ⚠️ `logolockup` 은 **글자를 포함하는 유일한 에셋**이다. 자간·획 형태가 앵커와 달라지면 IP 로고로 성립하지 않는다.
  생성 결과를 앵커와 나란히 놓고 반드시 육안 대조할 것.
- 원작 캐릭터 초상(`hostslotportrait_{hostKey}`)의 앵커는 `03_anchors/hosts_x3/` 에 12종이 준비되어 있다.
- 목업이 JPEG라 앵커에 압축 노이즈가 있다. **노이즈를 따라 그리지 말고 형태만 참조**한다.

---

## §C. 앵커 이미지 목록

| 앵커 파일 | 용도 | 대상 |
|-----------|------|------|
| `Reference/Mockups/lobby_hub.jpeg` | **주 앵커.** 전체 톤·팔레트·프레임 스타일 기준 | S1·S2 전 에셋 + 공유 HUD |
| `Reference/Mockups/host_select.jpeg` | 던전 톤·셀 프레임·CTA 버튼 기준 | S3 전 에셋 |
| `03_anchors/hosts_x3/{hostKey}_x3.png` (12) | **앵커.** 원작 캐릭터 초상 (273×252) | `hostslotportrait_*` · `hostportraitimage_*` |
| `Reference/Hosts/x3/{hostKey}_x3.png` (12) | **육안 확인용 확대본.** Unity 임포트 대상 아님 | 위 크롭의 형태 검토 |
| `Reference/Slides/slide_07.png` | **얼티밋 정본** [확정 #12] | `ultimateicon_{hostKey}` ×12 |

⚠️ `Reference/Slides/slide_01.png`(제안서 표지)는 **앵커로 쓰지 않는다.** 모던 산세리프 로고 + 네온 퍼플·마젠타는 프레젠테이션 덱 디자인이며 게임 UI 색이 아니다 [확정 §5].

---

## §D. 생성 순서 (일관성 확보)

`Prompt_Registry_Template.md` §0 규칙 2·3 준수 — **최종본은 text2img로 새로 생성하고, 톤 정렬은 완성본끼리만 한다.**
초안·목업 크롭을 img2img 레퍼런스로 넣지 않는다 (입력 해상도에 퀄이 앵커링된다).

| 단계 | 내용 | 산출 | 승인 |
|:---:|------|------|:---:|
| **0** | **앵커 준비 완료** — §B-2 좌표로 잘라낸 앵커가 `03_anchors/ui_crops/` 에 있다 | 로고·고스트·보스 + 호스트 12종 | — |
| **1** | **히어로 5장 시안** — `lobbybackground` · `continuebutton` · `goldicon` · `hostslotframe` · `hostdetailframe` | 각 카테고리 톤 기준 | **✅ 사용자 승인 게이트** |
| **2** | 배경 2종 — `backgroundimage` · `hostselectbackground` (`lobbybackground` 톤 참조) | | |
| **3** | 프레임·패널 — `tophudbackground` `ghostwidget` `chaptercard` `ultimatecard` `tipbar` `hostlisttitle` `featuretabbarbackground` `battlepasscard` `eventcard` `dailylogincard` 카운터 3종 셀 변형 2종 (`hostslotframe`·`hostdetailframe` 톤 참조) | | |
| **4** | 버튼 — `possessstartbutton` `hostupgradebutton` `hostbutton` `chapterbutton` `shopbutton` (`continuebutton` 톤 참조) | | |
| **5** | 게이지 바 5쌍 — `*barbg` / `*barfill` (프레임 톤 참조) | | |
| **6** | 아이콘 — 재화 3 · `plusbutton` · `notifybadge` · `mailbutton` · `settingsbutton` · 탭 5 · `friendstablock` · 스탯 4 · 잠금 2 · 기타 6 (`goldicon`·`staticon_hp` 톤 참조) | | |
| **7** | `GPT+ANCHOR` 잔여분 — 앵커를 실루엣 기준으로 삼아 목표 규격으로 리드로우 | | |
| **8** | `ultimateicon_{hostKey}` ×12 — `slide_07` 앵커. **`amazoness`(`BLADE STORM`) 1장 먼저 확정 후 나머지 11장의 톤 레퍼런스로 투입** | | |
| **9** | 자체 검수 → ZIP 납품 | `manifest.csv` + PNG 115 | **✅ 우리 검수 게이트** |

> 0단계를 먼저 두는 이유: **앵커의 실제 색이 생성물의 팔레트 기준선**이 된다. 로고와 호스트 초상의 색을 먼저 확보해야 히어로 시안의 팔레트를 거기 맞출 수 있다.

---

## §E. 우리 측 후처리 (ZIP 수령 이후)

```
ZIP 수령 → §B 표로 규격·알파 검수 → Assets/BaseResource/{프리팹명}/ import
        → 임포터 설정 → 아틀라스 팩 → Addressable 등록 → 프리팹 Image 슬롯 배선
```

**Unity 임포터 설정 (전 에셋 공통)**

| 항목 | 값 | 근거 |
|------|-----|------|
| Texture Type | `Sprite (2D and UI)` | — |
| Filter Mode | **`Point (no filter)`** | 확정 #16 |
| Compression | **`None`** | 픽셀아트 압축 아티팩트 방지 |
| Generate Mip Maps | **끔** | UI |
| Sprite Mode / Border | `Single` · §B `slice9` 값 입력 | 9-slice 31행 |
| Pivot | §B `pivot` 값 | 게이지 fill은 `left` 필수 |

**아틀라스·주소** — `atlas/titlemainui` · `atlas/lobbymainui` · `atlas/hostselectpanel` [`constants.md` §4]
**잠금 실루엣** — 에셋을 추가하지 않고 **머티리얼/셰이더 단색화**로 처리한다 `[근거: 에셋이 12장 늘지 않고, 원본과 픽셀 정렬이 자동 일치하며, 정수 배율이 유지된다]`. 아트팀 판단이 상위다.

---

## §F. 파일·변형 네이밍 규약

- **파일명 = 화면 설계서의 요소 이름**(소문자). 예: 요소 `ContinueButton` → `continuebutton.png`
- 한 에셋이 여러 요소에서 재사용되면 **요소 이름 자체를 동일하게** 짓고 부모를 달리해 형제 유일성을 만족시킨다.
  → `NotifyBadge`(6곳) · `PlusButton`(3곳) · `ArrowIcon`(2곳). **에셋은 1종으로 수렴한다.**

### F-1. 변형(variant) 표기법

| 형식 | 의미 | 허용 토큰 |
|------|------|-----------|
| `{요소명소문자}` | **기본형.** 상태·인스턴스 구분이 없는 단일 에셋 | — |
| `{요소명소문자}_{상태}` | **상태 변형.** 런타임 상태에 따라 스프라이트 교체 | `selected` · `locked` · `disabled` |
| `{요소명소문자}_{hostKey}` | **인스턴스 변형.** 데이터(호스트) 종속으로 N개 존재 | `constants.md` §4 호스트 키 12종 |

- **상태 토큰은 위 3종으로만 제한한다.** 새 토큰이 필요하면 이 표를 먼저 갱신한다 (임의 접미사 금지).
- **기본형에는 접미사를 붙이지 않는다.** `hostslotframe_normal` ✗ → `hostslotframe` ✓
- 두 형식을 **겹쳐 쓰지 않는다.** `hostslotportrait_amazoness_locked` ✗ — 잠금 실루엣은 §E대로 **머티리얼 단색화**로 처리하므로 에셋이 늘지 않는다.
- `_x3` 등 배율 접미사는 **작업 폴더(`Reference/Hosts/x3/`) 전용 표기**이며 **Unity 임포트 에셋에는 쓰지 않는다.**

### F-2. 변형 규약을 사용하는 에셋 — 전수 목록

| 형식 | 에셋 | 파일 수 | 근거 |
|------|------|:---:|------|
| `_{상태}` | `hostslotframe_selected` | 1 | 선택 시 골드 프레임 점등 |
| `_{상태}` | `hostslotframe_locked` | 1 | 잠금 시 무채색 프레임 |
| `_{hostKey}` | `hostslotportrait_{hostKey}` | 12 | 그리드 셀 초상 (`source=GPT+ANCHOR`) |
| `_{hostKey}` | `hostportraitimage_{hostKey}` | 12 | 상세 카드 대형 초상 (동일 크롭 ×2) |
| `_{hostKey}` | `ultimateicon_{hostKey}` | 12 | 얼티밋 아이콘 12종 |

> **위 5계열 외에 변형 접미사를 쓰는 에셋은 없다.** `hostslotframe`은 기본형 1 + 상태 변형 2 = **3파일**이며, `_selected`/`_locked`가 없으면 기본형을 쓴다.

### F-3. 우선순위

| 등급 | 의미 | 대상 |
|------|------|------|
| **P0** | 1차 범위 3화면이 **성립하기 위해 필수** | 배경 3 · 전 프레임/버튼 · HUD 전체 · `amazoness` 3파일 · 스탯 아이콘 4 |
| **P1** | 화면에 **보이지만 1차에서 동작하지 않는(⛔)** 요소 + **잠금 11종** | 프로모 카드 3 · 탭 아이콘 6 · `shopbutton*` · 호스트 11종 ×3계열 |
| **P2** | 다음 마일스톤 | 코스튬(색상 변형 3종) · 인게임 전용 |

---

## §G. 이월 · 미정

| # | 항목 | 상태 |
|---|------|------|
| 1 | `bossportrait` CH1 보스 | `[TBD — CH1 챕터명·보스명 미기획]` `MAD DOCTOR`(CH3) 크롭을 placeholder로 사용 |
| 2 | `ultimateicon_{hostKey}` 12종 디자인 | 정본 = `slide_07` [확정 #12]. 수치 스펙 미착수는 아이콘 제작에 영향 없음 |
| 3 | 원작 스프라이트 IP 참조 범위 | `[사용자 확인 중 — 확정 #25]`. 확정 전까지 목업 기준 |
| 4 | 인게임 전용 에셋 (룸·적·보스 HP바·조작 UI) | **1차 범위 밖** |
| 5 | 코스튬 3종 (`rambo_laser`·`wizard_green`·`ninja_red`) | **1차 범위 밖** [확정 #10]. 원본은 `Reference/Hosts/`에 확보됨 |
| 6 | 프롬프트 영속 기록 | 생성 착수 시 `AVSR_PromptRegistry.md`를 템플릿에서 복사해 §D 단계별 subject·경로를 기록 |

---

## 개정 이력
| 버전 | 날짜 | 변경 |
|------|------|------|
| 0.1 | 2026-08-07 | 최초 작성 — 3화면 Image 요소 집계, 재사용 3종 수렴, `Reference/Hosts/` 사용 불가 판정 |
| 0.2 | 2026-08-07 | 변형 네이밍 규약 명문화 · ChatGPT 배치 재편 · 후처리 파이프라인 · 2-티어 원칙 · 호스트 초상 재추출 반영 |
| 0.4 | 2026-08-07 | **전량 생성으로 방침 변경(사용자 지시)** — 목업 크롭 사용 폐기. `CROP`·`CROP+GPT` → `GPT+ANCHOR` 통합(21행/43파일). 크롭 좌표는 **앵커 이미지 추출**에 사용하고 `03_anchors/ui_crops/` 에 원본+×3 배포 |
| 0.3 | 2026-08-07 | **납품 명세서로 재구성** — 시트 그리드·입력 폴더 규약 폐기(ChatGPT가 분할·리네임·ZIP까지 수행). §A 지시문 / §B 82행 명세표(`source` 열 신설) / §B-2 CROP 좌표 **육안 검증** / §C 앵커 / §D 생성 순서 / §E 우리 측 후처리로 재편. CROP 실측치가 최종 규격을 결정하도록 설계서 6개 요소 크기 갱신 |
