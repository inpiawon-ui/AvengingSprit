# 신규 기획 패키지 대조 — v2.2 카드/에볼루션 + v3.3 버티컬 슬라이스

분석 대상
- `AVENGING_SPIRIT_REBORN_CARD_EVOLUTION_FINAL_PACKAGE_v2.2.zip`
- `AVENGING_SPIRIT_REBORN_CH01_03_VERTICAL_SLICE_FINAL_PACKAGE_v3.3_FINAL_LOCK.zip`
- `AVENGING_SPIRIT_REBORN_CARD_BUILD_EVOLUTION_MASTER_v2.2_CARD_RECIPE_FINAL.xlsx`
- `AVENGING_SPIRIT_REBORN_25_HOST_MASTER_DB_v2.2_FINAL_LOCK.xlsx`

현재 구현이 따르는 것: `Canon/Runtime/CH01_03_RUNTIME_DATA_v1.5.json`

---

## 0. 먼저 확인할 충돌 두 가지

### ① 붙여 주신 카드 도감 이미지는 v1.0 이고 zip 은 v2.2 다

| | 이미지(v1.0) | zip(v2.2 FINAL LOCK) |
|---|---|---|
| 등급 분포 | COMMON 11 · RARE 10 · EPIC 7 · LEGENDARY 4 | COMMON 8 · RARE 12 · EPIC 8 · LEGENDARY 4 |
| C017 | 이동 속도 향상 | 생명 회수 (Life Recovery) |
| C018 | 회피 본능 | 위기 방벽 (Crisis Barrier) |
| C019 | 체력 상승 | 흡수 변환 (Leech Conversion) |
| C020 | 방어 강화 | 불굴 (Lasting Body) |
| C028 | 자원 생성 | 통제 파동 (Control Pulse) |

zip 안에 v2.2 기준으로 다시 그린 도감 이미지가 들어 있다
(`VISUAL_C_CARD_ENCYCLOPEDIA_001_016.png` / `VISUAL_D_..._017_032.png`).
**어느 쪽을 정본으로 둘지 먼저 정해야 한다.** 아래 분석은 zip(v2.2)을 기준으로 한다.

### ② 방 크기가 3~4배로 커졌다

| | 현재(v1.5) | 신규(v3.3) |
|---|---|---|
| 방 폭 | **8.4 m 고정** | **24 / 28 / 32 m** |
| 방 높이 | 14 m | 14 / 17 m |

세로형 9:16 화면에서 폭 24~32 m 는 **한 화면에 방이 안 들어온다.**
카메라가 따라다니는 구조로 바뀌어야 하고, 이동 속도·사거리·탄속의 체감이 전부 달라진다.
이건 코드 몇 줄이 아니라 **인게임 카메라 정책의 변경**이다.

---

## 1. 호스트 — 21종 → 25종 (공개 23 + 히든 2)

`H01`~`H23` 이 적 `EN_01`~`EN_23` 과 1:1 로 묶였다. 우리 21종과의 대응:

| 신규 | 공식명 | 우리 키 |
|---|---|---|
| H01 | AMAZON | `amazon` |
| H02 | AMAZON ELITE | `amazon_elite` |
| H03 | COMMANDO — BOMB | `commando_grenade` |
| H04 | COMMANDO — LASER | `commando_laser` |
| H05 | COMMANDO — MACHINE GUN | `commando_mg` |
| **H06** | **COMMANDO — MISSILE** | `actor_missile_merc` (배우 전용 → 승격 필요) |
| H07 | DRAGON — BLUE | `dragon_blue` |
| **H08** | **DRAGON — GREEN** | **없음 — 신규** |
| H09 | DRAGON — RED | `salamander` + `dragoon` (**둘이 하나에 대응 — 정리 필요**) |
| H10 | GANGSTER — GUN | `gangster` |
| H11 | GANGSTER — TOMMY GUN | `thug` |
| H12 | GURU | `guru` |
| H13 | HOOPER — GUN | `hopper` |
| H14 | HOOPER — SUBMACHINE GUN | `hopper_smg` |
| H15 | MAGICIAN — DARK | `medium` |
| H16 | MAGICIAN — LIGHT | `white_wizard` |
| H17 | NINJA — CHAIN | `ninja_chain` |
| H18 | NINJA — SHURIKEN | `ninja` |
| H19 | ROBOT | `robot` |
| H20 | BASEBALL PLAYER | `baseball` |
| H21 | SNOW WOMAN | `snowwoman` |
| H22 | VAMPIRE | `vampire` |
| **H23** | **DEATH** | **없음 — 신규** |
| **H24** | MISS DARLING / JENNIFER (히든) | **없음 — CH1~3 런타임 제외** |
| **H25** | SUPERHUMAN (히든) | **없음 — CH1~3 런타임 제외** |

**해야 할 일**
- 신규 3종: H06(승격) · H08 독룡 · H23 사신 — 이 셋은 원작 시트가 **있다**
- `salamander` / `dragoon` 중복 정리 — 정본은 Red Dragon 하나
- 히든 2종(H24/H25)은 CH1~3 에서 제외 — 지금은 손대지 않는다

### 스탯 체계가 바뀌었다
현재는 실수치(HP 85, ATK 10…). 신규는 **평균 100 기준 인덱스**
(예: H23 DEATH = HP 120 / ATK 126 / 공속 68 / 이동 92 / 사거리 82, S 티어).
+ `Attack Interval(초)` 과 `Range Units(m)` 실수치가 따로 붙어 있다.

### 필살기 25종
호스트마다 고유 필살기 1종, 계수 4.2~7.4.
현재 우리는 **12종을 21명이 돌려 쓴다.** 예) `bullet_hell` 을 6명이 공유.

---

## 2. 카드 / Evolution — 통째로 새 시스템

### 지금과 무엇이 다른가

| | 현재 (버프 35종) | 신규 (카드 32종) |
|---|---|---|
| 성장 단위 | 버프 1장 = 고정 효과 | 카드 1장 × **Lv1~5** |
| 재선택 | 중첩 합산 | **같은 카드 = Lv+1** (최대 5) |
| 등급 | 없음 | COMMON 8 / RARE 12 / EPIC 8 / LEGENDARY 4 |
| 등장 확률 | 균등 가중치 | 60% / 28% / 9.5% / 2.5% |
| 보유 한도 | 없음 | **빌드 슬롯 8** (동일 카드는 슬롯 추가 안 씀) |
| 분류 | 없음 | 7종 (ATTACK/PROJECTILE/AREA/SURVIVAL/MOBILITY/UTILITY/SPECIAL) |
| 조합 | 시너지 8종 (내부 판정) | **Evolution 11종** — 카드 2장 보유 시 자동 특수공격 |
| 해금 | 챕터 | **Host 마스터리 Lv7** 또는 Research |

### Evolution 11종 (카드 A + 카드 B → 자동 특수공격, 슬롯 최대 3)

| | 조합 | 결과 | 쿨 | 모티프 | 해금 |
|---|---|---|---|---|---|
| EVO01 | C003 마무리 본능 + C011 확장 충격 | 죽음의 낫 | 12s | H23 | **스타터** |
| EVO02 | C026 화염 각인 + C011 | 적룡의 업화 | 14s | H09 | H09 Lv7 |
| EVO03 | C007 추가 발사 + C009 유도 보정 | 환영 수리검 | 12s | H18 | **스타터** |
| EVO04 | C010 반사 궤도 + C001 공격 증폭 | 유령 홈런 | 14s | H20 | H20 Lv7 |
| EVO05 | C030 유령 포대 + C009 | 자동 로켓 포대 | 16s | H19 | H19 Lv7 |
| EVO06 | C025 냉기 각인 + C012 잔류 지대 | 백색 눈보라 | 14s | H21 | H21 Lv7 |
| EVO07 | C007 + C013 폭발 메아리 | 미사일 포화 | 18s | H06 | H06 Lv7 |
| EVO08 | C025 + C008 관통 코어 | 빙룡의 숨결 | 15s | H07 | H07 Lv7 |
| EVO09 | C027 저주 각인 + C012 | 심연 인장 | 16s | H15 | H15 Lv7 |
| EVO10 | C013 + C011 | 전면 폭격 | 17s | H03 | H03 Lv7 |
| EVO11 | C008 + C031 과충전 회로 | 프리즘 레이저 | 15s | H04 | H04 Lv7 |

- **재료 카드는 소모되지 않는다.** 조건을 채우면 다음 카드 선택에 후보 1칸이 100% 확정 등장.
- Evolution 은 일반 카드 풀에 안 나온다.
- 빙의(Host 교체)해도 유지. **유령 상태에서는 공격·쿨다운이 PAUSE.**
- 이미지(EVO06 "설녀의 눈보라" / EVO09 "암흑 의식")와 zip(“백색 눈보라” / “심연 인장”)의 **이름이 다르다** — v2.2 기준으로 통일 필요.

### 새로 만들어야 하는 판정 규칙 (`CARD_PROC_RULE_RUNTIME.json`)
- **유효 기본공격 8타 카운터** — C030 유령 포대 · C032 영혼 복제가 이걸 쓴다
  (터렛/에코/에볼루션/필살기/상태틱/환경 피해는 카운트에 **안 들어간다**, 재귀 금지)
- C002 정밀 조준 — 유효 타깃이 **정확히 1** 일 때만
- C005 연속 압박 — 같은 대상 1~4타 = 25/50/75/100%, 대상 변경 또는 1.5초 공백 시 초기화
- C022 퀵 셋업 — 이동→정지 후 **첫** 기본공격의 준비시간 단축, 하한 `max(0.18s, 기본×55%)`
- C024 전투 스텝 — 기본공격 완료 후 1.2초 이동속도 버프 (지속시간만 갱신)

---

## 3. 방 / 루트 — 34방 → 48방

| | 현재 | 신규 |
|---|---|---|
| 방 수 | 34 (CH1 11 / CH2 11 / CH3 12) | **48** (CH1 12 / CH2 16 / CH3 20) |
| 방 타입 | 자체 22종 | **7종** TUTORIAL/COMBAT/EVENT/BOSS/REST/SHOP/ELITE |
| 지오메트리 | 자체 | **6 템플릿** TWIN_PLATFORM/RING/OFFSET_COVER/SPLIT_LEVEL/PILLAR_CROSS/LANE_WIDE |
| 스폰 | 런타임 랜덤 | **좌표 345개 수작업 배치** (ROOM_SPAWN) |
| 해저드 | 없음 | NONE 18 / TIMED 16 / ROTATING 14 |
| 보스 | 3종 | **6종** (B01 Crusher ~ B06 Sludge, HP 1650~6900) |
| 목표 플레이 시간 | — | CH1 15분 · CH2 19.6분 · CH3 26.1분 |
| 신규 클리어율 목표 | — | CH1 81% · CH2 56% · CH3 32% |

### 현재 없는 방 타입 — 전부 신규 구현
- **SHOP** (5방) — 3개 제시, 총 구매 2회·카드 1장, Host 회복 25~35%
- **EVENT** (5방) — 18종 이벤트 (CHOICE / COMBAT), 골드·Ghost HP 비용
- **REST** (5방) — Host 19~25% · Ghost 18~22% 회복 + **Evolution 1회 해결**
- **ELITE** (7방) — 베이스 적에 BERSERK/WARDEN/VOID 수식, 예고시간 보너스

### 보상 곡선 (CH1 실측)
전투방 EXP 42 → 56 → 70, 골드 20 → 28 → 36 / 보스방 EXP 141·155, 골드 108·116

---

## 4. 변경 규모 순위

| 순위 | 항목 | 규모 | 리소스 필요 |
|---|---|---|---|
| 1 | **방 폭 8.4 m → 24~32 m + 카메라 추적** | 매우 큼 (인게임 전면) | 배경·타일 |
| 2 | **카드 시스템 (레벨·등급·슬롯·해금)** | 큼 | 카드 아이콘 32 |
| 3 | **Evolution 11종 자동 특수공격** | 큼 | 이펙트 11 |
| 4 | **48방 데이터 + 스폰 345 좌표** | 중간 (데이터 임포터) | 방 배경 6 템플릿 |
| 5 | **SHOP / EVENT / REST 방 3종** | 중간 | UI 3화면 |
| 6 | **보스 3 → 6종** | 중간 | 보스 스프라이트 3 |
| 7 | **호스트 21 → 23 (H06·H08·H23)** | 작음 | 캐릭터 3종 × 5방향 |
| 8 | **필살기 12 → 25종** | 중간 | 아이콘 25 · 이펙트 |
| 9 | 스탯을 인덱스 체계로 재정렬 | 작음 (임포터) | — |

---

## 5. 권장 순서

기획서 `UNITY_HANDOFF.md` 가 제시한 순서와 우리 상황을 합치면:

```
0단계  정본 교체 결정  — v1.5 → v2.2/v3.3, 카드 도감 버전 확정
1단계  방 스케일 + 카메라  ← 여기가 안 정해지면 아래가 전부 다시 됨
2단계  48방 데이터 임포터 (ROOM_MASTER/GEOMETRY/SPAWN/WAVE/REWARD)
3단계  카드 32종 (레벨·등급·슬롯) — 지금 버프 35종을 대체
4단계  Evolution 11종
5단계  SHOP / EVENT / REST 방
6단계  보스 6종
7단계  호스트 3종 추가 + 필살기 25종
```

**1단계를 먼저 못 박아야 한다.** 방이 3배 넓어지면 이동속도·사거리·탄속·명중반경·
빙의 사거리가 전부 다시 잡혀야 하고, 지금까지의 플레이 튜닝이 무효가 된다.

---

## 6. ChatGPT 리소스 워크오더 목록 (초안)

원작 시트가 있는 것과 없는 것을 나눠 둔다.

### A. 원작 시트 있음 — 앵커 붙여서 발주
| 워크오더 | 내용 | 장수 |
|---|---|---|
| WO-A1 | H08 DRAGON GREEN 5방향 × 8동작 | 40 |
| WO-A2 | H23 DEATH 5방향 × 8동작 | 40 |
| WO-A3 | H06 COMMANDO MISSILE 5방향 × 8동작 | 40 |
| WO-A4 | 보스 B03~B06 (Kingpin/Python/Robot Snakes/Sludge) | 4세트 |

### B. 원작에 없음 — 새로 그림
| 워크오더 | 내용 | 장수 |
|---|---|---|
| WO-B1 | 카드 아이콘 32종 (등급 테두리 4종 포함) | 36 |
| WO-B2 | Evolution 아이콘 11종 + 발동 이펙트 11종 | 22 |
| WO-B3 | 필살기 아이콘 25종 | 25 |
| WO-B4 | 방 배경 6 템플릿 × 3 챕터 | 18 |
| WO-B5 | SHOP / EVENT / REST 화면 UI | 3화면 |
| WO-B6 | 해저드 2종 (TIMED / ROTATING) | 6 |

> 발주 전 반드시 원작 시트를 먼저 연다 — 상상해서 자세를 적지 않는다.
