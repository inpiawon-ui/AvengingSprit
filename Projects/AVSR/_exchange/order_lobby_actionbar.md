# 로비 하단 바 3장 — 바닥 판 · 파란 칸 액자 · 금색 칸 액자

목업의 하단 바는 **어두운 금속 판** 위에 칸 세 개가 얹혀 있다. 지금 게임에는 그 판이
아예 없어서 칸들이 도시 배경 위에 떠 있고, 칸 액자도 모서리가 둥글어 목업과 다르다.

## 기준 그림 — 목업에서 그대로 잘라 뒀다

```
Projects/AVSR/_exchange/ref/actionbar_ref.png    941 × 205   바 전체 (칸 세 개 포함)
Projects/AVSR/_exchange/ref/hostframe_ref.png    267 × 128   왼쪽 파란 칸
Projects/AVSR/_exchange/ref/playframe_ref.png    302 × 140   가운데 금색 칸
```

## 내보낼 것

### 1. `actionbarbackground.png` — 941 × 206

`actionbar_ref.png` 의 **바닥 판만** 그린다. 위에 얹힌 칸 세 개(HOST·PLAY·SHOP)와
글자·아이콘은 **빼라.** 판만 남는다.

- 기준 그림처럼 위 모서리가 계단꼴로 꺾인 어두운 금속 판
- 좌우 끝 120 px 안쪽은 장식, 가운데는 **가로로 늘려도 되는 평평한 결**로 둔다
  (9-slice 로 늘려 쓴다)
- 판 바깥(위쪽 빈 곳)은 완전 투명

### 2. `actionframe_blue.png` — 267 × 128

`hostframe_ref.png` 그대로. 지금 것은 모서리가 둥근데 기준은 **팔각으로 깎인 모서리**다.

- 팔각 테두리, 밝은 하늘색 빛나는 테
- 안쪽은 **짙은 남색으로 채운다** (속이 비면 뒤 배경이 비쳐 글자가 안 읽힌다)
- 글자·아이콘은 그리지 마라 — 게임이 얹는다
- 9-slice 로 늘려 쓴다: 네 귀의 장식은 24 px 안쪽에, 가운데는 평평하게

### 3. `actionframe_gold.png` — 302 × 140

`playframe_ref.png` 그대로. 팔각 모서리, 금색 면 + 짙은 금 테 + 바깥으로 번지는 금빛.
글자·아이콘은 그리지 마라. 9-slice 조건은 위와 같다.

## 지켜야 할 것

1. **배경은 완전 투명(알파 0).** 마젠타를 쓰지 마라
2. **글자를 그리지 마라** — 세 장 모두
3. 크기는 위에 적은 그대로

## 내보내는 곳

```
Projects/AVSR/_exchange/in/actionbarbackground.png
Projects/AVSR/_exchange/in/actionframe_blue.png
Projects/AVSR/_exchange/in/actionframe_gold.png
```

도형 스크립트(`ImageDraw` 로 사각형·원을 찍는 식)로 만들지 마라.
API 키를 쓰는 CLI 폴백은 쓰지 마라.
