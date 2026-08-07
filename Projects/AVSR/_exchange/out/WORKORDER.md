# 작업 지시 — 2026-08-07

> 이 파일이 현재 작업의 단일 지시서다. 갱신되면 상단 날짜가 바뀐다.
> 납품은 `_exchange/in/` 에 개별 PNG로. 파일명은 아래 표 그대로.

---

## ✅ 통과한 것 (재작업 금지)

| 항목 | 검수 결과 |
|---|---|
| `lobbybackground` `backgroundimage` | 밀도 1.2%→21.4% · 대칭 28% · 녹색 0 · 요구 요소 8종 전부 확인 |
| `chaptercard` | 내부 밀도 0.0%(속 완전 비움) · 테두리 11px(명세 24 이내) · 대칭 25% · 15색 · 340×260 확장 무결 |
| `02_restored/` 6종 | 목업에서 직접 복원. 재생성하면 품질이 떨어진다 |
| 호스트 초상 24종 · 얼티밋 아이콘 12종 | 기존 납품분 정상 |

**`chaptercard` 가 프레임 작업의 기준이다.** 나머지 프레임도 정확히 같은 방식으로.

---

## 🔨 이번 작업 — 39개

### A. 9-slice 프레임 14개

`chaptercard` 와 동일하게:
- 앵커의 **내용물(글자·아이콘·바·초상)은 전부 제거**, 속은 비운다
- **테두리 형태·두께·색·재질·마모는 살린다**
- **테두리 실측 두께가 아래 9-slice 값을 넘지 않게** (넘으면 확장 시 잘린다)

| filename | 규격 | 9-slice (L,R,T,B) | 앵커 |
|---|---|---|---|
| `tophudbackground.png` | 720×128 | 16,16,16,20 | `03_mockup/lobby_hub.jpeg` 상단 |
| `battlepasscard.png` | 238×112 | 16,16,16,16 | `battlepasscard_x3.png` |
| `eventcard.png` | 238×104 | 16,16,16,16 | `eventcard_x3.png` |
| `dailylogincard.png` | 238×112 | 16,16,16,16 | `dailylogincard_x3.png` |
| `featuretabbarbackground.png` | 616×108 | 16,16,12,12 | `featuretabbarbackground_x3.png` |
| `hostbutton.png` | 222×260 | 20,20,20,20 | `hostbutton_x3.png` — **자주색 테두리만** |
| `chapterbutton.png` | 236×260 | 20,20,20,20 | `chapterbutton_x3.png` — **골드 테두리만** |
| `shopbutton.png` | 222×260 | 20,20,20,20 | `shopbutton_x3.png` — **블루 테두리만** |
| `ghostwidget.png` | 188×96 | 10,10,10,10 | `ghostwidget_x3.png` |
| `staminacounter.png` | 126×60 | 20,20,12,12 | `staminacounter_x3.png` |
| `goldcounter.png` | 132×60 | 20,20,12,12 | `goldcounter_x3.png` |
| `gemcounter.png` | 112×60 | 20,20,12,12 | `gemcounter_x3.png` |
| `continuebutton.png` | 198×74 | 20,20,16,16 | `continuebutton_x3.png` |
| `progressbarbg.png` | 154×20 | 4,4,4,4 | `progressbarbg_x3.png` |

> 하단 3버튼(`hostbutton`·`chapterbutton`·`shopbutton`)은 **프레임만**이다.
> 안의 그림(캐릭터 3인·성채·보물상자)은 `02_restored/` 에 이미 완성돼 있다.

### B. 게이지 fill 4개

`~bg` 와 같은 형태에 **채움색만 다르다.** pivot 은 `left`(채워지는 방향).

| filename | 규격 | 9-slice | 색 |
|---|---|---|---|
| `progressbarfill.png` | 154×20 | 4,4,4,4 | 골드 `#D89000` |
| `ghostexpbarfill.png` | 106×14 | 4,4,4,4 | **청색 `#489CFC`** (EXP — 적색 HP 아님) |
| `battlepassbarfill.png` | 120×14 | 4,4,4,4 | 골드 |
| `statbarfill.png` | 124×16 | 4,4,4,4 | 골드 |

`ghostexpbarbg.png`(106×14) · `battlepassbarbg.png`(120×14) · `statbarbg.png`(124×16) 도 함께. 전부 9-slice `4,4,4,4`, 빈 트랙(어두운 슬롯).

### C. 아이콘 21개 — 투명 배경

| filename | 규격 | 앵커 | 내용 |
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
| `battlepassbadge.png` | 40×40 | `battlepassbadge_x3.png` | **보라 방패** (숫자는 엔진 렌더) |
| `dailylogincheck.png` | 26×26 | `dailylogincheck_x3.png` | 녹색 체크 원 |
| `progressrewardchest.png` | 42×44 | `progressrewardchest_x3.png` | 보물상자 |
| `ghostportraiticon.png` | 60×60 | `ghostportraiticon_x3.png` | 고스트 얼굴 |
| `bossportrait.png` | 84×84 | `bossportrait_x3.png` | 인물 흉상 |
| `ghostavatar.png` | 176×220 | `ghostavatar_x3.png` | 대형 고스트 |
| `portalring.png` | 200×56 | `portalring_x3.png` | 청색 포탈 링 |
| `logolockup.png` | 194×102 | `logolockup_x3.png` | **IP 로고 — 글자 포함 유일 예외** |

---

## 📏 합격 기준 (자동 검사)

```
좌우대칭 ≥98% AND 고유색 ≤6   → 코드 도형 판정. 반려
고유색 > 16                    → 반려
반투명 픽셀 존재                → 안티앨리어싱. 반려
규격 불일치                     → 반려
녹색 계열(hue 60~180°)          → 팔레트 이탈. 반려
프레임 테두리 두께 > 9slice 값   → 확장 시 잘림. 반려
```

**목표치**: 대칭 80% 미만 · 색 10~16개. `chaptercard` 통과본이 대칭 25% / 15색이었다.

---

## ⚠️ 반복 확인 사항

1. **`ImageDraw` 등 도형 그리기 함수로 만들지 말 것.** 전부 이미지 생성으로.
   이전 납품 실패 원인이 `build_avsr_ui_full_v2.py` 의 `draw.polygon/ellipse/line` 이었다.
2. **일괄 처리 금지.** 자산 1개당 앵커 1장 붙여서 하나씩.
3. **16색 축소는 마지막에.** 먼저 줄이면 형태와 질감이 죽는다.
4. **글자 금지.** 앵커에 `HOST` `CHAPTER` `BATTLE PASS` 등이 보이지만 전부 엔진이 렌더한다.
   유일한 예외는 `logolockup`.
5. **알파는 0 또는 255만.**

---

## 📦 납품

`_exchange/in/` 에 개별 PNG. 하위 폴더 자유. 파일명은 위 표 그대로.
납품 후 이 폴더의 `_verify_result.txt` 에 검수 결과가 기록된다.
