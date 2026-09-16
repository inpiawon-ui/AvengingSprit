# 재납품 — 모드 카드 그림 2장, **자물쇠와 테두리를 빼라**

지금 그림에는 **자물쇠와 카드 테두리가 같이 그려져** 있다. 게임은 그 위에 자물쇠와
카드 테를 따로 얹으므로 **둘이 겹쳐 두 개로 보인다.**

```
지금: Assets/BaseResource/LobbyMainUI/modeart_survival.png  (200 × 246)
      Assets/BaseResource/LobbyMainUI/modeart_defense.png   (200 × 246)
```

자물쇠는 모드가 열리면 사라져야 하는데, 그림에 박혀 있으면 열려도 그대로 남는다.

## 기준 그림

```
Projects/AVSR/_exchange/ref/modeart_survival_ref.png   212 × 240 (1:1 크기)
Projects/AVSR/_exchange/ref/modeart_defense_ref.png    212 × 240
```

기준 그림에도 자물쇠와 비스듬한 테두리가 같이 찍혀 있다 — **그 둘을 빼고** 뒤의
장면만 남긴다.

## 내보낼 것

| 파일 | 크기 | 내용 |
|---|---|---|
| `modeart_survival.png` | 212 × 240 | 보라빛 골목에 몰려드는 좀비 떼 |
| `modeart_defense.png` | 212 × 240 | 포탑과 그 앞의 좀비 떼, 뒤로 번지는 주황 섬광 |

- **네 귀까지 꽉 채운 직사각형 그림**이다. 모서리를 깎지 마라
- 자물쇠를 그리지 마라
- 금속 테두리·비스듬한 변을 그리지 마라 — 게임이 카드 테를 따로 얹는다
- 장면·색·분위기는 기준 그림 그대로

## 지켜야 할 것

1. 마젠타를 쓰지 마라
2. **글자를 그리지 마라**
3. 크기는 212 × 240 그대로

## 내보내는 곳

`Projects/AVSR/_exchange/in/modeart_survival.png` · `modeart_defense.png`

도형 스크립트로 만들지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라.
