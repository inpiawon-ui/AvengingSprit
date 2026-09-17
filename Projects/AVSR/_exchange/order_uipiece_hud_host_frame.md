# 발주 — 인게임 UI 조각: 호스트(빙의한 몸) **초상 틀 (112 × 120)

## 왜

인게임 UI 퀄업 시안이 **통과**했다: `Projects/AVSR/_exchange/in/ui_mockup_hud.png`
이제 그 시안을 게임에 넣을 수 있게 **조각으로** 받는다. 이 조각은 그중 하나다.

**통과한 시안과 똑같은 생김새**여야 한다 — 시안을 가장 먼저 열어 해당 부분을 보고 그대로 맞춰 달라.
색 · 재질 · 장식은 시안이 정답이다(새로 디자인하지 마라).

## 먼저 열어 볼 것

| 파일 | 무엇 |
|---|---|
| `Projects/AVSR/_exchange/in/ui_mockup_hud.png` | **통과한 시안** — 이 조각의 정답 |
| `Projects/AVSR/_exchange/ref/ui_now_hud.png` | 지금 게임 화면 — 이 조각이 어디에 쓰이는지 |
| `Assets/BaseResource/InGameMainUI/hud_host_frame.png` | 지금 이 자리의 그림(교체 대상) — 크기 · 쓰임만 참고, 생김새는 따르지 마라 |

## 이 조각

- 무엇: 호스트(빙의한 몸) **초상 틀** — 빙의 중일 때 HUD 에 뜬다(시안엔 없다, HUD 한 벌로). 가운데 초상은 게임이 넣는다
- 게임: AVENGING SPIRIT: RE:BORN — 세로 모바일, 기준 화면 720 × 1280. 시안이 이 크기라 **시안 속 크기가 곧 게임 속 크기**다

## 규격 (꼭 지킬 것)

| 항목 | 값 |
|---|---|
| 파일 | `uipiece_hud_host_frame.png` |
| 크기 | **112 × 120 px 정확히** |
| 형식 | PNG **투명 배경**(알파). 조각 바깥은 완전히 투명. 뒤에 전투 화면을 그려 넣지 마라 |
| 글자 | **글자 · 숫자 금지** — 글자는 게임이 쓴다 |

## 금지

- `ImageDraw` 같은 도형 스크립트로 그리지 마라 — 그림 생성 · 편집으로 만든다
- API 키를 쓰는 CLI 폴백은 쓰지 마라.

## 내보내는 곳

`Projects/AVSR/_exchange/in/uipiece_hud_host_frame.png`
잠겨 있으면 `Projects/AVSR/_exchange/in/_generated/` 에. **이 한 장만.** 작업 스크립트 · 임시 파일은 프로젝트 밖에서 쓰고 지운다.
