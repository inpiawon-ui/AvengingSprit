# 발주 (재작업) — 이벤트 창 부품 5장, **캔버스를 꽉 채워서**

## 무엇이 잘못됐나

방금 받은 7장 중 **액자 두 장(`eventframe`·`shrineframe`)은 좋다. 그대로 쓴다.**
나머지 5장은 그림이 **캔버스 한가운데에 납작하게** 그려져 왔다.

| 파일 | 캔버스 | 실제 그림이 찬 세로 | 문제 |
|---|---|---|---|
| `eventacceptbutton.png` | 430 × **104** | y 18 ~ 78 (60px) | 44px 가 빈 여백 |
| `eventdeclinebutton.png` | 430 × **84** | y 20 ~ 63 (43px) | 41px 가 빈 여백 |
| `eventcostpill.png` | 256 × **46** | y 14 ~ 34 (20px) · x 35 ~ 220 | 절반이 빈 여백 |
| `shrinechoiceslot.png` | 420 × **96** | y 25 ~ 70 (45px) | **글자 두 줄이 안 들어간다** |
| `shrinehintpill.png` | 320 × **44** | y 14 ~ 30 (16px) | 실 두께밖에 안 된다 |

게임은 이 그림을 **캔버스 크기 그대로** 화면에 놓는다. 늘리지 않는다.
그래서 그림이 캔버스보다 작으면 **버튼이 실제보다 얇게** 보이고,
`shrinechoiceslot` 은 안에 들어갈 **제목 한 줄 + 설명 한 줄**이 판 밖으로 삐져나간다.

## 고칠 것 — 이것 하나뿐이다

**그림이 캔버스를 꽉 채운다. 상하좌우 여백은 4px 이내.**

모양·색·화풍은 방금 준 것 그대로 좋다. **세로로 늘리라는 뜻이 아니라,
버튼을 그만큼 두껍게(모서리 두께·안쪽 면적을 키워서) 다시 그리라는 뜻이다.**
픽셀아트이므로 기존 그림을 늘려서 뭉개지 말고 다시 그린다.

---

## 다시 그릴 5장

| 파일 | 크기 | 무엇 | 글자가 얹히는 자리 (무늬 금지) |
|---|---|---|---|
| `eventacceptbutton.png` | 430 × 104 | 붉은 용암 팔각 버튼. 테두리 주황 발광 | x 45~385 · y 20~84 |
| `eventdeclinebutton.png` | 430 × 84 | 같은 팔각, 발광 없는 회색 돌 | x 45~385 · y 16~68 |
| `eventcostpill.png` | 256 × 46 | 양끝 붉은 해골 · 아래로 흐르는 핏자국 | x 55~200 · y 8~38 |
| `shrinechoiceslot.png` | 420 × 96 | 흰 대리석 판 · 청록 발광 테두리 · 좌우 끝 사방 별 | x 30~390 · y 10~86 |
| `shrinehintpill.png` | 320 × 44 | 청록 테두리 · 좌우 끝 사방 별 | x 45~275 · y 8~36 |

> `shrinechoiceslot` 은 **두 줄이 들어가는 판**이다.
> 상점 진열 칸 `Assets/BaseResource/InGameMainUI/shopitemslot.png`(560×104)를
> 열어 보면 같은 성격의 판이다 — 그 판이 캔버스를 어떻게 채우는지 참고한다.

## 참고 (이미 잘 나온 것 · 손대지 말 것)

- `Projects/AVSR/_exchange/in/eventframe.png` (620×792)
- `Projects/AVSR/_exchange/in/shrineframe.png` (620×860)

두 액자와 **같은 도트 크기·같은 테두리 두께·같은 색**으로 맞춘다.
액자 안에 놓였을 때 한 세트로 보여야 한다.
원본(마젠타 배경)은 `Projects/AVSR/_exchange/in/_src/*_raw.png` 에 있다.

## 규칙

- **배경은 마젠타 `#FF00FF` 단색.** 투명 PNG 로 주지 않는다. 알파는 이쪽에서 뚫는다
- 크기 정확히. 글자는 그리지 않는다
- 픽셀아트. 안티에일리어싱으로 뭉개지 않는다

## 내보낼 곳

```
Projects/AVSR/_exchange/in/
├── eventacceptbutton.png     430 × 104
├── eventdeclinebutton.png    430 × 84
├── eventcostpill.png         256 × 46
├── shrinechoiceslot.png      420 × 96
└── shrinehintpill.png        320 × 44
```

**API 키를 쓰는 CLI 폴백은 쓰지 마라.**
