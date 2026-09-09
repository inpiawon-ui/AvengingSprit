# 발주 — 레벨업 화면 조각 11장 (B안 「돌 서판」 확정)

## 배경

레벨업 3택1 화면의 시안 3안 중 **B안 「돌 서판」이 컨펌**됐다.
이제 그 화면을 게임에 붙일 수 있게 **조각으로 나눠** 그린다.

지금 이 화면은 카드 액자가 **1픽셀 색 테두리에 검은 속**이 전부라
(`Assets/BaseResource/Card/cardpanel_common.png`) 혼자 자리표시로 남아 있다.

## 반드시 열어 볼 파일

| 파일 | 무엇 |
|---|---|
| `Projects/AVSR/_exchange/in/levelup_mockup_b.png` | **컨펌본.** 720×1280 완성 화면. 이걸 그대로 나눈다 |
| `Assets/BaseResource/InGameMainUI/shrineframe.png` | 천사 액자 — 같은 화풍의 형제 |
| `Assets/BaseResource/InGameMainUI/eventframe.png` | 악마 액자 — 같은 화풍의 형제 |
| `Assets/BaseResource/Card/card_c004.png` | 카드 아이콘 128×128 (「처형」). 액자 안에 이 크기로 들어간다 |
| `Assets/BaseResource/Card/cardpanel_common.png` | 지금 쓰는 카드 액자 (교체 대상) |

## 공통 규칙 (11장 전부)

1. **글자를 그리지 않는다.** 등급 이름·카드 이름·설명은 전부 게임이 얹는다.
   시안의 「RARE」「수호 방패」 같은 글자는 **빼고** 그린다
2. **배경은 마젠타 `#FF00FF` 단색.** 투명 PNG 로 주지 않는다. 알파는 이쪽에서 뚫는다
3. PNG, 지정한 크기 **정확히**. 여백을 더 붙이지 않는다
4. 픽셀아트. 안티에일리어싱으로 뭉개지 않는다
5. 시안의 **바깥 배경(던전 바닥)은 그리지 않는다.** 조각만 오려 낸다

---

## A. 카드 액자 4장 — 등급만 다르다

시안에서 카드 한 장은 **x 22~232 · y 395~890** 자리다(보석 포함).
이것을 **216 × 500** 으로 옮긴다.

```
크기: 216 × 500
파일: cardpanel_common.png / cardpanel_rare.png
      cardpanel_epic.png   / cardpanel_legendary.png
```

세로 구성 (216×500 안에서):

| 구간 | y | 내용 |
|---|---|---|
| 보석 | 0 ~ 55 | 카드 위쪽 가운데에 박힌 마름모 보석. **등급을 가르는 것이 이것이다** |
| 서판 머리 | 30 ~ 105 | 깎은 돌 테두리. 보석이 여기에 물려 있다 |
| 안쪽 면 | 105 ~ 465 | 어두운 청록 돌면. 좌우 테두리를 따라 등급색 발광이 흐른다 |
| 서판 발 | 465 ~ 500 | 아래 돌 테두리 |

**비워 둘 자리 (게임이 여기에 얹는다)**

| 무엇 | x | y |
|---|---|---|
| 등급 이름판 (5번이 얹힌다) | 15 ~ 201 | 60 ~ 100 |
| 아이콘 (7번 테두리 + 그림) | 52 ~ 164 | 134 ~ 246 |
| 카드 이름 | 10 ~ 206 | 300 ~ 344 |
| 설명 두 줄 | 14 ~ 202 | 360 ~ 446 |

> 이름과 설명 사이의 **가는 구분선**(시안 y 750 자리)은 액자 그림에 넣어 둔다.
> 글자는 그 위아래에 얹힌다.

**등급별로 다른 것은 딱 둘이다** — 보석 색과 테두리 발광색. 돌의 모양·질감은 넷이 똑같아야 한다.

| 파일 | 보석·발광 |
|---|---|
| `cardpanel_common` | 무채색 은빛 `#B4BECD` |
| `cardpanel_rare` | 파랑 `#5AA9E6` |
| `cardpanel_epic` | 보라 `#B07DE0` |
| `cardpanel_legendary` | 금빛 `#F0B428` |

---

## B. 등급 이름판 2장

카드 위쪽에 얹히는 **가로로 긴 돌 명판**. 시안의 「RARE」가 놓인 그 판이다.

```
크기: 186 × 40
파일: cardchip_rarity.png   등급을 적는 판 (RARE · EPIC …)
      cardchip_level.png    레벨을 적는 판 (Lv.1 → Lv.2)
```

- 모양은 **둘이 똑같다.** 양끝이 살짝 깎인 돌 명판에 가는 금속 테두리
- `cardchip_rarity` 는 어두운 돌, `cardchip_level` 은 **조금 밝은 돌** —
  이미 가진 카드라는 것이 색으로 먼저 읽혀야 한다
- 가운데 x 20 ~ 166 은 글자가 얹히므로 무늬를 넣지 않는다

---

## C. 아이콘 테두리 4장

아이콘 그림 뒤에 깔리는 **네모 테두리**. 아이콘(112×112)보다 사방 8px 씩 크다.

```
크기: 128 × 128
파일: card_frame_common.png / card_frame_rare.png
      card_frame_epic.png   / card_frame_legendary.png
```

- 네 귀퉁이에 짧은 금속 꺾쇠. 변 가운데는 비어 있어도 된다
- 안쪽 **x 8~120 · y 8~120 은 완전히 비운다** — 아이콘이 그 위에 그려진다
- 등급색은 A의 표와 같게

---

## D. 제목 1장

```
크기: 560 × 96
파일: leveluptitle.png
```

시안 위쪽의 **「LEVEL UP」 금빛 입체 글자**. 이것만은 **글자를 그린다** —
게임 서체로는 이 두께와 광택이 안 나온다.

- 시안 그대로: 금빛 그라데이션에 짙은 테두리, 아래로 떨어지는 그림자
- 좌우의 마름모 장식과 가는 선까지 포함해서 560 안에 넣는다
- `LEVEL UP` 영문만. 한글 부제(「카드를 선택하세요」)는 게임이 얹으므로 넣지 않는다

---

## 내보낼 곳

```
Projects/AVSR/_exchange/in/
├── cardpanel_common.png       216 × 500
├── cardpanel_rare.png         216 × 500
├── cardpanel_epic.png         216 × 500
├── cardpanel_legendary.png    216 × 500
├── cardchip_rarity.png        186 × 40
├── cardchip_level.png         186 × 40
├── card_frame_common.png      128 × 128
├── card_frame_rare.png        128 × 128
├── card_frame_epic.png        128 × 128
├── card_frame_legendary.png   128 × 128
└── leveluptitle.png           560 × 96
```

배경은 전부 마젠타 `#FF00FF`. **API 키를 쓰는 CLI 폴백은 쓰지 마라.**
