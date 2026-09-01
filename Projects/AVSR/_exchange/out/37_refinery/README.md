# 37차 — 야간 정유소 장애물 8장

여섯 무대 중 다섯째. `rooftop` 9장이 한 번에 통과했으니 같은 방식으로 간다.

---

## 붙인 자료

| 파일 | 무엇 |
|------|------|
| `BACKGROUND_refinery.png` | **맞출 배경.** 여기서 색·재질을 가져온다 |
| `PASSED_rooftop_*.png` | **직전에 통과한 옥상 세트 9장.** 규격·완성도의 기준 |
| `SHARED_barricade.png` | 정유소가 **같이 쓰는** 파이프 난간 (공통) |
| `SHARED_blade_1.png` | 정유소가 **같이 쓰는** 톱니 (공통) |

---

## 정유소가 쓰는 도형 5가지

```
bulk · pillar · barricade(공통) · blade(공통) · timed_spike
```

**상자(`crate`)도 세로형(`rail`)도 없다.** 크고 높은 것 위주다 —
탱크와 배관이 서 있는 곳이라 낮고 잔잔한 물건이 적다.
배관은 여기서 **주인공**이라 공통 난간을 그대로 쓴다.

---

## 만들 것 — 8장

| 파일명 | 칸 | 캔버스 px | 솟음 |
|--------|-----|-----------|------|
| `obj_refinery_block_1.png` | 1 × 1 | **72 × 102** | 30 |
| `obj_refinery_block_2.png` | 1 × 1 | 72 × 102 | 30 |
| `obj_refinery_block_3.png` | 1 × 1 | 72 × 102 | 30 |
| `obj_refinery_pillar.png` | 1 × 1 | **72 × 166** | 94 |
| `obj_refinery_bulk.png` | 2 × 2 | **144 × 238** | 94 |
| `obj_refinery_timed_spike_1.png` | 2 × 2 | **144 × 144** | 0 |
| `obj_refinery_timed_spike_2.png` | 2 × 2 | 144 × 144 | 0 |
| `obj_refinery_timed_spike_3.png` | 2 × 2 | 144 × 144 | 0 |

**무엇으로 그릴지는 그쪽이 정한다.** 정유소에 있을 법한 물건이면 된다 —
`bulk` 가 저장 탱크든 열교환기든, `pillar` 가 굴뚝이든 증류탑이든 상관없다.

---

## 규격 (옥상 세트와 동일 · 예외 없음)

1. **캔버스 아래 `세로칸 × 72` px 가 발자국**이다. 그림의 **맨 아랫줄이 접지선** — 아래 여백 금지
   - `block` 아래 72 px 발자국 + 위 30 px 솟음
   - `pillar` 아래 72 px 발자국 + 위 94 px 솟음
   - `bulk` 아래 144 px 발자국 + 위 94 px 솟음
   - `timed_spike` 발자국만 144 px, 솟음 없음
2. 가로는 좌우 여백 없이 캔버스를 꽉 채운다
3. 바닥 판은 **바닥에 눕는 정사각**. 마름모 금지.
   외곽은 3장이 **똑같이 고정**하고 안쪽만 3단계로 변한다
4. 블록 3종은 **이어 붙였을 때 칸 경계가 보여야** 한다
5. 알파 있는 PNG. 반투명 픽셀 금지(0 또는 255)
6. `_exchange/in/` 에 평평하게 납품

---

## 다음

`refinery` 다음은 `holding`(보스 아레나) 10장, 그다음 앞선 무대에 빠진 새 도형 4장이다.

```
obj_junkyard_bulk  144 × 238     obj_junkyard_rail  72 × 174
obj_missile_bulk   144 × 238     obj_street_rail    72 × 174
```
