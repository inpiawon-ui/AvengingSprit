# 발주 — 인게임 UI 퀄업 시안: 악마 이벤트 「봉인된 궤짝」 (완성 화면 720 × 1280 한 장)

## 왜

인게임 배경을 새로 받아 **통과**했다(CH1 쓰레기 집적장). 이제 UI 차례다. 사장님 요청 그대로:

> UI 도 좀 저런 느낌으로 퀄업을 하면 좋겠어. 지금 UI 가 **예전 지저분한 느낌**으로 되어 있어서,
> 좀 **깔끔하게 어벤징 스피릿 퀄업 버전**으로 부탁해.

- **레이아웃(무엇이 어디에 있는지)은 지금 그대로 유지한다.** 바뀌는 건 생김새다
- 새 배경을 통과시킨 기준도 같다 — 「요즘 느낌」, 「원작(1991 Jaleco AVENGING SPIRIT) 느낌」,
  그리고 **배경은 죽고 그 위의 것이 산다.** UI 도 전투 화면에서 지저분하게 튀면 안 된다

**색 · 재질 · 테두리 · 장식 · 글자체 느낌 — 디자인은 전부 그쪽이 정한다.**

이번에 필요한 것은 **조각이 아니라 「다 적용된 완성 화면」 한 장**이다.
사장님이 이걸 보고 통과시키면 그때 조각으로 나눠 다시 발주한다.

## 먼저 열어 볼 것

| 파일 | 무엇 |
|---|---|
| `Projects/AVSR/_exchange/ref/ui_now_demon.png` | **이 화면의 지금 모습** (720 × 1280) — 레이아웃의 정본. 생김새는 따르지 마라 |
| `Projects/AVSR/_exchange/ref/ingame_floor_v3_approved_screen.png` | 통과한 새 배경을 넣은 전투 화면 — **UI 뒤에 깔리는 배경**이 이것이다 |
| `Projects/AVSR/_exchange/in/roomfloor_env_junkyard_full_v3.png` | 통과한 새 배경 원본 |
| `Projects/AVSR/_exchange/ref/ingame_style_example_1.png` · `ingame_style_example_2.png` | 사장님이 준 느낌 예시 |
| `Projects/AVSR/_exchange/out/33_concept/ORIGINAL_stage_cutscenes.png` | 1991 원작 |

## 이 화면 — 악마 이벤트 「봉인된 궤짝」

- 무엇이 있나: 대가를 치르고 보상을 받는 선택 창. 제목 판, 본문 두세 줄, 보상 한 줄, 대가 한 줄(붉은 알약 모양), 「대가를 치른다」 큰 버튼과 「내버려 둔다」 작은 버튼. 악마 · 저주의 기운
- 중요한 것: 위험한 거래라는 느낌 — 하지만 글이 잘 읽혀야 한다
- 게임: **AVENGING SPIRIT: RE:BORN** — 주인공은 **유령**, 적의 몸에 **빙의**해 싸우는 방 단위 로그라이크. 세로 모바일

## 규칙

| 항목 | 값 |
|---|---|
| 파일 | `ui_mockup_demon.png` |
| 크기 | **720 × 1280 px** 완성 화면 한 장 |
| 뒤 배경 | 통과한 새 전투 화면(쓰레기 집적장)이 깔린 상태로 그린다 |
| 레이아웃 | `ui_now_demon.png` 와 **같은 자리 · 같은 구성** |
| 글자 | `ui_now_demon.png` 에 적힌 글자를 **그대로** 옮겨 적는다(일본어). 임의로 바꾸지 않는다 |

## 금지

- 지금 UI 그림을 보정 · 복사해서 만들지 마라 — 새로 그린다
- `ImageDraw` 같은 도형 스크립트로 그리지 마라 — 그림 생성으로 그린다
- API 키를 쓰는 CLI 폴백은 쓰지 마라.

## 내보내는 곳

`Projects/AVSR/_exchange/in/ui_mockup_demon.png`
잠겨 있으면 `Projects/AVSR/_exchange/in/_generated/` 에. **이 한 장만.** 작업 스크립트 · 임시 파일은 프로젝트 밖에서 쓰고 지운다.
