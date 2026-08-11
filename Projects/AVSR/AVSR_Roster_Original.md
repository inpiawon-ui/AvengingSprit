# AVSR 로스터 — 원작 기준 (2026-08-11 전환)

원작 스프라이트 시트 39장을 받아 **로스터를 원작 전체로 확장한다.**
정본 v1.5 의 적 18종은 원작의 부분집합이었다 — 목업 15종이 오히려 원작에 충실했다.

앵커: [`_exchange/out/15_original/`](_exchange/out/15_original) — 캐릭터마다
`{시트명}__sheet.png`(배경 지운 2배)와 `{시트명}__palette.png`(쓸 수 있는 색 전부).
원본 시트: [`Reference/Original/`](Reference/Original)

---

## 왜 팔레트가 핵심인가

원작에 충실하다는 것은 사실상 **색이 같다**는 뜻이다. 실루엣이 조금 달라도 색이
같으면 같은 게임으로 읽히고, 색이 다르면 아무리 잘 그려도 남의 게임이 된다.

지금 우리 캐릭터는 폭력배 21색 · 갱스터 74색 · 샐러맨더 84색으로 제각각이라
한 화면에 사는 것처럼 보이지 않는다. 원작은 캐릭터마다 **20~47색**이다.

---

## 적 · 플레이어블 (22종)

| 슬러그 | 원작 시트 | 정본 | 팔레트 | 상태 |
|---|---|---|--:|---|
| `ghost` | Playable Characters - Ghost | — | 28 | 그림 있음(임시) |
| `gangster` | Enemies - Gangster (Gun) | E001 | 39 | 동작 보유 · **원작 기준 재작업** |
| `thug` | Enemies - Gangster (Tommy Gun) | E004 | 39 | 동작 보유 · **원작 기준 재작업** |
| `amazon` | Enemies - Amazon | E002 대체 | 33 | idle 보유(폐기했던 것 복귀) |
| `amazon_elite` | Enemies - Amazon Elite | E006 대체 | 40 | 신규 |
| `hopper` | Enemies - Hopper (Gun) | — | 38 | 신규 |
| `hopper_smg` | Enemies - Hopper (Sub-Machine Gun) | — | 39 | 신규 |
| `commando_mg` | Enemies - Commando (Machine Gun) | — | 38 | 신규 |
| `commando_laser` | Enemies - Commando (Laser) | E007 | 35 | idle 보유(`assault_gangster`) |
| `commando_grenade` | Enemies - Commando (Grenade) | E005 | 39 | 신규 |
| `commando_missile` | Enemies - Commando (Missiles Pack) | E013 | 43 | 신규 |
| `salamander` | Enemies - Dragon (Green) | E003 | 38 | idle 보유 · 동작 반려 |
| `dragoon` | Enemies - Dragon (Red) | E016 | 39 | 신규 |
| `dragon_blue` | Enemies - Dragon (Blue) | — | 38 | 신규 |
| `guru` | Enemies - Guru | E009 | 31 | idle 보유 |
| `white_wizard` | Enemies - Magician (Light) | E010 | 38 | idle 보유 · 동작 납품됨 |
| `medium` | Enemies - Magician (Dark) | E012 | 38 | 신규(폐기했던 `wizard_green` 자리) |
| `ninja` | Enemies - Ninja (Shuriken) | E011 | 33 | idle 보유 |
| `ninja_chain` | Enemies - Ninja (Chain) | — | 36 | idle 보유(폐기했던 `ninja_red`) |
| `robot` | Enemies - Robot | E008 | 20 | idle 보유 |
| `baseball` | Enemies - Slugger | E015 | 32 | idle 보유 |
| `snowwoman` | Enemies - SnowWoman | — | 33 | idle 보유(폐기했던 것 복귀) |
| `vampire` | Enemies - Vampire | E014 | 47 | idle 보유 |

> 정본에만 있고 원작 시트가 없는 것: `E017 방패병`·`E018 센서드론`
> (정본이 RE:BORN 신규로 표기한 둘이다. 원작에 없는 게 맞다.)

## 보스 (6종)

| 슬러그 | 원작 시트 | 정본 | 팔레트 |
|---|---|---|--:|
| `robot_snakes` | Bosses - Robot Snakes | **B01** | 66 |
| `crusher` | Bosses - Crusher | **B02** (Demolisher) | 76 |
| `python` | Bosses - Python | **B03** | 71 |
| `guardian` | Bosses - Guardian | — | 63 |
| `kingpin` | Bosses - Kingpin | — | 56 |
| `sludge` | Bosses - Sludge | — | 45 |

## 그 밖

| 시트 | 쓸 곳 |
|---|---|
| Miscellaneous - HUD | ENERGY/BOSS 게이지 · 열쇠 · **호스트 초상 16종** |
| Miscellaneous - Fonts | 픽셀 폰트 (지금 미결 항목) |
| Miscellaneous - Title Screen | 타이틀 화면 (목업 없어 미결이던 것) |
| Miscellaneous - Items & GO! Sign | 아이템 · 안내 표시 |
| Miscellaneous - Death | 사망 연출 |
| Miscellaneous - Prologue / Start / Stage End Cutscenes | 컷신 |
| Playable Characters - Miss Darling | 스토리 인물 |
| Unused Content - Super Human | 미사용 — 보류 |

---

## 원작이 알려준 동작 목록

드래곤 시트 기준. 지금 우리 8프레임보다 넓다.

```
Move 4 · Jump · Attack 2 · Jump Atk 2 · Crouch · Crouch Atk 2
Climb 2 · Hurt · Dead 2(해골) · Burned · Possessed 2 · 무기 이펙트
```

우리에 없던 것 셋:
- **`Possessed` 2프레임** — 빙의 진입·이탈. 정본 `CHARACTER_RESOURCE_SPEC` 도 요구한다
- **`Dead` 가 해골** — 우리 die1/die2 는 주저앉기다. 원작은 뼈만 남는다
- **`Burned`** — 사인에 따라 시체가 다르다. 화염 계열(샐러맨더·드라군)에 쓸 자리

`Jump`·`Crouch`·`Climb` 은 원작이 횡스크롤 액션이라 있던 것으로,
우리 쿼터뷰 오토어택에는 쓸 자리가 없다. 넣지 않는다.
