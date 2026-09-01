# AVSR — 호스트 상세 카드 레이아웃 (구조안)

> 2026-08-27. **구조만 잡은 것이다. 디자인(색·재질·아이콘)은 별도.**
> 기준: `AVSR_HostSkills.md` v0.3 · `AVSR_JobClasses.md`
>
> 규약 — **요소 이름 = 프리팹 GameObject 이름 = 클라 바인딩 키** (세 곳 동일).
> 여기 적힌 `name` 이 그대로 프리팹 GameObject 이름이 되고 코드가 그 이름으로 찾는다.

---

## 1. 왜 바꾸는가

지금 카드에는 **능력치 4줄과 액티브 스킬 하나**뿐이다. 새로 들어가야 하는 것이 셋이다.

| 없는 것 | 왜 필요한가 |
|---|---|
| **패시브 스킬** | 11명이 갖는다. 안 보이면 그 몸을 고를 이유의 절반이 사라진다 |
| **직업 상시 규칙** | 쉴드가 왜 차는지, 왜 이 몸은 붙어야 하는지가 화면 어디에도 없다 |
| **숙련도 · 파편** | 봉인이 언제 풀리는지 안 보이면 파편이 왜 쌓이는지 모른다 |

---

## 2. 화면 좌표계

| | 값 |
|---|---|
| 기준 해상도 | 720 × 1280 |
| 패널 루트 | `HostSelectPanel` — `[0, 105, 720, 1175]` |
| **이 카드** | `HostDetailCard` — **x 420 ~ 703 · y 250 ~ 987** (283 × 737) |
| 콘텐츠 폭 | x 430 ~ 693 (263) — 좌우 10px 여백 |

> 좌표는 **좌상단 원점 · y 아래로 증가**다 (기존 `_layout_HostSelect.json` 과 같다).

### 세로 예산

카드 737px 안에 다 들어간다. 지금 빈 공간이 257px 이고 새로 넣을 것이 246px 이라,
**스탯 줄 높이만 34 → 30 으로 조이면**(4줄에서 24px 확보) 35px 여유가 남는다.

---

## 3. 요소 전체 (위에서 아래로)

`NEW` = 새로 만드는 것 · `이동` = 있던 것이 자리만 바뀜

| name | x | y | w | h | 종류 | 구분 | 내용 |
|---|---|---|---|---|---|---|---|
| `HostDetailFrame` | 420 | 250 | 283 | 737 | Image |  | 카드 배경 |
| `HostNameEnText` | 430 | 268 | 263 | 28 | Text |  | AMAZON |
| `HostNameKrText` | 430 | 300 | 170 | 24 | Text | `이동` | 아마존 — 폭을 줄여 배지 자리를 낸다 |
| `JobBadge` | 605 | 300 | 88 | 24 | Image | `NEW` | 직업 배지 배경 |
| `JobBadgeText` | 605 | 300 | 88 | 24 | Text | `NEW` | 격투 |
| `JobRuleText` | 430 | 328 | 263 | 34 | Text | `NEW` | 직업 상시 규칙 (최대 2줄) |
| `HostPortraitImage` | 528 | 370 | 64 | 76 | Image | `이동` | 초상 — 80×96 에서 축소 |
| `StatGroupLabel` | 440 | 456 | 120 | 20 | Text | `이동` | 능력치 |
| `StatRow_HP` | 437 | 480 | 250 | 30 | Group | `이동` | 아이콘 + 라벨 + 바 + 숫자 |
| `StatRow_ATK` | 437 | 512 | 250 | 30 | Group | `이동` |  |
| `StatRow_SPD` | 437 | 544 | 250 | 30 | Group | `이동` |  |
| `StatRow_DASH` | 437 | 576 | 250 | 30 | Group | `이동` |  |
| `ActiveSkillCard` | 430 | 618 | 263 | 150 | Image |  | 액티브 스킬 상자 |
| `ActiveSkillLabel` | 442 | 626 | 130 | 18 | Text |  | ACTIVE SKILL |
| `ActiveSkillCooldownText` | 566 | 626 | 116 | 18 | Text | `NEW` | 쿨 8초 — 우측 정렬 |
| `ActiveSkillIcon` | 442 | 650 | 56 | 56 | Image |  |  |
| `ActiveSkillSealIcon` | 442 | 650 | 56 | 56 | Image | `NEW` | 봉인 자물쇠 — 아이콘 위에 덮는다 |
| `ActiveSkillNameText` | 506 | 652 | 176 | 24 | Text |  | 발키리 돌진 |
| `ActiveSkillDescText` | 442 | 712 | 240 | 48 | Text | `이동` | 설명 2줄 |
| `PassiveSkillCard` | 430 | 778 | 263 | 124 | Image | `NEW` | 패시브 스킬 상자 |
| `PassiveSkillLabel` | 442 | 786 | 130 | 18 | Text | `NEW` | PASSIVE SKILL |
| `PassiveSkillChanceText` | 566 | 786 | 116 | 18 | Text | `NEW` | 25% — 확률형만. 상시형은 빈칸 |
| `PassiveSkillIcon` | 442 | 810 | 44 | 44 | Image | `NEW` |  |
| `PassiveSkillSealIcon` | 442 | 810 | 44 | 44 | Image | `NEW` | 봉인 자물쇠 |
| `PassiveSkillNameText` | 494 | 812 | 188 | 22 | Text | `NEW` | 흡혈 |
| `PassiveSkillDescText` | 442 | 858 | 240 | 36 | Text | `NEW` | 설명 2줄 |
| `MasteryGroup` | 430 | 912 | 263 | 62 | Group | `NEW` | 숙련도 · 파편 |
| `MasteryLabel` | 442 | 918 | 120 | 18 | Text | `NEW` | 숙련도 |
| `MasteryValueText` | 573 | 918 | 110 | 18 | Text | `NEW` | Lv 3 / 10 — 우측 정렬 |
| `ShardBarBg` | 442 | 942 | 239 | 10 | Image | `NEW` | 파편 진행 바 |
| `ShardBarFill` | 442 | 942 | 239 | 10 | Image | `NEW` | 채움 (좌측 pivot) |
| `ShardText` | 442 | 956 | 239 | 16 | Text | `NEW` | 파편 18 / 26 |

마지막 요소 끝 y = **974**. 카드 바닥 987 까지 **13px 여백**. 가로 넘침 없음.

### 초상을 줄였다

`80 × 96` → **`64 × 76`**. 그 자리에 얼굴 하나가 들어갈 뿐인데 카드의 13% 를 쓰고 있었다.
줄인 만큼이 그대로 액티브·패시브 카드의 숨 쉴 자리가 됐다 —
액티브 설명이 2줄에서 편하게 들어가고, 패시브 카드 124px 가 통째로 새로 앉는다.

---

## 4. 직업 상시 규칙 — **문구는 4벌뿐이다**

같은 직업 6명 카드에 같은 문장을 반복하지 않는다.
`BattleDirector.JobOf()` 가 평타 방식 + 사거리에서 직업을 뽑아내므로,
UI 는 **그리는 자리에서 호출**한다. 저장하지 않으니 표와 어긋날 수 없다.

| 직업 | 배지 색 | `JobRuleText` 문구 |
|---|---|---|
| **격투** | 적색 | 때릴 때마다 쉴드 (최대 HP 3% · 상한 30%)<br>타격 시 12% 확률로 0.8초 스턴 (빼앗을 수 없는 적 한정) |
| **중거리** | 골드 | 평타가 맞은 자리에서 한 칸 함께 터진다 |
| **원거리** | 블루 | 직선탄으로 한 명씩 정확히 |
| **관통** | 시안 | 줄지어 선 것을 뚫는다 |

> 격투만 두 줄이라 `JobRuleText` 높이 34px 은 **2줄 기준**이다. 나머지 셋은 한 줄이고 아래가 빈다.

---

## 5. 상태별 표시

카드 하나가 네 상태를 그린다. **상태를 만드는 값은 숙련도 하나뿐이다.**

| 상태 | 조건 | 액티브 카드 | 패시브 카드 | 숙련도 그룹 |
|---|---|---|---|---|
| **봉인** | 숙련도 0 | 자물쇠 · 이름/설명 흐림 | 자물쇠 · 흐림 | `Lv 0 / 10` · `파편 6 / 10` |
| **개방** | 숙련도 1~4 | 정상 | 정상 (없으면 §5-1) | `Lv 3 / 10` · `파편 18 / 26` |
| **특수 효과** | 숙련도 5~9 | 설명에 특수 효과 한 줄 추가 | 정상 | 정상 |
| **만렙** | 숙련도 10 | 정상 | 정상 | `Lv 10 / 10` · 바 가득 · `MAX` |

> **봉인이어도 그 몸은 쓴다.** 로비에서 고르고 전장에서 빼앗을 수 있다 — 스킬만 안 나온다.
> 카드가 잠겨 보이는 것은 **스킬 칸**이지 몸이 아니다.

### 5-1. 패시브가 없는 호스트 (12명)

23명 중 **11명만** 패시브를 갖는다. 없는 호스트는:

- `PassiveSkillCard` 를 **끄지 않는다.** 끄면 카드마다 높이가 달라져 아래 요소가 출렁인다
- `PassiveSkillNameText` = `—`, `DescText` = `이 몸에는 패시브가 없다`
- 아이콘은 비활성

---

## 6. 코드가 붙는 자리

| 요소 | 값의 출처 |
|---|---|
| `JobBadgeText` · `JobRuleText` | `BattleDirector.JobOf(HostEntry)` — 그릴 때 호출, 저장 안 함 |
| `ActiveSkillNameText` · `DescText` | `IPlayerDataService.GetActiveSkill(e.ActiveSkillKey)` |
| `ActiveSkillCooldownText` | `HostEntry.ActiveSkillCooldown(fallback)` — 8 / 14 / 20 / 28 |
| `ActiveSkillSealIcon` · `PassiveSkillSealIcon` | `IPlayerDataService.IsSkillSealed(hostKey)` |
| `MasteryValueText` | `IPlayerDataService.GetMastery(hostKey)` |
| `ShardText` · `ShardBarFill` | `GetShards(hostKey)` / `MasteryShardCost(Lv)` — 봉인이면 `UnsealShardCost`(10) |
| 패시브 이름·설명 | **아직 없다** — 테이블이 필요하다. §7 |

---

## 7. 아직 없는 것

| | 내용 |
|---|---|
| **패시브 스킬 테이블** | 액티브는 `ActiveSkillTable` 이 있는데 패시브는 저장할 곳이 없다. `HostEntry` 에 `_passiveSkillKey` 를 더하고 `PassiveSkillTable` 을 만들거나, 액티브 표에 종류 칸을 두는 두 갈래다 |
| **패시브 아이콘 11종** | 발주 대상 |
| **직업 배지 그림 4종** | 발주 대상 — 또는 단색 + 글자로 그림 없이 갈 수도 있다 |
| **봉인 자물쇠** | `hostdetaillockicon` 이 이미 있다. 재활용 가능 |

---

## 8. 디자인에 넘기는 것

**이 문서가 정하는 것**: 요소 이름 · 좌표 · 크기 · 계층 · 무엇이 어디에 들어가는가 · 상태별 표시 규칙

**디자인이 정하는 것**: 색 · 재질 · 폰트 크기 · 여백의 시각적 리듬 · 배지와 자물쇠의 생김새 ·
잠긴 상태를 어떻게 흐리게 보일 것인가 · 파편 바의 채움 표현

좌표는 **구조가 들어간다는 것을 확인한 값**이지 미학적 최종값이 아니다.
디자인 쪽에서 밀거나 당기는 것은 자유롭되, **요소 이름과 계층은 바꾸지 않는다** —
그 이름이 곧 코드가 찾는 키다.
