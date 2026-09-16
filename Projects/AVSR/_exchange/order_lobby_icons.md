# 발주 — 로비 아이콘 6종 (낱장 6개)

> ⚠ **그리기 스크립트로 만들지 마라.** `ImageDraw` 로 도형을 조립하는 방식은 불합격이다.
> 손으로 그린 픽셀아트여야 한다. **매끈한 완전 대칭·그라데이션 채우기가 보이면 불합격이다.**

## 기준 목업 (반드시 열어 보고 따른다)

```
Projects/AVSR/Reference/Mockups/lobby_hub_v2.png      941 × 1672
```

| 파일 | 목업 위치 | 무엇인가 |
|---|---|---|
| `hostbuttonart.png` | x 50~168 · y 1492~1610 | 하단 HOST 칸의 **초록 헬멧 군인** |
| `chapterbuttonart.png` | x 346~458 · y 1496~1608 | 하단 PLAY 칸의 **파란 유령** |
| `shopbuttonart.png` | x 656~774 · y 1492~1610 | 하단 SHOP 칸의 **빨간 보물상자** |
| `ghostsearchicon.png` | x 58~182 · y 250~344 | 유령 수색 판의 **떠 있는 유령** |
| `chesttimeicon.png` | x 96~126 · y 764~794 | 상자 칸의 **하얀 시계** |
| `modecentericon.png` | x 314~396 · y 1212~1278 | 시나리오 모드의 **펼친 지도** |

## 낱장별로 무엇을 그리나

### 1. `hostbuttonart.png` — **118 × 118**
초록 철모를 쓴 군인이 **소총을 들고 정면을 본다.** 가슴까지 나오는 반신.
게임 안 호스트(`Assets/BaseResource/Unit/` 의 soldier 계열)와 같은 인물로 읽혀야 한다.

### 2. `chapterbuttonart.png` — **112 × 112**
둥근 **파란 유령**. 큰 검은 눈 두 개와 벌린 입, 아래는 물결치는 꼬리.
게임 마스코트다 — `Assets/BaseResource/LobbyMainUI/ghostavatar.png` 와 같은 유령으로 읽혀야 한다.

### 3. `shopbuttonart.png` — **118 × 118**
**빨간 보물상자**가 닫힌 채 살짝 비껴 있다. 금색 쇠 띠와 자물쇠판.

### 4. `ghostsearchicon.png` — **124 × 94**
`chapterbuttonart` 와 **같은 유령**이 옆으로 날아가는 자세. 팔을 벌리고 꼬리가 뒤로 흐른다.
둘레에 옅은 **푸른 빛무리**.

### 5. `chesttimeicon.png` — **36 × 36**
**하얀 원**에 검은 테. 안에 짧은 바늘(위)과 긴 바늘(오른쪽). 눈금은 넣지 마라 —
36 px 에서는 점으로 뭉친다.

### 6. `modecentericon.png` — **82 × 68**
**펼친 지도.** 가운데가 접혀 두 쪽으로 서고, 위에 길·표식이 몇 개. 모서리가 살짝 말렸다.

## 규격

| 항목 | 값 |
|---|---|
| 배경 | 마젠타 `#FF00FF` 한 색 (투명은 내가 뚫는다). 그림 안쪽에 마젠타를 쓰지 마라 |
| 여백 | 그림이 가장자리에 **최소 4 px** 닿지 않는다 |
| 화풍 | 픽셀아트, 안티에일리어싱 없이 또렷하게. 색 띠가 **계단으로** 나뉜다 |
| 윤곽 | 바깥에 **어두운 1 px 윤곽**. 어두운 판 위에 얹히므로 형태가 떠 보여야 한다 |

## 팔레트 (목업 스포이드)

| 쓰임 | 색 |
|---|---|
| 유령 흰 몸 | `#EAF6FF` / 그늘 `#9CC8E8` |
| 유령 푸른 빛 | `#4DC8FF` |
| 군복 초록 | `#2E5C2A` / 밝은 면 `#5A8C4A` |
| 살색 | `#E8A878` / 그늘 `#B07048` |
| 빨간 상자 몸통 | `#B02820` / 밝은 면 `#E05038` |
| 금색 | `#FFD766` / 어두운 면 `#C08820` |
| 지도 종이 | `#EDE0C0` / 그늘 `#C0A878` |
| 윤곽 | `#0A0A12` |

## 검수 기준

1. 크기가 각각 118×118 · 112×112 · 118×118 · 124×94 · 36×36 · 82×68
2. 배경 마젠타 한 색, 가장자리 4 px 여백
3. 목업의 해당 자리와 나란히 놓았을 때 **같은 그림으로 읽힌다**
4. `chapterbuttonart` 와 `ghostsearchicon` 이 **같은 유령**으로 읽힌다
5. 도형이 아니다 — 완전 대칭·매끈한 곡선이 없다
6. 글자가 없다

## 내보내는 곳

```
Projects/AVSR/_exchange/in/hostbuttonart.png
Projects/AVSR/_exchange/in/chapterbuttonart.png
Projects/AVSR/_exchange/in/shopbuttonart.png
Projects/AVSR/_exchange/in/ghostsearchicon.png
Projects/AVSR/_exchange/in/chesttimeicon.png
Projects/AVSR/_exchange/in/modecentericon.png
```

작업용 스크립트·임시 파일은 프로젝트 밖 임시 폴더에서 쓰고 지운다.

API 키를 쓰는 CLI 폴백은 쓰지 마라.
