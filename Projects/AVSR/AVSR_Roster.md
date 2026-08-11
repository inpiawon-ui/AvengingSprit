# AVSR 로스터 — 정본 대조표

> **자동 생성 — 손으로 고치지 말 것.** `python _gen_roster.py` 로 다시 만든다.
> 출처: [`Canon/Runtime/CH01_03_RUNTIME_DATA_v1.5.json`](Canon/Runtime/CH01_03_RUNTIME_DATA_v1.5.json)

아트 폴더명 · 아틀라스 주소 · 테이블 키를 **하나의 슬러그**로 통일한다.
`Assets/BaseResource/Unit/{슬러그}/` → 아틀라스 `atlas/unit_{슬러그}` → 스프라이트 `unit_{슬러그}_{방향}_{프레임}`.

**아트 순서는 CH1 스폰 수가 정한다.** P0 목표 방 `CH1_N01` 은 갱스터 ×2 + 파이터 ×1 이다.

| 슬러그 | 이름 | 정본 | 빙의 | 조건 | 호스트 | CH1 | CH2 | CH3 | 그림 |
|---|---|---|---|---|---|--:|--:|--:|---|
| `gangster` | 갱스터 | `E001` Gangster | 즉시 |  | `H01` | 16 | 6 |  | 보유 ← `mafia` |
| `fighter` | 파이터 | `E002` Fighter | 조건부 | BackAttack OR Down | `H02` | 10 | 5 | 6 | **필요** |
| `thug` | 폭력배 | `E004` Thug | 즉시 |  | `H02` | 3 |  |  | 보유 ← `rambo` |
| `salamander` | 샐러맨더 | `E003` Salamander | 조건부 | Burn3 OR ArmorBreak | `H03` | 1 |  | 2 | 보유 ← `dragon` |
| `baseball` | 야구선수 | `E015` Baseball Player | 즉시 |  | `H16` | 1 |  | 2 | 보유 ← `slugger` |
| `grenadier` | 그레네이더 | `E005` Grenadier | 조건부 | WeakPointBroken OR Down | `H05` | 1 | 1 |  | **필요** |
| `white_wizard` | 화이트 위저드 | `E010` White Wizard | 조건부 | Frozen OR Marked | `H10` | 1 |  | 1 | 보유 ← `wizard` |
| `ninja` | 닌자 | `E011` Ninja | 조건부 | BackAttack OR Marked | `H11` | 1 |  | 1 | 보유 |
| `assault_gangster` | 어설트 갱스터 | `E007` Assault Gangster | 조건부 | ArmorBreak | `H01` |  | 6 | 4 | 보유 ← `rambo_laser` |
| `sensor_drone` | 센서 드론 | `E018` Sensor Drone | 불가 | Summon | — |  | 5 | 5 | **필요** |
| `master_fighter` | 마스터 파이터 | `E006` Master Fighter | 조건부 | Stun OR HP40 | `H02` |  | 3 | 1 | **필요** |
| `missile_merc` | 미사일 용병 | `E013` Missile Pack Mercenary | 조건부 | WeakPointBroken | `H05` |  |  | 4 | **필요** |
| `vampire` | 흡혈귀 | `E014` Vampire | 조건부 | HP35 OR Burned | `H15` |  | 2 | 2 | 보유 |
| `shield_trooper` | 방패병 | `E017` Shield Trooper | 불가 | Shield unit | — |  | 2 | 2 | **필요** |
| `medium` | 영매 | `E012` Medium | 조건부 | Frozen OR Curse3 | `H12` |  | 1 | 2 | **필요** |
| `robot` | 로봇 | `E008` Robot | 조건부 | ShieldBreak OR EMP | `H08` |  | 1 | 1 | 보유 |
| `guru` | 구루 | `E009` Guru | 즉시 |  | `H09` |  | 1 |  | 보유 ← `yogamaster` |
| `dragoon` | 드라군 | `E016` Dragoon | 조건부 | ArmorBreak AND Burn3 | `H20` |  |  | 1 | **필요** |

적 **18종** 중 그림 보유 **10종**, 신규 필요 **8종** — `fighter` · `grenadier` · `sensor_drone` · `master_fighter` · `missile_merc` · `shield_trooper` · `medium` · `dragoon`

## 폐기 (목업에만 있던 5종)

정본에 대응이 없어 뺐다(확정 #10). 그림은 지우지 않고 `Projects/AVSR/_retired/Unit/` 에 보관한다.

| 목업 키 | 이유 |
|---|---|
| `amazoness` | 정본 18적·12호스트에 대응 없음 |
| `hitman` | 정본 18적·12호스트에 대응 없음 |
| `snowwoman` | 정본 18적·12호스트에 대응 없음 |
| `ninja_red` | 정본 18적·12호스트에 대응 없음 |
| `wizard_green` | 정본 18적·12호스트에 대응 없음 |

## 리소스 요구량 — 적과 호스트가 다르다

출처: [`Canon/Docs/CHARACTER_RESOURCE_SPEC.md`](Canon/Docs/CHARACTER_RESOURCE_SPEC.md)

| | 적 18종 | 호스트 12종 (추가) |
|---|---|---|
| 동작 | Idle · 이동 · 공격 · 피격 · 사망 | 궁극기 · 빙의 진입 · 빙의 이탈 |
| 방향 5장 기준 | 35장 | +15장 이상 |

**적 18종과 호스트 12종은 1:1 이 아니다.** 아래 넷은 빙의하면 다른 몸의 호스트 프로필로 바뀐다(v1.5 기술 락 — 널 HostID 제거).

- `E004` 폭력배 → **H02** (파이터) 프로필
- `E007` 어설트 갱스터 → **H01** (갱스터) 프로필
- `E006` 마스터 파이터 → **H02** (파이터) 프로필
- `E013` 미사일 용병 → **H05** (그레네이더) 프로필

즉 이 넷은 **적 그림만** 있으면 되고 호스트 동작(궁극기·빙의 진출입)은 대상 호스트 것을 쓴다.
