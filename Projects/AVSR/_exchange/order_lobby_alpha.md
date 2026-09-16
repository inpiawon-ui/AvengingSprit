# 재납품 — 같은 그림, 배경만 **투명**으로

지난 납품은 배경을 마젠타 한 색으로 받았다. 그런데 마젠타를 뚫으면 **반투명이 살지 않는다** —
상자 뒤 빛무리, 테두리 바깥 글로우처럼 부드럽게 번지는 부분이 통째로 잘려 나간다.
실제로 지금 프로젝트의 상자 그림은 반투명 픽셀이 **0개**다.

## 할 일

지난에 그린 아래 그림들을 **디자인을 바꾸지 말고 그대로**, 배경만 마젠타 대신
**투명(알파 0)** 으로 다시 내보낸다. 빛무리·글로우·부드러운 가장자리는 **반투명 그대로** 둔다.

```
chest_wood      chest_silver    chest_gold      chest_magic
panelframe      chestslotframe  buttonblue      buttongold
actionframe_blue  actionframe_gold
modecardframe_center  modecardframe_side
modecard_side   modecard_center
hudpill         goldicon        gemicon         plusbutton
mailbutton      settingsbutton  notifybadge
ghostsearchicon ghostsearchbig  chesttimeicon   modecentericon
hostbuttonart   chapterbuttonart shopbuttonart  chestreadybanner
```

같은 것이 프로젝트에도 들어가 있으니 모양 확인에 쓴다 —
`Assets/BaseResource/LobbyMainUI/` (단, 거기 것은 내가 마젠타를 뚫어 글로우가 잘린 판본이다).

## 지켜야 할 것

1. **배경은 완전 투명(알파 0).** 마젠타를 쓰지 마라
2. **빛무리·글로우·부드러운 가장자리는 반투명으로 남긴다** — 이번 재납품의 목적이다
3. 크기·모양·색은 지난 납품과 **똑같이**. 새로 디자인하지 마라
4. 「가운데 비움」이던 것(`panelframe` · `modecardframe_*`)은 그대로 가운데가 투명이다
5. 글자를 그리지 마라

## 검수

지난 납품본과 나란히 놓았을 때 **같은 그림**이고, 빛무리 자리에 **반투명 픽셀이 있으면** 합격.

## 내보내는 곳

`Projects/AVSR/_exchange/in/` 에 같은 파일 이름 그대로 덮어쓴다.

API 키를 쓰는 CLI 폴백은 쓰지 마라.
