# 발주 — 모드 카드 판 2장 + 유령 1장

지금 모드 카드는 **테두리만** 있고 속이 비어 배경이 비친다. 목업은 카드가
**불투명한 판**이고 그 위에 그림·글자가 얹힌다. 그래서 「네모가 목업과 다르다」는 지적이 나왔다.

## 기준 그림

```
Projects/AVSR/Reference/Mockups/parts/ref_modecard.png      좌우 카드
Projects/AVSR/Reference/Mockups/parts/ref_centerframe.png   가운데 카드
Projects/AVSR/Reference/Mockups/lobby_hub_v2.png            전체 (x 380~560 · y 280~470 의 큰 유령)
```

## 만들 것

| 기준 | 파일 | 크기 | 9-slice | 속 |
|---|---|---|---|---|
| `ref_modecard.png` | `modecard_side.png` | 230 × 414 | 늘리지 않음 | **채움** |
| `ref_centerframe.png` | `modecard_center.png` | 394 × 440 | 늘리지 않음 | **채움** |
| 목업 가운데 유령 | `ghostsearchbig.png` | 220 × 170 | — | — |

### 카드 두 장 — 테두리 + 속을 한 장으로

기준 그림을 보면 카드가 이렇게 생겼다.

- 위 `60 %` — 그림이 들어갈 자리. **짙은 남색으로 비워 둔다**(그림은 게임이 얹는다)
- 아래 `40 %` — **한 단 더 어두운 글자판.** 여기에 제목·부제·버튼이 올라간다
- 두 구역 사이에 가는 경계선
- 바깥은 기준 그림의 금속 테두리 그대로

⚠ 글자·그림·자물쇠를 그리지 마라. **빈 판**이어야 한다.
⚠ `modecard_side` 는 기준 그림처럼 **기운 사다리꼴**이다. 좌우 반전은 내가 한다.

### 유령 한 장

목업 가운데에서 보물상자 위를 나는 **큰 유령**. 지금 화면엔 이게 빠져 있다.
음표 말풍선까지 같이 그린다. 배경은 마젠타.

## 규격

- 배경 마젠타 `#FF00FF` 한 색. 그림 안쪽에 마젠타를 쓰지 마라
- 카드 두 장은 **속까지 불투명**하다 (비우지 마라)
- 글자를 그리지 마라

## 검수

기준 그림과 나란히 놓고 **같은 카드로 읽히면 합격**. 속이 비어 있으면 불합격.

## 내보내는 곳

```
Projects/AVSR/_exchange/in/modecard_side.png
Projects/AVSR/_exchange/in/modecard_center.png
Projects/AVSR/_exchange/in/ghostsearchbig.png
```

작업용 스크립트·임시 파일은 프로젝트 밖 임시 폴더에서 쓰고 지운다.

API 키를 쓰는 CLI 폴백은 쓰지 마라.
