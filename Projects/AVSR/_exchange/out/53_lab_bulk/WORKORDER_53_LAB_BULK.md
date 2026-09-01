# [AVSR] 53차 발주 — 연구소 큰 덩어리 1장

## 무엇이 왜 필요한가

지형지물 `BULK`(2×2 m 큰 덩어리)는 무대마다 그림이 따로 있다. 그런데 **연구소(CH1 앞쪽 여섯 방) 것만 없다.**

```
있다   obj_holding_bulk   obj_junkyard_bulk   obj_missile_bulk
       obj_rooftop_bulk   obj_refinery_bulk
없다   obj_bulk           ← 연구소(기본형)
```

손으로 짠 방 레이아웃 B·D·E 가 CH1 002·003·005 에서 이 덩어리를 쓴다. 지금은 코드가 **수용실(holding) 그림을 빌려서** 띄우고 있다. 빈 상자보다는 낫자는 임시 조치다.

발자국이 2×2 인 물건이 연구소 목록에 이것뿐이라, 크기가 같은 대체품이 없다. 다른 것으로 바꾸면 방 형태가 무너진다.

---

## 규격

| 항목 | 값 |
|---|---|
| 파일명 | **`obj_bulk.png`** (접두어 없음 — 연구소가 기본형이다) |
| 캔버스 | **144 × 238 px** |
| 발자국 | 아래쪽 **144 × 144 px** = 바닥에 닿는 2×2 m |
| 솟는 높이 | 위쪽 **94 px** = 바닥 위로 올라온 몸통 |
| 알파 | **0 또는 255 만.** 반투명 픽셀 없음 |
| 색 수 | 제한 없음. 기존 5장이 10,000~21,000색이다 |
| 배경 | 완전 투명 |

캔버스 규칙은 기존 물건 전부와 같다 — 기둥 `72×166`(발자국 72 + 솟음 94), 덩어리 `144×238`(발자국 144 + 솟음 94).

**아래 144 px 안에 바닥에 닿는 면이 들어와야 한다.** 그 선이 어긋나면 캐릭터가 물건을 뚫고 지나가는 것처럼 보인다.

---

## 크기 참고 (똑같이 만들 것)

```
Assets/BaseResource/InGameMainUI/obj_holding_bulk.png      144×238
Assets/BaseResource/InGameMainUI/obj_junkyard_bulk.png     144×238
Assets/BaseResource/InGameMainUI/obj_missile_bulk.png      144×238
Assets/BaseResource/InGameMainUI/obj_rooftop_bulk.png      144×238
Assets/BaseResource/InGameMainUI/obj_refinery_bulk.png     144×238
```

**이 다섯 장과 같은 실루엣 언어·같은 두께감으로 간다.** 무대만 연구소로 바꾸는 것이다.

---

## 색 참고 (연구소 팔레트)

같은 무대에 이미 서 있는 물건들이다. 이것들 옆에 놓았을 때 한 세트로 보여야 한다.

```
Assets/BaseResource/InGameMainUI/obj_pillar.png        72×166   부서진 석조 기둥
Assets/BaseResource/InGameMainUI/obj_low_cover.png     216×102  누운 파이프 다발
Assets/BaseResource/InGameMainUI/obj_barricade.png     216×102  철망 달린 파이프 난간
Assets/BaseResource/InGameMainUI/obj_block_1.png       72×102   격자 블록
Assets/BaseResource/InGameMainUI/obj_crate_1.png       144×132  철제 상자
```

실측 색:

| 자리 | 값 |
|---|---|
| 몸통 어두운 남색 | `#0B123F` · `#0C133C` · `#0A1132` |
| 중간 톤 | `#1C2134` · `#2A354F` |
| 금속 하이라이트 | `#FCFCFC` (흰색에 가깝게 세게 친다) |
| 포인트 청색 | `#333CA3` |
| 외곽선 | 순검정 `#000000` |

---

## 무엇을 그리나

**연구소의 큰 기계 덩어리.** 사람 키를 넘는 정육면체에 가까운 설비 — 냉각기·전원함·격리 캡슐 같은 것. 뒤에 숨으면 안 보일 만큼 꽉 찬 덩어리여야 한다.

지켜야 할 것:

- **시야를 완전히 가리는 부피.** 이 물건의 존재 이유가 그것이다. 속이 비치거나 다리가 달려 아래가 보이면 안 된다
- **윗면이 보이는 각도.** 기존 5장과 같은 부감이다. 정면 벽처럼 그리면 혼자 튄다
- 정사각에 가까운 발자국. 가로로 눕히거나 세로로 세우면 2×2 가 아니게 보인다

피해야 할 것:

- **파이프 다발·파이프 난간 금지.** 그건 이미 `LOW_COVER`·`BARRICADE` 가 맡고 있다. 같은 방에 같이 서므로 겹치면 방이 파이프밭이 된다
- **나무 상자·철제 상자 금지.** `CRATE` 가 맡고 있다
- **기둥 형태 금지.** `PILLAR` 가 맡고 있다
- 바닥에 눕는 물건 금지. 이건 서 있는 물건이다

---

## 납품

```
Projects/AVSR/_exchange/in/obj_bulk.png
```

파일명 그대로 넣으면 자동으로 `Assets/BaseResource/InGameMainUI/` 로 들어간다.

---

## 확인 기준

1. 144 × 238 px
2. 알파가 0 과 255 뿐 (반투명 픽셀 0개)
3. 아래 144 px 안에 바닥 접지면이 들어와 있다
4. 기존 5장과 나란히 놓았을 때 같은 물건의 다른 무대 판본으로 읽힌다
5. `obj_pillar` · `obj_low_cover` · `obj_barricade` 와 나란히 놓았을 때 같은 방 물건으로 읽힌다
