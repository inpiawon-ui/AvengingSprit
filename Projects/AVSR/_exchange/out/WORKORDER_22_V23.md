# 22차 워크오더 — v2.3 카드 / v3.3 슬라이스 전환

기준 문서
- `AVENGING_SPIRIT_REBORN_CARD_BUILD_EVOLUTION_MASTER_v2.3_CHAPTER_SYNC.xlsx`
- `AVENGING_SPIRIT_REBORN_CH01_03_VERTICAL_SLICE_FINAL_PACKAGE_v3.3_FINAL_LOCK.zip`
- 원작 시트: `Projects/AVSR/_exchange/out/15_original/`

납품 위치: `Projects/AVSR/_exchange/in/` (평평하게, 하위 폴더 없이)
파일명은 아래 표의 이름을 **그대로** 쓴다. 대소문자 구분.

---

## 공통 규격

| 항목 | 값 |
|---|---|
| 배경 | 완전 투명 (알파 0) |
| 색 | 원작 팔레트 우선. 새로 만드는 것도 원작 채도·명도에 맞춘다 |
| 안티에일리어싱 | **금지** — 픽셀 경계가 뭉개지면 게임에서 흐릿하게 나온다 |
| 외곽선 | 원작과 동일한 1px 어두운 라인 |

> 캐릭터·보스는 **원작 시트에 원본이 있다.** 상상해서 그리지 말고 해당 시트를 열어
> 그 자세·비율·팔레트를 그대로 따른다. 없는 방향만 유추해서 채운다.

---

## A. 캐릭터 3종 — 원작 시트 있음 (최우선)

정본 로스터가 H01~H23 으로 확정되면서 우리에게 없는 몸이 셋이다.

| 코드 | 이름 | 원작 시트 (`15_original/`) | 앵커 상태 |
|---|---|---|---|
| H06 | COMMANDO — MISSILE | `Enemies - Commando (Missiles Pack)__sheet.png` (596×758) | **완전** — 다른 코만도와 같은 구성 |
| H08 | DRAGON — GREEN | `Enemies - Dragon (Green)__sheet.png` (624×1142) | **완전** — 적룡·청룡과 같은 구성 |
| H23 | DEATH | `Miscellaneous - Death__sheet.png` (602×678) | **부족 — 아래 참고** |

> H06·H08 은 시트에 Move·Attack·Jump·Crouch·Climb·Hurt·Dead·Burned·Possessed 가
> 다 있다. 그대로 옮기면 된다.

### ⚠️ H23 DEATH 는 앵커가 6장뿐이다

시트를 열어 확인했다. **Move 4장 + Attack 2장**이 전부다.
원작에서 이 캐릭터는 적이 아니라 **이스터에그**였다 — 4분간 가만히 있으면 나타나
쫓아오고, 낫으로 호스트 몸을 즉사시킨다. 빙의 대상이 아니었으므로
Climb·Hurt·Dead·Burned·Possessed 가 아예 없다.

확실한 것 (반드시 지킬 것)
- 분홍 로브 + 파란 낫 + 해골 얼굴 + 붉은 눈
- 로브 밑단은 파랑, 테두리는 노랑
- 머리 위로 흰 깃털 같은 것이 솟아 있다
- 다리가 안 보인다 — 로브가 바닥까지 덮고 **떠 있는 것처럼** 움직인다

유추해서 채울 것
- `walk1/walk2` — 원작 Move 4장을 5방향으로 재해석. **걸음이 아니라 미끄러지듯**
- `hit` · `die1/die2` · `possess1/possess2` — 다른 로브 종(구루·매지션)의 피격·사망
  자세를 참고하되 실루엣은 사신 것을 유지
- `atk1/atk2` — 원작 Attack 2장(낫 치켜듦 → 낫 휘두름)을 5방향으로

> 다리가 없는 실루엣이라 방향 구분이 특히 어렵다. **`s`(정면)는 얼굴이 정면을 보고
> 낫이 오른쪽, `n`(뒤)은 로브 등판과 낫 자루만 보이게** 확실히 갈라 그린다.

### 각 캐릭터 규격

캔버스 **96 × 96**, 발끝이 아래에서 8px 위. 5방향 × 8동작 = **40장**.

방향 접미사: `s`(정면·아래) · `se`(오른쪽아래) · `e`(오른쪽) · `ne`(오른쪽위) · `n`(뒤·위)
> 왼쪽은 좌우 반전으로 만들므로 **오른쪽만** 그린다.

동작 접미사: `(없음)`=대기 · `walk1` · `walk2` · `atk1` · `atk2` · `hit` · `die1` · `die2`

파일명: `unit_{키}_{방향}_{동작}.png` (대기는 `unit_{키}_{방향}.png`)

```
예) unit_dragon_green_ne_walk1.png
    unit_dragon_green_s.png
```

키: H06 → `commando_missile` · H08 → `dragon_green` · H23 → `death`

> ⚠️ **`s`(정면)은 옆모습 변주가 아니라 실제 정면이어야 한다.** 기존 21종이 전부
> 옆모습을 조금씩 바꿔 그려서 방향이 화면에서 안 읽힌다. 같은 실수를 반복하지 않는다.
> `n`은 등이 보이는 뒷모습.

---

## B. 카드 아이콘 32종

캔버스 **128 × 128**, 아이콘은 중앙 96 × 96 안에.

| 파일명 | 카드 | 분류 |
|---|---|---|
| `card_c001.png` | 공격 증폭 | ATTACK |
| `card_c002.png` | 정밀 조준 | ATTACK |
| `card_c003.png` | 마무리 본능 | ATTACK |
| `card_c004.png` | 갑옷 분쇄 | ATTACK |
| `card_c005.png` | 연속 압박 | ATTACK |
| `card_c006.png` | 가속 탄체 | PROJECTILE |
| `card_c007.png` | 추가 발사 | PROJECTILE |
| `card_c008.png` | 관통 코어 | PROJECTILE |
| `card_c009.png` | 유도 보정 | PROJECTILE |
| `card_c010.png` | 반사 궤도 | PROJECTILE |
| `card_c011.png` | 확장 충격 | AREA |
| `card_c012.png` | 잔류 지대 | AREA |
| `card_c013.png` | 폭발 메아리 | AREA |
| `card_c014.png` | 연쇄 번짐 | AREA |
| `card_c015.png` | 보스 압축 | AREA |
| `card_c016.png` | 유령 갑주 | SURVIVAL |
| `card_c017.png` | 생명 회수 | SURVIVAL |
| `card_c018.png` | 위기 방벽 | SURVIVAL |
| `card_c019.png` | 흡수 변환 | SURVIVAL |
| `card_c020.png` | 불굴 | SURVIVAL |
| `card_c021.png` | 유령 가속 | MOBILITY |
| `card_c022.png` | 퀵 셋업 | MOBILITY |
| `card_c023.png` | 회피 잔상 | MOBILITY |
| `card_c024.png` | 전투 스텝 | MOBILITY |
| `card_c025.png` | 냉기 각인 | UTILITY |
| `card_c026.png` | 화염 각인 | UTILITY |
| `card_c027.png` | 저주 각인 | UTILITY |
| `card_c028.png` | 통제 파동 | UTILITY |
| `card_c029.png` | 필살기 공명 | UTILITY |
| `card_c030.png` | 유령 포대 | SPECIAL |
| `card_c031.png` | 과충전 회로 | SPECIAL |
| `card_c032.png` | 영혼 복제 | SPECIAL |

분류별 색조 — 아이콘만 보고도 계열이 읽혀야 한다.

| 분류 | 색 |
|---|---|
| ATTACK | 붉은 주황 |
| PROJECTILE | 하늘 |
| AREA | 자주 |
| SURVIVAL | 초록 |
| MOBILITY | 청록 |
| UTILITY | 남보라 |
| SPECIAL | 금색 |

### 등급 테두리 4종
캔버스 **128 × 128**, 가운데는 비우고 테두리만.

`card_frame_common.png` (회색) · `card_frame_rare.png` (파랑) ·
`card_frame_epic.png` (보라) · `card_frame_legendary.png` (금)

---

## C. Evolution 11종

### 아이콘 — 캔버스 **128 × 128**

| 파일명 | 이름 | 모티프 호스트 |
|---|---|---|
| `evo_01.png` | 죽음의 낫 | H23 DEATH |
| `evo_02.png` | 적룡의 업화 | H09 적룡 |
| `evo_03.png` | 환영 수리검 | H18 닌자 |
| `evo_04.png` | 유령 홈런 | H20 야구선수 |
| `evo_05.png` | 자동 로켓 포대 | H19 로봇 |
| `evo_06.png` | 백색 눈보라 | H21 설녀 |
| `evo_07.png` | 미사일 포화 | H06 미사일 코만도 |
| `evo_08.png` | 빙룡의 숨결 | H07 청룡 |
| `evo_09.png` | 심연 인장 | H15 다크 매지션 |
| `evo_10.png` | 전면 폭격 | H03 폭탄 코만도 |
| `evo_11.png` | 프리즘 레이저 | H04 레이저 코만도 |

> 모티프 호스트의 원작 그림을 열어 그 무기·색을 아이콘에 반영한다.

### 발동 이펙트 — 캔버스 **192 × 192**, 각 4장 시퀀스

파일명: `evofx_{번호}_{1~4}.png` (예: `evofx_06_1.png`)

각 Evolution 의 공격 형태:

| | 형태 | 이펙트 방향 |
|---|---|---|
| EVO01 | 낫이 가로지름 | 좌→우 호를 그리는 낫날 |
| EVO02 | 원뿔 화염 | 앞으로 벌어지는 불 |
| EVO03 | 수리검 8개 궤도 | 회전 후 발사 |
| EVO04 | 야구공 도탄 | 공 + 궤적 |
| EVO05 | 로켓 포대 2기 | 설치물 + 발사 연기 |
| EVO06 | 이동하는 눈보라 | 원형 눈보라 |
| EVO07 | 미사일 10발 | 위에서 쏟아지는 궤적 |
| EVO08 | 관통 냉기 레인 | 직선 얼음 |
| EVO09 | 인장 기둥 5개 | 바닥 마법진 + 기둥 |
| EVO10 | 순차 폭격 8발 | 원형 폭발 |
| EVO11 | 프리즘 레이저 | 쓸고 지나가는 광선 |

---

## D. 필살기 아이콘 25종

캔버스 **96 × 96**. 파일명 `psg_h01.png` ~ `psg_h25.png`.

| | 이름 | | 이름 |
|---|---|---|---|
| H01 | 발키리 돌진 | H14 | 크로스 파이어 |
| H02 | 여왕의 충격파 | H15 | 심연 의식 |
| H03 | 전면 폭격 | H16 | 성광의 문 |
| H04 | 궤도 레이저 | H17 | 사슬 난무 |
| H05 | 무한 탄창 | H18 | 환영 수리검 |
| H06 | 미사일 포화 | H19 | 기계 군단 |
| H07 | 빙룡의 숨결 | H20 | 유령 홈런 |
| H08 | 독룡의 늪 | H21 | 설녀의 폭설 |
| H09 | 적룡의 업화 | H22 | 진홍의 밤 |
| H10 | 처형의 여섯 발 | H23 | 사신의 수확 |
| H11 | 거리의 탄막 | H24 | 구원의 레이저 |
| H12 | 천공장 | H25 | 초인의 일격 |
| H13 | 도약 사격 | | |

---

## E. 방 배경 6 템플릿 × 3 챕터 = 18장

방 규격이 확정됐다 — **폭 8 m 고정, 높이 14 m 또는 17 m.**
화면 폭 720px 기준 **90 px/m** 이므로:

| 높이 | 캔버스 |
|---|---|
| 14 m | **720 × 1260** |
| 17 m | **720 × 1530** |

세로로 스크롤되므로 **위아래 끝이 이어질 필요는 없다** (방마다 끊긴다).

| 템플릿 | 성격 |
|---|---|
| `TWIN_PLATFORM` | 좌우 두 단 |
| `RING` | 가운데가 비고 둘레로 도는 |
| `OFFSET_COVER` | 엄폐물이 어긋나게 |
| `SPLIT_LEVEL` | 위아래 높이 차 |
| `PILLAR_CROSS` | 기둥이 십자로 |
| `LANE_WIDE` | 뻥 뚫린 넓은 길 |

챕터 테마

| | 이름 | 분위기 |
|---|---|---|
| CH1 | Haunted Harbor | 항구 · 밤 · 푸른 안개 |
| CH2 | Neon Underworld | 네온 뒷골목 · 자주/청록 |
| CH3 | Bio-Mechanical Citadel | 생체기계 요새 · 초록/금속 |

파일명: `room_{템플릿소문자}_{ch1|ch2|ch3}_{h14|h17}.png`
```
예) room_ring_ch1_h14.png
```
> 18장 = 6 템플릿 × 3 챕터. 높이는 우선 **h14 만** 그리고, h17 은 그 뒤에 받는다.

---

## F. 방 UI 3화면 — 상점 · 이벤트 · 휴식

기준 해상도 **720 × 1280** 세로. 박스 목업(PNG) 로 먼저 받는다.

| 파일명 | 화면 | 담을 것 |
|---|---|---|
| `screen_shop.png` | 상점 | 카드 3장 제시 · 가격 · 보유 골드 · Host/Ghost 회복 버튼 · 구매 한도(총 2회, 카드 1장) |
| `screen_event.png` | 이벤트 | 이벤트 이름 · 삽화 자리 · 선택지 2~3개 · 비용(골드 또는 Ghost HP) 표시 |
| `screen_rest.png` | 휴식 | Host 회복 % · Ghost 회복 % · **Evolution 1회 해결** 버튼 |

---

## G. 해저드 2종

| 파일명 | 내용 | 캔버스 |
|---|---|---|
| `hazard_timed_1~3.png` | 주기적으로 켜지는 바닥 함정 (꺼짐→예고→발동) | 180 × 180 |
| `hazard_rotating_1~4.png` | 회전하는 장애물 4프레임 | 180 × 180 |

---

## 우선순위

먼저 오면 바로 붙일 수 있는 순서다.

1. **B 카드 아이콘 32 + 테두리 4** — 카드 시스템이 첫 작업이다
2. **E 방 배경 (h14 9장 먼저)** — 48방 임포터가 그 다음이다
3. **A 캐릭터 3종**
4. **C Evolution 아이콘 11** (이펙트는 나중)
5. **F 방 UI 3화면**
6. **D 필살기 아이콘 25**
7. **G 해저드**
