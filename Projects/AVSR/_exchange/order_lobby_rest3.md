# 로비 남은 3장 — 완료 간판 · 시즌 패스 아이콘 · 이벤트 아이콘

목업에 있는데 게임에 없는 것들이다. 기준 그림을 목업에서 그대로 잘라 뒀다.

## 1. `chestreadybanner.png` — 224 × 49

```
기준: Projects/AVSR/_exchange/ref/banner_ref.png   (224 × 49, 1:1 크기)
```

지금 것(`Assets/BaseResource/LobbyMainUI/chestreadybanner.png`, 208×40)은 **가운데가 뚫려 있다.**
그래서 뒤의 칸 배경이 그대로 비쳐 글자가 안 읽힌다.

기준 그림처럼 **가운데를 어두운 명판으로 채운다.** 금 테두리 + 좌우 월계수는 그대로.
기준 그림의 「완료!」 **글자는 빼고** 빈 명판으로 내보낸다 — 글자는 게임이 얹는다.
배경(명판 바깥)은 완전 투명.

## 2. `seasonpassicon.png` — 64 × 70

```
기준: Projects/AVSR/_exchange/ref/seasonpass_ref.png
```

금테 두른 보라색 방패 안에 금색 별. 배경 완전 투명.

## 3. `eventicon.png` — 72 × 54

```
기준: Projects/AVSR/_exchange/ref/event_ref.png
```

보라색 입장권(티켓). 배경 완전 투명.

## 지켜야 할 것

1. **배경은 완전 투명(알파 0).** 마젠타를 쓰지 마라
2. **글자를 그리지 마라** — 세 장 모두
3. 크기는 위에 적은 그대로

## 내보내는 곳

```
Projects/AVSR/_exchange/in/chestreadybanner.png
Projects/AVSR/_exchange/in/seasonpassicon.png
Projects/AVSR/_exchange/in/eventicon.png
```

도형 스크립트(`ImageDraw` 로 사각형·원을 찍는 식)로 만들지 마라.
API 키를 쓰는 CLI 폴백은 쓰지 마라.
