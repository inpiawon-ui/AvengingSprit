# 발주 — 방 아래 「방 밖 바닥」 나머지 5장: `roomapron_{무대}.png`

샘플 `roomapron_junkyard.png` 가 통과했다. **같은 방식으로 아래 5장**을 한 번에 한다.

통과한 샘플(반드시 먼저 열어 볼 것): `Projects/AVSR/_exchange/in/roomapron_junkyard.png`
— 방 바닥 `Assets/BundleResource/RoomFloor/roomfloor_env_junkyard.png` **맨 아래 줄 바로 밑에** 붙여 보면
어떻게 이어지는지 보인다. 이번 5장도 똑같이 한다.

## 왜 필요한가

하단 조작판을 걷고 조작 버튼(D패드·빙의 버튼)을 게임 화면 위에 띄웠다.
방은 세로 936 px 로 정해져 있어서 긴 폰(20:9)에서는 방 아래가 **최대 약 520 px** 빈다.
그 자리에 **방 울타리(아래 벽) 바깥의 바닥**을 깐다. 그 위에 조작 버튼이 떠 있다 — 걷는 곳이 아니라 배경이다.
게임이 이 그림을 한 톤 어둡게 눌러서 깐다.

## 대상 — 무대마다 원본 두 장을 먼저 연다 (`Assets/BundleResource/RoomFloor/`)

| 만들 파일 | 방 바닥 (이 그림 **맨 아래 줄** 밑에 붙는다) | 같은 무대 좌우 벽 |
|---|---|---|
| `roomapron_missile.png`  | `roomfloor_env_missile.png`  | `roomside_missile.png`  |
| `roomapron_street.png`   | `roomfloor_env_street.png`   | `roomside_street.png`   |
| `roomapron_rooftop.png`  | `roomfloor_env_rooftop.png`  | `roomside_rooftop.png`  |
| `roomapron_lab.png`      | `roomfloor_env_lab.png`      | `roomside_lab.png`      |
| `roomapron_refinery.png` | `roomfloor_env_refinery.png` | `roomside_refinery.png` |

## 무엇을 그리나 — 샘플과 같다

- 맨 위 가장자리가 방 바닥 그림 **맨 아래 줄과 이어져야** 한다(아래 벽 바깥쪽 면 · 그 밑 땅)
- 원본(바닥·좌우 벽)과 **같은 무대 · 같은 화풍 · 같은 픽셀 크기**
- 위에서 아래로 갈수록 **점점 어두워져** 맨 아래 60 px 쯤은 거의 검정 — 화면마다 아래가 잘려 쓰인다
- 게임은 **위쪽부터** 쓴다. 폰마다 위 110 ~ 520 px 만 보인다 — **위쪽 200 px 가 제일 중요하다**
- 좌우 끝은 자르지 않은 듯 자연스럽게 (태블릿에서는 양옆 벽 그림이 이어 붙는다)

## 규격

| 항목 | 값 |
|---|---|
| 파일명 | 위 표의 `roomapron_{무대}.png` |
| 크기 | **720 × 540 px** |
| 형식 | PNG, 완전 불투명(RGB). 마젠타·알파 쓰지 마라 |

## 하지 말 것

- 방 안 바닥을 다시 그려 넣지 마라 — 이것은 방 **밖**이다
- 가운데에 눈에 띄는 큰 물체를 두지 마라 — D패드(왼쪽 아래)·버튼(오른쪽 아래)이 그 위에 뜬다
- 사람·캐릭터·글자·숫자 금지
- 무대끼리 섞지 마라 — 미사일 기지 밑에 쓰레기장 땅을 깔면 안 된다

## 검수 기준

1. 5장 전부 720 × 540, 불투명
2. 각 무대 방 바닥 아래에 붙였을 때 이음매가 안 튄다
3. 아래로 갈수록 어두워진다

## 내보내는 곳

`Projects/AVSR/_exchange/in/` 에 위 파일명 그대로 저장한다.
그 폴더가 잠겨 있으면 `Projects/AVSR/_exchange/in/_generated/` 에 저장한다.

5장을 **하나씩 차례로** 만든다. 한 장 끝날 때마다 저장하고 다음 장으로 넘어간다.

API 키를 쓰는 CLI 폴백은 쓰지 마라.
