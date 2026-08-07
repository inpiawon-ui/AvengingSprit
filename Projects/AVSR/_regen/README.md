# AVSR UI 리소스 재제작 — 앵커 기반

> 2026-08-07 · 이전 납품 115개 중 **37개가 코드로 그린 도형**으로 확인되어 재제작합니다.

---

## 1. 무엇이 잘못됐는지 (측정 결과)

이전 납품을 픽셀 단위로 분석한 결과입니다.

| 구분 | 좌우대칭 | 고유 색 수 | 판정 |
|---|---|---|---|
| 1차 스타일 게이트 6개 | 29~79% | 9~16색 | ✅ 정상 |
| 호스트 초상 24종 | 10~17% | 16색 | ✅ 정상 |
| **문제 자산 37개** | **98~100%** | **2~3색** | ❌ 코드 도형 |

**손으로 그리거나 이미지 생성으로 만든 그림은 좌우가 픽셀 단위로 100% 대칭일 수 없습니다.**
`battlepassbarbg` 색 2개, `achievementtabicon` 색 2개 — 도형 그리기 함수로 찍은 결과입니다.
그래서 서로 다른 자산에 **똑같은 꺾쇠(∧)** 가 반복됐습니다.

### 성공/실패를 가른 것은 하나입니다

```
앵커 이미지 첨부 O  →  성공 (호스트 초상 24종, 스타일 게이트 6개)
앵커 없이 텍스트만  →  코드 도형
```

**그래서 이번엔 모든 자산에 영역 앵커를 첨부했습니다.**

---

## 2. 폴더 구성

```
01_anchors/       목업에서 잘라낸 자산별 참조 영역 (42종)
                  {이름}.png     원본 해상도
                  {이름}_x3.png  ×3 확대 ← 프롬프트에는 이걸 첨부
02_restored/      이미 완성됨 — 만들지 마세요 (6종)
03_mockup/        원본 목업 (전체 화면 참조용)
```

---

## 3. 절대 규칙

1. **자산 1개당 앵커 1장을 반드시 첨부**하고 생성합니다. 앵커 없이 만들지 마세요.
2. **도형 그리기 함수(rectangle/polygon/ellipse 등)로 만들지 마세요.** 전부 이미지 생성으로.
3. **일괄 처리하지 마세요.** 한 번에 하나씩, 앵커를 보며 만듭니다.
4. **16색 축소는 마지막에.** 그림이 완성된 뒤 적용합니다. 먼저 줄이면 형태와 질감이 죽습니다.
5. **글자를 넣지 마세요.** 앵커에 `HOST` `CHAPTER` `SHOP` `BATTLE PASS` 등이 보이지만
   **모든 문자는 게임 엔진이 렌더**합니다. 그림만 그리세요.
   (유일한 예외: `logolockup` — 계약 IP 로고)
6. **알파는 0 또는 255만.** 반투명 픽셀이 생기면 안티앨리어싱으로 판정됩니다.

---

## 4. 합격 기준 (기계 검사)

납품물은 아래를 자동 검사합니다. 하나라도 걸리면 반려됩니다.

```
좌우대칭 ≥ 98%  AND  고유색 ≤ 6      → 코드 도형으로 판정. 반려
고유색 > 16                          → 색 수 초과. 반려
반투명 픽셀 존재                      → 안티앨리어싱. 반려
규격 불일치                          → 반려
녹색 계열(hue 60~180°) 존재           → 팔레트 이탈. 반려
```

**목표치**: 좌우대칭 80% 미만, 고유색 10~16개. 참고로 정상 판정된 호스트 초상은 대칭 10~17% / 16색입니다.

---

## 5. 아트 방향

```
스타일    1991 아케이드 HD 픽셀아트. Jaleco 계열 어둡고 음습한 오컬트 액션
팔레트    배경  #050812 #010711 #060512
          골드  #D89000 #E49000 #FCB418 #CC8400   ← 1순위 (CTA·재화·프레임)
          블루  #003C9C #00489C #489CFC           ← 2순위
          적색  #9C180C #C00C00                   ← 경고·HP 한정
금지      녹색 계열 · 네온 퍼플 · 마젠타 · 광택(glossy) · 소프트 그림자
질감      앵커의 마모·재질·광원·불규칙한 픽셀 밀도를 반드시 살릴 것.
          단색 채움 + 테두리 1줄은 반려 사유입니다.
```

---

## 6. 자산별 지시

### 6-1. 9-slice 프레임 — 내부를 비울 것

앵커에는 내용물(글자·아이콘·바)이 들어 있습니다. **테두리와 재질만 살리고 내부는 비우세요.**
늘려도 안 깨지도록 테두리 두께를 일정하게 유지합니다.

| 파일 | 규격 | 9-slice(L,R,T,B) | 앵커 |
|---|---|---|---|
| `tophudbackground.png` | 720×128 | 16,16,16,20 | *(전체 목업 상단)* |
| `chaptercard.png` | 222×344 | 24,24,24,24 | `chaptercard_x3.png` |
| `battlepasscard.png` | 238×112 | 16,16,16,16 | `battlepasscard_x3.png` |
| `eventcard.png` | 238×104 | 16,16,16,16 | `eventcard_x3.png` |
| `dailylogincard.png` | 238×112 | 16,16,16,16 | `dailylogincard_x3.png` |
| `featuretabbarbackground.png` | 616×108 | 16,16,12,12 | `featuretabbarbackground_x3.png` |
| `hostbutton.png` | 222×260 | 20,20,20,20 | `hostbutton_x3.png` (자주색 테두리만) |
| `chapterbutton.png` | 236×260 | 20,20,20,20 | `chapterbutton_x3.png` (골드 테두리만) |
| `shopbutton.png` | 222×260 | 20,20,20,20 | `shopbutton_x3.png` (블루 테두리만) |
| `ghostwidget.png` | 188×96 | 10,10,10,10 | `ghostwidget_x3.png` |
| `staminacounter.png` | 126×60 | 20,20,12,12 | `staminacounter_x3.png` |
| `goldcounter.png` | 132×60 | 20,20,12,12 | `goldcounter_x3.png` |
| `gemcounter.png` | 112×60 | 20,20,12,12 | `gemcounter_x3.png` |
| `continuebutton.png` | 198×74 | 20,20,16,16 | `continuebutton_x3.png` |
| `progressbarbg.png` | 154×20 | 4,4,4,4 | `progressbarbg_x3.png` |

> 게이지 `~fill` 은 `~bg` 와 같은 형태에 채움색만 다릅니다.
> `progressbarfill` 골드 / `ghostexpbarfill` 청색 `#489CFC` / `battlepassbarfill` 골드 / `statbarfill` 골드

### 6-2. 아이콘 — 앵커 그대로, 투명 배경

| 파일 | 규격 | 앵커 | 내용 |
|---|---|---|---|
| `staminaicon.png` | 32×32 | `staminaicon_x3.png` | 번개 |
| `goldicon.png` | 32×32 | `goldicon_x3.png` | 금화 |
| `gemicon.png` | 32×32 | `gemicon_x3.png` | 보라 젬 |
| `missiontabicon.png` | 48×48 | `missiontabicon_x3.png` | 클립보드 + 별 |
| `achievementtabicon.png` | 48×48 | `achievementtabicon_x3.png` | 트로피 |
| `rankingtabicon.png` | 48×48 | `rankingtabicon_x3.png` | 시상대 1·2·3 |
| `inventorytabicon.png` | 48×48 | `inventorytabicon_x3.png` | 배낭 |
| `friendstabicon.png` | 48×48 | `friendstabicon_x3.png` | 인물 실루엣 2인 |
| `friendstablock.png` | 28×28 | `friendstablock_x3.png` | 자물쇠 |
| `mailbutton.png` | 48×56 | `mailbutton_x3.png` | 봉투 |
| `settingsbutton.png` | 48×56 | `settingsbutton_x3.png` | 톱니 |
| `plusbutton.png` | 28×28 | `plusbutton_x3.png` | 골드 원형 + |
| `notifybadge.png` | 24×24 | `notifybadge_x3.png` | 적색 원 + ! |
| `battlepassbadge.png` | 40×40 | `battlepassbadge_x3.png` | **보라 방패** (숫자는 엔진이 렌더) |
| `dailylogincheck.png` | 26×26 | `dailylogincheck_x3.png` | 녹색 체크 원 |
| `progressrewardchest.png` | 42×44 | `progressrewardchest_x3.png` | 보물상자 |
| `ghostportraiticon.png` | 60×60 | `ghostportraiticon_x3.png` | 고스트 얼굴 |
| `bossportrait.png` | 84×84 | `bossportrait_x3.png` | 인물 흉상 |
| `ghostavatar.png` | 176×220 | `ghostavatar_x3.png` | 대형 고스트 |
| `portalring.png` | 200×56 | `portalring_x3.png` | 청색 포탈 링 |
| `logolockup.png` | 194×102 | `logolockup_x3.png` | **IP 로고 — 글자 포함 유일 예외** |

### 6-3. 배경 — 가장 중요합니다

이전 납품에서 내용 밀도가 **1.2%** 였습니다. 사실상 빈 화면이었습니다.

`lobbybackground.png` (720×1280) — 목업 전체(`03_mockup/lobby_hub.jpeg`)를 앵커로 쓰고
**아래 요소가 반드시 보여야 합니다**:

```
· 달 (화면 중상단)
· JALECO 네온 간판 (우상단, 분홍 발광)
· FACTORY AREA 3 표지판 (좌측, 주황 네온 + 큰 숫자 3)
· 공장 굴뚝·크레인·건물 실루엣 (좌우 양쪽)
· 창문 불빛 다수 (주황)
· 중앙 하단 청색 포탈 빔 (수직 광선)
· 연기/구름
```

`backgroundimage.png` (720×1280) — 타이틀 배경. 로비보다 어둡되 도시 실루엣은 보이게.

---

## 7. 이미 완성된 것 — 만들지 마세요

`02_restored/` 의 6개는 목업에서 직접 복원했습니다. 재생성하면 오히려 품질이 떨어집니다.

```
hostbuttonart.png      202×150
chapterbuttonart.png   216×150
shopbuttonart.png      202×150
battlepassart.png       90×80
dailyloginart.png       70×64
eventart.png            70×64
```

또한 **호스트 초상 24종**(`hostslotportrait_*` · `hostportraitimage_*`)과
**얼티밋 아이콘 12종**은 이전 납품분이 정상이므로 재생성 대상이 아닙니다.

---

## 8. 납품

개별 PNG + ZIP 1개. 파일명은 위 표의 이름 그대로.
